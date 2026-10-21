namespace VentasClaro;

public class IngresosPage : ContentPage
{
    const decimal IVA = 1.19m;
    DateTime mes = Ui.InicioMes();

    readonly Label titulo = Ui.TituloMes();
    readonly Label neto = new() { FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Ui.Rojo, HorizontalOptions = LayoutOptions.Center };
    readonly Label detalle = new() { TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center };
    readonly Label txtPlanes = new() { TextColor = Ui.Pizarra }, txtIngreso = new() { TextColor = Ui.Pizarra };
    readonly ProgressBar barPlanes = new() { ProgressColor = Ui.Rojo }, barIngreso = new() { ProgressColor = Ui.Rojo };
    readonly Entry metaPlanes = new() { Placeholder = "Meta de planes (cantidad)", Keyboard = Keyboard.Numeric, MaxLength = 5 };
    readonly Entry metaIngreso = new() { Placeholder = "Meta de ingreso neto, sin IVA (ej: 1500000)", Keyboard = Keyboard.Numeric, MaxLength = 10 };
    readonly Label avisoMeta = new() { FontSize = 12, TextColor = Colors.Gray };

    public IngresosPage()
    {
        Title = "Ingresos"; BackgroundColor = Ui.Fondo;
        Ui.DatosCambiaron += () => Ui.Seguro(Cargar);

        var nav = Ui.NavMes(titulo, async delta => { mes = mes.AddMonths(delta); await Todo(); });
        var guardar = Ui.Boton("GUARDAR META", Ui.Rojo, negrita: true);
        guardar.Clicked += (_, _) => Ui.Seguro(GuardarMeta);

        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = {
            nav,
            Ui.Card(new VerticalStackLayout { Spacing = 6, Children = {
                Ui.Titulo("INGRESO NETO DEL MES"), neto, detalle,
                new Label { Text = "Cuentan los planes activados en el mes (por fecha de activación), sin IVA (19%).", FontSize = 12, TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center } } }),
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                Ui.Titulo("CUMPLIMIENTO DE METAS"), txtPlanes, barPlanes, txtIngreso, barIngreso } }),
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                Ui.Titulo("META DEL MES"), metaPlanes, metaIngreso, avisoMeta, guardar } }) } } });
    }

    protected override void OnAppearing() { base.OnAppearing(); Ui.Seguro(Todo); }

    async Task Todo() { await CargarCampos(); await Cargar(); }

    // Las metas escritas se cargan solo al abrir la pestaña o cambiar de mes, para no pisar lo que estás escribiendo
    async Task CargarCampos()
    {
        var clave = Ui.Clave(mes);
        var meta = await Db.MetaMes(clave);
        if (meta != null)
        {
            metaPlanes.Text = meta.Planes > 0 ? meta.Planes.ToString() : "";
            metaIngreso.Text = meta.Ingreso > 0 ? meta.Ingreso.ToString() : "";
            avisoMeta.Text = "";
            return;
        }
        var ult = await Db.UltimaMeta(clave);
        metaPlanes.Text = ult != null && ult.Planes > 0 ? ult.Planes.ToString() : "";
        metaIngreso.Text = ult != null && ult.Ingreso > 0 ? ult.Ingreso.ToString() : "";
        avisoMeta.Text = ult != null ? "Este mes no tiene meta. Se muestran las del mes anterior: pulsa GUARDAR META para fijarlas." : "Este mes no tiene meta.";
    }

    async Task Cargar()
    {
        var clave = Ui.Clave(mes);
        titulo.Text = Ui.NombreMes(mes);
        var fila = (await Db.TotalesMes(clave)).FirstOrDefault();
        var n = fila?.N ?? 0;
        var bruto = fila?.Bruto ?? 0;
        var netoMes = (long)Math.Round(bruto / IVA, MidpointRounding.AwayFromZero);

        neto.Text = netoMes.ToString("C0", Ui.CL);
        detalle.Text = $"{n} planes activados\nCon IVA: {bruto.ToString("C0", Ui.CL)}";

        var meta = await Db.MetaMes(clave);
        var mp = meta?.Planes ?? 0; var mi = (long)(meta?.Ingreso ?? 0);
        Progreso(txtPlanes, barPlanes, "Planes", n.ToString(), n, mp, mp.ToString());
        Progreso(txtIngreso, barIngreso, "Ingreso neto", netoMes.ToString("C0", Ui.CL), netoMes, mi, mi.ToString("C0", Ui.CL));
    }

    static void Progreso(Label txt, ProgressBar bar, string nombre, string actualTxt, long real, long meta, string metaTxt)
    {
        if (meta <= 0) { txt.Text = $"{nombre}: {actualTxt} (sin meta definida)"; bar.Progress = 0; return; }
        var pct = (double)real / meta;
        bar.Progress = Math.Min(pct, 1);
        txt.Text = $"{nombre}: {actualTxt} de {metaTxt}  ·  {pct.ToString("P0", Ui.CL)}" + (pct >= 1 ? "  ✓ ¡Meta cumplida!" : "");
    }

    async Task GuardarMeta()
    {
        static int Num(string? t) => int.TryParse(new string((t ?? "").Where(char.IsDigit).ToArray()), out var v) ? v : 0;
        var p = Num(metaPlanes.Text); var i = Num(metaIngreso.Text);
        if (p <= 0 && i <= 0) { await DisplayAlertAsync("Meta", "Ingresa al menos una meta mayor a 0.", "OK"); return; }
        await Db.GuardarMeta(Ui.Clave(mes), p, i);
        avisoMeta.Text = "";
        await Cargar();
        await DisplayAlertAsync("Listo", "Meta guardada.", "OK");
    }
}