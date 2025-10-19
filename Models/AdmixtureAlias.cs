namespace takip.Models
{
    public class AdmixtureAlias
    {
        public int Id { get; set; }
        public short Slot { get; set; } // 1..4
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}


