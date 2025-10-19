using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Çimento silo bilgilerini tutan model
    /// </summary>
    public class CementSilo
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Silo numarası (1, 2, 3)
        /// </summary>
        [Required]
        public int SiloNumber { get; set; }

        /// <summary>
        /// Çimento türü (Standard, Beyaz, Siyah)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CementType { get; set; } = string.Empty;

        /// <summary>
        /// Mevcut çimento miktarı (kg)
        /// </summary>
        public double CurrentAmount { get; set; }

        /// <summary>
        /// Mevcut seviye yüzdesi
        /// </summary>
        public double CurrentLevel => FillPercentage;

        /// <summary>
        /// Son doldurma tarihi
        /// </summary>
        public DateTime? LastRefillDate { get; set; }

        /// <summary>
        /// Silo kapasitesi (kg)
        /// </summary>
        public double Capacity { get; set; }

        /// <summary>
        /// Minimum seviye uyarısı (kg)
        /// </summary>
        public double MinLevel { get; set; }

        /// <summary>
        /// Silo aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Silo doluluk yüzdesi
        /// </summary>
        public double FillPercentage => Capacity > 0 ? (CurrentAmount / Capacity) * 100 : 0;

        /// <summary>
        /// Silo durumu (Dolu, Normal, Düşük, Kritik)
        /// </summary>
        public string Status
        {
            get
            {
                if (CurrentAmount <= 0) return "Boş";
                if (CurrentAmount <= MinLevel) return "Kritik";
                if (CurrentAmount <= MinLevel * 1.5) return "Düşük";
                if (FillPercentage >= 80) return "Dolu";
                return "Normal";
            }
        }
    }
}

