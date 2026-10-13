using System.Globalization;
namespace VentasClaro;

public class HomePage : ContentPage
{
    readonly Label activas = new() { FontSize = 64, FontAttributes = FontAttributes.Bold, TextColor = Ui.Rojo, HorizontalOptions = LayoutOptions.Center };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };

    public HomePage()
    {
        Title = "Resumen de Ventas"; BackgroundColor = Ui.Fondo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All); // la página se achica cuando aparece el teclado
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 16, Children = {
            Ui.Card(new VerticalStackLayout { Children = { Ui.Titulo("VENTAS ACTIVAS DEL MES"), activas } }),
            Ui.Titulo("SOLICITUDES PENDIENTES"), lista } } };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Cargar();
    Ui.DatosCambiaron += async () => await Cargar();
    Content = Ui.ConFab(Content);}

    async Task Cargar()
{
    activas.Text = (await Db.ActivasMes()).ToString();
    lista.Children.Clear();
    foreach (var f in await Db.Pendientes())
    {
        var id = f.Id; var pcs = f.Pcs;
        var edit = new Button { Text = "Editar", BackgroundColor = Ui.Pizarra, TextColor = Colors.White };
        var ok = new Button { Text = "Marcar activa", BackgroundColor = Ui.Rojo, TextColor = Colors.White };
        var del = new Button { Text = "Eliminar", BackgroundColor = Colors.Gray, TextColor = Colors.White };
        var revisar = new Button { Text = "Revisar activación", BackgroundColor = Ui.Rojo, TextColor = Colors.White };
        foreach (var b in new[] { ok, edit, del }) { b.FontSize = 13; b.Padding = new Thickness(4, 10); } // para que el texto quepa en un tercio del ancho

        revisar.Clicked += async (_, _) =>
        {
            await Clipboard.Default.SetTextAsync(pcs); // respaldo: pegar si el relleno falla
            await Shell.Current.Navigation.PushModalAsync(new NumerosPortadosPage(pcs));
        };
        ok.Clicked += async (_, _) => { await Db.Activar(id); await Cargar(); };
        edit.Clicked += async (_, _) => await Navigation.PushAsync(new EditarVentaPage(id));
        del.Clicked += async (_, _) => { if (await DisplayAlert("Eliminar", "¿Eliminar esta venta?", "Sí", "No")) { await Db.Eliminar(id); await Cargar(); } };

        var botones = new Grid
        {
            ColumnSpacing = 8, RowSpacing = 8,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }
        };
        botones.Add(edit, 0, 0); botones.Add(ok, 1, 0); botones.Add(del, 2, 0);
        botones.Add(revisar, 0, 1); Grid.SetColumnSpan(revisar, 3);

        lista.Children.Add(Ui.FilaCard(f, botones));
    }
}
}

public class NuevaVentaPage : ContentPage
{
    readonly Entry rut = new() { Placeholder = "RUT*  (12345678-9)" }, nombre = new() { Placeholder = "Nombre*" },
        apellido = new() { Placeholder = "Apellido*" }, pcs = new() { Placeholder = "Teléfono móvil* (9 dígitos)", Keyboard = Keyboard.Numeric, MaxLength = 9 };
    readonly Picker plan = new() { Title = "Plan*" }, estado = new() { Title = "Estado*", ItemsSource = new[] { "Pendiente", "Activa", "Rechazada" }, SelectedIndex = 0 };
    readonly DatePicker fVenta = new() { Date = DateTime.Today }, fAct = new() { Date = DateTime.Today };
    readonly Label actLbl = new() { Text = "Fecha de activación", IsVisible = false };
    List<PlanMovil> planes = new();

