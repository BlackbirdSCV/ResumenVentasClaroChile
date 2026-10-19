using Microsoft.Maui.Controls.Shapes;
namespace VentasClaro;

public static class Ui
{
    public static readonly Color Rojo = Color.FromArgb("#D50000");
    public static readonly Color Pizarra = Color.FromArgb("#374151");
    public static readonly Color Fondo = Color.FromArgb("#FAFAFA");

    public static Border Card(View v) => new()
    {
        Content = v, Padding = 16, BackgroundColor = Colors.White, StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Shadow = new Shadow { Opacity = .15f, Radius = 8, Offset = new Point(0, 2) }
    };
    public static Label Titulo(string t) => new() { Text = t, TextColor = Rojo, FontAttributes = FontAttributes.Bold, FontSize = 14 };
    public static DateTime D(object? d) => Convert.ToDateTime(d); // DatePicker.Date: nullable en .NET 10

   static Button Copiable(string texto, string valorACopiar)
{
    var b = new Button
    {
        Text = texto, TextColor = Pizarra, BackgroundColor = Colors.Transparent,
        BorderColor = Colors.LightGray, BorderWidth = 1, CornerRadius = 8,
        Padding = new Thickness(10, 0), HeightRequest = 34, FontSize = 14,
        HorizontalOptions = LayoutOptions.Start
    };
    b.Clicked += async (_, _) =>
    {
        await Clipboard.Default.SetTextAsync(valorACopiar);
        b.Text = "✓ Copiado";
        await Task.Delay(1000);
        b.Text = texto;
    };
    return b;
}

    public static View FilaCard(Fila f, params View[] extra)
    {
        var s = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = f.Cliente, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Pizarra },
                new HorizontalStackLayout { Spacing = 8, Children = {
                Copiable(f.Rut, f.Rut.Replace("-", "").Replace(".", "")),
                Copiable(f.PcsFmt, f.Pcs.Replace(" ", "")) } },
                new Label { Text = f.NombrePlan, TextColor = Rojo },
                new Label { Text = $"Venta {f.FechaVenta}  ·  Activación {f.FechaActivacion ?? "—"}  ·  {f.Estado}", FontSize = 12, TextColor = Colors.Gray }
            }
        };

        if (f.VoucherCompetencia)
            s.Children.Add(new Label { Text = "Voucher de la competencia ingresado", TextColor = Rojo, FontSize = 12, FontAttributes = FontAttributes.Bold });
        if (!string.IsNullOrWhiteSpace(f.Comentario))
            s.Children.Add(new Label { Text = f.Comentario, TextColor = Colors.Gray, FontSize = 12, FontAttributes = FontAttributes.Italic });
            
        foreach (var e in extra) s.Children.Add(e);
        return Card(s);
    }

    public static View ConFab(View contenido)
    {
        if (contenido is ScrollView { Content: Layout l })
        l.Padding = new Thickness(l.Padding.Left, l.Padding.Top, l.Padding.Right, 96);

        var fab = new Border
        {
            HeightRequest = 52, Padding = new Thickness(22, 0), StrokeThickness = 0, BackgroundColor = Rojo,
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Shadow = new Shadow { Opacity = .35f, Radius = 8, Offset = new Point(0, 3) },
            HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 20, 20),
            Content = new Label { Text = "Nueva Venta", TextColor = Colors.White, FontSize = 16, FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await Shell.Current.Navigation.PushModalAsync(new NuevaVentaPage());
            fab.GestureRecognizers.Add(tap);

        var g = new Grid();
            g.Children.Add(contenido); g.Children.Add(fab);
        return g;
    }

    public static event Action? DatosCambiaron;
    public static void AvisarCambio() => DatosCambiaron?.Invoke();

    public static View ConEncabezado(string titulo, View contenido, Func<Task> cerrar)
    {
        var cerrarBtn = new Button { Text = "Cerrar", TextColor = Colors.White, BackgroundColor = Colors.Transparent, FontAttributes = FontAttributes.Bold };
        cerrarBtn.Clicked += async (_, _) => await cerrar();
        var cab = new Grid { BackgroundColor = Rojo, Padding = new Thickness(16, 4), ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        cab.Add(new Label { Text = titulo, TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0, 0);
        cab.Add(cerrarBtn, 1, 0);
        contenido.BackgroundColor = Fondo;
        var g = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        g.Add(cab, 0, 0); g.Add(contenido, 0, 1);
        return g;
    }

    public static Switch Interruptor()
    {
        var sw = new Switch { OnColor = Color.FromArgb("#F2A5A5"), ThumbColor = Color.FromArgb("#9E9E9E"), VerticalOptions = LayoutOptions.Center };
        sw.Toggled += (_, e) => sw.ThumbColor = e.Value ? Rojo : Color.FromArgb("#9E9E9E");
        return sw;
    }

}