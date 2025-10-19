namespace takip.Models
{
    public class AggregateAlias
    {
        public int Id { get; set; }
        public short Slot { get; set; } // 1..5
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}


