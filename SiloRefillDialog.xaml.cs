using System.Windows;
using System.Windows.Controls;
using takip.Models;

namespace takip
{
    public partial class SiloRefillDialog : Window
    {
        private readonly CementSilo _silo;
        
        public double RefillAmount { get; private set; }
        public string OperatorName { get; private set; } = string.Empty;
        public string ShipmentNumber { get; private set; } = string.Empty;
        public string Supplier { get; private set; } = string.Empty;
        public string Notes { get; private set; } = string.Empty;

        public SiloRefillDialog(CementSilo silo)
        {
            InitializeComponent();
            _silo = silo;
            
            LoadSiloInfo();
            RefillAmountText.TextChanged += RefillAmountText_TextChanged;
        }

        /// <summary>
        /// Silo bilgilerini yükle
        /// </summary>
        private void LoadSiloInfo()
        {
            SiloInfoText.Text = $"{_silo.CementType} Silo {_silo.SiloNumber}";
            CurrentAmountText.Text = $"{_silo.CurrentAmount:N0} kg";
            CapacityText.Text = $"{_silo.Capacity:N0} kg";
            
            // Varsayılan değerler
            OperatorNameText.Text = "Operatör";
            RefillAmountText.Text = "10000";
            
            UpdateNewAmount();
        }

        /// <summary>
        /// Eklenen miktar değiştiğinde
        /// </summary>
        private void RefillAmountText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateNewAmount();
        }

        /// <summary>
        /// Yeni miktarı güncelle
        /// </summary>
        private void UpdateNewAmount()
        {
            if (double.TryParse(RefillAmountText.Text, out double refillAmount))
            {
                var newAmount = Math.Min(_silo.Capacity, _silo.CurrentAmount + refillAmount);
                NewAmountText.Text = $"{newAmount:N0} kg";
                
                if (newAmount >= _silo.Capacity)
                {
                    NewAmountText.Foreground = System.Windows.Media.Brushes.Red;
                    NewAmountText.Text += " (Kapasite Dolu!)";
                }
                else
                {
                    NewAmountText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                NewAmountText.Text = "Geçersiz miktar";
                NewAmountText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Onayla butonu
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                {
                    return;
                }
                
                RefillAmount = double.Parse(RefillAmountText.Text);
                OperatorName = OperatorNameText.Text;
                ShipmentNumber = ShipmentNumberText.Text;
                Supplier = SupplierText.Text;
                Notes = NotesText.Text;
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form işlenirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// İptal butonu
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Form validasyonu
        /// </summary>
        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(RefillAmountText.Text) || !double.TryParse(RefillAmountText.Text, out double refillAmount))
            {
                MessageBox.Show("Lütfen geçerli bir eklenen miktar değeri girin.", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            if (refillAmount <= 0)
            {
                MessageBox.Show("Eklenen miktar 0'dan büyük olmalıdır.", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(OperatorNameText.Text))
            {
                MessageBox.Show("Lütfen operatör adını girin.", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            var newAmount = _silo.CurrentAmount + refillAmount;
            if (newAmount > _silo.Capacity)
            {
                var result = MessageBox.Show(
                    $"Eklenen miktar silo kapasitesini aşacak. Silo kapasitesi: {_silo.Capacity:N0} kg\n\n" +
                    $"Yeni miktar: {newAmount:N0} kg\n\n" +
                    $"Yine de devam etmek istiyor musunuz?",
                    "Kapasite Aşımı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
