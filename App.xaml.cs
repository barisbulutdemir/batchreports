using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;

namespace takip;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "TakipApplicationMutex";

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single Instance kontrolü
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // Program zaten çalışıyor - mevcut pencereyi aktif et
            ActivateExistingWindow();
            Shutdown();
            return;
        }

        base.OnStartup(e);
        
        // Command line argument'ları kontrol et
        if (e.Args.Length > 0)
        {
            switch (e.Args[0].ToLower())
            {
                case "--help":
                    Console.WriteLine("Kullanılabilir komutlar:");
                    Console.WriteLine("  --help: Bu yardım mesajını göster");
                    Console.WriteLine("  --version: Versiyon bilgisini göster");
                    Shutdown();
                    return;
                    
                case "--version":
                    Console.WriteLine("Takip Sistemi v1.0");
                    Shutdown();
                    return;
                    
                default:
                    Console.WriteLine($"Bilinmeyen komut: {e.Args[0]}");
                    Console.WriteLine("--help ile yardım alabilirsiniz");
                    Shutdown(1);
                    return;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Mutex'i serbest bırak
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        
        base.OnExit(e);
    }

    /// <summary>
    /// Mevcut pencereyi aktif et
    /// </summary>
    private static void ActivateExistingWindow()
    {
        try
        {
            // Mevcut MainWindow'u bul ve aktif et
            var mainWindow = Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                // System tray'den geri getir
                mainWindow.ShowFromTray();
                
                // Pencereyi öne getir
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mevcut pencereyi aktif etme hatası: {ex.Message}");
        }
    }
}