using System.Globalization;
using System.Resources;
using System.Windows;
using System.IO;

namespace takip.Services
{
    /// <summary>
    /// Dil yerelleştirme servisi - çok dilli destek için
    /// </summary>
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private static readonly object _lock = new object();
        
        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;
        private Dictionary<string, Dictionary<string, string>> _translations = null!;
        
        /// <summary>
        /// Dil değişikliği olayı
        /// </summary>
        public event EventHandler<CultureInfo>? LanguageChanged;
        
        /// <summary>
        /// Tekil örnek (Singleton)
        /// </summary>
        public static LocalizationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LocalizationService();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Mevcut dil
        /// </summary>
        public CultureInfo CurrentCulture => _currentCulture;
        
        /// <summary>
        /// Mevcut dil (string olarak)
        /// </summary>
        public string CurrentLanguage => _currentCulture.Name;
        
        /// <summary>
        /// Desteklenen diller
        /// </summary>
        public List<CultureInfo> SupportedCultures { get; private set; }
        
        private LocalizationService()
        {
            // Desteklenen dilleri tanımla
            SupportedCultures = new List<CultureInfo>
            {
                new CultureInfo("tr-TR"), // Türkçe
                new CultureInfo("en-US")  // İngilizce
            };
            
            // Varsayılan dil: İngilizce
            _currentCulture = new CultureInfo("en-US");
            
            // Çevirileri yükle
            LoadTranslations();
            
            // Kaynak dosyasını yükle
            LoadResourceManager();
        }
        
