namespace ExcelClient.Android;

[global::Android.App.ApplicationAttribute]
public class MainApplication : global::Microsoft.Maui.MauiApplication
{
    public MainApplication(global::System.IntPtr handle, global::Android.Runtime.JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override global::Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
