using System;

namespace takip.Models
{
    /// <summary>
    /// M2 Mixer veri modeli
    /// </summary>
    public class M2MixerData
    {
        // Çimento verileri
        public bool CementGroupActive { get; set; }
        public string CementGroupRegister { get; set; } = "DM452";
        public bool Cement1Active { get; set; }
        public string Cement1ActiveRegister { get; set; } = "DM453";
        public double Cement1Kg { get; set; }
        public string Cement1KgRegister { get; set; } = "DM454";
        public bool Cement2Active { get; set; }
        public string Cement2ActiveRegister { get; set; } = "DM455";
        public double Cement2Kg { get; set; }
        public string Cement2KgRegister { get; set; } = "DM456";
        public bool Cement3Active { get; set; }
        public string Cement3ActiveRegister { get; set; } = "DM457";
        public double Cement3Kg { get; set; }
        public string Cement3KgRegister { get; set; } = "DM458";

        // Agrega verileri
        public bool AggregateGroupActive { get; set; }
        public string AggregateGroupRegister { get; set; } = "DM460";
        public bool Aggregate1Active { get; set; }
        public string Aggregate1ActiveRegister { get; set; } = "H51.2";
        public double Aggregate1Kg { get; set; }
        public string Aggregate1KgRegister { get; set; } = "DM4704";
        public bool Aggregate2Active { get; set; }
        public string Aggregate2ActiveRegister { get; set; } = "H52.2";
        public double Aggregate2Kg { get; set; }
        public string Aggregate2KgRegister { get; set; } = "DM4714";
        public bool Aggregate3Active { get; set; }
        public string Aggregate3ActiveRegister { get; set; } = "H53.2";
        public double Aggregate3Kg { get; set; }
        public string Aggregate3KgRegister { get; set; } = "DM4724";
        public bool Aggregate4Active { get; set; }
        public string Aggregate4ActiveRegister { get; set; } = "H54.2";
        public double Aggregate4Kg { get; set; }
        public string Aggregate4KgRegister { get; set; } = "DM4734";
        public bool Aggregate5Active { get; set; }
        public string Aggregate5ActiveRegister { get; set; } = "H55.2";
        public double Aggregate5Kg { get; set; }
        public string Aggregate5KgRegister { get; set; } = "DM4744";
        public bool Aggregate6Active { get; set; }
        public string Aggregate6ActiveRegister { get; set; } = "H56.2";
        public double Aggregate6Kg { get; set; }
        public string Aggregate6KgRegister { get; set; } = "DM4754";
        public bool Aggregate7Active { get; set; }
        public string Aggregate7ActiveRegister { get; set; } = "H57.2";
        public double Aggregate7Kg { get; set; }
        public string Aggregate7KgRegister { get; set; } = "DM4764";
        public bool Aggregate8Active { get; set; }
        public string Aggregate8ActiveRegister { get; set; } = "H58.2";
        public double Aggregate8Kg { get; set; }
        public string Aggregate8KgRegister { get; set; } = "DM4774";

        // Su verileri
        public bool WaterGroupActive { get; set; }
        public string WaterGroupRegister { get; set; } = "DM480";
        public bool WaterLoadcellActive { get; set; }
        public string WaterLoadcellRegister { get; set; } = "DM481";
        public double WaterLoadcellKg { get; set; }
        public bool WaterPulseActive { get; set; }
        public string WaterPulseRegister { get; set; } = "DM483";
        public double WaterPulseKg { get; set; }

        // Katkı verileri
        public bool AdmixtureGroupActive { get; set; }
        public string AdmixtureGroupRegister { get; set; } = "DM490";
        public bool Admixture1Active { get; set; }
        public string Admixture1Register { get; set; } = "DM491";
        public double Admixture1ChemicalKg { get; set; }
        public double Admixture1WaterKg { get; set; }
        public bool Admixture2Active { get; set; }
        public string Admixture2Register { get; set; } = "DM493";
        public double Admixture2ChemicalKg { get; set; }
        public double Admixture2WaterKg { get; set; }
        public bool Admixture3Active { get; set; }
        public string Admixture3Register { get; set; } = "DM495";
        public double Admixture3ChemicalKg { get; set; }
        public double Admixture3WaterKg { get; set; }
        public bool Admixture4Active { get; set; }
        public string Admixture4Register { get; set; } = "DM497";
        public double Admixture4ChemicalKg { get; set; }
        public double Admixture4WaterKg { get; set; }

        // Pigment verileri
        public bool PigmentGroupActive { get; set; }
        public string PigmentGroupRegister { get; set; } = "DM500";
        public bool Pigment1Active { get; set; }
        public string Pigment1Register { get; set; } = "DM501";
        public double Pigment1Kg { get; set; }
        public bool Pigment2Active { get; set; }
        public string Pigment2Register { get; set; } = "DM503";
        public double Pigment2Kg { get; set; }
        public bool Pigment3Active { get; set; }
        public string Pigment3Register { get; set; } = "DM505";
        public double Pigment3Kg { get; set; }
        public bool Pigment4Active { get; set; }
        public string Pigment4Register { get; set; } = "DM507";
        public double Pigment4Kg { get; set; }

        // Genel durum
        public bool MixerActive { get; set; }
        public double MixerSpeed { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Nem ve malzeme sinyalleri
        public double Humidity { get; set; }
        public string HumidityRegister { get; set; } = "DM510";
        public bool HorizontalBucketMaterial { get; set; }
        public string HorizontalBucketRegister { get; set; } = "DM511";
        public bool VerticalBucketMaterial { get; set; }
        public string VerticalBucketRegister { get; set; } = "DM512";
        public bool WaitingBunkerMaterial { get; set; }
        public string WaitingBunkerRegister { get; set; } = "DM513";

        // Çevre verileri
        public double Temperature { get; set; }
        
        // Harç Hazır sinyali
        public bool HarçHazır { get; set; }
        public string HarçHazırRegister { get; set; } = "H71.5";
    }

    /// <summary>
    /// Toplu veri okuma sonucu
    /// </summary>
    public class BatchDataResult
    {
        public byte[] DmData { get; set; } = new byte[0];
        public byte[] HData { get; set; } = new byte[0];
        public byte[] CioData { get; set; } = new byte[0];
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
