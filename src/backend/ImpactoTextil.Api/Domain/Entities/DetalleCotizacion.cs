using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class DetalleCotizacion
{
    public int versionCotizacion { get; set; }

    public int detalle { get; set; }

    public decimal precioUnitarioAcordado { get; set; }

    public int? versionDiseno { get; set; }

    public virtual DetallePedido detalleNavigation { get; set; } = null!;

    public virtual VersionCotizacion versionCotizacionNavigation { get; set; } = null!;

    public virtual VersionDiseno? versionDisenoNavigation { get; set; }
}
