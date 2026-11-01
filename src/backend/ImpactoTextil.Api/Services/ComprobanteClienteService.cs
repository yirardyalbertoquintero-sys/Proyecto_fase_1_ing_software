using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class ComprobanteClienteService(IPedidoClienteRepository clientes,
    IPedidoAdministracionRepository pedidos, IAlmacenArchivosPrivados almacen,
    ILogger<ComprobanteClienteService> logger)
{
    public async Task<ResultadoOperacionAdmin<ComprobanteRecibidoDto>> RegistrarAsync(int cliente, int pedidoId,
        RegistrarComprobanteDto datos, IFormFile archivo, CancellationToken ct)
    {
        if (!await clientes.EsPropioAsync(cliente, pedidoId, ct)) return NoEncontrado();
        await using var entrada = archivo.OpenReadStream();
        using var memoria = new MemoryStream();
        await entrada.CopyToAsync(memoria, ct);
        var bytes = memoria.ToArray();
        var validacion = await ValidadorArchivosPrivados.ValidarAcotadoAsync(bytes, TipoArchivoPrivado.ComprobantePago, ct);
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!validacion.EsValido || !(validacion.ExtensionNormalizada == extension
            || validacion.ExtensionNormalizada == ".jpg" && extension == ".jpeg"))
            throw new InvalidDataException(validacion.MensajeError ?? "La extensión y el contenido deben coincidir: JPG, PNG o PDF.");
        var fecha = RelojPedido.TruncarASegundosUtc(datos.fecha);
        var referencia = datos.referencia!.Trim();
        var huella = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            cliente, pedidoId, monto = datos.monto.ToString("0.00", CultureInfo.InvariantCulture), datos.medio,
            referencia, fecha = fecha.ToString("O", CultureInfo.InvariantCulture), archivo = Convert.ToHexString(SHA256.HashData(bytes))
        }));
        string? nuevo = null;
        async Task Limpiar()
        {
            if (nuevo is null) return;
            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!await clientes.ArchivoReferenciadoAsync(nuevo, limite.Token))
                    await almacen.EliminarSiExisteAsync(nuevo, limite.Token);
            }
            catch (Exception ex) { logger.LogWarning(ex, "Conservar comprobante para conciliación: {Referencia}", nuevo); }
        }
        try
        {
            var resultado = await pedidos.EjecutarEnPedidoBloqueadoAsync(pedidoId, async (pedido, guardar, token) =>
            {
                if (pedido.cliente != cliente) return NoEncontrado();
                var previo = pedido.Pago.FirstOrDefault(g => g.confirmacion == datos.confirmacion);
                if (previo is not null)
                    return previo.huellaComprobante is { Length: 32 } hash && CryptographicOperations.FixedTimeEquals(hash, huella)
                        ? ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Exito(new(pedidoId, previo.id, previo.estadoVerificacion, false))
                        : ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                            "La confirmación ya corresponde a otro envío. Conserva la evidencia original y revisa el pedido.");
                if (pedido.condicionAdministrativa == "Archivada" || PagosAdminService.ObtenerCotizacionAceptadaVigente(pedido) is null)
                    return ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                        "Necesitas un pedido activo con cotización aceptada para aportar comprobantes.");
                if (pedido.Pago.Any(g => g.estadoVerificacion != "Rechazado" && g.huellaComprobante is { Length: 32 } hash
                    && CryptographicOperations.FixedTimeEquals(hash, huella)))
                    return ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                        "Esta misma evidencia ya fue recibida. Consulta sus pagos antes de enviar otra vez.");
                using var origen = new MemoryStream(bytes, writable: false);
                nuevo = await almacen.GuardarAsync(origen, TipoArchivoPrivado.ComprobantePago, token);
                var pago = new Pago { pedido = pedidoId, monto = datos.monto, medio = datos.medio!, referencia = referencia,
                    fecha = fecha, estadoVerificacion = "Pendiente", comprobanteURL = nuevo,
                    confirmacion = datos.confirmacion, huellaComprobante = huella };
                pedido.Pago.Add(pago);
                await guardar(token);
                return ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Exito(new(pedidoId, pago.id, "Pendiente", true));
            }, ct);
            if (!resultado.EsExito) await Limpiar();
            return resultado;
        }
        catch { await Limpiar(); throw; }
    }
    private static ResultadoOperacionAdmin<ComprobanteRecibidoDto> NoEncontrado() =>
        ResultadoOperacionAdmin<ComprobanteRecibidoDto>.Fallo(CodigoErrorAdmin.NoEncontrado, "No encontramos ese pedido en tu cuenta.");
}