        /// <summary>
        /// Çevirileri yükle (Dictionary tabanlı)
        /// </summary>
        private void LoadTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["tr-TR"] = new Dictionary<string, string>
                {
                    ["MainWindow.Title"] = "Üretim Takip Sistemi",
                    ["MainWindow.ShiftManagement"] = "Vardiya Yönetimi",
                    ["MainWindow.ProductionInfo"] = "Üretim Bilgileri",
                    ["MainWindow.StartShift"] = "Vardiyayı Başlat",
                    ["MainWindow.EndShift"] = "Vardiyayı Bitir",
                    ["MainWindow.ShiftHistory"] = "Vardiya Geçmişi",
                    ["MainWindow.PlcTest"] = "🔧 PLC Test Sayfası",
                    ["MainWindow.TotalProduction"] = "Toplam Üretim",
                    ["MainWindow.Logs"] = "Loglar",
                    ["MainWindow.DailyPalletProduction"] = "Günlük Palet Üretimi",
                    ["MainWindow.ShiftStart"] = "Vardiya Başlangıç",
                    ["MainWindow.ProductionStart"] = "Üretim Başlangıç",
                    ["MainWindow.NotStarted"] = "Başlatılmadı",
                    ["MainWindow.Pallet"] = "palet",
                    ["MainWindow.Add"] = "Ekle",
                    ["MainWindow.Delete"] = "Sil",
                    ["MainWindow.PlcStatus"] = "PLC: Bağlantı Kontrol Ediliyor...",
                    ["MainWindow.PlcConnected"] = "PLC: Bağlı",
                    ["MainWindow.PlcDisconnected"] = "PLC: Bağlantı Yok",
                    ["MainWindow.PlcError"] = "PLC: Hata",
                    
                    // Mold Management
                    ["MoldManagement.Title"] = "Mold Management",
                    ["MoldManagement.AddNewMold"] = "Add New Mold",
                    ["MoldManagement.AddMold"] = "Add Mold",
                    ["MoldManagement.ExistingMolds"] = "Existing Molds",
                    ["MoldManagement.MoldName"] = "Mold Name",
                    ["MoldManagement.MoldCode"] = "Mold Code",
                    ["MoldManagement.MoldDescription"] = "Description",
                    ["MoldManagement.Add"] = "Add",
                    ["MoldManagement.Cancel"] = "Cancel",
                    ["MoldManagement.Edit"] = "Edit",
                    ["MoldManagement.Delete"] = "Delete",
                    ["MoldManagement.Save"] = "Save",
                    ["MoldManagement.NoMolds"] = "No molds added yet",
                    ["MoldManagement.MoldAdded"] = "Mold added successfully",
                    ["MoldManagement.MoldUpdated"] = "Mold updated successfully",
                    ["MoldManagement.MoldDeleted"] = "Mold deleted successfully",
                    ["MoldManagement.EnterMoldName"] = "Please enter mold name",
                    ["MoldManagement.EnterMoldCode"] = "Please enter mold code",
                    ["MoldManagement.DeleteConfirm"] = "Are you sure you want to delete this mold?",
                    ["MoldManagement.ActiveMold"] = "Active Mold",
                    ["MoldManagement.PrintCount"] = "Print Count",
                    ["MoldManagement.Active"] = "Active",
                    ["MoldManagement.Inactive"] = "Inactive",
                    ["MoldManagement.MakeActive"] = "Make Active",
                    ["MoldManagement.MakeInactive"] = "Make Inactive",
                    ["MoldManagement.Delete"] = "Delete",
                    ["MoldManagement.TotalPrints"] = "Total Production",
                    ["MoldManagement.InstallDate"] = "Install",
                    ["MoldManagement.RemoveDate"] = "Remove",
                    ["MoldManagement.MakeInactiveConfirm"] = "Make Mold Inactive Confirmation",
                    ["MoldManagement.MakeInactiveMessage"] = "Are you sure you want to make this mold inactive?",
                    ["MoldManagement.DeleteConfirmTitle"] = "Delete Mold Confirmation",
                    ["MoldManagement.DeleteMessage"] = "Are you sure you want to delete this mold?\n\nThis action cannot be undone!",
                    ["MoldManagement.MadeActive"] = "Mold made active",
                    ["MoldManagement.MadeInactive"] = "Mold made inactive",
                    ["MoldManagement.Deleted"] = "Mold deleted",
                    ["MoldManagement.NotRemoved"] = "Not removed yet",
                    
                    // Batch Status Translations
                    ["BatchStatus.YatayKovada"] = "Horizontal Bucket",
                    ["BatchStatus.DikeyKovada"] = "Vertical Bucket", 
                    ["BatchStatus.BeklemeBunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.Bekleme Bunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.Bekleme Bunkeri"] = "Waiting Bunker",
                    ["BatchStatus.Mixerde"] = "In Mixer",
                    ["BatchStatus.TartimKovasinda"] = "Weighing Bucket",
                    ["BatchStatus.Tartım Kovasında"] = "Weighing Bucket",
                    ["BatchStatus.Harç Hazır"] = "Ready for Transport",
                    ["BatchStatus.Tamamlandı"] = "Completed",
                    ["BatchStatus.tamamlandi"] = "Completed",
                    ["BatchStatus.yatay_kovada"] = "Horizontal Bucket",
                    ["BatchStatus.dikey_kovada"] = "Vertical Bucket",
                    ["BatchStatus.bekleme_bunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.mixerde"] = "In Mixer",
                    
                    // Üretim Bilgileri
                    ["Production.ShiftProduction"] = "Vardiya İçi Üretim",
                    ["Production.NoProduction"] = "Henüz üretim yok",
                    ["Production.PalletCount"] = "0 palet",
                    
                    // Üretim Raporlama
                    ["ProductionReporting.Title"] = "Üretim Takip",
                    ["ProductionReporting.PlcStatus"] = "PLC Durumu",
                    ["ProductionReporting.PlcTest"] = "PLC Test",
                    ["ProductionReporting.ShiftManagement"] = "Vardiya Yönetimi",
                    ["ProductionReporting.StartShift"] = "Vardiyayı Başlat",
                    ["ProductionReporting.EndShift"] = "Vardiyayı Bitir",
                    ["ProductionReporting.ShiftHistory"] = "Vardiya Geçmişi",
                    ["ProductionReporting.ProductionInfo"] = "Üretim Bilgileri",
                    ["ProductionReporting.TotalProduction"] = "Toplam Üretim",
                    ["ProductionReporting.DailyPalletProduction"] = "Günlük Palet Üretimi",
                    ["ProductionReporting.ShiftStart"] = "Vardiya Başlangıç",
                    ["ProductionReporting.ProductionStart"] = "Üretim Başlangıç",
                    ["ProductionReporting.NotStarted"] = "Başlatılmadı",
                    ["ProductionReporting.Pallet"] = "palet",
                    ["ProductionReporting.Logs"] = "Loglar",
                    ["ProductionReporting.PlcConnected"] = "PLC: Bağlı",
                    ["ProductionReporting.PlcDisconnected"] = "PLC: Bağlantı Yok",
                    ["ProductionReporting.PlcError"] = "PLC: Hata",
                    ["ProductionReporting.PlcChecking"] = "PLC: Bağlantı Kontrol Ediliyor...",
                    ["ProductionReporting.SimulationMode"] = "⚠️ PLC bağlantısı yok - Simülasyon modunda çalışıyor",
                    ["ProductionReporting.RealDataMode"] = "✅ Gerçek PLC verisi alınıyor",
                    
                    // Beton Santrali Raporlama
                    ["ConcreteReporting.Title"] = "Beton Santrali Raporlama",
                    ["ConcreteReporting.Mixer1Reporting"] = "🏗️ Mixer1 Raporlama",
                    ["ConcreteReporting.Mixer2Reporting"] = "🏗️ Mixer2 Raporlama",
                    ["ConcreteReporting.Mixer1Reports"] = "📊 Mixer1 Raporlar",
                    ["ConcreteReporting.Mixer2Reports"] = "📊 Mixer2 Raporlar",
                    ["ConcreteReporting.CementSilos"] = "🏭 Çimento Siloları",
                    ["ConcreteReporting.CementSilosTitle"] = "🏭 Çimento Siloları",
                    ["ConcreteReporting.Refresh"] = "🔄 Yenile",
                    ["ConcreteReporting.Silo"] = "Silo",
                    ["ConcreteReporting.Level"] = "Seviye",
                    ["ConcreteReporting.LastRefill"] = "Son Doldurma",
                    ["ConcreteReporting.Amount"] = "Miktar",
                    ["ConcreteReporting.Details"] = "📊 Detay",
                    ["ConcreteReporting.Refill"] = "🔄 Doldur",
                    ["ConcreteReporting.InfoText"] = "Beton santrali raporlama sistemi. Mixer1 ve Mixer2 için tek sayfadan işlem yapın.",
                    ["ConcreteReporting.SiloRefreshed"] = "Çimento siloları yenilendi.",
                    ["ConcreteReporting.SiloRefreshError"] = "Çimento siloları yenileme hatası",
                    ["ConcreteReporting.SiloLoadError"] = "Çimento siloları yükleme hatası",
                    ["ConcreteReporting.SiloDetailsError"] = "Silo detayları açma hatası",
                    ["ConcreteReporting.SiloRefillError"] = "Silo doldurma hatası",
                    ["ConcreteReporting.SiloRefilled"] = "Silo dolduruldu"
                },
                ["en-US"] = new Dictionary<string, string>
                {
                    ["MainWindow.Title"] = "Production Tracking System",
                    ["MainWindow.ShiftManagement"] = "Shift Management",
                    ["MainWindow.ProductionInfo"] = "Production Information",
                    ["MainWindow.StartShift"] = "Start Shift",
                    ["MainWindow.EndShift"] = "End Shift",
                    ["MainWindow.ShiftHistory"] = "Shift History",
                    ["MainWindow.PlcTest"] = "🔧 PLC Test Page",
                    ["MainWindow.TotalProduction"] = "Total Production",
                    ["MainWindow.Logs"] = "Logs",
                    ["MainWindow.DailyPalletProduction"] = "Daily Pallet Production",
                    ["MainWindow.ShiftStart"] = "Shift Start",
                    ["MainWindow.ProductionStart"] = "Production Start",
                    ["MainWindow.NotStarted"] = "Not Started",
                    ["MainWindow.Pallet"] = "pallet",
                    ["MainWindow.Add"] = "Add",
                    ["MainWindow.Delete"] = "Delete",
                    ["MainWindow.PlcStatus"] = "PLC: Checking Connection...",
                    ["MainWindow.PlcConnected"] = "PLC: Connected",
                    ["MainWindow.PlcDisconnected"] = "PLC: Disconnected",
                    ["MainWindow.PlcError"] = "PLC: Error",
                    
                    // Mold Management
                    ["MoldManagement.Title"] = "Mold Management",
                    ["MoldManagement.AddNewMold"] = "Add New Mold",
                    ["MoldManagement.AddMold"] = "Add Mold",
                    ["MoldManagement.ExistingMolds"] = "Existing Molds",
                    ["MoldManagement.MoldName"] = "Mold Name",
                    ["MoldManagement.MoldCode"] = "Mold Code",
                    ["MoldManagement.MoldDescription"] = "Description",
                    ["MoldManagement.Add"] = "Add",
                    ["MoldManagement.Cancel"] = "Cancel",
                    ["MoldManagement.Edit"] = "Edit",
                    ["MoldManagement.Delete"] = "Delete",
                    ["MoldManagement.Save"] = "Save",
                    ["MoldManagement.NoMolds"] = "No molds added yet",
                    ["MoldManagement.MoldAdded"] = "Mold added successfully",
                    ["MoldManagement.MoldUpdated"] = "Mold updated successfully",
                    ["MoldManagement.MoldDeleted"] = "Mold deleted successfully",
                    ["MoldManagement.EnterMoldName"] = "Please enter mold name",
                    ["MoldManagement.EnterMoldCode"] = "Please enter mold code",
                    ["MoldManagement.DeleteConfirm"] = "Are you sure you want to delete this mold?",
                    ["MoldManagement.ActiveMold"] = "Active Mold",
                    ["MoldManagement.PrintCount"] = "Print Count",
                    ["MoldManagement.Active"] = "Active",
                    ["MoldManagement.Inactive"] = "Inactive",
                    ["MoldManagement.MakeActive"] = "Make Active",
                    ["MoldManagement.MakeInactive"] = "Make Inactive",
                    ["MoldManagement.Delete"] = "Delete",
                    ["MoldManagement.TotalPrints"] = "Total Production",
                    ["MoldManagement.InstallDate"] = "Install",
                    ["MoldManagement.RemoveDate"] = "Remove",
                    ["MoldManagement.MakeInactiveConfirm"] = "Make Mold Inactive Confirmation",
                    ["MoldManagement.MakeInactiveMessage"] = "Are you sure you want to make this mold inactive?",
                    ["MoldManagement.DeleteConfirmTitle"] = "Delete Mold Confirmation",
                    ["MoldManagement.DeleteMessage"] = "Are you sure you want to delete this mold?\n\nThis action cannot be undone!",
                    ["MoldManagement.MadeActive"] = "Mold made active",
                    ["MoldManagement.MadeInactive"] = "Mold made inactive",
                    ["MoldManagement.Deleted"] = "Mold deleted",
                    ["MoldManagement.NotRemoved"] = "Not removed yet",
                    
                    // Batch Status Translations
                    ["BatchStatus.YatayKovada"] = "Horizontal Bucket",
                    ["BatchStatus.DikeyKovada"] = "Vertical Bucket", 
                    ["BatchStatus.BeklemeBunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.Bekleme Bunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.Bekleme Bunkeri"] = "Waiting Bunker",
                    ["BatchStatus.Mixerde"] = "In Mixer",
                    ["BatchStatus.TartimKovasinda"] = "Weighing Bucket",
                    ["BatchStatus.Tartım Kovasında"] = "Weighing Bucket",
                    ["BatchStatus.Harç Hazır"] = "Ready for Transport",
                    ["BatchStatus.Tamamlandı"] = "Completed",
                    ["BatchStatus.tamamlandi"] = "Completed",
                    ["BatchStatus.yatay_kovada"] = "Horizontal Bucket",
                    ["BatchStatus.dikey_kovada"] = "Vertical Bucket",
                    ["BatchStatus.bekleme_bunkerinde"] = "Waiting Bunker",
                    ["BatchStatus.mixerde"] = "In Mixer",
                    
                    // Production Information
                    ["Production.ShiftProduction"] = "Shift Production",
                    ["Production.NoProduction"] = "No production yet",
                    ["Production.PalletCount"] = "0 pallets",
                    
                    // Production Reporting
                    ["ProductionReporting.Title"] = "Production Tracking",
                    ["ProductionReporting.PlcStatus"] = "PLC Status",
                    ["ProductionReporting.PlcTest"] = "PLC Test",
                    ["ProductionReporting.ShiftManagement"] = "Shift Management",
                    ["ProductionReporting.StartShift"] = "Start Shift",
                    ["ProductionReporting.EndShift"] = "End Shift",
                    ["ProductionReporting.ShiftHistory"] = "Shift History",
                    ["ProductionReporting.ProductionInfo"] = "Production Information",
                    ["ProductionReporting.TotalProduction"] = "Total Production",
                    ["ProductionReporting.DailyPalletProduction"] = "Daily Pallet Production",
                    ["ProductionReporting.ShiftStart"] = "Shift Start",
                    ["ProductionReporting.ProductionStart"] = "Production Start",
                    ["ProductionReporting.NotStarted"] = "Not Started",
                    ["ProductionReporting.Pallet"] = "pallet",
                    ["ProductionReporting.Logs"] = "Logs",
                    ["ProductionReporting.PlcConnected"] = "PLC: Connected",
                    ["ProductionReporting.PlcDisconnected"] = "PLC: Disconnected",
                    ["ProductionReporting.PlcError"] = "PLC: Error",
                    ["ProductionReporting.PlcChecking"] = "PLC: Checking Connection...",
                    ["ProductionReporting.SimulationMode"] = "⚠️ No PLC connection - Running in simulation mode",
                    ["ProductionReporting.RealDataMode"] = "✅ Receiving real PLC data",
                    
                    // Concrete Plant Reporting
                    ["ConcreteReporting.Title"] = "Concrete Plant Reporting",
                    ["ConcreteReporting.Mixer1Reporting"] = "🏗️ Mixer1 Reporting",
                    ["ConcreteReporting.Mixer2Reporting"] = "🏗️ Mixer2 Reporting",
                    ["ConcreteReporting.Mixer1Reports"] = "📊 Mixer1 Reports",
                    ["ConcreteReporting.Mixer2Reports"] = "📊 Mixer2 Reports",
                    ["ConcreteReporting.Mixer2Simulation"] = "🎮 Mixer2 Simulation",
                    ["ConcreteReporting.CementSilos"] = "🏭 Cement Silos",
                    ["ConcreteReporting.CementSilosTitle"] = "🏭 Cement Silos",
                    ["ConcreteReporting.Refresh"] = "🔄 Refresh",
                    ["ConcreteReporting.Silo"] = "Silo",
                    ["ConcreteReporting.Level"] = "Level",
                    ["ConcreteReporting.LastRefill"] = "Last Refill",
                    ["ConcreteReporting.Amount"] = "Amount",
                    ["ConcreteReporting.Details"] = "📊 Details",
                    ["ConcreteReporting.Refill"] = "🔄 Refill",
                    ["ConcreteReporting.InfoText"] = "Concrete plant reporting system. Process operations for Mixer1 and Mixer2 from a single page.",
                    ["ConcreteReporting.SiloRefreshed"] = "Cement silos refreshed.",
                    ["ConcreteReporting.SiloRefreshError"] = "Cement silos refresh error",
                    ["ConcreteReporting.SiloLoadError"] = "Cement silos loading error",
                    ["ConcreteReporting.SiloDetailsError"] = "Silo details opening error",
                    ["ConcreteReporting.SiloRefillError"] = "Silo refill error",
                    ["ConcreteReporting.SiloRefilled"] = "Silo refilled"
                }
            };
            
            System.Diagnostics.Debug.WriteLine("[LocalizationService] Çeviriler yüklendi");
        }
        
        /// <summary>
        /// Kaynak dosyasını yükle
        /// </summary>
        private void LoadResourceManager()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[LocalizationService] ResourceManager yükleniyor...");
                _resourceManager = new ResourceManager("takip.Resources.Strings", typeof(LocalizationService).Assembly);
                System.Diagnostics.Debug.WriteLine("[LocalizationService] ResourceManager başarıyla yüklendi");
                
                // Test için bir değer almayı dene
                var testValue = _resourceManager.GetString("MainWindow.Title", new CultureInfo("tr-TR"));
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Test değeri (tr-TR): '{testValue}'");
                
                var testValueEn = _resourceManager.GetString("MainWindow.Title", new CultureInfo("en-US"));
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Test değeri (en-US): '{testValueEn}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Kaynak dosyası yükleme hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Dil değiştir
        /// </summary>
        /// <param name="culture">Yeni dil kültürü</param>
        public void ChangeLanguage(CultureInfo culture)
        {
            if (!SupportedCultures.Contains(culture))
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Desteklenmeyen dil: {culture.Name}");
                return;
            }
            
            _currentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            
            // WPF'in dil ayarlarını güncelle
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.Name)));
            
            // Dil değişikliği olayını tetikle
            LanguageChanged?.Invoke(this, culture);
            
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dil değiştirildi: {culture.DisplayName}");
        }
        
        /// <summary>
        /// Dil değiştir (dil kodu ile)
        /// </summary>
        /// <param name="languageCode">Dil kodu (tr-TR, en-US)</param>
        public void ChangeLanguage(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);
                ChangeLanguage(culture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Geçersiz dil kodu: {languageCode}, Hata: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Metin al (yerelleştirilmiş)
        /// </summary>
        /// <param name="key">Metin anahtarı</param>
        /// <param name="defaultValue">Varsayılan değer</param>
        /// <returns>Yerelleştirilmiş metin</returns>
        public string GetString(string key, string? defaultValue = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] GetString çağrıldı - Key: {key}, Culture: {_currentCulture.Name}");
                
                // Önce Dictionary'den dene
                if (_translations.ContainsKey(_currentCulture.Name) && 
                    _translations[_currentCulture.Name].ContainsKey(key))
                {
                    var value = _translations[_currentCulture.Name][key];
                    System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dictionary'den alınan değer: '{value}'");
                    return value;
                }
                
                // Dictionary'de yoksa ResourceManager'dan dene
                if (_resourceManager != null)
                {
                    var value = _resourceManager.GetString(key, _currentCulture);
                    System.Diagnostics.Debug.WriteLine($"[LocalizationService] ResourceManager'dan alınan değer: '{value}'");
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LocalizationService] ResourceManager null!");
                }
                
                // Kaynak bulunamazsa varsayılan değeri döndür
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Varsayılan değer döndürülüyor: {defaultValue ?? key}");
                return defaultValue ?? key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Metin alma hatası - Key: {key}, Hata: {ex.Message}");
                return defaultValue ?? key;
            }
        }
        
        /// <summary>
        /// Dil ayarlarını kaydet
        /// </summary>
        public void SaveLanguageSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language_settings.json");
                var settings = new { Language = _currentCulture.Name };
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dil ayarları kaydedildi: {_currentCulture.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dil ayarları kaydetme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Dil ayarlarını yükle
        /// </summary>
        public void LoadLanguageSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language_settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (settings != null && settings.ContainsKey("Language"))
                    {
                        var savedLanguage = settings["Language"];
                        ChangeLanguage(savedLanguage);
                        System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dil ayarları yüklendi: {savedLanguage}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalizationService] Dil ayarları yükleme hatası: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Dil yükle (LoadLanguageSettings için alias)
        /// </summary>
        public void LoadLanguage()
        {
            LoadLanguageSettings();
        }
        
        /// <summary>
        /// Dil adını al (görüntüleme için)
        /// </summary>
        /// <param name="culture">Kültür</param>
        /// <returns>Dil adı</returns>
        public string GetLanguageDisplayName(CultureInfo culture)
        {
            return culture.Name switch
            {
                "tr-TR" => "Türkçe",
                "en-US" => "English",
                _ => culture.DisplayName
            };
        }
        
        /// <summary>
        /// Batch status'unu çevir (UI'da İngilizce göster)
        /// </summary>
        /// <param name="status">Türkçe status</param>
        /// <returns>İngilizce status</returns>
        public string TranslateBatchStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return status;
                
            var key = $"BatchStatus.{status}";
            return GetString(key) ?? status; // Çeviri bulunamazsa orijinal status'u döndür
        }
    }
}