    public NuevaVentaPage()
    {
        Title = "Nueva venta"; BackgroundColor = Ui.Fondo; fAct.IsVisible = false;
        estado.SelectedIndexChanged += (_, _) => actLbl.IsVisible = fAct.IsVisible = estado.SelectedItem as string == "Activa";
        rut.Unfocused += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(rut.Text)) return;
            var c = await Db.ClientePorRut(rut.Text);
            if (c != null) { nombre.Text = c.Nombre; apellido.Text = c.Apellido; }
        };
        var guardar = new Button { Text = "GUARDAR", BackgroundColor = Ui.Rojo, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        guardar.Clicked += async (_, _) => await Guardar();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = { Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
            Ui.Titulo("CLIENTE"), rut, nombre, apellido, Ui.Titulo("VENTA"), pcs, plan, estado,
            new Label { Text = "Fecha de venta" }, fVenta, actLbl, fAct, guardar } }) } } };
            BackgroundColor = Ui.Rojo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All);
        Content = Ui.ConEncabezado("Nueva venta", Content, async () => await Navigation.PopModalAsync());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var i = plan.SelectedIndex;
        planes = await Db.Planes();
        plan.ItemsSource = planes.Select(p => $"{p.Nombre}  (${p.Valor:N0})").ToList();
        plan.SelectedIndex = i < planes.Count ? i : -1;
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(rut.Text) || string.IsNullOrWhiteSpace(nombre.Text) || string.IsNullOrWhiteSpace(apellido.Text)
            || pcs.Text?.Length != 9 || plan.SelectedIndex < 0)
        { await DisplayAlert("Faltan datos", "Completa los campos obligatorios (teléfono de 9 dígitos).", "OK"); return; }

        var est = (string)estado.SelectedItem;
        DateTime? act = est == "Activa" ? Ui.D(fAct.Date) : null;
        await Db.GuardarVenta(rut.Text, nombre.Text.Trim(), apellido.Text.Trim(), pcs.Text, planes[plan.SelectedIndex].Id, Ui.D(fVenta.Date), est, act);
        Ui.AvisarCambio();
        rut.Text = nombre.Text = apellido.Text = pcs.Text = ""; plan.SelectedIndex = -1; estado.SelectedIndex = 0;
        await DisplayAlert("Listo", "Venta guardada.", "OK");
    }
}

