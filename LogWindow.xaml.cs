using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace takip
{
    public partial class LogWindow : Window
    {
        private MainWindow _mainWindow;
        private DispatcherTimer _refreshTimer;

        public LogWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // MainWindow'a LogWindow'un açıldığını bildir
            _mainWindow.OnLogWindowOpened();

            // Otomatik yenileme timer'ı (30 saniyede bir - performans için)
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(30);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // İlk yükleme
            RefreshLogs();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void RefreshLogs()
        {
            try
            {
                var logs = _mainWindow.GetLogMessages();
                
                // ⚡ PERFORMANS: Sadece son 200 log satırını göster (binlerce satırı render etmeyi önle)
                var maxDisplayLines = 200;
                var displayLogs = logs.Count > maxDisplayLines 
                    ? logs.Skip(logs.Count - maxDisplayLines).ToList() 
                    : logs;
                
                var logText = string.Join("\n", displayLogs);
                LogTextBlock.Text = logText;
                LogCountText.Text = $"Gösterilen: {displayLogs.Count} / Toplam: {logs.Count} log";

                // Otomatik scroll en alta
                LogScrollViewer.ScrollToEnd();
            }
            catch (Exception ex)
            {
                LogTextBlock.Text = $"Log yükleme hatası: {ex.Message}";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLogs();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.ClearLogMessages();
            RefreshLogs();
        }

        private void CopyLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(LogTextBlock.Text);
                MessageBox.Show("Loglar panoya kopyalandı!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kopyalama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Metin Dosyası (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, LogTextBlock.Text, Encoding.UTF8);
                    MessageBox.Show("Loglar başarıyla kaydedildi!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // MainWindow'a LogWindow'un kapandığını bildir
            _mainWindow.OnLogWindowClosed();

            // Timer'ı durdur
            _refreshTimer?.Stop();

            base.OnClosed(e);
        }
    }
}