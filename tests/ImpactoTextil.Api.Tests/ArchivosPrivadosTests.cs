using System.Buffers.Binary;
using System.Text;
using ImpactoTextil.Api.Infrastructure;
using PdfSharp.Pdf;

namespace ImpactoTextil.Api.Tests;

public sealed class ArchivosPrivadosTests : IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "ImpactoTextil_Privados_" + Guid.NewGuid().ToString("N"));
    private static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAAAE0lEQVR4nGP88PoOAwwwwVl4OQCI1gK/rni6WQAAAABJRU5ErkJggg==");
    private AlmacenArchivosPrivadosLocal Almacen => new(carpeta);

    public static IEnumerable<object[]> ArchivosFalsos()
    {
        yield return [new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, TipoArchivoPrivado.DisenoCliente];
        yield return [new byte[] { 255, 216, 255, 217 }, TipoArchivoPrivado.DisenoCliente];
        yield return [Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF"), TipoArchivoPrivado.ComprobantePago];
        yield return [Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj <<>> endobj\n%%EOF"), TipoArchivoPrivado.ComprobantePago];
        yield return [Png[..40].Concat(Png[^12..]).ToArray(), TipoArchivoPrivado.DisenoCliente];
    }

    [Theory, MemberData(nameof(ArchivosFalsos))]
    public async Task FirmaCorrectaConContenidoFalsoOTruncado_NoSeGuarda(byte[] bytes, TipoArchivoPrivado tipo)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Almacen.GuardarAsync(new MemoryStream(bytes), tipo, default));
        Assert.Empty(Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ArchivoVacio_NoSeGuarda()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Almacen.GuardarAsync(new MemoryStream(), TipoArchivoPrivado.DisenoCliente, default));
        Assert.Empty(Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task PngReal_ConservaTodosLosBytesYFlujoDesdeElInicio()
    {
        var original = Png;
        var referencia = await Almacen.GuardarAsync(new MemoryStream(original), TipoArchivoPrivado.DisenoCliente, default);
        var archivo = await Almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.DisenoCliente, default);
        Assert.NotNull(archivo);
        await using var flujo = archivo.Contenido;
        Assert.Equal(0, flujo.Position);
        using var copia = new MemoryStream();
        await flujo.CopyToAsync(copia);
        Assert.Equal(original, copia.ToArray());
        Assert.Equal("image/png", archivo.TipoContenido);
        Assert.StartsWith("disenos/", referencia);
    }

    [Fact]
    public async Task JpegReal_ConservaElOriginalYSuTipo()
    {
        var original = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAEAAQDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD2uiiisDQ//9k=");
        var referencia = await Almacen.GuardarAsync(new MemoryStream(original), TipoArchivoPrivado.DisenoCliente, default);
        var archivo = await Almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.DisenoCliente, default);
        Assert.NotNull(archivo);
        await using var flujo = archivo.Contenido;
        using var copia = new MemoryStream();
        await flujo.CopyToAsync(copia);
        Assert.Equal(original, copia.ToArray());
        Assert.Equal("image/jpeg", archivo.TipoContenido);
        Assert.EndsWith(".jpg", referencia);
    }

    [Fact]
    public async Task PdfReal_ConservaOriginalYNoSeAceptaComoDiseno()
    {
        var original = CrearPdf();
        var referencia = await Almacen.GuardarAsync(new MemoryStream(original), TipoArchivoPrivado.ComprobantePago, default);
        var archivo = await Almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.ComprobantePago, default);
        Assert.NotNull(archivo);
        await using (var flujo = archivo.Contenido)
        {
            using var copia = new MemoryStream();
            await flujo.CopyToAsync(copia);
            Assert.Equal(original, copia.ToArray());
        }
        Assert.Equal("application/pdf", archivo.TipoContenido);
        Assert.Null(await Almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.DisenoCliente, default));
        await Assert.ThrowsAsync<InvalidDataException>(() => Almacen.GuardarAsync(new MemoryStream(original), TipoArchivoPrivado.DisenoCliente, default));
    }

    [Fact]
    public async Task PdfTruncado_NoSeGuardaAunqueConserveLaCabecera()
    {
        var original = CrearPdf();
        await Assert.ThrowsAsync<InvalidDataException>(() => Almacen.GuardarAsync(new MemoryStream(original[..^20]), TipoArchivoPrivado.ComprobantePago, default));
        Assert.Empty(Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task PngRealDeDiezMillonesDeBytes_SeConservaYUnByteAdicionalSeRechaza()
    {
        var original = CrearPngConTamano((int)ValidadorArchivosPrivados.MaximoBytesInclusivo);
        var referencia = await Almacen.GuardarAsync(new MemoryStream(original), TipoArchivoPrivado.DisenoCliente, default);
        Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(carpeta, referencia)));
        var exceso = new byte[original.Length + 1];
        original.CopyTo(exceso, 0);
        await Assert.ThrowsAsync<InvalidDataException>(() => Almacen.GuardarAsync(new MemoryStream(exceso), TipoArchivoPrivado.DisenoCliente, default));
        Assert.Single(Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ArchivoAlteradoEnDisco_NoSeEntregaComoImagenValida()
    {
        var referencia = await Almacen.GuardarAsync(new MemoryStream(Png), TipoArchivoPrivado.DisenoCliente, default);
        await File.WriteAllBytesAsync(Path.Combine(carpeta, referencia), Png[..8]);
        Assert.Null(await Almacen.AbrirLecturaValidadaAsync(referencia, TipoArchivoPrivado.DisenoCliente, default));
    }

    [Fact]
    public async Task RutaFueraDelAlmacen_YRaizPublica_SeDeniegan()
    {
        Assert.Null(await Almacen.AbrirLecturaValidadaAsync("../fuera.png", TipoArchivoPrivado.DisenoCliente, default));
        Assert.Null(await Almacen.AbrirLecturaValidadaAsync(Path.Combine(Path.GetTempPath(), "fuera.png"), TipoArchivoPrivado.DisenoCliente, default));
        Assert.Throws<InvalidOperationException>(() => new AlmacenArchivosPrivadosLocal(Path.Combine(carpeta, "wwwroot", "privados")));
    }

    [Fact]
    public void CabeceraSeparadaYDimensionesExcesivas_NoBastanParaDeclararValidez()
    {
        Assert.False(ValidadorArchivosPrivados.ValidarContenido(Png[..16], Png.Length, TipoArchivoPrivado.DisenoCliente).EsValido);
        var falsa = Png;
        BinaryPrimitives.WriteInt32BigEndian(falsa.AsSpan(16, 4), 6000);
        BinaryPrimitives.WriteInt32BigEndian(falsa.AsSpan(20, 4), 3000);
        var resultado = ValidadorArchivosPrivados.ValidarContenido(falsa, falsa.Length, TipoArchivoPrivado.DisenoCliente);
        Assert.False(resultado.EsValido);
        Assert.Contains("píxeles", resultado.MensajeError);
    }

    private static byte[] CrearPdf()
    {
        using var documento = new PdfDocument();
        documento.AddPage();
        using var memoria = new MemoryStream();
        documento.Save(memoria, false);
        return memoria.ToArray();
    }

    private static byte[] CrearPngConTamano(int tamano)
    {
        // Chunk privado auxiliar válido; no cambia el raster y conserva el cierre IEND.
        var original = Png;
        var relleno = tamano - original.Length - 12;
        var resultado = new byte[tamano];
        original.AsSpan(0, 33).CopyTo(resultado);
        BinaryPrimitives.WriteInt32BigEndian(resultado.AsSpan(33, 4), relleno);
        "caNd"u8.CopyTo(resultado.AsSpan(37, 4));
        uint crc = uint.MaxValue;
        foreach (var dato in resultado.AsSpan(37, relleno + 4))
        {
            crc ^= dato;
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        BinaryPrimitives.WriteUInt32BigEndian(resultado.AsSpan(41 + relleno, 4), ~crc);
        original.AsSpan(33).CopyTo(resultado.AsSpan(45 + relleno));
        return resultado;
    }

    public void Dispose()
    {
        // Solo la carpeta temporal exclusiva creada por esta instancia de prueba.
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
    }
}
