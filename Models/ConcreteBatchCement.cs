namespace takip.Models
{
    public class ConcreteBatchCement
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public ConcreteBatch Batch { get; set; } = null!;

        public short Slot { get; set; } // 1-3 (Mixer1'de 3 çimento)
        public string CementType { get; set; } = string.Empty; // e.g., standard/black/white
        public double WeightKg { get; set; }
        
        // UI için alias name ve kg
        public string AliasName => CementType;
        public double Kg => WeightKg;
    }
}