public class ResumenPage : ContentPage
{
    DateTime mes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    readonly Label titulo = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pizarra, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    readonly Label total = new() { HorizontalOptions = LayoutOptions.Center, TextColor = Colors.Gray };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };
    readonly Picker orden = new() { Title = "Ordenar por", TextColor = Ui.Pizarra, ItemsSource = new[] { "Fecha (más reciente)", "Fecha (más antigua)", "Nombre A-Z" }, SelectedIndex = 0 };
    readonly Picker filtroPlan = new() { Title = "Plan", TextColor = Ui.Pizarra };
    readonly Picker filtroEstado = new() { Title = "Estado", TextColor = Ui.Pizarra, ItemsSource = new[] { "Todos los estados", "Pendiente", "Activa", "Rechazada" }, SelectedIndex = 0 };
    readonly DatePicker desde = new() { Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
    readonly DatePicker hasta = new() { Date = DateTime.Today };
    readonly Button modoFecha = new() { Text = "Filtrar por rango de fechas", BackgroundColor = Colors.Transparent, TextColor = Ui.Rojo };
    readonly Grid nav, rango;
    List<PlanMovil> planes = new();
    bool cargandoPlanes, usarRango;

    public ResumenPage()
    {
        Title = "Resumen mensual"; BackgroundColor = Ui.Fondo;
        var prev = new Button { Text = "‹", BackgroundColor = Ui.Pizarra, TextColor = Colors.White };
        var next = new Button { Text = "›", BackgroundColor = Ui.Pizarra, TextColor = Colors.White };
        prev.Clicked += async (_, _) => { mes = mes.AddMonths(-1); await Cargar(); };
        next.Clicked += async (_, _) => { mes = mes.AddMonths(1); await Cargar(); };
        orden.SelectedIndexChanged += async (_, _) => { if (!cargandoPlanes) await Cargar(); };
        filtroPlan.SelectedIndexChanged += async (_, _) => { if (!cargandoPlanes) await Cargar(); };
        filtroEstado.SelectedIndexChanged += async (_, _) => { if (!cargandoPlanes) await Cargar(); };
        desde.DateSelected += async (_, _) => { if (usarRango) await Cargar(); };
        hasta.DateSelected += async (_, _) => { if (usarRango) await Cargar(); };
        modoFecha.Clicked += async (_, _) =>
        {
            usarRango = !usarRango;
            nav!.IsVisible = !usarRango; rango!.IsVisible = usarRango;
            modoFecha.Text = usarRango ? "Volver a la vista por mes" : "Filtrar por rango de fechas";
            await Cargar();
        };

        nav = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        nav.Add(prev, 0, 0); nav.Add(titulo, 1, 0); nav.Add(next, 2, 0);

        rango = new Grid { IsVisible = false, ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
        rango.Add(new VerticalStackLayout { Children = { new Label { Text = "Desde", TextColor = Ui.Pizarra }, desde } }, 0, 0);
        rango.Add(new VerticalStackLayout { Children = { new Label { Text = "Hasta", TextColor = Ui.Pizarra }, hasta } }, 1, 0);

        var filtros = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
        filtros.Add(orden, 0, 0); filtros.Add(filtroPlan, 1, 0);
        filtros.Add(filtroEstado, 0, 1); Grid.SetColumnSpan(filtroEstado, 2);

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = { nav, rango, modoFecha, filtros, total, lista } } };
        Ui.DatosCambiaron += async () => await Cargar();
        Content = Ui.ConFab(Content);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        cargandoPlanes = true;
        var i = Math.Max(filtroPlan.SelectedIndex, 0);
        planes = await Db.Planes();
        filtroPlan.ItemsSource = new[] { "Todos los planes" }.Concat(planes.Select(p => p.Nombre)).ToList();
        filtroPlan.SelectedIndex = i < planes.Count + 1 ? i : 0;
        cargandoPlanes = false;
        await Cargar();
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
            titulo.Text = mes.ToString("MMMM yyyy", new CultureInfo("es-CL"));
            var clavMes = mes.ToString("yyyy-MM");
            filas = await Db.Resumen(clavMes, clave, idPlan, estado);
            activadas = await Db.ActivadasEnMes(clavMes, idPlan);
            periodo = "en el mes";
        }
        total.Text = $"{filas.Count} ventas listadas  ·  {activadas} activadas {periodo}";
        lista.Children.Clear();
        foreach (var f in filas)
{
    var id = f.Id;
    var rut = f.Rut.Replace("-", "").Replace(".", ""); // mismo formato que al copiar el RUT en la tarjeta
    var edit = new Button { Text = "Editar", BackgroundColor = Ui.Pizarra, TextColor = Colors.White };
    var pago = new Button { Text = "Revisar pago", BackgroundColor = Ui.Rojo, TextColor = Colors.White };
    edit.Clicked += async (_, _) => await Navigation.PushAsync(new EditarVentaPage(id));
    pago.Clicked += async (_, _) =>
    {
        await Clipboard.Default.SetTextAsync(rut);
        await Shell.Current.Navigation.PushModalAsync(new PagoExpressPage(rut));
    };
    var botones = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
    botones.Add(edit, 0, 0); botones.Add(pago, 1, 0);
    lista.Children.Add(Ui.FilaCard(f, botones));
}
    }
}

public class PlanesPage : ContentPage
{
    readonly Entry nombre = new() { Placeholder = "Nombre del plan*" };
    readonly Entry valor = new() { Placeholder = "Valor mensual* (ej: 15990)", Keyboard = Keyboard.Numeric, MaxLength = 7 };
    readonly Label tituloForm = Ui.Titulo("NUEVO PLAN");
    readonly Button guardar = new() { Text = "AGREGAR PLAN", BackgroundColor = Ui.Rojo, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
    readonly Button cancelar = new() { Text = "Cancelar", BackgroundColor = Colors.Gray, TextColor = Colors.White, IsVisible = false };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };
    int? editando;

