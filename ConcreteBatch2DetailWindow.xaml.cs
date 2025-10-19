using System;
using System.Linq;
using System.Text;
using System.Windows;
using takip.Models;
using takip.Utils;
using takip.Data;

namespace takip
{
    public partial class ConcreteBatch2DetailWindow : Window
    {
        private readonly ConcreteBatch2 _batch;

        public ConcreteBatch2DetailWindow(ConcreteBatch2 batch)
        {
            InitializeComponent();
            _batch = batch ?? throw new ArgumentNullException(nameof(batch));
            LoadBatchData();
        }

        private void LoadBatchData()
        {
            try
            {
                // Genel Bilgiler
                BatchIdLabel.Content = _batch.Id.ToString();
                DateLabel.Content = TimeZoneHelper.FormatDateTime(_batch.OccurredAt, "dd/MM/yyyy HH:mm:ss");
                RecipeLabel.Content = _batch.RecipeCode ?? "None";
                MoistureLabel.Content = _batch.MoisturePercent?.ToString("N1") + "%" ?? "None";

                // Su Bilgileri - TotalWaterKg kullan
                Water1Label.Content = $"{_batch.LoadcellWaterKg:N1} kg";
                PulseWater1Label.Content = $"{_batch.PulseWaterKg:N1} kg";

                // Pigment Bilgileri
                Pigment1Label.Content = $"{_batch.Pigment1Kg:N1} kg";
                Pigment2Label.Content = $"{_batch.Pigment2Kg:N1} kg";
                Pigment3Label.Content = $"{_batch.Pigment3Kg:N1} kg";
                Pigment4Label.Content = $"{_batch.Pigment4Kg:N1} kg";

                // Çimento isimlerini alias/CementType ile göster
                using var context = new ProductionDbContext();
                var cementAliasBySlot = context.Cement2Aliases
                    .Where(c => c.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                var cementRows = _batch.Cements
                    .OrderBy(c => c.Slot)
                    .Select(c => new
                    {
                        Slot = c.Slot,
                        CementType = !string.IsNullOrWhiteSpace(c.CementType)
                            ? c.CementType
                            : (cementAliasBySlot.TryGetValue(c.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                                ? aliasName
                                : $"Cement {c.Slot}"),
                        WeightKg = c.WeightKg
                    })
                    .ToList();
                CementGrid.ItemsSource = cementRows;
                
                // Agrega listesini düzenle - alias varsa onu kullan, yoksa isim ya da fallback
                var aggregateAliasBySlot = context.Aggregate2Aliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                // Alias eşlemesi: Yalnızca slot birebir eşleşme, 0-index fallback yok
                string ResolveAggregateName(short slot, string? rawName)
                {
                    if (aggregateAliasBySlot.TryGetValue(slot, out var exact) && !string.IsNullOrWhiteSpace(exact))
                        return exact;
                    return !string.IsNullOrWhiteSpace(rawName) ? rawName : $"Aggregate {slot}";
                }

                var aggregates = _batch.Aggregates?
                    .OrderBy(a => a.Slot)
                    .Select(a => new
                {
                    Slot = a.Slot,
                    Name = ResolveAggregateName(a.Slot, a.Name),
                    WeightKg = a.WeightKg
                }).ToList();
                AggregateGrid.ItemsSource = aggregates;
                
                // Katkı listesini düzenle - alias varsa onu kullan, yoksa isim ya da fallback
                var admixtureAliasBySlot = context.Admixture2Aliases
                    .Where(a => a.IsActive)
                    .ToDictionary(x => x.Slot, x => x.Name);
                var admixtures = _batch.Admixtures?
                    .OrderBy(a => a.Slot)
                    .Select(a => new
                {
                    Slot = a.Slot,
                    Name = (admixtureAliasBySlot.TryGetValue(a.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName))
                        ? aliasName
                        : (!string.IsNullOrWhiteSpace(a.Name) ? a.Name : $"Admixture {a.Slot}"),
                    ChemicalKg = a.ChemicalKg,
                    WaterKg = a.WaterKg
                }).ToList();
                AdmixtureGrid.ItemsSource = admixtures;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading batch data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2DetailWindow] Veri yükleme hatası: {ex}");
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"=== MIXER2 BATCH DETAILS ===");
                sb.AppendLine($"Batch ID: {_batch.Id}");
                sb.AppendLine($"Date: {_batch.OccurredAt:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine($"Recipe: {_batch.RecipeCode ?? "None"}");
                sb.AppendLine($"Moisture: {_batch.MoisturePercent?.ToString("N1") ?? "None"}%");
                sb.AppendLine();

                sb.AppendLine("=== WATER INFORMATION ===");
                sb.AppendLine($"Loadcell Water: {_batch.LoadcellWaterKg:N1} kg");
                sb.AppendLine($"Pulse Water: {_batch.PulseWaterKg:N1} kg");
                sb.AppendLine($"Total Water: {_batch.TotalWaterKg:N1} kg");
                sb.AppendLine();

                sb.AppendLine("=== PIGMENT INFORMATION ===");
                sb.AppendLine($"Pigment 1: {_batch.Pigment1Kg:N1} kg");
                sb.AppendLine($"Pigment 2: {_batch.Pigment2Kg:N1} kg");
                sb.AppendLine($"Pigment 3: {_batch.Pigment3Kg:N1} kg");
                sb.AppendLine($"Pigment 4: {_batch.Pigment4Kg:N1} kg");
                sb.AppendLine($"Total Pigment: {_batch.TotalPigmentKg:N1} kg");
                sb.AppendLine();

                if (_batch.Cements?.Any() == true)
                {
                    sb.AppendLine("=== CEMENT DETAILS ===");
                    foreach (var cement in _batch.Cements)
                    {
                        sb.AppendLine($"Slot {cement.Slot}: {cement.CementType} - {cement.WeightKg:N1} kg");
                    }
                    sb.AppendLine($"Total Cement: {_batch.TotalCementKg:N1} kg");
                    sb.AppendLine();
                }

                if (_batch.Aggregates?.Any() == true)
                {
                    sb.AppendLine("=== AGGREGATE DETAILS ===");
                    // Use alias names for printing
                    using (var context = new ProductionDbContext())
                    {
                        var aggregateAliasBySlot = context.Aggregate2Aliases
                            .Where(a => a.IsActive)
                            .ToDictionary(x => x.Slot, x => x.Name);
                        string ResolveAggCopyName(short slot, string? rawName)
                        {
                            if (aggregateAliasBySlot.TryGetValue(slot, out var exact) && !string.IsNullOrWhiteSpace(exact)) return exact;
                            return !string.IsNullOrWhiteSpace(rawName) ? rawName : $"aggregate{slot}";
                        }
                        foreach (var agg in _batch.Aggregates.OrderBy(a => a.Slot))
                        {
                            var aggName = ResolveAggCopyName(agg.Slot, agg.Name);
                            sb.AppendLine($"Slot {agg.Slot}: {aggName} - {agg.WeightKg:N1} kg");
                        }
                    }
                    sb.AppendLine($"Total Aggregate: {_batch.TotalAggregateKg:N1} kg");
                    sb.AppendLine();
                }

                if (_batch.Admixtures?.Any() == true)
                {
                    sb.AppendLine("=== ADMIXTURE DETAILS ===");
                    foreach (var adm in _batch.Admixtures)
                    {
                        sb.AppendLine($"Slot {adm.Slot}: {adm.Name ?? $"admixture{adm.Slot}"} - Chemical: {adm.ChemicalKg:N3} kg, Water: {adm.WaterKg:N1} kg");
                    }
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Batch details copied to clipboard.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
