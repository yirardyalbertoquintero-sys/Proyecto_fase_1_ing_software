using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class vPedido
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

    public decimal? totalReferencial { get; set; }

    public decimal? total { get; set; }

    public decimal? porcentajeAnticipo { get; set; }

    public decimal? anticipoExigible { get; set; }

    public decimal? saldoPendiente { get; set; }

    public bool? cotizacionAceptada { get; set; }

    public bool? excepcionAnticipo { get; set; }
}
