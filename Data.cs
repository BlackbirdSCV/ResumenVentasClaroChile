using SQLite;
using System.Text;
namespace VentasClaro;

public class Cliente { [PrimaryKey, AutoIncrement] public int Id { get; set; } public string Rut { get; set; } = ""; public string Nombre { get; set; } = ""; public string Apellido { get; set; } = ""; }
public class PlanMovil { [PrimaryKey, AutoIncrement] public int Id { get; set; } public string Nombre { get; set; } = ""; public int Valor { get; set; } }
public class Venta
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int IdCliente { get; set; }
    public string Pcs { get; set; } = "";
    public int IdPlan { get; set; }
    public DateTime FechaVenta { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente | Activa | Rechazada
}
public class Fila
{
    public int Id { get; set; } public string Cliente { get; set; } = ""; public string Rut { get; set; } = "";
    public string Pcs { get; set; } = ""; public string NombrePlan { get; set; } = ""; public string Estado { get; set; } = "";
    public string FechaVenta { get; set; } = ""; public string? FechaActivacion { get; set; }
    public string PcsFmt => Pcs.Length == 9 ? $"{Pcs[0]} {Pcs[1..5]} {Pcs[5..]}" : Pcs;
}

public static class Db
{
    static Lazy<Task<SQLiteAsyncConnection>> _c = new(Init);
    static Task<SQLiteAsyncConnection> C() => _c.Value;

    const string Base = @"SELECT v.Id, c.Nombre||' '||c.Apellido AS Cliente, c.Rut, v.Pcs, p.Nombre AS NombrePlan, v.Estado,
      strftime('%d/%m/%Y',v.FechaVenta) AS FechaVenta, strftime('%d/%m/%Y',v.FechaActivacion) AS FechaActivacion
      FROM Venta v JOIN Cliente c ON c.Id=v.IdCliente JOIN PlanMovil p ON p.Id=v.IdPlan ";

    static async Task<SQLiteAsyncConnection> Init()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "ventas_claro.db3");
        var c = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache, false);
        await c.CreateTableAsync<Cliente>(); await c.CreateTableAsync<PlanMovil>(); await c.CreateTableAsync<Venta>();
        if (!Preferences.Get("seed_hecho", false))
    {
        if (await c.Table<PlanMovil>().CountAsync() == 0) await Seed(c);
        Preferences.Set("seed_hecho", true);
    }
        return c;
    }

    // Datos importados de tu ventas_claro.sql
    static async Task Seed(SQLiteAsyncConnection c)
{
    await c.ExecuteAsync("INSERT INTO PlanMovil(Id,Nombre,Valor) VALUES (1,'GGTT 250',15990),(2,'Libre 500 GB',9990),(3,'Adicional GGTT',9990)");
}

    public static async Task<int> ActivasMes() =>
        await (await C()).ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Venta WHERE Estado='Activa' AND strftime('%Y-%m',FechaActivacion)=strftime('%Y-%m','now','localtime')");
public static async Task<List<Fila>> Resumen(string mes, string orden = "fecha", int idPlan = 0, string estado = "", DateTime? desde = null, DateTime? hasta = null)
{
    var order = orden switch
    {
        "nombre"    => "c.Nombre COLLATE NOCASE, c.Apellido COLLATE NOCASE, v.Id",
        "fecha_asc" => "v.FechaVenta ASC, v.Id ASC",
        _           => "v.FechaVenta DESC, v.Id DESC"
    };
    var args = new List<object>();
    string periodo;
    if (desde != null && hasta != null)
    {
        var d = desde.Value.ToString("yyyy-MM-dd"); var h = hasta.Value.ToString("yyyy-MM-dd");
        periodo = "(date(v.FechaVenta) BETWEEN ? AND ? OR date(v.FechaActivacion) BETWEEN ? AND ?)";
        args.AddRange(new object[] { d, h, d, h });
    }
    else
    {
        periodo = "(strftime('%Y-%m',v.FechaVenta)=? OR strftime('%Y-%m',v.FechaActivacion)=?)";
        args.AddRange(new object[] { mes, mes });
    }
    args.AddRange(new object[] { idPlan, idPlan, estado, estado });
    return await (await C()).QueryAsync<Fila>(
        Base + $"WHERE {periodo} AND (?=0 OR v.IdPlan=?) AND (?='' OR v.Estado=?) ORDER BY " + order, args.ToArray());
}

public static async Task<int> ActivadasEntre(DateTime d, DateTime h, int idPlan = 0) =>
    await (await C()).ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM Venta WHERE Estado='Activa' AND date(FechaActivacion) BETWEEN ? AND ? AND (?=0 OR IdPlan=?)",
        d.ToString("yyyy-MM-dd"), h.ToString("yyyy-MM-dd"), idPlan, idPlan);
    public static async Task<List<Fila>> Pendientes() =>
        await (await C()).QueryAsync<Fila>(Base + "WHERE v.Estado='Pendiente' ORDER BY v.FechaVenta DESC, v.Id DESC");
    public static async Task<List<PlanMovil>> Planes() => await (await C()).Table<PlanMovil>().ToListAsync();

    public static async Task<int> ActivadasEnMes(string mes, int idPlan = 0) =>
    await (await C()).ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM Venta WHERE Estado='Activa' AND strftime('%Y-%m',FechaActivacion)=? AND (?=0 OR IdPlan=?)",
        mes, idPlan, idPlan);

