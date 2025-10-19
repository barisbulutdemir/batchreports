using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Pigment/Boya slotları için kullanıcı tanımlı isimler
    /// </summary>
    public class PigmentAlias
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public short Slot { get; set; } // 1-4

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
