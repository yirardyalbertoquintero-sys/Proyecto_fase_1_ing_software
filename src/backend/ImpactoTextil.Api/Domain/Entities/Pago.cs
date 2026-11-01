using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class Pago
{
    public int id { get; set; }

    public int pedido { get; set; }

    public decimal monto { get; set; }

    public DateTimeOffset fecha { get; set; }

    public string medio { get; set; } = null!;

    public string? referencia { get; set; }

    public string estadoVerificacion { get; set; } = null!;

    public string? comprobanteURL { get; set; }

    public DateTimeOffset? fechaVerificacion { get; set; }

    public int? responsableVerificacion { get; set; }

    public Guid confirmacion { get; set; }

    public byte[]? huellaComprobante { get; set; }

    public byte[] controlConcurrencia { get; set; } = null!;

    public virtual Pedido pedidoNavigation { get; set; } = null!;

    public virtual Usuario? responsableVerificacionNavigation { get; set; }
}
