using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace ExcelClient;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // تنظیم تقویم شمسی (فارسی) به عنوان تقویم پیش‌فرض برنامه
        var persianCulture = (CultureInfo)new CultureInfo("fa-IR").Clone();
        persianCulture.DateTimeFormat.Calendar = new PersianCalendar();
        persianCulture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Saturday;
        persianCulture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        Thread.CurrentThread.CurrentCulture = persianCulture;
        Thread.CurrentThread.CurrentUICulture = persianCulture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(persianCulture.IetfLanguageTag)));
        base.OnStartup(e);
    }
}
