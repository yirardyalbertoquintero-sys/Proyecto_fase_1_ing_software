using System;
using System.Collections.Generic;

namespace ImpactoTextil.Api.Domain.Entities;

public partial class Producto
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public string color { get; set; } = null!;

    public string talla { get; set; } = null!;

    public string tela { get; set; } = null!;

    public string imagenURL { get; set; } = null!;

    public decimal precioBase { get; set; }

    public bool activo { get; set; }

    public virtual ICollection<DetallePedido> DetallePedido { get; set; } = new List<DetallePedido>();
}
