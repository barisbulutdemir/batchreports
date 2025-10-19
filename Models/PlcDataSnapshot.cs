using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// PLC'den gelen ham veri snapshot'ı
    /// </summary>
    public class PlcDataSnapshot
    {
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        [MaxLength(100)]
        public string? Operator { get; set; }
        
        [MaxLength(50)]
        public string? RecipeCode { get; set; }
        
        // Grup aktiflik durumları
        public bool AggregateGroupActive { get; set; }
        public bool WaterGroupActive { get; set; }
        public bool CementGroupActive { get; set; }
        public bool AdmixtureGroupActive { get; set; }
        public bool PigmentGroupActive { get; set; }
        
        // Agrega verileri (8 slot)
        public bool Aggregate1Active { get; set; }
        public double Aggregate1Amount { get; set; }
        public bool Aggregate1TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate2Active { get; set; }
        public double Aggregate2Amount { get; set; }
        public bool Aggregate2TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate3Active { get; set; }
        public double Aggregate3Amount { get; set; }
        public bool Aggregate3TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate4Active { get; set; }
        public double Aggregate4Amount { get; set; }
        public bool Aggregate4TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate5Active { get; set; }
        public double Aggregate5Amount { get; set; }
        public bool Aggregate5TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate6Active { get; set; }
        public double Aggregate6Amount { get; set; }
        public bool Aggregate6TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate7Active { get; set; }
        public double Aggregate7Amount { get; set; }
        public bool Aggregate7TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Aggregate8Active { get; set; }
        public double Aggregate8Amount { get; set; }
        public bool Aggregate8TartimOk { get; set; } // Tartım OK sinyali
        
        // Su verileri (2 slot)
        public bool Water1Active { get; set; }
        public double Water1Amount { get; set; }
        
        public bool Water2Active { get; set; }
        public double Water2Amount { get; set; }
        
        // Çimento verileri (3 slot)
        public bool Cement1Active { get; set; }
        public double Cement1Amount { get; set; }
        public bool Cement1TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Cement2Active { get; set; }
        public double Cement2Amount { get; set; }
        public bool Cement2TartimOk { get; set; } // Tartım OK sinyali
        
        public bool Cement3Active { get; set; }
        public double Cement3Amount { get; set; }
        public bool Cement3TartimOk { get; set; } // Tartım OK sinyali
        
        // Katkı verileri (4 slot)
        public bool Admixture1Active { get; set; }
        public bool Admixture1TartimOk { get; set; }
        public bool Admixture1WaterTartimOk { get; set; }
        public double Admixture1ChemicalAmount { get; set; }
        public double Admixture1WaterAmount { get; set; }
        
        public bool Admixture2Active { get; set; }
        public bool Admixture2TartimOk { get; set; }
        public bool Admixture2WaterTartimOk { get; set; }
        public double Admixture2ChemicalAmount { get; set; }
        public double Admixture2WaterAmount { get; set; }
        
        public bool Admixture3Active { get; set; }
        public bool Admixture3TartimOk { get; set; }
        public bool Admixture3WaterTartimOk { get; set; }
        public double Admixture3ChemicalAmount { get; set; }
        public double Admixture3WaterAmount { get; set; }
        
        public bool Admixture4Active { get; set; }
        public bool Admixture4TartimOk { get; set; }
        public bool Admixture4WaterTartimOk { get; set; }
        public double Admixture4ChemicalAmount { get; set; }
        public double Admixture4WaterAmount { get; set; }
        
        // Pigment verileri (4 slot)
        public bool Pigment1Active { get; set; }
        public bool Pigment1TartimOk { get; set; }
        public double Pigment1Amount { get; set; }
        
        public bool Pigment2Active { get; set; }
        public bool Pigment2TartimOk { get; set; }
        public double Pigment2Amount { get; set; }
        
        public bool Pigment3Active { get; set; }
        public bool Pigment3TartimOk { get; set; }
        public double Pigment3Amount { get; set; }
        
        public bool Pigment4Active { get; set; }
        public bool Pigment4TartimOk { get; set; }
        public double Pigment4Amount { get; set; }
        
        // Nem verisi
        public double MoisturePercent { get; set; }
        
        // Harç Hazır sinyali
        public bool BatchReadySignal { get; set; }
        
        // Konveyör/kova durumu sinyalleri (agrega akışı takibi için)
        public bool HorizontalHasMaterial { get; set; } // Yatay kovada malzeme var
        public bool VerticalHasMaterial { get; set; }   // Dikey kovada malzeme var
        public bool WaitingBunkerHasMaterial { get; set; } // Bekleme bunkerinde malzeme var
        public bool MixerHasAggregate { get; set; }     // Mixerde agrega var (yeni)
        
        // Mixer içerik sinyalleri
        public bool MixerHasCement { get; set; }        // Mixerde çimento var
        public bool MixerHasAdmixture { get; set; }     // Mixerde katkı var
        public bool MixerHasWaterLoadcell { get; set; } // Mixerde su kantarı suyu var
        public bool MixerHasWaterPulse { get; set; }    // Mixerde puls su var
        
        // Ham veri (JSON)
        [MaxLength(8000)]
        public string? RawDataJson { get; set; }
    }
}
