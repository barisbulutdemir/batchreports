namespace takip.Models
{
    /// <summary>
    /// Mixer2 batch katkı detayları
    /// </summary>
    public class ConcreteBatch2Admixture
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public short Slot { get; set; } // 1-4 (Mixer2'de 4 katkı)
        public string? Name { get; set; }
        public double ChemicalKg { get; set; }
        public double WaterKg { get; set; }
        
        // UI için alias name
        public string AliasName => Name ?? $"Admixture{Slot}";

        // Navigation property
        public ConcreteBatch2 Batch { get; set; } = null!;
    }
}