public static async Task AgregarPlan(string nombre, int valor) =>
    await (await C()).InsertAsync(new PlanMovil { Nombre = nombre, Valor = valor });

public static async Task ActualizarPlan(int id, string nombre, int valor) =>
    await (await C()).ExecuteAsync("UPDATE PlanMovil SET Nombre=?, Valor=? WHERE Id=?", nombre, valor, id);

public static async Task<bool> EliminarPlan(int id)
{
    var c = await C();
    if (await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Venta WHERE IdPlan=?", id) > 0) return false; // tiene ventas asociadas
    await c.DeleteAsync<PlanMovil>(id);
    return true;
}

    public static string NormRut(string r)
    {
        r = r.Replace(".", "").Replace(" ", "").ToUpperInvariant();
        return !r.Contains('-') && r.Length > 1 ? r[..^1] + "-" + r[^1] : r;
    }
    public static async Task<Cliente?> ClientePorRut(string rut)
    {
        rut = NormRut(rut);
        return await (await C()).Table<Cliente>().Where(x => x.Rut == rut).FirstOrDefaultAsync();
    }
    public static async Task GuardarVenta(string rut, string nombre, string apellido, string pcs, int idPlan, DateTime fv, string estado, DateTime? fa)
    {
        var c = await C();
        var cli = await ClientePorRut(rut);
        if (cli == null) { cli = new Cliente { Rut = NormRut(rut), Nombre = nombre, Apellido = apellido }; await c.InsertAsync(cli); }
        await c.InsertAsync(new Venta { IdCliente = cli.Id, Pcs = pcs.Replace(" ", ""), IdPlan = idPlan, FechaVenta = fv, FechaActivacion = fa, Estado = estado });
    }
    public static async Task Activar(int id) =>
        await (await C()).ExecuteAsync("UPDATE Venta SET Estado='Activa', FechaActivacion=? WHERE Id=?", DateTime.Today, id);
    public static async Task Eliminar(int id) => await (await C()).DeleteAsync<Venta>(id);

    public static string Ruta => Path.Combine(FileSystem.AppDataDirectory, "ventas_claro.db3");

public static async Task<byte[]> LeerArchivo()
{
    await C(); // asegura que la base exista
    await using var fs = new FileStream(Ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var ms = new MemoryStream();
    await fs.CopyToAsync(ms);
    return ms.ToArray();
}

public static async Task ReemplazarArchivo(byte[] datos)
{
    if (datos.Length < 16 || Encoding.ASCII.GetString(datos, 0, 15) != "SQLite format 3")
        throw new Exception("El respaldo no es una base de datos válida.");
    await (await C()).CloseAsync();
    foreach (var s in new[] { "-journal", "-wal", "-shm" }) File.Delete(Ruta + s);
    await File.WriteAllBytesAsync(Ruta, datos);
    _c = new(Init);
}

public static async Task<(Venta, Cliente)?> ObtenerVenta(int id)
{
    var c = await C();
    var v = await c.FindAsync<Venta>(id);
    if (v == null) return null;
    var cli = await c.FindAsync<Cliente>(v.IdCliente);
    return cli == null ? null : (v, cli);
}

public static async Task ActualizarVenta(int id, string rut, string nombre, string apellido, string pcs, int idPlan, DateTime fv, string estado, DateTime? fa)
{
    var c = await C();
    var v = await c.FindAsync<Venta>(id);
    if (v == null) return;
    rut = NormRut(rut);

    var actual = await c.FindAsync<Cliente>(v.IdCliente);
    var existente = await ClientePorRut(rut);
    Cliente cli;
    if (existente != null) cli = existente;                                   // el RUT ya es de otro cliente: se reutiliza
    else if (actual != null && await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Venta WHERE IdCliente=? AND Id<>?", actual.Id, id) == 0)
        cli = actual;                                                         // el cliente solo tiene esta venta: se corrige su RUT
    else cli = new Cliente();                                                 // el cliente tiene otras ventas: se crea uno nuevo
    cli.Rut = rut; cli.Nombre = nombre; cli.Apellido = apellido;
    if (cli.Id == 0) await c.InsertAsync(cli); else await c.UpdateAsync(cli);

    v.IdCliente = cli.Id; v.Pcs = pcs.Replace(" ", ""); v.IdPlan = idPlan;
    v.FechaVenta = fv; v.Estado = estado; v.FechaActivacion = estado == "Activa" ? fa : null;
    await c.UpdateAsync(v);
    await c.ExecuteAsync("DELETE FROM Cliente WHERE Id NOT IN (SELECT IdCliente FROM Venta)"); // limpia clientes sin ventas
}

}