using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class VersionCotizacion
{
    public int id { get; set; }

    public int numeroVersion { get; set; }

    public int pedido { get; set; }

    public DateTimeOffset fechaRegistro { get; set; }

    public int responsableRegistro { get; set; }

    public string decision { get; set; } = null!;

    public DateTimeOffset? fechaDecision { get; set; }

    public int? responsableDecision { get; set; }

    public string? referenciaDecision { get; set; }

    public virtual ICollection<ComprobacionSublimacion> ComprobacionSublimacion { get; set; } = new List<ComprobacionSublimacion>();

    public virtual ICollection<DetalleCotizacion> DetalleCotizacion { get; set; } = new List<DetalleCotizacion>();

    public virtual ICollection<Pedido> Pedido { get; set; } = new List<Pedido>();

    public virtual Pedido pedidoNavigation { get; set; } = null!;

    public virtual Usuario? responsableDecisionNavigation { get; set; }

    public virtual Usuario responsableRegistroNavigation { get; set; } = null!;
}
