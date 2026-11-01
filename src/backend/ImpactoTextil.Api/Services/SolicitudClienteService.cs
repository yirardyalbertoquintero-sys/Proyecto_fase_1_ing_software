using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Domain.Entities;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class SolicitudClienteService(ISolicitudClienteRepository repositorio, IAlmacenArchivosPrivados almacen,
    TimeProvider reloj, ILogger<SolicitudClienteService> logger)
{
    public async Task<ResultadoOperacionAdmin<SolicitudCreadaDto>> RegistrarAsync(int clienteId, RegistrarSolicitudDto datos,
        IReadOnlyDictionary<string, IFormFile> archivos, CancellationToken ct)
    {
        var lineas = datos.detalles!.Select(d => d!).ToArray();
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (nombre, archivo) in archivos.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            await using var entrada = archivo.OpenReadStream();
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var cabecera = new byte[8];
            var leidos = await entrada.ReadAtLeastAsync(cabecera, 8, throwOnEndOfStream: false, ct);
            var png = leidos == 8 && cabecera.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpg = leidos >= 3 && cabecera[0] == 255 && cabecera[1] == 216 && cabecera[2] == 255;
            if (!((extension == ".png" && png) || (extension is ".jpg" or ".jpeg" && jpg)))
                throw new InvalidDataException("La extensión debe ser JPG/PNG y coincidir con el contenido de la imagen.");
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(cabecera.AsSpan(0, leidos));
            var buffer = new byte[81920];
            while ((leidos = await entrada.ReadAsync(buffer, ct)) > 0) hash.AppendData(buffer.AsSpan(0, leidos));
            hashes[nombre] = Convert.ToHexString(hash.GetHashAndReset());
        }
        static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        // Orden de líneas significativo; nombres/rutas/boundaries de multipart no lo son.
        var huella = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            modalidadEntrega = datos.modalidadEntrega,
            direccionEntrega = datos.modalidadEntrega == "Domicilio" ? Limpio(datos.direccionEntrega) : null,
            total = datos.totalReferencialConfirmado!.Value.ToString("0.00", CultureInfo.InvariantCulture),
            detalles = lineas.Select(d => new
            {
                d.producto,
                d.cantidad,
                talla = Limpio(d.talla),
                color = Limpio(d.color),
                tela = Limpio(d.tela),
                d.empaque,
                d.sinPersonalizacion,
                ideaDiseno = d.sinPersonalizacion ? "Sin personalización" : Limpio(d.ideaDiseno),
                observaciones = Limpio(d.observaciones),
                precio = d.precioBaseConfirmado!.Value.ToString("0.00", CultureInfo.InvariantCulture),
                hashArchivo = d.archivo is null ? null : hashes[d.archivo]
            })
        }));
        var nuevos = new List<string>();
        try
        {
            return await repositorio.RegistrarAsync(clienteId, datos, huella, async (productos, cancelacion) =>
            {
                decimal total = 0;
                foreach (var d in lineas)
                {
                    var p = productos.SingleOrDefault(p => p.id == d.producto);
                    if (p is null || !p.activo || p.talla != d.talla!.Trim() || p.color != d.color!.Trim()
                        || p.tela != d.tela!.Trim() || p.precioBase != d.precioBaseConfirmado)
                        return ResultadoOperacionAdmin<Pedido>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                            "El catálogo cambió. Actualiza las variantes del carrito y confirma de nuevo antes de enviar.", "catalogo");
                    total += p.precioBase * d.cantidad;
                }
                if (total > RegistrarSolicitudDto.MaximoTotal || total != datos.totalReferencialConfirmado)
                    return ResultadoOperacionAdmin<Pedido>.Fallo(CodigoErrorAdmin.ConflictoEstado,
                        "La referencia cambió. Revisa los precios y confirma de nuevo.", "catalogo");
                var ahora = reloj.GetUtcNow();
                var pedido = new Pedido
                {
                    cliente = clienteId,
                    codigo = "IT-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                    estado = "Pendiente",
                    condicionAdministrativa = "Activa",
                    fechaOrden = ahora,
                    modalidadEntrega = datos.modalidadEntrega!,
                    direccionEntrega = datos.modalidadEntrega == "Domicilio" ? Limpio(datos.direccionEntrega) : null,
                    confirmacion = datos.confirmacion,
                    huellaSolicitud = huella
                };
                foreach (var d in lineas)
                {
                    string? referencia = null;
                    if (d.archivo is not null)
                    {
                        await using var entrada = archivos[d.archivo].OpenReadStream();
                        referencia = await almacen.GuardarAsync(entrada, TipoArchivoPrivado.DisenoCliente, cancelacion);
                        nuevos.Add(referencia);
                    }
                    var detalle = new DetallePedido
                    {
                        producto = d.producto,
                        cantidad = d.cantidad,
                        talla = d.talla!.Trim(),
                        color = d.color!.Trim(),
                        tela = d.tela!.Trim(),
                        empaque = d.empaque!,
                        ideaDiseno = d.sinPersonalizacion ? "Sin personalización" : Limpio(d.ideaDiseno),
                        observaciones = Limpio(d.observaciones),
                        precioBaseReferencia = d.precioBaseConfirmado!.Value,
                        archivoDisenoURL = referencia
                    };
                    detalle.VersionDiseno.Add(new VersionDiseno
                    {
                        numeroVersion = 1,
                        archivoURL = referencia,
                        descripcion = detalle.ideaDiseno ?? "Imagen aportada por el cliente",
                        fechaRegistro = ahora,
                        responsableRegistro = clienteId,
                        decision = "Pendiente"
                    });
                    pedido.DetallePedido.Add(detalle);
                }
                return ResultadoOperacionAdmin<Pedido>.Exito(pedido);
            }, ct);
        }
        catch
        {
            // Después de rollback/commit incierto: nunca borrar un archivo que SQL confirmó.
            // Usar un token independiente de la petición que pudo cancelarse.
            foreach (var referencia in nuevos)
            {
                try
                {
                    using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    if (!await repositorio.ArchivoReferenciadoAsync(referencia, limite.Token))
                        await almacen.EliminarSiExisteAsync(referencia, limite.Token);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Conservar archivo de solicitud para conciliación: {Referencia}", referencia); }
            }
            throw;
        }
    }
}
