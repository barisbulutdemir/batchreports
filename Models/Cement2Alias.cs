using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Mixer2 çimento alias tanımları
    /// </summary>
    public class Cement2Alias
    {
        public int Id { get; set; }
        
        [Required]
        public int Slot { get; set; } // 1-3 (Mixer2'de 3 çimento)
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
