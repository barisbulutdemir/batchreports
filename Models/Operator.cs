using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Operatör modeli - Veritabanında operatörleri saklamak için
    /// </summary>
    public class Operator
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true; // Aktif operatör mü?

        /// <summary>
        /// Operatör adını döndür - ComboBox'ta görüntüleme için
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
