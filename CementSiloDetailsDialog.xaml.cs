using System.Windows;
using System.Windows.Media;
using takip.Models;
using takip.Services;

namespace takip
{
    public partial class CementSiloDetailsDialog : Window
    {
        private readonly int _siloId;
        private readonly CementSiloService _cementSiloService;
        private CementSilo? _silo;

        public CementSiloDetailsDialog(int siloId, CementSiloService cementSiloService)
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

            _siloId = siloId;
            _cementSiloService = cementSiloService;

            _ = LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                _silo = await _cementSiloService.GetSiloByIdAsync(_siloId);
                if (_silo == null) return;

                // Update title
                TitleText.Text = $"Silo {_silo.SiloNumber} - {_silo.CementType} Details";

                // Fill general information
                SiloNumberText.Text = _silo.SiloNumber.ToString();
                CementTypeText.Text = _silo.CementType;
                CurrentAmountText.Text = $"{_silo.CurrentAmount:F0} kg";
                CapacityText.Text = $"{_silo.Capacity:F0} kg";
                MinLevelText.Text = $"{_silo.MinLevel:F0} kg";

                FillPercentageText.Text = $"{_silo.FillPercentage:F1}%";
                StatusText.Text = _silo.Status;
                LastUpdatedText.Text = _silo.LastUpdated.ToString("dd.MM.yyyy HH:mm");

                // Update visual silo
                UpdateVisualSilo();

                // Load history data
                await LoadHistoryData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloDetailsDialog] Data loading error: {ex.Message}");
            }
        }

        private void UpdateVisualSilo()
        {
            if (_silo == null) return;

            try
            {
                // Determine color based on fill percentage
                var fillColor = _silo.FillPercentage switch
                {
                    <= 0 => Colors.Red,
                    <= 20 => Colors.DarkRed,
                    <= 40 => Colors.Orange,
                    <= 80 => Colors.Yellow,
                    _ => Colors.Green
                };

                FillRectangle.Fill = new SolidColorBrush(fillColor);

                // Calculate height (160px maximum)
                var height = Math.Max(5, (_silo.FillPercentage / 100.0) * 160);
                FillRectangle.Height = height;

                FillText.Text = $"{_silo.FillPercentage:F1}%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloDetailsDialog] Visual update error: {ex.Message}");
            }
        }

        private async Task LoadHistoryData()
        {
            try
            {
                // Consumption history
                var consumptions = await _cementSiloService.GetRecentConsumptionsAsync(_siloId, 50);
                ConsumptionGrid.ItemsSource = consumptions;

                // Addition history
                var refills = await _cementSiloService.GetRecentRefillsAsync(_siloId, 50);
                RefillGrid.ItemsSource = refills;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloDetailsDialog] History data loading error: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
