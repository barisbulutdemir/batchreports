using System.Windows;
using takip.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using takip.Data;

namespace takip
{
    /// <summary>
    /// Kalıp ekleme diyalog penceresi
    /// </summary>
    public partial class AddMoldDialog : Window
    {
        public string MoldName { get; private set; } = "";
        public string MoldCode { get; private set; } = "";
        private LocalizationService _localizationService = LocalizationService.Instance;

        public AddMoldDialog()
        {
            InitializeComponent();
            InitializeLocalization();
            MoldNameTextBox.Focus();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
        }

        /// <summary>
        /// Dil yerelleştirmesini başlat
        /// </summary>
        private void InitializeLocalization()
        {
            try
            {
                // Dil değişikliği olayını dinle
                _localizationService.LanguageChanged += OnLanguageChanged;
                
                // UI'yi güncelle
                UpdateUI();
                
                System.Diagnostics.Debug.WriteLine("[AddMoldDialog] Dil yerelleştirmesi başlatıldı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddMoldDialog] Dil yerelleştirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Dil değişikliği olayı
        /// </summary>
        private void OnLanguageChanged(object? sender, System.Globalization.CultureInfo culture)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateUI();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddMoldDialog] Dil değişikliği hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// UI metinlerini güncelle
        /// </summary>
        private void UpdateUI()
        {
            try
            {
                AddMoldDialogWindow.Title = _localizationService.GetString("MoldManagement.AddNewMold", "Yeni Kalıp Ekle");
                MoldNameLabel.Content = _localizationService.GetString("MoldManagement.MoldName", "Kalıp Adı:") + ":";
                MoldCodeLabel.Content = _localizationService.GetString("MoldManagement.MoldCode", "Kalıp Kodu:") + ":";
                SaveButton.Content = _localizationService.GetString("MoldManagement.Save", "Kaydet");
                CancelButton.Content = _localizationService.GetString("MoldManagement.Cancel", "İptal");
                
                System.Diagnostics.Debug.WriteLine("[AddMoldDialog] UI metinleri güncellendi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddMoldDialog] UI güncelleme hatası: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MoldName = MoldNameTextBox.Text.Trim();
            MoldCode = MoldCodeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(MoldName))
            {
                var message = _localizationService.GetString("MoldManagement.EnterMoldName", "Lütfen kalıp adı giriniz");
                var title = _localizationService.GetString("Message.Warning", "Uyarı");
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                MoldNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(MoldCode))
            {
                var message = _localizationService.GetString("MoldManagement.EnterMoldCode", "Lütfen kalıp kodu giriniz");
                var title = _localizationService.GetString("Message.Warning", "Uyarı");
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                MoldCodeTextBox.Focus();
                return;
            }

            // Unique kontrolü
            if (!CheckUniqueness())
            {
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Kalıp adı ve kodunun benzersizliğini kontrol et
        /// </summary>
        private bool CheckUniqueness()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    // Kalıp adı kontrolü
                    var existingMoldByName = context.Molds
                        .FirstOrDefault(m => m.Name.ToLower() == MoldName.ToLower());
                    
                    if (existingMoldByName != null)
                    {
                        var message = _localizationService.GetString("MoldManagement.NameExists", 
                            $"'{MoldName}' adında bir kalıp zaten mevcut!");
                        var title = _localizationService.GetString("Message.Error", "Hata");
                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                        MoldNameTextBox.Focus();
                        return false;
                    }

                    // Kalıp kodu kontrolü
                    var existingMoldByCode = context.Molds
                        .FirstOrDefault(m => m.Code.ToLower() == MoldCode.ToLower());
                    
                    if (existingMoldByCode != null)
                    {
                        var message = _localizationService.GetString("MoldManagement.CodeExists", 
                            $"'{MoldCode}' kodunda bir kalıp zaten mevcut!");
                        var title = _localizationService.GetString("Message.Error", "Hata");
                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                        MoldCodeTextBox.Focus();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                var message = _localizationService.GetString("MoldManagement.CheckError", 
                    $"Benzersizlik kontrolü sırasında hata: {ex.Message}");
                var title = _localizationService.GetString("Message.Error", "Hata");
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}