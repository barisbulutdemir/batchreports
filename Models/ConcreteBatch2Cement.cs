namespace takip.Models
{
    /// <summary>
    /// Mixer2 batch çimento detayları
    /// </summary>
    public class ConcreteBatch2Cement
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public short Slot { get; set; } // 1-3 (Mixer2'de 3 çimento)
        public string CementType { get; set; } = string.Empty;
        public double WeightKg { get; set; }
        
        // UI için alias name ve kg
        public string AliasName => CementType;
        public double Kg => WeightKg;

        // Navigation property
        public ConcreteBatch2 Batch { get; set; } = null!;
    }
}
