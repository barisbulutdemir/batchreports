using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace takip.Models
{
    /// <summary>
    /// Üretim notları ve fire ürün takibi
    /// </summary>
    public class ProductionNote
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Vardiya ID'si
        /// </summary>
        public int ShiftId { get; set; }
        
        /// <summary>
        /// Vardiya bilgisi
        /// </summary>
        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
        
        /// <summary>
        /// Üretim notu
        /// </summary>
        public string Note { get; set; } = "";
        
        /// <summary>
        /// Fire ürün miktarı
        /// </summary>
        public int FireProductCount { get; set; }
        
        /// <summary>
        /// Not oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Not güncellenme tarihi
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Not oluşturan operatör
        /// </summary>
        public string CreatedBy { get; set; } = "";
    }
}
