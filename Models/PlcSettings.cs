using System.Collections.Generic;

namespace takip.Models
{
    public class PlcSettings
    {
        public ProductionPlcConfig ProductionPlc { get; set; } = new ProductionPlcConfig();
        public ConcretePlcConfig ConcretePlc { get; set; } = new ConcretePlcConfig();
        public M2Registers M2Registers { get; set; } = new M2Registers();
        
        // Backward compatibility - production PLC için
        public string PlcIp 
        { 
            get => ProductionPlc.IpAddress; 
            set => ProductionPlc.IpAddress = value; 
        }
        public int PlcPort 
        { 
            get => ProductionPlc.Port; 
            set => ProductionPlc.Port = value; 
        }
        public int PollIntervalSeconds 
        { 
            get => ConcretePlc.PollIntervalSeconds; 
            set => ConcretePlc.PollIntervalSeconds = value; 
        }
        public int MinBatchIntervalSeconds 
        { 
            get => ConcretePlc.MinBatchIntervalSeconds; 
            set => ConcretePlc.MinBatchIntervalSeconds = value; 
        }

        // Anahtar = mantıksal isim, Değer = register adresi
        public Dictionary<string, object> Registers { get; set; } = new Dictionary<string, object>
        {
            // MIXER1 REGISTERLARI
            // Mixer1 Grup Aktiflik Sinyalleri
            { "M1_CimentoGrupAktif", "H62.0" },
            { "M1_AgregaGrupAktif", "H45.0" },
            { "M1_SuGrupAktif", "H60.3" },
            { "M1_KatkiGrupAktif", "H35.10" },
            { "M1_PigmentGrupAktif", "H30.2" },
            
            // Mixer1 Çimento (3 adet)
            { "M1_Cimento1Aktif", "H62.2" },
            { "M1_Cimento1Kg", "DM4404" },
            { "M1_Cimento2Aktif", "H63.2" },
            { "M1_Cimento2Kg", "DM4414" },
            { "M1_Cimento3Aktif", "H64.2" },
            { "M1_Cimento3Kg", "DM4424" },
            
            // Mixer1 Agregalar (5 adet)
            { "M1_Agrega1Aktif", "H45.2" },
            { "M1_Agrega1Kg", "DM4204" },
            { "M1_Agrega1TartimOk", "H45.7" },
            { "M1_Agrega2Aktif", "H46.2" },
            { "M1_Agrega2Kg", "DM4214" },
            { "M1_Agrega2TartimOk", "H46.7" },
            { "M1_Agrega3Aktif", "H47.2" },
            { "M1_Agrega3Kg", "DM4224" },
            { "M1_Agrega3TartimOk", "H47.7" },
            { "M1_Agrega4Aktif", "H48.2" },
            { "M1_Agrega4Kg", "DM4234" },
            { "M1_Agrega4TartimOk", "H48.7" },
            { "M1_Agrega5Aktif", "H49.2" },
            { "M1_Agrega5Kg", "DM4244" },
            { "M1_Agrega5TartimOk", "H49.7" },
            
            // Mixer1 Su (2 adet)
            { "M1_SuLoadcellAktif", "H60.0" },
            { "M1_SuLoadcellKg", "DM204" },
            { "M1_SuPulseAktif", "H04.0" },
            { "M1_SuPulseKg", "DM210" },
            
            // Mixer1 Katkılar (4 adet - kimyasal + su)
            { "M1_Katki1Aktif", "H35.0" },
            { "M1_Katki1KimyasalKg", "DM4104" },
            { "M1_Katki1SuKg", "DM4105" },
            { "M1_Katki2Aktif", "H36.0" },
            { "M1_Katki2KimyasalKg", "DM4114" },
            { "M1_Katki2SuKg", "DM4115" },
            { "M1_Katki3Aktif", "H37.0" },
            { "M1_Katki3KimyasalKg", "DM4124" },
            { "M1_Katki3SuKg", "DM4125" },
            { "M1_Katki4Aktif", "H38.0" },
            { "M1_Katki4KimyasalKg", "DM4134" },
            { "M1_Katki4SuKg", "DM4135" },
            
            // Mixer1 Pigment (1 adet)
            { "M1_Pigment1Aktif", "H30.10" },
            { "M1_Pigment1Kg", "DM208" },
            
            // Mixer1 Bekleme Bunkeri ve Mixer İçi Sinyalleri
            { "M1_MixerAgregaVar", "H70.0" },
            { "M1_MixerCimentoVar", "H70.1" },
            { "M1_MixerKatkiVar", "H70.2" },
            { "M1_MixerLoadcellSuVar", "H70.3" },
            { "M1_MixerPulseSuVar", "H70.4" },
            { "M1_HarcHazir", "H70.5" },
            { "M1_WaitingBunker", "H70.7" },
            
            // MIXER2 REGISTERLARI
            // Mixer2 Grup Aktiflik Sinyalleri
            { "M2_CimentoGrupAktif", "H65.0" },
            { "M2_AgregaGrupAktif", "H51.0" },
            { "M2_SuGrupAktif", "H61.2" },
            { "M2_KatkiGrupAktif", "H39.10" },
            { "M2_PigmentGrupAktif", "H31.2" },
            
            // Mixer2 Çimento (3 adet)
            { "M2_Cimento1Aktif", "H65.2" },
            { "M2_Cimento1TartimOk", "H65.7" },
            { "M2_Cimento1Kg", "DM4434" },
            { "M2_Cimento2Aktif", "H66.2" },
            { "M2_Cimento2TartimOk", "H66.7" },
            { "M2_Cimento2Kg", "DM4444" },
            { "M2_Cimento3Aktif", "H67.2" },
            { "M2_Cimento3TartimOk", "H67.7" },
            { "M2_Cimento3Kg", "DM4454" },
            
            // Mixer2 Agregalar (8 adet)
            { "M2_Agrega1Aktif", "H51.2" },
            { "M2_Agrega1Kg", "DM4704" },
            { "M2_Agrega2Aktif", "H52.2" },
            { "M2_Agrega2Kg", "DM4714" },
            { "M2_Agrega3Aktif", "H53.2" },
            { "M2_Agrega3Kg", "DM4724" },
            { "M2_Agrega4Aktif", "H54.2" },
            { "M2_Agrega4Kg", "DM4734" },
            { "M2_Agrega5Aktif", "H55.2" },
            { "M2_Agrega5Kg", "DM4744" },
            { "M2_Agrega6Aktif", "H56.2" },
            { "M2_Agrega6Kg", "DM4754" },
            { "M2_Agrega7Aktif", "H57.2" },
            { "M2_Agrega7Kg", "DM4764" },
            { "M2_Agrega8Aktif", "H58.2" },
            { "M2_Agrega8Kg", "DM4774" },
            
            // Mixer2 Su (2 adet)
            { "M2_SuLoadcellAktif", "H61.0" },
            { "M2_SuLoadcellKg", "DM304" },
            { "M2_SuPulseAktif", "H14.0" },
            { "M2_SuPulseKg", "DM306" },
            
            // Mixer2 Katkılar (4 adet - kimyasal + su + tartım ok)
            { "M2_Katki1Aktif", "H39.0" },
            { "M2_Katki1TartimOk", "H39.3" },
            { "M2_Katki1SuTartimOk", "H39.4" },
            { "M2_Katki1KimyasalKg", "DM4604" },
            { "M2_Katki1SuKg", "DM4605" },
            { "M2_Katki2Aktif", "H40.0" },
            { "M2_Katki2TartimOk", "H40.3" },
            { "M2_Katki2SuTartimOk", "H40.4" },
            { "M2_Katki2KimyasalKg", "DM4614" },
            { "M2_Katki2SuKg", "DM4615" },
            { "M2_Katki3Aktif", "H41.0" },
            { "M2_Katki3TartimOk", "H41.3" },
            { "M2_Katki3SuTartimOk", "H41.4" },
            { "M2_Katki3KimyasalKg", "DM4624" },
            { "M2_Katki3SuKg", "DM4625" },
            { "M2_Katki4Aktif", "H42.0" },
            { "M2_Katki4TartimOk", "H42.3" },
            { "M2_Katki4SuTartimOk", "H42.4" },
            { "M2_Katki4KimyasalKg", "DM4634" },
            { "M2_Katki4SuKg", "DM4635" },
            
            // Mixer2 Pigmentler (4 adet + tartım ok)
            { "M2_Pigment1Aktif", "H31.10" },
            { "M2_Pigment1TartimOk", "H31.3" },
            { "M2_Pigment1Kg", "DM308" },
            { "M2_Pigment2Aktif", "H32.10" },
            { "M2_Pigment2TartimOk", "H32.3" },
            { "M2_Pigment2Kg", "DM310" },
            { "M2_Pigment3Aktif", "H33.10" },
            { "M2_Pigment3TartimOk", "H33.3" },
            { "M2_Pigment3Kg", "DM312" },
            { "M2_Pigment4Aktif", "H34.10" },
            { "M2_Pigment4TartimOk", "H34.3" },
            { "M2_Pigment4Kg", "DM314" },
            
            // Diğer Registerlar
            { "M2_Nem", "DM122" },
            { "HarçHazır", "H71.5" },
            
            // Kovada malzeme sinyalleri
            { "YatayKovadaMalzemeVar", "H71.7" },
            { "DikeyKovadaMalzemeVar", "H71.10" },
            { "BeklemeBunkerindeMalzemeVar", "H71.11" },
            
            // Mixerde malzeme sinyalleri
            { "MixerdeAggregaVar", "H70.0" },
            { "MixerdeCimentoVar", "H71.1" },
            { "MixerdeKatkiVar", "H71.2" },
            { "MixerdeLoadcellSuVar", "H71.3" },
            { "MixerdePulseSuVar", "H71.4" }
        };
    }
    
