using System.Net;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImpactoTextil.Api.Tests;

public sealed class DisponibilidadTests
{
    [Fact]
    public async Task ConexionDisponible_Responde200SinDatosInternos()
    {
        using var servidor = CrearServidor(new ConsultaFija(true));
        using var cliente = CrearCliente(servidor);
        var respuesta = await cliente.GetAsync("/api/disponibilidad");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Disponible", await respuesta.Content.ReadAsStringAsync());
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task BaseNoDisponible_Responde503YMensajeComprensible()
    {
        using var servidor = CrearServidor(new ConsultaFija(false));
        using var cliente = CrearCliente(servidor);
        var respuesta = await cliente.GetAsync("/api/disponibilidad");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("Servicio temporalmente no disponible", cuerpo);
        Assert.DoesNotContain("ConnectionString", cuerpo);
    }

    [Fact]
    public async Task FalloInesperado_NoExponeLaExcepcion()
    {
        using var servidor = CrearServidor(new ConsultaQueFalla());
        using var cliente = CrearCliente(servidor);
        var respuesta = await cliente.GetAsync("/api/disponibilidad");
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("detalle-interno-de-prueba", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.Contains("referencia", cuerpo);
    }

    private static WebApplicationFactory<Program> CrearServidor(IConsultaDisponibilidad consulta) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConsultaDisponibilidad>();
                services.AddSingleton(consulta);
            });
        });

    private static HttpClient CrearCliente(WebApplicationFactory<Program> servidor) =>
        servidor.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private sealed class ConsultaFija(bool disponible) : IConsultaDisponibilidad
    {
        public Task<bool> ComprobarAsync(CancellationToken cancelacion) => Task.FromResult(disponible);
    }

    private sealed class ConsultaQueFalla : IConsultaDisponibilidad
    {
        public Task<bool> ComprobarAsync(CancellationToken cancelacion) =>
            throw new InvalidOperationException("detalle-interno-de-prueba");
    }
}
