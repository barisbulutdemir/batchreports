using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Mixer2 pigment alias tanımları
    /// </summary>
    public class Pigment2Alias
    {
        public int Id { get; set; }
        
        [Required]
        public int Slot { get; set; } // 1-4 (Mixer2'de 4 pigment)
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
