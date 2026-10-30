using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class vDetalleCotizacion
{
    public int versionCotizacion { get; set; }

    public int detalle { get; set; }

    public decimal precioUnitarioAcordado { get; set; }

    public int? versionDiseno { get; set; }

    public int cantidad { get; set; }

    public decimal? subtotal { get; set; }
}