    public PlanesPage()
    {
        Title = "Planes"; BackgroundColor = Ui.Fondo;
        guardar.Clicked += async (_, _) => await Guardar();
        cancelar.Clicked += (_, _) => Limpiar();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { tituloForm, nombre, valor, guardar, cancelar } }),
            Ui.Titulo("PLANES"), lista } } };
            Content = Ui.ConFab(Content);
    }
    protected override async void OnAppearing() { base.OnAppearing(); await Cargar(); }

    void Limpiar()
    {
        editando = null; nombre.Text = ""; valor.Text = "";
        tituloForm.Text = "NUEVO PLAN"; guardar.Text = "AGREGAR PLAN"; cancelar.IsVisible = false;
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(nombre.Text) || !int.TryParse(valor.Text, out var v) || v <= 0)
        { await DisplayAlert("Faltan datos", "Ingresa un nombre y un valor numérico mayor a 0.", "OK"); return; }
        if (editando is int id) await Db.ActualizarPlan(id, nombre.Text.Trim(), v);
        else await Db.AgregarPlan(nombre.Text.Trim(), v);
        Limpiar();
        await Cargar();
    }

    async Task Cargar()
    {
        lista.Children.Clear();
        foreach (var p in await Db.Planes())
        {
            var plan = p;
            var edit = new Button { Text = "Editar", BackgroundColor = Ui.Pizarra, TextColor = Colors.White };
            var del = new Button { Text = "Eliminar", BackgroundColor = Colors.Gray, TextColor = Colors.White };
            edit.Clicked += (_, _) =>
            {
                editando = plan.Id; nombre.Text = plan.Nombre; valor.Text = plan.Valor.ToString();
                tituloForm.Text = "EDITAR PLAN"; guardar.Text = "GUARDAR CAMBIOS"; cancelar.IsVisible = true;
            };
            del.Clicked += async (_, _) =>
            {
                if (!await DisplayAlert("Eliminar plan", $"¿Eliminar \"{plan.Nombre}\"?", "Sí", "No")) return;
                if (!await Db.EliminarPlan(plan.Id)) await DisplayAlert("No se puede eliminar", "Este plan tiene ventas asociadas.", "OK");
                else if (editando == plan.Id) Limpiar();
                await Cargar();
            };
            var datos = new VerticalStackLayout { Children = {
                new Label { Text = plan.Nombre, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pizarra },
                new Label { Text = $"${plan.Valor:N0} mensual", TextColor = Ui.Rojo } } };
            var fila = new VerticalStackLayout { Spacing = 8, Children = { datos, new HorizontalStackLayout { Spacing = 8, Children = { edit, del } } } };
            lista.Children.Add(Ui.Card(fila));
        }
    }
}

public class RespaldoPage : ContentPage
{
    readonly Label estado = new() { TextColor = Ui.Pizarra };
    readonly Button conectar = Boton("CONECTAR CON GOOGLE", Ui.Rojo), subir = Boton("SUBIR RESPALDO A DRIVE", Ui.Rojo),
        bajar = Boton("RESTAURAR DESDE DRIVE", Ui.Pizarra), salir = Boton("Desconectar cuenta", Colors.Gray);
    static Button Boton(string t, Color c) => new() { Text = t, BackgroundColor = c, TextColor = Colors.White };

