using System.Windows;
using System.Windows.Controls;
using takip.Models;
using takip.Services;

namespace takip
{
    public partial class CementRefillDialog : Window
    {
        private readonly int _siloId;
        private readonly CementSiloService _cementSiloService;
        private CementSilo? _silo;

        public CementRefillDialog(int siloId, CementSiloService cementSiloService)
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

            _siloId = siloId;
            _cementSiloService = cementSiloService;

            LoadSiloInfo();
            AmountTextBox.TextChanged += UpdatePreview;
        }

        private async void LoadSiloInfo()
        {
            try
            {
                _silo = await _cementSiloService.GetSiloByIdAsync(_siloId);
                if (_silo != null)
                {
                    SiloInfoText.Text = $"Silo {_silo.SiloNumber} - {_silo.CementType}\n" +
                                      $"Mevcut: {_silo.CurrentAmount:F0} kg\n" +
                                      $"Kapasite: {_silo.Capacity:F0} kg\n" +
                                      $"Kalan Kapasite: {_silo.Capacity - _silo.CurrentAmount:F0} kg";
                }
                UpdatePreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementRefillDialog] Silo bilgi yükleme hatası: {ex.Message}");
            }
        }

        private void UpdatePreview(object? sender = null, TextChangedEventArgs? e = null)
        {
            try
            {
                if (_silo == null) return;

                if (double.TryParse(AmountTextBox.Text, out var amount))
                {
                    var newAmount = _silo.CurrentAmount + amount;
                    var fillPercentage = (newAmount / _silo.Capacity) * 100;

                    var status = "Normal";
                    if (newAmount <= 0) status = "Boş";
                    else if (newAmount <= _silo.MinLevel) status = "Kritik";
                    else if (newAmount <= _silo.MinLevel * 1.5) status = "Düşük";
                    else if (fillPercentage >= 80) status = "Dolu";

                    PreviewText.Text = $"Ekleme Sonrası:\n" +
                                     $"Yeni Miktar: {newAmount:F0} kg\n" +
                                     $"Doluluk: {fillPercentage:F1}%\n" +
                                     $"Durum: {status}";

                    // Kapasite kontrolü
                    if (newAmount > _silo.Capacity)
                    {
                        PreviewText.Text += "\n\n⚠️ UYARI: Kapasite aşılacak!";
                        PreviewText.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {
                        PreviewText.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }
                else
                {
                    PreviewText.Text = "Geçerli bir miktar girin";
                    PreviewText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementRefillDialog] Önizleme güncelleme hatası: {ex.Message}");
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
                {
                    MessageBox.Show("Geçerli bir miktar girin!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_silo == null)
                {
                    MessageBox.Show("Silo bilgisi bulunamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_silo.CurrentAmount + amount > _silo.Capacity)
                {
                    MessageBox.Show($"Kapasite aşılacak! Maksimum eklenebilir: {_silo.Capacity - _silo.CurrentAmount:F0} kg", 
                                  "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var operatorName = string.IsNullOrWhiteSpace(OperatorTextBox.Text) ? "Operatör" : OperatorTextBox.Text;
                var shipmentNumber = string.IsNullOrWhiteSpace(ShipmentTextBox.Text) ? null : ShipmentTextBox.Text;
                var supplier = string.IsNullOrWhiteSpace(SupplierTextBox.Text) ? null : SupplierTextBox.Text;
                var notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text;

                var success = await _cementSiloService.RefillSiloAsync(
                    _siloId, (decimal)amount, operatorName, shipmentNumber, supplier, notes);

                if (success)
                {
                    MessageBox.Show($"Çimento başarıyla eklendi!\nEklenen: {amount:F0} kg", 
                                  "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Çimento eklenemedi!\nKapasite aşımı: {amount:F0} kg eklenmek isteniyor\nMevcut: {_silo.CurrentAmount:F0} kg\nKapasite: {_silo.Capacity:F0} kg", 
                                  "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Çimento ekleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[CementRefillDialog] Çimento ekleme hatası: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
