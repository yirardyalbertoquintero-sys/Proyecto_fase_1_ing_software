namespace ImpactoTextil.Api.Infrastructure;

public enum TipoArchivoPrivado
{
    DisenoCliente,
    ComprobantePago
}

public enum FormatoArchivoPrivado
{
    Jpeg,
    Png,
    Pdf
}

public sealed record ResultadoValidacionArchivo(
    bool EsValido,
    FormatoArchivoPrivado? Formato,
    string? TipoContenido,
    string? ExtensionNormalizada,
    string? MensajeError);

public static class ValidadorArchivosPrivados
{
    public const long MaximoBytesInclusivo = 10_000_000L;

    private static ReadOnlySpan<byte> FirmaPng => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> FirmaJpeg => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> FirmaPdf => [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    public static ResultadoValidacionArchivo ValidarContenido(
        ReadOnlySpan<byte> cabecera,
        long tamanoTotalBytes,
        TipoArchivoPrivado tipoEsperado)
    {
        if (tamanoTotalBytes <= 0)
            return new(false, null, null, null, "El archivo está vacío.");

        if (tamanoTotalBytes > MaximoBytesInclusivo)
            return new(false, null, null, null, "El archivo supera el tamaño máximo permitido de 10 MB (10,000,000 bytes).");

        if (cabecera.Length >= FirmaPng.Length && cabecera[..FirmaPng.Length].SequenceEqual(FirmaPng))
            return new(true, FormatoArchivoPrivado.Png, "image/png", ".png", null);

        if (cabecera.Length >= FirmaJpeg.Length && cabecera[..FirmaJpeg.Length].SequenceEqual(FirmaJpeg))
            return new(true, FormatoArchivoPrivado.Jpeg, "image/jpeg", ".jpg", null);

        if (tipoEsperado == TipoArchivoPrivado.ComprobantePago
            && cabecera.Length >= FirmaPdf.Length
            && cabecera[..FirmaPdf.Length].SequenceEqual(FirmaPdf))
        {
            return new(true, FormatoArchivoPrivado.Pdf, "application/pdf", ".pdf", null);
        }

        var mensaje = tipoEsperado == TipoArchivoPrivado.DisenoCliente
            ? "El archivo de diseño debe ser una imagen JPG o PNG real de hasta 10 MB."
            : "El comprobante debe ser un archivo JPG, PNG o PDF real de hasta 10 MB.";

        return new(false, null, null, null, mensaje);
    }
}

public sealed record ArchivoPrivadoAbierto(
    Stream Contenido,
    string TipoContenido,
    string ExtensionNormalizada,
    long TamanoBytes);

public interface IAlmacenArchivosPrivados
{
    Task<string> GuardarAsync(
        Stream origen,
        TipoArchivoPrivado tipo,
        CancellationToken cancelacion);

    Task<ArchivoPrivadoAbierto?> AbrirLecturaValidadaAsync(
        string referenciaRelativa,
        TipoArchivoPrivado tipoEsperado,
        CancellationToken cancelacion);

    Task EliminarSiExisteAsync(string referenciaRelativa, CancellationToken cancelacion);
}

public sealed class AlmacenArchivosPrivadosLocal : IAlmacenArchivosPrivados
{
    private readonly string raizCompleta;

