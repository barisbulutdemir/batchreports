using System;
using System.Collections.Generic;

namespace takip.Models
{
    /// <summary>
    /// Malzeme detayları modeli - Vardiya raporu için
    /// </summary>
    public class MaterialDetails
    {
        /// <summary>
        /// Çimento detayları (alias ismi -> miktar)
        /// </summary>
        public Dictionary<string, double> Cements { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Agrega detayları (alias ismi -> miktar)
        /// </summary>
        public Dictionary<string, double> Aggregates { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Katkı detayları (alias ismi -> miktar)
        /// </summary>
        public Dictionary<string, double> Admixtures { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Pigment detayları (alias ismi -> miktar)
        /// </summary>
        public Dictionary<string, double> Pigments { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Toplam su miktarı (kg)
        /// </summary>
        public double TotalWaterKg { get; set; } = 0;
        
        /// <summary>
        /// Toplam çimento miktarı (kg)
        /// </summary>
        public double TotalCementKg { get; set; } = 0;
        
        /// <summary>
        /// Toplam agrega miktarı (kg)
        /// </summary>
        public double TotalAggregateKg { get; set; } = 0;
        
        /// <summary>
        /// Toplam katkı miktarı (kg)
        /// </summary>
        public double TotalAdmixtureKg { get; set; } = 0;
        
        /// <summary>
        /// Toplam pigment miktarı (kg)
        /// </summary>
        public double TotalPigmentKg { get; set; } = 0;
    }
}

