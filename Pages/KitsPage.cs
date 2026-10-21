namespace VentasClaro;

public class KitsPage : ContentPage
{
    DateTime mes = Ui.InicioMes();
    readonly Label titulo = Ui.TituloMes();
    readonly Label total = new() { HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };
    readonly Entry manual = new() { Placeholder = "O escribe el número de la SIM", Keyboard = Keyboard.Numeric, MaxLength = 22 };

    public KitsPage()
    {
        Title = "Kits"; BackgroundColor = Ui.Fondo;
        var escanear = Ui.Boton("ESCANEAR SIM", Ui.Rojo, negrita: true);
        var guardar = Ui.Boton("Guardar número escrito", Ui.Pizarra);
        escanear.Clicked += (_, _) => Ui.Seguro(Escanear);
        guardar.Clicked += (_, _) => Ui.Seguro(() => Registrar(manual.Text));

        var nav = Ui.NavMes(titulo, async delta => { mes = mes.AddMonths(delta); await Cargar(); });

        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { Ui.Titulo("REGISTRAR KIT"), escanear, manual, guardar } }),
            nav, total, lista } } });
    }

    protected override void OnAppearing() { base.OnAppearing(); Ui.Seguro(Cargar); }

    async Task Escanear()
    {
        var st = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (st != PermissionStatus.Granted) st = await Permissions.RequestAsync<Permissions.Camera>();
        if (st != PermissionStatus.Granted)
        { await DisplayAlertAsync("Cámara", "Sin permiso de cámara no se puede escanear. Puedes escribir el número a mano.", "OK"); return; }

        var tcs = new TaskCompletionSource<string?>();
        await Shell.Current.Navigation.PushModalAsync(new EscanearSimPage(tcs));
        var valor = await tcs.Task;
        if (valor == null) return;
        await Task.Delay(400); // deja que termine de cerrarse la cámara antes de mostrar el aviso
        await Registrar(valor);
    }

    async Task Registrar(string? texto)
    {
        var sim = new string((texto ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (sim.Length < 8) { await DisplayAlertAsync("Número inválido", "El número de la SIM parece incompleto.", "OK"); return; }
        var existente = await Db.KitPorSim(sim);
        if (existente != null) { await DisplayAlertAsync("Ya registrada", $"Esta SIM ya se registró el {existente.FechaHora:dd/MM/yyyy HH:mm}.", "OK"); return; }
        if (!await DisplayAlertAsync("Registrar kit", $"SIM: {sim}\n¿Guardar con la fecha y hora de ahora?", "Guardar", "Cancelar")) return;
        await Db.AgregarKit(sim);
        manual.Text = "";
        mes = Ui.InicioMes();
        await Cargar();
    }

    async Task Cargar()
    {
        titulo.Text = Ui.NombreMes(mes);
        var kits = await Db.Kits(Ui.Clave(mes));
        total.Text = $"{kits.Count} kits en el mes";
        lista.Children.Clear();
        foreach (var k in kits)
        {
            var id = k.Id; var sim = k.Sim;
            var del = Ui.Boton("Eliminar", Colors.Gray);
            del.HorizontalOptions = LayoutOptions.Start;
            del.Clicked += (_, _) => Ui.Seguro(async () =>
            {
                if (await DisplayAlertAsync("Eliminar kit", $"¿Eliminar la SIM {sim}?", "Sí", "No")) { await Db.EliminarKit(id); await Cargar(); }
            });
            lista.Children.Add(Ui.Card(new VerticalStackLayout { Spacing = 4, Children = {
                new Label { Text = k.Sim, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pizarra },
                new Label { Text = k.FechaHora.ToString("dd/MM/yyyy HH:mm"), TextColor = Ui.Rojo }, del } }));
        }
    }
}

public class EscanearSimPage : ContentPage
{
    readonly TaskCompletionSource<string?> tcs;
    readonly BarcodeScanning.CameraView cam = new() { VibrationOnDetected = true };
    bool cerrando;

    public EscanearSimPage(TaskCompletionSource<string?> tcs)
    {
        this.tcs = tcs;
        Title = "Escanear SIM"; BackgroundColor = Ui.Rojo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All);
        cam.OnDetectionFinished += async (_, e) =>
        {
            var valor = e.BarcodeResults.FirstOrDefault()?.DisplayValue;
            if (cerrando || string.IsNullOrWhiteSpace(valor)) return;
            cerrando = true;
            tcs.TrySetResult(valor); // primero el resultado, después se cierra la pantalla
            await MainThread.InvokeOnMainThreadAsync(async () => { cam.CameraEnabled = false; await Navigation.PopModalAsync(); });
        };
        var ayuda = new Label { Text = "Apunta al código de barras de la SIM", TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, Margin = 12 };
        var cuerpo = new Grid { RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) } };
        cuerpo.Add(cam, 0, 0); cuerpo.Add(ayuda, 0, 1);
        Content = Ui.ConEncabezado("Escanear SIM", cuerpo, async () => await Navigation.PopModalAsync());
    }

    protected override void OnAppearing() { base.OnAppearing(); cam.CameraEnabled = true; }
    protected override void OnDisappearing() { base.OnDisappearing(); cam.CameraEnabled = false; tcs.TrySetResult(null); }
}
