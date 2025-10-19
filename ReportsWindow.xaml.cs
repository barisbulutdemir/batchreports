using System;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;


namespace takip
{
    public partial class ReportsWindow : Window
    {
        private string _lastFetchedMixer = ""; // Son getirilen mixer'ı takip et
        
        public ReportsWindow()
        {
            try
            {
                InitializeComponent();
                Loaded += ReportsWindow_Loaded;
                FetchM1Button.Click += FetchM1Button_Click;
                FetchM2Button.Click += FetchM2Button_Click;
                ExportReportButton.Click += ExportReportButton_Click;
                // Mixer1/Mixer2 hızlı raporlama butonları kaldırıldı
                PresetCombo.SelectionChanged += PresetCombo_SelectionChanged;
                PurgeButton.Click += PurgeButton_Click;
                PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Reports window startup error: {0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ReportsWindow] Constructor hatası: {ex}");
            }
        }

        // İç DataGrid'lerde mouse tekerleği olayını üst ScrollViewer'a kabarcıklat
        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                if (e.Handled) return;
                e.Handled = true;

                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = System.Windows.UIElement.MouseWheelEvent,
                    Source = sender
                };

                // Görsel ağacında yukarı doğru ilerleyerek en yakın UIElement'lere ilet
                var parent = System.Windows.Media.VisualTreeHelper.GetParent((DependencyObject)sender) as System.Windows.UIElement;
                while (parent != null)
                {
                    parent.RaiseEvent(eventArg);
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as System.Windows.UIElement;
                }
            }
            catch { }
        }

        private void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Varsayılan olarak bugünün verilerini göster
                StartDate.SelectedDate = DateTime.Today;
                EndDate.SelectedDate = DateTime.Today;
                PresetCombo.SelectedIndex = 0; // "Today" seçeneği
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Reports window load error: {0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ReportsWindow] Loaded hatası: {ex}");
            }
        }

        private void SetRangeAndFetch(TimeSpan span)
        {
            if (span.TotalDays == 0) // Today
            {
                StartDate.SelectedDate = DateTime.Today;
                EndDate.SelectedDate = DateTime.Today;
            }
            else
            {
                EndDate.SelectedDate = DateTime.Today;
                StartDate.SelectedDate = DateTime.Today - span;
            }
            // Varsayılan Mixer1 getir
            FetchM1Button_Click(this, new RoutedEventArgs());
        }

        private void PresetCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (PresetCombo.SelectedIndex == 0) SetRangeAndFetch(TimeSpan.FromDays(0)); // Today
                else if (PresetCombo.SelectedIndex == 1) SetRangeAndFetch(TimeSpan.FromDays(1));
                else if (PresetCombo.SelectedIndex == 2) SetRangeAndFetch(TimeSpan.FromDays(7));
                else if (PresetCombo.SelectedIndex == 3) SetRangeAndFetch(TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Preset error: {ex.Message}";
            }
        }

        private void FetchM1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading Mixer1 data...";
                
                var localStart = StartDate.SelectedDate?.Date ?? DateTime.Today.AddDays(-7);
                var localEnd = EndDate.SelectedDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);
                var start = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
                var end = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();
                
                using var context = new ProductionDbContext();
                var batches = context.ConcreteBatches
                    .Where(b => b.OccurredAt >= start && b.OccurredAt <= end && b.Status == "Tamamlandı") // Sadece tamamlanmış batch'ler
                    .OrderByDescending(b => b.OccurredAt)
                    .Take(10000) // Maksimum 10,000 kayıt ile sınırla
                    .AsNoTracking()
                    .ToList();

                var totalCement = (long)Math.Round(batches.Sum(b => b.TotalCementKg), 0);
                var totalAgg = (long)Math.Round(batches.Sum(b => b.TotalAggregateKg), 0);
                var totalPigment = Math.Round(batches.Sum(b => b.PigmentKg), 1);
                var totalWaterEff = Math.Round(batches.Sum(b => b.TotalWaterKg), 1); // Total Water (Loadcell + Pulse + Admixture Water)
                var moistVals = batches.Where(b => b.MoisturePercent.HasValue).Select(b => b.MoisturePercent!.Value).ToList();
                var avgMoist = moistVals.Count > 0 ? Math.Round(moistVals.Average(), 0) : 0;
                var avgCementPerBatch = batches.Count > 0 ? (long)Math.Round(batches.Average(b => b.TotalCementKg), 0) : 0;
                var cementSharePercent = (totalCement + totalAgg) > 0 ? Math.Round((double)totalCement / (totalCement + totalAgg) * 100.0, 0) : 0;

                // Kırılımlar - Sadece Mixer1 verilerini al
                // Alias'ları yükle
                var cementAliases = context.CementAliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                var aggregateAliases = context.AggregateAliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                var admixtureAliases = context.AdmixtureAliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);

                // Cement data - Mixer1 only (with aliases)
                var cementRows1 = context.ConcreteBatchCements
                    .Include(c => c.Batch)
                    .Where(c => c.Batch.OccurredAt >= start && c.Batch.OccurredAt <= end && c.Batch.Status == "Tamamlandı")
                    .Select(c => new { c.CementType, c.Slot, c.WeightKg })
                    .AsNoTracking()
                    .ToList();
                
                var cementTotals = cementRows1
                    .GroupBy(c => cementAliases.TryGetValue(c.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                        ? aliasName 
                        : (!string.IsNullOrWhiteSpace(c.CementType) ? c.CementType : $"Cement{c.Slot}"))
                    .Select(g => new { Tip = g.Key, ToplamKg = (long)Math.Round(g.Sum(x => x.WeightKg), 0) })
                    .OrderByDescending(x => x.ToplamKg)
                    .ToList();

                // Aggregate data - Mixer1 only (with aliases)
                var aggRows1 = context.ConcreteBatchAggregates
                    .Include(a => a.Batch)
                    .Where(a => a.Batch.OccurredAt >= start && a.Batch.OccurredAt <= end && a.Batch.Status == "Tamamlandı")
                    .Select(a => new { a.Name, a.Slot, a.WeightKg })
                    .AsNoTracking()
                    .ToList();
                
                var aggTotals = aggRows1
                    .GroupBy(r => aggregateAliases.TryGetValue(r.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                        ? aliasName 
                        : (!string.IsNullOrWhiteSpace(r.Name) ? r.Name : $"Aggregate{r.Slot}"))
                    .Select(g => new { Ad = g.Key, ToplamKg = (long)Math.Round(g.Sum(x => x.WeightKg), 0) })
                    .OrderByDescending(x => x.ToplamKg)
                    .ToList();

                // Admixture data - Mixer1 only (with aliases)
                var admRows1 = context.ConcreteBatchAdmixtures
                    .Include(a => a.Batch)
                    .Where(a => a.Batch.OccurredAt >= start && a.Batch.OccurredAt <= end && a.Batch.Status == "Tamamlandı")
                    .Select(a => new { a.Name, a.Slot, a.ChemicalKg, a.WaterKg })
                    .AsNoTracking()
                    .ToList();
                
                var admTotals = admRows1
                    .GroupBy(a => admixtureAliases.TryGetValue(a.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                        ? aliasName 
                        : (!string.IsNullOrWhiteSpace(a.Name) ? a.Name : $"Admixture{a.Slot}"))
                    .Select(g => new { Ad = g.Key, KimyasalKg = Math.Round(g.Sum(x => x.ChemicalKg), 3), SuKg = Math.Round(g.Sum(x => x.WaterKg), 1) })
                    .OrderByDescending(x => x.KimyasalKg)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine(string.Format("Total Batch: {0}", batches.Count));
                sb.AppendLine($"Cement: {totalCement} kg (Avg: {avgCementPerBatch} kg)");
                sb.AppendLine($"Agrega: {totalAgg} kg");
                sb.AppendLine($"Pigment: {totalPigment:0.0} kg");
                sb.AppendLine($"Total Water: {totalWaterEff:0.0} kg");
                sb.AppendLine($"Average Moisture: {avgMoist:0}%");
                sb.AppendLine();
                sb.AppendLine($"Cement: ");
                foreach (var c in cementTotals) sb.AppendLine($"  - {c.Tip}: {c.ToplamKg} kg");
                sb.AppendLine();
                sb.AppendLine($"Aggregate: ");
                foreach (var a in aggTotals) sb.AppendLine($"  - {a.Ad}: {a.ToplamKg} kg");
                sb.AppendLine();
                sb.AppendLine($"Admixture: ");
                foreach (var k in admTotals) sb.AppendLine($"  - {k.Ad}: {k.KimyasalKg:0.000} kg (Water: {k.SuKg:0.0} kg)");
                sb.AppendLine();
                sb.AppendLine($"Pigment Total: {totalPigment:0.0} kg");

                // General table
                SummaryGeneralGrid.ItemsSource = new System.Collections.Generic.List<object>
                {
                    new { Bilgi = "Total Batch", Deger = batches.Count.ToString() },
                    new { Bilgi = "Cement", Deger = $"{totalCement} kg  ( average {avgCementPerBatch} kg %{cementSharePercent:0} )" },
                    new { Bilgi = "Aggregate (Total)", Deger = $"{totalAgg} kg" },
                    new { Bilgi = "Pigment (Total)", Deger = $"{totalPigment:0.0} kg" },
                    new { Bilgi = "Total Water", Deger = $"{totalWaterEff:0.0} kg" },
                    new { Bilgi = "Average Moisture", Deger = $"{avgMoist:0}%" }
                };
                CementSummaryGrid.ItemsSource = cementTotals.Select(c => new { Ad = c.Tip, ToplamKg = c.ToplamKg }).ToList();
                AggregateSummaryGrid.ItemsSource = aggTotals.Select(a => new { Ad = a.Ad, ToplamKg = a.ToplamKg }).ToList(); // ✅ Mixer1 agrega detayları
                AdmixtureSummaryGrid.ItemsSource = admTotals;
                PigmentSummaryGrid.ItemsSource = new System.Collections.Generic.List<object> { new { Ad = "Pigment Total", ToplamKg = totalPigment } };
                
                StatusText.Text = string.Format("Mixer1 data loaded: {0} batches", batches.Count);
                _lastFetchedMixer = "Mixer1"; // Son getirilen mixer'ı kaydet
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Mixer1 data loading error: {ex.Message}";
                MessageBox.Show(string.Format("Mixer1 report loading error: {0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ReportsWindow] Mixer1 veri yükleme hatası: {ex}");
            }
        }

        private void FetchM2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading Mixer2 data...";
                
                var localStart = StartDate.SelectedDate?.Date ?? DateTime.Today.AddDays(-7);
                var localEnd = EndDate.SelectedDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);
                var start = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
                var end = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();
                
                using var context = new ProductionDbContext();
                var batches2 = context.ConcreteBatch2s
                        .Where(b => b.OccurredAt >= start && b.OccurredAt <= end && b.Status == "Tamamlandı") // Sadece tamamlanmış batch'ler
                        .OrderByDescending(b => b.OccurredAt)
                        .Take(10000) // Maksimum 10,000 kayıt ile sınırla
                        .AsNoTracking()
                        .ToList();

                    var totalCement = (long)Math.Round(batches2.Sum(b => b.TotalCementKg), 0);
                    var totalAgg = (long)Math.Round(batches2.Sum(b => b.TotalAggregateKg), 0);
                    // totalPigment'i aşağıda pigment kırılımından hesaplayacağız
                    var totalWaterEff = Math.Round(batches2.Sum(b => b.TotalWaterKg), 1); // Total Water (Loadcell + Pulse + Admixture Water)
                    var avgMoist = batches2.Any() ? Math.Round(batches2.Average(b => b.MoisturePercent ?? 0), 0) : 0;

                    // Kırılımlar - Mixer2 alias'ları ile
                    var cement2Aliases = context.Cement2Aliases
                        .Where(a => a.IsActive)
                        .ToDictionary(x => x.Slot, x => x.Name);
                    var aggregate2Aliases = context.Aggregate2Aliases
                        .Where(a => a.IsActive)
                        .ToDictionary(x => x.Slot, x => x.Name);
                    var admixture2Aliases = context.Admixture2Aliases
                        .Where(a => a.IsActive)
                        .ToDictionary(x => x.Slot, x => x.Name);

                    var cementRows2 = context.ConcreteBatch2Cements
                        .Include(c => c.Batch)
                        .Where(c => c.Batch.OccurredAt >= start && c.Batch.OccurredAt <= end && c.Batch.Status == "Tamamlandı")
                        .Select(c => new { c.CementType, c.Slot, c.WeightKg })
                        .AsNoTracking()
                        .ToList();
                    var cementTotals2 = cementRows2
                        .GroupBy(c => cement2Aliases.TryGetValue(c.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                            ? aliasName 
                            : (!string.IsNullOrWhiteSpace(c.CementType) ? c.CementType : $"Cement{c.Slot}"))
                        .Select(g => new { Tip = g.Key, ToplamKg = (long)Math.Round(g.Sum(x => x.WeightKg), 0) })
                        .OrderByDescending(x => x.ToplamKg)
                        .ToList();

                    var aggRows2 = context.ConcreteBatch2Aggregates
                        .Include(a => a.Batch)
                        .Where(a => a.Batch.OccurredAt >= start && a.Batch.OccurredAt <= end && a.Batch.Status == "Tamamlandı")
                        .Select(a => new { a.Name, a.Slot, a.WeightKg })
                        .AsNoTracking()
                        .ToList();
                    var aggTotals2 = aggRows2
                        .GroupBy(r => aggregate2Aliases.TryGetValue(r.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                            ? aliasName 
                            : (!string.IsNullOrWhiteSpace(r.Name) ? r.Name : $"Aggregate{r.Slot}"))
                        .Select(g => new { Ad = g.Key, ToplamKg = (long)Math.Round(g.Sum(x => x.WeightKg), 0) })
                        .OrderByDescending(x => x.ToplamKg)
                        .ToList();

                    var admRows2 = context.ConcreteBatch2Admixtures
                        .Include(a => a.Batch)
                        .Where(a => a.Batch.OccurredAt >= start && a.Batch.OccurredAt <= end && a.Batch.Status == "Tamamlandı")
                        .Select(a => new { a.Name, a.Slot, a.ChemicalKg, a.WaterKg })
                        .AsNoTracking()
                        .ToList();
                    var admTotals2 = admRows2
                        .GroupBy(a => admixture2Aliases.TryGetValue(a.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                            ? aliasName 
                            : (!string.IsNullOrWhiteSpace(a.Name) ? a.Name : $"Admixture{a.Slot}"))
                        .Select(g => new { Ad = g.Key, KimyasalKg = Math.Round(g.Sum(x => x.ChemicalKg), 3), SuKg = Math.Round(g.Sum(x => x.WaterKg), 1) })
                        .OrderByDescending(x => x.KimyasalKg)
                        .ToList();

                    // Pigment kırılımı (alias ile) - Sadece ayrı pigment değerlerini topla
                    var pigmentAliases = context.Pigment2Aliases.ToDictionary(p => p.Slot, p => p.Name);
                    var pigmentTotalsDict = new System.Collections.Generic.Dictionary<string, double>();
                    foreach (var b in batches2)
                    {
                        if (b.Pigment1Kg > 0) { var n = pigmentAliases.ContainsKey(1) ? pigmentAliases[1] : "pigment1"; pigmentTotalsDict[n] = pigmentTotalsDict.GetValueOrDefault(n) + b.Pigment1Kg; }
                        if (b.Pigment2Kg > 0) { var n = pigmentAliases.ContainsKey(2) ? pigmentAliases[2] : "pigment2"; pigmentTotalsDict[n] = pigmentTotalsDict.GetValueOrDefault(n) + b.Pigment2Kg; }
                        if (b.Pigment3Kg > 0) { var n = pigmentAliases.ContainsKey(3) ? pigmentAliases[3] : "pigment3"; pigmentTotalsDict[n] = pigmentTotalsDict.GetValueOrDefault(n) + b.Pigment3Kg; }
                        if (b.Pigment4Kg > 0) { var n = pigmentAliases.ContainsKey(4) ? pigmentAliases[4] : "pigment4"; pigmentTotalsDict[n] = pigmentTotalsDict.GetValueOrDefault(n) + b.Pigment4Kg; }
                    }
                    
                    // TotalPigmentKg'yi pigment kırılımından hesapla (çifte toplama önlemek için)
                    var totalPigmentFromBreakdown = pigmentTotalsDict.Values.Sum();
                    var totalPigment = Math.Round(totalPigmentFromBreakdown, 1);

                    var avgCementPerBatch2 = batches2.Count > 0 ? (long)Math.Round(batches2.Average(b => b.TotalCementKg), 0) : 0;
                    var cementSharePercent2 = (totalCement + totalAgg) > 0 ? Math.Round((double)totalCement / (totalCement + totalAgg) * 100.0, 0) : 0;
                    var sb = new StringBuilder();
                    sb.AppendLine($"Total Batch: {batches2.Count}");
                    sb.AppendLine($"Cement: {totalCement} kg (Avg: {avgCementPerBatch2} kg)");
                    sb.AppendLine($"Agrega: {totalAgg} kg");
                    sb.AppendLine($"Pigment: {totalPigment:0.0} kg");
                    sb.AppendLine($"Total Water: {totalWaterEff:0.0} kg");
                    sb.AppendLine($"Average Moisture: {avgMoist:0}%");
                    sb.AppendLine();
                    sb.AppendLine("Cement:");
                    foreach (var c in cementTotals2) sb.AppendLine($"  - {c.Tip}: {c.ToplamKg} kg");
                    sb.AppendLine();
                    sb.AppendLine("Aggregate:");
                    foreach (var a in aggTotals2) sb.AppendLine($"  - {a.Ad}: {a.ToplamKg} kg");
                    sb.AppendLine();
                    sb.AppendLine("Admixture:");
                    foreach (var k in admTotals2) sb.AppendLine($"  - {k.Ad}: {k.KimyasalKg:0.000} kg (Water: {k.SuKg:0.0} kg)");
                    sb.AppendLine();
                    sb.AppendLine("Pigment:");
                    foreach (var kv in pigmentTotalsDict.OrderByDescending(x => x.Value)) sb.AppendLine($"  - {kv.Key}: {kv.Value:0.0} kg");

                    SummaryGeneralGrid.ItemsSource = new System.Collections.Generic.List<object>
                    {
                        new { Bilgi = "Total Batch", Deger = batches2.Count.ToString() },
                        new { Bilgi = "Cement", Deger = $"{totalCement} kg  ( average {avgCementPerBatch2} kg %{cementSharePercent2:0} )" },
                        new { Bilgi = "Aggregate (Total)", Deger = $"{totalAgg} kg" },
                        new { Bilgi = "Pigment (Total)", Deger = $"{totalPigment:0.0} kg" },
                        new { Bilgi = "Total Water", Deger = $"{totalWaterEff:0.0} kg" },
                        new { Bilgi = "Average Moisture", Deger = $"{avgMoist:0}%" }
                    };
                    CementSummaryGrid.ItemsSource = cementTotals2.Select(c => new { Ad = c.Tip, ToplamKg = c.ToplamKg }).ToList();
                    AggregateSummaryGrid.ItemsSource = aggTotals2;
                    AdmixtureSummaryGrid.ItemsSource = admTotals2;
                    PigmentSummaryGrid.ItemsSource = pigmentTotalsDict.OrderByDescending(x => x.Value).Select(kv => new { Ad = kv.Key, ToplamKg = Math.Round(kv.Value, 1) }).ToList();

                StatusText.Text = string.Format("Mixer2 data loaded: {0} batches", batches2.Count);
                _lastFetchedMixer = "Mixer2"; // Son getirilen mixer'ı kaydet
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Mixer2 data loading error: {ex.Message}";
                MessageBox.Show(string.Format("Mixer2 report loading error: {0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ReportsWindow] Mixer2 veri yükleme hatası: {ex}");
            }
        }

        // Mixer1/Mixer2 hızlı raporlama butonları kaldırıldı

        private async void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to delete all history?", 
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                using var context = new ProductionDbContext();
                context.ConcreteBatchAdmixtures.RemoveRange(context.ConcreteBatchAdmixtures);
                context.ConcreteBatchAggregates.RemoveRange(context.ConcreteBatchAggregates);
                context.ConcreteBatchCements.RemoveRange(context.ConcreteBatchCements);
                context.ConcreteBatches.RemoveRange(context.ConcreteBatches);

                context.ConcreteBatch2Admixtures.RemoveRange(context.ConcreteBatch2Admixtures);
                context.ConcreteBatch2Aggregates.RemoveRange(context.ConcreteBatch2Aggregates);
                context.ConcreteBatch2Cements.RemoveRange(context.ConcreteBatch2Cements);
                context.ConcreteBatch2s.RemoveRange(context.ConcreteBatch2s);

                await context.SaveChangesAsync();
                StatusText.Text = "All history deleted";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Deletion error: {ex.Message}";
            }
        }

        private void ExportReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Creating report...";
                
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (string.IsNullOrEmpty(desktop))
                {
                    StatusText.Text = "Desktop folder not found";
                    return;
                }

                // Son getirilen mixer'ı kullan
                string mixerType = _lastFetchedMixer;
                if (string.IsNullOrEmpty(mixerType))
                {
                    StatusText.Text = "Please load data first";
                    MessageBox.Show("Please load data first", 
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vardiya_Raporlari klasörünü kullan
                var exportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Vardiya_Raporlari");
                if (!System.IO.Directory.Exists(exportPath))
                {
                    System.IO.Directory.CreateDirectory(exportPath);
                }
                
                var path = System.IO.Path.Combine(exportPath, $"Temp_GeneralReport_{mixerType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                // Seçilen tarih aralığını al
                var localStart = StartDate.SelectedDate?.Date ?? DateTime.Today.AddDays(-7);
                var localEnd = EndDate.SelectedDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);
                var start = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
                var end = DateTime.SpecifyKind(localEnd, DateTimeKind.Local).ToUniversalTime();

                // Veri toplama - sadece seçilen tarih aralığındaki veriler
                using var context = new ProductionDbContext();
                List<ConcreteBatch> mixer1Data = new List<ConcreteBatch>();
                List<ConcreteBatch2> mixer2Data = new List<ConcreteBatch2>();
                
                try
                {
                    StatusText.Text = "Reading data...";
                    
                    // Sadece seçilen mixer'ın verilerini al
                    if (mixerType == "Mixer1")
                    {
                        mixer1Data = context.ConcreteBatches
                            .Where(b => b.OccurredAt >= start && b.OccurredAt <= end && b.Status == "Tamamlandı")
                            .OrderByDescending(b => b.OccurredAt)
                            .Take(10000) // Maksimum 10,000 kayıt ile sınırla
                            .AsNoTracking()
                            .ToList();
                    }
                    else if (mixerType == "Mixer2")
                    {
                        mixer2Data = context.ConcreteBatch2s
                            .Where(b => b.OccurredAt >= start && b.OccurredAt <= end && b.Status == "Tamamlandı")
                            .OrderByDescending(b => b.OccurredAt)
                            .Take(10000) // Maksimum 10,000 kayıt ile sınırla
                            .AsNoTracking()
                            .ToList();
                    }
                    
                    StatusText.Text = "Preparing data...";
                }
                catch (Exception ex)
                {
                    StatusText.Text = string.Format("Data read error: {0}", ex.Message);
                    MessageBox.Show(string.Format("Data read error: {0}", ex.Message), 
                        "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

       // PDF raporu oluştur
       try
       {
           StatusText.Text = "Creating PDF report...";
           
           using (var fileStream = new FileStream(path, FileMode.Create))
           {
               var document = new Document(PageSize.A4, 50, 50, 25, 25);
               var writer = PdfWriter.GetInstance(document, fileStream);
               
               document.Open();

               // Başlık
               var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
               var title = new Paragraph(FixTurkishCharacters($"CONCRETE PLANT {mixerType.ToUpper()} REPORT"), titleFont)
               {
                   Alignment = Element.ALIGN_CENTER,
                   SpacingAfter = 20
               };
               document.Add(title);

               // Tarih aralığı
               var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
               var dateRangeText = $"Date Range: {localStart:dd.MM.yyyy} - {EndDate.SelectedDate?.Date:dd.MM.yyyy}";
               var dateParagraph = new Paragraph(FixTurkishCharacters(dateRangeText), dateFont)
               {
                   Alignment = Element.ALIGN_CENTER,
                   SpacingAfter = 20
               };
               document.Add(dateParagraph);
               
               // Ekranda gösterilen verileri PDF'e aktar
               var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);
               var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.DARK_GRAY);
               
               // Toplam verileri hesapla - sadece seçilen mixer
               var totalCement = mixerType == "Mixer1" ? (mixer1Data?.Sum(b => b.TotalCementKg) ?? 0) : (mixer2Data?.Sum(b => b.TotalCementKg) ?? 0);
               var totalAggregate = mixerType == "Mixer1" ? (mixer1Data?.Sum(b => b.TotalAggregateKg) ?? 0) : (mixer2Data?.Sum(b => b.TotalAggregateKg) ?? 0);
               var totalAdmixture = mixerType == "Mixer1" ? (mixer1Data?.Sum(b => b.TotalAdmixtureKg) ?? 0) : (mixer2Data?.Sum(b => b.TotalAdmixtureKg) ?? 0);
               var totalPigment = mixerType == "Mixer1" ? (mixer1Data?.Sum(b => b.PigmentKg) ?? 0) : (mixer2Data?.Sum(b => b.TotalPigmentKg) ?? 0);
               var totalWater = mixerType == "Mixer1" ? (mixer1Data?.Sum(b => b.TotalWaterKg) ?? 0) : (mixer2Data?.Sum(b => b.TotalWaterKg) ?? 0);
               var totalBatchCount = mixerType == "Mixer1" ? (mixer1Data?.Count ?? 0) : (mixer2Data?.Count ?? 0);
               var avgCementPerBatch = totalBatchCount > 0 ? Math.Round(totalCement / totalBatchCount, 0, MidpointRounding.AwayFromZero) : 0;
               var cementSharePercent = (totalCement + totalAggregate) > 0 ? Math.Round((totalCement / (totalCement + totalAggregate)) * 100, 0, MidpointRounding.AwayFromZero) : 0;
               
               // Nem ortalaması - sadece seçilen mixer
               var allMoistureValues = new List<double>();
               if (mixerType == "Mixer1" && mixer1Data?.Any() == true) 
                   allMoistureValues.AddRange(mixer1Data.Where(b => b.MoisturePercent.HasValue).Select(b => b.MoisturePercent.Value));
               else if (mixerType == "Mixer2" && mixer2Data?.Any() == true) 
                   allMoistureValues.AddRange(mixer2Data.Where(b => b.MoisturePercent.HasValue).Select(b => b.MoisturePercent.Value));
               var avgMoisture = allMoistureValues.Any() ? allMoistureValues.Average() : 0;
               
               // GENEL BÖLÜMÜ - Tablo formatında
               var generalTable = new PdfPTable(2)
               {
                   WidthPercentage = 100,
                   SpacingAfter = 15
               };
               
               // Genel tablo başlığı
               var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
               var headerCell = new PdfPCell(new Phrase(FixTurkishCharacters("GENERAL SUMMARY"), headerFont))
               {
                   BackgroundColor = BaseColor.DARK_GRAY,
                   HorizontalAlignment = Element.ALIGN_CENTER,
                   Colspan = 2,
                   Padding = 8
               };
               generalTable.AddCell(headerCell);
               
               // Sütun başlıkları
               var columnFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Information"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Value"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
               
               // Veri satırları
               var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total Batch"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{totalBatchCount}"), dataFont)) { Padding = 4 });
               
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Cement"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(totalCement, 0)} kg (ortalama {avgCementPerBatch} kg %{cementSharePercent})"), dataFont)) { Padding = 4 });
               
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Aggregate Total"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(totalAggregate, 0)} kg"), dataFont)) { Padding = 4 });
               
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Pigment Total"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(totalPigment, 1)} kg"), dataFont)) { Padding = 4 });
               
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total Water"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(totalWater, 1)} kg"), dataFont)) { Padding = 4 });
               
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Average Moisture"), dataFont)) { Padding = 4 });
               generalTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(avgMoisture, 0)}%"), dataFont)) { Padding = 4 });
               
               document.Add(generalTable);
               
               // ÇİMENTO BÖLÜMÜ - Tablo formatında (alias'larla)
               if ((mixerType == "Mixer1" && mixer1Data?.Any() == true) || (mixerType == "Mixer2" && mixer2Data?.Any() == true))
               {
                   // Alias'ları yükle
                   Dictionary<short, string> cementAliases;
                   if (mixerType == "Mixer1")
                   {
                       cementAliases = context.CementAliases.Where(a => a.IsActive).ToDictionary(x => x.Slot, x => x.Name);
                   }
                   else
                   {
                       cementAliases = context.Cement2Aliases.Where(a => a.IsActive).ToDictionary(x => (short)x.Slot, x => x.Name);
                   }
                   
                   var cementRows = new List<dynamic>();
                   
                   // Selected mixer's cement data (with aliases)
                   if (mixerType == "Mixer1" && mixer1Data?.Any() == true)
                   {
                       var batchIds = mixer1Data.Select(b => b.Id).ToList();
                       var m1CementRows = context.ConcreteBatchCements
                           .Where(c => batchIds.Contains(c.BatchId))
                           .Select(c => new { CementType = c.CementType, Slot = c.Slot, WeightKg = c.WeightKg })
                           .AsNoTracking()
                           .ToList();
                       cementRows.AddRange(m1CementRows);
                   }
                   else if (mixerType == "Mixer2" && mixer2Data?.Any() == true)
                   {
                       var batchIds2 = mixer2Data.Select(b => b.Id).ToList();
                       var m2CementRows = context.ConcreteBatch2Cements
                           .Where(c => batchIds2.Contains(c.BatchId))
                           .Select(c => new { CementType = c.CementType, Slot = c.Slot, WeightKg = c.WeightKg })
                           .AsNoTracking()
                           .ToList();
                       cementRows.AddRange(m2CementRows);
                   }
                   
                   var cementTotals = cementRows
                       .GroupBy(c => cementAliases.TryGetValue(c.Slot, out string? aliasName) && !string.IsNullOrWhiteSpace(aliasName) 
                           ? aliasName 
                           : (!string.IsNullOrWhiteSpace(c.CementType) ? c.CementType : $"Cement{c.Slot}"))
                       .Select(g => new { 
                           Tip = g.Key, 
                           ToplamKg = Convert.ToInt64(Math.Round(Convert.ToDouble(g.Sum(x => (double)x.WeightKg)), 0)) 
                       })
                       .OrderByDescending(x => x.ToplamKg)
                       .ToList();
                   
                   if (cementTotals.Any())
                   {
                       var cementTable = new PdfPTable(2)
                       {
                           WidthPercentage = 100,
                           SpacingAfter = 15
                       };
                       
                       // Cement table header
                       var cementHeaderCell = new PdfPCell(new Phrase(FixTurkishCharacters("CEMENT DETAIL"), headerFont))
                       {
                           BackgroundColor = BaseColor.DARK_GRAY,
                           HorizontalAlignment = Element.ALIGN_CENTER,
                           Colspan = 2,
                           Padding = 8
                       };
                       cementTable.AddCell(cementHeaderCell);
                       
                       // Sütun başlıkları
                       cementTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Cement Type"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       cementTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total (kg)"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       
                       // Veri satırları
                       foreach (var cement in cementTotals)
                       {
                           cementTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(cement.Tip ?? "Unknown"), dataFont)) { Padding = 4 });
                           cementTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{cement.ToplamKg}"), dataFont)) { Padding = 4 });
                       }
                       
                       document.Add(cementTable);
                   }
               }
               
               // AGGREGATE SECTION - Table format (both Mixer1 and Mixer2)
               if ((mixer1Data != null && mixer1Data.Any()) || (mixer2Data != null && mixer2Data.Any()))
               {
                   var aggRows = new List<dynamic>();
                   
                   // Mixer1 aggregate data
                   if (mixer1Data != null && mixer1Data.Any())
                   {
                       var batchIds = mixer1Data.Select(b => b.Id).ToList();
                       var m1AggRows = context.ConcreteBatchAggregates
                           .Where(a => batchIds.Contains(a.BatchId))
                           .Select(a => new { Name = a.Name, Slot = a.Slot, WeightKg = a.WeightKg })
                           .AsNoTracking()
                           .ToList();
                       aggRows.AddRange(m1AggRows);
                   }
                   
                   // Mixer2 aggregate data
                   if (mixer2Data != null && mixer2Data.Any())
                   {
                       var batchIds2 = mixer2Data.Select(b => b.Id).ToList();
                       var m2AggRows = context.ConcreteBatch2Aggregates
                           .Where(a => batchIds2.Contains(a.BatchId))
                           .Select(a => new { Name = a.Name, Slot = a.Slot, WeightKg = a.WeightKg })
                           .AsNoTracking()
                           .ToList();
                       aggRows.AddRange(m2AggRows);
                   }
                   
                   var aggTotals = aggRows
                       .GroupBy(r => string.IsNullOrWhiteSpace(r.Name) ? ($"agrega{r.Slot}") : r.Name)
                       .Select(g => new { 
                           Ad = g.Key, 
                           ToplamKg = Convert.ToInt64(Math.Round(Convert.ToDouble(g.Sum(x => (double)x.WeightKg)), 0)) 
                       })
                       .OrderByDescending(x => x.ToplamKg)
                       .ToList();
                   
                   if (aggTotals.Any())
                   {
                       var aggTable = new PdfPTable(2)
                       {
                           WidthPercentage = 100,
                           SpacingAfter = 15
                       };
                       
                       // Aggregate table header
                       var aggHeaderCell = new PdfPCell(new Phrase(FixTurkishCharacters("AGGREGATE DETAIL"), headerFont))
                       {
                           BackgroundColor = BaseColor.DARK_GRAY,
                           HorizontalAlignment = Element.ALIGN_CENTER,
                           Colspan = 2,
                           Padding = 8
                       };
                       aggTable.AddCell(aggHeaderCell);
                       
                       // Sütun başlıkları
                       aggTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Aggregate Type"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       aggTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total (kg)"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       
                       // Veri satırları
                       foreach (var aggregate in aggTotals)
                       {
                           aggTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(aggregate.Ad), dataFont)) { Padding = 4 });
                           aggTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{aggregate.ToplamKg}"), dataFont)) { Padding = 4 });
                       }
                       
                       document.Add(aggTable);
                   }
               }
               
               // ADMIXTURE SECTION - Table format (both Mixer1 and Mixer2)
               if ((mixer1Data != null && mixer1Data.Any()) || (mixer2Data != null && mixer2Data.Any()))
               {
                   var admRows = new List<dynamic>();
                   
                   // Mixer1 admixture data
                   if (mixer1Data != null && mixer1Data.Any())
                   {
                       var batchIds = mixer1Data.Select(b => b.Id).ToList();
                       var m1AdmRows = context.ConcreteBatchAdmixtures
                           .Where(a => batchIds.Contains(a.BatchId))
                           .Select(a => new { Name = a.Name, Slot = a.Slot, ChemicalKg = a.ChemicalKg, WaterKg = a.WaterKg })
                           .AsNoTracking()
                           .ToList();
                       admRows.AddRange(m1AdmRows);
                   }
                   
                   // Mixer2 admixture data
                   if (mixer2Data != null && mixer2Data.Any())
                   {
                       var batchIds2 = mixer2Data.Select(b => b.Id).ToList();
                       var m2AdmRows = context.ConcreteBatch2Admixtures
                           .Where(a => batchIds2.Contains(a.BatchId))
                           .Select(a => new { Name = a.Name, Slot = a.Slot, ChemicalKg = a.ChemicalKg, WaterKg = a.WaterKg })
                           .AsNoTracking()
                           .ToList();
                       admRows.AddRange(m2AdmRows);
                   }
                   
                   var admTotals = admRows
                       .GroupBy(a => string.IsNullOrWhiteSpace(a.Name) ? ($"katki{a.Slot}") : a.Name)
                       .Select(g => new { 
                           Ad = g.Key, 
                           KimyasalKg = Math.Round(Convert.ToDouble(g.Sum(x => (double)x.ChemicalKg)), 3), 
                           SuKg = Math.Round(Convert.ToDouble(g.Sum(x => (double)x.WaterKg)), 1) 
                       })
                       .OrderByDescending(x => x.KimyasalKg)
                       .ToList();
                   
                   if (admTotals.Any())
                   {
                       var admTable = new PdfPTable(3)
                       {
                           WidthPercentage = 100,
                           SpacingAfter = 15
                       };
                       
                       // Admixture table header
                       var admHeaderCell = new PdfPCell(new Phrase(FixTurkishCharacters("ADMIXTURE DETAIL"), headerFont))
                       {
                           BackgroundColor = BaseColor.DARK_GRAY,
                           HorizontalAlignment = Element.ALIGN_CENTER,
                           Colspan = 3,
                           Padding = 8
                       };
                       admTable.AddCell(admHeaderCell);
                       
                       // Sütun başlıkları
                       admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Admixture Type"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Chemical (kg)"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Water (kg)"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
                       
                       // Veri satırları
                       foreach (var admixture in admTotals)
                       {
                           admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(admixture.Ad), dataFont)) { Padding = 4 });
                           admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{admixture.KimyasalKg:0.000}"), dataFont)) { Padding = 4 });
                           admTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{admixture.SuKg:0.0}"), dataFont)) { Padding = 4 });
                       }
                       
                       document.Add(admTable);
                   }
               }
               
               // PIGMENT SECTION - Table format
               var boyaTable = new PdfPTable(2)
               {
                   WidthPercentage = 100,
                   SpacingAfter = 15
               };
               
               // Pigment table header
               var boyaHeaderCell = new PdfPCell(new Phrase(FixTurkishCharacters("PIGMENT DETAIL"), headerFont))
               {
                   BackgroundColor = BaseColor.DARK_GRAY,
                   HorizontalAlignment = Element.ALIGN_CENTER,
                   Colspan = 2,
                   Padding = 8
               };
               boyaTable.AddCell(boyaHeaderCell);
               
               // Sütun başlıkları
               boyaTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Pigment Type"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
               boyaTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total (kg)"), columnFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 6 });
               
               // Veri satırları
               boyaTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters("Total Pigment"), dataFont)) { Padding = 4 });
               boyaTable.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters($"{Math.Round(totalPigment, 1)}"), dataFont)) { Padding = 4 });
               
               document.Add(boyaTable);

               // Silo verileri tablosu - KALDIRILDI (mixer bazlı raporlar için gerekli değil)
               // Silo verileri artık mixer bazlı raporlarda gösterilmiyor

               // Alt bilgi
               document.Add(new Paragraph(" ", normalFont)); // Boş satır
               var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);
               var footer = new Paragraph($"Report Creation Date: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", footerFont)
               {
                   Alignment = Element.ALIGN_RIGHT
               };
               document.Add(footer);

               document.Close();
           }
           
           try 
           { 
               System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
               { 
                   FileName = path, 
                   UseShellExecute = true 
               }); 
           } 
           catch (Exception openEx)
           {
               StatusText.Text = $"PDF saved but could not be opened: {openEx.Message}";
               return;
           }
           
           // 5 dakika sonra dosyayı sil
           _ = Task.Run(async () =>
           {
               await Task.Delay(5 * 60 * 1000); // 5 dakika bekle
               try
               {
                   if (System.IO.File.Exists(path))
                   {
                       System.IO.File.Delete(path);
                       DetailedLogger.LogInfo($"Geçici PDF dosyası silindi: {System.IO.Path.GetFileName(path)}");
                   }
               }
               catch (Exception ex)
               {
                   DetailedLogger.LogError($"Geçici PDF silme hatası: {ex.Message}");
               }
           });
           
           StatusText.Text = $"PDF created and opened on screen. Will be automatically deleted in 5 minutes.";
           
           // Kullanıcıya MessageBox ile bilgi ver
           MessageBox.Show("PDF created and opened on screen.\nWill be automatically deleted in 5 minutes.", 
               "Success", MessageBoxButton.OK, MessageBoxImage.Information);
       }
       catch (Exception reportEx)
       {
           StatusText.Text = $"PDF creation error: {reportEx.Message}";
           var errorDetails = $"PDF creation error:\n{reportEx.Message}\n\nError Type: {reportEx.GetType().Name}\n\nDetails: {reportEx.StackTrace}";
           
           // Inner exception varsa onu da göster
           if (reportEx.InnerException != null)
           {
               errorDetails += $"\n\nInner Error: {reportEx.InnerException.Message}\nInner Error Details: {reportEx.InnerException.StackTrace}";
           }
           
           MessageBox.Show(errorDetails, "PDF Error", 
               MessageBoxButton.OK, MessageBoxImage.Error);
           return;
       }
                
                /*
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
                {
                    var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);
                    var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    var titleFont = iTextSharp.text.FontFactory.GetFont("Helvetica", 14, iTextSharp.text.Font.BOLD);
                    var headerFont = iTextSharp.text.FontFactory.GetFont("Helvetica", 11, iTextSharp.text.Font.BOLD);
                    var cellFont = iTextSharp.text.FontFactory.GetFont("Helvetica", 10);

                    doc.Add(new iTextSharp.text.Paragraph("Concrete Plant Summary", titleFont)
                    {
                        Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                        SpacingAfter = 12f
                    });

                    var startTxt = StartDate.SelectedDate.HasValue ? StartDate.SelectedDate.Value.ToString("dd.MM.yyyy") : "-";
                    var endTxt = EndDate.SelectedDate.HasValue ? EndDate.SelectedDate.Value.ToString("dd.MM.yyyy") : "-";
                    doc.Add(new iTextSharp.text.Paragraph($"Tarih Aralığı: {startTxt} - {endTxt}", cellFont)
                    {
                        Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                        SpacingAfter = 10f
                    });

                    iTextSharp.text.pdf.PdfPCell MakeHeader(string text)
                    {
                        var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(text, headerFont));
                        cell.BackgroundColor = new iTextSharp.text.BaseColor(230, 230, 230);
                        cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
                        cell.Padding = 5f;
                        return cell;
                    }

                    iTextSharp.text.pdf.PdfPCell MakeCell(string text)
                    {
                        var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(text, cellFont));
                        cell.Padding = 5f;
                        return cell;
                    }

                    var summaryTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
                    summaryTable.SetWidths(new float[] { 40f, 60f });
                    summaryTable.AddCell(MakeHeader("Information"));
                    summaryTable.AddCell(MakeHeader("Value"));
                    foreach (var row in SummaryGeneralGrid.Items.Cast<object>())
                    {
                        var t = row.GetType();
                        var k = t.GetProperty("Bilgi")?.GetValue(row)?.ToString() ?? string.Empty;
                        var v = t.GetProperty("Deger")?.GetValue(row)?.ToString() ?? string.Empty;
                        summaryTable.AddCell(MakeCell(k));
                        summaryTable.AddCell(MakeCell(v));
                    }
                    doc.Add(new iTextSharp.text.Paragraph("Özet", headerFont) { SpacingBefore = 6f, SpacingAfter = 6f });
                    doc.Add(summaryTable);

                    var cementItems = CementSummaryGrid.Items.Cast<object>().ToList();
                    if (cementItems.Count > 0)
                    {
                        var cementTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
                        cementTable.SetWidths(new float[] { 60f, 40f });
                        cementTable.AddCell(MakeHeader("Cement Name"));
                        cementTable.AddCell(MakeHeader("Total (kg)"));
                        foreach (var row in cementItems)
                        {
                            var t = row.GetType();
                            var name = t.GetProperty("Ad")?.GetValue(row)?.ToString() ?? string.Empty;
                            var kg = t.GetProperty("ToplamKg")?.GetValue(row)?.ToString() ?? string.Empty;
                            cementTable.AddCell(MakeCell(name));
                            cementTable.AddCell(MakeCell(kg));
                        }
                        doc.Add(new iTextSharp.text.Paragraph("Cement", headerFont) { SpacingBefore = 10f, SpacingAfter = 6f });
                        doc.Add(cementTable);
                    }

                    var aggItems = AggregateSummaryGrid.Items.Cast<object>().ToList();
                    if (aggItems.Count > 0)
                    {
                        var aggTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
                        aggTable.SetWidths(new float[] { 60f, 40f });
                        aggTable.AddCell(MakeHeader("Aggregate Name"));
                        aggTable.AddCell(MakeHeader("Total (kg)"));
                        foreach (var row in aggItems)
                        {
                            var t = row.GetType();
                            var name = t.GetProperty("Ad")?.GetValue(row)?.ToString() ?? string.Empty;
                            var kg = t.GetProperty("ToplamKg")?.GetValue(row)?.ToString() ?? string.Empty;
                            aggTable.AddCell(MakeCell(name));
                            aggTable.AddCell(MakeCell(kg));
                        }
                        doc.Add(new iTextSharp.text.Paragraph("Aggregate", headerFont) { SpacingBefore = 10f, SpacingAfter = 6f });
                        doc.Add(aggTable);
                    }

                    var admItems = AdmixtureSummaryGrid.Items.Cast<object>().ToList();
                    if (admItems.Count > 0)
                    {
                        var admTable = new iTextSharp.text.pdf.PdfPTable(3) { WidthPercentage = 100 };
                        admTable.SetWidths(new float[] { 50f, 25f, 25f });
                        admTable.AddCell(MakeHeader("Admixture Name"));
                        admTable.AddCell(MakeHeader("Chemical (kg)"));
                        admTable.AddCell(MakeHeader("Water (kg)"));
                        foreach (var row in admItems)
                        {
                            var t = row.GetType();
                            var name = t.GetProperty("Ad")?.GetValue(row)?.ToString() ?? string.Empty;
                            var chem = t.GetProperty("KimyasalKg")?.GetValue(row)?.ToString() ?? string.Empty;
                            var water = t.GetProperty("SuKg")?.GetValue(row)?.ToString() ?? string.Empty;
                            admTable.AddCell(MakeCell(name));
                            admTable.AddCell(MakeCell(chem));
                            admTable.AddCell(MakeCell(water));
                        }
                        doc.Add(new iTextSharp.text.Paragraph("Admixture", headerFont) { SpacingBefore = 10f, SpacingAfter = 6f });
                        doc.Add(admTable);
                    }

                    var pigItems = PigmentSummaryGrid.Items.Cast<object>().ToList();
                    if (pigItems.Count > 0)
                    {
                        var pigTable = new iTextSharp.text.pdf.PdfPTable(2) { WidthPercentage = 100 };
                        pigTable.SetWidths(new float[] { 60f, 40f });
                        pigTable.AddCell(MakeHeader("Pigment Name"));
                        pigTable.AddCell(MakeHeader("Total (kg)"));
                        foreach (var row in pigItems)
                        {
                            var t = row.GetType();
                            var name = t.GetProperty("Ad")?.GetValue(row)?.ToString() ?? string.Empty;
                            var kg = t.GetProperty("ToplamKg")?.GetValue(row)?.ToString() ?? string.Empty;
                            pigTable.AddCell(MakeCell(name));
                            pigTable.AddCell(MakeCell(kg));
                        }
                        doc.Add(new iTextSharp.text.Paragraph("Pigment", headerFont) { SpacingBefore = 10f, SpacingAfter = 6f });
                        doc.Add(pigTable);
                    }

                    doc.Close();
                    writer.Close();
                }

                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch {}
                StatusText.Text = $"PDF kaydedildi: {path}";
                */
            }
            catch (Exception ex)
            {
                StatusText.Text = $"PDF error: {ex.Message}";
                MessageBox.Show($"PDF creation error:\n{ex.Message}\n\nDetails: {ex.StackTrace}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Türkçe karakterleri PDF uyumlu hale getir
        /// </summary>
        private string FixTurkishCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                // Özel Türkçe karakterler - öncelik sırasına göre
                .Replace("İ", "I")  // Büyük İ -> I
                .Replace("ı", "i")  // Küçük ı -> i
                .Replace("Ğ", "G")  // Büyük Ğ -> G
                .Replace("ğ", "g")  // Küçük ğ -> g
                .Replace("Ü", "U")  // Büyük Ü -> U
                .Replace("ü", "u")  // Küçük ü -> u
                .Replace("Ş", "S")  // Büyük Ş -> S
                .Replace("ş", "s")  // Küçük ş -> s
                .Replace("Ö", "O")  // Büyük Ö -> O
                .Replace("ö", "o")  // Küçük ö -> o
                .Replace("Ç", "C")  // Büyük Ç -> C
                .Replace("ç", "c")  // Küçük ç -> c
                // Diğer Türkçe karakterler
                .Replace("Â", "A")  // Büyük Â -> A
                .Replace("â", "a")  // Küçük â -> a
                .Replace("Ê", "E")  // Büyük Ê -> E
                .Replace("ê", "e")  // Küçük ê -> e
                .Replace("Î", "I")  // Büyük Î -> I
                .Replace("î", "i")  // Küçük î -> i
                .Replace("Ô", "O")  // Büyük Ô -> O
                .Replace("ô", "o")  // Küçük ô -> o
                .Replace("Û", "U")  // Büyük Û -> U
                .Replace("û", "u")  // Küçük û -> u
                .Trim(); // Boşlukları temizle
        }
    }
}


