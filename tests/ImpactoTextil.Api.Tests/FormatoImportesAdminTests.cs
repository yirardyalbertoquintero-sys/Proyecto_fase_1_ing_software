using System.Text.Json;
using ImpactoTextil.Api.Contracts;

namespace ImpactoTextil.Api.Tests;

public sealed class FormatoImportesAdminTests
{
    private static readonly JsonSerializerOptions opciones = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Cotizacion_ConservaCentavosGrandesSinConvertirIdentificadoresEnTexto()
    {
        var dto = new LineaCotizacionAdminDto(7, 2, 9999999999999999.99m, 19999999999999999.98m, null);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, opciones));
        Assert.Equal("9999999999999999.99", json.RootElement.GetProperty("precioUnitarioAcordado").GetString());
        Assert.Equal("19999999999999999.98", json.RootElement.GetProperty("subtotal").GetString());
        Assert.Equal(7, json.RootElement.GetProperty("detalle").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("cantidad").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("versionDiseno").ValueKind);
    }

    [Theory]
    [InlineData("\"9999999999999999.99\"")]
    [InlineData("9999999999999999.99")]
    public void PrecioEntrante_AdmiteImporteExactoComoTextoONumero(string valor)
    {
        var dto = JsonSerializer.Deserialize<LineaCotizacionPropuestaDto>("{\"detalle\":7,\"precioUnitarioAcordado\":" + valor + "}", opciones);
        Assert.NotNull(dto);
        Assert.Equal(9999999999999999.99m, dto.precioUnitarioAcordado);
    }

    [Fact]
    public void CotizacionPropuesta_SinAcuerdoVigente_ConservaValoresPendientes()
    {
        var dto = new ResultadoCotizacionAdminDto(1, 2, 1, "Propuesta", 300.01m, null, null, null, null, null);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, opciones));
        Assert.Equal("300.01", json.RootElement.GetProperty("totalPropuesto").GetString());
        foreach (var nombre in new[] { "totalVigente", "anticipoExigible", "saldoPendiente" })
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty(nombre).ValueKind);
    }
}
