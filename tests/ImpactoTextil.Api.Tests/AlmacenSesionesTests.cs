using System.Security.Claims;
using ImpactoTextil.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;

namespace ImpactoTextil.Api.Tests;

public sealed class AlmacenSesionesTests
{
    [Fact]
    public async Task LimpiezaConcurrente_NoBorraUnaSesionQueAcabaDeRenovarse()
    {
        var reloj = new RelojIntercalado();
        var almacen = new AlmacenSesiones(reloj);
        var id = await almacen.StoreAsync(Ticket(reloj.Ahora.AddMinutes(-1)));
        var renovado = Ticket(reloj.Ahora.AddMinutes(10));
        reloj.AlConsultar = () => almacen.RenewAsync(id, renovado).GetAwaiter().GetResult();
        await almacen.StoreAsync(Ticket(reloj.Ahora.AddMinutes(10)));
        Assert.Same(renovado, await almacen.RetrieveAsync(id));
    }

    [Fact]
    public async Task ConsultaConcurrente_ReleeLaRenovacionAntesDeDeclararVencidaLaSesion()
    {
        var reloj = new RelojIntercalado();
        var almacen = new AlmacenSesiones(reloj);
        var id = await almacen.StoreAsync(Ticket(reloj.Ahora.AddMinutes(-1)));
        var renovado = Ticket(reloj.Ahora.AddMinutes(10));
        reloj.AlConsultar = () => almacen.RenewAsync(id, renovado).GetAwaiter().GetResult();
        Assert.Same(renovado, await almacen.RetrieveAsync(id));
    }

    [Fact]
    public async Task RenovacionDespuesDelCierre_NoResucitaLaSesion()
    {
        var reloj = new RelojIntercalado();
        var almacen = new AlmacenSesiones(reloj);
        var ticket = Ticket(reloj.Ahora.AddMinutes(10));
        var id = await almacen.StoreAsync(ticket);
        await almacen.RemoveAsync(id);
        await almacen.RenewAsync(id, ticket);
        Assert.Null(await almacen.RetrieveAsync(id));
    }

    private static AuthenticationTicket Ticket(DateTimeOffset fin) => new(
        new ClaimsPrincipal(new ClaimsIdentity("Cookies")), new AuthenticationProperties { ExpiresUtc = fin }, "Cookies");

    private sealed class RelojIntercalado : TimeProvider
    {
        public DateTimeOffset Ahora { get; } = DateTimeOffset.UtcNow;
        public Action? AlConsultar { get; set; }
        public override DateTimeOffset GetUtcNow()
        {
            var accion = AlConsultar;
            AlConsultar = null;
            accion?.Invoke();
            return Ahora;
        }
    }
}
