using System.Windows;
using takip.Services;
using takip.Data;
using takip.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace takip
{
    /// <summary>
    /// Operatör ekleme diyalog penceresi
    /// </summary>
    public partial class AddOperatorDialog : Window
    {
        public string OperatorName { get; private set; } = "";
        private LocalizationService _localizationService = LocalizationService.Instance;

        public AddOperatorDialog()
        {
            InitializeComponent();
            InitializeLocalization();
            OperatorNameTextBox.Focus();
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
                
                System.Diagnostics.Debug.WriteLine("[AddOperatorDialog] Dil yerelleştirmesi başlatıldı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddOperatorDialog] Dil yerelleştirme hatası: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[AddOperatorDialog] Dil değişikliği hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// UI metinlerini güncelle
        /// </summary>
        private void UpdateUI()
        {
            try
            {
                AddOperatorDialogWindow.Title = _localizationService.GetString("OperatorManagement.AddNewOperator", "Yeni Operatör Ekle");
                OperatorNameLabel.Content = _localizationService.GetString("OperatorManagement.OperatorName", "Operatör Adı:") + ":";
                SaveButton.Content = _localizationService.GetString("OperatorManagement.Save", "Kaydet");
                CancelButton.Content = _localizationService.GetString("OperatorManagement.Cancel", "İptal");
                
                System.Diagnostics.Debug.WriteLine("[AddOperatorDialog] UI metinleri güncellendi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddOperatorDialog] UI güncelleme hatası: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            OperatorName = OperatorNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(OperatorName))
            {
                var message = _localizationService.GetString("OperatorManagement.EnterOperatorName", "Lütfen operatör adı giriniz");
                var title = _localizationService.GetString("Message.Warning", "Uyarı");
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                OperatorNameTextBox.Focus();
                return;
            }

            // Unique kontrolü
            if (!CheckUniqueness())
            {
                return;
            }

            // Operatörü veritabanına kaydet
            try
            {
                using var context = new ProductionDbContext();
                var newOperator = new Operator
                {
                    Name = OperatorName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Operators.Add(newOperator);
                context.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var message = _localizationService.GetString("OperatorManagement.SaveError", 
                    $"Operatör kaydetme hatası: {ex.Message}");
                var title = _localizationService.GetString("Message.Error", "Hata");
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Operatör adının benzersizliğini kontrol et
        /// </summary>
        private bool CheckUniqueness()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    // Operatör adı kontrolü
                    var existingOperatorByName = context.Operators
                        .FirstOrDefault(o => o.Name.ToLower() == OperatorName.ToLower());
                    
                    if (existingOperatorByName != null)
                    {
                        var message = _localizationService.GetString("OperatorManagement.NameExists", 
                            $"'{OperatorName}' adında bir operatör zaten mevcut!");
                        var title = _localizationService.GetString("Message.Error", "Hata");
                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                        OperatorNameTextBox.Focus();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                var message = _localizationService.GetString("OperatorManagement.CheckError", 
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
