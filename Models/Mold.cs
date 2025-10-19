using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Kalıp modeli - Üretimde kullanılan kalıpları temsil eder
    /// </summary>
    public class Mold
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Kalıp adı - Kullanıcı tarafından girilen kalıp adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Kalıp kodu - Benzersiz kalıp tanımlayıcısı
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Kalıp aktif mi? - Şu anda kullanımda olan kalıp
        /// </summary>
        public bool IsActive { get; set; } = false;
        
        /// <summary>
        /// Toplam baskı sayısı - Bu kalıpla yapılan toplam üretim
        /// </summary>
        public int TotalPrints { get; set; } = 0;
        
        /// <summary>
        /// Oluşturulma tarihi - Kalıbın sisteme eklenme zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Son güncelleme tarihi - Kalıp bilgilerinin son değiştirilme zamanı
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Kalıp açıklaması - Ek bilgiler için
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
