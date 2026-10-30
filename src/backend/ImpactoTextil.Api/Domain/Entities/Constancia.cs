using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class Constancia
{
    public string? tipo { get; set; }

    public int registroOrigen { get; set; }

    public int pedido { get; set; }

    public bool? decision { get; set; }

    public DateTimeOffset? fecha { get; set; }

    public int? responsable { get; set; }

    public string? referencia { get; set; }

    public int? versionCotizacion { get; set; }
}
