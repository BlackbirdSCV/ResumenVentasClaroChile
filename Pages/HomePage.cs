namespace VentasClaro;

public class HomePage : ContentPage
{
    readonly Label activas = new() { FontSize = 64, FontAttributes = FontAttributes.Bold, TextColor = Ui.Rojo, HorizontalOptions = LayoutOptions.Center };
    readonly VerticalStackLayout lista = new() { Spacing = 12 };

    public HomePage()
    {
        Title = "Resumen de Ventas"; BackgroundColor = Ui.Fondo;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.All); // la página se achica cuando aparece el teclado
        Content = Ui.ConFab(new ScrollView { Content = new VerticalStackLayout { Padding = 16, Spacing = 16, Children = {
            Ui.Card(new VerticalStackLayout { Children = { Ui.Titulo("VENTAS ACTIVAS DEL MES"), activas } }),
            Ui.Titulo("SOLICITUDES PENDIENTES"), lista } } });
        Ui.DatosCambiaron += () => Ui.Seguro(Cargar);
    }

    protected override void OnAppearing() { base.OnAppearing(); Ui.Seguro(Cargar); }

    async Task Cargar()
    {
        activas.Text = (await Db.ActivasMes()).ToString();
        lista.Children.Clear();
        foreach (var f in await Db.Pendientes())
        {
            var id = f.Id; var pcs = f.Pcs;
            var edit = Ui.Boton("Editar", Ui.Pizarra);
            var ok = Ui.Boton("Marcar activa", Ui.Rojo);
            var del = Ui.Boton("Eliminar", Colors.Gray);
            var revisar = Ui.Boton("Revisar activación", Ui.Rojo);
            foreach (var b in new[] { edit, ok, del }) { b.FontSize = 13; b.Padding = new Thickness(4, 10); } // que el texto quepa en un tercio

            edit.Clicked += async (_, _) => await Navigation.PushAsync(new EditarVentaPage(id));
            ok.Clicked += (_, _) => Ui.Seguro(async () => { await Db.Activar(id); await Cargar(); });
            del.Clicked += (_, _) => Ui.Seguro(async () =>
            {
                if (await DisplayAlertAsync("Eliminar", "¿Eliminar esta venta?", "Sí", "No")) { await Db.Eliminar(id); await Cargar(); }
            });
            revisar.Clicked += async (_, _) =>
            {
                await Clipboard.Default.SetTextAsync(pcs); // respaldo: pegar si el relleno falla
                await Shell.Current.Navigation.PushModalAsync(new NumerosPortadosPage(pcs));
            };

            var botones = new Grid { RowSpacing = 8, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) } };
            var fila1 = Ui.Botones(edit, ok, del);
            botones.Add(fila1, 0, 0); botones.Add(revisar, 0, 1);
            lista.Children.Add(Ui.FilaCard(f, botones));
        }
    }
}
