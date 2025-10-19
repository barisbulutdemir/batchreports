using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip
{
    public partial class UnifiedBatchTrackingWindow : Window
    {
        // DbContext'i thread-safe kullanmak için her işlemde yeni instance oluştur
        private readonly ConcreteBatchService _m1BatchService;
        private ConcreteBatch2Service _m2BatchService;
        private List<ConcreteBatch> _m1Batches = new List<ConcreteBatch>();
        private List<ConcreteBatch2> _m2Batches = new List<ConcreteBatch2>(); // M2 ConcreteBatch2 kullanıyor
        private string _currentFilter = "all"; // all, active, completed
        
        // Debug log ile ilgili tüm kodlar kaldırıldı
        
        // Otomatik yenileme için timer
        private readonly DispatcherTimer _autoRefreshTimer;
        private const int AUTO_REFRESH_INTERVAL_SECONDS = 5; // 5 saniyede bir yenile
        
        public UnifiedBatchTrackingWindow()
        {
            try
            {
                InitializeComponent();
                
                // Her işlem için yeni DbContext oluştur (thread-safe)
                using var context = new ProductionDbContext();
                _m1BatchService = new ConcreteBatchService(context, new ConcretePlcSimulator(context));
                _m2BatchService = new ConcreteBatch2Service(context);
                
                // Otomatik yenileme timer'ını başlat
                _autoRefreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(AUTO_REFRESH_INTERVAL_SECONDS)
                };
                _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
                _autoRefreshTimer.Start();
                
                // İlk yükleme
                _ = LoadAllBatchesAsync();
                
                // Katkı sinyalleri event'ini bağla
                Mixer2StatusBasedProcessor.OnDebugLogEvent += OnKatkiSignalsReceived;
                
                // Başlangıç mesajı (Katkı Register Şablonu)
                KatkiSignalsText.Text = "🧪 KATKI SİNYALLERİ\n" +
                                      "═══════════════════════════════════════\n" +
                                      "📊 GRUP AKTİF (H39.10): -\n\n" +
                                      "🧪 KATKI1 (H39.0):\n" +
                                      "   ✅ Aktif: -\n" +
                                      "   ⚖️ Tartım OK (H39.3): -\n" +
                                      "   💧 Su Tartım OK (H39.4): -\n" +
                                      "   🧪 Kimyasal Kg (DM4604): -\n" +
                                      "   💧 Su Kg (DM4605): -\n\n" +
                                      "🧪 KATKI2 (H40.0):\n" +
                                      "   ✅ Aktif: -\n" +
                                      "   ⚖️ Tartım OK (H40.3): -\n" +
                                      "   💧 Su Tartım OK (H40.4): -\n" +
                                      "   🧪 Kimyasal Kg (DM4614): -\n" +
                                      "   💧 Su Kg (DM4615): -\n\n" +
                                      "🧪 KATKI3 (H41.0):\n" +
                                      "   ✅ Aktif: -\n" +
                                      "   ⚖️ Tartım OK (H41.3): -\n" +
                                      "   💧 Su Tartım OK (H41.4): -\n" +
                                      "   🧪 Kimyasal Kg (DM4624): -\n" +
                                      "   💧 Su Kg (DM4625): -\n\n" +
                                      "🧪 KATKI4 (H42.0):\n" +
                                      "   ✅ Aktif: -\n" +
                                      "   ⚖️ Tartım OK (H42.3): -\n" +
                                      "   💧 Su Tartım OK (H42.4): -\n" +
                                      "   🧪 Kimyasal Kg (DM4634): -\n" +
                                      "   💧 Su Kg (DM4635): -";
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.StartupError"), ex.Message, ex.StackTrace), 
                    LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.CriticalError"), MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Otomatik yenileme - UI donmasını önlemek için async
                await LoadAllBatchesAsync();
                
                // Status text'e son yenileme zamanını ekle
                if (StatusText != null)
                {
                    var currentText = StatusText.Text;
                    if (!currentText.Contains("Son yenileme:"))
                    {
                        StatusText.Text += $" | Son yenileme: {DateTime.Now:HH:mm:ss}";
                    }
                    else
                    {
                        var parts = currentText.Split('|');
                        if (parts.Length > 0)
                        {
                            StatusText.Text = $"{parts[0].Trim()} | Son yenileme: {DateTime.Now:HH:mm:ss}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoRefreshTimer_Tick Hata: {ex.Message}");
            }
        }

        private async Task LoadAllBatchesAsync()
        {
            try
            {
                if (StatusText != null)
                    StatusText.Text = "Yükleniyor...";
                
                // M1 ve M2 batch'lerini paralel olarak yükle
                var m1Task = LoadM1BatchesAsync();
                var m2Task = LoadM2BatchesAsync();
                
                await Task.WhenAll(m1Task, m2Task);
                
                // UI'yi güncelle
                Dispatcher.Invoke(() =>
                {
                    // Filtreleme uygula
                    ApplyCurrentFilter();
                    
                    if (StatusText != null)
                        StatusText.Text = $"M1: {_m1Batches.Count}, M2: {_m2Batches.Count} batch yüklendi";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    if (StatusText != null)
                        StatusText.Text = $"Hata: {ex.Message}";
                    
                    MessageBox.Show($"Batch'ler yüklenirken hata oluştu:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                        "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadM1BatchesAsync()
        {
            try
            {
                // Thread-safe: Her işlem için yeni DbContext
                using var context = new ProductionDbContext();
                
                // ConcreteBatch tablosundan SADECE MIXER1 batch'lerini al
                _m1Batches = await context.ConcreteBatches
                    .Where(b => b.PlantCode == "MIXER1") // 🔥 SADECE MIXER1
                    .Include(b => b.Cements)
                    .Include(b => b.Aggregates)
                    .Include(b => b.Admixtures)
                    .OrderByDescending(b => b.OccurredAt)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();
                
                if (M1StatusText != null)
                    M1StatusText.Text = $"M1 Batch'leri: {_m1Batches.Count}";
                
                System.Diagnostics.Debug.WriteLine($"[UnifiedBatchTrackingWindow] M1 batch'leri yüklendi: {_m1Batches.Count} adet");
            }
            catch (Exception ex)
            {
                if (M1StatusText != null)
                    M1StatusText.Text = $"M1 Hata: {ex.Message}";
                
                // Debug için console'a yaz
                System.Diagnostics.Debug.WriteLine($"LoadM1BatchesAsync Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private async Task LoadM2BatchesAsync()
        {
            try
            {
                // Thread-safe: Her işlem için yeni DbContext
                using var context = new ProductionDbContext();
                
                // ConcreteBatch2 tablosundan MIXER2 batch'lerini al
                _m2Batches = await context.ConcreteBatch2s
                    .Include(b => b.Cements)
                    .Include(b => b.Aggregates)
                    .Include(b => b.Admixtures)
                    .OrderByDescending(b => b.OccurredAt)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();
                
                if (M2StatusText != null)
                    M2StatusText.Text = $"M2 Batch'leri: {_m2Batches.Count}";
                
                System.Diagnostics.Debug.WriteLine($"[UnifiedBatchTrackingWindow] M2 batch'leri yüklendi: {_m2Batches.Count} adet");
            }
            catch (Exception ex)
            {
                if (M2StatusText != null)
                    M2StatusText.Text = $"M2 Hata: {ex.Message}";
                
                System.Diagnostics.Debug.WriteLine($"LoadM2BatchesAsync Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void ApplyCurrentFilter()
        {
            try
            {
                // UI thread'de çalıştığımızdan emin ol
                Dispatcher.Invoke(() =>
                {
                    switch (_currentFilter)
                    {
                        case "active":
                            ShowActiveBatches();
                            break;
                        case "completed":
                            ShowCompletedBatches();
                            break;
                        default:
                            ShowAllBatches();
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyCurrentFilter Hata: {ex.Message}");
            }
        }

        private void ShowActiveBatches()
        {
            try
            {
                _currentFilter = "active";
                
                // M1: Status'u "Tamamlandı" olmayan batch'leri göster
                var activeM1Batches = _m1Batches.Where(b => !string.Equals(b.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)).ToList();
                if (M1BatchDataGrid != null)
                    M1BatchDataGrid.ItemsSource = activeM1Batches;
                
                // M2: Status'u "Tamamlandı" olmayan batch'leri göster
                var activeM2Batches = _m2Batches.Where(b => !string.Equals(b.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)).ToList();
                if (M2BatchDataGrid != null)
                    M2BatchDataGrid.ItemsSource = activeM2Batches;
                
                if (StatusText != null)
                    StatusText.Text = $"Aktif Batch'ler - M1: {_m1Batches.Count}, M2: {activeM2Batches.Count}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowActiveBatches Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void ShowAllBatches()
        {
            try
            {
                _currentFilter = "all";
                
                if (M1BatchDataGrid != null)
                    M1BatchDataGrid.ItemsSource = _m1Batches;
                if (M2BatchDataGrid != null)
                    M2BatchDataGrid.ItemsSource = _m2Batches;
                
                if (StatusText != null)
                    StatusText.Text = $"Tüm Batch'ler - M1: {_m1Batches.Count}, M2: {_m2Batches.Count}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowAllBatches Hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void ShowCompletedBatches()
        {
            _currentFilter = "completed";
            
            // M1: Status'u "Tamamlandı" olan batch'leri göster
            var completedM1Batches = _m1Batches.Where(b => string.Equals(b.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)).ToList();
            M1BatchDataGrid.ItemsSource = completedM1Batches;
            
            // M2: Status'u "Tamamlandı" olan batch'leri göster (büyük/küçük harf duyarsız)
            var completedM2Batches = _m2Batches.Where(b => string.Equals(b.Status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)).ToList();
            M2BatchDataGrid.ItemsSource = completedM2Batches;
            
            StatusText.Text = $"Tamamlanan Batch'ler - M1: {_m1Batches.Count}, M2: {completedM2Batches.Count}";
        }

        #region Event Handlers

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAllBatchesAsync();
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

        private void M1RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadM1BatchesAsync();
        }

        private void M2RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadM2BatchesAsync();
        }

        private void M1DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (M1BatchDataGrid.SelectedItem is ConcreteBatch selectedBatch)
            {
                var result = MessageBox.Show(string.Format(LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.M1BatchDeleteConfirmation"), selectedBatch.Id), 
                    LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.BatchDelete"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Thread-safe: Her işlem için yeni DbContext
                        using var context = new ProductionDbContext();
                        
                        // Veritabanından güncel entity'yi al
                        var batchToDelete = context.ConcreteBatches.Find(selectedBatch.Id);
                        if (batchToDelete != null)
                        {
                            context.ConcreteBatches.Remove(batchToDelete);
                            context.SaveChanges();
                            
                            _m1Batches.Remove(selectedBatch);
                            ApplyCurrentFilter();
                            
                            MessageBox.Show(LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.M1BatchDeletedSuccessfully"), 
                                LocalizationService.Instance.GetString("UnifiedBatchTrackingWindow.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Batch bulunamadı. Zaten silinmiş olabilir.", "Uyarı", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            
                            // UI'dan da kaldır
                            _m1Batches.Remove(selectedBatch);
                            ApplyCurrentFilter();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"M1 Batch silme hatası:\n{ex.Message}", "Hata", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek M1 batch'i seçin.", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void M2DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (M2BatchDataGrid.SelectedItem is ConcreteBatch2 selectedBatch)
            {
                var result = MessageBox.Show($"M2 Batch ID {selectedBatch.Id} silinsin mi?", "Batch Silme", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Thread-safe: Her işlem için yeni DbContext
                        using var context = new ProductionDbContext();
                        
                        // Veritabanından güncel entity'yi al (PlantCode=MIXER2)
                        var batchToDelete = context.ConcreteBatch2s.Find(selectedBatch.Id);
                        if (batchToDelete != null)
                        {
                            context.ConcreteBatch2s.Remove(batchToDelete);
                            context.SaveChanges();
                            
                            _m2Batches.Remove(selectedBatch);
                            ApplyCurrentFilter();
                            
                            MessageBox.Show("M2 Batch başarıyla silindi.", "Başarılı", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Batch bulunamadı. Zaten silinmiş olabilir.", "Uyarı", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            
                            // UI'dan da kaldır
                            _m2Batches.Remove(selectedBatch);
                            ApplyCurrentFilter();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"M2 Batch silme hatası:\n{ex.Message}", "Hata", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek M2 batch'i seçin.", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void M1BatchDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (M1BatchDataGrid.SelectedItem is ConcreteBatch selectedBatch)
            {
                // M1 batch detayları gösterilebilir
                // Şimdilik sadece seçim yapıldığını belirt
            }
        }

        private void M2BatchDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (M2BatchDataGrid.SelectedItem is ConcreteBatch selectedBatch)
            {
                // M2 batch detayları gösterilebilir
                // Şimdilik sadece seçim yapıldığını belirt
            }
        }

        #endregion
        
        #region Toplu Silme Fonksiyonları
        
        /// <summary>
        /// M1 tamamlanan batch'leri toplu sil
        /// </summary>
        private async void M1DeleteAllCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var completedBatches = _m1Batches.Where(b => b.Status == "Tamamlandı" || b.Status == "Harç Hazır").ToList();
                
                if (completedBatches.Count == 0)
                {
                    MessageBox.Show("Silinecek tamamlanmış M1 batch bulunamadı.", "Bilgi", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var result = MessageBox.Show(
                    $"{completedBatches.Count} adet tamamlanmış M1 batch silinecek. Emin misiniz?", 
                    "Toplu Silme", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Thread-safe: Her işlem için yeni DbContext
                    using var context = new ProductionDbContext();
                    
                    int deletedCount = 0;
                    foreach (var batch in completedBatches)
                    {
                        try
                        {
                            var batchToDelete = await context.ConcreteBatches.FindAsync(batch.Id);
                            if (batchToDelete != null)
                            {
                                context.ConcreteBatches.Remove(batchToDelete);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Batch {batch.Id} silme hatası: {ex.Message}");
                        }
                    }
                    
                    await context.SaveChangesAsync();
                    await LoadAllBatchesAsync();
                    
                    MessageBox.Show($"{deletedCount} adet M1 batch başarıyla silindi.", "Başarılı", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Toplu silme hatası:\n{ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// M2 tamamlanan batch'leri toplu sil
        /// </summary>
        private async void M2DeleteAllCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var completedBatches = _m2Batches.Where(b => b.Status == "Tamamlandı" || b.Status == "Harç Hazır").ToList();
                
                if (completedBatches.Count == 0)
                {
                    MessageBox.Show("Silinecek tamamlanmış M2 batch bulunamadı.", "Bilgi", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var result = MessageBox.Show(
                    $"{completedBatches.Count} adet tamamlanmış M2 batch silinecek. Emin misiniz?", 
                    "Toplu Silme", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Thread-safe: Her işlem için yeni DbContext
                    using var context = new ProductionDbContext();
                    
                    int deletedCount = 0;
                    foreach (var batch in completedBatches)
                    {
                        try
                        {
                            // ConcreteBatch2 tablosundan sil
                            var batchToDelete = await context.ConcreteBatch2s.FindAsync(batch.Id);
                            if (batchToDelete != null)
                            {
                                context.ConcreteBatch2s.Remove(batchToDelete);
                                deletedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Batch {batch.Id} silme hatası: {ex.Message}");
                        }
                    }
                    
                    await context.SaveChangesAsync();
                    await LoadAllBatchesAsync();
                    
                    MessageBox.Show($"{deletedCount} adet M2 batch başarıyla silindi.", "Başarılı", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Toplu silme hatası:\n{ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion

        #region Katkı Sinyalleri Methods

        /// <summary>
        /// Katkı sinyalleri event handler
        /// </summary>
        private void OnKatkiSignalsReceived(string message)
        {
            // UI thread'de çalıştır
            Dispatcher.Invoke(() =>
            {
                KatkiSignalsText.Text = message;
            });
        }

        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            // Timer'ı durdur
            _autoRefreshTimer?.Stop();
            
            // DbContext artık her işlemde yeni oluşturuluyor, dispose etmeye gerek yok
        }
    }
}