    /// <summary>
    /// Üretim (taş) PLC ayarları - 192.168.250.1
    /// </summary>
    public class ProductionPlcConfig
    {
        public string IpAddress { get; set; } = "192.168.250.1";
        public int Port { get; set; } = 9600;
        public int MachineCountRegister { get; set; } = 452;
        public int StoneCountRegister { get; set; } = 453;
        public string StoneName { get; set; } = "6cm XShape";
        public int FireProductRegister { get; set; } = 452;
    }
    
    /// <summary>
    /// Beton santrali PLC ayarları - 192.168.250.10
    /// </summary>
    public class ConcretePlcConfig
    {
        public string IpAddress { get; set; } = "192.168.250.10";
        public int Port { get; set; } = 9600;
        public int PollIntervalSeconds { get; set; } = 2;
        // Mixer batch açılışları arası minimum süre (cooldown)
        public int MinBatchIntervalSeconds { get; set; } = 60;
        // Bekleme bunkerine geçiş için: eklenmiş agregaların TartımOK sinyali kesintisiz kapalı kalma süresi
        public int WaitingBunkerTartimOkOffSeconds { get; set; } = 5;
    }
    
    /// <summary>
    /// M2 Mixer register adresleri
    /// </summary>
    public class M2Registers
    {
        // Çimento register'ları
        public string CementGroupRegister { get; set; } = "H65.0";
        public string Cement1Register { get; set; } = "H65.2";
        public string Cement1KgRegister { get; set; } = "DM4434";
        public string Cement2Register { get; set; } = "H66.2";
        public string Cement2KgRegister { get; set; } = "DM4444";
        public string Cement3Register { get; set; } = "H67.2";
        public string Cement3KgRegister { get; set; } = "DM4454";
        
