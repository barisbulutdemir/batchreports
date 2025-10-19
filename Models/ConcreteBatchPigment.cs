using System;

namespace takip.Models
{
    /// <summary>
    /// Beton partisindeki pigment bilgilerini tutan model
    /// </summary>
    public class ConcreteBatchPigment
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Beton partisi ID'si - Foreign key
        /// </summary>
        public int BatchId { get; set; }
        
        /// <summary>
        /// Pigment slot numarası (1-4 arası)
        /// </summary>
        public int Slot { get; set; }
        
        /// <summary>
        /// Pigment adı
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Pigment miktarı (kg)
        /// </summary>
        public double Kg { get; set; }
        
        /// <summary>
        /// Pigment ağırlığı (kg) - WeightKg alias
        /// </summary>
        public double WeightKg { get; set; }
        
        /// <summary>
        /// Pigment yüzdesi
        /// </summary>
        public double Percent { get; set; }
        
        /// <summary>
        /// İlgili beton partisi - Navigation property
        /// </summary>
        public ConcreteBatch Batch { get; set; } = null!;
    }
}
