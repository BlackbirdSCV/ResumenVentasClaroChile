namespace VentasClaro;

public class NuevaVentaPage : ContentPage
{
    readonly Entry rut = new() { Placeholder = "RUT*  (12345678-9)" }, nombre = new() { Placeholder = "Nombre*" },
        apellido = new() { Placeholder = "Apellido*" }, pcs = new() { Placeholder = "Teléfono móvil* (9 dígitos)", Keyboard = Keyboard.Numeric, MaxLength = 9 };
    readonly Picker plan = new() { Title = "Plan*" }, estado = new() { Title = "Estado*", ItemsSource = new[] { "Pendiente", "Activa", "Rechazada" }, SelectedIndex = 0 };
    readonly DatePicker fVenta = new() { Date = DateTime.Today }, fAct = new() { Date = DateTime.Today, IsVisible = false };
    readonly Label actLbl = new() { Text = "Fecha de activación", IsVisible = false };
    readonly Switch voucher = Ui.Interruptor();
    readonly Editor comentario = new() { Placeholder = "Comentario sobre la venta (opcional)", HeightRequest = 90, MaxLength = 500 };
    List<PlanMovil> planes = new();

    public NuevaVentaPage()
    {
        Title = "Nueva venta"; BackgroundColor = Ui.Rojo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All);

        estado.SelectedIndexChanged += (_, _) => actLbl.IsVisible = fAct.IsVisible = estado.SelectedItem as string == "Activa";
        rut.Unfocused += (_, _) => Ui.Seguro(async () =>
        {
            if (string.IsNullOrWhiteSpace(rut.Text)) return;
            var c = await Db.ClientePorRut(rut.Text);
            if (c != null) { nombre.Text = c.Nombre; apellido.Text = c.Apellido; }
        });
        var guardar = Ui.Boton("GUARDAR", Ui.Rojo, negrita: true);
        guardar.Clicked += (_, _) => Ui.Seguro(Guardar);

        var cuerpo = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                Ui.Titulo("CLIENTE"), rut, nombre, apellido, Ui.Titulo("VENTA"), pcs, plan, estado,
                new Label { Text = "Fecha de venta" }, fVenta, actLbl, fAct,
                Ui.FilaSwitch("Voucher de pago de la competencia ingresado", voucher), comentario, guardar } }) } } };
        Content = Ui.ConEncabezado("Nueva venta", cuerpo, async () => await Navigation.PopModalAsync());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Ui.Seguro(async () =>
        {
            var i = plan.SelectedIndex;
            planes = await Db.Planes();
            plan.ItemsSource = planes.Select(p => $"{p.Nombre}  (${p.Valor:N0})").ToList();
            plan.SelectedIndex = i < planes.Count ? i : -1;
        });
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(rut.Text) || string.IsNullOrWhiteSpace(nombre.Text) || string.IsNullOrWhiteSpace(apellido.Text)
            || pcs.Text?.Length != 9 || plan.SelectedIndex < 0)
        { await DisplayAlertAsync("Faltan datos", "Completa los campos obligatorios (teléfono de 9 dígitos).", "OK"); return; }

        var est = (string)estado.SelectedItem;
        DateTime? act = est == "Activa" ? Ui.D(fAct.Date) : null;
        var coment = string.IsNullOrWhiteSpace(comentario.Text) ? null : comentario.Text.Trim();
        await Db.GuardarVenta(rut.Text, nombre.Text.Trim(), apellido.Text.Trim(), pcs.Text, planes[plan.SelectedIndex].Id, Ui.D(fVenta.Date), est, act, voucher.IsToggled, coment);
        Ui.AvisarCambio();
        rut.Text = nombre.Text = apellido.Text = pcs.Text = ""; plan.SelectedIndex = -1; estado.SelectedIndex = 0;
        voucher.IsToggled = false; comentario.Text = "";
        await DisplayAlertAsync("Listo", "Venta guardada.", "OK");
    }
}
