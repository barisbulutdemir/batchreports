namespace takip.Models
{
    public class ConcreteBatchAggregate
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public ConcreteBatch Batch { get; set; } = null!;

        public short Slot { get; set; } // 1..5
        public string? Name { get; set; }
        public double WeightKg { get; set; }
        
        // UI için alias name ve kg
        public string AliasName => Name ?? $"Aggregate{Slot}";
        public double Kg => WeightKg;
    }
}


