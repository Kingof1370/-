using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace ExcelClient.Android;

public partial class MainPage : ContentPage
{
    private readonly string _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "client_settings.txt");
    private ExcelDataRow? _currentData;

    public MainPage()
    {
        InitializeComponent();
        LoadIpSetting();
        DpReportDate.Date = DateTime.Now;
    }

    private void LoadIpSetting()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                EntServerIp.Text = File.ReadAllText(_settingsFilePath).Trim();
            }
        }
        catch {}
    }

    private void SaveIpSetting(string ip)
    {
        try
        {
            File.WriteAllText(_settingsFilePath, ip.Trim());
        }
        catch {}
    }

    private async void OnBtnFetchClicked(object sender, EventArgs e)
    {
        string serverIp = EntServerIp.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(serverIp))
        {
            await DisplayAlert("خطا", "لطفاً آدرس IP سرور را وارد کنید.", "تایید");
            return;
        }

        SaveIpSetting(serverIp);

        DateTime selectedDate = DpReportDate.Date;
        string dateQuery = GetPersianDateString(selectedDate);

        BtnFetch.IsEnabled = false;
        BtnFetch.Text = "در حال دریافت...";

        try
        {
            string jsonResponse = await FetchDataFromServerAsync(serverIp, 12345, dateQuery);
            if (string.IsNullOrEmpty(jsonResponse))
            {
                throw new Exception("پاسخ خالی از سرور دریافت شد.");
            }

            if (jsonResponse.Contains("\"error\""))
            {
                var errorObj = JsonConvert.DeserializeAnonymousType(jsonResponse, new { error = "" });
                await DisplayAlert("خطای سرور", errorObj?.error ?? "خطای نامشخص از سرور", "تایید");
                ClearUI();
            }
            else
            {
                _currentData = JsonConvert.DeserializeObject<ExcelDataRow>(jsonResponse);
                if (_currentData != null)
                {
                    PopulateUI(_currentData);
                }
                else
                {
                    throw new Exception("ساختار داده دریافتی نامعتبر است.");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطا در ارتباط", $"خطا در اتصال به سرور:\n{ex.Message}\nمطمئن شوید سرور فعال و در دسترس است.", "تایید");
            ClearUI();
        }
        finally
        {
            BtnFetch.IsEnabled = true;
            BtnFetch.Text = "دریافت گزارش";
        }
    }

    private string GetPersianDateString(DateTime date)
    {
        var pc = new PersianCalendar();
        int year = pc.GetYear(date);
        int month = pc.GetMonth(date);
        int day = pc.GetDayOfMonth(date);
        return $"{year}/{month:D2}/{day:D2}";
    }

    private async Task<string> FetchDataFromServerAsync(string ip, int port, string query)
    {
        using (var client = new TcpClient())
        {
            var connectTask = client.ConnectAsync(ip, port);
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                throw new TimeoutException("زمان اتصال به سرور پایان یافت (Timeout).");
            }

            using (var stream = client.GetStream())
            {
                byte[] queryBytes = Encoding.UTF8.GetBytes(query);
                await stream.WriteAsync(queryBytes, 0, queryBytes.Length);

                byte[] buffer = new byte[8192];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
            }
        }
        return string.Empty;
    }

    private void PopulateUI(ExcelDataRow data)
    {
        // SECTION 1: KPIs
        LblFeedWeight1.Text = data.FeedWeight1.ToString("N2");
        LblFeedGrade1.Text = data.FeedGrade1.ToString("N2");
        LblRecoveryStage1.Text = data.RecoveryStage1.ToString("N2") + "%";
        LblRecoveryStage2.Text = data.RecoveryStage2.ToString("N2") + "%";
        LblRecoveryScavenger.Text = data.RecoveryScavenger.ToString("N2") + "%";
        LblRecoveryTotal.Text = data.RecoveryTotal.ToString("N2") + "%";

        // SECTION 2: Mass Balance
        LblMassBalanceStage1.Text = data.MassBalanceStage1.ToString("N2") + "%";
        ApplyMassBalanceStyle(FrameMass1, data.MassBalanceStage1);

        LblMassBalanceStage2.Text = data.MassBalanceStage2.ToString("N2") + "%";
        ApplyMassBalanceStyle(FrameMass2, data.MassBalanceStage2);

        LblMassBalanceScavenger.Text = data.MassBalanceScavenger.ToString("N2") + "%";
        ApplyMassBalanceStyle(FrameMassScavenger, data.MassBalanceScavenger);

        // SECTION 3: Sales & Stock
        LblCumulativeSales.Text = data.CumulativeSales.ToString("N2");
        LblCumulativeCarryStock.Text = data.CumulativeCarryStock.ToString("N2");
        LblRemainingPileStock.Text = data.RemainingPileStock.ToString("N2");
        LblAverageSalesGrade.Text = data.AverageSalesGrade.ToString("N2") + "%";
        LblAverageCarryStockGrade.Text = data.AverageCarryStockGrade.ToString("N2") + "%";

        // SECTION 4: Resources & Consumption
        LblPersonnelCount.Text = data.PersonnelCount.ToString("N0");
        LblDieselConsumption.Text = data.DieselConsumption.ToString("N2");
        LblLineOperatingHours.Text = data.LineOperatingHours.ToString("N2");

        // SECTION 5: Separator Status
        LblSepPre1.Text = data.PreProcessingSeparator1;
        ApplySeparatorStyle(FrameSepPre1, data.PreProcessingSeparator1);

        LblSepPre2.Text = data.PreProcessingSeparator2;
        ApplySeparatorStyle(FrameSepPre2, data.PreProcessingSeparator2);

        LblSepProc1.Text = data.ProcessingSeparator1;
        ApplySeparatorStyle(FrameSepProc1, data.ProcessingSeparator1);

        LblSepProc2.Text = data.ProcessingSeparator2;
        ApplySeparatorStyle(FrameSepProc2, data.ProcessingSeparator2);

        LblSepScav.Text = data.ScavengerSeparator;
        ApplySeparatorStyle(FrameSepScav, data.ScavengerSeparator);

        // SECTION 6: Text Report
        EdReportText.Text = data.ReportText;
    }

    private void ApplyMassBalanceStyle(Frame frame, double errorPercentage)
    {
        if (Math.Abs(errorPercentage) > 5.0)
        {
            frame.BackgroundColor = Color.FromRgb(255, 230, 230); // light red
            frame.BorderColor = Color.FromRgb(255, 100, 100);
        }
        else
        {
            frame.BackgroundColor = Color.FromRgb(230, 255, 230); // light green
            frame.BorderColor = Color.FromRgb(100, 200, 100);
        }
    }

    private void ApplySeparatorStyle(Frame frame, string status)
    {
        if (status != null && (status.Trim() == "روشن" || status.Trim().ToLower() == "on" || status.Trim().ToLower() == "active"))
        {
            frame.BackgroundColor = Color.FromRgb(220, 255, 220); // green
            frame.BorderColor = Color.FromRgb(40, 180, 40);
        }
        else
        {
            frame.BackgroundColor = Color.FromRgb(255, 220, 220); // red
            frame.BorderColor = Color.FromRgb(230, 50, 50);
        }
    }

    private void ClearUI()
    {
        LblFeedWeight1.Text = "--";
        LblFeedGrade1.Text = "--";
        LblRecoveryStage1.Text = "--";
        LblRecoveryStage2.Text = "--";
        LblRecoveryScavenger.Text = "--";
        LblRecoveryTotal.Text = "--";

        LblMassBalanceStage1.Text = "--";
        FrameMass1.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameMass1.BorderColor = Color.FromRgb(238, 238, 238);

        LblMassBalanceStage2.Text = "--";
        FrameMass2.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameMass2.BorderColor = Color.FromRgb(238, 238, 238);

        LblMassBalanceScavenger.Text = "--";
        FrameMassScavenger.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameMassScavenger.BorderColor = Color.FromRgb(238, 238, 238);

        LblCumulativeSales.Text = "--";
        LblCumulativeCarryStock.Text = "--";
        LblRemainingPileStock.Text = "--";
        LblAverageSalesGrade.Text = "--";
        LblAverageCarryStockGrade.Text = "--";

        LblPersonnelCount.Text = "--";
        LblDieselConsumption.Text = "--";
        LblLineOperatingHours.Text = "--";

        LblSepPre1.Text = "--";
        FrameSepPre1.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameSepPre1.BorderColor = Color.FromRgb(238, 238, 238);

        LblSepPre2.Text = "--";
        FrameSepPre2.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameSepPre2.BorderColor = Color.FromRgb(238, 238, 238);

        LblSepProc1.Text = "--";
        FrameSepProc1.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameSepProc1.BorderColor = Color.FromRgb(238, 238, 238);

        LblSepProc2.Text = "--";
        FrameSepProc2.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameSepProc2.BorderColor = Color.FromRgb(238, 238, 238);

        LblSepScav.Text = "--";
        FrameSepScav.BackgroundColor = Color.FromRgb(252, 252, 252);
        FrameSepScav.BorderColor = Color.FromRgb(238, 238, 238);

        EdReportText.Text = string.Empty;
        _currentData = null;
    }

    private async void OnBtnShareClicked(object sender, EventArgs e)
    {
        if (_currentData == null)
        {
            await DisplayAlert("خطا", "لطفاً ابتدا داده‌ها را دریافت کنید تا بتوانید گزارش را ذخیره یا به اشتراک بگذارید.", "تایید");
            return;
        }

        try
        {
            string dateStr = GetPersianDateString(DpReportDate.Date);
            string reportContent = $"سنگ آهن ونوس زاگرس - گزارش روزانه خط تولید - تاریخ {dateStr}\n" +
                                   "=========================================\n" +
                                   "توسعه‌دهنده: علی بهمنی | 09915420558\n" +
                                   "=========================================\n" +
                                   $"وزن خوراک مرحله1: {_currentData.FeedWeight1:N2}\n" +
                                   $"عیار خوراک مرحله1: {_currentData.FeedGrade1:N2} %\n" +
                                   $"بازیابی مرحله1: {_currentData.RecoveryStage1:N2} %\n" +
                                   $"بازیابی مرحله2: {_currentData.RecoveryStage2:N2} %\n" +
                                   $"بازیابی رمق‌گیری: {_currentData.RecoveryScavenger:N2} %\n" +
                                   $"بازیابی کل: {_currentData.RecoveryTotal:N2} %\n" +
                                   "=========================================\n" +
                                   $"موازنه جرم مرحله1: {_currentData.MassBalanceStage1:N2} %\n" +
                                   $"موازنه جرم مرحله2: {_currentData.MassBalanceStage2:N2} %\n" +
                                   $"موازنه جرم رمق‌گیری: {_currentData.MassBalanceScavenger:N2} %\n" +
                                   "=========================================\n" +
                                   $"فروش تجمعی: {_currentData.CumulativeSales:N2} (تن)\n" +
                                   $"حمل تجمعی به استوک: {_currentData.CumulativeCarryStock:N2} (تن)\n" +
                                   $"مانده استوک پایل: {_currentData.RemainingPileStock:N2} (تن)\n" +
                                   $"میانگین عیار فروش: {_currentData.AverageSalesGrade:N2} %\n" +
                                   $"میانگین عیار حمل به استوک: {_currentData.AverageCarryStockGrade:N2} %\n" +
                                   "=========================================\n" +
                                   $"تعداد پرسنل: {_currentData.PersonnelCount} (نفر)\n" +
                                   $"مصرف گازوئیل: {_currentData.DieselConsumption:N2} (لیتر)\n" +
                                   $"ساعت کارکرد خط: {_currentData.LineOperatingHours:N2} (ساعت)\n" +
                                   "=========================================\n" +
                                   $"سپراتور پیش‌فرآوری 1: {_currentData.PreProcessingSeparator1}\n" +
                                   $"سپراتور پیش‌فرآوری 2: {_currentData.PreProcessingSeparator2}\n" +
                                   $"سپراتور فرآوری 1: {_currentData.ProcessingSeparator1}\n" +
                                   $"سپراتور فرآوری 2: {_currentData.ProcessingSeparator2}\n" +
                                   $"سپراتور رمق‌گیری: {_currentData.ScavengerSeparator}\n" +
                                   "=========================================\n" +
                                   $"توضیحات و گزارش متنی:\n{_currentData.ReportText}\n";

            string tempFile = Path.Combine(FileSystem.CacheDirectory, $"Report_{dateStr.Replace("/", "_")}.txt");
            await File.WriteAllTextAsync(tempFile, reportContent);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"سنگ آهن ونوس زاگرس - گزارش روزانه خط تولید {dateStr}",
                File = new ShareFile(tempFile)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطا", $"خطا در اشتراک‌گذاری گزارش:\n{ex.Message}", "تایید");
        }
    }
}
