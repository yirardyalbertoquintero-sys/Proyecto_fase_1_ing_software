using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class vVersionCotizacion
{
    public int id { get; set; }

    public int numeroVersion { get; set; }

    public int pedido { get; set; }

    public DateTimeOffset fechaRegistro { get; set; }

    public int responsableRegistro { get; set; }

    public string decision { get; set; } = null!;

    public DateTimeOffset? fechaDecision { get; set; }

    public int? responsableDecision { get; set; }

    public string? referenciaDecision { get; set; }

    public decimal? total { get; set; }
}
