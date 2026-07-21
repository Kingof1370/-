namespace ExcelClient.Android;

[global::Android.App.Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.ScreenSize
                         | global::Android.Content.PM.ConfigChanges.Orientation
                         | global::Android.Content.PM.ConfigChanges.UiMode
                         | global::Android.Content.PM.ConfigChanges.ScreenLayout
                         | global::Android.Content.PM.ConfigChanges.SmallestScreenSize
                         | global::Android.Content.PM.ConfigChanges.Density)]
public class MainActivity : global::Microsoft.Maui.MauiAppCompatActivity
{
}
