namespace VentasClaro;

public class EditarVentaPage : ContentPage
{
    static readonly string[] Estados = { "Pendiente", "Activa", "Rechazada" };
    readonly int id;
    readonly Entry rut = new() { Placeholder = "RUT*" }, nombre = new() { Placeholder = "Nombre*" }, apellido = new() { Placeholder = "Apellido*" },
        pcs = new() { Placeholder = "Teléfono móvil* (9 dígitos)", Keyboard = Keyboard.Numeric, MaxLength = 9 };
    readonly Picker plan = new() { Title = "Plan*", TextColor = Ui.Pizarra }, estado = new() { Title = "Estado*", ItemsSource = Estados, TextColor = Ui.Pizarra };
    readonly DatePicker fVenta = new(), fAct = new();
    readonly Label actLbl = new() { Text = "Fecha de activación", TextColor = Ui.Pizarra };
    readonly Switch voucher = Ui.Interruptor();
    readonly Editor comentario = new() { Placeholder = "Comentario sobre la venta (opcional)", HeightRequest = 90, MaxLength = 500 };
    List<PlanMovil> planes = new();
    bool cargado;

    public EditarVentaPage(int id)
    {
        this.id = id; Title = "Editar venta"; BackgroundColor = Ui.Fondo;
        estado.SelectedIndexChanged += (_, _) => actLbl.IsVisible = fAct.IsVisible = estado.SelectedItem as string == "Activa";
        var guardar = Ui.Boton("GUARDAR CAMBIOS", Ui.Rojo, negrita: true);
        var cancelar = Ui.Boton("Cancelar", Colors.Gray);
        guardar.Clicked += (_, _) => Ui.Seguro(Guardar);
        cancelar.Clicked += async (_, _) => await Navigation.PopAsync();

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                Ui.Titulo("CLIENTE"), rut, nombre, apellido,
                new Label { Text = "Si el cliente tiene varias ventas, un cambio de nombre se aplica a todas.", FontSize = 12, TextColor = Colors.Gray },
                Ui.Titulo("VENTA"), pcs, plan, estado,
                new Label { Text = "Fecha de venta", TextColor = Ui.Pizarra }, fVenta, actLbl, fAct,
                Ui.FilaSwitch("Voucher de pago de la competencia ingresado", voucher), comentario, guardar, cancelar } }) } } };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (cargado) return;
        cargado = true;
        Ui.Seguro(Cargar);
    }

    async Task Cargar()
    {
        var r = await Db.ObtenerVenta(id);
        if (r == null) { await Navigation.PopAsync(); return; }
        var (v, c) = r.Value;
        planes = await Db.Planes();
        plan.ItemsSource = planes.Select(p => p.Nombre).ToList();
        plan.SelectedIndex = planes.FindIndex(p => p.Id == v.IdPlan);
        rut.Text = c.Rut; nombre.Text = c.Nombre; apellido.Text = c.Apellido; pcs.Text = v.Pcs;
        fVenta.Date = v.FechaVenta;
        fAct.Date = v.FechaActivacion ?? DateTime.Today;
        voucher.IsToggled = v.VoucherCompetencia;
        comentario.Text = v.Comentario ?? "";
        estado.SelectedIndex = Math.Max(Array.IndexOf(Estados, v.Estado), 0);
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(rut.Text) || string.IsNullOrWhiteSpace(nombre.Text) || string.IsNullOrWhiteSpace(apellido.Text)
            || pcs.Text?.Length != 9 || plan.SelectedIndex < 0)
        { await DisplayAlertAsync("Faltan datos", "Completa los campos obligatorios (teléfono de 9 dígitos).", "OK"); return; }

        var est = (string)estado.SelectedItem;
        var fv = Ui.D(fVenta.Date);
        DateTime? act = est == "Activa" ? Ui.D(fAct.Date) : null;
        if (act < fv && !await DisplayAlertAsync("Fechas", "La activación es anterior a la venta. ¿Guardar igual?", "Guardar", "Revisar")) return;

        var coment = string.IsNullOrWhiteSpace(comentario.Text) ? null : comentario.Text.Trim();
        await Db.ActualizarVenta(id, rut.Text, nombre.Text.Trim(), apellido.Text.Trim(), pcs.Text, planes[plan.SelectedIndex].Id, fv, est, act, voucher.IsToggled, coment);
        Ui.AvisarCambio();
        await Navigation.PopAsync();
    }
}
