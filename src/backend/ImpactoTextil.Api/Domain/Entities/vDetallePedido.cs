using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class vDetallePedido
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

    public decimal? precioUnitarioAcordado { get; set; }

    public decimal? subtotal { get; set; }

    public bool? disenoAprobado { get; set; }

    public DateTimeOffset? fechaAprobacionDiseno { get; set; }

    public int? responsableAprobacionDiseno { get; set; }
}
