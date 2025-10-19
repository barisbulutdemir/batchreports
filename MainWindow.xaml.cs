using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Services;
using takip.Utils;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace takip
{
    /// <summary>
    /// Ana pencere - Üretim Takip Sistemi
    /// </summary>
    public partial class MainWindow : Window
    {
        // 🔧 MIXER1 REGISTER ADRESLERİ - UI'DAN DEĞİŞTİRİLEBİLİR (TEST AMAÇLI)
        public static string REG_M1_CIMENTO1_KG = "DM4404";
        public static string REG_M1_CIMENTO2_KG = "DM4414";
        public static string REG_M1_CIMENTO3_KG = "DM4424";
        public static string REG_M1_SU_LOADCELL_KG = "DM204";
        public static string REG_M1_SU_PULSE_KG = "DM130";
        public static string REG_M1_NEM = "DM120";
        public static string REG_M1_AGREGA1_KG = "DM4204";
        public static string REG_M1_AGREGA2_KG = "DM4214";
        public static string REG_M1_AGREGA3_KG = "DM4224";
        public static string REG_M1_AGREGA4_KG = "DM4234";
        public static string REG_M1_AGREGA5_KG = "DM4244";
        
        // 🔧 MIXER1 PIGMENT REGISTER ADRESLERİ (Sadece 1 pigment)
        public static string REG_M1_PIGMENT1_KG = "DM208";
        
        // 🔧 MIXER1 KATKI REGISTER ADRESLERİ
        public static string REG_M1_KATKI1_CHEMICAL_KG = "DM4104";
        public static string REG_M1_KATKI1_WATER_KG = "DM4105";
        public static string REG_M1_KATKI2_CHEMICAL_KG = "DM4114";
        public static string REG_M1_KATKI2_WATER_KG = "DM4115";
        public static string REG_M1_KATKI3_CHEMICAL_KG = "DM4124";
        public static string REG_M1_KATKI3_WATER_KG = "DM4125";
        public static string REG_M1_KATKI4_CHEMICAL_KG = "DM4134";
        public static string REG_M1_KATKI4_WATER_KG = "DM4135";
        
        // 🔧 MIXER2 REGISTER ADRESLERİ
        public static string REG_M2_CIMENTO1_KG = "DM4434";
        public static string REG_M2_CIMENTO2_KG = "DM4444";
        public static string REG_M2_CIMENTO3_KG = "DM4454";
        public static string REG_M2_SU_LOADCELL_KG = "DM304";
        public static string REG_M2_SU_PULSE_KG = "DM306";
        public static string REG_M2_NEM = "DM122";
        public static string REG_M2_AGREGA1_KG = "DM4704";
        public static string REG_M2_AGREGA2_KG = "DM4714";
        public static string REG_M2_AGREGA3_KG = "DM4724";
        public static string REG_M2_AGREGA4_KG = "DM4734";
        public static string REG_M2_AGREGA5_KG = "DM4744";
        public static string REG_M2_AGREGA6_KG = "DM4754";
        public static string REG_M2_AGREGA7_KG = "DM4764";
        public static string REG_M2_AGREGA8_KG = "DM4774";
        public static string REG_M2_KATKI1_CHEMICAL_KG = "DM4604";
        public static string REG_M2_KATKI1_WATER_KG = "DM4605";
        public static string REG_M2_KATKI2_CHEMICAL_KG = "DM4614";
        public static string REG_M2_KATKI2_WATER_KG = "DM4615";
        public static string REG_M2_KATKI3_CHEMICAL_KG = "DM4624";
        public static string REG_M2_KATKI3_WATER_KG = "DM4625";
        public static string REG_M2_KATKI4_CHEMICAL_KG = "DM4634";
        public static string REG_M2_KATKI4_WATER_KG = "DM4635";
        
        private ProductionDbContext _context = null!;
        private DispatcherTimer _productionTimer = null!;
        private DispatcherTimer _logCleanupTimer = null!;
        private DispatcherTimer _concretePageTimer = null!;
        private ProductionService _productionService = new ProductionService();
        private ShiftRecordService _shiftRecordService = null!;
        private OperatorService _operatorService = null!;
        private PdfExportService _pdfExportService = new PdfExportService();
        private MoldService _moldService = null!;
        private LocalizationService _localizationService = LocalizationService.Instance;
        private PlcDataService _plcDataService = null!;
        private ShiftMoldTrackingService _shiftMoldTrackingService = null!;
        private ActiveShiftService _activeShiftService = null!;

        // Vardiya durumu
        private bool _shiftActive = false;
        // private bool _productionStarted = false; // Kullanılmıyor
        private DateTime? _shiftStartTime = null;
        private DateTime? _productionStartTime = null;
        private int _currentShiftId = 0; // Aktif vardiya ID'si
        private string _currentOperatorName = "";

        // Üretim sayıları
        // private int _lastMachineCount = 0; // Kullanılmıyor
        private Dictionary<string, int> _stoneCounters = new Dictionary<string, int>();
        private Dictionary<string, int> _shiftStoneCounters = new Dictionary<string, int>();
        private string _lastLogMessage = "";

        // Global log sistemi - OPTIMIZE EDİLDİ
        private readonly List<string> _globalLogMessages = new List<string>();
        private readonly object _logLock = new object();
        private const int MaxLogMessages = 1000;
        private bool _logWindowOpen = false; // LogWindow açık mı kontrolü
        private LogWindow? _logWindow = null; // LogWindow referansı
        
        // Background M2DataWindow kaldırıldı - artık gerek yok

        // DM452 polling (Üretim Takibi - ayrı PLC)
        private DispatcherTimer _dm452Timer = null!;
        private HslCommunication.Profinet.Omron.OmronFinsNet? _dm452Client;
        private int? _dm452LastValue = null; // geçici alan - son okunan değer
        private int _totalPalletProduction = 0; // Toplam palet üretimi
        private const string Dm452Ip = "192.168.250.1";
        private const int Dm452Port = 9600;

        // Fire mal sayısı ve boşta geçen süre takibi
        private DispatcherTimer _fireProductTimer = null!;
        private HslCommunication.Profinet.Omron.OmronFinsNet? _fireProductClient;
        private int _currentFireProductCount = 0;
        private int _startFireProductCount = 0;
        private DateTime _lastProductionTime = DateTime.Now;
        private int _idleTimeSeconds = 0;
        private const string FireProductIp = "192.168.250.1";
        private const int FireProductPort = 9600;

        // Boşta geçen süre PLC takibi
        private DispatcherTimer _idleTimeTimer = null!;
        private HslCommunication.Profinet.Omron.OmronFinsNet? _idleTimeClient;
        private int _currentIdleTimeSeconds = 0;
        private int _startIdleTimeSeconds = 0;
        private const string IdleTimeIp = "192.168.250.1";
        private const int IdleTimePort = 9600;

        // Vardiya log sistemi
        private readonly List<string> _vardiyaLogMessages = new List<string>();
        private DispatcherTimer _vardiyaLogCleanupTimer = null!;

        // System tray özellikleri
        private WinForms.NotifyIcon? _notifyIcon;
        private bool _isMinimizedToTray = false;
        private bool _shouldClose = false;

        public MainWindow()
        {
            try
            {
                DetailedLogger.ClearLog();
                DetailedLogger.LogInfo("MainWindow constructor başlatılıyor...");
                
            InitializeComponent();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.InitializeComponentCompleted"));
                
            InitializeDatabase();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.InitializeDatabaseCompleted"));
                
                // Veritabanı tablolarını kontrol et
                DetailedLogger.LogInfo("Veritabanı tabloları kontrol ediliyor...");
                DatabaseChecker.CheckDatabase();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.DatabaseCheckCompleted"));
                
                
                // PLC veri servisini başlat
                InitializePlcService();
                // DetailedLogger.LogInfo("Çimento silo servisleri test başlatıldı");
            
                // DM452 timer init (vardiya ile birlikte aç/kapat)
                _dm452Timer = new DispatcherTimer();
                _dm452Timer.Interval = TimeSpan.FromSeconds(2);
                _dm452Timer.Tick += async (s, e) => await PollDm452Once();

                // Log cleanup timer init (2 dakikada bir temizle)
                _vardiyaLogCleanupTimer = new DispatcherTimer();
                _vardiyaLogCleanupTimer.Interval = TimeSpan.FromMinutes(2);
                _vardiyaLogCleanupTimer.Tick += (s, e) => ClearVardiyaLog();
                
            InitializeServices();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.InitializeServicesCompleted"));
                
            InitializeTimer();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.InitializeTimerCompleted"));
                
                // Uygulama açılışında yarım kalan batch'leri geri yükle
                _ = Task.Run(async () => await RecoverOpenBatchesAsync());
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.RecoverOpenBatchesTriggered"));
                
                // Uygulama açılışında aktif vardiyayı kurtar
                _ = Task.Run(async () => await RecoverActiveShiftAsync());
                DetailedLogger.LogInfo("MainWindow.RecoverActiveShiftTriggered");
                
            // LoadInitialData'yı Task.Run ile çağır
            _ = Task.Run(async () => 
            {
                await LoadInitialData();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.LoadInitialDataCompleted"));
            });
                
            InitializeLocalization();
                DetailedLogger.LogInfo(LocalizationService.Instance.GetString("MainWindow.InitializeLocalizationCompleted"));
            
            // System tray'i başlat
            InitializeSystemTray();
                DetailedLogger.LogInfo("System tray başlatıldı");
            
            // PLC durumunu kontrol et
                DetailedLogger.LogInfo("CheckPlcStatusAsync başlatılıyor... - KALDIRILDI");
            // _ = CheckPlcStatusAsync(); // KALDIRILDI
            
            // PLC veri servisini başlat (MainWindow açılınca otomatik)
            // StartPlcDataService(); // ❌ ÇAKIŞMA! Yukarıda InitializePlcService() zaten başlatıyor!
                
            // Mixer1 sistemi kaldırıldı - sıfırdan yapacağız
            
            // LogWindow otomatik açma kaldırıldı - sadece manuel açılımda çalışacak
                
                DetailedLogger.LogInfo("MainWindow constructor başarıyla tamamlandı");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("MainWindow constructor hatası", ex);
                Console.WriteLine($"[MainWindow] Constructor hatası: {ex.Message}");
                Console.WriteLine($"[MainWindow] Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"[MainWindow] Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // OpenLogWindow fonksiyonu kaldırıldı - artık otomatik açılmıyor

        private async void StartDm452Polling()
        {
            try
            {
                if (_dm452Client == null)
                {
                    _dm452Client = new HslCommunication.Profinet.Omron.OmronFinsNet(Dm452Ip, Dm452Port);
                    _dm452Client.ConnectTimeOut = 1000; // Daha kısa timeout
                    _dm452Client.ReceiveTimeOut = 1000;
                    
                    // Async bağlantı
                    var result = await Task.Run(() => _dm452Client.ConnectServer());
                    if (!result.IsSuccess)
                    {
                        AddVardiyaLog($"DM452 PLC bağlantı hatası: {result.Message}");
                        _dm452Client = null;
                        return;
                    }
                }
                _dm452Timer?.Start();
                AddVardiyaLog("DM452 polling başlatıldı (2 sn)");
            }
            catch (Exception ex)
            {
                AddVardiyaLog($"DM452 polling başlatma hatası: {ex.Message}");
            }
        }

        private void StopDm452Polling()
        {
            try
            {
                _dm452Timer?.Stop();
                _dm452Client?.ConnectClose();
                _dm452Client = null;
                AddLog("DM452 polling durduruldu");
            }
            catch (Exception ex)
            {
                AddLog($"DM452 polling durdurma hatası: {ex.Message}");
            }
        }

        private async Task PollDm452Once()
        {
            try
            {
                if (_dm452Client == null)
                {
                    StartDm452Polling();
                    if (_dm452Client == null) return;
                }

                var read = await Task.Run(() => _dm452Client!.ReadUInt16("DM452"));
                if (!read.IsSuccess)
                {
                AddLog($"DM452 read error: {read.Message}");
                    return;
                }

                var value = (int)read.Content;
                AddVardiyaLog($"🔍 DM452 read: {value}, _dm452LastValue: {_dm452LastValue}, _totalPalletProduction: {_totalPalletProduction}");
                if (_dm452LastValue == null)
                {
                    _dm452LastValue = value; // başlangıç değeri
                    // Recovery sırasında palet sayısını koru - sıfırlama
                    AddVardiyaLog($"DM452 start: {value} (Total pallets: {_totalPalletProduction} - RECOVERY MODE)");
                    // Recovery sırasında _totalPalletProduction değerini koru, sıfırlama
                    UpdatePalletProductionUI();
                }
                else if (_dm452LastValue != value)
                {
                    var diff = value - _dm452LastValue.Value;
                    _dm452LastValue = value;
                    if (diff > 0) // Sadece pozitif değişiklikleri say
                    {
                        _totalPalletProduction += diff;
                        AddVardiyaLog($"DM452 changed: new={value} (Δ={diff}) → Total pallets +{diff} = {_totalPalletProduction}");
                        
                        // Aktif kalıbın baskı sayısını da güncelle
                        await UpdateActiveMoldPrintCount(diff);
                        
                        // Üretim zamanını güncelle (boşta geçen süre takibi için)
                        _lastProductionTime = DateTime.Now;
                        
                        // Aktif vardiya kaydına anlık değerleri persist et
                        if (_shiftActive && _currentShiftId > 0)
                        {
                            await _activeShiftService.UpdateActiveShift(_currentShiftId, null, _totalPalletProduction, _dm452LastValue, _currentFireProductCount, _currentIdleTimeSeconds);
                        }
                        
                        UpdatePalletProductionUI();
                    }
                    else
                    {
                        AddVardiyaLog($"DM452 changed: new={value} (Δ={diff}) → Negative change, not counted");
                    }
                }
                else
                {
                    AddVardiyaLog($"DM452: {value}");
                }
            }
            catch (Exception ex)
            {
                AddVardiyaLog($"DM452 polling error: {ex.Message}");
            }
        }

        private async Task PollFireProductCount()
        {
            try
            {
                if (_fireProductClient == null)
                {
                    _fireProductClient = new HslCommunication.Profinet.Omron.OmronFinsNet(FireProductIp, FireProductPort);
                    _fireProductClient.ConnectTimeOut = 1000;
                    _fireProductClient.ReceiveTimeOut = 1000;
                    
                    var result = await Task.Run(() => _fireProductClient.ConnectServer());
                    if (!result.IsSuccess)
                    {
                        AddVardiyaLog($"Fire Product PLC bağlantı hatası: {result.Message}");
                        _fireProductClient = null;
                        return;
                    }
                }

                // D453 register'ından fire mal sayısını oku
                var readResult = await Task.Run(() => _fireProductClient.ReadUInt16("D453"));
                if (readResult.IsSuccess)
                {
                    var newFireCount = (int)readResult.Content;
                    
                    if (_currentFireProductCount != newFireCount)
                    {
                        _currentFireProductCount = newFireCount;
                        
                        // UI'da göster
                        Dispatcher.Invoke(() =>
                        {
                            FireProductText.Text = _currentFireProductCount.ToString();
                        });
                        
                        AddVardiyaLog($"Fire mal sayısı güncellendi: {_currentFireProductCount}");
                        
                        // Aktif vardiya kaydına güncelle
                        if (_shiftActive && _currentShiftId > 0)
                        {
                            await _activeShiftService.UpdateActiveShift(_currentShiftId, null, null, null, _currentFireProductCount, _currentIdleTimeSeconds);
                        }
                    }
                }
                else
                {
                    AddVardiyaLog($"Fire Product register okuma hatası: {readResult.Message}");
                }
            }
            catch (Exception ex)
            {
                AddVardiyaLog($"Fire Product polling error: {ex.Message}");
            }
        }

        private async Task PollIdleTime()
        {
            try
            {
                if (_idleTimeClient == null)
                {
                    _idleTimeClient = new HslCommunication.Profinet.Omron.OmronFinsNet(IdleTimeIp, IdleTimePort);
                    _idleTimeClient.ConnectTimeOut = 1000;
                    _idleTimeClient.ReceiveTimeOut = 1000;
                    
                    var result = await Task.Run(() => _idleTimeClient.ConnectServer());
                    if (!result.IsSuccess)
                    {
                        AddVardiyaLog($"Idle Time PLC bağlantı hatası: {result.Message}");
                        _idleTimeClient = null;
                        return;
                    }
                }

                // D455 register'ından boşta geçen süreyi oku (saniye cinsinden)
                var readResult = await Task.Run(() => _idleTimeClient.ReadUInt16("D455"));
                if (readResult.IsSuccess)
                {
                    var newIdleTimeSeconds = (int)readResult.Content;
                    
                    if (_currentIdleTimeSeconds != newIdleTimeSeconds)
                    {
                        _currentIdleTimeSeconds = newIdleTimeSeconds;
                        
                        // UI'da göster
                        Dispatcher.Invoke(() =>
                        {
                            IdleTimeText.Text = FormatIdleTime(_currentIdleTimeSeconds);
                        });
                        
                        AddVardiyaLog($"Boşta geçen süre güncellendi: {FormatIdleTime(_currentIdleTimeSeconds)}");
                        
                        // Aktif vardiya kaydına güncelle
                        if (_shiftActive && _currentShiftId > 0)
                        {
                            await _activeShiftService.UpdateActiveShift(_currentShiftId, null, null, null, _currentFireProductCount, _currentIdleTimeSeconds);
                        }
                    }
                }
                else
                {
                    AddVardiyaLog($"Idle Time register okuma hatası: {readResult.Message}");
                }
            }
            catch (Exception ex)
            {
                AddVardiyaLog($"Idle Time polling error: {ex.Message}");
            }
        }

        /// <summary>
        /// Boşta geçen süreyi saat:dakika:saniye formatında döndürür
        /// </summary>
        private string FormatIdleTime(int totalSeconds)
        {
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        private void AddVardiyaLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";
                
                _vardiyaLogMessages.Add(logMessage);
                
                // UI'da göster
                Dispatcher.Invoke(() =>
                {
                    VardiyaLogText.Text = string.Join("\n", _vardiyaLogMessages.TakeLast(20));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Vardiya log hatası: {ex.Message}");
            }
        }

        private void ClearVardiyaLog()
        {
            try
            {
                _vardiyaLogMessages.Clear();
                Dispatcher.Invoke(() =>
                {
                    VardiyaLogText.Text = LocalizationService.Instance.GetString("MainWindow.LogCleared");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log temizleme hatası: {ex.Message}");
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            ClearVardiyaLog();
        }
        /// <summary>
        /// Uygulama açılışında yarım kalan batch'leri geri yükler ve RAM içi işaretçileri doldurur.
        /// </summary>
        private static async Task RecoverOpenBatchesAsync()
        {
            try
            {
                using var context = new ProductionDbContext();

                // Mixer1 için açık batch'ler - sadece en son olanı al
                var m1Open = context.ConcreteBatches
                    .Where(b => b.Status == "Tartım Kovasında" || b.Status == "Bekleme Bunkeri" || b.Status == "Mixerde")
                    .OrderByDescending(b => b.Id)
                    .FirstOrDefault();

                if (m1Open != null)
                {
                    if (m1Open.Status == "Tartım Kovasında")
                        _tartimKovasiBatchId = m1Open.Id;
                    else if (m1Open.Status == "Bekleme Bunkeri")
                        _beklemeBunkeriBatchId = m1Open.Id;
                    else if (m1Open.Status == "Mixerde")
                        _mixerdeBatchId = m1Open.Id;
                }

                // Mixer2 için açık batch'ler - sadece en son olanı al
                var m2Open = context.ConcreteBatch2s
                    .Where(b => b.Status == "Yatay Kovada" || b.Status == "Dikey Kovada" || b.Status == "Bekleme Bunkerinde" || b.Status == "Mixerde")
                    .OrderByDescending(b => b.Id)
                    .FirstOrDefault();

                if (m2Open != null)
                {
                    if (m2Open.Status == "Yatay Kovada")
                        _m2YatayKovaBatchId = m2Open.Id;
                    else if (m2Open.Status == "Dikey Kovada")
                        _m2DikeyKovaBatchId = m2Open.Id;
                    else if (m2Open.Status == "Bekleme Bunkerinde")
                        _m2BeklemeBunkeriBatchId = m2Open.Id;
                    else if (m2Open.Status == "Mixerde")
                        _m2MixerdeBatchId = m2Open.Id;
                }

                DetailedLogger.LogInfo($"Recovery tamamlandı. M1: Tartım={_tartimKovasiBatchId}, Bekleme={_beklemeBunkeriBatchId}, Mixer={_mixerdeBatchId}; M2: Yatay={_m2YatayKovaBatchId}, Dikey={_m2DikeyKovaBatchId}, Bekleme={_m2BeklemeBunkeriBatchId}, Mixer={_m2MixerdeBatchId}");
                
                // 🔥 KRİTİK: Recovery sonrası normal PLC tick'leri devam edecek
                DetailedLogger.LogInfo("🔄 Recovery tamamlandı - Normal PLC işleme devam ediyor");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("RecoverOpenBatchesAsync hata", ex);
            }
        }

        /// <summary>
        /// Uygulama açılışında aktif vardiyayı kurtarır
        /// </summary>
        private async Task RecoverActiveShiftAsync()
        {
            try
            {
                
                // Aktif vardiya var mı kontrol et
                var activeShift = await _activeShiftService.GetActiveShift();
                
                if (activeShift == null)
                {
                    DetailedLogger.LogInfo("Aktif vardiya bulunamadı - normal başlangıç");
                    return;
                }

                // Aktif vardiya bulundu, kurtar
                DetailedLogger.LogInfo($"Aktif vardiya kurtarılıyor - Operatör: {activeShift.OperatorName}, Başlangıç: {activeShift.ShiftStartTime:dd.MM.yyyy HH:mm}");

                // UI thread'de güncelle
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        // Vardiya durumunu aktif yap
                        _shiftActive = true;
                        _currentShiftId = activeShift.ShiftRecordId;
                        _currentOperatorName = activeShift.OperatorName;
                        _shiftStartTime = activeShift.ShiftStartTime;
                        _productionStartTime = activeShift.ProductionStartTime;
                        
                        // Palet üretim sayısını geri yükle
                        // Recovery logları temizlendi
                        _totalPalletProduction = activeShift.StartTotalProduction;
                        _dm452LastValue = activeShift.StartDm452Value; // null ise null kalacak
                        // Recovery logları temizlendi

                        // UI güncelle
                        ShiftStartTimeText.Text = _shiftStartTime != null ? TimeZoneHelper.FormatDateTime(_shiftStartTime.Value, "dd.MM.yyyy HH:mm") : "Başlatılmadı";
                        
                        if (_productionStartTime.HasValue)
                        {
                            ProductionStartTimeText.Text = TimeZoneHelper.FormatDateTime(_productionStartTime.Value, "dd.MM.yyyy HH:mm");
                        }
                        
                        // Toplam üretim sayısını UI'da göster
                        TotalProductionText.Text = _totalPalletProduction.ToString();

                        // Operatör seçimini güncelle
                        var operatorIndex = OperatorComboBox.Items.Cast<Operator>()
                            .ToList()
                            .FindIndex(op => op.Name == _currentOperatorName);
                        
                        if (operatorIndex >= 0)
                        {
                            OperatorComboBox.SelectedIndex = operatorIndex;
                            OperatorComboBox.IsEnabled = false;
                        }

                        // Buton durumunu güncelle
                        ToggleShiftButton.Content = "End Shift";
                        ToggleShiftButton.Background = new SolidColorBrush(Colors.Red);

                        // Timer'ları başlat
                        _productionTimer.Start();
                        _vardiyaLogCleanupTimer.Start();
                        StartDm452Polling();

                        AddLog($"✅ Aktif vardiya kurtarıldı - Operatör: {_currentOperatorName}");
                        AddLog($"🕐 Vardiya başlangıcı: {_shiftStartTime:dd.MM.yyyy HH:mm}");
                        AddLog($"📊 Palet üretimi: {_totalPalletProduction} palet");
                        
                        if (_productionStartTime.HasValue)
                        {
                            AddLog($"🏭 Üretim başlangıcı: {_productionStartTime:dd.MM.yyyy HH:mm}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogError($"Aktif vardiya kurtarılırken UI hatası: {ex.Message}");
                        AddLog($"❌ Aktif vardiya kurtarılırken hata: {ex.Message}");
                    }
                });

                DetailedLogger.LogInfo("Aktif vardiya kurtarma tamamlandı");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya kurtarılırken hata: {ex.Message}");
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                DetailedLogger.LogInfo("ProductionDbContext oluşturuluyor...");
                _context = new ProductionDbContext();
                DetailedLogger.LogInfo("ProductionDbContext oluşturuldu");
                
                DetailedLogger.LogInfo("Database.EnsureCreated() çağrılıyor...");
                _context.Database.EnsureCreated();
                DetailedLogger.LogInfo("Database.EnsureCreated() tamamlandı");
                
                // Veritabanı şemasını kontrol et ve gerekirse güncelle
                DetailedLogger.LogInfo("Veritabanı şeması kontrol ediliyor...");
                _ = Task.Run(async () => await CheckAndUpdateDatabaseSchemaAsync());
                DetailedLogger.LogInfo("Veritabanı şeması kontrol başlatıldı");
                
                // Veritabanı tablolarını düzelt
                DetailedLogger.LogInfo("DatabaseFixer.FixDatabase() çağrılıyor...");
                DatabaseFixer.FixDatabase();
                DetailedLogger.LogInfo("DatabaseFixer.FixDatabase() tamamlandı");
                
                // ProductionNotes tablosunu manuel olarak oluştur
                DetailedLogger.LogInfo("ProductionNotes tablosu kontrol ediliyor...");
                CreateProductionNotesTableIfNotExists();
                DetailedLogger.LogInfo("ProductionNotes tablosu kontrol tamamlandı");
                
                // ShiftRecord tablosuna FireProductCount sütunu ekle
                DetailedLogger.LogInfo("ShiftRecord tablosu kontrol ediliyor...");
                AddFireProductCountColumnIfNotExists();
                DetailedLogger.LogInfo("ShiftRecord tablosu kontrol tamamlandı");
                
                AddLog("✅ Veritabanı tabloları kontrol edildi ve düzeltildi");
                DetailedLogger.LogInfo("InitializeDatabase başarıyla tamamlandı");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("InitializeDatabase hatası", ex);
                Console.WriteLine($"[MainWindow] Veritabanı başlatma hatası: {ex.Message}");
                Console.WriteLine($"[MainWindow] Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"[MainWindow] Stack Trace: {ex.StackTrace}");
                AddLog($"Veritabanı başlatma hatası: {ex.Message}");
            }
        }

        private async Task CheckAndUpdateDatabaseSchemaAsync()
        {
            try
            {
                DetailedLogger.LogInfo("Veritabanı şeması kontrol ediliyor...");
                
                using var connection = new Npgsql.NpgsqlConnection(_context.Database.GetConnectionString());
                await connection.OpenAsync();
                
                // TotalWaterKg kolonunu ekle (yoksa)
                try
                {
                    var addTotalWaterKgCmd = new Npgsql.NpgsqlCommand(@"
                        ALTER TABLE ""ConcreteBatches""
                        ADD COLUMN IF NOT EXISTS ""TotalWaterKg"" double precision NOT NULL DEFAULT 0;
                    ", connection);
                    await addTotalWaterKgCmd.ExecuteNonQueryAsync();
                    DetailedLogger.LogInfo("✅ TotalWaterKg kolonu kontrol edildi/eklendi");
                }
                catch (Exception ex)
                {
                    DetailedLogger.LogError($"TotalWaterKg kolonu eklenirken hata: {ex.Message}");
                }
                
                // ConcreteBatch2s tablosunun sütunlarını kontrol et
                var checkColumnsCommand = new Npgsql.NpgsqlCommand(@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ConcreteBatch2s' 
                    AND column_name IN ('LoadcellWater1Kg', 'LoadcellWater2Kg', 'PulseWater1Kg', 'PulseWater2Kg', 'LoadcellWaterKg', 'PulseWaterKg')
                    ORDER BY column_name;", connection);

                var existingColumns = new List<string>();
                using (var reader = await checkColumnsCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingColumns.Add(reader.GetString(0));
                    }
                }

                DetailedLogger.LogInfo($"Mevcut su sütunları: {string.Join(", ", existingColumns)}");

                // Eğer eski sütunlar varsa ve yeni sütunlar yoksa güncelle
                if (existingColumns.Contains("LoadcellWater1Kg") && !existingColumns.Contains("LoadcellWaterKg"))
                {
                    DetailedLogger.LogInfo("Su alanları güncelleniyor...");

                    // Yeni alanları ekle
                    var addColumnsCommand = new Npgsql.NpgsqlCommand(@"
                        ALTER TABLE ""ConcreteBatch2s"" 
                        ADD COLUMN ""LoadcellWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ADD COLUMN ""PulseWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0;", connection);
                    await addColumnsCommand.ExecuteNonQueryAsync();
                    DetailedLogger.LogInfo("Yeni su alanları eklendi.");

                    // Mevcut verileri yeni alanlara kopyala
                    var copyDataCommand = new Npgsql.NpgsqlCommand(@"
                        UPDATE ""ConcreteBatch2s"" 
                        SET ""LoadcellWaterKg"" = ""LoadcellWater1Kg"",
                            ""PulseWaterKg"" = ""PulseWater1Kg""
                        WHERE ""LoadcellWater1Kg"" IS NOT NULL OR ""PulseWater1Kg"" IS NOT NULL;", connection);
                    await copyDataCommand.ExecuteNonQueryAsync();
                    DetailedLogger.LogInfo("Veriler yeni alanlara kopyalandı.");

                    // Eski alanları sil
                    var dropColumnsCommand = new Npgsql.NpgsqlCommand(@"
                        ALTER TABLE ""ConcreteBatch2s"" 
                        DROP COLUMN ""LoadcellWater1Kg"",
                        DROP COLUMN ""LoadcellWater2Kg"",
                        DROP COLUMN ""PulseWater1Kg"",
                        DROP COLUMN ""PulseWater2Kg"";", connection);
                    await dropColumnsCommand.ExecuteNonQueryAsync();
                    DetailedLogger.LogInfo("Eski su alanları silindi.");

                    // EffectiveWaterKg'yi güncelle
                    var updateEffectiveCommand = new Npgsql.NpgsqlCommand(@"
                        UPDATE ""ConcreteBatch2s"" 
                        SET ""EffectiveWaterKg"" = ""LoadcellWaterKg"" + ""PulseWaterKg"";", connection);
                    await updateEffectiveCommand.ExecuteNonQueryAsync();
                    DetailedLogger.LogInfo("EffectiveWaterKg güncellendi.");

                    DetailedLogger.LogInfo("✅ Su alanları başarıyla güncellendi!");
                }
                else if (existingColumns.Contains("LoadcellWaterKg"))
                {
                    DetailedLogger.LogInfo("✅ Su alanları zaten güncel.");
                }
                else
                {
                    DetailedLogger.LogInfo("❌ ConcreteBatch2s tablosu bulunamadı veya beklenmeyen durum.");
                }
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("Veritabanı şeması güncelleme hatası", ex);
                Console.WriteLine($"[MainWindow] Veritabanı şeması güncelleme hatası: {ex.Message}");
            }
        }

        private void CreateProductionNotesTableIfNotExists()
        {
            try
            {
                // ProductionNotes tablosunun var olup olmadığını kontrol et
                var connection = _context.Database.GetDbConnection();
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""ProductionNotes"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""ShiftId"" INTEGER NOT NULL,
                        ""Note"" TEXT NOT NULL DEFAULT '',
                        ""FireProductCount"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""CreatedBy"" TEXT NOT NULL DEFAULT ''
                    );
                ";
                
                command.ExecuteNonQuery();
                connection.Close();
                
                AddLog("✅ ProductionNotes tablosu oluşturuldu/kontrol edildi");
            }
            catch (Exception ex)
            {
                AddLog($"ProductionNotes tablosu oluşturma hatası: {ex.Message}");
            }
        }
        
        private void AddFireProductCountColumnIfNotExists()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ShiftRecords' 
                            AND column_name = 'FireProductCount'
                        ) THEN
                            ALTER TABLE ""ShiftRecords"" ADD COLUMN ""FireProductCount"" INTEGER NOT NULL DEFAULT 0;
                        END IF;
                    END $$;
                ";
                
                command.ExecuteNonQuery();
                connection.Close();
                
                AddLog("✅ ShiftRecord tablosuna FireProductCount sütunu eklendi/kontrol edildi");
            }
            catch (Exception ex)
            {
                AddLog($"FireProductCount sütunu ekleme hatası: {ex.Message}");
            }
        }

        private async Task<int> CreateShiftRecord()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var shift = new Shift
                    {
                        Name = $"Vardiya - {_shiftStartTime.Value:dd.MM.yyyy HH:mm}",
                        OperatorName = _currentOperatorName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    context.Shifts.Add(shift);
                    await context.SaveChangesAsync();
                    
                    AddLog($"✅ Vardiya kaydı oluşturuldu - ID: {shift.Id}");
                    return shift.Id;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Vardiya kaydı oluşturma hatası: {ex.Message}");
                return 0;
            }
        }

        private async Task<List<ProductionNote>> GetShiftNotes(int shiftId)
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    return await context.ProductionNotes
                        .Where(n => n.ShiftId == shiftId)
                        .OrderBy(n => n.CreatedAt)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                AddLog($"Shift notes retrieval error: {ex.Message}");
                return new List<ProductionNote>();
            }
        }

        private async Task DeleteShiftNotes(int shiftId)
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var notes = await context.ProductionNotes
                        .Where(n => n.ShiftId == shiftId)
                        .ToListAsync();
                    
                    if (notes.Any())
                    {
                        context.ProductionNotes.RemoveRange(notes);
                        await context.SaveChangesAsync();
                        AddLog($"✅ {notes.Count} adet vardiya notu silindi");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Shift notes deletion error: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            try
            {
                _shiftRecordService = new ShiftRecordService(_context);
                _operatorService = new OperatorService(_context);
                _moldService = new MoldService(_context);
                _shiftMoldTrackingService = new ShiftMoldTrackingService(_context);
                _activeShiftService = new ActiveShiftService(_context);
            }
            catch (Exception ex)
            {
                AddLog($"Servis başlatma hatası: {ex.Message}");
            }
        }

        private void InitializeLocalization()
        {
            try
            {
                // Program varsayılan olarak İngilizce başlatılır
                _localizationService.ChangeLanguage("en-US");
                UpdateUI();
            }
            catch (Exception ex)
            {
                AddLog($"Language loading error: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            try
            {
                // Başlık
                TitleText.Text = _localizationService.GetString("MainWindow.Title", "Üretim Takip Sistemi");
                
                // Sekme başlıkları
                ProductionTab.Header = _localizationService.GetString("ProductionReporting.Title", "Üretim Takip");
                ConcreteTab.Header = _localizationService.GetString("ConcreteReporting.Title", "Beton Santrali Raporlama");
                
                // Üretim takip sekmesi metinleri
                UpdateProductionTabTexts();
                
                // Beton santrali sekmesi metinleri
                UpdateConcreteTabTexts();
                
                // Vardiya yönetimi
                ShiftManagementText.Text = _localizationService.GetString("ProductionReporting.ShiftManagement", "Vardiya Yönetimi");
                // PlcStatusText.Text // KALDIRILDI = _localizationService.GetString("ProductionReporting.PlcChecking", "PLC: Bağlantı Kontrol Ediliyor...");
                ToggleShiftButton.Content = _localizationService.GetString("ProductionReporting.StartShift", "Vardiyayı Başlat");
                OpenShiftHistoryButton.Content = _localizationService.GetString("ShiftHistory");
                
                
                // Tarih bilgileri
                ShiftStartLabel.Text = _localizationService.GetString("ShiftStart");
                ProductionStartLabel.Text = _localizationService.GetString("ProductionStart");
                
                // Üretim bilgileri
                DailyPalletText.Text = _localizationService.GetString("DailyPalletProduction");
                TotalProductionLabel.Text = _localizationService.GetString("TotalPalletProduction");
                ShiftProductionText.Text = _localizationService.GetString("ShiftProduction");
                NoProductionText.Text = _localizationService.GetString("NoProductionYet");
                
                // ⚡ Log alanı kaldırıldı - performans için
                // LogsLabel.Text = _localizationService.GetString("SystemLogs");
                // CopyLogButton.Content = _localizationService.GetString("Copy");
                
                // Kalıp yönetimi
                MoldManagementTitle.Text = _localizationService.GetString("MoldManagement");
                // AddNewMoldText.Text // KALDIRILDI = _localizationService.GetString("AddNewMold");
                // AddMoldButton.Content = _localizationService.GetString("AddMold"); // KALDIRILDI - Sadece + olmalı
                ExistingMoldsText.Text = _localizationService.GetString("ExistingMolds");
                
                // Beton santrali
                ConcreteInfoText.Text = _localizationService.GetString("ConcretePlantInfo");
            }
            catch (Exception ex)
            {
                AddLog($"UI güncelleme hatası: {ex.Message}");
            }
        }

        private void InitializeTimer()
        {
            _productionTimer = new DispatcherTimer();
            _productionTimer.Interval = TimeSpan.FromSeconds(2); // Sinyalleri kaçırmamak için 2 saniye
            _productionTimer.Tick += ProductionTimer_Tick;
            // Timer'ı başlatma - sadece vardiya başlatıldığında başlatılacak
            // _productionTimer.Start();
            
            // Log temizleme timer'ı
            _logCleanupTimer = new DispatcherTimer();
            _logCleanupTimer.Interval = TimeSpan.FromMinutes(5);
            _logCleanupTimer.Tick += LogCleanupTimer_Tick;
            _logCleanupTimer.Start();
            
            // Beton santrali sayfası güncelleme timer'ı
            _concretePageTimer = new DispatcherTimer();
            _concretePageTimer.Interval = TimeSpan.FromSeconds(10); // 10 saniyede bir güncelle
            _concretePageTimer.Tick += ConcretePageTimer_Tick;

            // Fire mal sayısı timer'ı
            _fireProductTimer = new DispatcherTimer();
            _fireProductTimer.Interval = TimeSpan.FromSeconds(2);
            _fireProductTimer.Tick += async (s, e) => await PollFireProductCount();

            // Boşta geçen süre timer'ı
            _idleTimeTimer = new DispatcherTimer();
            _idleTimeTimer.Interval = TimeSpan.FromSeconds(2);
            _idleTimeTimer.Tick += async (s, e) => await PollIdleTime();
        }

        private async void InitializePlcService()
        {
            try
            {
                _plcDataService = new PlcDataService();
                
                // Event handler'ları bağla
                _plcDataService.DataChanged += OnPlcDataChanged;
                _plcDataService.LogMessage += OnPlcLogMessage;
                
                // Servisi başlat
                var started = await _plcDataService.StartAsync();
                if (started)
                {
                    // PLC servisi başlatıldı (log kaldırıldı)
                }
                else
                {
                    // PLC servisi başlatılamadı (log kaldırıldı)
                }
            }
            catch (Exception ex)
            {
                // PLC servisi başlatma hatası (log kaldırıldı)
            }
        }

        private async void OnPlcDataChanged(object? sender, PlcDataChangedEventArgs e)
        {
            // ⚡ SADECE MIXER1 BATCH AÇMA ODAKLI
            // Gereksiz log'ları kaldırdık, sadece batch açma için kritik olanları gösteriyoruz
            
            // 🆕 BASİT MIXER1 SİSTEMİ
            await ProcessSimpleMixer1(e.CurrentData);
            
            // 🆕 MIXER2 SİSTEMİ - DEVRE DIŞI (Mixer2StatusBasedProcessor kullanılıyor)
            // await ProcessSimpleMixer2(e.CurrentData);
            
            // 🆕 MIXER2 STATUS-BASED PROCESSOR (YENİ SİSTEM)
            await ProcessMixer2WithStatusBasedProcessor(e.CurrentData);
        }

        // Mixer2StatusBasedProcessor için static instance
        private static Mixer2StatusBasedProcessor? _mixer2Processor;
        
        // Mixer1 için static değişkenler
        private static DateTime _lastMixer1BatchTime = DateTime.MinValue;
        private static HashSet<string> _lastActiveTartimOk = new HashSet<string>();
        
        // 🔥 MIXER1 ÜÇLÜ TAKİP SİSTEMİ - 3 farklı pozisyonda 3 farklı batch olabilir
        private static int? _tartimKovasiBatchId = null;      // Pozisyon 1: Tartım kovasında (yeni tartım yapılıyor)
        private static int? _beklemeBunkeriBatchId = null;    // Pozisyon 2: Bekleme bunkerinde (mixer'a girmek için bekliyor)
        private static int? _mixerdeBatchId = null;           // Pozisyon 3: Mixer'de (çimento, su, katkı ekleniyor)
        
        private static HashSet<string> _currentBatchAgregas = new HashSet<string>(); // Bu batch'te hangi agregalar var
        private static HashSet<string> _expectedAgregas = new HashSet<string>(); // Bu batch için beklenilen aktif agregalar
        
        // 🔥 MIXER2 DÖRTLÜ TAKİP SİSTEMİ - 4 farklı pozisyonda 4 farklı batch olabilir
        private static int? _m2YatayKovaBatchId = null;       // Pozisyon 1: Yatay kovada (agrega tartımı yapılıyor)
        private static int? _m2DikeyKovaBatchId = null;       // Pozisyon 2: Dikey kovada (yatay kovadan geçiyor)
        private static int? _m2BeklemeBunkeriBatchId = null;  // Pozisyon 3: Bekleme bunkerinde (mixer'a girmek için bekliyor)
        private static int? _m2MixerdeBatchId = null;         // Pozisyon 4: Mixer'de (çimento, su, katkı ekleniyor)
        
        private static HashSet<string> _m2CurrentBatchAgregas = new HashSet<string>(); // Mixer2 batch'te hangi agregalar var
        private static HashSet<string> _m2ExpectedAgregas = new HashSet<string>(); // Mixer2 batch için beklenilen aktif agregalar
        
        // Mixer1 katkı bekleyen batch listesi yönetimi
        private static HashSet<int> _m1WaitingForAdmixtureBatchIds = new HashSet<int>();
        private static HashSet<int> _m1AdmixtureRecordedBatchIds = new HashSet<int>();
        private static Dictionary<int, DateTime> _m1AdmixtureRecordTimes = new Dictionary<int, DateTime>();
        
        // Mixer durumu takibi
        private static bool _lastMixerAgregaVar = false;
        private static bool _lastHarcHazir = false;
        
        // 🔥 ÇİMENTO TARTIM OK Sinyalleri (Agregalar gibi!)
        private static bool _lastCimento1TartimOk = false; // H62.7
        private static bool _lastCimento2TartimOk = false; // H63.7
        private static bool _lastCimento3TartimOk = false; // H64.7
        
        // 🔥 SU TARTIM OK Sinyalleri
        private static bool _lastSuLoadcellTartimOk = false; // H60.0
        
        // 🔥 MİXERDE SU VAR Sinyalleri (Pulse su için)
        private static bool _lastMixerSuVar = false; // H60.3
        
        // 🔥 KATKI TARTIM OK Sinyalleri
        private static bool _lastKatki1TartimOk = false; // H35.7
        private static bool _lastKatki2TartimOk = false; // H36.7
        private static bool _lastKatki3TartimOk = false; // H37.7
        private static bool _lastKatki4TartimOk = false; // H38.7
        
        // 🔥 MIXER1 CACHE Mekanizması - PLC sıfırlamadan önce değerleri kaydet
        private static Dictionary<string, double>? _pendingCimentoData = null;
        private static DateTime? _pendingCimentoTime = null;
        private static Dictionary<string, double>? _pendingSuData = null;
        private static DateTime? _pendingSuTime = null;
        
        // 🔥 MIXER2 CACHE Mekanizması - PLC sıfırlamadan önce değerleri kaydet
        private static Dictionary<string, double>? _pendingKatkiData = null;
        private static DateTime? _pendingKatkiTime = null;
        
        // 🔥 MIXER2 EDGE DETECTION - Sinyal yükselme algılama
        private static bool _lastM2YatayKovaVar = false;       // H71.7
        private static bool _lastM2DikeyKovaVar = false;       // H71.10
        private static bool _lastM2BeklemeVar = false;         // H71.11
        private static bool _lastM2MixerAgregaVar = false;     // H71.0
        private static bool _lastM2HarcHazir = false;          // H71.5
        
        // Mixer2 Su Tartım OK
        private static bool _lastM2SuLoadcellTartimOk = false; // H61.6
        
        // Mixer2 Pulse Su
        private static bool _lastM2PulseSuVar = false; // H71.4
        
        // Mixer2 Katkı Tartım OK
        private static bool _lastM2Katki1TartimOk = false;     // H39.3
        private static bool _lastM2Katki2TartimOk = false;     // H40.3
        private static bool _lastM2Katki3TartimOk = false;     // H41.3
        private static bool _lastM2Katki4TartimOk = false;     // H43.3
        
        // Mixer2 Katkı Su Tartım OK
        private static bool _lastM2Katki1SuTartimOk = false;   // H39.4
        private static bool _lastM2Katki2SuTartimOk = false;   // H40.4
        private static bool _lastM2Katki3SuTartimOk = false;   // H41.4
        private static bool _lastM2Katki4SuTartimOk = false;   // H43.4
        
        /// <summary>
        /// 🆕 BASİT MIXER1 SİSTEMİ - Tartım OK gelince batch aç
        /// </summary>
        private async Task ProcessSimpleMixer1(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                // 1️⃣ AKTİF agregaları tespit et
                var agregaActiveSignals = new[]
                {
                    ("H45.2", "Agrega1"), // Agrega1 Aktif
                    ("H46.2", "Agrega2"), // Agrega2 Aktif
                    ("H47.2", "Agrega3"), // Agrega3 Aktif
                    ("H48.2", "Agrega4"), // Agrega4 Aktif
                    ("H49.2", "Agrega5")  // Agrega5 Aktif
                };
                
                var activeAgregas = agregaActiveSignals
                    .Where(x => plcData.ContainsKey(x.Item1) && plcData[x.Item1].Value)
                    .Select(x => x.Item2)
                    .ToHashSet();
                
                // 2️⃣ Tartım OK sinyallerini kontrol et
                var tartimOkSignals = new[]
                {
                    ("H45.7", "Agrega1"), // Agrega1 Tartım OK
                    ("H46.7", "Agrega2"), // Agrega2 Tartım OK
                    ("H47.7", "Agrega3"), // Agrega3 Tartım OK
                    ("H48.7", "Agrega4"), // Agrega4 Tartım OK
                    ("H49.7", "Agrega5")  // Agrega5 Tartım OK
                };
                
                var activeTartimOk = tartimOkSignals
                    .Where(x => plcData.ContainsKey(x.Item1) && plcData[x.Item1].Value)
                    .Select(x => x.Item1)
                    .ToHashSet();
                
                var tartimOkAgregas = tartimOkSignals
                    .Where(x => activeTartimOk.Contains(x.Item1))
                    .Select(x => x.Item2)
                    .ToHashSet();
                
                if (activeTartimOk.Any())
                {
                    // YENİ sinyaller mi? (FALSE → TRUE geçişi)
                    var newSignals = activeTartimOk.Except(_lastActiveTartimOk).ToList();
                    
                    if (newSignals.Any())
                    {
                        // Yeni sinyal geldi!
                        var newAgregaNames = tartimOkSignals
                            .Where(x => newSignals.Contains(x.Item1))
                            .Select(x => x.Item2)
                            .ToList();
                        
                        AddLog($"🆕 Mixer1: YENİ TARTIM OK: {string.Join(", ", newAgregaNames)}");
                        
                        // AKTİF BATCH VAR MI? (Tartım kovasında)
                        if (_tartimKovasiBatchId.HasValue)
                        {
                            // Mevcut batch'e EKLE
                            await AddAgregasToCurrentBatch(newAgregaNames, plcData, activeAgregas);
                        }
                        else
                        {
                            // YENİ BATCH AÇ - COOLDOWN kontrolü (son 60 saniye içinde batch açılmış mı?)
                            var timeSinceLastBatch = DateTime.Now - _lastMixer1BatchTime;
                            if (timeSinceLastBatch.TotalSeconds < 60)
                            {
                                AddLog($"⏳ Mixer1: COOLDOWN - Son batch {timeSinceLastBatch.TotalSeconds:F1}sn önce açıldı, {60 - timeSinceLastBatch.TotalSeconds:F0}sn sonra yeni batch açılabilir");
                                return;
                            }
                            
                            // YENİ BATCH AÇ - Aktif agregaları kaydet
                            _expectedAgregas = new HashSet<string>(activeAgregas);
                            AddLog($"📋 Mixer1: Aktif Agregalar: {string.Join(", ", activeAgregas)} (Toplam: {activeAgregas.Count})");
                            
                            await CreateNewMixer1Batch(tartimOkAgregas.ToList(), plcData);
                            _lastMixer1BatchTime = DateTime.Now;
                        }
                    }
                }
                
                // Son durumu kaydet (sinyaller gitti mi kontrol et)
                var disappearedSignals = _lastActiveTartimOk.Except(activeTartimOk).ToList();
                if (disappearedSignals.Any())
                {
                    var disappearedNames = tartimOkSignals
                        .Where(x => disappearedSignals.Contains(x.Item1))
                        .Select(x => x.Item2)
                        .ToList();
                    AddLog($"🔴 Mixer1: Tartım OK sinyali düştü: {string.Join(", ", disappearedNames)}");
                    
                    // Tüm agregalar düştü mü? → Batch'i Bekleme Bunkerine taşı
                    if (!activeTartimOk.Any() && _tartimKovasiBatchId.HasValue)
                    {
                        await MoveToBeklemeBunkeri();
                    }
                }
                
                _lastActiveTartimOk = activeTartimOk;
                
                // 3️⃣ MIXER TAKİBİ - Bekleme bunkerinden sonraki aşamalar
                await ProcessMixerStages(plcData);
                
                // 4️⃣ MIXER1 PIGMENT VE KATKI KAYIT - Bekleme Bunkerindeki batch'lere pigment ekle
                if (_beklemeBunkeriBatchId.HasValue)
                {
                    await RecordMixer1PigmentData();
                }
                
                // Mixer'deki batch'lere katkı ekle - Mixer2 gibi bekleyen batch listesi yönetimi
                await CheckMixer1KatkiSignal(plcData);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                AddLog($"❌ ProcessSimpleMixer1 hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🆕 MIXER2 SİSTEMİ - 4 Pozisyonlu Takip
        /// Yatay Kova → Dikey Kova → Bekleme Bunkeri → Mixer
        /// </summary>
        private async Task ProcessSimpleMixer2(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                // Pozisyon sinyallerini oku
                bool yatayKovaVar = plcData.ContainsKey("H71.7") && plcData["H71.7"].Value;
                bool dikeyKovaVar = plcData.ContainsKey("H71.10") && plcData["H71.10"].Value;
                bool beklemeVar = plcData.ContainsKey("H71.11") && plcData["H71.11"].Value;
                bool mixerAgregaVar = plcData.ContainsKey("H71.0") && plcData["H71.0"].Value;
                bool harcHazir = plcData.ContainsKey("H71.5") && plcData["H71.5"].Value;
                
                // Debug: Pozisyon durumlarını logla
                AddLog($"🔍 Mixer2 Pozisyonlar: Yatay={yatayKovaVar}, Dikey={dikeyKovaVar}, Bekleme={beklemeVar}(H71.11), MixerAgrega={mixerAgregaVar}(H71.0), HarcHazir={harcHazir}");
                AddLog($"🔍 Mixer2 Batch ID'ler: Yatay={_m2YatayKovaBatchId}, Dikey={_m2DikeyKovaBatchId}, Bekleme={_m2BeklemeBunkeriBatchId}, Mixer={_m2MixerdeBatchId}");
                
                using var context = new ProductionDbContext();
                
                // POZİSYON 1: YATAY KOVA - Batch başlangıcı (Daha esnek koşul)
                if (yatayKovaVar && !_lastM2YatayKovaVar && !_m2YatayKovaBatchId.HasValue)
                {
                    await CreateMixer2Batch(plcData);
                }
                
                // POZİSYON 2: YATAY KOVA → DİKEY KOVA (Daha esnek koşul)
                if (!yatayKovaVar && dikeyKovaVar && _m2YatayKovaBatchId.HasValue)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2YatayKovaBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Dikey Kovada";
                        await context.SaveChangesAsync();
                        AddLog($"📦 Mixer2: Batch #{batch.Id} → DİKEY KOVADA");
                        _m2DikeyKovaBatchId = batch.Id;
                        _m2YatayKovaBatchId = null; // Yatay kova boş, yeni batch açılabilir
                    }
                }
                
                // POZİSYON 3: DİKEY KOVA → BEKLEME BUNKERİ (Daha esnek koşul)
                if (!dikeyKovaVar && beklemeVar && _m2DikeyKovaBatchId.HasValue)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2DikeyKovaBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Bekleme Bunkerinde";
                        await context.SaveChangesAsync();
                        AddLog($"📦 Mixer2: Batch #{batch.Id} → BEKLEME BUNKERİNDE");
                        _m2BeklemeBunkeriBatchId = batch.Id;
                        _m2DikeyKovaBatchId = null;
                    }
                }
                
                // POZİSYON 4: BEKLEME BUNKERİ → MIXER (Öncelik: H71.11 düşüşü, Alternatif: H71.0 yükselen kenar)
                if (_m2BeklemeBunkeriBatchId.HasValue && !beklemeVar && _lastM2BeklemeVar)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2BeklemeBunkeriBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Mixerde";
                        await context.SaveChangesAsync();
                        AddLog($"🔄 Mixer2: Batch #{batch.Id} → MIXER'A GİRDİ (H71.11 Pasif Oldu)");
                        _m2MixerdeBatchId = batch.Id;
                        _m2BeklemeBunkeriBatchId = null;
                    }
                }
                // Alternatif tetik: Mixer agrega sinyali yükselen kenarıyla geçişi yakala (H71.0)
                else if (_m2BeklemeBunkeriBatchId.HasValue && mixerAgregaVar && !_lastM2MixerAgregaVar)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2BeklemeBunkeriBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Mixerde";
                        await context.SaveChangesAsync();
                        AddLog($"🔄 Mixer2: Batch #{batch.Id} → MIXER'A GİRDİ (H71.0 Yükselen Kenar)");
                        _m2MixerdeBatchId = batch.Id;
                        _m2BeklemeBunkeriBatchId = null;
                    }
                }
                // Basit tetik: Bekleme bunkerinde batch varken mixer agrega aktifse geçiş yap
                else if (_m2BeklemeBunkeriBatchId.HasValue && mixerAgregaVar)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2BeklemeBunkeriBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Mixerde";
                        await context.SaveChangesAsync();
                        AddLog($"🔄 Mixer2: Batch #{batch.Id} → MIXER'A GİRDİ (H71.0 Basit Tetik)");
                        _m2MixerdeBatchId = batch.Id;
                        _m2BeklemeBunkeriBatchId = null;
                    }
                }
                else if (_m2BeklemeBunkeriBatchId.HasValue)
                {
                    // Debug: Bekleme bunkerinde batch var ama geçiş koşulları sağlanmamış
                    AddLog($"🔍 Mixer2 Debug: Bekleme bunkerinde Batch #{_m2BeklemeBunkeriBatchId} var ama geçiş koşulları:");
                    AddLog($"   🔍 H71.11 Bekleme={beklemeVar}, LastBekleme={_lastM2BeklemeVar}");
                    AddLog($"   🔍 H71.0 MixerAgrega={mixerAgregaVar}, LastMixerAgrega={_lastM2MixerAgregaVar}");
                }
                
                // MIXER'DEYKEN: Çimento, Su, Katkı Ekleme
                if (_m2MixerdeBatchId.HasValue)
                {
                    var mixerBatch = await context.ConcreteBatch2s.FindAsync(_m2MixerdeBatchId.Value);
                    if (mixerBatch != null)
                    {
                        await ProcessMixer2Ingredients(mixerBatch, context, plcData, harcHazir);
                    }
                }
                
                // HARÇ HAZIR → BATCH TAMAMLANDI
                if (harcHazir && !_lastM2HarcHazir && _m2MixerdeBatchId.HasValue)
                {
                    var batch = await context.ConcreteBatch2s.FindAsync(_m2MixerdeBatchId.Value);
                    if (batch != null)
                    {
                        batch.Status = "Tamamlandı";
                        batch.CompletedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                        AddLog($"✅ Mixer2: Batch #{batch.Id} → TAMAMLANDI! 🎉");
                        AddLog($"   📊 Toplam: Agrega={batch.TotalAggregateKg}kg, Çimento={batch.TotalCementKg}kg, Su={batch.TotalWaterKg}kg, Katkı={batch.TotalAdmixtureKg}kg");
                        _m2MixerdeBatchId = null;
                    }
                }
                
                // Edge detection güncelle
                _lastM2YatayKovaVar = yatayKovaVar;
                _lastM2DikeyKovaVar = dikeyKovaVar;
                _lastM2BeklemeVar = beklemeVar;
                _lastM2MixerAgregaVar = mixerAgregaVar;
                _lastM2HarcHazir = harcHazir;
            }
            catch (Exception ex)
            {
                AddLog($"❌ ProcessSimpleMixer2 hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🎯 MIXER AŞAMALARI TAKİBİ
        /// Bekleme bunkerindeki ve mixer'deki batch'leri takip eder
        /// </summary>
        private async Task ProcessMixerStages(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                // Mixer sinyallerini oku
                bool mixerAgregaVar = plcData.ContainsKey("H70.0") && plcData["H70.0"].Value;
                bool mixerCimentoVar = plcData.ContainsKey("H70.1") && plcData["H70.1"].Value;
                bool mixerKatkiVar = plcData.ContainsKey("H70.2") && plcData["H70.2"].Value;
                bool mixerLoadcellSuVar = plcData.ContainsKey("H60.3") && plcData["H60.3"].Value;
                bool mixerPulseSuVar = plcData.ContainsKey("H70.4") && plcData["H70.4"].Value;
                bool harcHazir = plcData.ContainsKey("H70.5") && plcData["H70.5"].Value;
                
                using var context = new ProductionDbContext();
                
                // 🔥 POZİSYON 2: BEKLEME BUNKERİ → MIXER'A GEÇİŞ
                if (_beklemeBunkeriBatchId.HasValue)
                {
                    var beklemeBatch = await context.ConcreteBatches.FindAsync(_beklemeBunkeriBatchId.Value);
                    if (beklemeBatch != null && beklemeBatch.Status == "Bekleme Bunkeri")
                    {
                        // Mixer'a giriş sinyali geldi mi?
                        if (mixerAgregaVar && !_lastMixerAgregaVar)
                        {
                            beklemeBatch.Status = "Mixerde";
                            await context.SaveChangesAsync();
                            AddLog($"🔄 Mixer1: Batch #{beklemeBatch.Id} → MIXER'A GİRDİ (Bekleme Bunkerinden)");
                            
                            // Pozisyon geçişi: Bekleme → Mixer
                            _mixerdeBatchId = beklemeBatch.Id;
                            _beklemeBunkeriBatchId = null;
                            
                            // 🔥 KRİTİK: YENİ BATCH MİXER'A GİRDİ - STATE'LERİ VE CACHE'LERİ SIFIRLA!
                            // Böylece çimento/su/katkı Tartım OK sinyalleri FALSE→TRUE geçişi yapabilir
                            _lastHarcHazir = false;
                            
                            // Çimento Tartım OK state'lerini sıfırla
                            _lastCimento1TartimOk = false;
                            _lastCimento2TartimOk = false;
                            _lastCimento3TartimOk = false;
                            
                            // Su state'lerini sıfırla
                            _lastSuLoadcellTartimOk = false;
                            _lastMixerSuVar = false;
                            
                            // Katkı Tartım OK state'lerini sıfırla
                            _lastKatki1TartimOk = false;
                            _lastKatki2TartimOk = false;
                            _lastKatki3TartimOk = false;
                            _lastKatki4TartimOk = false;
                            
                            // CACHE'leri temizle
                            _pendingCimentoData = null;
                            _pendingCimentoTime = null;
                            _pendingSuData = null;
                            _pendingSuTime = null;
                            _pendingKatkiData = null;
                            _pendingKatkiTime = null;
                            
                            
                            AddLog($"🔄 Mixer1: Batch #{beklemeBatch.Id} için TÜM Tartım OK state'leri ve CACHE'ler SIFIRLANDI (Edge detection hazır)");
                        }
                    }
                }
                
                // 🔥 POZİSYON 3: MIXER'DE - Çimento, Su, Katkı, Harç Hazır
                if (_mixerdeBatchId.HasValue)
                {
                    var mixerBatch = await context.ConcreteBatches.FindAsync(_mixerdeBatchId.Value);
                    if (mixerBatch != null)
                    {
                        if (mixerBatch.Status == "Mixerde")
                        {
                            // ⚠️ KRİTİK: Çimento kontrolü SADECE "Mixerde Agrega Var" sinyali TRUE ise!
                            // H70.0 = TRUE → Agrega mixer'de, şimdi çimento kontrol et
                            if (mixerAgregaVar)
                            {
                                // Çimento, su, katkı ve harç hazır sinyallerini takip et
                                await ProcessMixerIngredients(mixerBatch, context, plcData, mixerCimentoVar, mixerLoadcellSuVar, mixerPulseSuVar, mixerKatkiVar, harcHazir);
                            }
                        }
                        else if (mixerBatch.Status == "Harç Hazır")
                        {
                            // Harç hazır sinyali düştü mü? → Tamamlandı
                            if (!harcHazir && _lastHarcHazir)
                            {
                                mixerBatch.Status = "Tamamlandı";
                                mixerBatch.CompletedAt = DateTime.UtcNow;
                                await context.SaveChangesAsync();
                                AddLog($"🚚 Mixer1: Batch #{mixerBatch.Id} → TAMAMLANDI (Kamyona Yükleniyor)");
                                
                                // Mixer pozisyonunu temizle
                                _mixerdeBatchId = null;
                                
                                // Mixer durumlarını sıfırla
                                _lastMixerAgregaVar = false;
                                
                                // Tartım OK state'lerini sıfırla
                                _lastCimento1TartimOk = false;
                                _lastCimento2TartimOk = false;
                                _lastCimento3TartimOk = false;
                                _lastSuLoadcellTartimOk = false;
                                _lastMixerSuVar = false;
                                _lastKatki1TartimOk = false;
                                _lastKatki2TartimOk = false;
                                _lastKatki3TartimOk = false;
                                _lastKatki4TartimOk = false;
                            }
                        }
                    }
                }
                
                // Son durumu kaydet
                _lastMixerAgregaVar = mixerAgregaVar;
                _lastHarcHazir = harcHazir;
            }
            catch (Exception ex)
            {
                AddLog($"❌ ProcessMixerStages hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Mixer'deki malzeme ekleme işlemlerini takip et
        /// </summary>
        private async Task ProcessMixerIngredients(ConcreteBatch batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData, 
            bool mixerCimentoVar, bool mixerLoadcellSuVar, bool mixerPulseSuVar, bool mixerKatkiVar, bool harcHazir)
        {
            // 🔸 ÇİMENTO TARTIM OK SİNYALLERİNİ TAKİP ET (Aktif çimento kontrolü ile!)
            bool cimento1Aktif = plcData.ContainsKey("H62.2") && plcData["H62.2"].Value;
            bool cimento2Aktif = plcData.ContainsKey("H63.2") && plcData["H63.2"].Value;
            bool cimento3Aktif = plcData.ContainsKey("H64.2") && plcData["H64.2"].Value;
            
            bool cimento1TartimOk = plcData.ContainsKey("H62.7") && plcData["H62.7"].Value;
            bool cimento2TartimOk = plcData.ContainsKey("H63.7") && plcData["H63.7"].Value;
            bool cimento3TartimOk = plcData.ContainsKey("H64.7") && plcData["H64.7"].Value;
            
            // Herhangi bir çimento Tartım OK sinyali yükseldi mi? (FALSE → TRUE) VE AKTİF ÇİMENTO VAR MI?
            bool yeniCimentoTartimOk = ((cimento1TartimOk && !_lastCimento1TartimOk) && cimento1Aktif) ||
                                       ((cimento2TartimOk && !_lastCimento2TartimOk) && cimento2Aktif) ||
                                       ((cimento3TartimOk && !_lastCimento3TartimOk) && cimento3Aktif);
            
            // ⚠️ KRİTİK: SADECE batch'e henüz çimento EKLENMEMİŞSE oku!
            // Çimento bir kez eklendikten sonra, yeni Tartım OK'lar bu batch'e tekrar yazılmamalı
            bool cimentoHenuzEklenmedi = batch.TotalCementKg == 0;
            
            if (yeniCimentoTartimOk && _pendingCimentoData == null && cimentoHenuzEklenmedi)
            {
                AddLog($"🔍 ÇİMENTO TARTIM OK algılandı! (Batch #{batch.Id}, Mevcut Çimento={batch.TotalCementKg}kg)");
                // ⚡ ÇİMENTO TARTIM OK! HEMEN oku ve CACHE'e al! (Sadece aktif çimentoları)
                var cimentoNames = new List<string>();
                if ((cimento1TartimOk && !_lastCimento1TartimOk) && cimento1Aktif) cimentoNames.Add("Çimento1");
                if ((cimento2TartimOk && !_lastCimento2TartimOk) && cimento2Aktif) cimentoNames.Add("Çimento2");
                if ((cimento3TartimOk && !_lastCimento3TartimOk) && cimento3Aktif) cimentoNames.Add("Çimento3");
                
                _pendingCimentoData = ReadCimentoValues(plcData);
                _pendingCimentoTime = DateTime.Now;
                
                AddLog($"🔍 ÇİMENTO TARTIM OK: {string.Join(", ", cimentoNames)}");
                if (_pendingCimentoData.Any())
                {
                    AddLog($"   📦 {string.Join(", ", _pendingCimentoData.Select(x => $"{x.Key}={x.Value}kg"))} (Toplam: {_pendingCimentoData.Values.Sum()}kg)");
                    AddLog($"   ⏳ 2 saniye sonra batch'e yazılacak...");
                }
            }
            
            // ⚠️ Eğer çimento Tartım OK geldi ama batch'e zaten eklenmişse LOGLA!
            if (yeniCimentoTartimOk && !cimentoHenuzEklenmedi)
            {
                AddLog($"⚠️ ÇİMENTO TARTIM OK geldi ama Batch #{batch.Id} zaten çimento var ({batch.TotalCementKg}kg) - ATLA!");
            }
            
            // Son çimento durumlarını kaydet
            _lastCimento1TartimOk = cimento1TartimOk;
            _lastCimento2TartimOk = cimento2TartimOk;
            _lastCimento3TartimOk = cimento3TartimOk;
            
            // 🔸 CACHE'teki çimentoyu 2 saniye sonra batch'e yaz
            if (_pendingCimentoData != null && _pendingCimentoTime != null)
            {
                var elapsed = (DateTime.Now - _pendingCimentoTime.Value).TotalSeconds;
                if (elapsed >= 2.0)
                {
                    var cimentoData = _pendingCimentoData;
                    if (cimentoData.Any() && cimentoData.Values.Sum() > 0)
                    {
                        batch.TotalCementKg = cimentoData.Sum(x => x.Value);
                        
                        // Alt tablolara çimento satırları ekle
                        short slot = 1;
                        foreach (var cement in cimentoData)
                        {
                            var cementRecord = new ConcreteBatchCement
                            {
                                BatchId = batch.Id,
                                Slot = slot++,
                                CementType = cement.Key,
                                WeightKg = cement.Value
                            };
                            context.ConcreteBatchCements.Add(cementRecord);
                        }
                        
                        var payload = System.Text.Json.JsonDocument.Parse(batch.RawPayloadJson ?? "{}");
                        var root = payload.RootElement;
                        var newPayload = new
                        {
                            agregalar = root.TryGetProperty("agregalar", out var agr) ? agr.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            cimento = cimentoData.Select(x => $"{x.Key}={x.Value}kg").ToArray(),
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        batch.RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(newPayload);
                        await context.SaveChangesAsync();
                        
                        // Çimento tüketimini silo sistemine işle
                        try
                        {
                            var consumptionService = new CementConsumptionService(context);
                            await consumptionService.RecordMixer1ConsumptionAsync(batch);
                        }
                        catch (Exception ex)
                        {
                            AddLog($"❌ Mixer1 çimento tüketimi işlenemedi: {ex.Message}");
                        }
                        
                        AddLog($"🔸 Mixer1: Batch #{batch.Id} → ÇİMENTO EKLENDİ (CACHE'ten)");
                        AddLog($"   📦 {string.Join(", ", cimentoData.Select(x => $"{x.Key}={x.Value}kg"))}, Toplam: {batch.TotalCementKg}kg");
                    }
                    
                    // Cache'i temizle
                    _pendingCimentoData = null;
                    _pendingCimentoTime = null;
                }
            }
            
            // 💧 SU SİSTEMİ - 2 AYRI SİSTEM
            
            // ⚠️ KRİTİK: SADECE batch'e henüz su EKLENMEMİŞSE oku!
            bool suHenuzEklenmedi = batch.TotalWaterKg == 0;
            
            // 1️⃣ LOADCELL SU: Status "Mixerde" iken, Loadcell Su Aktif VE Tartım OK
            bool loadcellSuAktif = plcData.ContainsKey("H60.0") && plcData["H60.0"].Value; // Mixer1 Loadcell Su Aktif
            bool suLoadcellTartimOk = plcData.ContainsKey("H60.6") && plcData["H60.6"].Value; // Mixer1 Loadcell Su Tartım OK
            
            // Debug: Su durumunu logla (sadece su işleme sırasında)
            if (loadcellSuAktif || suLoadcellTartimOk)
            {
                AddLog($"🔍 Mixer1 Su Debug: Batch #{batch.Id}, Mevcut Su={batch.TotalWaterKg}kg, SuHenuzEklenmedi={suHenuzEklenmedi}");
            }
            
            if (loadcellSuAktif && suLoadcellTartimOk && !_lastSuLoadcellTartimOk && _pendingSuData == null && suHenuzEklenmedi)
            {
                AddLog($"🔍 MIXER1 LOADCELL SU algılandı! (Batch #{batch.Id}, H60.0=TRUE, H60.6=TRUE, Mevcut Su={batch.TotalWaterKg}kg)");
                
                var suData = new Dictionary<string, double>();
                if (plcData.ContainsKey(REG_M1_SU_LOADCELL_KG))
                {
                    double loadcellRaw = plcData[REG_M1_SU_LOADCELL_KG].NumericValue;
                    // ⚠️ KRİTİK: Loadcell değerini 10.0'a böl!
                    double loadcellKg = loadcellRaw / 10.0;
                    if (loadcellKg > 0)
                    {
                        suData["SuLoadcell"] = loadcellKg;
                        AddLog($"   📊 DM204 Raw={loadcellRaw} → Loadcell={loadcellKg}kg (÷10.0)");
                    }
                }
                
                if (suData.Any())
                {
                    _pendingSuData = suData;
                    _pendingSuTime = DateTime.Now;
                    
                    AddLog($"🔍 SU LOADCELL TARTIM OK!");
                    AddLog($"   📦 {string.Join(", ", suData.Select(x => $"{x.Key}={x.Value}kg"))} (Toplam: {suData.Values.Sum()}kg)");
                    AddLog($"   ⏳ 2 saniye sonra batch'e yazılacak...");
                }
            }
            
            // ⚠️ Eğer su sinyali geldi ama batch'e zaten eklenmişse LOGLA!
            if (suLoadcellTartimOk && !_lastSuLoadcellTartimOk && !suHenuzEklenmedi)
            {
                AddLog($"⚠️ SU LOADCELL TARTIM OK geldi ama Batch #{batch.Id} zaten su var ({batch.TotalWaterKg}kg) - ATLA!");
            }
            
            _lastSuLoadcellTartimOk = suLoadcellTartimOk;
            
            // 2️⃣ PULSE SU: Harç hazır sinyali (H70.5) geldiğinde DM210 kg değerini oku
            // Pulse su sadece harç hazır olduğunda eklenir
            // Bu kontrol ProcessMixerIngredients fonksiyonunun sonunda yapılacak
            
            // 💧 CACHE'teki suyu 2 saniye sonra batch'e yaz
            if (_pendingSuData != null && _pendingSuTime != null)
            {
                var elapsed = (DateTime.Now - _pendingSuTime.Value).TotalSeconds;
                if (elapsed >= 2.0)
                {
                    var suData = _pendingSuData;
                    if (suData.Any() && suData.Values.Sum() > 0)
                    {
                        // Su verilerini LoadcellWaterKg ve PulseWaterKg'ye ata
                        if (suData.ContainsKey("SuLoadcell"))
                        {
                            batch.LoadcellWaterKg = suData["SuLoadcell"];
                        }
                        
                        var payload = System.Text.Json.JsonDocument.Parse(batch.RawPayloadJson ?? "{}");
                        var root = payload.RootElement;
                        var newPayload = new
                        {
                            agregalar = root.TryGetProperty("agregalar", out var agr) ? agr.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            cimento = root.TryGetProperty("cimento", out var cem) ? cem.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            su = suData.Select(x => $"{x.Key}={x.Value}kg").ToArray(),
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        batch.RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(newPayload);
                        await context.SaveChangesAsync();
                        
                        AddLog($"💧 Mixer1: Batch #{batch.Id} → SU EKLENDİ (CACHE'ten)");
                        AddLog($"   📦 {string.Join(", ", suData.Select(x => $"{x.Key}={x.Value}kg"))}, Toplam: {batch.TotalWaterKg}kg");
                    }
                    
                    // Cache'i temizle
                    _pendingSuData = null;
                    _pendingSuTime = null;
                }
            }
            
            // 🧪 KATKI - ŞİMDİLİK BASİT (Çimento çalışırsa aynı mantığı ekleriz)
            // TODO: Katkı için de Tartım OK sinyallerini ekle (H35.7, H36.7, H37.7, H38.7)
            
            // 🧪 CACHE'teki katkıyı 2 saniye sonra batch'e yaz
            if (_pendingKatkiData != null && _pendingKatkiTime != null)
            {
                var elapsed = (DateTime.Now - _pendingKatkiTime.Value).TotalSeconds;
                if (elapsed >= 2.0)
                {
                    var katkiData = _pendingKatkiData;
                    if (katkiData.Any() && katkiData.Values.Sum() > 0)
                    {
                        batch.TotalAdmixtureKg = katkiData.Sum(x => x.Value);
                        
                        var payload = System.Text.Json.JsonDocument.Parse(batch.RawPayloadJson ?? "{}");
                        var root = payload.RootElement;
                        var newPayload = new
                        {
                            agregalar = root.TryGetProperty("agregalar", out var agr) ? agr.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            cimento = root.TryGetProperty("cimento", out var cem) ? cem.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            su = root.TryGetProperty("su", out var su) ? su.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[] { },
                            katki = katkiData.Select(x => $"{x.Key}={x.Value}kg").ToArray(),
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        batch.RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(newPayload);
                        await context.SaveChangesAsync();
                        
                        AddLog($"🧪 Mixer1: Batch #{batch.Id} → KATKI EKLENDİ (CACHE'ten)");
                        AddLog($"   📦 {string.Join(", ", katkiData.Select(x => $"{x.Key}={x.Value}kg"))}, Toplam: {batch.TotalAdmixtureKg}kg");
                    }
                    
                    // Cache'i temizle
                    _pendingKatkiData = null;
                    _pendingKatkiTime = null;
                }
            }
            
            // ✅ HARÇ HAZIR (H70.5)
            if (harcHazir && !_lastHarcHazir)
            {
                // 💧 PULSE SU: Harç hazır olduğunda DM210 kg değerini oku
                // TotalWaterKg artık salt okunur property - LoadcellWaterKg ve PulseWaterKg'ye atama yapılmalı
                if (batch.LoadcellWaterKg == 0 && batch.PulseWaterKg == 0 && plcData.ContainsKey(REG_M1_SU_PULSE_KG))
                {
                    double pulseKg = plcData[REG_M1_SU_PULSE_KG].NumericValue;
                    if (pulseKg > 0 && pulseKg != 65535)
                    {
                        batch.PulseWaterKg = pulseKg;
                        await context.SaveChangesAsync();
                        AddLog($"💧 Mixer1: Batch #{batch.Id} → PULSE SU EKLENDİ: {pulseKg}kg (Harç Hazır)");
                    }
                }
                
                // 🌡️ NEM değerini oku (DM120)
                if (plcData.ContainsKey(REG_M1_NEM))
                {
                    var nemPercent = plcData[REG_M1_NEM].NumericValue;
                    if (nemPercent > 0 && nemPercent != 65535)
                    {
                        batch.MoisturePercent = nemPercent;
                        await context.SaveChangesAsync();
                        AddLog($"🌡️ Mixer1: Batch #{batch.Id} → NEM EKLENDİ: {nemPercent}%");
                    }
                }
                
                batch.Status = "Harç Hazır";
                await context.SaveChangesAsync();
                AddLog($"✅ Mixer1: Batch #{batch.Id} → HARÇ HAZIR! 🎉");
                AddLog($"   📊 Toplam: Agrega={batch.TotalAggregateKg}kg, Çimento={batch.TotalCementKg}kg, Su={batch.TotalWaterKg}kg, Katkı={batch.TotalAdmixtureKg}kg");
                if (batch.MoisturePercent.HasValue)
                {
                    AddLog($"   🌡️ Nem: {batch.MoisturePercent.Value}%");
                }
            }
        }
        
        /// <summary>
        /// 🆕 MIXER2 MALZEME İŞLEME - Çimento, Su, Katkı Ekleme
        /// Her malzeme SADECE 1 kere kaydedilir!
        /// </summary>
        private async Task ProcessMixer2Ingredients(ConcreteBatch2 batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData, bool harcHazir)
        {
            // ÇİMENTO İŞLEME - Mixer2StatusBasedProcessor tarafından yapılıyor, burada devre dışı
            // await ProcessMixer2Cimento(batch, context, plcData);
            
            // SU İŞLEME - Loadcell Tartım OK ile
            await ProcessMixer2LoadcellSu(batch, context, plcData);
            
            // KATKI İŞLEME - Katkı ve Su Tartım OK sinyalleriyle
            await ProcessMixer2Katki(batch, context, plcData);
            
            // PULSE SU İŞLEME - Harç hazır sinyali ile
            await ProcessMixer2PulseSu(batch, context, plcData, harcHazir);
            
            // HARÇ HAZIR - Nem Ekleme
            if (harcHazir && !_lastM2HarcHazir)
            {
                
                // Nem Ekleme
                if (plcData.ContainsKey(REG_M2_NEM))
                {
                    double nemPercent = plcData[REG_M2_NEM].NumericValue;
                    if (nemPercent > 0 && nemPercent != 65535)
                    {
                        batch.MoisturePercent = nemPercent;
                        await context.SaveChangesAsync();
                        AddLog($"🌡️ Mixer2: Batch #{batch.Id} → NEM EKLENDİ: {nemPercent}%");
                    }
                }
            }
        }
        
        /// <summary>
        /// MIXER2 ÇİMENTO İŞLEME - Basit algoritma: Mixerde + Aktif + TartımOK = Kaydet
        /// </summary>
        private async Task ProcessMixer2Cimento(ConcreteBatch2 batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData)
        {
            // Sadece Mixerdeyken kaydet
            if (!string.Equals(batch.Status, "Mixerde", StringComparison.OrdinalIgnoreCase))
                return;
                
            // ⚠️ KRİTİK: SADECE batch'e henüz çimento EKLENMEMİŞSE kaydet!
            if (batch.TotalCementKg > 0)
                return;
                
            // 🔍 DEBUG: Mixerde batch var, çimento sinyallerini kontrol et
            DetailedLogger.LogInfo($"🔍 Mixer2 Çimento Debug: Batch #{batch.Id} Mixerde, Mevcut Çimento={batch.TotalCementKg}kg");
                
            // Çimento Aktif ve Tartım OK sinyallerini kontrol et
            bool cimento1Aktif = plcData.ContainsKey("H65.2") && plcData["H65.2"].Value;
            bool cimento2Aktif = plcData.ContainsKey("H66.2") && plcData["H66.2"].Value;
            bool cimento3Aktif = plcData.ContainsKey("H67.2") && plcData["H67.2"].Value;
            
            // Tartım OK sinyalleri
            bool cimento1TartimOk = plcData.ContainsKey("H65.7") && plcData["H65.7"].Value;
            bool cimento2TartimOk = plcData.ContainsKey("H66.7") && plcData["H66.7"].Value;
            bool cimento3TartimOk = plcData.ContainsKey("H67.7") && plcData["H67.7"].Value;
            
            // 🔍 DEBUG: Çimento sinyallerini logla
            DetailedLogger.LogInfo($"   🔍 Aktif: H65.2={cimento1Aktif}, H66.2={cimento2Aktif}, H67.2={cimento3Aktif}");
            DetailedLogger.LogInfo($"   🔍 TartımOK: H65.7={cimento1TartimOk}, H66.7={cimento2TartimOk}, H67.7={cimento3TartimOk}");
            
            // BASİT ALGORİTMA: Herhangi bir çimento için Aktif VE TartımOK ise kaydet
            bool kaydetCimento1 = cimento1Aktif && cimento1TartimOk;
            bool kaydetCimento2 = cimento2Aktif && cimento2TartimOk;
            bool kaydetCimento3 = cimento3Aktif && cimento3TartimOk;
            
            bool kaydetCimento = kaydetCimento1 || kaydetCimento2 || kaydetCimento3;
            
            // 🔍 DEBUG: Kaydetme koşullarını logla
            DetailedLogger.LogInfo($"   🔍 Kaydet: Cimento1={kaydetCimento1}, Cimento2={kaydetCimento2}, Cimento3={kaydetCimento3}");
            DetailedLogger.LogInfo($"   🔍 ToplamKaydet={kaydetCimento}");
            
            if (kaydetCimento)
            {
                DetailedLogger.LogInfo($"🔍 Mixer2 ÇİMENTO KAYIT KOŞULLARI SAĞLANDI! (Batch #{batch.Id})");
                
                // Çimento değerlerini oku
                var cimentoValues = ReadM2CimentoValues(plcData);
                
                if (cimentoValues.Count > 0)
                {
                    // Çimento değerlerini batch'e ekle
                    double toplamCimento = cimentoValues.Values.Sum();
                    batch.TotalCementKg = toplamCimento;
                    
                    // ConcreteBatch2Cements tablosuna kaydet
                    short slot = 1;
                    foreach (var kvp in cimentoValues)
                    {
                        var cimentoKayit = new ConcreteBatch2Cement
                        {
                            BatchId = batch.Id,
                            Slot = slot++,
                            CementType = kvp.Key,
                            WeightKg = kvp.Value
                        };
                        context.ConcreteBatch2Cements.Add(cimentoKayit);
                    }
                    
                    await context.SaveChangesAsync();
                    
                    // Çimento tüketimini silo sistemine işle
                    try
                    {
                        var consumptionService = new CementConsumptionService(context);
                        await consumptionService.RecordMixer2ConsumptionAsync(batch);
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogInfo($"❌ Mixer2 çimento tüketimi işlenemedi: {ex.Message}");
                    }
                    
                    DetailedLogger.LogInfo($"🔸 Mixer2: Batch #{batch.Id} → ÇİMENTO EKLENDİ");
                    DetailedLogger.LogInfo($"   📦 {string.Join(", ", cimentoValues.Select(kvp => $"{kvp.Key}={kvp.Value}kg"))}, Toplam: {toplamCimento}kg");
                }
                else
                {
                    DetailedLogger.LogInfo($"⚠️ Mixer2: Çimento sinyalleri aktif ama KG değerleri okunamadı!");
                }
            }
        }
        
        /// <summary>
        /// MIXER2 LOADCELL SU İŞLEME
        /// </summary>
        private async Task ProcessMixer2LoadcellSu(ConcreteBatch2 batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData)
        {
            // Zaten loadcell su eklenmişse çık
            if (batch.LoadcellWaterKg > 0) return;
            
            // Mixer2 Loadcell Su: Status "Mixerde" iken, Loadcell Su Aktif VE Tartım OK
            bool loadcellSuAktif = plcData.ContainsKey("H61.0") && plcData["H61.0"].Value; // Mixer2 Loadcell Su Aktif
            bool suLoadcellTartimOk = plcData.ContainsKey("H61.6") && plcData["H61.6"].Value; // Mixer2 Loadcell Su Tartım OK
            
            // Edge detection - hem aktif hem tartım OK olmalı
            if (loadcellSuAktif && suLoadcellTartimOk && !_lastM2SuLoadcellTartimOk)
            {
                if (plcData.ContainsKey(REG_M2_SU_LOADCELL_KG))
                {
                    double suKg = plcData[REG_M2_SU_LOADCELL_KG].NumericValue;
                    if (suKg != 65535) // Sadece 65535 filtresi (kg > 0 kaldırıldı)
                    {
                        // DM304 için bölme işlemi gerekli (Mixer1 ile aynı)
                        double loadcellKg = suKg / 10.0;
                        batch.LoadcellWaterKg = loadcellKg;
                        await context.SaveChangesAsync();
                        DetailedLogger.LogInfo($"💧 Mixer2: Batch #{batch.Id} → LOADCELL SU EKLENDİ: {loadcellKg}kg (H61.0=TRUE, H61.6=TRUE, DM304={suKg}÷10.0)");
                    }
                }
            }
            
            _lastM2SuLoadcellTartimOk = suLoadcellTartimOk;
        }
        
        /// <summary>
        /// MIXER2 PULSE SU İŞLEME - Harç hazır sinyali ile çalışır
        /// </summary>
        private async Task ProcessMixer2PulseSu(ConcreteBatch2 batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData, bool harcHazir)
        {
            // Debug: Pulse su durumunu logla
            AddLog($"🔍 Mixer2 Pulse Su Debug: Batch #{batch.Id}, HarcHazir={harcHazir}, LastHarcHazir={_lastM2HarcHazir}, Mevcut Pulse Su={batch.PulseWaterKg}kg");
            
            // Zaten pulse su eklenmişse çık
            if (batch.PulseWaterKg > 0) 
            {
                AddLog($"⚠️ Mixer2: Batch #{batch.Id} zaten pulse su var ({batch.PulseWaterKg}kg) - Pulse su atlanıyor");
                return;
            }
            
            // Pulse Su: Harç hazır sinyali (H71.5) geldiğinde DM306 kg değerini oku
            if (harcHazir && !_lastM2HarcHazir)
            {
                AddLog($"🔍 Mixer2: Harç hazır sinyali algılandı! (H71.5=TRUE, Edge Detection)");
                
                if (plcData.ContainsKey(REG_M2_SU_PULSE_KG))
                {
                    double pulseKg = plcData[REG_M2_SU_PULSE_KG].NumericValue;
                    AddLog($"🔍 Mixer2: DM306 register değeri: {pulseKg}kg");
                    
                    if (pulseKg != 65535) // Sadece 65535 filtresi (kg > 0 kaldırıldı)
                    {
                        batch.PulseWaterKg = pulseKg;
                        await context.SaveChangesAsync();
                        AddLog($"💧 Mixer2: Batch #{batch.Id} → PULSE SU EKLENDİ: {pulseKg}kg (Harç Hazır)");
                    }
                    else
                    {
                        AddLog($"⚠️ Mixer2: DM306 değeri 65535 - Pulse su atlanıyor");
                    }
                }
                else
                {
                    AddLog($"⚠️ Mixer2: DM306 register bulunamadı - Pulse su atlanıyor");
                }
            }
            else if (harcHazir)
            {
                AddLog($"⚠️ Mixer2: Harç hazır sinyali TRUE ama edge detection yok (LastHarcHazir={_lastM2HarcHazir})");
            }
        }
        
        /// <summary>
        /// MIXER2 KATKI İŞLEME
        /// </summary>
        private async Task ProcessMixer2Katki(ConcreteBatch2 batch, ProductionDbContext context, Dictionary<string, PlcRegisterData> plcData)
        {
            // Zaten katkı eklenmişse çık
            if (batch.TotalAdmixtureKg > 0) return;
            
            var katkiList = new[]
            {
                ("Katkı1", "H39.3", "H39.4", REG_M2_KATKI1_CHEMICAL_KG, REG_M2_KATKI1_WATER_KG, _lastM2Katki1TartimOk, _lastM2Katki1SuTartimOk),
                ("Katkı2", "H40.3", "H40.4", REG_M2_KATKI2_CHEMICAL_KG, REG_M2_KATKI2_WATER_KG, _lastM2Katki2TartimOk, _lastM2Katki2SuTartimOk),
                ("Katkı3", "H41.3", "H41.4", REG_M2_KATKI3_CHEMICAL_KG, REG_M2_KATKI3_WATER_KG, _lastM2Katki3TartimOk, _lastM2Katki3SuTartimOk),
                ("Katkı4", "H43.3", "H43.4", REG_M2_KATKI4_CHEMICAL_KG, REG_M2_KATKI4_WATER_KG, _lastM2Katki4TartimOk, _lastM2Katki4SuTartimOk)
            };
            
            var katkiData = new Dictionary<string, double>();
            bool anyKatkiAdded = false;
            
            foreach (var (name, katkiTartimReg, suTartimReg, katkiKgReg, suKgReg, lastKatkiTartim, lastSuTartim) in katkiList)
            {
                bool katkiTartimOk = plcData.ContainsKey(katkiTartimReg) && plcData[katkiTartimReg].Value;
                bool suTartimOk = plcData.ContainsKey(suTartimReg) && plcData[suTartimReg].Value;
                
                // Edge detection - yeni tartım OK sinyali geldi mi?
                bool yeniKatki = katkiTartimOk && !lastKatkiTartim;
                bool yeniSu = suTartimOk && !lastSuTartim;
                
                if (yeniKatki && plcData.ContainsKey(katkiKgReg))
                {
                    double kg = plcData[katkiKgReg].NumericValue;
                    if (kg > 0 && kg != 65535)
                    {
                        katkiData[$"{name}_Chemical"] = kg;
                        anyKatkiAdded = true;
                    }
                }
                
                if (yeniSu && plcData.ContainsKey(suKgReg))
                {
                    double kg = plcData[suKgReg].NumericValue;
                    if (kg > 0 && kg != 65535)
                    {
                        katkiData[$"{name}_Water"] = kg;
                        anyKatkiAdded = true;
                    }
                }
            }
            
            if (anyKatkiAdded && katkiData.Any())
            {
                batch.TotalAdmixtureKg = katkiData.Values.Sum();
                await context.SaveChangesAsync();
                AddLog($"🧪 Mixer2: Batch #{batch.Id} → KATKI EKLENDİ");
                AddLog($"   📦 {string.Join(", ", katkiData.Select(x => $"{x.Key}={x.Value}kg"))}, Toplam: {batch.TotalAdmixtureKg}kg");
            }
            
            // Update edge detection variables
            _lastM2Katki1TartimOk = plcData.ContainsKey("H39.3") && plcData["H39.3"].Value;
            _lastM2Katki2TartimOk = plcData.ContainsKey("H40.3") && plcData["H40.3"].Value;
            _lastM2Katki3TartimOk = plcData.ContainsKey("H41.3") && plcData["H41.3"].Value;
            _lastM2Katki4TartimOk = plcData.ContainsKey("H43.3") && plcData["H43.3"].Value;
            
            _lastM2Katki1SuTartimOk = plcData.ContainsKey("H39.4") && plcData["H39.4"].Value;
            _lastM2Katki2SuTartimOk = plcData.ContainsKey("H40.4") && plcData["H40.4"].Value;
            _lastM2Katki3SuTartimOk = plcData.ContainsKey("H41.4") && plcData["H41.4"].Value;
            _lastM2Katki4SuTartimOk = plcData.ContainsKey("H43.4") && plcData["H43.4"].Value;
        }
        
        /// <summary>
        /// Mixer2 Çimento miktarlarını oku (Mixer içinde - aktif sinyal kontrolü YOK)
        /// </summary>
        private Dictionary<string, double> ReadM2CimentoValues(Dictionary<string, PlcRegisterData> plcData)
        {
            var cimentoRegisters = new[]
            {
                (REG_M2_CIMENTO1_KG, "Standard"),  // Çimento1 -> Standard
                (REG_M2_CIMENTO2_KG, "Beyaz"),     // Çimento2 -> Beyaz
                (REG_M2_CIMENTO3_KG, "Siyah")      // Çimento3 -> Siyah
            };
            
            var result = new Dictionary<string, double>();
            foreach (var (kgRegister, name) in cimentoRegisters)
            {
                if (plcData.ContainsKey(kgRegister))
                {
                    var kg = plcData[kgRegister].NumericValue;
                    // Ölçek kontrolü: çok büyük değer gelirse 10'a böl (gözlemsel koruma)
                    if (kg > 20000) kg = (ushort)(kg / 10.0);
                    if (kg > 0 && kg != 65535)  // ✅ SADECE 0'dan BÜYÜK ve geçerli değerleri kaydet
                    {
                        result[name] = kg;
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Çimento miktarlarını oku (Mixer içinde - aktif sinyal kontrolü YOK)
        /// H70.1 TRUE ise mixer'de çimento var demektir, direkt registerleri oku
        /// plcData key'leri FİZİKSEL ADRES (DM4404) olarak geliyor!
        /// </summary>
        private Dictionary<string, double> ReadCimentoValues(Dictionary<string, PlcRegisterData> plcData)
        {
            var cimentoRegisters = new[]
            {
                (REG_M1_CIMENTO1_KG, "Standard"),  // Çimento1 -> Standard
                (REG_M1_CIMENTO2_KG, "Beyaz"),     // Çimento2 -> Beyaz  
                (REG_M1_CIMENTO3_KG, "Siyah")      // Çimento3 -> Siyah
            };
            
            var result = new Dictionary<string, double>();
            foreach (var (kgRegister, name) in cimentoRegisters)
            {
                if (plcData.ContainsKey(kgRegister))
                {
                    var kg = plcData[kgRegister].NumericValue;
                    if (kg > 0)  // ✅ SADECE 0'dan BÜYÜK değerleri kaydet
                    {
                        result[name] = kg;
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Su miktarlarını oku (Mixer içinde - aktif sinyal kontrolü YOK)
        /// H70.3 veya H70.4 TRUE ise mixer'de su var demektir, direkt registerleri oku
        /// plcData key'leri FİZİKSEL ADRES (DM204) olarak geliyor!
        /// </summary>
        private Dictionary<string, double> ReadWaterValues(Dictionary<string, PlcRegisterData> plcData)
        {
            var suRegisters = new[]
            {
                (REG_M1_SU_LOADCELL_KG, "SuLoadcell"),
                (REG_M1_SU_PULSE_KG, "SuPulse")
            };
            
            var result = new Dictionary<string, double>();
            foreach (var (kgRegister, name) in suRegisters)
            {
                if (plcData.ContainsKey(kgRegister))
                {
                    var kg = plcData[kgRegister].NumericValue;
                    if (kg > 0)  // ✅ SADECE 0'dan BÜYÜK değerleri kaydet
                    {
                        result[name] = kg;
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Katkı miktarlarını oku (Mixer içinde - aktif sinyal kontrolü YOK)
        /// H70.2 TRUE ise mixer'de katkı var demektir, direkt registerleri oku
        /// plcData key'leri FİZİKSEL ADRES (DM4104) olarak geliyor!
        /// </summary>
        private Dictionary<string, double> ReadAdmixtureValues(Dictionary<string, PlcRegisterData> plcData)
        {
            var katkiRegisters = new[]
            {
                ("DM4104", "DM4105", "Katki1"),
                ("DM4114", "DM4115", "Katki2"),
                ("DM4124", "DM4125", "Katki3"),
                ("DM4134", "DM4135", "Katki4")
            };
            
            var result = new Dictionary<string, double>();
            foreach (var (kimyasalReg, suReg, name) in katkiRegisters)
            {
                double totalKg = 0;
                
                if (plcData.ContainsKey(kimyasalReg))
                {
                    totalKg += plcData[kimyasalReg].NumericValue;
                }
                
                if (plcData.ContainsKey(suReg))
                {
                    totalKg += plcData[suReg].NumericValue;
                }
                
                if (totalKg > 0)
                    result[name] = totalKg;
            }
            return result;
        }
        
        /// <summary>
        /// YENİ Mixer1 batch oluştur (İLK tartım OK) - Alias'larla
        /// </summary>
        private async Task CreateNewMixer1Batch(List<string> agregaNames, Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Alias'ları yükle
                var aggregateAliases = context.AggregateAliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                
                // Agrega kg değerlerini oku
                var (agregaDetails, totalAgregaKg) = GetAgregaValues(agregaNames, plcData);
                
                var batch = new ConcreteBatch
                {
                    OccurredAt = DateTime.UtcNow,
                    PlantCode = "MIXER1",
                    OperatorName = Environment.UserName,
                    RecipeCode = "AUTO",
                    IsSimulated = false,
                    Status = "Tartım Kovasında", // 🔥 Yeni durum
                    TotalAggregateKg = totalAgregaKg,
                    RawPayloadJson = $"{{\"agregalar\":[\"{string.Join("\",\"", agregaDetails)}\"],\"timestamp\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}",
                    CreatedAt = DateTime.UtcNow
                };
                
                context.ConcreteBatches.Add(batch);
                await context.SaveChangesAsync();
                
                // Alt tablolara agrega satırları ekle (alias isimleri ile)
                foreach (var agregaName in agregaNames)
                {
                    // Slot numarasını çıkar (Agrega1 -> 1, Agrega2 -> 2, vb.)
                    var slotNumber = ExtractSlotNumber(agregaName);
                    
                    // Alias varsa kullan, yoksa orijinal ismi kullan
                    var displayName = aggregateAliases.TryGetValue(slotNumber, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : agregaName;
                    
                    var agregaRecord = new ConcreteBatchAggregate
                    {
                        BatchId = batch.Id,
                        Slot = slotNumber, // GERÇEK SLOT NUMARASINI KULLAN (slot++ değil!)
                        Name = displayName, // Alias ismi kullan
                        WeightKg = GetAgregaValues(new List<string> { agregaName }, plcData).Item2
                    };
                    context.ConcreteBatchAggregates.Add(agregaRecord);
                }
                await context.SaveChangesAsync();
                
                // 🔥 POZİSYON 1: Tartım Kovasında
                _tartimKovasiBatchId = batch.Id;
                _currentBatchAgregas = new HashSet<string>(agregaNames);
                
                AddLog($"🎉 Mixer1: YENİ BATCH AÇILDI! ID={batch.Id}, Status=Tartım Kovasında");
                AddLog($"   📦 Agregalar: {string.Join(", ", agregaDetails)}, Toplam: {totalAgregaKg}kg");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 batch oluşturma hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Agrega isminden slot numarasını çıkar (Agrega1 -> 1, Agrega2 -> 2, vb.)
        /// </summary>
        private short ExtractSlotNumber(string agregaName)
        {
            if (string.IsNullOrWhiteSpace(agregaName))
                return 1;
                
            // "Agrega1", "Agrega2" gibi isimlerden slot numarasını çıkar
            if (agregaName.StartsWith("Agrega", StringComparison.OrdinalIgnoreCase))
            {
                var numberPart = agregaName.Substring(6); // "Agrega" kısmını atla
                if (short.TryParse(numberPart, out var slot))
                    return slot;
            }
            
            return 1; // Varsayılan
        }
        
        /// <summary>
        /// 🆕 MIXER2 BATCH OLUŞTUR - Yatay kovada agrega varken
        /// </summary>
        private async Task CreateMixer2Batch(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Aktif agregaları bul ve oku
                var agregaData = GetMixer2AgregaValues(plcData);
                if (agregaData.Count == 0)
                {
                    AddLog($"⚠️ Mixer2: Yatay kovada agrega var ama aktif agrega bulunamadı!");
                    return;
                }
                
                double totalAgregaKg = agregaData.Values.Sum();
                string agregaDetails = string.Join(", ", agregaData.Select(x => $"{x.Key}={x.Value}kg"));
                
                var batch = new ConcreteBatch2
                {
                    OccurredAt = DateTime.UtcNow,
                    PlantCode = "MIXER2",
                    OperatorName = Environment.UserName,
                    RecipeCode = "AUTO",
                    IsSimulated = false,
                    Status = "Yatay Kovada",
                    TotalAggregateKg = totalAgregaKg,
                    RawPayloadJson = $"{{\"agregalar\":[\"{string.Join("\",\"", agregaData.Select(x => $"{x.Key}={x.Value}kg"))}\"],\"timestamp\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}",
                    CreatedAt = DateTime.UtcNow
                };
                
                context.ConcreteBatch2s.Add(batch);
                await context.SaveChangesAsync();
                
                // Alias'ları yükle
                var aggregate2Aliases = context.Aggregate2Aliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                
                // Alt tablolara agrega satırları ekle (gerçek slot numaraları ile)
                foreach (var agrega in agregaData)
                {
                    // Slot numarasını çıkar (Agrega1 -> 1, Agrega2 -> 2, vb.)
                    var slotNumber = ExtractSlotNumber(agrega.Key);
                    
                    // Alias varsa kullan, yoksa orijinal ismi kullan
                    var displayName = aggregate2Aliases.TryGetValue(slotNumber, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : agrega.Key;
                    
                    var agregaRecord = new ConcreteBatch2Aggregate
                    {
                        BatchId = batch.Id,
                        Slot = slotNumber, // GERÇEK SLOT NUMARASINI KULLAN (slot++ değil!)
                        Name = displayName, // Alias ismi kullan
                        WeightKg = agrega.Value
                    };
                    context.ConcreteBatch2Aggregates.Add(agregaRecord);
                }
                await context.SaveChangesAsync();
                
                _m2YatayKovaBatchId = batch.Id;
                _m2CurrentBatchAgregas = new HashSet<string>(agregaData.Keys);
                
                AddLog($"🎉 Mixer2: YENİ BATCH AÇILDI! ID={batch.Id}, Status=Yatay Kovada");
                AddLog($"   📦 Agregalar: {agregaDetails}, Toplam: {totalAgregaKg}kg");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer2 batch oluşturma hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🆕 MIXER2 AGREGA DEĞERLERİNİ OKU
        /// </summary>
        private Dictionary<string, double> GetMixer2AgregaValues(Dictionary<string, PlcRegisterData> plcData)
        {
            var agregaData = new Dictionary<string, double>();
            
            // 65535 filtresi eklenmiş hali
            var agregaList = new[]
            {
                ("Agrega1", "H51.2", REG_M2_AGREGA1_KG),
                ("Agrega2", "H52.2", REG_M2_AGREGA2_KG),
                ("Agrega3", "H53.2", REG_M2_AGREGA3_KG),
                ("Agrega4", "H54.2", REG_M2_AGREGA4_KG),
                ("Agrega5", "H55.2", REG_M2_AGREGA5_KG),
                ("Agrega6", "H56.2", REG_M2_AGREGA6_KG),
                ("Agrega7", "H57.2", REG_M2_AGREGA7_KG),
                ("Agrega8", "H58.2", REG_M2_AGREGA8_KG)
            };
            
            foreach (var (name, aktifReg, kgReg) in agregaList)
            {
                bool aktif = plcData.ContainsKey(aktifReg) && plcData[aktifReg].Value;
                if (aktif && plcData.ContainsKey(kgReg))
                {
                    double kg = plcData[kgReg].NumericValue;
                    if (kg != 65535) // Sadece 65535 filtresi (kg > 0 kaldırıldı)
                    {
                        agregaData[name] = kg;
                    }
                }
            }
            
            return agregaData;
        }
        
        /// <summary>
        /// Mevcut batch'e agrega EKLE ve tamamlanma kontrolü yap - Alias'larla
        /// </summary>
        private async Task AddAgregasToCurrentBatch(List<string> newAgregaNames, Dictionary<string, PlcRegisterData> plcData, HashSet<string> currentActiveAgregas)
        {
            try
            {
                if (!_tartimKovasiBatchId.HasValue) return;
                
                using var context = new ProductionDbContext();
                var batch = await context.ConcreteBatches.FindAsync(_tartimKovasiBatchId.Value);
                
                if (batch == null)
                {
                    AddLog($"❌ Mixer1: Batch #{_tartimKovasiBatchId} bulunamadı!");
                    _tartimKovasiBatchId = null;
                    return;
                }
                
                // Alias'ları yükle
                var aggregateAliases = context.AggregateAliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                
                // Detay tablosuna yeni gelen agregaları ekle (alias isimleri ile)
                foreach (var newAgg in newAgregaNames)
                {
                    // Slot numarasını çıkar
                    var slotNumber = ExtractSlotNumber(newAgg);
                    
                    // Alias varsa kullan, yoksa orijinal ismi kullan
                    var displayName = aggregateAliases.TryGetValue(slotNumber, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : newAgg;
                    
                    var weight = GetAgregaValues(new List<string> { newAgg }, plcData).Item2;
                    var agregaRecord = new ConcreteBatchAggregate
                    {
                        BatchId = batch.Id,
                        Slot = slotNumber, // GERÇEK SLOT NUMARASINI KULLAN (slot++ değil!)
                        Name = displayName, // Alias ismi kullan
                        WeightKg = weight
                    };
                    context.ConcreteBatchAggregates.Add(agregaRecord);
                }
                
                // Yeni agregaları memory tarafında da işaretle
                _currentBatchAgregas.UnionWith(newAgregaNames);
                
                // Toplam kg'yi güncelle
                var (agregaDetails, totalAgregaKg) = GetAgregaValues(_currentBatchAgregas.ToList(), plcData);
                batch.TotalAggregateKg = totalAgregaKg;
                batch.RawPayloadJson = $"{{\"agregalar\":[\"{string.Join("\",\"", agregaDetails)}\"],\"timestamp\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
                
                await context.SaveChangesAsync();
                
                AddLog($"➕ Mixer1: Batch #{batch.Id}'e AGREGA EKLENDİ: {string.Join(", ", newAgregaNames)}");
                AddLog($"   📦 Güncel: {string.Join(", ", agregaDetails)}, Toplam: {totalAgregaKg}kg");
                
                // 🔥 TAMAMLANMA KONTROLÜ: Tüm beklenen agregalar geldi mi?
                var missingAgregas = _expectedAgregas.Except(_currentBatchAgregas).ToList();
                
                if (!missingAgregas.Any())
                {
                    AddLog($"✅ Mixer1: TÜM AKTİF AGREGALAR TAMAMLANDI! ({_currentBatchAgregas.Count}/{_expectedAgregas.Count})");
                    AddLog($"   🎯 Batch #{batch.Id} tartım işlemi tamamlandı");
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 agrega ekleme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// POZİSYON 1 → 2: Tartım Kovasından Bekleme Bunkerine taşı
        /// </summary>
        private async Task MoveToBeklemeBunkeri()
        {
            try
            {
                if (!_tartimKovasiBatchId.HasValue) return;
                
                using var context = new ProductionDbContext();
                var batch = await context.ConcreteBatches.FindAsync(_tartimKovasiBatchId.Value);
                
                if (batch != null)
                {
                    batch.Status = "Bekleme Bunkeri";
                    await context.SaveChangesAsync();
                    
                    AddLog($"📦 Mixer1: Batch #{batch.Id} → BEKLEME BUNKERİ'NE GEÇTİ");
                    
                    // 🔥 Pozisyon geçişi: Tartım Kovası → Bekleme Bunkeri
                    _beklemeBunkeriBatchId = batch.Id;  // Bekleme bunkerine al
                    _tartimKovasiBatchId = null;        // Tartım kovası boşaldı, yeni batch açılabilir!
                }
                
                // Tartım bilgilerini temizle
                _currentBatchAgregas.Clear();
                _expectedAgregas.Clear();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 bekleme bunkeri geçişi hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Helper: Agrega kg değerlerini oku
        /// </summary>
        private (List<string> details, double total) GetAgregaValues(List<string> agregaNames, Dictionary<string, PlcRegisterData> plcData)
        {
            var agregaKgMap = new Dictionary<string, string>
            {
                { "Agrega1", REG_M1_AGREGA1_KG },
                { "Agrega2", REG_M1_AGREGA2_KG },
                { "Agrega3", REG_M1_AGREGA3_KG },
                { "Agrega4", REG_M1_AGREGA4_KG },
                { "Agrega5", REG_M1_AGREGA5_KG }
            };
            
            double totalKg = 0;
            var details = new List<string>();
            
            foreach (var agregaName in agregaNames)
            {
                if (agregaKgMap.TryGetValue(agregaName, out var dmAddress))
                {
                    if (plcData.ContainsKey(dmAddress))
                    {
                        var kg = plcData[dmAddress].NumericValue;
                        totalKg += kg;
                        details.Add($"{agregaName}={kg}kg");
                    }
                }
            }
            
            return (details, totalKg);
        }

        private void OnPlcLogMessage(object? sender, string message)
        {
            // PLC servisinden gelen log mesajlarını ana log'a ekle
            AddLog($"📡 PLC: {message}");
        }

        /// <summary>
        /// PLC verilerini döndür (LogWindow için)
        /// </summary>
        public Dictionary<string, PlcRegisterData> GetPlcData()
        {
            return _plcDataService?.GetLastData() ?? new Dictionary<string, PlcRegisterData>();
        }

        /// <summary>
        /// PLC durumunu döndür (LogWindow için)
        /// </summary>
        public string GetPlcStatus()
        {
            if (_plcDataService == null)
                return "PLC Durumu: Servis başlatılmadı";
            
            if (_plcDataService.IsRunning)
                return "PLC Durumu: 🟢 Bağlı ve Çalışıyor";
            else
                return "PLC Durumu: 🔴 Bağlı Değil";
        }

    private async Task LoadInitialData()
        {
            try
            {
            AddLog("🔄 LoadInitialData başlatılıyor...");
                await RefreshOperatorList();
            AddLog("✅ RefreshOperatorList tamamlandı");
            
            // UI thread'de çalışması gereken metodları Dispatcher.Invoke ile çağır
            Dispatcher.Invoke(() =>
            {
                RefreshMoldsList();
                RefreshProductionNotes();
            });
            AddLog("✅ RefreshMoldsList ve RefreshProductionNotes tamamlandı");
            
                // Silo verilerini sadece yükle, yeniden oluşturma
                await LoadCementSilosOnly();
            AddLog("✅ LoadCementSilosOnly tamamlandı");
                // Bugünün tamamlanan batchlarını yükle
            AddLog("🔄 LoadShiftBatches çağrılıyor...");
            await LoadShiftBatches();
            AddLog("✅ LoadShiftBatches tamamlandı");
            }
            catch (Exception ex)
            {
            AddLog($"❌ İlk veri yükleme hatası: {ex.Message}");
            AddLog($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        private async void ProductionTimer_Tick(object? sender, EventArgs e)
        {
            // Bugünün tamamlanan batchlarını güncelle (her 30 saniyede bir) - VARDİYA DURUMUNDAN BAĞIMSIZ
            if (DateTime.Now.Second % 30 == 0)
            {
                await LoadShiftBatches();
            }
            
            // Vardiya aktif olduğunda ek işlemler
            if (_shiftActive) 
            {
                // Üretim başlangıcını kontrol et ve ActiveShift'i güncelle
                var currentProduction = GetCurrentProductionCount();
                if (currentProduction > 0 && !_productionStartTime.HasValue)
                {
                    _productionStartTime = DateTime.UtcNow;
                    ProductionStartTimeText.Text = TimeZoneHelper.FormatDateTime(_productionStartTime.Value, "dd.MM.yyyy HH:mm");
                    
                    // ActiveShift'i güncelle
                    if (_currentShiftId > 0)
                    {
                        // Palet sayısını ve DM452'yi de persist et
                        await _activeShiftService.UpdateActiveShift(_currentShiftId, _productionStartTime, _totalPalletProduction, _dm452LastValue);
                        AddLog($"🏭 Üretim başladı - {_productionStartTime:dd.MM.yyyy HH:mm}");
                    }
            }
            
            // Burada manuel üretim kaydı ekleme işlemleri yapılabilir
            // Örneğin: UI'dan manuel üretim girişi
            }
        }

        private async Task UpdateActiveMoldPrintCount(int newProduction)
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var activeMold = context.Molds.FirstOrDefault(m => m.IsActive);
                    if (activeMold != null)
                    {
                        activeMold.TotalPrints += newProduction;
                        await context.SaveChangesAsync();
                        RefreshMoldsList();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Kalıp baskı sayısı güncelleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Mevcut toplam üretim sayısını alır
        /// </summary>
        private int GetCurrentProductionCount()
        {
            try
            {
                // UI'dan toplam üretim sayısını al
                if (int.TryParse(TotalProductionText.Text, out int totalProduction))
                {
                    return totalProduction;
                }
                return 0;
            }
            catch (Exception ex)
            {
                AddLog($"Üretim sayısı alma hatası: {ex.Message}");
                return 0;
            }
        }

        private void UpdatePalletProductionUI()
        {
            try
            {
                // Ana üretim sayısını güncelle
                TotalProductionText.Text = _totalPalletProduction.ToString();
                
                // Vardiya içi üretim panelini güncelle
                ShiftStoneCountersPanel.Children.Clear();
                
                if (_totalPalletProduction > 0)
                {
                    var textBlock = new TextBlock
                    {
                        Text = $"Pallet Production: {_totalPalletProduction}",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Yeşil renk
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    ShiftStoneCountersPanel.Children.Add(textBlock);
                    NoProductionText.Text = "";
                }
                else
                {
                    NoProductionText.Text = "No pallet production yet";
                }
            }
            catch (Exception ex)
            {
                AddLog($"Pallet production UI update error: {ex.Message}");
            }
        }

        private void UpdateShiftStoneCountersUI()
        {
            try
            {
                var totalProduction = _shiftStoneCounters.Values.Sum();
                TotalProductionText.Text = totalProduction.ToString();
                
                ShiftStoneCountersPanel.Children.Clear();
                foreach (var kvp in _shiftStoneCounters.Where(x => x.Value > 0))
                {
                    var textBlock = new TextBlock
                    {
                        Text = $"{kvp.Key}: {kvp.Value}",
                        FontSize = 12,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    ShiftStoneCountersPanel.Children.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                AddLog($"UI güncelleme hatası: {ex.Message}");
            }
        }

        public void AddLog(string message)
        {
            try
            {
                _lastLogMessage = message;
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logEntry = $"[{timestamp}] {message}";
                
                // ⚡ PERFORMANS: Ana penceredeki log alanı kaldırıldı - sadece Log penceresi kullanılıyor
                // LogTextBlock.Text artık güncellenmeyecek
                
                // Global log sistemine yaz - SADECE LogWindow açıksa
                if (_logWindowOpen)
                {
                lock (_logLock)
                {
                    _globalLogMessages.Add(logEntry);
                    
                        // Son MaxLogMessages kadar mesajı tut - memory leak önleme
                    if (_globalLogMessages.Count > MaxLogMessages)
                    {
                            // Toplu temizlik - performans için
                            var removeCount = _globalLogMessages.Count - (MaxLogMessages * 3 / 4);
                            _globalLogMessages.RemoveRange(0, removeCount);
                            System.Diagnostics.Debug.WriteLine($"🧹 Global log temizlendi: {removeCount} eski mesaj silindi, kalan: {_globalLogMessages.Count}");
                        }
                    }
                }
                
                // Log dosyasına yazma - DEVRE DIŞI (performans için)
                // try
                // {
                //     System.IO.File.AppendAllText("application.log", logEntry + Environment.NewLine);
                // }
                // catch
                // {
                //     // Log dosyası yazma hatası görmezden gel
                // }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log ekleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// PLC bağlantı durumunu kontrol eder ve UI'ı günceller - KALDIRILDI
        /// </summary>

        /// <summary>
        /// PLC test butonu tıklandığında - KALDIRILDI
        /// </summary>

        private void LogCleanupTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // ⚡ PERFORMANS: LogTextBlock kaldırıldı, bu timer artık gerekli değil
                // Sadece global log temizliği yapılıyor
                
                // GC.Collect() kaldırıldı - performans optimizasyonu
                // .NET runtime otomatik olarak memory management yapar
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log temizleme hatası: {ex.Message}");
            }
        }

        private async void ConcretePageTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Sadece beton santrali sekmesi aktifse güncelle
                if (MainTabControl.SelectedItem == ConcreteTab)
                {
                    // Sadece silo verilerini yenile, yeniden oluşturma
                    await LoadCementSilosOnly();
                    AddLog("🔄 Beton santrali sayfası otomatik güncellendi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Beton santrali güncelleme hatası: {ex.Message}");
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MainTabControl.SelectedItem == ConcreteTab)
                {
                    // Beton santrali sekmesi seçildiğinde timer'ı başlat
                    if (!_concretePageTimer.IsEnabled)
                    {
                        _concretePageTimer.Start();
                        AddLog("⏰ Beton santrali otomatik güncelleme başlatıldı (10 saniye aralık)");
                    }
                }
                else
                {
                    // Diğer sekmeler seçildiğinde timer'ı durdur
                    if (_concretePageTimer.IsEnabled)
                    {
                        _concretePageTimer.Stop();
                        AddLog("⏹️ Beton santrali otomatik güncelleme durduruldu");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tab değişimi hatası: {ex.Message}");
            }
        }

        private void CopyLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ⚡ PERFORMANS: Ana penceredeki log butonu kaldırıldı
                // Log kopyalama için "📋 Log" penceresini kullanın
                MessageBox.Show("Log kopyalama özelliği kaldırıldı.\n\nLog'ları kopyalamak için üstteki '📋 Log' butonundan Log penceresini açın.",
                    "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                /*
                if (!string.IsNullOrEmpty(LogTextBlock.Text))
                {
                    Clipboard.SetText(LogTextBlock.Text);
                    AddLog("Log panoya kopyalandı");
                }
                else
                {
                    AddLog("Kopyalanacak log bulunamadı");
                }
                */
            }
            catch (Exception ex)
            {
                AddLog($"Log kopyalama hatası: {ex.Message}");
            }
        }

        private async void ToggleShiftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_shiftActive)
                {
                    if (OperatorComboBox.SelectedIndex == -1)
                    {
                        MessageBox.Show("Lütfen operatör seçiniz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    var selectedOperator = OperatorComboBox.SelectedItem as Operator;
                    if (selectedOperator != null)
                    {
                        _currentOperatorName = selectedOperator.Name;
                    }
                    
                    _shiftActive = true;
                    _shiftStartTime = DateTime.UtcNow;
                    ShiftStartTimeText.Text = _shiftStartTime != null ? TimeZoneHelper.FormatDateTime(_shiftStartTime.Value, "dd.MM.yyyy HH:mm") : "Başlatılmadı";
                    
                    // Vardiya kaydı oluştur ve ID'sini sakla
                    _currentShiftId = await CreateShiftRecord();
                    
                    // Mevcut değerleri kaydet (sıfırlamadan önce)
                    var currentTotalProduction = _totalPalletProduction;
                    var currentDm452Value = _dm452LastValue;
                    _startFireProductCount = _currentFireProductCount;
                    _startIdleTimeSeconds = _currentIdleTimeSeconds;
                    _lastProductionTime = DateTime.Now;
                    
                    // Aktif vardiya kaydı oluştur (palet sayısı sıfırlanmadan önce)
                    if (_currentShiftId > 0)
                    {
                        AddLog($"🔧 StartActiveShift çağrılıyor - _totalPalletProduction: {currentTotalProduction}, _dm452LastValue: {currentDm452Value}");
                        var activeShiftSuccess = await _activeShiftService.StartActiveShift(
                            _currentShiftId, 
                            _currentOperatorName, 
                            _shiftStartTime.Value,
                            currentTotalProduction, // Mevcut palet sayısını kullan
                            currentDm452Value,
                            _startFireProductCount); // Fire mal sayısı başlangıç değeri
                        
                        if (activeShiftSuccess)
                        {
                            AddLog($"Aktif vardiya kaydı oluşturuldu - ShiftId: {_currentShiftId}");
                        }
                        else
                        {
                            AddLog("Uyarı: Aktif vardiya kaydı oluşturulamadı");
                        }
                    }
                    
                    // Kalıp takibini başlat
                    if (_currentShiftId > 0)
                    {
                        var moldTrackingId = await _shiftMoldTrackingService.StartShiftMoldTracking(_currentShiftId, _currentOperatorName);
                        if (moldTrackingId > 0)
                        {
                            AddLog($"Kalıp takibi başlatıldı - Kayıt ID: {moldTrackingId}");
                        }
                        else
                        {
                            AddLog("Uyarı: Kalıp takibi başlatılamadı - aktif kalıp bulunamadı");
                        }
                    }
                    
                    // Notları yenile
                    RefreshProductionNotes();
                    
                    // Timer'ları başlat
                    _productionTimer.Start();
                    _vardiyaLogCleanupTimer.Start();
                    StartDm452Polling(); // Async çağrı - UI'ı bloklamaz
                    _fireProductTimer.Start(); // Fire mal sayısı takibi başlat
                    _idleTimeTimer.Start(); // Boşta geçen süre takibi başlat
                    System.Diagnostics.Debug.WriteLine($"[MAIN] {DateTime.Now:HH:mm:ss} - Vardiya başlatıldı, timer başlatıldı");
                    
                    OperatorComboBox.IsEnabled = false;
                    ToggleShiftButton.Content = "End Shift";
                    ToggleShiftButton.Background = new SolidColorBrush(Colors.Red);
                    
                    _shiftStoneCounters.Clear();
                    AddLog($"🔧 Vardiya başlatılıyor - Mevcut _totalPalletProduction: {currentTotalProduction}");
                    
                    // UI'da başlangıç değerlerini göster
                    FireProductText.Text = _currentFireProductCount.ToString();
                    IdleTimeText.Text = FormatIdleTime(_currentIdleTimeSeconds);
                    
                    // Shift batches'i yükle
                    await LoadShiftBatches();
                    _totalPalletProduction = 0; // Palet üretimini sıfırla
                    _dm452LastValue = null; // DM452 başlangıç değerini sıfırla
                    AddLog($"🔧 Vardiya başlatıldı - Yeni _totalPalletProduction: {_totalPalletProduction}");
                    // _productionStarted = false; // Field silindi
                    _productionStartTime = null;
                    ProductionStartTimeText.Text = "Başlatılmadı";
                    TotalProductionText.Text = "0";
                    ShiftStoneCountersPanel.Children.Clear();
                    
                    AddLog($"Vardiya başlatıldı - Operatör: {_currentOperatorName}");
                }
                else
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to end the shift?\n\nOperator: {_currentOperatorName}\nTotal Pallet Production: {_totalPalletProduction} pallets",
                        "Shift End Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var shiftEndTime = DateTime.UtcNow;
                        
                        // Aktif vardiya kaydını bitir
                        if (_currentShiftId > 0)
                        {
                            var activeShiftSuccess = await _activeShiftService.EndActiveShift(_currentShiftId);
                            if (activeShiftSuccess)
                            {
                                AddLog($"Aktif vardiya kaydı bitirildi - ShiftId: {_currentShiftId}");
                            }
                            else
                            {
                                AddLog("Uyarı: Aktif vardiya kaydı bitirilemedi");
                            }
                        }
                        
                        // Kalıp takibini tamamla
                        if (_currentShiftId > 0)
                        {
                            var moldRecords = await _shiftMoldTrackingService.CompleteShiftMoldTracking(_currentShiftId);
                            AddLog($"Kalıp takibi tamamlandı - {moldRecords.Count} kalıp kaydı");
                            
                            // Kalıp bazında üretim özetini göster
                            foreach (var record in moldRecords)
                            {
                                AddLog($"  - {record.MoldName}: {record.ProductionCount} palet");
                            }
                        }
                        
                        var pdfPath = await SaveShiftRecordAndExportPdf(shiftEndTime);
                        
                        if (!string.IsNullOrEmpty(pdfPath))
                        {
                            try
                            {
                                _pdfExportService.OpenPdfFile(pdfPath);
                                AddLog("PDF report opened automatically");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"PDF opening error: {ex.Message}");
                            }
                        }
                        
                        _currentOperatorName = "";
                        _shiftActive = false;
                        _totalPalletProduction = 0; // Palet üretimini sıfırla
                        _dm452LastValue = null; // DM452 başlangıç değerini sıfırla
                        // _productionStarted = false; // Field silindi
                        _currentShiftId = 0; // Vardiya ID'sini sıfırla
                        
                        // Notları sıfırla
                        RefreshProductionNotes();
                        
                        // Timer'ları durdur
                        _productionTimer.Stop();
                        StopDm452Polling();
                        _fireProductTimer.Stop(); // Fire mal sayısı takibi durdur
                        _idleTimeTimer.Stop(); // Boşta geçen süre takibi durdur
                        _vardiyaLogCleanupTimer.Stop();
                        
                        // PLC'den gelen son değerleri kullan
                        AddLog($"Fire mal sayısı: {_currentFireProductCount}, Boşta geçen süre: {FormatIdleTime(_currentIdleTimeSeconds)}");
                        
                        // UI'da son değerleri göster
                        FireProductText.Text = _currentFireProductCount.ToString();
                        IdleTimeText.Text = FormatIdleTime(_currentIdleTimeSeconds);
                        
                        System.Diagnostics.Debug.WriteLine($"[MAIN] {DateTime.Now:HH:mm:ss} - Vardiya bitirildi, timer'lar durduruldu");
                        
                        OperatorComboBox.IsEnabled = true;
                        OperatorComboBox.SelectedIndex = -1;
                        ToggleShiftButton.Content = "Vardiyayı Başlat";
                        ToggleShiftButton.Background = new SolidColorBrush(Colors.Green);
                        
                        // Shift batches'i temizle
                        await LoadShiftBatches();
                        
                        AddLog("Vardiya bitirildi");
                        
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Shift operation error: {ex.Message}");
            }
        }

        private async Task<string> SaveShiftRecordAndExportPdf(DateTime shiftEndTime)
        {
            try
            {
                if (_shiftStartTime.HasValue)
                {
                    // Get fire product count from PLC (already updated in _currentFireProductCount)
                    var fireProductCount = _currentFireProductCount;
                    
                    // Subtract defective products from total production
                    var totalProduction = _totalPalletProduction; // Palet üretimini kullan
                    var netProduction = totalProduction - fireProductCount;
                    
                    // Create stone production dictionary including pallet production
                    var palletProductionDict = new Dictionary<string, int>
                    {
                        { "Pallet Production", _totalPalletProduction }
                    };
                    
                    // Vardiya süresince batch bilgilerini hesapla
                    var batchInfo = await CalculateShiftBatchInfo(_shiftStartTime.Value, shiftEndTime);
                    
                    // Kalıp bilgilerini al
                    string? moldProductionJson = null;
                    if (_currentShiftId > 0)
                    {
                        moldProductionJson = await _shiftMoldTrackingService.GetShiftMoldProductionSummary(_currentShiftId);
                        AddLog($"🔍 MoldProductionJson alındı - ShiftId: {_currentShiftId}, JSON: {moldProductionJson}");
                        
                        // Eğer boş array dönerse null yap
                        if (moldProductionJson == "[]" || string.IsNullOrEmpty(moldProductionJson))
                        {
                            moldProductionJson = null;
                            AddLog("⚠️ MoldProductionJson boş, null yapıldı");
                        }
                    }
                    
                    await _shiftRecordService.CreateShiftRecordAsync(
                        _shiftStartTime.Value,
                        shiftEndTime,
                        _currentOperatorName,
                        netProduction, // Net production minus defective products
                        _productionStartTime,
                        palletProductionDict,
                        batchInfo.Mixer1BatchCount,
                        batchInfo.Mixer1CementTotal,
                        batchInfo.Mixer1CementTypesJson,
                        batchInfo.Mixer2BatchCount,
                        batchInfo.Mixer2CementTotal,
                        batchInfo.Mixer2CementTypesJson,
                        moldProductionJson,
                        batchInfo.Mixer1MaterialsJson,
                        batchInfo.Mixer2MaterialsJson,
                        batchInfo.TotalMaterialsJson,
                        fireProductCount,
                        _currentIdleTimeSeconds);
                    
                    var shiftRecord = new ShiftRecord
                    {
                        ShiftStartTime = _shiftStartTime.Value,
                        ShiftEndTime = shiftEndTime,
                        OperatorName = _currentOperatorName,
                        TotalProduction = netProduction, // Net production minus defective products
                        StoneProductionJson = System.Text.Json.JsonSerializer.Serialize(palletProductionDict),
                        ProductionStartTime = _productionStartTime,
                        ShiftDurationMinutes = (int)(shiftEndTime - _shiftStartTime.Value).TotalMinutes,
                        ProductionDurationMinutes = _productionStartTime.HasValue 
                            ? (int)(shiftEndTime - _productionStartTime.Value).TotalMinutes 
                            : 0,
                        FireProductCount = fireProductCount, // Save fire product count
                        IdleTimeSeconds = _currentIdleTimeSeconds, // Save idle time from PLC
                        Mixer1BatchCount = batchInfo.Mixer1BatchCount,
                        Mixer1CementTotal = batchInfo.Mixer1CementTotal,
                        Mixer1CementTypesJson = batchInfo.Mixer1CementTypesJson,
                        Mixer2BatchCount = batchInfo.Mixer2BatchCount,
                        Mixer2CementTotal = batchInfo.Mixer2CementTotal,
                        Mixer2CementTypesJson = batchInfo.Mixer2CementTypesJson,
                        MoldProductionJson = moldProductionJson, // Kalıp bilgilerini ekle
                        Mixer1MaterialsJson = batchInfo.Mixer1MaterialsJson,
                        Mixer2MaterialsJson = batchInfo.Mixer2MaterialsJson,
                        TotalMaterialsJson = batchInfo.TotalMaterialsJson
                    };
                    
                    // Add shift notes to PDF
                    var shiftNotes = await GetShiftNotes(_currentShiftId);
                    
                    var pdfPath = _pdfExportService.CreateShiftReportPdf(shiftRecord, shiftNotes);
                    
                    // Delete shift notes
                    await DeleteShiftNotes(_currentShiftId);
                    
                    AddLog($"Shift record saved, PDF created and notes deleted. Defective products: {fireProductCount} pieces removed.");
                    return pdfPath;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                AddLog($"Vardiya kaydetme hatası: {ex.Message}");
                AddLog($"Inner Exception: {ex.InnerException?.Message ?? "Yok"}");
                AddLog($"Stack Trace: {ex.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Vardiya süresince batch bilgilerini hesapla
        /// </summary>
        private async Task<(int Mixer1BatchCount, double Mixer1CementTotal, string Mixer1CementTypesJson, 
                           int Mixer2BatchCount, double Mixer2CementTotal, string Mixer2CementTypesJson,
                           string Mixer1MaterialsJson, string Mixer2MaterialsJson, string TotalMaterialsJson)> 
            CalculateShiftBatchInfo(DateTime shiftStartTime, DateTime shiftEndTime)
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Mixer1 batch bilgileri - CompletedAt null ise OccurredAt kullan
                var mixer1Batches = await context.ConcreteBatches
                    .Where(b => b.Status == "Tamamlandı" && 
                               ((b.CompletedAt.HasValue && b.CompletedAt >= shiftStartTime && b.CompletedAt <= shiftEndTime) ||
                                (!b.CompletedAt.HasValue && b.OccurredAt >= shiftStartTime && b.OccurredAt <= shiftEndTime)))
                    .Include(b => b.Aggregates)      // ✅ Agrega detayları için
                    .Include(b => b.Admixtures)      // ✅ Katkı detayları için
                    .Include(b => b.Pigments)        // ✅ Pigment detayları için
                    .Include(b => b.Cements)         // ✅ Çimento detayları için
                    .ToListAsync();
                
                var mixer1Count = mixer1Batches.Count;
                var mixer1CementTotal = mixer1Batches.Sum(b => b.TotalCementKg);
                
                // Mixer1 çimento türleri
                var mixer1CementTypes = mixer1Batches
                    .Where(b => b.TotalCementKg > 0)
                    .GroupBy(b => b.CementType ?? "Unknown")
                    .Select(g => new { Type = g.Key, Total = g.Sum(b => b.TotalCementKg) })
                    .OrderByDescending(x => x.Total)
                    .ToList();
                
                var mixer1CementTypesJson = System.Text.Json.JsonSerializer.Serialize(
                    mixer1CementTypes.ToDictionary(c => c.Type, c => c.Total));
                
                // Mixer1 malzeme detayları
                var mixer1Materials = await CalculateMixerMaterials(context, mixer1Batches, 1);
                var mixer1MaterialsJson = System.Text.Json.JsonSerializer.Serialize(mixer1Materials);
                
                // Mixer2 batch bilgileri - CompletedAt null ise OccurredAt kullan
                var mixer2Batches = await context.ConcreteBatch2s
                    .Where(b => b.Status == "Tamamlandı" && 
                               ((b.CompletedAt.HasValue && b.CompletedAt >= shiftStartTime && b.CompletedAt <= shiftEndTime) ||
                                (!b.CompletedAt.HasValue && b.OccurredAt >= shiftStartTime && b.OccurredAt <= shiftEndTime)))
                    .Include(b => b.Aggregates)      // ✅ Agrega detayları için
                    .Include(b => b.Admixtures)      // ✅ Katkı detayları için
                    .Include(b => b.Cements)          // ✅ Çimento detayları için
                    .ToListAsync();
                
                var mixer2Count = mixer2Batches.Count;
                var mixer2CementTotal = mixer2Batches.Sum(b => b.TotalCementKg);
                
                // Mixer2 çimento türleri
                var mixer2CementTypes = mixer2Batches
                    .Where(b => b.TotalCementKg > 0)
                    .GroupBy(b => b.CementType ?? "Unknown")
                    .Select(g => new { Type = g.Key, Total = g.Sum(b => b.TotalCementKg) })
                    .OrderByDescending(x => x.Total)
                    .ToList();
                
                var mixer2CementTypesJson = System.Text.Json.JsonSerializer.Serialize(
                    mixer2CementTypes.ToDictionary(c => c.Type, c => c.Total));
                
                // Mixer2 malzeme detayları
                var mixer2Materials = await CalculateMixer2Materials(context, mixer2Batches);
                var mixer2MaterialsJson = System.Text.Json.JsonSerializer.Serialize(mixer2Materials);
                
                // Toplam malzeme detayları (alias birleştirme ile)
                var totalMaterials = CombineMaterials(mixer1Materials, mixer2Materials);
                var totalMaterialsJson = System.Text.Json.JsonSerializer.Serialize(totalMaterials);
                
                AddLog($"Vardiya batch bilgileri hesaplandı - M1: {mixer1Count} batches ({mixer1CementTotal:F0}kg), M2: {mixer2Count} batches ({mixer2CementTotal:F0}kg)");
                
                return (mixer1Count, mixer1CementTotal, mixer1CementTypesJson, 
                        mixer2Count, mixer2CementTotal, mixer2CementTypesJson,
                        mixer1MaterialsJson, mixer2MaterialsJson, totalMaterialsJson);
            }
            catch (Exception ex)
            {
                AddLog($"Vardiya batch bilgileri hesaplama hatası: {ex.Message}");
                return (0, 0, "", 0, 0, "", "", "", "");
            }
        }

        /// <summary>
        /// Mixer malzeme detaylarını hesapla
        /// </summary>
        private async Task<MaterialDetails> CalculateMixerMaterials(ProductionDbContext context, List<ConcreteBatch> batches, int mixerId)
        {
            var materials = new MaterialDetails();
            
            try
            {
                // Çimento detayları
                foreach (var batch in batches)
                {
                    if (batch?.Cements != null)
                    {
                        foreach (var cement in batch.Cements)
                        {
                            var aliasName = await GetCementAliasName(context, cement.Slot, mixerId);
                            if (!materials.Cements.ContainsKey(aliasName))
                                materials.Cements[aliasName] = 0;
                            materials.Cements[aliasName] += cement.WeightKg;
                            materials.TotalCementKg += cement.WeightKg;
                        }
                    }
                    
                    // Agrega detayları
                    if (batch?.Aggregates != null)
                    {
                        foreach (var aggregate in batch.Aggregates)
                        {
                            var aliasName = await GetAggregateAliasName(context, aggregate.Slot, mixerId);
                            if (!materials.Aggregates.ContainsKey(aliasName))
                                materials.Aggregates[aliasName] = 0;
                            materials.Aggregates[aliasName] += aggregate.WeightKg;
                            materials.TotalAggregateKg += aggregate.WeightKg;
                        }
                    }
                    
                    // Katkı detayları
                    if (batch?.Admixtures != null)
                    {
                        foreach (var admixture in batch.Admixtures)
                        {
                            var aliasName = await GetAdmixtureAliasName(context, admixture.Slot, mixerId);
                            if (!materials.Admixtures.ContainsKey(aliasName))
                                materials.Admixtures[aliasName] = 0;
                            materials.Admixtures[aliasName] += admixture.ChemicalKg + admixture.WaterKg;
                            materials.TotalAdmixtureKg += admixture.ChemicalKg + admixture.WaterKg;
                        }
                    }
                    
                    // Pigment detayları
                    if (batch?.Pigments != null)
                    {
                        foreach (var pigment in batch.Pigments)
                        {
                            var aliasName = await GetPigmentAliasName(context, (short)pigment.Slot, mixerId);
                            if (!materials.Pigments.ContainsKey(aliasName))
                                materials.Pigments[aliasName] = 0;
                            materials.Pigments[aliasName] += pigment.WeightKg;
                            materials.TotalPigmentKg += pigment.WeightKg;
                        }
                    }
                    
                    // Su miktarı
                    materials.TotalWaterKg += batch?.EffectiveWaterKg ?? 0;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Mixer{mixerId} malzeme hesaplama hatası: {ex.Message}");
            }
            
            return materials;
        }

        /// <summary>
        /// Mixer2 malzeme detaylarını hesapla
        /// </summary>
        private async Task<MaterialDetails> CalculateMixer2Materials(ProductionDbContext context, List<ConcreteBatch2> batches)
        {
            var materials = new MaterialDetails();
            
            try
            {
                // Çimento detayları
                foreach (var batch in batches)
                {
                    if (batch?.Cements != null)
                    {
                        foreach (var cement in batch.Cements)
                        {
                            var aliasName = await GetCementAliasName(context, cement.Slot, 2);
                            if (!materials.Cements.ContainsKey(aliasName))
                                materials.Cements[aliasName] = 0;
                            materials.Cements[aliasName] += cement.WeightKg;
                            materials.TotalCementKg += cement.WeightKg;
                        }
                    }
                    
                    // Agrega detayları
                    if (batch?.Aggregates != null)
                    {
                        foreach (var aggregate in batch.Aggregates)
                        {
                            var aliasName = await GetAggregateAliasName(context, aggregate.Slot, 2);
                            if (!materials.Aggregates.ContainsKey(aliasName))
                                materials.Aggregates[aliasName] = 0;
                            materials.Aggregates[aliasName] += aggregate.WeightKg;
                            materials.TotalAggregateKg += aggregate.WeightKg;
                        }
                    }
                    
                    // Katkı detayları
                    if (batch?.Admixtures != null)
                    {
                        foreach (var admixture in batch.Admixtures)
                        {
                            var aliasName = await GetAdmixtureAliasName(context, admixture.Slot, 2);
                            if (!materials.Admixtures.ContainsKey(aliasName))
                                materials.Admixtures[aliasName] = 0;
                            materials.Admixtures[aliasName] += admixture.ChemicalKg + admixture.WaterKg;
                            materials.TotalAdmixtureKg += admixture.ChemicalKg + admixture.WaterKg;
                        }
                    }
                    
                    // Pigment detayları (Mixer2'de koleksiyon yok, tek tek alanlar var)
                    if (batch.Pigment1Kg > 0)
                    {
                        var aliasName = await GetPigmentAliasName(context, 1, 2);
                        if (!materials.Pigments.ContainsKey(aliasName))
                            materials.Pigments[aliasName] = 0;
                        materials.Pigments[aliasName] += batch.Pigment1Kg;
                        materials.TotalPigmentKg += batch.Pigment1Kg;
                    }
                    if (batch.Pigment2Kg > 0)
                    {
                        var aliasName = await GetPigmentAliasName(context, 2, 2);
                        if (!materials.Pigments.ContainsKey(aliasName))
                            materials.Pigments[aliasName] = 0;
                        materials.Pigments[aliasName] += batch.Pigment2Kg;
                        materials.TotalPigmentKg += batch.Pigment2Kg;
                    }
                    if (batch.Pigment3Kg > 0)
                    {
                        var aliasName = await GetPigmentAliasName(context, 3, 2);
                        if (!materials.Pigments.ContainsKey(aliasName))
                            materials.Pigments[aliasName] = 0;
                        materials.Pigments[aliasName] += batch.Pigment3Kg;
                        materials.TotalPigmentKg += batch.Pigment3Kg;
                    }
                    if (batch.Pigment4Kg > 0)
                    {
                        var aliasName = await GetPigmentAliasName(context, 4, 2);
                        if (!materials.Pigments.ContainsKey(aliasName))
                            materials.Pigments[aliasName] = 0;
                        materials.Pigments[aliasName] += batch.Pigment4Kg;
                        materials.TotalPigmentKg += batch.Pigment4Kg;
                    }
                    
                    // Su miktarı
                    materials.TotalWaterKg += batch?.EffectiveWaterKg ?? 0;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Mixer2 malzeme hesaplama hatası: {ex.Message}");
            }
            
            return materials;
        }

        /// <summary>
        /// İki mixer'in malzeme detaylarını birleştir (alias birleştirme ile)
        /// </summary>
        private MaterialDetails CombineMaterials(MaterialDetails mixer1, MaterialDetails mixer2)
        {
            var combined = new MaterialDetails();
            
            // Çimento birleştirme
            foreach (var cement in mixer1.Cements)
            {
                if (!combined.Cements.ContainsKey(cement.Key))
                    combined.Cements[cement.Key] = 0;
                combined.Cements[cement.Key] += cement.Value;
            }
            foreach (var cement in mixer2.Cements)
            {
                if (!combined.Cements.ContainsKey(cement.Key))
                    combined.Cements[cement.Key] = 0;
                combined.Cements[cement.Key] += cement.Value;
            }
            
            // Agrega birleştirme
            foreach (var aggregate in mixer1.Aggregates)
            {
                if (!combined.Aggregates.ContainsKey(aggregate.Key))
                    combined.Aggregates[aggregate.Key] = 0;
                combined.Aggregates[aggregate.Key] += aggregate.Value;
            }
            foreach (var aggregate in mixer2.Aggregates)
            {
                if (!combined.Aggregates.ContainsKey(aggregate.Key))
                    combined.Aggregates[aggregate.Key] = 0;
                combined.Aggregates[aggregate.Key] += aggregate.Value;
            }
            
            // Katkı birleştirme
            foreach (var admixture in mixer1.Admixtures)
            {
                if (!combined.Admixtures.ContainsKey(admixture.Key))
                    combined.Admixtures[admixture.Key] = 0;
                combined.Admixtures[admixture.Key] += admixture.Value;
            }
            foreach (var admixture in mixer2.Admixtures)
            {
                if (!combined.Admixtures.ContainsKey(admixture.Key))
                    combined.Admixtures[admixture.Key] = 0;
                combined.Admixtures[admixture.Key] += admixture.Value;
            }
            
            // Pigment birleştirme
            foreach (var pigment in mixer1.Pigments)
            {
                if (!combined.Pigments.ContainsKey(pigment.Key))
                    combined.Pigments[pigment.Key] = 0;
                combined.Pigments[pigment.Key] += pigment.Value;
            }
            foreach (var pigment in mixer2.Pigments)
            {
                if (!combined.Pigments.ContainsKey(pigment.Key))
                    combined.Pigments[pigment.Key] = 0;
                combined.Pigments[pigment.Key] += pigment.Value;
            }
            
            // Toplam değerler
            combined.TotalWaterKg = mixer1.TotalWaterKg + mixer2.TotalWaterKg;
            combined.TotalCementKg = mixer1.TotalCementKg + mixer2.TotalCementKg;
            combined.TotalAggregateKg = mixer1.TotalAggregateKg + mixer2.TotalAggregateKg;
            combined.TotalAdmixtureKg = mixer1.TotalAdmixtureKg + mixer2.TotalAdmixtureKg;
            combined.TotalPigmentKg = mixer1.TotalPigmentKg + mixer2.TotalPigmentKg;
            
            return combined;
        }

        /// <summary>
        /// Çimento alias ismini al
        /// </summary>
        private async Task<string> GetCementAliasName(ProductionDbContext context, short slot, int mixerId)
        {
            try
            {
                if (mixerId == 1)
                {
                    var alias = await context.CementAliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Cimento{slot}";
                }
                else
                {
                    var alias = await context.Cement2Aliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Cimento{slot}";
                }
            }
            catch
            {
                return $"Cimento{slot}";
            }
        }

        /// <summary>
        /// Agrega alias ismini al
        /// </summary>
        private async Task<string> GetAggregateAliasName(ProductionDbContext context, short slot, int mixerId)
        {
            try
            {
                if (mixerId == 1)
                {
                    var alias = await context.AggregateAliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Agrega{slot}";
                }
                else
                {
                    var alias = await context.Aggregate2Aliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Agrega{slot}";
                }
            }
            catch
            {
                return $"Agrega{slot}";
            }
        }

        /// <summary>
        /// Katkı alias ismini al
        /// </summary>
        private async Task<string> GetAdmixtureAliasName(ProductionDbContext context, short slot, int mixerId)
        {
            try
            {
                if (mixerId == 1)
                {
                    var alias = await context.AdmixtureAliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Katki{slot}";
                }
                else
                {
                    var alias = await context.Admixture2Aliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Katki{slot}";
                }
            }
            catch
            {
                return $"Katki{slot}";
            }
        }

        /// <summary>
        /// Pigment alias ismini al
        /// </summary>
        private async Task<string> GetPigmentAliasName(ProductionDbContext context, short slot, int mixerId)
        {
            try
            {
                if (mixerId == 1)
                {
                    var alias = await context.PigmentAliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Pigment{slot}";
                }
                else
                {
                    var alias = await context.Pigment2Aliases.FirstOrDefaultAsync(a => a.Slot == slot && a.IsActive);
                    return alias?.Name ?? $"Pigment{slot}";
                }
            }
            catch
            {
                return $"Pigment{slot}";
            }
        }

        public async Task RefreshOperatorList()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var operatorService = new OperatorService(context);
                    var operators = await operatorService.GetAllOperatorsAsync();
                    
                    Dispatcher.Invoke(() =>
                    {
                        OperatorComboBox.ItemsSource = operators;
                    });
                }
            }
            catch (Exception ex)
            {
                AddLog($"Operatör listesi yenileme hatası: {ex.Message}");
            }
        }

        public async Task RefreshMoldList()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var molds = await context.Molds
                        .Where(m => m.IsActive)
                        .OrderBy(m => m.Name)
                        .ToListAsync();
                    
                    Dispatcher.Invoke(() =>
                    {
                        // Kalıp listesini yenile - eğer kalıp listesi varsa
                        // Bu metod ayarlardan kalıp eklendiğinde ana sayfayı yenilemek için
                        AddLog($"Kalıp listesi yenilendi: {molds.Count} adet kalıp");
                    });
                }
            }
            catch (Exception ex)
            {
                AddLog($"Mold list refresh error: {ex.Message}");
            }
        }

        private async void AddMoldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddMoldDialog();
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    var mold = new Mold
                    {
                        Name = dialog.MoldName,
                        Code = dialog.MoldCode,
                        IsActive = false,
                        Description = "Yeni kalıp",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    using (var context = new ProductionDbContext())
                    {
                        context.Molds.Add(mold);
                        await context.SaveChangesAsync();
                    }
                    
                    RefreshMoldsList();
                    AddLog($"Yeni kalıp eklendi: {dialog.MoldName} ({dialog.MoldCode})");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Kalıp ekleme hatası: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nDetay: {ex.InnerException.Message}";
                }
                AddLog(errorMessage);
                MessageBox.Show(errorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNoteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vardiya aktif değilse not eklemeyi engelle
                if (!_shiftActive || _currentShiftId == 0)
                {
                    MessageBox.Show("Not eklemek için önce vardiyayı başlatmanız gerekiyor!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var dialog = new AddProductionNoteDialog();
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    var note = new ProductionNote
                    {
                        ShiftId = _currentShiftId, // Aktif vardiya ID'si
                        Note = dialog.NoteText,
                        FireProductCount = 0, // Fire ürün artık PLC'den gelecek
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = _currentOperatorName ?? "Bilinmeyen"
                    };
                    
                    using (var context = new ProductionDbContext())
                    {
                        context.ProductionNotes.Add(note);
                        context.SaveChanges();
                    }
                    
                    RefreshProductionNotes();
                    AddLog($"Üretim notu eklendi: {dialog.NoteText}");
                }
            }
            catch (Exception ex)
            {
                string detailedError = $"Not ekleme hatası: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    detailedError += $"\n\nİç Hata: {ex.InnerException.Message}";
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        detailedError += $"\n\nDetay: {ex.InnerException.InnerException.Message}";
                    }
                }
                
                detailedError += $"\n\nStack Trace:\n{ex.StackTrace}";
                
                MessageBox.Show(detailedError, "Detaylı Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshMoldsList()
        {
            try
            {
                MoldsPanel.Children.Clear();
                using (var context = new ProductionDbContext())
                {
                    var molds = context.Molds.OrderByDescending(m => m.IsActive).ThenBy(m => m.Name).ToList();
                
                    foreach (var mold in molds)
                    {
                        var moldBorder = new Border
                        {
                            Background = mold.IsActive ? new SolidColorBrush(Color.FromRgb(232, 245, 232)) : new SolidColorBrush(Colors.White),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(6),
                            Margin = new Thickness(0, 3, 0, 3),
                            Padding = new Thickness(10)
                        };

                        var mainGrid = new Grid();
                        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var infoPanel = new StackPanel();
                        
                        var nameText = new TextBlock
                        {
                            FontSize = 13,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 3)
                        };
                        
                        var nameRun = new Run($"{mold.Name} ({mold.Code}) - ")
                        {
                            Foreground = new SolidColorBrush(Colors.DarkBlue)
                        };
                        
                        var statusRun = new Run(mold.IsActive ? "🟢 Active" : "🔴 Inactive")
                        {
                            Foreground = mold.IsActive ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red)
                        };
                        
                        nameText.Inlines.Add(nameRun);
                        nameText.Inlines.Add(statusRun);
                        infoPanel.Children.Add(nameText);

                        // Total, Install and Remove dates side by side
                        var infoRowPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 2, 0, 0)
                        };

                        var totalText = new TextBlock
                        {
                            Text = $"Total: {mold.TotalPrints}",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Colors.Black),
                            Margin = new Thickness(0, 0, 15, 0)
                        };

                        var installDateText = new TextBlock
                        {
                            Text = $"Install: {mold.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}",
                            FontSize = 9,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(0, 0, 15, 0)
                        };

                        var removeDateText = new TextBlock
                        {
                            Text = $"Remove: {(mold.UpdatedAt != mold.CreatedAt ? TimeZoneHelper.FormatDateTime(mold.UpdatedAt, "dd.MM.yyyy HH:mm") : "Not removed yet")}",
                            FontSize = 9,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(0, 0, 0, 0)
                        };

                        infoRowPanel.Children.Add(totalText);
                        infoRowPanel.Children.Add(installDateText);
                        infoRowPanel.Children.Add(removeDateText);
                        infoPanel.Children.Add(infoRowPanel);

                        Grid.SetColumn(infoPanel, 0);
                        mainGrid.Children.Add(infoPanel);

                        var toggleButton = new Button
                        {
                            Content = mold.IsActive ? "Make Inactive" : "Make Active",
                            FontSize = 11,
                            Padding = new Thickness(10, 5, 10, 5),
                            Margin = new Thickness(5, 0, 5, 0),
                            Background = mold.IsActive ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Green),
                            Foreground = new SolidColorBrush(Colors.White),
                            Tag = mold.Id
                        };
                        toggleButton.Click += ToggleMoldStatus_Click;
                        Grid.SetColumn(toggleButton, 1);
                        mainGrid.Children.Add(toggleButton);

                        // Delete button removed for safety - only available in settings

                        moldBorder.Child = mainGrid;
                        MoldsPanel.Children.Add(moldBorder);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Mold list refresh error: {ex.Message}");
            }
        }

        private async void ToggleMoldStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int moldId)
                {
                    var selectedMold = _context.Molds.FirstOrDefault(m => m.Id == moldId);
                    if (selectedMold != null)
                    {
                        if (selectedMold.IsActive)
                        {
                            var result = MessageBox.Show(
                                $"Are you sure you want to make the mold '{selectedMold.Name}' inactive?",
                                "Make Mold Inactive Confirmation",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                selectedMold.IsActive = false;
                                selectedMold.UpdatedAt = DateTime.UtcNow;
                                AddLog($"Mold made inactive: {selectedMold.Name}");
                                
                                // Vardiya aktifse kalıp değişikliğini kaydet
                                if (_shiftActive && _currentShiftId > 0)
                                {
                                    try
                                    {
                                        // Mevcut üretim sayısını al
                                        var currentProduction = GetCurrentProductionCount();
                                        
                                        // Kalıp değişikliğini kaydet (pasif yapma)
                                        var success = await _shiftMoldTrackingService.RecordMoldChange(
                                            _currentShiftId, 
                                            selectedMold.Id, 
                                            _currentOperatorName);
                                        
                                        if (success)
                                        {
                                            AddLog($"Kalıp pasif yapıldı: {selectedMold.Name}");
                                        }
                                        else
                                        {
                                            AddLog("Uyarı: Kalıp değişikliği kaydedilemedi");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AddLog($"Kalıp değişikliği kaydetme hatası: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            var result = MessageBox.Show(
                                $"Are you sure you want to make the mold '{selectedMold.Name}' active?\n\nThis will make all other molds inactive.",
                                "Mold Change Confirmation",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                // Önceki aktif kalıbı kaydet
                                var previousActiveMold = _context.Molds.FirstOrDefault(m => m.IsActive);
                                
                                var allMolds = _context.Molds.ToList();
                                foreach (var mold in allMolds)
                                {
                                    mold.IsActive = false;
                                    mold.UpdatedAt = DateTime.UtcNow;
                                }
                                
                                selectedMold.IsActive = true;
                                selectedMold.CreatedAt = DateTime.UtcNow;
                                AddLog($"Mold made active: {selectedMold.Name}");
                                
                                // Vardiya aktifse kalıp değişikliğini kaydet
                                if (_shiftActive && _currentShiftId > 0)
                                {
                                    try
                                    {
                                        // Mevcut üretim sayısını al
                                        var currentProduction = GetCurrentProductionCount();
                                        
                                        // Kalıp değişikliğini kaydet
                                        var success = await _shiftMoldTrackingService.RecordMoldChange(
                                            _currentShiftId, 
                                            selectedMold.Id, 
                                            _currentOperatorName);
                                        
                                        if (success)
                                        {
                                            AddLog($"Kalıp değişikliği kaydedildi: {previousActiveMold?.Name ?? "Bilinmeyen"} → {selectedMold.Name}");
                                        }
                                        else
                                        {
                                            AddLog("Uyarı: Kalıp değişikliği kaydedilemedi");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AddLog($"Kalıp değişikliği kaydetme hatası: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        RefreshMoldsList();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Kalıp durum değiştirme hatası: {ex.Message}");
            }
        }

        // DeleteMold_Click method removed for safety - only available in settings

        private void OpenShiftHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var shiftHistoryWindow = new ShiftHistoryWindow();
                shiftHistoryWindow.Owner = this;
                shiftHistoryWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AddLog($"Vardiya geçmişi açma hatası: {ex.Message}");
            }
        }



        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AddLog($"Ayarlar sayfası açma hatası: {ex.Message}");
            }
        }

        private void OpenHRegisterMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // H Register Monitor penceresi kaldırıldı
                AddLog("📊 H Register İzleme penceresi açıldı");
            }
            catch (Exception ex)
            {
                AddLog($"H Register İzleme penceresi açma hatası: {ex.Message}");
            }
        }

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logWindow = new LogWindow(this);
                logWindow.Owner = this;
                logWindow.Show();
                AddLog("📋 Log penceresi açıldı");
            }
            catch (Exception ex)
            {
                AddLog($"Log penceresi açma hatası: {ex.Message}");
            }
        }



        // Beton Santrali Raporlama Butonları
        private void OpenConcreteReportingM1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var concreteReportingWindow = new ConcreteReportingWindow();
                concreteReportingWindow.Owner = this;
                concreteReportingWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AddLog($"Mixer1 raporlama açma hatası: {ex.Message}");
            }
        }

        private void OpenConcreteReportingM2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var concreteReporting2Window = new ConcreteReporting2Window();
                concreteReporting2Window.Owner = this;
                concreteReporting2Window.ShowDialog();
            }
            catch (Exception ex)
            {
                AddLog($"Mixer2 raporlama açma hatası: {ex.Message}");
            }
        }

        private void OpenGeneralReportsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportsWindow = new ReportsWindow();
                reportsWindow.Owner = this;
                reportsWindow.ShowDialog();
                AddLog("📊 General Reporting page opened");
            }
            catch (Exception ex)
            {
                AddLog($"General reporting opening error: {ex.Message}");
            }
        }

        private async void RefreshSilosButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Siloları veritabanından yeniden yükle (temizleme yapmadan)
                await LoadCementSilosOnly();
                AddLog("✅ Çimento siloları yenilendi.");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Çimento siloları yenileme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Vardiya batch'larını yükle ve göster
        /// </summary>
        private async Task LoadShiftBatches()
        {
            try
            {
                AddLog("🔄 Shift Batches yükleniyor...");
                
                // UI elementlerini temizle
                Dispatcher.Invoke(() =>
                {
                    ShiftBatchesPanel.Children.Clear();
                });
                
                // Eğer aktif vardiya yoksa bilgi göster
                if (!_shiftActive || !_shiftStartTime.HasValue)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var noShiftText = new TextBlock
                        {
                            Text = "No active shift - batches will be shown when shift starts",
                            FontSize = 12,
                            FontStyle = FontStyles.Italic,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(0, 5, 0, 5)
                        };
                        ShiftBatchesPanel.Children.Add(noShiftText);
                    });
                    AddLog("✅ Shift Batches - No active shift");
                    return;
                }
                
                using var context = new ProductionDbContext();
                
                // Vardiya başlangıcından şimdiye kadar olan batch'lar
                var shiftStart = _shiftStartTime.Value;
                var shiftEnd = DateTime.UtcNow;
                
                // Mixer1 batch'ları - CompletedAt null ise OccurredAt kullan
                var mixer1Batches = await context.ConcreteBatches
                    .Include(b => b.Cements)
                    .Include(b => b.Aggregates)
                    .Include(b => b.Admixtures)
                    .Where(b => b.Status == "Tamamlandı" && 
                               ((b.CompletedAt.HasValue && b.CompletedAt >= shiftStart && b.CompletedAt <= shiftEnd) ||
                                (!b.CompletedAt.HasValue && b.OccurredAt >= shiftStart && b.OccurredAt <= shiftEnd)))
                    .OrderByDescending(b => b.CompletedAt ?? b.OccurredAt)
                    .ToListAsync();
                
                // Mixer2 batch'ları - CompletedAt null ise OccurredAt kullan
                var mixer2Batches = await context.ConcreteBatch2s
                    .Include(b => b.Cements)
                    .Include(b => b.Aggregates)
                    .Include(b => b.Admixtures)
                    .Where(b => b.Status == "Tamamlandı" && 
                               ((b.CompletedAt.HasValue && b.CompletedAt >= shiftStart && b.CompletedAt <= shiftEnd) ||
                                (!b.CompletedAt.HasValue && b.OccurredAt >= shiftStart && b.OccurredAt <= shiftEnd)))
                    .OrderByDescending(b => b.CompletedAt ?? b.OccurredAt)
                    .ToListAsync();
                
                Dispatcher.Invoke(() =>
                {
                    // Mixer1 başlığı
                    var mixer1Header = new TextBlock
                    {
                        Text = $"🏗️ Mixer1 - {mixer1Batches.Count} batches",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.DarkBlue),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    ShiftBatchesPanel.Children.Add(mixer1Header);
                    
                    // Mixer1 batch detayları
                    foreach (var batch in mixer1Batches.Take(10)) // İlk 10 batch'ı göster
                    {
                        var batchPanel = CreateBatchDetailPanel(batch, "M1");
                        ShiftBatchesPanel.Children.Add(batchPanel);
                    }
                    
                    if (mixer1Batches.Count > 10)
                    {
                        var moreText = new TextBlock
                        {
                            Text = $"... and {mixer1Batches.Count - 10} more batches",
                            FontSize = 10,
                            FontStyle = FontStyles.Italic,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(20, 2, 0, 5)
                        };
                        ShiftBatchesPanel.Children.Add(moreText);
                    }
                    
                    // Mixer2 başlığı
                    var mixer2Header = new TextBlock
                    {
                        Text = $"🏗️ Mixer2 - {mixer2Batches.Count} batches",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.DarkGreen),
                        Margin = new Thickness(0, 10, 0, 5)
                    };
                    ShiftBatchesPanel.Children.Add(mixer2Header);
                    
                    // Mixer2 batch detayları
                    foreach (var batch in mixer2Batches.Take(10)) // İlk 10 batch'ı göster
                    {
                        var batchPanel = CreateBatch2DetailPanel(batch, "M2");
                        ShiftBatchesPanel.Children.Add(batchPanel);
                    }
                    
                    if (mixer2Batches.Count > 10)
                    {
                        var moreText = new TextBlock
                        {
                            Text = $"... and {mixer2Batches.Count - 10} more batches",
                            FontSize = 10,
                            FontStyle = FontStyles.Italic,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(20, 2, 0, 5)
                        };
                        ShiftBatchesPanel.Children.Add(moreText);
                    }
                });
                
                AddLog($"✅ Shift Batches yüklendi - M1: {mixer1Batches.Count}, M2: {mixer2Batches.Count}");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Shift Batches yükleme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Mixer1 batch detay paneli oluştur
        /// </summary>
        private StackPanel CreateBatchDetailPanel(ConcreteBatch batch, string mixerPrefix)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(10, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 255))
            };
            
            // Batch ID ve zaman
            var batchInfo = new TextBlock
            {
                Text = $"{mixerPrefix} #{batch.Id} - {batch.OccurredAtLocalFull}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            panel.Children.Add(batchInfo);
            
            // Çimento detayları
            if (batch.Cements?.Any() == true)
            {
                var cementText = new TextBlock
                {
                    Text = $"Cement: {string.Join(", ", batch.Cements.Select(c => $"{c.AliasName} {c.Kg:F0}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(cementText);
            }
            
            // Agrega detayları
            if (batch.Aggregates?.Any() == true)
            {
                var aggregateText = new TextBlock
                {
                    Text = $"Aggregate: {string.Join(", ", batch.Aggregates.Select(a => $"{a.AliasName} {a.Kg:F0}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(aggregateText);
            }
            
            // Katkı detayları
            if (batch.Admixtures?.Any() == true)
            {
                var admixtureText = new TextBlock
                {
                    Text = $"Admixture: {string.Join(", ", batch.Admixtures.Select(a => $"{a.AliasName} {a.ChemicalKg:F1}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(admixtureText);
            }
            
            return panel;
        }
        
        /// <summary>
        /// Mixer2 batch detay paneli oluştur
        /// </summary>
        private StackPanel CreateBatch2DetailPanel(ConcreteBatch2 batch, string mixerPrefix)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(10, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 128, 0))
            };
            
            // Batch ID ve zaman
            var batchInfo = new TextBlock
            {
                Text = $"{mixerPrefix} #{batch.Id} - {batch.OccurredAtLocalFull}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            panel.Children.Add(batchInfo);
            
            // Çimento detayları
            if (batch.Cements?.Any() == true)
            {
                var cementText = new TextBlock
                {
                    Text = $"Cement: {string.Join(", ", batch.Cements.Select(c => $"{c.AliasName} {c.Kg:F0}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(cementText);
            }
            
            // Agrega detayları
            if (batch.Aggregates?.Any() == true)
            {
                var aggregateText = new TextBlock
                {
                    Text = $"Aggregate: {string.Join(", ", batch.Aggregates.Select(a => $"{a.AliasName} {a.Kg:F0}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(aggregateText);
            }
            
            // Katkı detayları
            if (batch.Admixtures?.Any() == true)
            {
                var admixtureText = new TextBlock
                {
                    Text = $"Admixture: {string.Join(", ", batch.Admixtures.Select(a => $"{a.AliasName} {a.ChemicalKg:F1}kg"))}",
                    FontSize = 10,
                    Margin = new Thickness(5, 0, 0, 1)
                };
                panel.Children.Add(admixtureText);
            }
            
            return panel;
        }

        private async Task LoadCementSilosOnly()
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Önce test silolarını temizle
                await CleanupTestSilos(context);
                
                // Sadece mevcut siloları yükle, yeniden oluşturma
                var silos = context.CementSilos
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SiloNumber)
                    .ToList();
                
                // Eğer hiç silo yoksa, o zaman oluştur
                if (!silos.Any())
                {
                    AddLog("Hiç silo bulunamadı, varsayılan silolar oluşturuluyor...");
                    await EnsureDefaultSilos(context);
                    
                    // Tekrar yükle
                    silos = context.CementSilos
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.SiloNumber)
                        .ToList();
                }
                
                Dispatcher.Invoke(() =>
                {
                    CementSilosContainer.ItemsSource = silos;
                });
                
                AddLog($"Silo verileri yüklendi: {silos.Count} adet");
            }
            catch (Exception ex)
            {
                AddLog($"{_localizationService.GetString("ConcreteReporting.SiloLoadError", "Çimento siloları yükleme hatası")}: {ex.Message}");
            }
        }

        private Task RefreshCementSilos()
        {
            return Task.Run(async () =>
        {
            try
            {
                    using var context = new ProductionDbContext();
                    
                    // 3 sabit silo oluştur (eğer yoksa)
                    await EnsureDefaultSilos(context);
                    
                    var silos = context.CementSilos
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.SiloNumber)
                        .ToList();
                    
                    Dispatcher.Invoke(() =>
                    {
                        CementSilosContainer.ItemsSource = silos;
                    });
            }
            catch (Exception ex)
            {
                    AddLog($"{_localizationService.GetString("ConcreteReporting.SiloLoadError", "Çimento siloları yükleme hatası")}: {ex.Message}");
            }
            });
        }

        private async Task EnsureDefaultSilos(ProductionDbContext context)
        {
            try
            {
                // Mevcut siloları kontrol et
                var existingSilos = await context.CementSilos.ToListAsync();
                
                // Her zaman raporlama sistemine uygun silolar oluştur
                AddLog("Raporlama sistemine uygun silolar oluşturuluyor...");
                await UpdateSiloNamesForReporting(context, existingSilos);
            }
            catch (Exception ex)
            {
                AddLog($"Silo kontrolü sırasında hata: {ex.Message}");
            }
        }

        private async Task UpdateSiloNamesForReporting(ProductionDbContext context, List<CementSilo> existingSilos)
        {
            try
            {
                // Eğer hiç silo yoksa, sadece o zaman oluştur
                if (!existingSilos.Any())
                {
                    AddLog("Hiç silo bulunamadı, varsayılan silolar oluşturuluyor...");
                    
                    // 3 sabit silo oluştur - Raporlama sistemine uygun (ID'ler sabit)
                    var newSilos = new List<CementSilo>
                    {
                        new CementSilo
                        {
                            Id = 1, // Sabit ID - Sistem bağlantısı için kritik
                            SiloNumber = 1,
                            CementType = "Standart çimento",
                            Capacity = 50000,
                            CurrentAmount = 25000,
                            IsActive = true,
                            LastRefillDate = DateTime.Now.AddDays(-5),
                            LastUpdated = DateTime.Now
                        },
                        new CementSilo
                        {
                            Id = 2, // Sabit ID - Sistem bağlantısı için kritik
                            SiloNumber = 2,
                            CementType = "Siyah çimento",
                            Capacity = 50000,
                            CurrentAmount = 35000,
                            IsActive = true,
                            LastRefillDate = DateTime.Now.AddDays(-3),
                            LastUpdated = DateTime.Now
                        },
                        new CementSilo
                        {
                            Id = 3, // Sabit ID - Sistem bağlantısı için kritik
                            SiloNumber = 3,
                            CementType = "Beyaz çimento",
                            Capacity = 50000,
                            CurrentAmount = 15000,
                            IsActive = true,
                            LastRefillDate = DateTime.Now.AddDays(-7),
                            LastUpdated = DateTime.Now
                        }
                    };

                    context.CementSilos.AddRange(newSilos);
                    await context.SaveChangesAsync();
                    AddLog("3 sabit silo oluşturuldu: ID:1 Standart çimento, ID:2 Siyah çimento, ID:3 Beyaz çimento");
                    
                    // Test geçmiş verileri ekle
                    await AddTestHistoryData(context, newSilos);
                }
                else
                {
                    AddLog($"Mevcut silolar korundu: {existingSilos.Count} adet");
                    
                    // Test çimento silolarını sil (test, Test Çimento, vb.)
                    var testSilos = existingSilos.Where(s => 
                        s.CementType.ToLower().Contains("test") || 
                        s.CementType.Contains("Test Çimento") ||
                        s.CementType.Contains("Test çimento")).ToList();
                    if (testSilos.Any())
                    {
                        context.CementSilos.RemoveRange(testSilos);
                        await context.SaveChangesAsync();
                        AddLog($"{testSilos.Count} test çimento silo silindi.");
                    }
                    
                    // Mevcut siloları raporlama sistemine uygun adlandır (sadece isim güncelle)
                    bool updated = false;
                    
                    // Silo 1: Standart çimento
                    var silo1 = existingSilos.FirstOrDefault(s => s.SiloNumber == 1);
                    if (silo1 != null && silo1.CementType != "Standart çimento")
                    {
                        silo1.CementType = "Standart çimento";
                        updated = true;
                        AddLog("Silo 1 adlandırıldı: Standart çimento");
                    }
                    
                    // Silo 2: Siyah çimento
                    var silo2 = existingSilos.FirstOrDefault(s => s.SiloNumber == 2);
                    if (silo2 != null && silo2.CementType != "Siyah çimento")
                    {
                        silo2.CementType = "Siyah çimento";
                        updated = true;
                        AddLog("Silo 2 adlandırıldı: Siyah çimento");
                    }
                    
                    // Silo 3: Beyaz çimento
                    var silo3 = existingSilos.FirstOrDefault(s => s.SiloNumber == 3);
                    if (silo3 != null && silo3.CementType != "Beyaz çimento")
                    {
                        silo3.CementType = "Beyaz çimento";
                        updated = true;
                        AddLog("Silo 3 adlandırıldı: Beyaz çimento");
                    }
                    
                    if (updated)
                    {
                        await context.SaveChangesAsync();
                        AddLog("Silo adlandırmaları raporlama sistemine uygun olarak güncellendi.");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Silo adlandırma hatası: {ex.Message}");
            }
        }

        private async Task CleanupTestSilos(ProductionDbContext context)
            {
                try
                {
                // Test silolarını bul ve sil
                var testSilos = context.CementSilos
                    .Where(s => s.CementType.ToLower().Contains("test") || 
                               s.CementType.Contains("Test Çimento") ||
                               s.CementType.Contains("Test çimento") ||
                               s.CementType.Contains("test çimento"))
                    .ToList();
                
                if (testSilos.Any())
                {
                    // Test silolarına ait refill ve consumption kayıtlarını da sil
                    var testSiloIds = testSilos.Select(s => s.Id).ToList();
                    
                    var testRefills = context.CementRefills
                        .Where(r => testSiloIds.Contains(r.SiloId))
                        .ToList();
                    
                    var testConsumptions = context.CementConsumptions
                        .Where(c => testSiloIds.Contains(c.SiloId))
                        .ToList();
                    
                    // Test kayıtlarını sil
                    if (testRefills.Any())
                    {
                        context.CementRefills.RemoveRange(testRefills);
                        AddLog($"{testRefills.Count} test refill kaydı silindi");
                    }
                    
                    if (testConsumptions.Any())
                    {
                        context.CementConsumptions.RemoveRange(testConsumptions);
                        AddLog($"{testConsumptions.Count} test consumption kaydı silindi");
                    }
                    
                    // Test silolarını sil
                    context.CementSilos.RemoveRange(testSilos);
                    await context.SaveChangesAsync();
                    
                    AddLog($"{testSilos.Count} test çimento silo silindi");
                }
                }
                catch (Exception ex)
                {
                AddLog($"Test silo temizleme hatası: {ex.Message}");
            }
        }

        private async Task AddTestHistoryData(ProductionDbContext context, List<CementSilo> silos)
            {
                try
                {
                // Test doldurma geçmişi ekle
                var testRefills = new List<CementRefill>();
                foreach (var silo in silos)
                {
                    // Son 5 gün için doldurma geçmişi
                    for (int i = 0; i < 5; i++)
                    {
                        testRefills.Add(new CementRefill
                        {
                            SiloId = silo.Id,
                            AddedAmount = 5000 + (i * 1000),
                            PreviousAmount = silo.CurrentAmount - (5000 + (i * 1000)),
                            NewAmount = silo.CurrentAmount,
                            RefilledAt = DateTime.Now.AddDays(-i),
                            OperatorName = $"Operatör {i + 1}",
                            Notes = $"Test doldurma {i + 1}"
                        });
                    }
                }

                // Test tüketim geçmişi ekle
                var testConsumptions = new List<CementConsumption>();
                foreach (var silo in silos)
                {
                    // Son 3 gün için tüketim geçmişi
                    for (int i = 0; i < 3; i++)
                    {
                        testConsumptions.Add(new CementConsumption
                        {
                            SiloId = silo.Id,
                            ConsumedAmount = 2000 + (i * 500),
                            RemainingAmount = silo.CurrentAmount - (2000 + (i * 500)),
                            ConsumedAt = DateTime.Now.AddDays(-i),
                            BatchId = 100 + i,
                            MixerId = 1,
                            ConsumptionType = "Production",
                            Notes = $"Test tüketim {i + 1}"
                        });
                    }
                }

                context.CementRefills.AddRange(testRefills);
                context.CementConsumptions.AddRange(testConsumptions);
                await context.SaveChangesAsync();

                AddLog("Test geçmiş verileri eklendi.");
                }
                catch (Exception ex)
                {
                AddLog($"Test geçmiş verileri eklenirken hata: {ex.Message}");
                }
        }

        private async void ViewSiloDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int siloId)
                {
                    using var context = new ProductionDbContext();
                    var silo = await context.CementSilos.FindAsync(siloId);
                    
                    if (silo != null)
                    {
                        // Silo detayları dialog'u oluştur
                        var dialog = new Window
                        {
                            Title = $"Silo {silo.SiloNumber} - {silo.CementType} Details",
                            Width = 800,
                            Height = 600,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this
                        };

                        var mainGrid = new Grid();
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                        // Sekme kontrolü
                        var tabControl = new TabControl
                        {
                            Margin = new Thickness(10)
                        };

                        // Silo Information Tab
                        var siloInfoTab = new TabItem
                        {
                            Header = "Silo Information"
                        };

                        var siloInfoGrid = new Grid();
                        siloInfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        siloInfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        siloInfoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                        // Silo information panel
                        var siloInfoPanel = new GroupBox
                        {
                            Header = "Silo Information",
                            Margin = new Thickness(10),
                            FontSize = 14,
                            FontWeight = FontWeights.Bold
                        };

                        var siloInfoContentGrid = new Grid();
                        siloInfoContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        siloInfoContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var leftPanel = new StackPanel { Margin = new Thickness(10) };
                        var rightPanel = new StackPanel { Margin = new Thickness(10) };

                        leftPanel.Children.Add(new TextBlock { Text = $"Silo Number: {silo.SiloNumber}", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });
                        leftPanel.Children.Add(new TextBlock { Text = $"Cement Type: {silo.CementType}", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });
                        leftPanel.Children.Add(new TextBlock { Text = $"Capacity: {silo.Capacity:N0} kg", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });

                        rightPanel.Children.Add(new TextBlock { Text = $"Current Amount: {silo.CurrentAmount:N0} kg", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });
                        rightPanel.Children.Add(new TextBlock { Text = $"Fill Level: {silo.FillPercentage:F1}%", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });
                        rightPanel.Children.Add(new TextBlock { Text = $"Last Refill: {silo.LastRefillDate:dd.MM.yyyy HH:mm}", FontSize = 12, Margin = new Thickness(0, 5, 0, 5) });

                        Grid.SetColumn(leftPanel, 0);
                        Grid.SetColumn(rightPanel, 1);
                        siloInfoContentGrid.Children.Add(leftPanel);
                        siloInfoContentGrid.Children.Add(rightPanel);

                        siloInfoPanel.Content = siloInfoContentGrid;
                        Grid.SetRow(siloInfoPanel, 0);
                        siloInfoGrid.Children.Add(siloInfoPanel);

                        // Edit button
                        var editButton = new Button
                        {
                            Content = "✏️ Edit",
                            Width = 100,
                            Height = 30,
                            Margin = new Thickness(10),
                            Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };

                        editButton.Click += async (s, e) =>
                        {
                            await EditSiloDetails(silo, context, dialog);
                        };

                        Grid.SetRow(editButton, 1);
                        siloInfoGrid.Children.Add(editButton);

                        // Silo visualization
                        var siloVisualPanel = new GroupBox
                        {
                            Header = "Silo Visualization",
                            Margin = new Thickness(10),
                            FontSize = 14,
                            FontWeight = FontWeights.Bold
                        };

                        var siloControl = new CementSiloControl
                        {
                            FillPercent = silo.FillPercentage,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(20)
                        };

                        siloVisualPanel.Content = siloControl;
                        Grid.SetRow(siloVisualPanel, 2);
                        siloInfoGrid.Children.Add(siloVisualPanel);

                        siloInfoTab.Content = siloInfoGrid;
                        tabControl.Items.Add(siloInfoTab);

                        // Refill History Tab
                        var refillHistoryTab = new TabItem
                        {
                            Header = "Refill History"
                        };

                        var refillDataGrid = new DataGrid
                        {
                            AutoGenerateColumns = false,
                            CanUserAddRows = false,
                            CanUserDeleteRows = false,
                            IsReadOnly = false,
                            Margin = new Thickness(10)
                        };

                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = new System.Windows.Data.Binding("RefilledAt"), Width = 150, IsReadOnly = true });
                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "Added (kg)", Binding = new System.Windows.Data.Binding("AddedAmount"), Width = 100, IsReadOnly = true });
                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "Previous (kg)", Binding = new System.Windows.Data.Binding("PreviousAmount"), Width = 100, IsReadOnly = true });
                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "New (kg)", Binding = new System.Windows.Data.Binding("NewAmount"), Width = 100, IsReadOnly = true });
                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "Operator", Binding = new System.Windows.Data.Binding("OperatorName"), Width = 100, IsReadOnly = true });
                        refillDataGrid.Columns.Add(new DataGridTextColumn { Header = "Description", Binding = new System.Windows.Data.Binding("Notes"), Width = 200, IsReadOnly = false });

                        // Load refill history - Last 30 records + date filtering
                        var refills = await context.CementRefills
                            .Where(r => r.SiloId == siloId)
                            .OrderByDescending(r => r.RefilledAt)
                            .Take(30) // Son 30 kayıt
                            .ToListAsync();

                        refillDataGrid.ItemsSource = refills;
                        
                        // Tarih filtreleme kontrolleri ekle
                        var refillFilterPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(10, 5, 10, 10),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        var refillDateFromLabel = new TextBlock
                        {
                            Text = "Start:",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };

                        var refillDateFromPicker = new DatePicker
                        {
                            Width = 120,
                            Margin = new Thickness(0, 0, 10, 0),
                            SelectedDate = DateTime.Now.AddDays(-30) // Son 30 gün
                        };

                        var refillDateToLabel = new TextBlock
                        {
                            Text = "End:",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };

                        var refillDateToPicker = new DatePicker
                        {
                            Width = 120,
                            Margin = new Thickness(0, 0, 10, 0),
                            SelectedDate = DateTime.Now
                        };

                        var refillFilterButton = new Button
                        {
                            Content = "🔍 Filter",
                            Width = 80,
                            Height = 25,
                            Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                            Foreground = Brushes.White
                        };

                        refillFilterPanel.Children.Add(refillDateFromLabel);
                        refillFilterPanel.Children.Add(refillDateFromPicker);
                        refillFilterPanel.Children.Add(refillDateToLabel);
                        refillFilterPanel.Children.Add(refillDateToPicker);
                        refillFilterPanel.Children.Add(refillFilterButton);

                        // Filtreleme event handler'ı
                        refillFilterButton.Click += async (s, e) =>
                        {
                            try
                            {
                                var fromDate = refillDateFromPicker.SelectedDate ?? DateTime.Now.AddDays(-30);
                                var toDate = refillDateToPicker.SelectedDate ?? DateTime.Now;

                                var filteredRefills = await context.CementRefills
                                    .Where(r => r.SiloId == siloId && 
                                               r.RefilledAt >= fromDate && 
                                               r.RefilledAt <= toDate)
                                    .OrderByDescending(r => r.RefilledAt)
                                    .Take(100) // Maksimum 100 kayıt
                                    .ToListAsync();

                                refillDataGrid.ItemsSource = filteredRefills;
                                AddLog($"📅 Refill history filtered: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy} ({filteredRefills.Count} records)");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"Refill history filtering error: {ex.Message}");
                            }
                        };

                        // Add save button for refill history
                        var refillSaveButton = new Button
                        {
                            Content = "💾 Save Descriptions",
                            Width = 150,
                            Height = 30,
                            Margin = new Thickness(10),
                            Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        
                        refillSaveButton.Click += async (s, e) =>
                        {
                            try
                            {
                                var modifiedRefills = refillDataGrid.ItemsSource as IEnumerable<CementRefill>;
                                if (modifiedRefills != null)
                                {
                                    foreach (var refill in modifiedRefills)
                                    {
                                        context.Entry(refill).State = EntityState.Modified;
                                    }
                                    await context.SaveChangesAsync();
                                    AddLog("Refill history descriptions updated.");
                                    MessageBox.Show("Descriptions saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLog($"Refill history update error: {ex.Message}");
                                MessageBox.Show($"Error occurred while saving descriptions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        };
                        
                        var refillStackPanel = new StackPanel();
                        refillStackPanel.Children.Add(refillFilterPanel);
                        refillStackPanel.Children.Add(refillDataGrid);
                        refillStackPanel.Children.Add(refillSaveButton);
                        
                        refillHistoryTab.Content = refillStackPanel;
                        tabControl.Items.Add(refillHistoryTab);

                        // Consumption History Tab
                        var consumptionHistoryTab = new TabItem
                        {
                            Header = "Consumption History"
                        };

                        var consumptionDataGrid = new DataGrid
                        {
                            AutoGenerateColumns = false,
                            CanUserAddRows = false,
                            CanUserDeleteRows = false,
                            IsReadOnly = true,
                            Margin = new Thickness(10)
                        };

                        consumptionDataGrid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = new System.Windows.Data.Binding("ConsumedAt"), Width = 150, IsReadOnly = true });
                        consumptionDataGrid.Columns.Add(new DataGridTextColumn { Header = "Consumed (kg)", Binding = new System.Windows.Data.Binding("ConsumedAmount"), Width = 100, IsReadOnly = true });
                        consumptionDataGrid.Columns.Add(new DataGridTextColumn { Header = "Remaining (kg)", Binding = new System.Windows.Data.Binding("RemainingAmount"), Width = 100, IsReadOnly = true });
                        consumptionDataGrid.Columns.Add(new DataGridTextColumn { Header = "Batch ID", Binding = new System.Windows.Data.Binding("BatchId"), Width = 80, IsReadOnly = true });
                        consumptionDataGrid.Columns.Add(new DataGridTextColumn { Header = "Mixer", Binding = new System.Windows.Data.Binding("MixerId"), Width = 80, IsReadOnly = true });

                        // Load consumption history - Last 30 records + date filtering
                        var consumptions = await context.CementConsumptions
                            .Where(c => c.SiloId == siloId)
                            .OrderByDescending(c => c.ConsumedAt)
                            .Take(30) // Son 30 kayıt
                            .ToListAsync();

                        consumptionDataGrid.ItemsSource = consumptions;

                        // Date filtering controls for consumption history
                        var consumptionFilterPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(10, 5, 10, 10),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        var consumptionDateFromLabel = new TextBlock
                        {
                            Text = "Start:",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };

                        var consumptionDateFromPicker = new DatePicker
                        {
                            Width = 120,
                            Margin = new Thickness(0, 0, 10, 0),
                            SelectedDate = DateTime.Now.AddDays(-30)
                        };

                        var consumptionDateToLabel = new TextBlock
                        {
                            Text = "End:",
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        };

                        var consumptionDateToPicker = new DatePicker
                        {
                            Width = 120,
                            Margin = new Thickness(0, 0, 10, 0),
                            SelectedDate = DateTime.Now
                        };

                        var consumptionFilterButton = new Button
                        {
                            Content = "🔍 Filter",
                            Width = 80,
                            Height = 25,
                            Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                            Foreground = Brushes.White
                        };

                        consumptionFilterPanel.Children.Add(consumptionDateFromLabel);
                        consumptionFilterPanel.Children.Add(consumptionDateFromPicker);
                        consumptionFilterPanel.Children.Add(consumptionDateToLabel);
                        consumptionFilterPanel.Children.Add(consumptionDateToPicker);
                        consumptionFilterPanel.Children.Add(consumptionFilterButton);

                        // Tüketim filtreleme event handler'ı
                        consumptionFilterButton.Click += async (s, e) =>
                        {
                            try
                            {
                                var fromDate = consumptionDateFromPicker.SelectedDate ?? DateTime.Now.AddDays(-30);
                                var toDate = consumptionDateToPicker.SelectedDate ?? DateTime.Now;

                                var filteredConsumptions = await context.CementConsumptions
                                    .Where(c => c.SiloId == siloId && 
                                               c.ConsumedAt >= fromDate && 
                                               c.ConsumedAt <= toDate)
                                    .OrderByDescending(c => c.ConsumedAt)
                                    .Take(100) // Maksimum 100 kayıt
                                    .ToListAsync();

                                consumptionDataGrid.ItemsSource = filteredConsumptions;
                                AddLog($"📅 Tüketim geçmişi filtrelendi: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy} ({filteredConsumptions.Count} kayıt)");
                            }
                            catch (Exception ex)
                            {
                                AddLog($"Tüketim geçmişi filtreleme hatası: {ex.Message}");
                            }
                        };

                        var consumptionStackPanel = new StackPanel();
                        consumptionStackPanel.Children.Add(consumptionFilterPanel);
                        consumptionStackPanel.Children.Add(consumptionDataGrid);

                        consumptionHistoryTab.Content = consumptionStackPanel;
                        tabControl.Items.Add(consumptionHistoryTab);

                        // Ana grid'e ekle
                        Grid.SetRow(tabControl, 1);
                        mainGrid.Children.Add(tabControl);

                        dialog.Content = mainGrid;
                        dialog.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"{_localizationService.GetString("ConcreteReporting.SiloDetailsError", "Silo detayları açma hatası")}: {ex.Message}");
            }
        }

        private async Task EditSiloDetails(CementSilo silo, ProductionDbContext context, Window parentDialog)
        {
            try
            {
                var editDialog = new Window
                {
                    Title = $"Silo {silo.SiloNumber} Düzenle",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = parentDialog,
                    ResizeMode = ResizeMode.CanResize
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cementTypeLabel = new Label { Content = "Çimento Türü:", Margin = new Thickness(10) };
                var cementTypeTextBox = new TextBox { Text = silo.CementType, Margin = new Thickness(10) };
                var capacityLabel = new Label { Content = "Kapasite (kg):", Margin = new Thickness(10) };
                var capacityTextBox = new TextBox { Text = silo.Capacity.ToString(), Margin = new Thickness(10) };
                var currentAmountLabel = new Label { Content = "Mevcut Miktar (kg):", Margin = new Thickness(10) };
                var currentAmountTextBox = new TextBox { Text = silo.CurrentAmount.ToString(), Margin = new Thickness(10) };

                Grid.SetRow(cementTypeLabel, 0);
                Grid.SetRow(cementTypeTextBox, 1);
                Grid.SetRow(capacityLabel, 2);
                Grid.SetRow(capacityTextBox, 3);
                Grid.SetRow(currentAmountLabel, 4);
                Grid.SetRow(currentAmountTextBox, 5);

                grid.Children.Add(cementTypeLabel);
                grid.Children.Add(cementTypeTextBox);
                grid.Children.Add(capacityLabel);
                grid.Children.Add(capacityTextBox);
                grid.Children.Add(currentAmountLabel);
                grid.Children.Add(currentAmountTextBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 20),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var okButton = new Button
                {
                    Content = "💾 Kaydet",
                    Width = 100,
                    Height = 40,
                    Margin = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };

                var cancelButton = new Button
                {
                    Content = "❌ İptal",
                    Width = 100,
                    Height = 40,
                    Margin = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetRow(grid, 0);
                Grid.SetRow(buttonPanel, 1);

                mainGrid.Children.Add(grid);
                mainGrid.Children.Add(buttonPanel);

                editDialog.Content = mainGrid;

                bool? result = null;
                okButton.Click += async (s, args) => 
                { 
                    try
                    {
                        if (double.TryParse(capacityTextBox.Text, out double capacity) &&
                            double.TryParse(currentAmountTextBox.Text, out double currentAmount))
                        {
                            silo.CementType = cementTypeTextBox.Text;
                            silo.Capacity = capacity;
                            silo.CurrentAmount = currentAmount;
                            silo.LastUpdated = DateTime.Now;

                            await context.SaveChangesAsync();
                            result = true;
                            editDialog.Close();
                        }
                        else
                        {
                            MessageBox.Show("Geçersiz değerler girdiniz.", "Hata", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Silo güncellenirken hata oluştu: {ex.Message}", "Hata", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                cancelButton.Click += (s, args) => { result = false; editDialog.Close(); };

                editDialog.ShowDialog();

                if (result == true)
                {
                    // Ana dialog'u yenile
                    parentDialog.Close();
                    ViewSiloDetails_Click(new Button { Tag = silo.Id }, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silo düzenleme hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefillSilo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int siloId)
                {
                    var cementSiloService = new CementSiloService();
                    var refillDialog = new CementRefillDialog(siloId, cementSiloService);
                    refillDialog.Owner = this;
                    if (refillDialog.ShowDialog() == true)
                    {
                        // Only refresh silo data, don't recreate
                        await LoadCementSilosOnly();
                        AddLog($"{_localizationService.GetString("ConcreteReporting.SiloRefilled", "Silo dolduruldu")} #{siloId}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"{_localizationService.GetString("ConcreteReporting.SiloRefillError", "Silo doldurma hatası")}: {ex.Message}");
            }
        }


        /// <summary>
        /// Üretim takip sekmesi metinlerini güncelle
        /// </summary>
        private void UpdateProductionTabTexts()
        {
            try
            {
                // PLC durum metni
                // if (PlcStatusText != null) // KALDIRILDI
                // {
                //     // PlcStatusText.Text // KALDIRILDI = _localizationService.GetString("ProductionReporting.PlcChecking", "PLC: Bağlantı Kontrol Ediliyor...");
                // }
                
                // Buton metinleri
                if (ToggleShiftButton != null)
                {
                    ToggleShiftButton.Content = _localizationService.GetString("ProductionReporting.StartShift", "Vardiyayı Başlat");
                }
                
                if (OpenShiftHistoryButton != null)
                {
                    OpenShiftHistoryButton.Content = _localizationService.GetString("ProductionReporting.ShiftHistory", "Vardiya Geçmişi");
                }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Üretim sekmesi metinleri güncelleme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Beton santrali sekmesi metinlerini güncelle
        /// </summary>
        private void UpdateConcreteTabTexts()
        {
            try
            {
                // Buton metinleri
                if (OpenConcreteReportingM1Button != null)
                {
                    OpenConcreteReportingM1Button.Content = _localizationService.GetString("ConcreteReporting.Mixer1Reporting", "🏗️ Mixer1 Raporlama");
                }
                
                if (OpenConcreteReportingM2Button != null)
                {
                    OpenConcreteReportingM2Button.Content = _localizationService.GetString("ConcreteReporting.Mixer2Reporting", "🏗️ Mixer2 Raporlama");
                }
                
                if (OpenGeneralReportsButton != null)
                {
                    OpenGeneralReportsButton.Content = _localizationService.GetString("ConcreteReporting.GeneralReports", "📊 General Reporting");
                }
                
                
                
                if (RefreshSilosButton != null)
                {
                    RefreshSilosButton.Content = _localizationService.GetString("ConcreteReporting.Refresh", "🔄 Yenile");
                }
                
                if (ConcreteInfoText != null)
                {
                    ConcreteInfoText.Text = _localizationService.GetString("ConcreteReporting.InfoText", "Beton santrali raporlama sistemi. Mixer1 ve Mixer2 için tek sayfadan işlem yapın.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Beton santrali sekmesi metinleri güncelleme hatası: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Timer'ları düzgün dispose et - memory leak önleme
                if (_productionTimer != null)
                {
                    _productionTimer.Stop();
                    _productionTimer.Tick -= ProductionTimer_Tick;
                    _productionTimer = null;
                }
                
                if (_logCleanupTimer != null)
                {
                    _logCleanupTimer.Stop();
                    _logCleanupTimer.Tick -= LogCleanupTimer_Tick;
                    _logCleanupTimer = null;
                }
                
                if (_concretePageTimer != null)
                {
                    _concretePageTimer.Stop();
                    _concretePageTimer.Tick -= ConcretePageTimer_Tick;
                    _concretePageTimer = null;
                }
                
                // Diğer kaynakları temizle
                _context?.Dispose();
                
                // Background M2DataWindow kaldırıldı - artık gerek yok
                
                // Sadece gerekli durumlarda GC çağır
                if (_globalLogMessages.Count > 100)
                {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnClosed hatası: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        public void RefreshProductionNotes()
        {
            try
            {
                ProductionNotesPanel.Children.Clear();
                using (var context = new ProductionDbContext())
                {
                    // Vardiya aktif değilse notları gösterme
                    if (_currentShiftId == 0)
                    {
                        // Vardiya aktif değilse son vardiyanın notlarını göster
                        var lastShift = context.Shifts
                            .Where(s => s.IsActive == false)
                            .OrderByDescending(s => s.CreatedAt)
                            .FirstOrDefault();
                        
                        if (lastShift != null)
                        {
                            var lastShiftNotes = context.ProductionNotes
                                .Where(n => n.ShiftId == lastShift.Id)
                                .OrderByDescending(n => n.CreatedAt)
                                .ToList();
                            
                            if (lastShiftNotes.Any())
                            {
                                NoNotesText.Visibility = Visibility.Collapsed;
                                
                                foreach (var note in lastShiftNotes)
                                {
                                    var noteBorder = new Border
                                    {
                                        Background = Brushes.White,
                                        CornerRadius = new CornerRadius(4),
                                        Padding = new Thickness(8),
                                        Margin = new Thickness(0, 0, 0, 5)
                                    };
                                    
                                    var noteStack = new StackPanel();
                                    
                                    var noteText = new TextBlock
                                    {
                                        Text = note.Note,
                                        FontSize = 11,
                                        Foreground = Brushes.Black,
                                        TextWrapping = TextWrapping.Wrap
                                    };
                                    
                                    var infoText = new TextBlock
                                    {
                                        Text = $"Tarih: {note.CreatedAt:HH:mm} | Operatör: {note.CreatedBy} | Fire: {note.FireProductCount}",
                                        FontSize = 9,
                                        Foreground = Brushes.Gray,
                                        Margin = new Thickness(0, 4, 0, 0)
                                    };
                                    
                                    noteStack.Children.Add(noteText);
                                    noteStack.Children.Add(infoText);
                                    noteBorder.Child = noteStack;
                                    
                                    ProductionNotesPanel.Children.Add(noteBorder);
                                }
                                
                                // Fire ürün toplamını güncelle
                                var lastShiftTotalFire = lastShiftNotes.Sum(n => n.FireProductCount);
                                FireProductText.Text = lastShiftTotalFire.ToString();
                            }
                            else
                            {
                                NoNotesText.Visibility = Visibility.Visible;
                                FireProductText.Text = "0";
                            }
                        }
                        else
                        {
                            NoNotesText.Visibility = Visibility.Visible;
                            FireProductText.Text = "0";
                        }
                        return;
                    }
                    
                    var notes = context.ProductionNotes
                        .Where(n => n.ShiftId == _currentShiftId) // Sadece aktif vardiyanın notları
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();
                    
                    if (notes.Any())
                    {
                        NoNotesText.Visibility = Visibility.Collapsed;
                        
                        foreach (var note in notes)
                        {
                            var noteBorder = new Border
                            {
                                Background = Brushes.White,
                                CornerRadius = new CornerRadius(4),
                                Padding = new Thickness(8),
                                Margin = new Thickness(0, 0, 0, 5)
                            };
                            
                            var noteStack = new StackPanel();
                            
                            var noteText = new TextBlock
                            {
                                Text = note.Note,
                                FontSize = 11,
                                Foreground = Brushes.Black,
                                TextWrapping = TextWrapping.Wrap
                            };
                            
                            var infoText = new TextBlock
                            {
                                Text = $"Tarih: {note.CreatedAt:HH:mm} | Operatör: {note.CreatedBy} | Fire: {note.FireProductCount}",
                                FontSize = 9,
                                Foreground = Brushes.Gray,
                                Margin = new Thickness(0, 4, 0, 0)
                            };
                            
                            noteStack.Children.Add(noteText);
                            noteStack.Children.Add(infoText);
                            noteBorder.Child = noteStack;
                            
                            ProductionNotesPanel.Children.Add(noteBorder);
                        }
                    }
                    else
                    {
                        NoNotesText.Visibility = Visibility.Visible;
                    }
                    
                    // Fire ürün toplamını güncelle
                    var totalFire = notes.Sum(n => n.FireProductCount);
                    FireProductText.Text = totalFire.ToString();
                }
            }
            catch (Exception ex)
            {
                AddLog($"Üretim notları yenileme hatası: {ex.Message}");
            }
        }

        #region Global Log System

        /// <summary>
        /// Tüm log mesajlarını döndür
        /// </summary>
        public List<string> GetAllLogMessages()
        {
            lock (_logLock)
            {
                return new List<string>(_globalLogMessages);
            }
        }

        /// <summary>
        /// Log mesajlarını temizle
        /// </summary>
        public void ClearLog()
        {
            lock (_logLock)
            {
                _globalLogMessages.Clear();
            }
        }

        /// <summary>
        /// Log mesajlarını dosyaya kaydet
        /// </summary>
        public void SaveLogToFile(string filePath)
        {
            lock (_logLock)
            {
                System.IO.File.WriteAllLines(filePath, _globalLogMessages);
            }
        }

        /// <summary>
        /// LogWindow açıldığında çağrılır
        /// </summary>
        public void OnLogWindowOpened()
        {
            _logWindowOpen = true;
            System.Diagnostics.Debug.WriteLine("[MainWindow] LogWindow açıldı - Global log sistemi aktif");
        }

        /// <summary>
        /// LogWindow kapandığında çağrılır
        /// </summary>
        public void OnLogWindowClosed()
        {
            _logWindowOpen = false;
            System.Diagnostics.Debug.WriteLine("[MainWindow] LogWindow kapandı - Global log sistemi pasif");
        }

        #endregion

        #region PLC Data Service

        /// <summary>
        /// PLC veri servisini başlat
        /// </summary>
        private async void StartPlcDataService()
        {
            try
            {
                _plcDataService = new PlcDataService();
                
                // ❌ Event handler'ları KALDIRDIM - zaten MainWindow constructor'da (satır 505) subscribe edilmiş!
                // Çift subscribe olursa her event 2 kere tetiklenir ve 2 batch açılır!
                // _plcDataService.DataChanged += OnPlcDataChanged;
                // _plcDataService.LogMessage += OnPlcLogMessage;
                
                // Servisi başlat
                var started = await _plcDataService.StartAsync();
                if (started)
                {
                    // PLC servisi başlatıldı (log kaldırıldı)
                }
                else
                {
                    // PLC servisi başlatılamadı (log kaldırıldı)
                }
            }
            catch (Exception ex)
            {
                // PLC servisi başlatma hatası (log kaldırıldı)
                DetailedLogger.LogError("PLC servisi başlatma hatası", ex);
            }
        }

        #endregion

        #region Batch Tracking

        /// <summary>
        /// Birleşik Batch Takip penceresini aç
        /// </summary>
        private void OpenBatchTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // PlcDataService'i inject et - Debug paneli için
                var batchWindow = new UnifiedBatchTrackingWindow();
                batchWindow.Owner = this;
                batchWindow.Show();
                AddLog("📊 Birleşik Batch Takip penceresi açıldı (PLC Debug Paneli Aktif)");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Batch Takip penceresi açma hatası: {ex.Message}");
                MessageBox.Show($"Batch Takip penceresi açma hatası:\n{ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // SimpleTestWindow kaldırıldı - artık kullanılmıyor

        /// <summary>
        /// PLC register değerini oku
        /// </summary>
        private double GetPlcValue(string register)
        {
            try
            {
                var plcData = GetPlcData();
                if (plcData.TryGetValue(register, out var data) && data.IsSuccess)
                {
                    return data.NumericValue;
                }
                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        #endregion

        #region Mixer1 Pigment ve Katkı Kayıt Fonksiyonları

        /// <summary>
        /// Mixer1 pigment verilerini kaydet (alias ile) - Bekleme Bunkerinde status'ta
        /// </summary>
        private async Task RecordMixer1PigmentData()
        {
            try
            {
                // Sadece "Bekleme Bunkerinde" status'taki batch'e pigment ekle
                if (!_beklemeBunkeriBatchId.HasValue) return;

                using var context = new ProductionDbContext();
                var batch = await context.ConcreteBatches.FindAsync(_beklemeBunkeriBatchId.Value);
                if (batch == null) return;

                // Pigment alias'larını yükle
                var pigmentAliases = await context.PigmentAliases
                    .Where(a => a.IsActive)
                    .ToDictionaryAsync(a => a.Slot, a => a.Name);

                // PLC verilerini al
                var plcData = GetPlcData();

                // Boya grup aktif mi? (H30.2)
                bool boyaGrupAktif = plcData.ContainsKey("H30.2") && plcData["H30.2"].Value;
                if (!boyaGrupAktif) return;

                // Boya aktif mi? (H30.10)
                bool boyaAktif = plcData.ContainsKey("H30.10") && plcData["H30.10"].Value;
                if (!boyaAktif) return;

                // Tartım ok sinyali var mı? (H30.3)
                bool tartimOk = plcData.ContainsKey("H30.3") && plcData["H30.3"].Value;
                if (!tartimOk) return;

                // KG değerini oku (DM208)
                double kg = GetPlcValue(REG_M1_PIGMENT1_KG);
                if (kg <= 0.1) return; // Eşik kontrolü

                // Alias ismi kullan
                var displayName = pigmentAliases.TryGetValue(1, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                    ? aliasName
                    : "Boya1";

                // Pigment kaydı oluştur
                var pigmentRecord = new ConcreteBatchPigment
                {
                    BatchId = batch.Id,
                    Slot = 1,
                    Name = displayName,
                    WeightKg = kg
                };
                context.ConcreteBatchPigments.Add(pigmentRecord);

                // Batch'e toplam pigment kg ekle
                batch.TotalPigmentKg += kg;

                await context.SaveChangesAsync();
                AddLog($"🎨 Mixer1: {displayName} kaydedildi: {kg}kg (Batch {batch.Id})");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 pigment kayıt hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Mixer1 katkı sinyali kontrolü - Katkı verilerini kaydet (Mixer2 gibi)
        /// </summary>
        private async Task CheckMixer1KatkiSignal(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                // Mixerde batch kontrolü
                if (!_mixerdeBatchId.HasValue) return;

                using var context = new ProductionDbContext();
                var batch = await context.ConcreteBatches.FindAsync(_mixerdeBatchId.Value);
                if (batch == null) return;

                // Yeni Mixerde batch'i katkı bekleyen listesine ekle (sadece daha önce kayıt yapılmamış olanlar)
                // 2 saniye bekleme kontrolü
                bool canRecord = true;
                if (_m1AdmixtureRecordTimes.ContainsKey(batch.Id))
                {
                    var timeSinceLastRecord = DateTime.Now - _m1AdmixtureRecordTimes[batch.Id];
                    if (timeSinceLastRecord.TotalSeconds < 2)
                    {
                        canRecord = false;
                        AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} 2 saniye bekleme süresinde - kayıt atlandı (Kalan: {2 - timeSinceLastRecord.TotalSeconds:F1}s)");
                    }
                }
                
                if (!_m1WaitingForAdmixtureBatchIds.Contains(batch.Id) && !_m1AdmixtureRecordedBatchIds.Contains(batch.Id) && canRecord)
                {
                    _m1WaitingForAdmixtureBatchIds.Add(batch.Id);
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} katkı bekleyen listesine eklendi");
                }
                else if (!canRecord)
                {
                    // 2 saniye bekleme süresinde - zaten log yazıldı
                }
                else if (_m1AdmixtureRecordedBatchIds.Contains(batch.Id))
                {
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} zaten kayıt edilmiş - atlandı");
                }
                else if (_m1WaitingForAdmixtureBatchIds.Contains(batch.Id))
                {
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} zaten bekleyen - atlandı");
                }

                // Artık Mixerde olmayan batch'leri katkı bekleyen listesinden çıkar
                var allBatchIds = new HashSet<int>();
                if (_mixerdeBatchId.HasValue) allBatchIds.Add(_mixerdeBatchId.Value);
                
                var toRemove = _m1WaitingForAdmixtureBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
                foreach (var id in toRemove)
                {
                    _m1WaitingForAdmixtureBatchIds.Remove(id);
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {id} katkı bekleyen listesinden çıkarıldı (statüs değişti)");
                }
                
                // Artık Mixerde olmayan batch'leri katkı kayıt edilen listesinden de çıkar
                var toRemoveFromRecorded = _m1AdmixtureRecordedBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
                foreach (var id in toRemoveFromRecorded)
                {
                    _m1AdmixtureRecordedBatchIds.Remove(id);
                    _m1AdmixtureRecordTimes.Remove(id);
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {id} katkı kayıt edilen listesinden çıkarıldı (statüs değişti)");
                }

                // Katkı bekleyen batch'ler varsa katkı kayıt işlemi yap
                if (_m1WaitingForAdmixtureBatchIds.Count > 0)
                {
                    await RecordMixer1AdmixtureData();
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 katkı sinyali kontrol hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Mixer1 katkı verilerini kaydet (alias ile) - Mixer2 gibi bekleyen batch listesi yönetimi
        /// </summary>
        private async Task RecordMixer1AdmixtureData()
        {
            try
            {
                // SADECE bekleyen batch'lere katkı verilerini ekle
                if (_m1WaitingForAdmixtureBatchIds.Count == 0)
                {
                    AddLog("🧪 DEBUG: Mixer1 - Bekleyen batch yok - katkı kayıt atlandı");
                    return;
                }

                using var context = new ProductionDbContext();
                var waitingBatches = await context.ConcreteBatches
                    .Where(b => _m1WaitingForAdmixtureBatchIds.Contains(b.Id))
                    .Include(b => b.Admixtures)
                    .ToListAsync();

                if (waitingBatches.Count == 0)
                {
                    AddLog("🧪 DEBUG: Mixer1 - Bekleyen batch'ler bulunamadı - katkı kayıt atlandı");
                    return;
                }

                foreach (var batch in waitingBatches)
                {
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} için katkı kayıt işlemi başlatılıyor");

                // Katkı alias'larını yükle
                var admixtureAliases = await context.AdmixtureAliases
                    .Where(a => a.IsActive)
                    .ToDictionaryAsync(a => a.Slot, a => a.Name);

                // Tüm aktif katkıları topla
                var activeAdmixtures = new List<(int Slot, string Name, double ChemicalKg, double WaterKg)>();

                for (int i = 1; i <= 4; i++)
                {
                    var chemicalReg = i switch
                    {
                        1 => REG_M1_KATKI1_CHEMICAL_KG,
                        2 => REG_M1_KATKI2_CHEMICAL_KG,
                        3 => REG_M1_KATKI3_CHEMICAL_KG,
                        4 => REG_M1_KATKI4_CHEMICAL_KG,
                        _ => ""
                    };
                    
                    var waterReg = i switch
                    {
                        1 => REG_M1_KATKI1_WATER_KG,
                        2 => REG_M1_KATKI2_WATER_KG,
                        3 => REG_M1_KATKI3_WATER_KG,
                        4 => REG_M1_KATKI4_WATER_KG,
                        _ => ""
                    };

                    var chemicalKg = GetPlcValue(chemicalReg) / 10.0;
                    var waterKg = GetPlcValue(waterReg) / 10.0;
                    var totalKg = chemicalKg + waterKg;

                    if (totalKg > 0.1) // Eşik kontrolü
                    {
                        // Batch'de zaten bu slot'ta katkı var mı kontrol et
                        var existingAdmixture = batch.Admixtures.FirstOrDefault(a => a.Slot == i);
                        if (existingAdmixture != null)
                        {
                            AddLog($"🧪 DEBUG: Mixer1 - Batch {batch.Id} için Katkı{i} zaten kayıtlı ({existingAdmixture.ChemicalKg + existingAdmixture.WaterKg}kg) - tekrar kayıt atlandı");
                            continue;
                        }

                        var displayName = admixtureAliases.TryGetValue((short)i, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                            ? aliasName
                            : $"Katki{i}";

                        AddLog($"🧪 DEBUG: Mixer1 - Katkı{i} kaydedilecek - İsim: {displayName}");
                        activeAdmixtures.Add((i, displayName, chemicalKg, waterKg));
                    }
                }

                // Eğer aktif katkı varsa, hepsini tek seferde kaydet
                AddLog($"🧪 DEBUG: Mixer1 - Toplam aktif katkı sayısı: {activeAdmixtures.Count}");
                if (activeAdmixtures.Count > 0)
                {
                    foreach (var (slot, name, chemicalKg, waterKg) in activeAdmixtures)
                    {
                        var admixture = new ConcreteBatchAdmixture
                        {
                            BatchId = batch.Id,
                            Slot = (short)slot,
                            Name = name,
                            ChemicalKg = chemicalKg,
                            WaterKg = waterKg
                        };
                        batch.Admixtures.Add(admixture);
                        batch.TotalAdmixtureKg += (chemicalKg + waterKg);
                    }

                    await context.SaveChangesAsync();

                    var totalChemical = activeAdmixtures.Sum(a => a.ChemicalKg);
                    var totalWater = activeAdmixtures.Sum(a => a.WaterKg);
                    var totalTotal = totalChemical + totalWater;

                    AddLog($"🧪 Mixer1 - Katkı kayıt tamamlandı: {activeAdmixtures.Count} katkı, Kimyasal={totalChemical}kg, Su={totalWater}kg, Toplam={totalTotal}kg (Batch {batch.Id})");
                }
                else
                {
                    AddLog("🧪 DEBUG: Mixer1 - Hiç aktif katkı bulunamadı - kayıt yapılmadı");
                }
                
                // Kayıt yapılan batch'leri bekleyen listesinden çıkar
                var recordedBatchIds = _m1WaitingForAdmixtureBatchIds.ToList();
                foreach (var batchId in recordedBatchIds)
                {
                    _m1WaitingForAdmixtureBatchIds.Remove(batchId);
                    _m1AdmixtureRecordedBatchIds.Add(batchId);
                    _m1AdmixtureRecordTimes[batchId] = DateTime.Now;
                    AddLog($"🧪 DEBUG: Mixer1 - Batch {batchId} katkı kayıt edilen listeye eklendi");
                }
                AddLog($"🧪 DEBUG: Mixer1 - Katkı kaydedildi - kayıt yapılan batch'ler bekleyen listesinden çıkarıldı. Kayıt edilen batch sayısı: {_m1AdmixtureRecordedBatchIds.Count}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer1 katkı kayıt hatası: {ex.Message}");
            }
        }

        #endregion

        #region LogWindow Support Methods

        /// <summary>
        /// Log mesajlarını döndür (LogWindow için)
        /// </summary>
        public List<string> GetLogMessages()
        {
            lock (_logLock)
            {
                return new List<string>(_globalLogMessages);
            }
        }

        /// <summary>
        /// Log mesajlarını temizle (LogWindow için)
        /// </summary>
        public void ClearLogMessages()
        {
            lock (_logLock)
            {
                _globalLogMessages.Clear();
            }
        }

        #endregion

        #region Window Events

        private async void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                AddLog("🔄 Uygulama kapatılıyor...");
                
                
                // Timer'ları durdur
                _productionTimer?.Stop();
                _logCleanupTimer?.Stop();
                _concretePageTimer?.Stop();
                
                // PLC servisini durdur
                _plcDataService?.Stop();
                _plcDataService?.Dispose();
                
                
                // Context'i dispose et
                _context?.Dispose();
                
                AddLog("✅ Uygulama güvenli şekilde kapatıldı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Window_Closed hatası: {ex.Message}");
            }
        }

        #endregion

        #region Mixer2StatusBasedProcessor Integration

        /// <summary>
        /// Mixer2StatusBasedProcessor ile Mixer2 işleme
        /// </summary>
        private async Task ProcessMixer2WithStatusBasedProcessor(Dictionary<string, PlcRegisterData> plcData)
        {
            try
            {
                // Processor'ı sadece bir kez oluştur (edge detection için gerekli)
                if (_mixer2Processor == null)
                {
                    using var context = new ProductionDbContext();
                    var batchService = new ConcreteBatch2Service(context);
                    var cementConsumptionService = new CementConsumptionService(context);
                    _mixer2Processor = new Mixer2StatusBasedProcessor(context, batchService, cementConsumptionService);
                    
                    // Log event'ini bağla
                    Mixer2StatusBasedProcessor.OnFlowEvent += (message) => AddLog(message);
                }

                // Her çağrıda yeni context oluştur ama processor'ı koru
                using var newContext = new ProductionDbContext();
                var newBatchService = new ConcreteBatch2Service(newContext);
                var newCementConsumptionService = new CementConsumptionService(newContext);
                var tempProcessor = new Mixer2StatusBasedProcessor(newContext, newBatchService, newCementConsumptionService);
                
                // Edge detection state'ini kopyala
                tempProcessor.CopyStateFrom(_mixer2Processor);

                // Dictionary'yi PlcDataSnapshot'a dönüştür
                var snapshot = ConvertToPlcDataSnapshot(plcData);
                
                // Mixer2StatusBasedProcessor'ı çağır
                await tempProcessor.ProcessPlcSnapshotAsync(snapshot, "SYSTEM");
                
                // State'i geri kopyala
                _mixer2Processor.CopyStateFrom(tempProcessor);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Mixer2StatusBasedProcessor hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Dictionary'yi PlcDataSnapshot'a dönüştür
        /// </summary>
        private PlcDataSnapshot ConvertToPlcDataSnapshot(Dictionary<string, PlcRegisterData> plcData)
        {
            return new PlcDataSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Operator = "SYSTEM",
                RecipeCode = "AUTO",
                
                // Grup aktiflik durumları - MIXER2 REGISTER'LARI
                AggregateGroupActive = GetBoolValue(plcData, "H51.0"),
                WaterGroupActive = GetBoolValue(plcData, "H61.2"),
                CementGroupActive = GetBoolValue(plcData, "H65.0"),
                AdmixtureGroupActive = GetBoolValue(plcData, "H39.10"),
                PigmentGroupActive = GetBoolValue(plcData, "H31.2"),
                
                // Agrega verileri (8 slot) - MIXER2 REGISTER'LARI (Tartım OK yok)
                Aggregate1Active = GetBoolValue(plcData, "H51.2"),
                Aggregate1Amount = GetDoubleValue(plcData, "DM4704"),
                Aggregate1TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate2Active = GetBoolValue(plcData, "H52.2"),
                Aggregate2Amount = GetDoubleValue(plcData, "DM4714"),
                Aggregate2TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate3Active = GetBoolValue(plcData, "H53.2"),
                Aggregate3Amount = GetDoubleValue(plcData, "DM4724"),
                Aggregate3TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate4Active = GetBoolValue(plcData, "H54.2"),
                Aggregate4Amount = GetDoubleValue(plcData, "DM4734"),
                Aggregate4TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate5Active = GetBoolValue(plcData, "H55.2"),
                Aggregate5Amount = GetDoubleValue(plcData, "DM4744"),
                Aggregate5TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate6Active = GetBoolValue(plcData, "H56.2"),
                Aggregate6Amount = GetDoubleValue(plcData, "DM4754"),
                Aggregate6TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate7Active = GetBoolValue(plcData, "H57.2"),
                Aggregate7Amount = GetDoubleValue(plcData, "DM4764"),
                Aggregate7TartimOk = false, // Mixer2'de tartım OK yok
                
                Aggregate8Active = GetBoolValue(plcData, "H58.2"),
                Aggregate8Amount = GetDoubleValue(plcData, "DM4774"),
                Aggregate8TartimOk = false, // Mixer2'de tartım OK yok
                
                // Çimento verileri (3 slot) - MIXER2 REGISTER'LARI
                Cement1Active = GetBoolValue(plcData, "H65.2"),
                Cement1Amount = GetDoubleValue(plcData, "DM4434"),
                Cement1TartimOk = GetBoolValue(plcData, "H65.7"), // Çimento tartım OK
                
                Cement2Active = GetBoolValue(plcData, "H66.2"),
                Cement2Amount = GetDoubleValue(plcData, "DM4444"),
                Cement2TartimOk = GetBoolValue(plcData, "H66.7"), // Çimento tartım OK
                
                Cement3Active = GetBoolValue(plcData, "H67.2"),
                Cement3Amount = GetDoubleValue(plcData, "DM4454"),
                Cement3TartimOk = GetBoolValue(plcData, "H67.7"), // Çimento tartım OK
                
                // Su verileri - MIXER2 REGISTER'LARI
                Water1Amount = GetDoubleValue(plcData, "DM304"), // Loadcell su
                Water2Amount = GetDoubleValue(plcData, "DM306"), // Pulse su
                
                // Katkı verileri (4 slot) - MIXER2 REGISTER'LARI
                Admixture1Active = GetBoolValue(plcData, "H39.0"),
                Admixture1ChemicalAmount = GetDoubleValue(plcData, "DM4604"),
                Admixture1WaterAmount = GetDoubleValue(plcData, "DM4605"),
                Admixture1TartimOk = GetBoolValue(plcData, "H39.3"), // Katkı tartım OK
                Admixture1WaterTartimOk = GetBoolValue(plcData, "H39.4"), // Katkı su tartım OK
                
                Admixture2Active = GetBoolValue(plcData, "H40.0"),
                Admixture2ChemicalAmount = GetDoubleValue(plcData, "DM4614"),
                Admixture2WaterAmount = GetDoubleValue(plcData, "DM4615"),
                Admixture2TartimOk = GetBoolValue(plcData, "H40.3"), // Katkı tartım OK
                Admixture2WaterTartimOk = GetBoolValue(plcData, "H40.4"), // Katkı su tartım OK
                
                Admixture3Active = GetBoolValue(plcData, "H41.0"),
                Admixture3ChemicalAmount = GetDoubleValue(plcData, "DM4624"),
                Admixture3WaterAmount = GetDoubleValue(plcData, "DM4625"),
                Admixture3TartimOk = GetBoolValue(plcData, "H41.3"), // Katkı tartım OK
                Admixture3WaterTartimOk = GetBoolValue(plcData, "H41.4"), // Katkı su tartım OK
                
                Admixture4Active = GetBoolValue(plcData, "H42.0"),
                Admixture4ChemicalAmount = GetDoubleValue(plcData, "DM4634"),
                Admixture4WaterAmount = GetDoubleValue(plcData, "DM4635"),
                Admixture4TartimOk = GetBoolValue(plcData, "H42.3"), // Katkı tartım OK
                Admixture4WaterTartimOk = GetBoolValue(plcData, "H42.4"), // Katkı su tartım OK
                
                // Pigment verileri (4 slot)
                Pigment1Active = GetBoolValue(plcData, "H31.10"),
                Pigment1TartimOk = GetBoolValue(plcData, "H31.3"),
                Pigment1Amount = GetDoubleValue(plcData, "DM308"),
                
                Pigment2Active = GetBoolValue(plcData, "H32.10"),
                Pigment2TartimOk = GetBoolValue(plcData, "H32.3"),
                Pigment2Amount = GetDoubleValue(plcData, "DM310"),
                
                Pigment3Active = GetBoolValue(plcData, "H33.10"),
                Pigment3TartimOk = GetBoolValue(plcData, "H33.3"),
                Pigment3Amount = GetDoubleValue(plcData, "DM312"),
                
                Pigment4Active = GetBoolValue(plcData, "H34.10"),
                Pigment4TartimOk = GetBoolValue(plcData, "H34.3"),
                Pigment4Amount = GetDoubleValue(plcData, "DM314"),
                
                // Nem verisi
                MoisturePercent = GetDoubleValue(plcData, "DM5100"),
                
                // Harç Hazır sinyali
                BatchReadySignal = GetBoolValue(plcData, "H71.5"),
                
                // Konveyör/kova durumu sinyalleri
                HorizontalHasMaterial = GetBoolValue(plcData, "H71.7"),
                VerticalHasMaterial = GetBoolValue(plcData, "H71.10"),
                WaitingBunkerHasMaterial = GetBoolValue(plcData, "H71.11"),
                MixerHasAggregate = GetBoolValue(plcData, "H71.0"),
                
                // Mixer içerik sinyalleri
                MixerHasCement = GetBoolValue(plcData, "H70.1"),
                MixerHasAdmixture = GetBoolValue(plcData, "H70.2"),
                MixerHasWaterLoadcell = GetBoolValue(plcData, "H70.3"),
                MixerHasWaterPulse = GetBoolValue(plcData, "H70.4")
            };
        }

        /// <summary>
        /// Dictionary'den bool değer al
        /// </summary>
        private bool GetBoolValue(Dictionary<string, PlcRegisterData> plcData, string address)
        {
            return plcData.TryGetValue(address, out var data) && data.IsSuccess && data.Value;
        }

        /// <summary>
        /// Dictionary'den double değer al
        /// </summary>
        private double GetDoubleValue(Dictionary<string, PlcRegisterData> plcData, string address)
        {
            if (plcData.TryGetValue(address, out var data) && data.IsSuccess)
            {
                return data.NumericValue;
            }
            return 0.0;
        }

        #endregion

        #region System Tray Methods

        /// <summary>
        /// System tray'i başlat
        /// </summary>
        private void InitializeSystemTray()
        {
            try
            {
                // NotifyIcon oluştur
                _notifyIcon = new WinForms.NotifyIcon();
                
                // İkon ayarla (basit bir ikon oluştur)
                _notifyIcon.Icon = CreateApplicationIcon();
                _notifyIcon.Text = "Üretim Takip Sistemi";
                _notifyIcon.Visible = true;

                // Çift tık event'i
                _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

                // Sağ tık menüsü
                var contextMenu = new WinForms.ContextMenuStrip();
                contextMenu.Items.Add("Aç", null, (s, e) => ShowFromTray());
                contextMenu.Items.Add("-"); // Separator
                contextMenu.Items.Add("Kapat", null, (s, e) => CloseApplication());
                
                _notifyIcon.ContextMenuStrip = contextMenu;

                // Pencere kapatma event'ini yakala
                this.Closing += MainWindow_Closing;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("System tray başlatma hatası", ex);
            }
        }

        /// <summary>
        /// Basit uygulama ikonu oluştur
        /// </summary>
        private Drawing.Icon CreateApplicationIcon()
        {
            try
            {
                // 16x16 basit ikon oluştur
                var bitmap = new Drawing.Bitmap(16, 16);
                using (var g = Drawing.Graphics.FromImage(bitmap))
                {
                    g.Clear(Drawing.Color.Blue);
                    g.FillRectangle(Drawing.Brushes.White, 2, 2, 12, 12);
                    g.DrawRectangle(Drawing.Pens.Black, 2, 2, 12, 12);
                    
                    // Basit "T" harfi çiz
                    g.DrawLine(Drawing.Pens.Black, 6, 4, 6, 12);
                    g.DrawLine(Drawing.Pens.Black, 4, 6, 8, 6);
                }
                
                return Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                // Hata durumunda sistem ikonu kullan
                return Drawing.SystemIcons.Application;
            }
        }

        /// <summary>
        /// System tray'den pencereyi göster
        /// </summary>
        public void ShowFromTray()
        {
            try
            {
                _isMinimizedToTray = false;
                
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
                this.Focus();
                
                DetailedLogger.LogInfo("Pencere system tray'den geri getirildi");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("System tray'den pencere gösterme hatası", ex);
            }
        }

        /// <summary>
        /// System tray ikonuna çift tık
        /// </summary>
        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowFromTray();
        }

        /// <summary>
        /// Pencere kapatma event'i
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Eğer gerçekten kapatmak istiyorsa (sağ tık menüsünden)
                if (_shouldClose)
                {
                    // System tray'i temizle
                    _notifyIcon?.Dispose();
                    
                    // Tüm servisleri durdur
                    StopAllServices();
                    
                    DetailedLogger.LogInfo("Uygulama kapatılıyor...");
                    return;
                }
                
                // "X" butonuna basıldığında system tray'e taşı
                e.Cancel = true; // Kapatmayı iptal et
                
                // System tray'e taşı
                Hide();
                ShowInTaskbar = false;
                _isMinimizedToTray = true;
                
                DetailedLogger.LogInfo("Program system tray'e taşındı (X butonuna basıldı)");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("Pencere kapatma hatası", ex);
            }
        }

        /// <summary>
        /// Uygulamayı tamamen kapat
        /// </summary>
        private void CloseApplication()
        {
            try
            {
                _shouldClose = true; // Gerçekten kapatmak istiyoruz
                
                // System tray'i temizle
                _notifyIcon?.Dispose();
                
                // Tüm servisleri durdur
                StopAllServices();
                
                // Uygulamayı kapat
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("Uygulama kapatma hatası", ex);
            }
        }

        /// <summary>
        /// Tüm servisleri durdur
        /// </summary>
        private void StopAllServices()
        {
            try
            {
                // Timer'ları durdur
                _dm452Timer?.Stop();
                _vardiyaLogCleanupTimer?.Stop();
                
                // PLC bağlantılarını kapat
                try
                {
                    _dm452Client?.ConnectClose();
                }
                catch
                {
                    // Bağlantı zaten kapalı olabilir
                }
                
                DetailedLogger.LogInfo("Tüm servisler durduruldu");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("Servisleri durdurma hatası", ex);
            }
        }

        #endregion
    }
}
