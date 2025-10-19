using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Aktif vardiya durumu modeli - Program kapanıp açıldığında vardiya durumunu korumak için
    /// </summary>
    public class ActiveShift
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Vardiya başlama tarihi
        /// </summary>
        public DateTime ShiftStartTime { get; set; }

        /// <summary>
        /// Operatör adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OperatorName { get; set; } = "";

        /// <summary>
        /// Vardiya kaydının ID'si (ShiftRecord tablosundaki ID)
        /// </summary>
        public int ShiftRecordId { get; set; }

        /// <summary>
        /// Üretim başlama tarihi (ilk taş geldiğinde)
        /// </summary>
        public DateTime? ProductionStartTime { get; set; }

        /// <summary>
        /// Başlangıç toplam üretim sayısı (vardiya başlangıcındaki değer)
        /// </summary>
        public int StartTotalProduction { get; set; } = 0;

        /// <summary>
        /// Başlangıç DM452 değeri (vardiya başlangıcındaki değer)
        /// </summary>
        public int? StartDm452Value { get; set; }

        /// <summary>
        /// Vardiya durumu (aktif/pasif)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kayıt oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fire mal sayısı (vardiya başlangıcındaki değer)
        /// </summary>
        public int StartFireProductCount { get; set; } = 0;

        /// <summary>
        /// Boşta geçen süre (saniye cinsinden)
        /// </summary>
        public int IdleTimeSeconds { get; set; } = 0;
    }
}