        // Agrega register'ları
        public string AggregateGroupRegister { get; set; } = "H51.0";
        public string Aggregate1Register { get; set; } = "H51.2";
        public string Aggregate1KgRegister { get; set; } = "DM4704";
        public string Aggregate2Register { get; set; } = "H52.2";
        public string Aggregate2KgRegister { get; set; } = "DM4714";
        public string Aggregate3Register { get; set; } = "H53.2";
        public string Aggregate3KgRegister { get; set; } = "DM4724";
        public string Aggregate4Register { get; set; } = "H54.2";
        public string Aggregate4KgRegister { get; set; } = "DM4734";
        public string Aggregate5Register { get; set; } = "H55.2";
        public string Aggregate5KgRegister { get; set; } = "DM4744";
        public string Aggregate6Register { get; set; } = "H56.2";
        public string Aggregate6KgRegister { get; set; } = "DM4754";
        public string Aggregate7Register { get; set; } = "H57.2";
        public string Aggregate7KgRegister { get; set; } = "DM4764";
        public string Aggregate8Register { get; set; } = "H58.2";
        public string Aggregate8KgRegister { get; set; } = "DM4774";
        
        // Su register'ları
        public string WaterGroupRegister { get; set; } = "H61.2";
        public string WaterLoadcellRegister { get; set; } = "H61.0";
        public string WaterLoadcellKgRegister { get; set; } = "DM304";
        public string WaterPulseRegister { get; set; } = "H14.0";
        public string WaterPulseKgRegister { get; set; } = "DM306";
        
