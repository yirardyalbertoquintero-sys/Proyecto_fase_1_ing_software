using ImpactoTextil.Api.Contracts;
using ImpactoTextil.Api.Infrastructure;
using ImpactoTextil.Api.Repositories;

namespace ImpactoTextil.Api.Services;

public sealed class DescargaDisenoAdminService(
    IPedidoAdministracionRepository repositorio,
    IAlmacenArchivosPrivados almacen)
{
    public async Task<ResultadoOperacionAdmin<ArchivoDescargaDto>> DescargarDisenoOriginalAsync(
        int administradorId,
        int pedidoId,
        int detalleId,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo está autorizado para descargar archivos originales de diseño.");

        var referencia = await repositorio.ObtenerReferenciaDisenoOriginalAsync(pedidoId, detalleId, cancelacion);
        return await AbrirReferenciaVerificadaAsync(referencia, cancelacion);
    }

    public async Task<ResultadoOperacionAdmin<ArchivoDescargaDto>> DescargarArchivoVersionDisenoAsync(
        int administradorId,
        int pedidoId,
        int detalleId,
        int versionDisenoId,
        CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAdministradorActivoAsync(administradorId, cancelacion) is null)
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(
                CodigoErrorAdmin.PermisoDenegado,
                "Solo un administrador activo está autorizado para descargar versiones de diseño.");

        var referencia = await repositorio.ObtenerReferenciaVersionDisenoAsync(pedidoId, detalleId, versionDisenoId, cancelacion);
        return await AbrirReferenciaVerificadaAsync(referencia, cancelacion);
    }

    private async Task<ResultadoOperacionAdmin<ArchivoDescargaDto>> AbrirReferenciaVerificadaAsync(
        ReferenciaArchivoDiseno referencia,
        CancellationToken cancelacion)
    {
        if (!referencia.ExisteRegistro)
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(
                CodigoErrorAdmin.NoEncontrado,
                "El pedido o detalle de diseño solicitado no existe.");

        if (string.IsNullOrWhiteSpace(referencia.ArchivoRelativo))
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(
                CodigoErrorAdmin.ConflictoEstado,
                "Esta prenda se registró con una idea descrita o bajo la condición «Sin personalización», por lo que no tiene archivo gráfico para descargar.",
                "archivoDisenoURL");

        var abierto = await almacen.AbrirLecturaValidadaAsync(
            referencia.ArchivoRelativo,
            TipoArchivoPrivado.DisenoCliente,
            cancelacion);

        if (abierto is null)
            return ResultadoOperacionAdmin<ArchivoDescargaDto>.Fallo(
                CodigoErrorAdmin.NoEncontrado,
                "No se encontró un archivo de diseño válido e íntegro para la referencia solicitada.");

        var nombreDescarga = $"{referencia.NombreSugerido}{abierto.ExtensionNormalizada}";
        return ResultadoOperacionAdmin<ArchivoDescargaDto>.Exito(new ArchivoDescargaDto(
            contenido: abierto.Contenido,
            tipoContenido: abierto.TipoContenido,
            nombreDescarga: nombreDescarga,
            tamanoBytes: abierto.TamanoBytes));
    }
}
