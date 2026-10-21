using SQLite;
namespace VentasClaro;

public class Cliente
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Rut { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
}

public class PlanMovil
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Valor { get; set; }
}

public class Venta
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int IdCliente { get; set; }
    public string Pcs { get; set; } = "";
    public int IdPlan { get; set; }
    public DateTime FechaVenta { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente | Activa | Rechazada
    public bool VoucherCompetencia { get; set; }
    public string? Comentario { get; set; }
    public int ValorPlan { get; set; }                // valor del plan al momento de la venta
}

public class Kit
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Sim { get; set; } = "";
    public DateTime FechaHora { get; set; }
}

public class Meta
{
    [PrimaryKey] public string Mes { get; set; } = ""; // "yyyy-MM"
    public int Planes { get; set; }
    public int Ingreso { get; set; }
}

/// <summary>Fila lista para mostrar en pantalla (resultado de consultas).</summary>
public class Fila
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";
    public string Rut { get; set; } = "";
    public string Pcs { get; set; } = "";
    public string NombrePlan { get; set; } = "";
    public string Estado { get; set; } = "";
    public string FechaVenta { get; set; } = "";
    public string? FechaActivacion { get; set; }
    public bool VoucherCompetencia { get; set; }
    public string? Comentario { get; set; }
    public string PcsFmt => Pcs.Length == 9 ? $"{Pcs[0]} {Pcs[1..5]} {Pcs[5..]}" : Pcs;
    public string RutPlano => Rut.Replace("-", "").Replace(".", "");
}

public class TotRow
{
    public string Estado { get; set; } = "";
    public int N { get; set; }
    public long Bruto { get; set; }
}
