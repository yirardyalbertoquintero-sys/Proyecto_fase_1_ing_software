using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class Pedido
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int cliente { get; set; }

    public DateTimeOffset fechaOrden { get; set; }

    public DateOnly? fechaEntregaAcordada { get; set; }

    public DateTimeOffset? fechaEntregaReal { get; set; }

    public string estado { get; set; } = null!;

    public string modalidadEntrega { get; set; } = null!;

    public string? direccionEntrega { get; set; }

    public int? versionCotizacionVigente { get; set; }

    public string condicionAdministrativa { get; set; } = null!;

    public string? motivoArchivo { get; set; }

    public DateTimeOffset? fechaArchivo { get; set; }

    public int? responsableArchivo { get; set; }

    public Guid confirmacion { get; set; }

    public byte[] huellaSolicitud { get; set; } = null!;

    public byte[] controlConcurrencia { get; set; } = null!;

    public virtual ICollection<ConstanciaEntrega> ConstanciaEntrega { get; set; } = new List<ConstanciaEntrega>();

    public virtual ICollection<DetallePedido> DetallePedido { get; set; } = new List<DetallePedido>();

    public virtual ICollection<Pago> Pago { get; set; } = new List<Pago>();

    public virtual ICollection<VersionCotizacion> VersionCotizacion { get; set; } = new List<VersionCotizacion>();

    public virtual Usuario clienteNavigation { get; set; } = null!;

    public virtual Usuario? responsableArchivoNavigation { get; set; }

    public virtual VersionCotizacion? versionCotizacionVigenteNavigation { get; set; }
}
