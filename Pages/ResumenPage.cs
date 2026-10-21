namespace VentasClaro;

public class ResumenPage : ContentPage
{
    DateTime mes = Ui.InicioMes();
    readonly Label titulo = Ui.TituloMes();
    readonly Label total = new() { HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };
    readonly Picker orden = new() { Title = "Ordenar por", TextColor = Ui.Pizarra, ItemsSource = new[] { "Fecha (más reciente)", "Fecha (más antigua)", "Nombre A-Z" }, SelectedIndex = 0 };
    readonly Picker filtroPlan = new() { Title = "Plan", TextColor = Ui.Pizarra };
    readonly Picker filtroEstado = new() { Title = "Estado", TextColor = Ui.Pizarra, ItemsSource = new[] { "Todos los estados", "Pendiente", "Activa", "Rechazada" }, SelectedIndex = 0 };
    readonly DatePicker desde = new() { Date = Ui.InicioMes() }, hasta = new() { Date = DateTime.Today };
    readonly Button modoFecha = new() { Text = "Filtrar por rango de fechas", BackgroundColor = Colors.Transparent, TextColor = Ui.Rojo };
    readonly Grid nav, rango;
    List<PlanMovil> planes = new();
    bool cargandoPlanes, usarRango;

    public ResumenPage()
    {
        Title = "Resumen mensual"; BackgroundColor = Ui.Fondo;

        orden.SelectedIndexChanged += (_, _) => { if (!cargandoPlanes) Ui.Seguro(Cargar); };
        filtroPlan.SelectedIndexChanged += (_, _) => { if (!cargandoPlanes) Ui.Seguro(Cargar); };
        filtroEstado.SelectedIndexChanged += (_, _) => { if (!cargandoPlanes) Ui.Seguro(Cargar); };
        desde.DateSelected += (_, _) => { if (usarRango) Ui.Seguro(Cargar); };
        hasta.DateSelected += (_, _) => { if (usarRango) Ui.Seguro(Cargar); };

        nav = Ui.NavMes(titulo, async delta => { mes = mes.AddMonths(delta); await Cargar(); });

        rango = new Grid { IsVisible = false, ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
        rango.Add(new VerticalStackLayout { Children = { new Label { Text = "Desde", TextColor = Ui.Pizarra }, desde } }, 0, 0);
        rango.Add(new VerticalStackLayout { Children = { new Label { Text = "Hasta", TextColor = Ui.Pizarra }, hasta } }, 1, 0);

        modoFecha.Clicked += (_, _) => Ui.Seguro(async () =>
        {
            usarRango = !usarRango;
            nav.IsVisible = !usarRango; rango.IsVisible = usarRango;
            modoFecha.Text = usarRango ? "Volver a la vista por mes" : "Filtrar por rango de fechas";
            await Cargar();
        });

        var filtros = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
        filtros.Add(orden, 0, 0); filtros.Add(filtroPlan, 1, 0);
        filtros.Add(filtroEstado, 0, 1); Grid.SetColumnSpan(filtroEstado, 2);

        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = { nav, rango, modoFecha, filtros, total, lista } } });
        Ui.DatosCambiaron += () => Ui.Seguro(Cargar);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Ui.Seguro(async () =>
        {
            cargandoPlanes = true;
            var i = Math.Max(filtroPlan.SelectedIndex, 0);
            planes = await Db.Planes();
            filtroPlan.ItemsSource = new[] { "Todos los planes" }.Concat(planes.Select(p => p.Nombre)).ToList();
            filtroPlan.SelectedIndex = i < planes.Count + 1 ? i : 0;
            cargandoPlanes = false;
            await Cargar();
        });
    }

    async Task Cargar()
    {
        var clave = orden.SelectedIndex switch { 1 => "fecha_asc", 2 => "nombre", _ => "fecha" };
        var idPlan = filtroPlan.SelectedIndex > 0 ? planes[filtroPlan.SelectedIndex - 1].Id : 0;
        var estado = filtroEstado.SelectedIndex > 0 ? (string)filtroEstado.SelectedItem : "";
        List<Fila> filas; int activadas; string periodo;

        if (usarRango)
        {
            var d = Ui.D(desde.Date); var h = Ui.D(hasta.Date);
            if (d > h) (d, h) = (h, d);
            filas = await Db.Resumen("", clave, idPlan, estado, d, h);
            activadas = await Db.ActivadasEntre(d, h, idPlan);
            periodo = "en el rango";
        }
        else
        {
            titulo.Text = Ui.NombreMes(mes);
            var claveMes = Ui.Clave(mes);
            filas = await Db.Resumen(claveMes, clave, idPlan, estado);
            activadas = await Db.ActivadasEnMes(claveMes, idPlan);
            periodo = "en el mes";
        }
        total.Text = $"{filas.Count} ventas listadas  ·  {activadas} activadas {periodo}";

        lista.Children.Clear();
        foreach (var f in filas)
        {
            var id = f.Id; var rut = f.RutPlano;
            var edit = Ui.Boton("Editar", Ui.Pizarra);
            var pago = Ui.Boton("Revisar pago", Ui.Rojo);
            edit.Clicked += async (_, _) => await Navigation.PushAsync(new EditarVentaPage(id));
            pago.Clicked += async (_, _) =>
            {
                await Clipboard.Default.SetTextAsync(rut);
                await Shell.Current.Navigation.PushModalAsync(new PagoExpressPage(rut));
            };
            lista.Children.Add(Ui.FilaCard(f, Ui.Botones(edit, pago)));
        }
    }
}
