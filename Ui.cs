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

    static Label Copiable(string texto, string valorACopiar)
    {
        var l = new Label { Text = texto, TextColor = Colors.Gray, TextDecorations = TextDecorations.Underline };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            await Clipboard.Default.SetTextAsync(valorACopiar);
            l.Text = "✓ Copiado";
            await Task.Delay(1000);
            l.Text = texto;
        };
        l.GestureRecognizers.Add(tap);
        return l;
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
    new Label { Text = "·", TextColor = Colors.Gray },
    Copiable(f.PcsFmt, f.Pcs.Replace(" ", "")) } },
                new Label { Text = f.NombrePlan, TextColor = Rojo },
                new Label { Text = $"Venta {f.FechaVenta}  ·  Activación {f.FechaActivacion ?? "—"}  ·  {f.Estado}", FontSize = 12, TextColor = Colors.Gray }
            }
        };
        foreach (var e in extra) s.Children.Add(e);
        return Card(s);
    }

}