    public RespaldoPage()
    {
        Title = "Respaldo"; BackgroundColor = Ui.Fondo;
        conectar.Clicked += async (_, _) => await Ejecutar(DriveSync.Conectar);
        subir.Clicked += async (_, _) => await Ejecutar(async () =>
        {
            var mod = await DriveSync.UltimoRespaldo();
            if (mod is DateTime m && m.ToUniversalTime().Ticks > Preferences.Get("ultima_sync", 0L)
                && !await DisplayAlert("Hay un respaldo más reciente", $"Drive tiene un respaldo del {m:dd/MM/yyyy HH:mm}, posterior a tu última sincronización (¿otro teléfono?). Subir ahora lo sobrescribirá. ¿Continuar?", "Sobrescribir", "Cancelar")) return;
            await DriveSync.Subir(); await Marcar();
            await DisplayAlert("Listo", "Respaldo subido a tu Drive.", "OK");
        });
        bajar.Clicked += async (_, _) => await Ejecutar(async () =>
        {
            var mod = await DriveSync.UltimoRespaldo();
            if (mod is not DateTime m) { await DisplayAlert("Sin respaldo", "No hay ningún respaldo en tu Drive.", "OK"); return; }
            if (!await DisplayAlert("Restaurar", $"Esto reemplazará TODOS los datos de este teléfono por el respaldo del {m:dd/MM/yyyy HH:mm}. ¿Continuar?", "Restaurar", "Cancelar")) return;
            await DriveSync.Restaurar(); await Marcar();
            await DisplayAlert("Listo", "Datos restaurados desde tu Drive.", "OK");
        });
        salir.Clicked += async (_, _) => { DriveSync.CerrarSesion(); await Refrescar(); };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = {
                Ui.Titulo("RESPALDO EN GOOGLE DRIVE"), estado, conectar, subir, bajar, salir,
                new Label { Text = "El respaldo es una copia completa de tus datos y queda en una carpeta privada de la app en tu Drive. Úsalo desde un solo teléfono a la vez.", FontSize = 12, TextColor = Colors.Gray } } }) } } };
                Content = Ui.ConFab(Content);
    }

    static async Task Marcar()
    {
        if (await DriveSync.UltimoRespaldo() is DateTime m) Preferences.Set("ultima_sync", m.ToUniversalTime().Ticks);
    }

    protected override async void OnAppearing() { base.OnAppearing(); await Refrescar(); }

    async Task Refrescar()
    {
        var sesion = await DriveSync.HaySesion();
        conectar.IsVisible = !sesion; subir.IsVisible = bajar.IsVisible = salir.IsVisible = sesion;
        estado.Text = "Sin conectar.";
        if (!sesion) return;
        try { estado.Text = await DriveSync.UltimoRespaldo() is DateTime m ? $"Último respaldo: {m:dd/MM/yyyy HH:mm}" : "Cuenta conectada. Aún no hay respaldo."; }
        catch { estado.Text = "Cuenta conectada (sin conexión)."; }
    }

    async Task Ejecutar(Func<Task> accion)
    {
        estado.Text = "Trabajando…";
        try { await accion(); }
        catch (TaskCanceledException) { }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        await Refrescar();
    }
}

public class EditarVentaPage : ContentPage
{
    readonly int id;
    readonly Entry rut = new() { Placeholder = "RUT*" }, nombre = new() { Placeholder = "Nombre*" }, apellido = new() { Placeholder = "Apellido*" },
        pcs = new() { Placeholder = "Teléfono móvil* (9 dígitos)", Keyboard = Keyboard.Numeric, MaxLength = 9 };
    static readonly string[] Estados = { "Pendiente", "Activa", "Rechazada" };
    readonly Picker plan = new() { Title = "Plan*", TextColor = Ui.Pizarra }, estado = new() { Title = "Estado*", ItemsSource = Estados, TextColor = Ui.Pizarra };
    readonly DatePicker fVenta = new(), fAct = new();
    readonly Label actLbl = new() { Text = "Fecha de activación", TextColor = Ui.Pizarra };
    List<PlanMovil> planes = new();
    bool cargado;

