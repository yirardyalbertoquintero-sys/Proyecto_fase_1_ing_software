using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class ConstanciaEntrega
{
    public int id { get; set; }

    public int pedido { get; set; }

    public bool decision { get; set; }

    public DateTimeOffset fecha { get; set; }

    public int responsable { get; set; }

    public string referencia { get; set; } = null!;

    public virtual Pedido pedidoNavigation { get; set; } = null!;

    public virtual Usuario responsableNavigation { get; set; } = null!;
}
