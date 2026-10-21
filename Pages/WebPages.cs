namespace VentasClaro;

/// <summary>Página con WebView que escribe un valor (RUT o número) en el primer campo que coincida.</summary>
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
                await web.EvaluateJavaScriptAsync(ScriptFoco);
                if (!string.IsNullOrEmpty(valor)) await web.EvaluateJavaScriptAsync(ScriptRelleno(valor, campos ?? Array.Empty<string>()));
            }
            catch { /* la página puede haber cambiado mientras se ejecutaba el script */ }
        };

        Content = web;
        web.Source = url;
    }

    // Mantiene visible el campo enfocado cuando aparece el teclado
    const string ScriptFoco = @"
if(!window.__ventasFoco){
  window.__ventasFoco=true;
  var ver=function(){var a=document.activeElement;
    if(a&&a!==document.body&&(a.tagName==='INPUT'||a.tagName==='TEXTAREA'||a.tagName==='SELECT'))
      a.scrollIntoView({block:'center'});};
  document.addEventListener('focusin',function(){setTimeout(ver,300);setTimeout(ver,700);});
  window.addEventListener('resize',function(){setTimeout(ver,100);});
}";

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

    /// <summary>Cuando se abre desde una venta es una pantalla modal con encabezado propio.</summary>
    protected void ComoModal(string tituloModal)
    {
        BackgroundColor = Ui.Rojo;
        Content = Ui.ConEncabezado(tituloModal, Content, async () => await Navigation.PopModalAsync());
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
        if (rut != null) ComoModal("Revisar pago");
    }
}

public class NumerosPortadosPage : WebPageBase
{
    static readonly string[] Campos = { "input[placeholder*=\"deseas consultar\"]", "input[type=tel]", "input[type=text]" };

    public NumerosPortadosPage(string? numero = null)
        : base("Números portados", "https://www.numerosportados.cl/PublicWebsite/#/tn-inquiry", numero, Campos)
    {
        if (numero != null) ComoModal("Revisar activación");
    }
}