    public EditarVentaPage(int id)
    {
        this.id = id; Title = "Editar venta"; BackgroundColor = Ui.Fondo;
        estado.SelectedIndexChanged += (_, _) => actLbl.IsVisible = fAct.IsVisible = estado.SelectedItem as string == "Activa";
        var guardar = new Button { Text = "GUARDAR CAMBIOS", BackgroundColor = Ui.Rojo, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
        var cancelar = new Button { Text = "Cancelar", BackgroundColor = Colors.Gray, TextColor = Colors.White };
        guardar.Clicked += async (_, _) => await Guardar();
        cancelar.Clicked += async (_, _) => await Navigation.PopAsync();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                Ui.Titulo("CLIENTE"), rut, nombre, apellido,
                new Label { Text = "Si el cliente tiene varias ventas, un cambio de nombre se aplica a todas.", FontSize = 12, TextColor = Colors.Gray },
                Ui.Titulo("VENTA"), pcs, plan, estado,
                new Label { Text = "Fecha de venta", TextColor = Ui.Pizarra }, fVenta, actLbl, fAct, guardar, cancelar } }) } } };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (cargado) return;
        cargado = true;
        var r = await Db.ObtenerVenta(id);
        if (r == null) { await Navigation.PopAsync(); return; }
        var (v, c) = r.Value;
        planes = await Db.Planes();
        plan.ItemsSource = planes.Select(p => p.Nombre).ToList();
        plan.SelectedIndex = planes.FindIndex(p => p.Id == v.IdPlan);
        rut.Text = c.Rut; nombre.Text = c.Nombre; apellido.Text = c.Apellido; pcs.Text = v.Pcs;
        fVenta.Date = v.FechaVenta;
        fAct.Date = v.FechaActivacion ?? DateTime.Today;
        estado.SelectedIndex = Math.Max(Array.IndexOf(Estados, v.Estado), 0);
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(rut.Text) || string.IsNullOrWhiteSpace(nombre.Text) || string.IsNullOrWhiteSpace(apellido.Text)
            || pcs.Text?.Length != 9 || plan.SelectedIndex < 0)
        { await DisplayAlert("Faltan datos", "Completa los campos obligatorios (teléfono de 9 dígitos).", "OK"); return; }

        var est = (string)estado.SelectedItem;
        var fv = Ui.D(fVenta.Date);
        DateTime? act = est == "Activa" ? Ui.D(fAct.Date) : null;
        if (act < fv && !await DisplayAlert("Fechas", "La activación es anterior a la venta. ¿Guardar igual?", "Guardar", "Revisar")) return;

        await Db.ActualizarVenta(id, rut.Text, nombre.Text.Trim(), apellido.Text.Trim(), pcs.Text, planes[plan.SelectedIndex].Id, fv, est, act);
        await Navigation.PopAsync();
    }
}

public abstract class WebPageBase : ContentPage
{
    readonly WebView web = new();

    protected WebPageBase(string titulo, string url, string? valor = null, string[]? campos = null)
    {
        Title = titulo; BackgroundColor = Ui.Fondo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All); // la página se achica cuando aparece el teclado

        web.Navigated += async (_, _) =>
        {
            try
            {
                await web.EvaluateJavaScriptAsync(@"
if(!window.__ventasFoco){
  window.__ventasFoco=true;
  var ver=function(){var a=document.activeElement;
    if(a&&a!==document.body&&(a.tagName==='INPUT'||a.tagName==='TEXTAREA'||a.tagName==='SELECT'))
      a.scrollIntoView({block:'center'});};
  document.addEventListener('focusin',function(){setTimeout(ver,300);setTimeout(ver,700);});
  window.addEventListener('resize',function(){setTimeout(ver,100);});
}");
                if (!string.IsNullOrEmpty(valor)) await web.EvaluateJavaScriptAsync(ScriptRelleno(valor, campos ?? Array.Empty<string>()));
            }
            catch { }
        };

        Content = web;
        web.Source = url;
    }

