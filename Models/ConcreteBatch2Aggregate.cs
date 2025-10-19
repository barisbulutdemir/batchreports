namespace takip.Models
{
    /// <summary>
    /// Mixer2 batch agrega detayları
    /// </summary>
    public class ConcreteBatch2Aggregate
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public short Slot { get; set; } // 1-8 (Mixer2'de 8 agrega)
        public string? Name { get; set; }
        public double WeightKg { get; set; }
        
        // UI için alias name ve kg
        public string AliasName => Name ?? $"Aggregate{Slot}";
        public double Kg => WeightKg;

        // Navigation property
        public ConcreteBatch2 Batch { get; set; } = null!;
    }
}
