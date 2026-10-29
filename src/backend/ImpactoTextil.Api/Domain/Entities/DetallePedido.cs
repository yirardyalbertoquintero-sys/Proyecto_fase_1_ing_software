using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class DetallePedido
{
    public int id { get; set; }

    public int pedido { get; set; }

    public int producto { get; set; }

    public int cantidad { get; set; }

    public string talla { get; set; } = null!;

    public string color { get; set; } = null!;

    public string tela { get; set; } = null!;

    public string empaque { get; set; } = null!;

    public string? observaciones { get; set; }

    public string? ideaDiseno { get; set; }

    public string? archivoDisenoURL { get; set; }

    public decimal precioBaseReferencia { get; set; }

    public int? versionDisenoVigente { get; set; }

    public virtual ICollection<DetalleCotizacion> DetalleCotizacion { get; set; } = new List<DetalleCotizacion>();

    public virtual ICollection<VersionDiseno> VersionDiseno { get; set; } = new List<VersionDiseno>();

    public virtual Pedido pedidoNavigation { get; set; } = null!;

    public virtual Producto productoNavigation { get; set; } = null!;

    public virtual VersionDiseno? versionDisenoVigenteNavigation { get; set; }
}
