using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class ComprobacionSublimacion
{
    public int id { get; set; }

    public int versionCotizacion { get; set; }

    public DateTimeOffset fecha { get; set; }

    public int responsable { get; set; }

    public decimal montoVerificado { get; set; }

    public virtual Usuario responsableNavigation { get; set; } = null!;

    public virtual VersionCotizacion versionCotizacionNavigation { get; set; } = null!;

    public virtual ICollection<VersionDiseno> versionDiseno { get; set; } = new List<VersionDiseno>();
}
