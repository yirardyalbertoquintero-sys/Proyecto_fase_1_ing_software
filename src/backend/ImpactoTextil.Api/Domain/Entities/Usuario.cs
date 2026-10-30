using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class Usuario
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public string correo { get; set; } = null!;

    public string passwordHash { get; set; } = null!;

    public string rol { get; set; } = null!;

    public bool activo { get; set; }

    public bool aceptacionTerminos { get; set; }

    public DateTimeOffset? fechaAceptacion { get; set; }

    public virtual ICollection<ComprobacionSublimacion> ComprobacionSublimacion { get; set; } = new List<ComprobacionSublimacion>();

    public virtual ICollection<ConstanciaEntrega> ConstanciaEntrega { get; set; } = new List<ConstanciaEntrega>();

    public virtual ICollection<Pago> Pago { get; set; } = new List<Pago>();

    public virtual ICollection<Pedido> PedidoclienteNavigation { get; set; } = new List<Pedido>();

    public virtual ICollection<Pedido> PedidoresponsableArchivoNavigation { get; set; } = new List<Pedido>();

    public virtual ICollection<VersionCotizacion> VersionCotizacionresponsableDecisionNavigation { get; set; } = new List<VersionCotizacion>();

    public virtual ICollection<VersionCotizacion> VersionCotizacionresponsableRegistroNavigation { get; set; } = new List<VersionCotizacion>();

    public virtual ICollection<VersionDiseno> VersionDisenoresponsableDecisionNavigation { get; set; } = new List<VersionDiseno>();

    public virtual ICollection<VersionDiseno> VersionDisenoresponsableRegistroNavigation { get; set; } = new List<VersionDiseno>();
}
