using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Üretim modeli - Python projesindeki gibi basit yapı
    /// </summary>
    public class Production
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Operatör adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OperatorName { get; set; } = string.Empty;
        
        /// <summary>
        /// Taş adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string StoneName { get; set; } = string.Empty;
        
        /// <summary>
        /// Toplam üretim sayısı (makina ana sayısı)
        /// </summary>
        public int Count { get; set; } = 0;
        
        /// <summary>
        /// Başlangıç zamanı
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// Bitiş zamanı
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = false;
        
        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Üretim log modeli - Her üretilen taş için ayrı kayıt
    /// </summary>
    public class ProductionLog
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Üretim ID
        /// </summary>
        public int ProductionId { get; set; }
        
        /// <summary>
        /// Taş adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string StoneName { get; set; } = string.Empty;
        
        /// <summary>
        /// Üretilen palet sayısı (bu seferde)
        /// </summary>
        public int Count { get; set; } = 1;
        
        /// <summary>
        /// Toplam üretim sayısı (makina ana sayısı)
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