        // Katkı register'ları
        public string AdmixtureGroupRegister { get; set; } = "H39.10";
        public string Admixture1Register { get; set; } = "H39.0";
        public string Admixture1ChemicalKgRegister { get; set; } = "DM4604";
        public string Admixture1WaterKgRegister { get; set; } = "DM4605";
        public string Admixture2Register { get; set; } = "H40.0";
        public string Admixture2ChemicalKgRegister { get; set; } = "DM4614";
        public string Admixture2WaterKgRegister { get; set; } = "DM4615";
        public string Admixture3Register { get; set; } = "H41.0";
        public string Admixture3ChemicalKgRegister { get; set; } = "DM4624";
        public string Admixture3WaterKgRegister { get; set; } = "DM4625";
        public string Admixture4Register { get; set; } = "H42.0";
        public string Admixture4ChemicalKgRegister { get; set; } = "DM4634";
        public string Admixture4WaterKgRegister { get; set; } = "DM4635";
        
        // Pigment register'ları
        public string PigmentGroupRegister { get; set; } = "H31.2";
        public string Pigment1Register { get; set; } = "H31.10";
        public string Pigment1KgRegister { get; set; } = "DM308";
        public string Pigment2Register { get; set; } = "H32.10";
        public string Pigment2KgRegister { get; set; } = "DM310";
        public string Pigment3Register { get; set; } = "H33.10";
        public string Pigment3KgRegister { get; set; } = "DM312";
        public string Pigment4Register { get; set; } = "H34.10";
        public string Pigment4KgRegister { get; set; } = "DM314";
        
        // Diğer register'lar
        public string HumidityRegister { get; set; } = "DM122";
        public string HorizontalBucketRegister { get; set; } = "H71.7";
        public string VerticalBucketRegister { get; set; } = "H71.10";
        public string WaitingBunkerRegister { get; set; } = "H71.11";
        public string MixerAggregateRegister { get; set; } = "H70.0";
        public string MixerCementRegister { get; set; } = "H71.1";
        public string MixerAdmixtureRegister { get; set; } = "H71.2";
        public string MixerWaterLoadcellRegister { get; set; } = "H71.3";
        public string MixerWaterPulseRegister { get; set; } = "H71.4";
        public string HarçHazırRegister { get; set; } = "H71.5";
    }
}