    public AlmacenArchivosPrivadosLocal(string rutaRaizPrivada)
    {
        if (string.IsNullOrWhiteSpace(rutaRaizPrivada))
            throw new ArgumentException("Se requiere una ruta raíz privada fuera de wwwroot.", nameof(rutaRaizPrivada));

        var normalizada = Path.GetFullPath(rutaRaizPrivada);
        if (!normalizada.EndsWith(Path.DirectorySeparatorChar))
            normalizada += Path.DirectorySeparatorChar;

        if (normalizada.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El almacenamiento privado no puede ubicarse dentro de wwwroot.");

        raizCompleta = normalizada;
    }

    public async Task<string> GuardarAsync(
        Stream origen,
        TipoArchivoPrivado tipo,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(origen);

        var subcarpeta = tipo == TipoArchivoPrivado.DisenoCliente ? "disenos" : "comprobantes";
        var directorioDestino = Path.Combine(raizCompleta, subcarpeta);
        Directory.CreateDirectory(directorioDestino);

        var rutaTemporal = Path.Combine(directorioDestino, $"tmp-{Guid.NewGuid():N}.bin");
        byte[] cabecera = new byte[16];
        int bytesCabecera = 0;
        long totalEscrito = 0;

        try
        {
            await using (var salida = new FileStream(rutaTemporal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
            {
                var buffer = new byte[81_920];
                int leidos;
                while ((leidos = await origen.ReadAsync(buffer, cancelacion)) > 0)
                {
                    if (bytesCabecera < cabecera.Length)
                    {
                        var porCopiar = Math.Min(cabecera.Length - bytesCabecera, leidos);
                        Buffer.BlockCopy(buffer, 0, cabecera, bytesCabecera, porCopiar);
                        bytesCabecera += porCopiar;
                    }

                    totalEscrito += leidos;
                    if (totalEscrito > ValidadorArchivosPrivados.MaximoBytesInclusivo)
                        throw new InvalidDataException("El archivo supera el tamaño máximo permitido de 10 MB (10,000,000 bytes).");

                    await salida.WriteAsync(buffer.AsMemory(0, leidos), cancelacion);
                }
            }

            var validacion = ValidadorArchivosPrivados.ValidarContenido(
                cabecera.AsSpan(0, bytesCabecera),
                totalEscrito,
                tipo);

            if (!validacion.EsValido || validacion.ExtensionNormalizada is null)
                throw new InvalidDataException(validacion.MensajeError ?? "El contenido del archivo no es válido.");

            var nombreFinal = $"{Guid.NewGuid():N}{validacion.ExtensionNormalizada}";
            var rutaFinal = Path.Combine(directorioDestino, nombreFinal);
            File.Move(rutaTemporal, rutaFinal, overwrite: false);
            return $"{subcarpeta}/{nombreFinal}";
        }
        catch
        {
            IntentarBorrarSilenciosamente(rutaTemporal);
            throw;
        }
    }

    public async Task<ArchivoPrivadoAbierto?> AbrirLecturaValidadaAsync(
        string referenciaRelativa,
        TipoArchivoPrivado tipoEsperado,
        CancellationToken cancelacion)
    {
        var rutaFisica = ResolverRutaSegura(referenciaRelativa);
        if (rutaFisica is null || !File.Exists(rutaFisica))
            return null;

        var flujo = new FileStream(rutaFisica, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, useAsync: true);
        try
        {
            var tamano = flujo.Length;
            var cabecera = new byte[16];
            var leidos = await flujo.ReadAsync(cabecera, cancelacion);
            flujo.Position = 0;

            var validacion = ValidadorArchivosPrivados.ValidarContenido(
                cabecera.AsSpan(0, leidos),
                tamano,
                tipoEsperado);

            if (!validacion.EsValido || validacion.TipoContenido is null || validacion.ExtensionNormalizada is null)
            {
                await flujo.DisposeAsync();
                return null;
            }

            return new ArchivoPrivadoAbierto(flujo, validacion.TipoContenido, validacion.ExtensionNormalizada, tamano);
        }
        catch
        {
            await flujo.DisposeAsync();
            throw;
        }
    }

    public Task EliminarSiExisteAsync(string referenciaRelativa, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();
        var rutaFisica = ResolverRutaSegura(referenciaRelativa);
        if (rutaFisica is not null)
            IntentarBorrarSilenciosamente(rutaFisica);
        return Task.CompletedTask;
    }

    private string? ResolverRutaSegura(string? referenciaRelativa)
    {
        if (string.IsNullOrWhiteSpace(referenciaRelativa))
            return null;

        var limpia = referenciaRelativa.Trim();
        if (Path.IsPathRooted(limpia) || limpia.Contains("..", StringComparison.Ordinal) || limpia.Contains(':', StringComparison.Ordinal))
            return null;

        var combinada = Path.GetFullPath(Path.Combine(raizCompleta, limpia.Replace('/', Path.DirectorySeparatorChar)));
        return combinada.StartsWith(raizCompleta, StringComparison.OrdinalIgnoreCase)
            ? combinada
            : null;
    }

    private static void IntentarBorrarSilenciosamente(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch (IOException)
        {
            // La tarea de conciliación retirará archivos temporales retenidos por el sistema operativo.
        }
    }
}
