using Android.App;
using Android.Content;
using Android.Content.PM;
namespace VentasClaro;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.googleusercontent.apps.258805615162-dp4c6f1si8270shsio4f6plrng1ae484")]
public class WebAuthCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity { }
