using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Vardiya modeli - Çalışma vardiyalarını temsil eder
    /// </summary>
    public class Shift
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Vardiya adı - Vardiya tanımlayıcısı (Örn: Sabah Vardiyası)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Vardiya aktif mi? - Şu anda devam eden vardiya
        /// </summary>
        public bool IsActive { get; set; } = false;
        
        /// <summary>
        /// Operatör adı - Bu vardiyada çalışan operatör
        /// </summary>
        [MaxLength(100)]
        public string? OperatorName { get; set; }
        
        /// <summary>
        /// Oluşturulma tarihi - Vardiyanın sisteme eklenme zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
