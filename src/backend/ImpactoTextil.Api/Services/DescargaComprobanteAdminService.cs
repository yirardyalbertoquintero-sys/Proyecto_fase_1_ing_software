using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class DescargaComprobanteAdminService(IPedidoAdministracionRepository repositorio, IAlmacenArchivosPrivados almacen)
{
    public async Task<ResultadoOperacionAdmin<ArchivoDescargaDto>> DescargarAsync(int administradorId, int pedidoId, int pagoId, CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(CodigoErrorAdmin.PermisoDenegado, "Solo un administrador activo puede consultar comprobantes.");
        var referencia = await repositorio.ObtenerReferenciaComprobanteAsync(pedidoId, pagoId, cancelacion);
        if (!referencia.ExisteRegistro || string.IsNullOrWhiteSpace(referencia.ArchivoRelativo))
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(CodigoErrorAdmin.NoEncontrado, "Este pago no tiene un comprobante disponible para descargar.");
        var archivo = await almacen.AbrirLecturaValidadaAsync(referencia.ArchivoRelativo, TipoArchivoPrivado.ComprobantePago, cancelacion);
        return archivo is null
            ? ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(CodigoErrorAdmin.NoEncontrado, "El comprobante no está disponible o su contenido no es válido.")
            : ResultadoOperacionAdmin<ArchivoDescargaDto>.Exito(new ArchivoDescargaDto(archivo.Contenido, archivo.TipoContenido, referencia.NombreSugerido + archivo.ExtensionNormalizada, archivo.TamanoBytes));
    }
}
