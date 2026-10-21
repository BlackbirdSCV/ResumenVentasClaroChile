namespace VentasClaro;

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
