using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Utils;

namespace takip
{
    public partial class ConcreteBatchDetailWindow : Window
    {
        private readonly int _batchId;

        public ConcreteBatchDetailWindow(int batchId)
        {
            _batchId = batchId;
            InitializeComponent();
            Loaded += ConcreteBatchDetailWindow_Loaded;
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
        }

        private void ConcreteBatchDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Thread-safe: Her işlem için yeni DbContext
            using var context = new ProductionDbContext();
            
            var batch = context.ConcreteBatches
                .Include(b => b.Cements)
                .Include(b => b.Aggregates)
                .Include(b => b.Admixtures)
                .Where(b => b.Id == _batchId)
                .Select(b => new
                {
                    B = b,
                    Cements = b.Cements.ToList(),
                    Aggs = b.Aggregates.OrderBy(a => a.Slot).ToList(),
                    Adms = b.Admixtures.OrderBy(a => a.Slot).ToList()
                })
                .FirstOrDefault();
            if (batch == null)
            {
                Close();
                return;
            }

            HeaderText.Text = $"Batch #{_batchId} - {TimeZoneHelper.FormatDateTime(batch.B.OccurredAt, "dd.MM.yyyy - HH:mm")}";
            // Çimento isimleri için alias ile birleştir
            var cementAliasBySlot = context.CementAliases
                .Where(c => c.IsActive)
                .ToDictionary(x => x.Slot, x => x.Name);
            var cementItems = batch.Cements
                .OrderBy(c => c.Slot)
                .Select(c => new
                {
                    Slot = c.Slot,
                    Name = (cementAliasBySlot.TryGetValue(c.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName))
                        ? aliasName
                        : (!string.IsNullOrWhiteSpace(c.CementType) ? c.CementType : $"Cement {c.Slot}"),
                    WeightKg = c.WeightKg
                })
                .ToList();
            CementGrid.ItemsSource = cementItems;

            // Agrega isimleri: boşsa alias tablosundan doldur, o da yoksa fallback
            var aliasBySlot = context.AggregateAliases
                .Where(a => a.IsActive)
                .ToDictionary(x => x.Slot, x => x.Name);
            string ResolveAgg1Name(short slot, string? rawName)
            {
                if (aliasBySlot.TryGetValue(slot, out var exact) && !string.IsNullOrWhiteSpace(exact)) return exact;
                return !string.IsNullOrWhiteSpace(rawName) ? rawName : $"Aggregate {slot}";
            }

            var aggItems = batch.Aggs
                .OrderBy(a => a.Slot)
                .Select(a => new
                {
                    Slot = a.Slot,
                    Name = ResolveAgg1Name(a.Slot, a.Name),
                    WeightKg = a.WeightKg
                })
                .ToList();
            AggGrid.ItemsSource = aggItems;
            AdmGrid.ItemsSource = batch.Adms.OrderBy(x => x.Slot).ToList();

            var totalCement = batch.B.TotalCementKg;
            var totalAgg = batch.B.TotalAggregateKg;
            var percent = (totalCement + totalAgg) > 0 ? (totalCement / (totalCement + totalAgg)) * 100.0 : 0.0;

            // Su bilgilerini DataGrid için hazırla - TotalWaterKg kullan
            var waterInfo = new List<WaterInfoItem>
            {
                new WaterInfoItem { Info = "Loadcell Water", Value = $"{batch.B.LoadcellWaterKg:0.0} kg" },
                new WaterInfoItem { Info = "Pulse Water", Value = $"{batch.B.PulseWaterKg:0.0} kg" },
                new WaterInfoItem { Info = "Total Water", Value = $"{batch.B.TotalWaterKg:0.0} kg" },
                new WaterInfoItem { Info = "Moisture", Value = $"{batch.B.MoisturePercent:0}%" }
            };

            WaterInfoGrid.ItemsSource = waterInfo;
            CementPercentText.Text = $"Cement %: {percent:0.0}%";

            // Pigment ayrı grup olarak
            var pigmentList = new List<Batch1PigmentItem>();
            if (batch.B.PigmentKg > 0)
            {
                pigmentList.Add(new Batch1PigmentItem { Name = "Pigment", WeightKg = batch.B.PigmentKg });
            }
            PigmentGrid.ItemsSource = pigmentList;
        }
    }

    public class WaterInfoItem
    {
        public string Info { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class Batch1PigmentItem
    {
        public string Name { get; set; } = string.Empty;
        public double WeightKg { get; set; }
    }
}


