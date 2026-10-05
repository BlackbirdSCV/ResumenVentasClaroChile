using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace VentasClaro;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Window is { } w)
        {
            w.SetStatusBarColor(Android.Graphics.Color.ParseColor("#D50000"));
            WindowCompat.GetInsetsController(w, w.DecorView).AppearanceLightStatusBars = false; // íconos blancos
        }
    }
}