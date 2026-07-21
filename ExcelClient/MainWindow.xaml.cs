using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using Newtonsoft.Json;
using MahApps.Metro.Controls;

namespace ExcelClient;

public partial class MainWindow : MetroWindow
{
    private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_settings.txt");

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // Load Saved IP Settings
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                TxtServerIp.Text = File.ReadAllText(_settingsFilePath).Trim();
            }
            catch {}
        }

        // Apply Persian Culture to the DatePicker & window
        try
        {
            var persianCulture = new CultureInfo("fa-IR");
            System.Threading.Thread.CurrentThread.CurrentCulture = persianCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = persianCulture;
            DpReportDate.Language = System.Windows.Markup.XmlLanguage.GetLanguage("fa-IR");
        }
        catch {}

        DpReportDate.SelectedDate = DateTime.Now;
    }

    private void SaveIpSetting(string ip)
    {
        try
        {
            File.WriteAllText(_settingsFilePath, ip.Trim());
        }
        catch {}
    }

    private async void BtnFetchReport_Click(object sender, RoutedEventArgs e)
    {
        string serverIp = TxtServerIp.Text.Trim();
        if (string.IsNullOrEmpty(serverIp))
        {
            MessageBox.Show("لطفاً آدرس IP سرور را وارد کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SaveIpSetting(serverIp);

        if (DpReportDate.SelectedDate == null)
        {
            MessageBox.Show("لطفاً یک تاریخ معتبر انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DateTime selectedDate = DpReportDate.SelectedDate.Value;
        string dateQuery = GetPersianDateString(selectedDate);

        BtnFetchReport.IsEnabled = false;
        BtnFetchReport.Content = "در حال دریافت...";

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
                MessageBox.Show(errorObj?.error ?? "خطای نامشخص از سرور", "خطای سرور", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearUI();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<ExcelDataRow>(jsonResponse);
                if (data != null)
                {
                    PopulateUI(data, dateQuery);
                }
                else
                {
                    throw new Exception("ساختار داده دریافتی نامعتبر است.");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در اتصال به سرور:\n{ex.Message}\nمطمئن شوید سرور در حال اجرا بوده و فایروال پورت 12345 را مسدود نکرده است.", "خطا در ارتباط", MessageBoxButton.OK, MessageBoxImage.Error);
            ClearUI();
        }
        finally
        {
            BtnFetchReport.IsEnabled = true;
            BtnFetchReport.Content = "دریافت گزارش از سرور";
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

    private void PopulateUI(ExcelDataRow data, string persianDate)
    {
        TxtPrintTitle.Text = $"سنگ آهن ونوس زاگرس - گزارش روزانه خط تولید - تاریخ: {persianDate}";
        TxtPrintTitle.Visibility = Visibility.Visible;

        // SECTION 1: KPIs
        TxtFeedWeight1.Text = data.FeedWeight1.ToString("N2");
        TxtFeedGrade1.Text = data.FeedGrade1.ToString("N2");
        TxtRecoveryStage1.Text = data.RecoveryStage1.ToString("N2") + " %";
        TxtRecoveryStage2.Text = data.RecoveryStage2.ToString("N2") + " %";
        TxtRecoveryScavenger.Text = data.RecoveryScavenger.ToString("N2") + " %";
        TxtRecoveryTotal.Text = data.RecoveryTotal.ToString("N2") + " %";

        // SECTION 2: Mass Balance
        TxtMassBalanceStage1.Text = data.MassBalanceStage1.ToString("N2") + " %";
        ApplyMassBalanceStyle(BorderMass1, data.MassBalanceStage1);

        TxtMassBalanceStage2.Text = data.MassBalanceStage2.ToString("N2") + " %";
        ApplyMassBalanceStyle(BorderMass2, data.MassBalanceStage2);

        TxtMassBalanceScavenger.Text = data.MassBalanceScavenger.ToString("N2") + " %";
        ApplyMassBalanceStyle(BorderMassScavenger, data.MassBalanceScavenger);

        // SECTION 3: Sales and Stock
        TxtCumulativeSales.Text = data.CumulativeSales.ToString("N2");
        TxtCumulativeCarryStock.Text = data.CumulativeCarryStock.ToString("N2");
        TxtRemainingPileStock.Text = data.RemainingPileStock.ToString("N2");
        TxtAverageSalesGrade.Text = data.AverageSalesGrade.ToString("N2") + " %";
        TxtAverageCarryStockGrade.Text = data.AverageCarryStockGrade.ToString("N2") + " %";

        // SECTION 4: Resources and Consumption
        TxtPersonnelCount.Text = data.PersonnelCount.ToString("N0");
        TxtDieselConsumption.Text = data.DieselConsumption.ToString("N2");
        TxtLineOperatingHours.Text = data.LineOperatingHours.ToString("N2");

        // SECTION 5: Separator Status
        TxtSepPre1.Text = data.PreProcessingSeparator1;
        ApplySeparatorStyle(BorderSepPre1, data.PreProcessingSeparator1);

        TxtSepPre2.Text = data.PreProcessingSeparator2;
        ApplySeparatorStyle(BorderSepPre2, data.PreProcessingSeparator2);

        TxtSepProc1.Text = data.ProcessingSeparator1;
        ApplySeparatorStyle(BorderSepProc1, data.ProcessingSeparator1);

        TxtSepProc2.Text = data.ProcessingSeparator2;
        ApplySeparatorStyle(BorderSepProc2, data.ProcessingSeparator2);

        TxtSepScav.Text = data.ScavengerSeparator;
        ApplySeparatorStyle(BorderSepScav, data.ScavengerSeparator);

        // SECTION 6: Scrollable text report
        TxtReportText.Text = data.ReportText;
    }

    private void ApplyMassBalanceStyle(Border border, double errorPercentage)
    {
        if (Math.Abs(errorPercentage) > 5.0)
        {
            border.Background = new SolidColorBrush(Color.FromRgb(255, 230, 230));
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100));
        }
        else
        {
            border.Background = new SolidColorBrush(Color.FromRgb(230, 255, 230));
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 200, 100));
        }
    }

    private void ApplySeparatorStyle(Border border, string status)
    {
        if (status != null && (status.Trim() == "روشن" || status.Trim().ToLower() == "on" || status.Trim().ToLower() == "active"))
        {
            border.Background = new SolidColorBrush(Color.FromRgb(220, 255, 220));
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(40, 180, 40));
        }
        else
        {
            border.Background = new SolidColorBrush(Color.FromRgb(255, 220, 220));
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 50, 50));
        }
    }

    private void ClearUI()
    {
        TxtPrintTitle.Visibility = Visibility.Collapsed;

        TxtFeedWeight1.Text = "--";
        TxtFeedGrade1.Text = "--";
        TxtRecoveryStage1.Text = "--";
        TxtRecoveryStage2.Text = "--";
        TxtRecoveryScavenger.Text = "--";
        TxtRecoveryTotal.Text = "--";

        TxtMassBalanceStage1.Text = "--";
        BorderMass1.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderMass1.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtMassBalanceStage2.Text = "--";
        BorderMass2.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderMass2.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtMassBalanceScavenger.Text = "--";
        BorderMassScavenger.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderMassScavenger.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtCumulativeSales.Text = "--";
        TxtCumulativeCarryStock.Text = "--";
        TxtRemainingPileStock.Text = "--";
        TxtAverageSalesGrade.Text = "--";
        TxtAverageCarryStockGrade.Text = "--";

        TxtPersonnelCount.Text = "--";
        TxtDieselConsumption.Text = "--";
        TxtLineOperatingHours.Text = "--";

        TxtSepPre1.Text = "--";
        BorderSepPre1.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderSepPre1.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtSepPre2.Text = "--";
        BorderSepPre2.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderSepPre2.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtSepProc1.Text = "--";
        BorderSepProc1.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderSepProc1.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtSepProc2.Text = "--";
        BorderSepProc2.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderSepProc2.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtSepScav.Text = "--";
        BorderSepScav.Background = new SolidColorBrush(Color.FromRgb(253, 253, 253));
        BorderSepScav.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        TxtReportText.Text = string.Empty;
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(PrintArea, "گزارش روزانه اکسل");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در انجام فرآیند چاپ:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
