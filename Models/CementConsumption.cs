using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Çimento tüketim kayıtlarını tutan model
    /// </summary>
    public class CementConsumption
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Silo ID
        /// </summary>
        [Required]
        public int SiloId { get; set; }

        /// <summary>
        /// Silo referansı
        /// </summary>
        public virtual CementSilo Silo { get; set; } = null!;

        /// <summary>
        /// Tüketilen miktar (kg)
        /// </summary>
        public double ConsumedAmount { get; set; }

        /// <summary>
        /// Miktar (Amount için alias)
        /// </summary>
        public double Amount => ConsumedAmount;

        /// <summary>
        /// Tüketim tarihi
        /// </summary>
        public DateTime ConsumedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tüketim tarihi (ConsumptionDate için alias)
        /// </summary>
        public DateTime ConsumptionDate => ConsumedAt;

        /// <summary>
        /// Hangi batch'ten tüketildi
        /// </summary>
        public int? BatchId { get; set; }

        /// <summary>
        /// Hangi mixer'dan tüketildi (1 veya 2)
        /// </summary>
        public int MixerId { get; set; }

        /// <summary>
        /// Tüketim sonrası silo miktarı
        /// </summary>
        public double RemainingAmount { get; set; }

        /// <summary>
        /// Tüketim türü (Production, Manual, Adjustment)
        /// </summary>
        [MaxLength(50)]
        public string ConsumptionType { get; set; } = "Production";

        /// <summary>
        /// Notlar
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

