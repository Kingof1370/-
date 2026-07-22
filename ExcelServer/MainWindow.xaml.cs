using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ExcelServer;

public partial class MainWindow
{
    private string _excelFilePath = string.Empty;
    private List<ExcelDataRow> _loadedData = new();
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _serverCts;
    private DispatcherTimer? _autoReloadTimer;
    private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_settings.txt");

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        LoadSettings();
        DisplayLocalIP();
        SetupAutoReloadTimer();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                _excelFilePath = File.ReadAllText(_settingsFilePath).Trim();
                TxtExcelPath.Text = _excelFilePath;
                Log($"مسیر ذخیره‌شده فایل اکسل بارگذاری شد: {_excelFilePath}");
            }
        }
        catch (Exception ex)
        {
            Log($"خطا در بارگذاری تنظیمات: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        try
        {
            File.WriteAllText(_settingsFilePath, _excelFilePath);
        }
        catch (Exception ex)
        {
            Log($"خطا در ذخیره تنظیمات: {ex.Message}");
        }
    }

    private void DisplayLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            LblLocalIP.Text = $"IP محلی: {ip?.ToString() ?? "127.0.0.1"}";
        }
        catch
        {
            LblLocalIP.Text = "IP محلی: 127.0.0.1";
        }
    }

    private void SetupAutoReloadTimer()
    {
        _autoReloadTimer = new DispatcherTimer();
        _autoReloadTimer.Interval = TimeSpan.FromMinutes(30);
        _autoReloadTimer.Tick += (s, e) =>
        {
            Log("شروع بارگذاری خودکار دوره‌ای (هر ۳۰ دقیقه)...");
            ReloadExcelDataSilently();
        };
        _autoReloadTimer.Start();
    }

    private static string GetPersianNow()
    {
        var pc = new PersianCalendar();
        var now = DateTime.Now;
        return $"{pc.GetYear(now):D4}/{pc.GetMonth(now):D2}/{pc.GetDayOfMonth(now):D2} {now:HH:mm:ss}";
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            TxtLogs.AppendText($"[{GetPersianNow()}] {message}\n");
            TxtLogs.ScrollToEnd();
        });
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*",
            Title = "انتخاب فایل اکسل داده‌ها"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _excelFilePath = openFileDialog.FileName;
            TxtExcelPath.Text = _excelFilePath;
            SaveSettings();
            Log($"فایل اکسل انتخاب شد: {_excelFilePath}");
        }
    }

    private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_excelFilePath))
        {
            System.Windows.MessageBox.Show("لطفاً ابتدا مسیر فایل اکسل را مشخص کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _loadedData = ExcelParser.ParseExcelFile(_excelFilePath);
            LblLastUpdate.Text = $"آخرین به‌روزرسانی: {GetPersianNow()}";
            Log($"فایل با موفقیت بارگذاری شد. تعداد ردیف‌های یافت‌شده: {_loadedData.Count}");
            System.Windows.MessageBox.Show($"فایل با موفقیت بارگذاری شد. تعداد ردیف‌ها: {_loadedData.Count}", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"خطا در بارگذاری فایل: {ex.Message}");
            System.Windows.MessageBox.Show($"خطا در بارگذاری فایل اکسل:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReloadFile_Click(object sender, RoutedEventArgs e)
    {
        ReloadExcelDataSilently();
        System.Windows.MessageBox.Show("داده‌های اکسل با موفقیت مجدداً بارگذاری شد.", "اطلاعات", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ReloadExcelDataSilently()
    {
        if (string.IsNullOrEmpty(_excelFilePath))
        {
            Log("خطا در بارگذاری مجدد: مسیر فایل نامعتبر است.");
            return;
        }

        try
        {
            _loadedData = ExcelParser.ParseExcelFile(_excelFilePath);
            Dispatcher.Invoke(() =>
            {
                LblLastUpdate.Text = $"آخرین به‌روزرسانی: {GetPersianNow()}";
            });
            Log($"بارگذاری مجدد موفقیت‌آمیز بود. تعداد کل ردیف‌ها: {_loadedData.Count}");
        }
        catch (Exception ex)
        {
            Log($"خطا در بارگذاری خودکار/مجدد: {ex.Message}");
        }
    }

    private void BtnStartServer_Click(object sender, RoutedEventArgs e)
    {
        if (_tcpListener != null)
        {
            System.Windows.MessageBox.Show("سرور قبلاً راه‌اندازی شده و در حال اجرا است.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _serverCts = new CancellationTokenSource();
            _tcpListener = new TcpListener(IPAddress.Any, 12345);
            _tcpListener.Start();

            LblServerStatus.Text = "وضعیت: در حال اجرا (Port 12345)";
            Log("سرور TCP با موفقیت روی پورت 12345 شروع به کار کرد.");

            Task.Run(() => ListenForClientsAsync(_serverCts.Token));
        }
        catch (Exception ex)
        {
            Log($"خطا در شروع سرور: {ex.Message}");
            System.Windows.MessageBox.Show($"خطا در شروع سرور:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            _tcpListener = null;
        }
    }

    private async Task ListenForClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_tcpListener == null) break;
                var tcpClient = await _tcpListener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClientAsync(tcpClient, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"خطا در پذیرش اتصال جدید: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        Log($"کلاینت متصل شد: {clientEndPoint}");

        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead > 0)
                {
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Log($"درخواست دریافت شد از {clientEndPoint}: {request}");

                    // Request format is expected to be "YYYY/MM/DD"
                    string responseJson = ProcessQuery(request);

                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                    Log($"پاسخ برای {clientEndPoint} ارسال شد.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"خطا در پاسخ‌گویی به کلاینت {clientEndPoint}: {ex.Message}");
        }
    }

    private string ProcessQuery(string dateQuery)
    {
        // Parse "YYYY/MM/DD" or "YYYY-MM-DD"
        string[] parts = dateQuery.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return JsonConvert.SerializeObject(new { error = "قالب تاریخ نامعتبر است. فرمت مورد انتظار: YYYY/MM/DD" });
        }

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
        {
            return JsonConvert.SerializeObject(new { error = "پارامترهای تاریخ باید عددی باشند." });
        }

        var matchingRow = _loadedData.FirstOrDefault(r => r.Year == year && r.Month == month && r.Day == day);
        if (matchingRow == null)
        {
            return JsonConvert.SerializeObject(new { error = $"داده‌ای برای تاریخ {dateQuery} پیدا نشد." });
        }

        return JsonConvert.SerializeObject(matchingRow);
    }

    private void BtnStopServer_Click(object sender, RoutedEventArgs e)
    {
        StopServer();
    }

    private void StopServer()
    {
        if (_tcpListener == null)
        {
            return;
        }

        try
        {
            _serverCts?.Cancel();
            _tcpListener.Stop();
            _tcpListener = null;

            LblServerStatus.Text = "وضعیت: متوقف";
            Log("سرور TCP متوقف شد.");
        }
        catch (Exception ex)
        {
            Log($"خطا در توقف سرور: {ex.Message}");
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == System.Windows.WindowState.Minimized)
        {
            // Minimize to Tray
            Hide();
            CreateTrayIcon();
        }
    }

    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    private void CreateTrayIcon()
    {
        if (_notifyIcon != null) return;

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "سرور گزارش‌گیری اکسل (علی بهمنی)"
        };

        _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("نمایش برنامه", null, (s, e) => RestoreFromTray());
        contextMenu.Items.Add("خروج", null, (s, e) => ShutdownApp());
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = System.Windows.WindowState.Normal;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private void ShutdownApp()
    {
        StopServer();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        ShutdownApp();
        base.OnClosed(e);
    }
}
