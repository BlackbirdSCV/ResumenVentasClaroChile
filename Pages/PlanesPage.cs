namespace VentasClaro;

public class PlanesPage : ContentPage
{
    readonly Entry nombre = new() { Placeholder = "Nombre del plan*" };
    readonly Entry valor = new() { Placeholder = "Valor mensual* (ej: 15990)", Keyboard = Keyboard.Numeric, MaxLength = 7 };
    readonly Label tituloForm = Ui.Titulo("NUEVO PLAN");
    readonly Button guardar = Ui.Boton("AGREGAR PLAN", Ui.Rojo, negrita: true);
    readonly Button cancelar = Ui.Boton("Cancelar", Colors.Gray);
    readonly VerticalStackLayout lista = new() { Spacing = 12 };
    int? editando;

    public PlanesPage()
    {
        Title = "Planes"; BackgroundColor = Ui.Fondo;
        cancelar.IsVisible = false;
        guardar.Clicked += (_, _) => Ui.Seguro(Guardar);
        cancelar.Clicked += (_, _) => Limpiar();
        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 16, Children = {
            Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { tituloForm, nombre, valor, guardar, cancelar } }),
            Ui.Titulo("PLANES"), lista } } });
    }

    protected override void OnAppearing() { base.OnAppearing(); Ui.Seguro(Cargar); }

    void Limpiar()
    {
        editando = null; nombre.Text = ""; valor.Text = "";
        tituloForm.Text = "NUEVO PLAN"; guardar.Text = "AGREGAR PLAN"; cancelar.IsVisible = false;
    }

    async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(nombre.Text) || !int.TryParse(valor.Text, out var v) || v <= 0)
        { await DisplayAlertAsync("Faltan datos", "Ingresa un nombre y un valor numérico mayor a 0.", "OK"); return; }
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
            var edit = Ui.Boton("Editar", Ui.Pizarra);
            var del = Ui.Boton("Eliminar", Colors.Gray);
            edit.Clicked += (_, _) =>
            {
                editando = plan.Id; nombre.Text = plan.Nombre; valor.Text = plan.Valor.ToString();
                tituloForm.Text = "EDITAR PLAN"; guardar.Text = "GUARDAR CAMBIOS"; cancelar.IsVisible = true;
            };
            del.Clicked += (_, _) => Ui.Seguro(async () =>
            {
                if (!await DisplayAlertAsync("Eliminar plan", $"¿Eliminar \"{plan.Nombre}\"?", "Sí", "No")) return;
                if (!await Db.EliminarPlan(plan.Id)) await DisplayAlertAsync("No se puede eliminar", "Este plan tiene ventas asociadas.", "OK");
                else if (editando == plan.Id) Limpiar();
                await Cargar();
            });
            lista.Children.Add(Ui.Card(new VerticalStackLayout { Spacing = 8, Children = {
                new VerticalStackLayout { Children = {
                    new Label { Text = plan.Nombre, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Ui.Pizarra },
                    new Label { Text = $"${plan.Valor:N0} mensual", TextColor = Ui.Rojo } } },
                new HorizontalStackLayout { Spacing = 8, Children = { edit, del } } } }));
        }
    }
}
