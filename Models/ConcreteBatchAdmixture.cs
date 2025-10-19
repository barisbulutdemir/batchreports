namespace takip.Models
{
    public class ConcreteBatchAdmixture
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public ConcreteBatch Batch { get; set; } = null!;

        public short Slot { get; set; } // 1..4
        public string? Name { get; set; }
        public double ChemicalKg { get; set; }
        public double WaterKg { get; set; }
        
        // UI için alias name
        public string AliasName => Name ?? $"Admixture{Slot}";
    }
}


