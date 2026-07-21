using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace ExcelClient.Android;

[Activity(Theme = "@android:style/Theme.NoTitleBar", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
