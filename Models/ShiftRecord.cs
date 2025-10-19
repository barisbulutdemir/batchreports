using System;
using System.Collections.Generic;

namespace takip.Models
{
    /// <summary>
    /// Vardiya kayıt modeli - Her vardiya bitiminde kaydedilir
    /// </summary>
    public class ShiftRecord
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Vardiya başlama tarihi
        /// </summary>
        public DateTime ShiftStartTime { get; set; }
        
        /// <summary>
        /// Vardiya bitiş tarihi
        /// </summary>
        public DateTime ShiftEndTime { get; set; }
        
        /// <summary>
        /// Operatör adı
        /// </summary>
        public string OperatorName { get; set; } = "";
        
        /// <summary>
        /// Toplam üretim adedi (vardiya içi)
        /// </summary>
        public int TotalProduction { get; set; }
        
        /// <summary>
        /// Üretim başlama tarihi (ilk taş geldiğinde)
        /// </summary>
        public DateTime? ProductionStartTime { get; set; }
        
        /// <summary>
        /// Vardiya süresi (dakika)
        /// </summary>
        public int ShiftDurationMinutes { get; set; }
        
        /// <summary>
        /// Üretim süresi (dakika) - üretim başladıktan vardiya bitimine kadar
        /// </summary>
        public int ProductionDurationMinutes { get; set; }
        
        /// <summary>
        /// O gün basılan taş isimleri ve adetleri (JSON formatında)
        /// </summary>
        public string StoneProductionJson { get; set; } = "";
        
        /// <summary>
        /// Fire mal sayısı (PLC'den otomatik çekilir)
        /// </summary>
        public int FireProductCount { get; set; } = 0;
        
        /// <summary>
        /// Boşta geçen süre (saniye cinsinden)
        /// </summary>
        public int IdleTimeSeconds { get; set; } = 0;
        
        /// <summary>
        /// Mixer1 batch sayısı (vardiya içi)
        /// </summary>
        public int Mixer1BatchCount { get; set; } = 0;
        
        /// <summary>
        /// Mixer1 toplam çimento miktarı (kg)
        /// </summary>
        public double Mixer1CementTotal { get; set; } = 0;
        
        /// <summary>
        /// Mixer1 çimento türleri (JSON formatında)
        /// </summary>
        public string Mixer1CementTypesJson { get; set; } = "";
        
        /// <summary>
        /// Mixer2 batch sayısı (vardiya içi)
        /// </summary>
        public int Mixer2BatchCount { get; set; } = 0;
        
        /// <summary>
        /// Mixer2 toplam çimento miktarı (kg)
        /// </summary>
        public double Mixer2CementTotal { get; set; } = 0;
        
        /// <summary>
        /// Mixer2 çimento türleri (JSON formatında)
        /// </summary>
        public string Mixer2CementTypesJson { get; set; } = "";
        
        /// <summary>
        /// Kalıp bazında üretim bilgileri (JSON formatında)
        /// </summary>
        public string? MoldProductionJson { get; set; } = "";
        
        /// <summary>
        /// Mixer1 malzeme detayları (JSON formatında)
        /// </summary>
        public string Mixer1MaterialsJson { get; set; } = "";
        
        /// <summary>
        /// Mixer2 malzeme detayları (JSON formatında)
        /// </summary>
        public string Mixer2MaterialsJson { get; set; } = "";
        
        /// <summary>
        /// Toplam malzeme kullanımı (JSON formatında)
        /// </summary>
        public string TotalMaterialsJson { get; set; } = "";
        
        /// <summary>
        /// Kayıt oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
