using PdfSharp.Pdf.IO;
using StbImageSharp;

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
    // Presupuesto técnico de decodificación, separado del límite de bytes del SRS.
    public const long MaximoPixelesDecodificacion = 12_000_000L;

    private static ReadOnlySpan<byte> FirmaPng => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> FirmaJpeg => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> FirmaPdf => [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    public static ResultadoValidacionArchivo ValidarContenido(
        ReadOnlySpan<byte> contenido,
        long tamanoTotalBytes,
        TipoArchivoPrivado tipoEsperado)
    {
        if (tamanoTotalBytes <= 0)
            return new(false, null, null, null, "El archivo está vacío.");

        if (tamanoTotalBytes > MaximoBytesInclusivo)
            return new(false, null, null, null, "El archivo supera el tamaño máximo permitido de 10 MB (10,000,000 bytes).");

        if (contenido.Length != tamanoTotalBytes)
            return new(false, null, null, null, "Se necesita el archivo completo para comprobar su integridad.");

        var esPng = contenido.StartsWith(FirmaPng);
        var esJpeg = contenido.StartsWith(FirmaJpeg);
        if (esPng || esJpeg)
        {
            if ((esPng && (contenido.Length < 12 || !contenido[^12..].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 })))
                || (esJpeg && (contenido.Length < 2 || contenido[^2] != 0xFF || contenido[^1] != 0xD9)))
                return new(false, null, null, null, "La imagen está incompleta o no tiene un cierre de formato válido.");

            try
            {
                var bytes = contenido.ToArray();
                using var entrada = new MemoryStream(bytes, writable: false);
                var info = ImageInfo.FromStream(entrada);
                if (info is not { Width: > 0, Height: > 0 } imagen)
                    return new(false, null, null, null, "La imagen no se puede leer.");
                if ((long)imagen.Width * imagen.Height > MaximoPixelesDecodificacion)
                    return new(false, null, null, null, "La imagen supera el presupuesto técnico de 12 millones de píxeles; utiliza una copia de menor resolución.");
                var decodificada = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
                if (decodificada.Width != imagen.Width || decodificada.Height != imagen.Height)
                    return new(false, null, null, null, "La imagen está dañada.");
                return esPng
                    ? new(true, FormatoArchivoPrivado.Png, "image/png", ".png", null)
                    : new(true, FormatoArchivoPrivado.Jpeg, "image/jpeg", ".jpg", null);
            }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException or EndOfStreamException or InvalidDataException)
            {
                return new(false, null, null, null, "La imagen está dañada o su contenido no se puede decodificar.");
            }
        }

        if (tipoEsperado == TipoArchivoPrivado.ComprobantePago
            && contenido.StartsWith(FirmaPdf))
        {
            var fin = contenido.Length;
            while (fin > 0 && contenido[fin - 1] is 9 or 10 or 13 or 32) fin--;
            if (fin < 5 || !contenido.Slice(fin - 5, 5).SequenceEqual("%%EOF"u8))
                return new(false, null, null, null, "El PDF está incompleto.");
            try
            {
                using var entrada = new MemoryStream(contenido.ToArray(), writable: false);
                using var documento = PdfReader.Open(entrada, PdfDocumentOpenMode.Import);
                if (documento.PageCount < 1)
                    return new(false, null, null, null, "El PDF debe contener al menos una página legible.");
                return new(true, FormatoArchivoPrivado.Pdf, "application/pdf", ".pdf", null);
            }
            // PDFsharp 6.2.4 también usa Exception base para ciertos errores de xref.
            catch (Exception error) when (error.GetType() == typeof(Exception)
                || error is PdfReaderException or InvalidOperationException or ArgumentException or IOException or FormatException or NotSupportedException)
            {
                return new(false, null, null, null, "El PDF está dañado, protegido o no se puede leer.");
            }
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
    private static readonly SemaphoreSlim validador = new(1, 1);
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
        long totalEscrito = 0;

        try
        {
            await using (var salida = new FileStream(rutaTemporal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
            {
                var buffer = new byte[81_920];
                int leidos;
                while ((leidos = await origen.ReadAsync(buffer, cancelacion)) > 0)
                {
                    totalEscrito += leidos;
                    if (totalEscrito > ValidadorArchivosPrivados.MaximoBytesInclusivo)
                        throw new InvalidDataException("El archivo supera el tamaño máximo permitido de 10 MB (10,000,000 bytes).");

                    await salida.WriteAsync(buffer.AsMemory(0, leidos), cancelacion);
                }
            }

            ResultadoValidacionArchivo validacion;
            await using (var archivoCompleto = new FileStream(rutaTemporal, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, useAsync: true))
                validacion = await ValidarFlujoCompletoAsync(archivoCompleto, tipo, cancelacion);

            if (!validacion.EsValido || validacion.ExtensionNormalizada is null)
                throw new InvalidDataException(validacion.MensajeError ?? "El contenido del archivo no es válido.");

            var nombreFinal = $"{Guid.NewGuid():N}{validacion.ExtensionNormalizada}";
            var rutaFinal = Path.Combine(directorioDestino, nombreFinal);
            cancelacion.ThrowIfCancellationRequested();
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
            var validacion = await ValidarFlujoCompletoAsync(flujo, tipoEsperado, cancelacion);

            if (!validacion.EsValido || validacion.TipoContenido is null || validacion.ExtensionNormalizada is null)
            {
                await flujo.DisposeAsync();
                return null;
            }

            cancelacion.ThrowIfCancellationRequested();
            return new ArchivoPrivadoAbierto(flujo, validacion.TipoContenido, validacion.ExtensionNormalizada, tamano);
        }
        catch
        {
            await flujo.DisposeAsync();
            throw;
        }
    }

    private static async Task<ResultadoValidacionArchivo> ValidarFlujoCompletoAsync(
        FileStream flujo, TipoArchivoPrivado tipo, CancellationToken cancelacion)
    {
        var tamano = flujo.Length;
        if (tamano is <= 0 or > ValidadorArchivosPrivados.MaximoBytesInclusivo)
            return ValidadorArchivosPrivados.ValidarContenido([], tamano, tipo);
        await validador.WaitAsync(cancelacion);
        try
        {
            var bytes = new byte[(int)tamano];
            await flujo.ReadExactlyAsync(bytes, cancelacion);
            flujo.Position = 0;
            return ValidadorArchivosPrivados.ValidarContenido(bytes, tamano, tipo);
        }
        finally { validador.Release(); }
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
