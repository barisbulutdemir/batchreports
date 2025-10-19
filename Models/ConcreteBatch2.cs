using System;
using System.Collections.Generic;

namespace takip.Models
{
    /// <summary>
    /// Mixer2 için beton batch modeli
    /// </summary>
    public class ConcreteBatch2
    {
        public int Id { get; set; }

        public DateTime OccurredAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Yerel saat formatı için property (TimeZoneHelper kullanır)
        public string OccurredAtLocal => Utils.TimeZoneHelper.FormatDateTime(OccurredAt, "HH:mm");
        public string OccurredAtLocalFull => Utils.TimeZoneHelper.FormatDateTime(OccurredAt, "dd.MM.yyyy HH:mm");

        public string? PlantCode { get; set; }
        public string? OperatorName { get; set; }
        public string? RecipeCode { get; set; }

        public bool IsSimulated { get; set; }

        public double? MoisturePercent { get; set; }

        // Mixer2 - Su sistemi (1 loadcell + 1 pulse)
        public double LoadcellWaterKg { get; set; }
        public double PulseWaterKg { get; set; }
        
        // Toplam su hesaplama property'si (Loadcell + Pulse + Katkılardaki Su)
        public double TotalWaterKg => LoadcellWaterKg + PulseWaterKg + Admixtures.Sum(a => a.WaterKg);

        // Mixer2 - 4 farklı pigment
        public double Pigment1Kg { get; set; }
        public double Pigment2Kg { get; set; }
        public double Pigment3Kg { get; set; }
        public double Pigment4Kg { get; set; }

        // Toplam değerler
        public double TotalCementKg { get; set; }
        public double TotalAggregateKg { get; set; }
        public double TotalAdmixtureKg { get; set; }
        public double TotalPigmentKg { get; set; }

        // Çimento türü - Cements koleksiyonundan hesaplanan
        public string? CementType => Cements?.Where(c => c.WeightKg > 0)
            .OrderByDescending(c => c.WeightKg)
            .Select(c => c.CementType)
            .FirstOrDefault();

        public double EffectiveWaterKg { get; set; }
        public double? WaterCementRatio { get; set; }

        public string? RawPayloadJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Batch durumu - akış takibi için
        public string Status { get; set; } = "Yatay Kovada"; // Yatay Kovada, Dikey Kovada, Bekleme Bunkerinde, Mixerde, Tamamlandı
        
        // Status'u İngilizce çevir (UI için)
        public string StatusTranslated => Services.LocalizationService.Instance.TranslateBatchStatus(Status);

        // Navigation properties
        public ICollection<ConcreteBatch2Cement> Cements { get; set; } = new List<ConcreteBatch2Cement>();
        public ICollection<ConcreteBatch2Aggregate> Aggregates { get; set; } = new List<ConcreteBatch2Aggregate>();
        public ICollection<ConcreteBatch2Admixture> Admixtures { get; set; } = new List<ConcreteBatch2Admixture>();
    }
}
