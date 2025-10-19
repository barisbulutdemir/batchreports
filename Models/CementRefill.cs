using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Çimento ekleme kayıtlarını tutan model
    /// </summary>
    public class CementRefill
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
        /// Eklenen miktar (kg)
        /// </summary>
        public double AddedAmount { get; set; }

        /// <summary>
        /// Doldurma miktarı (RefillAmount için alias)
        /// </summary>
        public double RefillAmount => AddedAmount;

        /// <summary>
        /// Miktar (Amount için alias)
        /// </summary>
        public double Amount => AddedAmount;

        /// <summary>
        /// Ekleme tarihi
        /// </summary>
        public DateTime RefilledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Doldurma tarihi (RefillDate için alias)
        /// </summary>
        public DateTime RefillDate => RefilledAt;

        /// <summary>
        /// Ekleme öncesi silo miktarı
        /// </summary>
        public double PreviousAmount { get; set; }

        /// <summary>
        /// Ekleme sonrası silo miktarı
        /// </summary>
        public double NewAmount { get; set; }

        /// <summary>
        /// Operatör adı
        /// </summary>
        [MaxLength(100)]
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// Sevkiyat numarası
        /// </summary>
        [MaxLength(100)]
        public string? ShipmentNumber { get; set; }

        /// <summary>
        /// Tedarikçi
        /// </summary>
        [MaxLength(100)]
        public string? Supplier { get; set; }

        /// <summary>
        /// Notlar
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

