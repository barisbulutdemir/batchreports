using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip
{
    public partial class BatchTrackingWindow : Window
    {
        private readonly ConcreteBatch2Service _batchService;
        private List<ConcreteBatch2> _allBatches = new List<ConcreteBatch2>();

        public BatchTrackingWindow()
        {
            InitializeComponent();
            
            using var context = new ProductionDbContext();
            _batchService = new ConcreteBatch2Service(context);
            
            // İlk yükleme
            _ = LoadBatchesAsync();
        }

        private async Task LoadBatchesAsync()
        {
            try
            {
                StatusText.Text = "Yükleniyor...";
                
                using var context = new ProductionDbContext();
                var batchService = new ConcreteBatch2Service(context);
                
                _allBatches = await batchService.GetRecentBatchesAsync(100);
                
                // Varsayılan olarak aktif batch'leri göster
                ShowActiveBatches();
                
                StatusText.Text = $"Toplam {_allBatches.Count} batch yüklendi";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Hata: {ex.Message}";
                MessageBox.Show($"Batch'ler yüklenirken hata oluştu:\n{ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowActiveBatches()
        {
            var activeBatches = _allBatches.Where(b => b.Status != "Tamamlandı").ToList();
            BatchDataGrid.ItemsSource = activeBatches;
            StatusText.Text = $"Aktif Batch'ler: {activeBatches.Count}";
        }

        private void ShowAllBatches()
        {
            BatchDataGrid.ItemsSource = _allBatches;
            StatusText.Text = $"Tüm Batch'ler: {_allBatches.Count}";
        }

        private void ShowCompletedBatches()
        {
            var completedBatches = _allBatches.Where(b => b.Status == "tamamlandi").ToList();
            BatchDataGrid.ItemsSource = completedBatches;
            StatusText.Text = $"Tamamlanan Batch'ler: {completedBatches.Count}";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadBatchesAsync();
        }

        private void ShowActiveButton_Click(object sender, RoutedEventArgs e)
        {
            ShowActiveBatches();
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAllBatches();
        }

        private void ShowCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCompletedBatches();
        }

        private void BatchDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchDataGrid.SelectedItem is ConcreteBatch2 selectedBatch)
            {
                LoadBatchDetails(selectedBatch);
            }
        }

        private void LoadBatchDetails(ConcreteBatch2 batch)
        {
            try
            {
                // Agregalar
                AggregateDataGrid.ItemsSource = batch.Aggregates?.ToList() ?? new List<ConcreteBatch2Aggregate>();
                
                // Çimentolar
                CementDataGrid.ItemsSource = batch.Cements?.ToList() ?? new List<ConcreteBatch2Cement>();
                
                // Katkılar
                AdmixtureDataGrid.ItemsSource = batch.Admixtures?.ToList() ?? new List<ConcreteBatch2Admixture>();
                
                // Su detayları - Ayrı ayrı LoadcellWaterKg ve PulseWaterKg göster
                LoadcellWaterDetail.Text = $"{batch.LoadcellWaterKg:F1} kg";
                PulseWaterDetail.Text = $"{batch.PulseWaterKg:F1} kg";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch detayları yüklenirken hata oluştu:\n{ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchDataGrid.SelectedItem is ConcreteBatch2 selectedBatch)
            {
                var result = MessageBox.Show(
                    $"Batch {selectedBatch.Id} silinsin mi?\n\nTarih: {selectedBatch.OccurredAt:dd.MM.yyyy HH:mm:ss}\nDurum: {selectedBatch.Status}",
                    "Batch Silme Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _batchService.DeleteBatchAsync(selectedBatch.Id);
                        StatusText.Text = $"Batch {selectedBatch.Id} silindi";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        
                        // Listeyi yenile
                        await LoadBatchesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Batch silinirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Silme hatası";
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek batch'i seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