    // Busca el campo (la página se dibuja con JavaScript, así que espera a que aparezca) y escribe el valor
    static string ScriptRelleno(string valor, string[] campos)
    {
        var v = new string(valor.Where(char.IsLetterOrDigit).ToArray());
        var sel = "[" + string.Join(",", campos.Select(s => "'" + s.Replace("'", "\\'") + "'")) + "]";
        return @"
(function(v,sels){
  var n=0;
  var poner=function(i){
    if(i.value) return;
    var set=Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set;
    set.call(i,v);
    i.dispatchEvent(new Event('input',{bubbles:true}));
    i.dispatchEvent(new Event('change',{bubbles:true}));
  };
  var id=setInterval(function(){
    n++;
    for(var k=0;k<sels.length;k++){var i=document.querySelector(sels[k]); if(i){poner(i);clearInterval(id);return;}}
    if(n>30){
      var g=document.querySelectorAll('input[type=text],input[type=tel],input:not([type])');
      for(var j=0;j<g.length;j++){ if(g[j].offsetParent!==null){poner(g[j]);clearInterval(id);return;} }
    }
    if(n>120) clearInterval(id);
  },250);
})('" + v + "'," + sel + ");";
    }

    // El botón Atrás de Android retrocede dentro de la página antes de salir
    protected override bool OnBackButtonPressed()
    {
        if (web.CanGoBack) { web.GoBack(); return true; }
        return base.OnBackButtonPressed();
    }
}

public class PagoExpressPage : WebPageBase
{
    static readonly string[] Campos =
    {
        "input[placeholder*=\"rut\" i]", "input[name*=\"rut\" i]", "input[id*=\"rut\" i]",
        "input[formcontrolname*=\"rut\" i]", "input[aria-label*=\"rut\" i]"
    };

    public PagoExpressPage(string? rut = null)
        : base("Portal Pagos", "https://sucursalvirtual.clarochile.cl/PagoExpress/index", rut, Campos)
    {
        if (rut != null) // abierta desde una venta: pantalla modal con encabezado propio
        {
            BackgroundColor = Ui.Rojo;
            Content = Ui.ConEncabezado("Revisar pago", Content, async () => await Navigation.PopModalAsync());
        }
    }
}

public class NumerosPortadosPage : WebPageBase
{
    static readonly string[] Campos = { "input[placeholder*=\"deseas consultar\"]", "input[type=tel]", "input[type=text]" };

    public NumerosPortadosPage(string? numero = null)
        : base("Números portados", "https://www.numerosportados.cl/PublicWebsite/#/tn-inquiry", numero, Campos)
    {
        if (numero != null) // abierta desde una venta: pantalla modal con encabezado propio
        {
            BackgroundColor = Ui.Rojo;
            Content = Ui.ConEncabezado("Revisar activación", Content, async () => await Navigation.PopModalAsync());
        }
    }
}

public class MasPage : ContentPage
{
    public MasPage()
    {
        Title = "Más"; BackgroundColor = Ui.Fondo;
        var lista = new VerticalStackLayout { Padding = 16, Spacing = 12 };
        lista.Children.Add(Opcion("Planes", "Crear, editar y eliminar planes", () => new PlanesPage()));
        lista.Children.Add(Opcion("Portal Pagos", "Pago Express de Claro", () => new PagoExpressPage()));
        lista.Children.Add(Opcion("Números portados", "Consulta de portabilidad", () => new NumerosPortadosPage()));
        lista.Children.Add(Opcion("Respaldo", "Copia de tus datos en Google Drive", () => new RespaldoPage()));
        lista.Children.Add(new Label
        {
            Text = $"Versión {AppInfo.Current.VersionString}",
            TextColor = Colors.Gray, FontSize = 12, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 12, 0, 0)
        });
        Content = Ui.ConFab(new ScrollView { Content = lista });
    }

    View Opcion(string titulo, string detalle, Func<Page> crear)
    {
        var card = Ui.Card(new VerticalStackLayout { Spacing = 2, Children = {
            new Label { Text = titulo, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pizarra },
            new Label { Text = detalle, TextColor = Colors.Gray } } });
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Navigation.PushAsync(crear());
        card.GestureRecognizers.Add(tap);
        return card;
    }
}