using System;
using System.Collections.Generic;

namespace takip.Models
{
    public class ConcreteBatch
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

        public double LoadcellWaterKg { get; set; }
        public double PulseWaterKg { get; set; }
        public double PigmentKg { get; set; }

        public double TotalCementKg { get; set; }
        public double TotalAggregateKg { get; set; }
        public double TotalAdmixtureKg { get; set; }
        public double TotalPigmentKg { get; set; }
        
        // Çimento türü - Cements koleksiyonundan hesaplanan
        public string? CementType => Cements?.Where(c => c.WeightKg > 0)
            .OrderByDescending(c => c.WeightKg)
            .Select(c => c.CementType)
            .FirstOrDefault();
        
        // Toplam su hesaplama property'si (Loadcell + Pulse + Katkılardaki Su)
        public double TotalWaterKg => LoadcellWaterKg + PulseWaterKg + Admixtures.Sum(a => a.WaterKg);

        public double EffectiveWaterKg { get; set; }
        public double? WaterCementRatio { get; set; }

        public string? RawPayloadJson { get; set; }

        public string? Status { get; set; }
        
        // Status'u İngilizce çevir (UI için)
        public string? StatusTranslated => Services.LocalizationService.Instance.TranslateBatchStatus(Status ?? "");

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // GEÇİCİ OLARAK KAPALI

        public ICollection<ConcreteBatchCement> Cements { get; set; } = new List<ConcreteBatchCement>();
        public ICollection<ConcreteBatchAggregate> Aggregates { get; set; } = new List<ConcreteBatchAggregate>();
        public ICollection<ConcreteBatchAdmixture> Admixtures { get; set; } = new List<ConcreteBatchAdmixture>();
        public ICollection<ConcreteBatchPigment> Pigments { get; set; } = new List<ConcreteBatchPigment>();
    }
}


