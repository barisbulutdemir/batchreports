namespace takip.Enums
{
    /// <summary>
    /// Olay türleri
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Malzeme tartıldı
        /// </summary>
        Weighed,
        
        /// <summary>
        /// Dikey kovaya alındı
        /// </summary>
        MovedToVertical,
        
        /// <summary>
        /// Bunkere alındı
        /// </summary>
        MovedToBunker,
        
        /// <summary>
        /// Bunker için beklemeye alındı
        /// </summary>
        WaitingForBunker,
        
        /// <summary>
        /// Mixere teslim edildi
        /// </summary>
        DeliveredToMixer,
        
        /// <summary>
        /// Bunker boşaltıldı
        /// </summary>
        BunkerEmptied,
        
        /// <summary>
        /// Mixer boşaltıldı
        /// </summary>
        MixerEmptied,
        
        /// <summary>
        /// Durum değişimi
        /// </summary>
        StateChanged
    }
}






