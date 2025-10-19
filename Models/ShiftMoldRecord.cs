using System;
using System.ComponentModel.DataAnnotations;

namespace takip.Models
{
    /// <summary>
    /// Vardiya kalıp kayıt modeli - Vardiya içinde hangi kalıpla ne kadar üretim yapıldığını takip eder
    /// </summary>
    public class ShiftMoldRecord
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Vardiya ID'si
        /// </summary>
        public int ShiftId { get; set; }
        
        /// <summary>
        /// Kalıp ID'si
        /// </summary>
        public int MoldId { get; set; }
        
        /// <summary>
        /// Kalıp adı (değişiklik olursa diye kaydedilir)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string MoldName { get; set; } = "";
        
        /// <summary>
        /// Operatör adı
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OperatorName { get; set; } = "";
        
        /// <summary>
        /// Kalıp kullanım başlangıç zamanı
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Kalıp kullanım bitiş zamanı
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Başlangıç üretim sayısı (kalıp aktif olduğunda)
        /// </summary>
        public int StartProductionCount { get; set; }
        
        /// <summary>
        /// Bitiş üretim sayısı (kalıp değiştirildiğinde veya vardiya bittiğinde)
        /// </summary>
        public int EndProductionCount { get; set; }
        
        /// <summary>
        /// Bu kalıpla yapılan üretim sayısı (EndProductionCount - StartProductionCount)
        /// </summary>
        public int ProductionCount { get; set; }
        
        /// <summary>
        /// Bu kayıt aktif mi? (vardiya devam ediyor mu)
        /// </summary>
        public bool IsActive { get; set; }
    }
}
