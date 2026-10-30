using System.Text.RegularExpressions;
using StbImageSharp;

namespace ImpactoTextil.Api.Services;

public sealed partial class ImagenCatalogoStore(IConfiguration configuracion, IWebHostEnvironment entorno)
{
    public const int MaximoBytes = 10_000_000;
    private static readonly SemaphoreSlim decodificador = new(1, 1);
    private string Carpeta => Path.GetFullPath(configuracion["Catalogo:CarpetaImagenes"]
        ?? Path.Combine(entorno.ContentRootPath, "storage", "catalogo"));

    [GeneratedRegex("^/api/imagenes-catalogo/([a-f0-9]{32}\\.(?:jpg|png))$", RegexOptions.CultureInvariant)]
    private static partial Regex Referencia();

    public string? Ruta(string referencia)
    {
        var coincidencia = Referencia().Match(referencia);
        return coincidencia.Success ? Path.Combine(Carpeta, coincidencia.Groups[1].Value) : null;
    }

    public bool Existe(string referencia) => Ruta(referencia) is { } ruta && File.Exists(ruta);

    public async Task<string> GuardarAsync(IFormFile archivo, CancellationToken cancelacion)
    {
        if (archivo.Length is <= 0 or > MaximoBytes)
            throw new ImagenCatalogoInvalidaException("Selecciona un JPG o PNG de hasta 10 000 000 bytes.");
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new ImagenCatalogoInvalidaException("La imagen debe ser JPG o PNG.");
        await using var entrada = archivo.OpenReadStream();
        using var memoria = new MemoryStream();
        var bloque = new byte[81920];
        int leidos;
        while ((leidos = await entrada.ReadAsync(bloque, cancelacion)) > 0)
        {
            if (memoria.Length + leidos > MaximoBytes) throw new ImagenCatalogoInvalidaException("La imagen supera 10 000 000 bytes.");
            await memoria.WriteAsync(bloque.AsMemory(0, leidos), cancelacion);
        }
        var bytes = memoria.ToArray();
        var png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpg = bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        if ((extension == ".png" && !png) || (extension != ".png" && !jpg))
            throw new ImagenCatalogoInvalidaException("El contenido de la imagen no corresponde al formato indicado.");
        await decodificador.WaitAsync(cancelacion);
        try
        {
            using var datos = new MemoryStream(bytes, writable: false);
            var info = ImageInfo.FromStream(datos);
            if (info is not { Width: > 0, Height: > 0 } imagen || (long)imagen.Width * imagen.Height > 12_000_000)
                throw new ImagenCatalogoInvalidaException("La imagen debe tener como máximo 12 millones de píxeles.");
            var decodificada = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
            if (decodificada.Width != imagen.Width || decodificada.Height != imagen.Height)
                throw new ImagenCatalogoInvalidaException("La imagen no se pudo validar.");
        }
        catch (Exception error) when (error is InvalidOperationException or EndOfStreamException or ArgumentException)
        { throw new ImagenCatalogoInvalidaException("La imagen está dañada o no se puede leer."); }
        finally { decodificador.Release(); }
        Directory.CreateDirectory(Carpeta);
        var nombre = Guid.NewGuid().ToString("N") + (png ? ".png" : ".jpg");
        var ruta = Path.Combine(Carpeta, nombre);
        try { await File.WriteAllBytesAsync(ruta, bytes, cancelacion); }
        catch { if (File.Exists(ruta)) File.Delete(ruta); throw; }
        return "/api/imagenes-catalogo/" + nombre;
    }
}

public sealed class ImagenCatalogoInvalidaException(string mensaje) : Exception(mensaje);
