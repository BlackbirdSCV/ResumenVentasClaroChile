namespace VentasClaro;

public class RespaldoPage : ContentPage
{
    readonly Label estado = new() { TextColor = Ui.Pizarra };
    readonly Button conectar = Ui.Boton("CONECTAR CON GOOGLE", Ui.Rojo), subir = Ui.Boton("SUBIR RESPALDO A DRIVE", Ui.Rojo),
        bajar = Ui.Boton("RESTAURAR DESDE DRIVE", Ui.Pizarra), salir = Ui.Boton("Desconectar cuenta", Colors.Gray);

    public RespaldoPage()
    {
        Title = "Respaldo"; BackgroundColor = Ui.Fondo;
        conectar.Clicked += (_, _) => Ui.Seguro(() => Ejecutar(DriveSync.Conectar));
        subir.Clicked += (_, _) => Ui.Seguro(() => Ejecutar(Subir));
        bajar.Clicked += (_, _) => Ui.Seguro(() => Ejecutar(Restaurar));
        salir.Clicked += (_, _) => Ui.Seguro(async () => { DriveSync.CerrarSesion(); await Refrescar(); });

        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = {
                Ui.Titulo("RESPALDO EN GOOGLE DRIVE"), estado, conectar, subir, bajar, salir,
                new Label { Text = "El respaldo es una copia completa de tus datos y queda en una carpeta privada de la app en tu Drive. Úsalo desde un solo teléfono a la vez.", FontSize = 12, TextColor = Colors.Gray } } }) } } });
    }

    protected override void OnAppearing() { base.OnAppearing(); Ui.Seguro(Refrescar); }

    async Task Subir()
    {
        var mod = await DriveSync.UltimoRespaldo();
        if (mod is DateTime m && m.ToUniversalTime().Ticks > Preferences.Get("ultima_sync", 0L)
            && !await DisplayAlertAsync("Hay un respaldo más reciente", $"Drive tiene un respaldo del {m:dd/MM/yyyy HH:mm}, posterior a tu última sincronización (¿otro teléfono?). Subir ahora lo sobrescribirá. ¿Continuar?", "Sobrescribir", "Cancelar")) return;
        await DriveSync.Subir(); await Marcar();
        await DisplayAlertAsync("Listo", "Respaldo subido a tu Drive.", "OK");
    }

    async Task Restaurar()
    {
        var mod = await DriveSync.UltimoRespaldo();
        if (mod is not DateTime m) { await DisplayAlertAsync("Sin respaldo", "No hay ningún respaldo en tu Drive.", "OK"); return; }
        if (!await DisplayAlertAsync("Restaurar", $"Esto reemplazará TODOS los datos de este teléfono por el respaldo del {m:dd/MM/yyyy HH:mm}. ¿Continuar?", "Restaurar", "Cancelar")) return;
        await DriveSync.Restaurar(); await Marcar();
        Ui.AvisarCambio();
        await DisplayAlertAsync("Listo", "Datos restaurados desde tu Drive.", "OK");
    }

    static async Task Marcar()
    {
        if (await DriveSync.UltimoRespaldo() is DateTime m) Preferences.Set("ultima_sync", m.ToUniversalTime().Ticks);
    }

    async Task Refrescar()
    {
        var sesion = await DriveSync.HaySesion();
        conectar.IsVisible = !sesion; subir.IsVisible = bajar.IsVisible = salir.IsVisible = sesion;
        estado.Text = "Sin conectar.";
        if (!sesion) return;
        try { estado.Text = await DriveSync.UltimoRespaldo() is DateTime m ? $"Último respaldo: {m:dd/MM/yyyy HH:mm}" : "Cuenta conectada. Aún no hay respaldo."; }
        catch { estado.Text = "Cuenta conectada (sin conexión)."; }
    }

    // Muestra "Trabajando…", captura errores de red/Google y refresca el estado al terminar
    async Task Ejecutar(Func<Task> accion)
    {
        estado.Text = "Trabajando…";
        try { await accion(); }
        catch (TaskCanceledException) { }
        catch (Exception ex) { await DisplayAlertAsync("Error", ex.Message, "OK"); }
        await Refrescar();
    }
}
