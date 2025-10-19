using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip.Services
{
    /// <summary>
    /// Mixer2 Status-Based Batch Processor - Çoklu Batch Sistemi
    /// </summary>
    public class Mixer2StatusBasedProcessor
    {
        private readonly ProductionDbContext _context;
        private readonly ConcreteBatch2Service _batchService;
        private readonly CementConsumptionService _cementConsumptionService;
        
        public static event Action<string>? OnFlowEvent;
        
        // Debug log için event
        public static event Action<string>? OnDebugLogEvent;

        // Edge detection için önceki durumlar
        private bool _prevYatayKova = false;
        private bool _prevDikeyKova = false;
        private bool _prevBeklemeBunker = false;
        private bool _prevMixerAgrega = false;
        private bool _prevMixerCimento = false;
        private bool _prevMixerLoadcellSu = false;
        private bool _prevMixerKatki = false;
        private bool _prevMixerPulseSu = false;
        private bool _prevKatkiGrupAktif = false;
        private bool _prevHarcHazir = false;
        
        
        // Katkı tartım OK edge detection için önceki durumlar
        private bool _prevKatki1TartimOk = false;
        private bool _prevKatki2TartimOk = false;
        private bool _prevKatki3TartimOk = false;
        private bool _prevKatki4TartimOk = false;
        private bool _prevKatki1SuTartimOk = false;
        private bool _prevKatki2SuTartimOk = false;
        private bool _prevKatki3SuTartimOk = false;
        private bool _prevKatki4SuTartimOk = false;
        
        // Pigment edge detection için önceki durumlar
        private bool _prevMixerPigmentGrup = false;
        private bool _prevMixerPigment1 = false;
        
        // Çimento kaydı için bekleyen batch ID'leri
        private HashSet<int> _waitingForCementBatchIds = new HashSet<int>();
        
        // Çimento kaydı yapılan batch ID'leri (tekrar kayıt yapılmasın diye)
        private HashSet<int> _cementRecordedBatchIds = new HashSet<int>();
        
        // Çimento kayıt zamanları (2 saniye bekleme için)
        private Dictionary<int, DateTime> _cementRecordTimes = new Dictionary<int, DateTime>();
        
        // Katkı kaydı için bekleyen batch ID'leri
        private HashSet<int> _waitingForAdmixtureBatchIds = new HashSet<int>();
        
        // Katkı kaydı yapılan batch ID'leri (tekrar kayıt yapılmasın diye)
        private HashSet<int> _admixtureRecordedBatchIds = new HashSet<int>();
        
        // Katkı kayıt zamanları (2 saniye bekleme için)
        private Dictionary<int, DateTime> _admixtureRecordTimes = new Dictionary<int, DateTime>();
        
        // Çimento edge detection için önceki durumlar
        private bool _prevCimento1Active = false;
        private bool _prevCimento2Active = false;
        private bool _prevCimento3Active = false;
        private bool _prevMixerPigment2 = false;
        private bool _prevMixerPigment3 = false;
        private bool _prevMixerPigment4 = false;

        // Anlık durumlar (UI için)
        public bool CurrentYatayKova { get; private set; }
        public bool CurrentDikeyKova { get; private set; }
        public bool CurrentBeklemeBunker { get; private set; }
        public bool CurrentMixerAgrega { get; private set; }
        public bool CurrentMixerCimento { get; private set; }
        public bool CurrentMixerLoadcellSu { get; private set; }
        public bool CurrentMixerKatki { get; private set; }
        public bool CurrentMixerPulseSu { get; private set; }
        public bool CurrentHarcHazir { get; private set; }

        public Mixer2StatusBasedProcessor(ProductionDbContext context, ConcreteBatch2Service batchService, CementConsumptionService cementConsumptionService)
        {
            _context = context;
            _batchService = batchService;
            _cementConsumptionService = cementConsumptionService;
        }

        /// <summary>
        /// State'i başka processor'dan kopyala (edge detection için)
        /// </summary>
        public void CopyStateFrom(Mixer2StatusBasedProcessor other)
        {
            _prevYatayKova = other._prevYatayKova;
            _prevDikeyKova = other._prevDikeyKova;
            _prevBeklemeBunker = other._prevBeklemeBunker;
            _prevMixerAgrega = other._prevMixerAgrega;
            _prevMixerCimento = other._prevMixerCimento;
            _prevMixerLoadcellSu = other._prevMixerLoadcellSu;
            _prevMixerKatki = other._prevMixerKatki;
            _prevMixerPulseSu = other._prevMixerPulseSu;
            _prevKatkiGrupAktif = other._prevKatkiGrupAktif;
            _prevHarcHazir = other._prevHarcHazir;
            
            // Katkı tartım OK edge detection
            _prevKatki1TartimOk = other._prevKatki1TartimOk;
            _prevKatki2TartimOk = other._prevKatki2TartimOk;
            _prevKatki3TartimOk = other._prevKatki3TartimOk;
            _prevKatki4TartimOk = other._prevKatki4TartimOk;
            _prevKatki1SuTartimOk = other._prevKatki1SuTartimOk;
            _prevKatki2SuTartimOk = other._prevKatki2SuTartimOk;
            _prevKatki3SuTartimOk = other._prevKatki3SuTartimOk;
            _prevKatki4SuTartimOk = other._prevKatki4SuTartimOk;
            _prevMixerPigmentGrup = other._prevMixerPigmentGrup;
            _prevMixerPigment1 = other._prevMixerPigment1;
            _prevMixerPigment2 = other._prevMixerPigment2;
            _prevMixerPigment3 = other._prevMixerPigment3;
            _prevMixerPigment4 = other._prevMixerPigment4;
        }

        /// <summary>
        /// PLC snapshot'ını işle - Mixer2 çoklu batch sistemi
        /// </summary>
        public async Task ProcessPlcSnapshotAsync(PlcDataSnapshot snapshot, string operatorName)
        {
            try
            {
                // 1. Yatay kova sinyali kontrolü - Yeni batch oluştur
                await CheckYatayKovaSignal(snapshot, operatorName);

                // 2. Dikey kova sinyali kontrolü - Status geçişi
                await CheckDikeyKovaSignal(snapshot);

                // 3. Bekleme bunker sinyali kontrolü - Status geçişi
                await CheckBeklemeBunkerSignal(snapshot);

                // 4. Mixer agrega sinyali kontrolü - Status geçişi
                await CheckMixerAgregaSignal(snapshot);

                // 5. Mixer içerik sinyalleri - Veri kaydetme
                var mixerBatches = await GetBatchesByStatus("Mixerde");
                OnFlowEvent?.Invoke($"🔍 Mixer Debug - Mixerde batch sayısı: {mixerBatches.Count}");
                
                await CheckMixerCimentoSignal(snapshot);
                await CheckMixerLoadcellSuSignal(snapshot);
                await CheckMixerKatkiSignal(snapshot);
                await CheckMixerPulseSuSignal(snapshot);
                await CheckMixerPigmentSignal(snapshot); // ✅ Pigment kayıt eklendi

                // 6. Harç hazır sinyali - Batch tamamlama
                await CheckHarcHazirSignal(snapshot);
            }
            catch (Exception ex)
            {
                OnFlowEvent?.Invoke($"❌ {LocalizationService.Instance.GetString("Mixer2StatusBasedProcessor.ProcessingError")}: {ex.Message}");
            }
        }

        /// <summary>
        /// Yatay kova sinyali kontrolü - Yeni batch oluştur
        /// </summary>
        private async Task CheckYatayKovaSignal(PlcDataSnapshot snapshot, string operatorName)
        {
            var yatayKova = GetBitValue(snapshot, "YatayKovadaMalzemeVar");
            CurrentYatayKova = yatayKova;

            // Rising edge: Yeni batch oluştur
            if (yatayKova && !_prevYatayKova)
            {
                await CreateNewBatch(snapshot, operatorName);
                OnFlowEvent?.Invoke($"🟢 {LocalizationService.Instance.GetString("Mixer2StatusBasedProcessor.HorizontalBucketNewBatch")}");
            }

            // _prevYatayKova'yı burada güncelleme - CheckDikeyKovaSignal'de güncellenecek
        }

        /// <summary>
        /// Dikey kova sinyali kontrolü - Status geçişi
        /// </summary>
        private async Task CheckDikeyKovaSignal(PlcDataSnapshot snapshot)
        {
            var dikeyKova = GetBitValue(snapshot, "DikeyKovadaMalzemeVar");
            CurrentDikeyKova = dikeyKova;

            // Debug log - Dikey kova durumu
            OnFlowEvent?.Invoke($"🔍 Dikey Kova Debug - DikeyKova: {dikeyKova}");

            // Yatay kova sinyali pasif olduğunda dikey kovaya taşı (falling edge)
            var yatayKova = GetBitValue(snapshot, "YatayKovadaMalzemeVar");
            
            // Debug log
            OnFlowEvent?.Invoke($"🔍 Statüs Debug - YatayKova: {yatayKova}, PrevYatayKova: {_prevYatayKova}");
            
            if (!yatayKova && _prevYatayKova) // Yatay kova pasif oldu
            {
                await UpdateBatchStatus("Yatay Kovada", "Dikey Kovada");
                OnFlowEvent?.Invoke($"📦 Yatay kova boşaldı - Batch'ler dikey kovaya taşındı");
            }

            _prevYatayKova = yatayKova; // Yatay kova durumunu burada güncelle
        }

        /// <summary>
        /// Bekleme bunker sinyali kontrolü - Status geçişi
        /// </summary>
        private async Task CheckBeklemeBunkerSignal(PlcDataSnapshot snapshot)
        {
            var beklemeBunker = GetBitValue(snapshot, "BeklemeBunkerindeMalzemeVar");
            CurrentBeklemeBunker = beklemeBunker;

            // Dikey kova sinyali pasif olduğunda bekleme bunkerine taşı (falling edge)
            var dikeyKova = GetBitValue(snapshot, "DikeyKovadaMalzemeVar");
            
            // Debug log
            OnFlowEvent?.Invoke($"🔍 Bekleme Debug - DikeyKova: {dikeyKova}, PrevDikeyKova: {_prevDikeyKova}");
            
            if (!dikeyKova && _prevDikeyKova) // Dikey kova pasif oldu
            {
                await UpdateBatchStatus("Dikey Kovada", "Bekleme Bunkerinde");
                OnFlowEvent?.Invoke($"📦 Dikey kova boşaldı - Batch'ler bekleme bunkerine taşındı");
            }

            _prevDikeyKova = dikeyKova; // Dikey kova durumunu burada güncelle
        }

        /// <summary>
        /// Mixer agrega sinyali kontrolü - Status geçişi
        /// </summary>
        private async Task CheckMixerAgregaSignal(PlcDataSnapshot snapshot)
        {
            var mixerAgrega = GetBitValue(snapshot, "MixerdeAggregaVar");
            CurrentMixerAgrega = mixerAgrega;

            // Bekleme bunkeri sinyali pasif olduğunda mixere taşı (falling edge)
            var beklemeBunker = GetBitValue(snapshot, "BeklemeBunkerindeMalzemeVar");
            
            // Debug log
            OnFlowEvent?.Invoke($"🔍 Mixer Debug - BeklemeBunker: {beklemeBunker}, PrevBeklemeBunker: {_prevBeklemeBunker}");
            
            if (!beklemeBunker && _prevBeklemeBunker) // Bekleme bunkeri pasif oldu
            {
                await UpdateBatchStatus("Bekleme Bunkerinde", "Mixerde");
                OnFlowEvent?.Invoke($"🏭 Bekleme bunkeri boşaldı - Batch'ler mixere taşındı");
            }

            _prevMixerAgrega = mixerAgrega;
            _prevBeklemeBunker = beklemeBunker; // Bekleme bunker durumunu burada güncelle
        }

        /// <summary>
        /// Mixer çimento sinyali kontrolü - Veri kaydetme
        /// </summary>
        private async Task CheckMixerCimentoSignal(PlcDataSnapshot snapshot)
        {
            // Mixerde batch kontrolü
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            
            // Yeni Mixerde batch'leri çimento bekleyen listesine ekle (sadece daha önce kayıt yapılmamış olanlar)
            foreach (var batch in mixerBatches)
            {
                // 2 saniye bekleme kontrolü
                bool canRecord = true;
                if (_cementRecordTimes.ContainsKey(batch.Id))
                {
                    var timeSinceLastRecord = DateTime.Now - _cementRecordTimes[batch.Id];
                    if (timeSinceLastRecord.TotalSeconds < 2)
                    {
                        canRecord = false;
                        OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} 2 saniye bekleme süresinde - kayıt atlandı (Kalan: {2 - timeSinceLastRecord.TotalSeconds:F1}s)");
                    }
                }
                
                if (!_waitingForCementBatchIds.Contains(batch.Id) && !_cementRecordedBatchIds.Contains(batch.Id) && canRecord)
                {
                    _waitingForCementBatchIds.Add(batch.Id);
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} çimento bekleyen listesine eklendi");
                }
                else if (!canRecord)
                {
                    // 2 saniye bekleme süresinde - zaten log yazıldı
                }
                else if (_cementRecordedBatchIds.Contains(batch.Id))
                {
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} zaten kayıt edilmiş - atlandı");
                }
                else if (_waitingForCementBatchIds.Contains(batch.Id))
                {
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} zaten bekleyen - atlandı");
                }
            }
            
            // Artık Mixerde olmayan batch'leri çimento bekleyen listesinden çıkar
            var allBatchIds = mixerBatches.Select(b => b.Id).ToHashSet();
            var toRemove = _waitingForCementBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                _waitingForCementBatchIds.Remove(id);
                OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {id} çimento bekleyen listesinden çıkarıldı (statüs değişti)");
            }
            
            // Artık Mixerde olmayan batch'leri çimento kayıt edilen listesinden de çıkar
            var toRemoveFromRecorded = _cementRecordedBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
            foreach (var id in toRemoveFromRecorded)
            {
                _cementRecordedBatchIds.Remove(id);
                _cementRecordTimes.Remove(id);
                OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {id} çimento kayıt edilen listesinden çıkarıldı (statüs değişti)");
            }

            if (mixerBatches.Count == 0) 
            {
                OnFlowEvent?.Invoke("🧱 DEBUG: Mixerde batch bulunamadı - çimento kayıt atlandı");
                return;
            }

            OnFlowEvent?.Invoke($"🧱 DEBUG: Mixerde batch sayısı: {mixerBatches.Count}, Çimento bekleyen: {_waitingForCementBatchIds.Count}");

            // Çimento bekleyen batch'ler varsa çimento kontrolü yap
            if (_waitingForCementBatchIds.Count > 0)
            {
                // Her çimento için edge detection kontrolü
                bool anyCimentoRisingEdge = false;
                for (int i = 1; i <= 3; i++)
                {
                    var aktif = GetBitValue(snapshot, $"M2_Cimento{i}Aktif");
                    var kg = GetWordValue(snapshot, $"M2_Cimento{i}Kg");
                    
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento{i} - Aktif: {aktif}, Kg: {kg}");
                    
                    if (aktif && kg > 0)
                    {
                        // Edge detection: Aktif False → True geçişi
                        bool prevAktif = i switch
                        {
                            1 => _prevCimento1Active,
                            2 => _prevCimento2Active,
                            3 => _prevCimento3Active,
                            _ => false
                        };
                        
                        if (!prevAktif) // Rising edge
                        {
                            anyCimentoRisingEdge = true;
                            OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento{i} aktif rising edge tespit edildi - kayıt işlemi başlatılıyor");
                            break;
                        }
                    }
                }
                
                // Eğer bekleyen batch'ler zaten kayıt edilmişse, rising edge kontrolünü atla
                if (anyCimentoRisingEdge && _waitingForCementBatchIds.Any(id => _cementRecordedBatchIds.Contains(id)))
                {
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Bekleyen batch'ler zaten kayıt edilmiş - rising edge atlandı");
                    anyCimentoRisingEdge = false;
                    
                    // Bekleyen batch'leri kayıt edilen listesinden çıkar
                    var alreadyRecorded = _waitingForCementBatchIds.Where(id => _cementRecordedBatchIds.Contains(id)).ToList();
                    foreach (var batchId in alreadyRecorded)
                    {
                        _waitingForCementBatchIds.Remove(batchId);
                        OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batchId} bekleyen listesinden çıkarıldı (zaten kayıt edilmiş)");
                    }
                }

                // Eğer herhangi bir çimento rising edge ise kaydet
                if (anyCimentoRisingEdge)
                {
                    await RecordCementData(snapshot);
                    
                    // Kayıt yapılan batch'leri kayıt edilen listeye ekle ve zamanı kaydet
                    var recordedBatchIds = _waitingForCementBatchIds.ToList();
                    foreach (var batchId in recordedBatchIds)
                    {
                        _cementRecordedBatchIds.Add(batchId);
                        _cementRecordTimes[batchId] = DateTime.Now; // Kayıt zamanını kaydet
                        OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batchId} kayıt edilen listeye eklendi ve zaman kaydedildi");
                    }
                    
                    // Sadece kayıt yapılan batch'leri bekleyen listesinden çıkar (tüm listeyi temizleme)
                    foreach (var batchId in recordedBatchIds)
                    {
                        _waitingForCementBatchIds.Remove(batchId);
                    }
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento kaydedildi - kayıt yapılan batch'ler bekleyen listesinden çıkarıldı. Kayıt edilen batch sayısı: {_cementRecordedBatchIds.Count}");
                    
                    // SADECE KAYIT YAPILDIĞINDA önceki durumları güncelle
                    _prevCimento1Active = GetBitValue(snapshot, "M2_Cimento1Aktif");
                    _prevCimento2Active = GetBitValue(snapshot, "M2_Cimento2Aktif");
                    _prevCimento3Active = GetBitValue(snapshot, "M2_Cimento3Aktif");
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento state'leri güncellendi - C1: {_prevCimento1Active}, C2: {_prevCimento2Active}, C3: {_prevCimento3Active}");
                }
                else
                {
                    OnFlowEvent?.Invoke("🧱 DEBUG: Hiçbir çimento rising edge değil - beklemeye devam ediyor");
                }
            }
        }

        /// <summary>
        /// Mixer loadcell su sinyali kontrolü - Veri kaydetme
        /// </summary>
        private async Task CheckMixerLoadcellSuSignal(PlcDataSnapshot snapshot)
        {
            var mixerLoadcellSu = GetBitValue(snapshot, "MixerdeLoadcellSuVar");
            CurrentMixerLoadcellSu = mixerLoadcellSu;

            // Rising edge: Mixerdeki batch'lere loadcell su verilerini kaydet
            if (mixerLoadcellSu && !_prevMixerLoadcellSu)
            {
                await RecordLoadcellWaterData(snapshot);
                OnFlowEvent?.Invoke("💧 Mixer loadcell su - Loadcell su verileri kaydedildi");
            }

            _prevMixerLoadcellSu = mixerLoadcellSu;
        }

        /// <summary>
        /// Mixer katkı sinyali kontrolü - Veri kaydetme
        /// </summary>
        /// <summary>
        /// Katkı sinyallerini sürekli güncelle (batch status'u fark etmez)
        /// </summary>
        private void UpdateAdmixtureSignals(PlcDataSnapshot snapshot)
        {
            var admixtureDebugSb = new System.Text.StringBuilder();
            admixtureDebugSb.AppendLine("🧪 KATKI SİNYALLERİ");
            admixtureDebugSb.AppendLine("═══════════════════════════════════════");
            
            // Katkı grup aktif (H39.10)
            var katkiGrupAktif = GetBitValue(snapshot, "M2_KatkiGrupAktif");
            admixtureDebugSb.AppendLine($"📊 GRUP AKTİF (H39.10): {katkiGrupAktif}");
            admixtureDebugSb.AppendLine("");
            
            // Her katkı için tüm register'ları logla
            for (int i = 1; i <= 4; i++)
            {
                var aktif = GetBitValue(snapshot, $"M2_Katki{i}Aktif");
                var tartimOk = GetBitValue(snapshot, $"M2_Katki{i}TartimOk");
                var suTartimOk = GetBitValue(snapshot, $"M2_Katki{i}SuTartimOk");
                var kimyasalKg = GetWordValue(snapshot, $"M2_Katki{i}KimyasalKg");
                var suKg = GetWordValue(snapshot, $"M2_Katki{i}SuKg");

                admixtureDebugSb.AppendLine($"🧪 KATKI{i} (H3{i+8}.0):");
                admixtureDebugSb.AppendLine($"   ✅ Aktif: {aktif}");
                admixtureDebugSb.AppendLine($"   ⚖️ Tartım OK (H3{i+8}.3): {tartimOk}");
                admixtureDebugSb.AppendLine($"   💧 Su Tartım OK (H3{i+8}.4): {suTartimOk}");
                admixtureDebugSb.AppendLine($"   🧪 Kimyasal Kg (DM460{i*10+4}): {kimyasalKg:F2}");
                admixtureDebugSb.AppendLine($"   💧 Su Kg (DM460{i*10+5}): {suKg:F2}");
                admixtureDebugSb.AppendLine("");
            }
            
            OnDebugLogEvent?.Invoke(admixtureDebugSb.ToString());
        }

        /// <summary>
        /// Mixer katkı sinyali kontrolü - Katkı verilerini kaydet
        /// </summary>
        private async Task CheckMixerKatkiSignal(PlcDataSnapshot snapshot)
        {
            // Katkı sinyallerini sürekli güncelle
            UpdateAdmixtureSignals(snapshot);
            
            // Mixerde batch kontrolü
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            
            // Yeni Mixerde batch'leri katkı bekleyen listesine ekle (sadece daha önce kayıt yapılmamış olanlar)
            foreach (var batch in mixerBatches)
            {
                // 2 saniye bekleme kontrolü
                bool canRecord = true;
                if (_admixtureRecordTimes.ContainsKey(batch.Id))
                {
                    var timeSinceLastRecord = DateTime.Now - _admixtureRecordTimes[batch.Id];
                    if (timeSinceLastRecord.TotalSeconds < 2)
                    {
                        canRecord = false;
                        OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {batch.Id} 2 saniye bekleme süresinde - kayıt atlandı (Kalan: {2 - timeSinceLastRecord.TotalSeconds:F1}s)");
                    }
                }
                
                if (!_waitingForAdmixtureBatchIds.Contains(batch.Id) && !_admixtureRecordedBatchIds.Contains(batch.Id) && canRecord)
                {
                    _waitingForAdmixtureBatchIds.Add(batch.Id);
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {batch.Id} katkı bekleyen listesine eklendi");
                }
                else if (!canRecord)
                {
                    // 2 saniye bekleme süresinde - zaten log yazıldı
                }
                else if (_admixtureRecordedBatchIds.Contains(batch.Id))
                {
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {batch.Id} zaten kayıt edilmiş - atlandı");
                }
                else if (_waitingForAdmixtureBatchIds.Contains(batch.Id))
                {
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {batch.Id} zaten bekleyen - atlandı");
                }
            }
            
            // Artık Mixerde olmayan batch'leri katkı bekleyen listesinden çıkar
            var allBatchIds = mixerBatches.Select(b => b.Id).ToHashSet();
            var toRemove = _waitingForAdmixtureBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                _waitingForAdmixtureBatchIds.Remove(id);
                OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {id} katkı bekleyen listesinden çıkarıldı (statüs değişti)");
            }
            
            // Artık Mixerde olmayan batch'leri katkı kayıt edilen listesinden de çıkar
            var toRemoveFromRecorded = _admixtureRecordedBatchIds.Where(id => !allBatchIds.Contains(id)).ToList();
            foreach (var id in toRemoveFromRecorded)
            {
                _admixtureRecordedBatchIds.Remove(id);
                _admixtureRecordTimes.Remove(id);
                OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {id} katkı kayıt edilen listesinden çıkarıldı (statüs değişti)");
            }

            // Katkı grup aktif kontrolü
            var katkiGrupAktif = GetBitValue(snapshot, "M2_KatkiGrupAktif");
            if (!katkiGrupAktif) 
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Katkı grup aktif değil - kayıt atlandı");
                return;
            }

            // Çimento bekleyen batch'ler varsa katkı kontrolü yap
            if (_waitingForAdmixtureBatchIds.Count == 0)
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Katkı bekleyen batch yok - kayıt atlandı");
                return;
            }

            // Her katkı için tartım OK edge detection kontrolü
            bool anyKatkiTartimOkRisingEdge = false;
            for (int i = 1; i <= 4; i++)
            {
                var aktif = GetBitValue(snapshot, $"M2_Katki{i}Aktif");
                var tartimOk = GetBitValue(snapshot, $"M2_Katki{i}TartimOk");
                var suTartimOk = GetBitValue(snapshot, $"M2_Katki{i}SuTartimOk");
                
                if (aktif && tartimOk && suTartimOk)
                {
                    // Edge detection: Hem kimyasal hem su tartım OK False → True geçişi
                    bool prevTartimOk = i switch
                    {
                        1 => _prevKatki1TartimOk,
                        2 => _prevKatki2TartimOk,
                        3 => _prevKatki3TartimOk,
                        4 => _prevKatki4TartimOk,
                        _ => false
                    };
                    
                    bool prevSuTartimOk = i switch
                    {
                        1 => _prevKatki1SuTartimOk,
                        2 => _prevKatki2SuTartimOk,
                        3 => _prevKatki3SuTartimOk,
                        4 => _prevKatki4SuTartimOk,
                        _ => false
                    };
                    
                    if (!prevTartimOk || !prevSuTartimOk) // Rising edge
                    {
                        anyKatkiTartimOkRisingEdge = true;
                        OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} tartım OK rising edge tespit edildi");
                        // break kaldırıldı - tüm katkıları kontrol et
                    }
                }
            }

            // Eğer herhangi bir katkı tartım OK rising edge ise kaydet
            if (anyKatkiTartimOkRisingEdge)
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Katkı tartım OK rising edge tespit edildi - kayıt işlemi başlatılıyor");
                await RecordAdmixtureData(snapshot);
            }
            else
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Hiçbir katkı tartım OK rising edge değil - kayıt atlandı");
            }

            // Önceki durumları güncelle
            _prevKatki1TartimOk = GetBitValue(snapshot, "M2_Katki1TartimOk");
            _prevKatki2TartimOk = GetBitValue(snapshot, "M2_Katki2TartimOk");
            _prevKatki3TartimOk = GetBitValue(snapshot, "M2_Katki3TartimOk");
            _prevKatki4TartimOk = GetBitValue(snapshot, "M2_Katki4TartimOk");
            _prevKatki1SuTartimOk = GetBitValue(snapshot, "M2_Katki1SuTartimOk");
            _prevKatki2SuTartimOk = GetBitValue(snapshot, "M2_Katki2SuTartimOk");
            _prevKatki3SuTartimOk = GetBitValue(snapshot, "M2_Katki3SuTartimOk");
            _prevKatki4SuTartimOk = GetBitValue(snapshot, "M2_Katki4SuTartimOk");
        }

        /// <summary>
        /// Mixer pulse su sinyali kontrolü - Veri kaydetme
        /// </summary>
        private async Task CheckMixerPulseSuSignal(PlcDataSnapshot snapshot)
        {
            var mixerPulseSu = GetBitValue(snapshot, "MixerdePulseSuVar");
            CurrentMixerPulseSu = mixerPulseSu;

            // Rising edge: Mixerdeki batch'lere pulse su verilerini kaydet
            if (mixerPulseSu && !_prevMixerPulseSu)
            {
                await RecordPulseWaterData(snapshot);
                OnFlowEvent?.Invoke("💧 Mixer pulse su - Pulse su verileri kaydedildi");
            }

            _prevMixerPulseSu = mixerPulseSu;
        }

        /// <summary>
        /// Harç hazır sinyali kontrolü - Batch tamamlama
        /// </summary>
        private async Task CheckHarcHazirSignal(PlcDataSnapshot snapshot)
        {
            var harcHazir = GetBitValue(snapshot, "HarçHazır");
            CurrentHarcHazir = harcHazir;

            // Rising edge: Mixerdeki batch'leri tamamla
            if (harcHazir && !_prevHarcHazir)
            {
                await CompleteBatch(snapshot);
                OnFlowEvent?.Invoke("🎉 Harç hazır - Batch'ler tamamlandı! Status: Tamamlandı");
            }

            _prevHarcHazir = harcHazir;
        }

        /// <summary>
        /// Yeni batch oluştur
        /// </summary>
        private async Task CreateNewBatch(PlcDataSnapshot snapshot, string operatorName)
        {
            try
            {
                var batch = new ConcreteBatch2
                {
                    OccurredAt = DateTime.UtcNow,
                    PlantCode = "MIXER2",
                    OperatorName = operatorName,
                    RecipeCode = "AUTO_RECIPE",
                    Status = "Yatay Kovada",
                    IsSimulated = false,
                    CreatedAt = DateTime.UtcNow,
                    RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(snapshot)
                };

                // Aktif agregaları kaydet
                await RecordAggregateData(batch, snapshot);

                _context.ConcreteBatch2s.Add(batch);
                await _context.SaveChangesAsync();

                OnFlowEvent?.Invoke($"✅ Yeni Mixer2 batch oluşturuldu (ID: {batch.Id}) - Status: Yatay Kovada");
            }
            catch (Exception ex)
            {
                OnFlowEvent?.Invoke($"❌ Mixer2 batch oluşturma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega verilerini kaydet
        /// </summary>
        private async Task RecordAggregateData(ConcreteBatch2 batch, PlcDataSnapshot snapshot)
        {
            // Aktif bitlerinde olası mapping sapmalarını bertaraf etmek için KG değerine göre kayıt yap
            // Eşik: 0.1 kg üzeri değerleri gerçek kabul et
            var totalKg = 0.0;
            var aggregateCount = 0;
            
            // 🔍 DEBUG: Tüm agrega değerlerini logla
            var debugInfo = "🔍 Agrega Debug:\n";
            for (int i = 1; i <= 8; i++)
            {
                var kg = GetWordValue(snapshot, $"M2_Agrega{i}Kg");
                debugInfo += $"Agrega{i}: {kg:F2}kg ";
                
                if (kg > 0.1)
                {
                    var aggregate = new ConcreteBatch2Aggregate
                    {
                        Slot = (short)i,
                        Name = $"Agrega{i}",
                        WeightKg = kg
                    };
                    batch.Aggregates.Add(aggregate);
                    totalKg += kg;
                    aggregateCount++;
                }
            }
            
            // Toplam agrega miktarını kaydet
            batch.TotalAggregateKg = totalKg;
            
            // Debug log - Her zaman göster
            OnFlowEvent?.Invoke(debugInfo);
            OnFlowEvent?.Invoke($"🪨 Agrega kayıt: {aggregateCount} adet, Toplam: {totalKg:F1}kg");
        }

        /// <summary>
        /// Çimento verilerini kaydet
        /// </summary>
        private async Task RecordCementData(PlcDataSnapshot snapshot)
        {
            // SADECE bekleyen batch'lere çimento verilerini ekle
            if (_waitingForCementBatchIds.Count == 0)
            {
                OnFlowEvent?.Invoke("🧱 DEBUG: Bekleyen batch yok - çimento kayıt atlandı");
                return;
            }

            // Bekleyen batch'leri al
            using var context = new ProductionDbContext();
            var waitingBatches = await context.ConcreteBatch2s
                .Where(b => _waitingForCementBatchIds.Contains(b.Id))
                .Include(b => b.Cements)
                .ToListAsync();

            if (waitingBatches.Count == 0)
            {
                OnFlowEvent?.Invoke("🧱 DEBUG: Bekleyen batch'ler bulunamadı - çimento kayıt atlandı");
                return;
            }

            foreach (var batch in waitingBatches)
            {
                OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} için çimento kayıt işlemi başlatılıyor");
                
                for (int i = 1; i <= 3; i++)
                {
                    var aktif = GetBitValue(snapshot, $"M2_Cimento{i}Aktif");
                    var tartimOk = GetBitValue(snapshot, $"M2_Cimento{i}TartimOk");
                    var kg = GetWordValue(snapshot, $"M2_Cimento{i}Kg");
                    
                    if (aktif && tartimOk && kg > 0)
                    {
                        // Batch'de zaten bu slot'ta çimento var mı kontrol et
                        var existingCement = batch.Cements.FirstOrDefault(c => c.Slot == i);
                        if (existingCement != null)
                        {
                            OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} için Çimento{i} zaten kayıtlı ({existingCement.WeightKg}kg) - tekrar kayıt atlandı");
                            continue;
                        }
                        
                        var cement = new ConcreteBatch2Cement
                        {
                            Slot = (short)i,
                            CementType = i switch
                            {
                                1 => "Standard",  // Çimento1 -> Standard
                                2 => "Beyaz",     // Çimento2 -> Beyaz
                                3 => "Siyah",     // Çimento3 -> Siyah
                                _ => $"Cimento{i}"
                            },
                            WeightKg = kg
                        };
                        batch.Cements.Add(cement);
                        batch.TotalCementKg += kg;
                        
                        OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento{i} kaydedildi: {kg}kg (Batch {batch.Id})");
                    }
                    else if (aktif && tartimOk && kg == 0)
                    {
                        OnFlowEvent?.Invoke($"🧱 DEBUG: Çimento{i} aktif ve tartım OK ama kg=0 - kayıt atlandı");
                    }
                }
                
                // Batch'i veritabanına kaydet
                await context.SaveChangesAsync();
                
                // Silo güncellemesi
                try
                {
                    var consumptionService = new CementConsumptionService(context);
                    await consumptionService.RecordMixer2ConsumptionAsync(batch);
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Batch {batch.Id} için silo güncelleme tamamlandı");
                }
                catch (Exception ex)
                {
                    OnFlowEvent?.Invoke($"🧱 DEBUG: Silo güncelleme hatası: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Katkı verilerini kaydet - Tek seferde tüm aktif katkıları kaydet
        /// </summary>
        private async Task RecordAdmixtureData(PlcDataSnapshot snapshot)
        {
            // SADECE bekleyen batch'lere katkı verilerini ekle
            if (_waitingForAdmixtureBatchIds.Count == 0)
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Bekleyen batch yok - katkı kayıt atlandı");
                return;
            }

            // Bekleyen batch'leri al
            using var context = new ProductionDbContext();
            var waitingBatches = await context.ConcreteBatch2s
                .Where(b => _waitingForAdmixtureBatchIds.Contains(b.Id))
                .Include(b => b.Admixtures)
                .ToListAsync();

            if (waitingBatches.Count == 0)
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Bekleyen batch'ler bulunamadı - katkı kayıt atlandı");
                return;
            }

            foreach (var mixerBatch in waitingBatches)
            {
                OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {mixerBatch.Id} için katkı kayıt işlemi başlatılıyor");

            // Katkı grup aktif kontrolü
            var katkiGrupAktif = GetBitValue(snapshot, "M2_KatkiGrupAktif");
            if (!katkiGrupAktif) 
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Katkı grup aktif değil - katkı kayıt atlandı");
                return;
            }

            // Katkı alias'larını yükle
            var admixtureAliases = await _context.Admixture2Aliases
                .Where(a => a.IsActive)
                .ToDictionaryAsync(a => a.Slot, a => a.Name);

            // Tüm aktif katkıları topla
            var activeAdmixtures = new List<(int Slot, string Name, double ChemicalKg, double SuKg)>();

            for (int i = 1; i <= 4; i++)
            {
                // Katkı aktif mi?
                var aktif = GetBitValue(snapshot, $"M2_Katki{i}Aktif");
                OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} aktif: {aktif}");
                if (!aktif) continue;

                // Hem kimyasal hem su tartım OK gerekli
                var tartimOk = GetBitValue(snapshot, $"M2_Katki{i}TartimOk");
                var suTartimOk = GetBitValue(snapshot, $"M2_Katki{i}SuTartimOk");
                OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} tartım OK: {tartimOk}, Su tartım OK: {suTartimOk}");

                // Her ikisi de tartım OK olmalı
                if (!tartimOk || !suTartimOk) 
                {
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} tartım OK değil - atlandı");
                    continue;
                }

                var kimyasalKg = GetWordValue(snapshot, $"M2_Katki{i}KimyasalKg") / 10.0;
                var suKg = GetWordValue(snapshot, $"M2_Katki{i}SuKg") / 10.0;
                var totalKg = kimyasalKg + suKg;
                OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} miktarları - Kimyasal: {kimyasalKg}kg, Su: {suKg}kg, Toplam: {totalKg}kg");
                
                // Eşik kontrolü
                if (totalKg <= 0.1) 
                {
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} miktar çok az ({totalKg}kg) - atlandı");
                    continue;
                }
                
                // Batch'de zaten bu slot'ta katkı var mı kontrol et
                var existingAdmixture = mixerBatch.Admixtures.FirstOrDefault(a => a.Slot == i);
                if (existingAdmixture != null)
                {
                    OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {mixerBatch.Id} için Katkı{i} zaten kayıtlı ({existingAdmixture.ChemicalKg + existingAdmixture.WaterKg}kg) - tekrar kayıt atlandı");
                    continue;
                }
                
                // Alias ismi kullan
                var displayName = admixtureAliases.TryGetValue((short)i, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                    ? aliasName
                    : $"Katki{i}";

                OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı{i} kaydedilecek - İsim: {displayName}");
                activeAdmixtures.Add((i, displayName, kimyasalKg, suKg));
            }

            // Eğer aktif katkı varsa, hepsini tek seferde kaydet
            OnFlowEvent?.Invoke($"🧪 DEBUG: Toplam aktif katkı sayısı: {activeAdmixtures.Count}");
            if (activeAdmixtures.Count > 0)
            {
                foreach (var (slot, name, chemicalKg, suKg) in activeAdmixtures)
                {
                    var admixture = new ConcreteBatch2Admixture
                    {
                        Slot = (short)slot,
                        Name = name,
                        ChemicalKg = chemicalKg,
                        WaterKg = suKg
                    };
                    mixerBatch.Admixtures.Add(admixture);
                    mixerBatch.TotalAdmixtureKg += (chemicalKg + suKg);
                }

                await context.SaveChangesAsync();

                var totalChemical = activeAdmixtures.Sum(a => a.ChemicalKg);
                var totalWater = activeAdmixtures.Sum(a => a.SuKg);
                var totalTotal = totalChemical + totalWater;

                OnFlowEvent?.Invoke($"🧪 Katkı kayıt tamamlandı: {activeAdmixtures.Count} katkı, Kimyasal={totalChemical}kg, Su={totalWater}kg, Toplam={totalTotal}kg (Batch {mixerBatch.Id})");
            }
            else
            {
                OnFlowEvent?.Invoke("🧪 DEBUG: Hiç aktif katkı bulunamadı - kayıt yapılmadı");
            }
            
            // Kayıt yapılan batch'leri bekleyen listesinden çıkar
            var recordedBatchIds = _waitingForAdmixtureBatchIds.ToList();
            foreach (var batchId in recordedBatchIds)
            {
                _waitingForAdmixtureBatchIds.Remove(batchId);
                _admixtureRecordedBatchIds.Add(batchId);
                _admixtureRecordTimes[batchId] = DateTime.Now;
                OnFlowEvent?.Invoke($"🧪 DEBUG: Batch {batchId} katkı kayıt edilen listeye eklendi");
            }
            OnFlowEvent?.Invoke($"🧪 DEBUG: Katkı kaydedildi - kayıt yapılan batch'ler bekleyen listesinden çıkarıldı. Kayıt edilen batch sayısı: {_admixtureRecordedBatchIds.Count}");
            }
        }

        /// <summary>
        /// Loadcell su verilerini kaydet
        /// </summary>
        private async Task RecordLoadcellWaterData(PlcDataSnapshot snapshot)
        {
            // "Mixerde" status'taki tüm batch'lere loadcell su verilerini ekle
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            if (mixerBatches.Count == 0) return;

            var kg = GetWordValue(snapshot, "M2_SuLoadcellKg");
            foreach (var batch in mixerBatches)
            {
                batch.LoadcellWaterKg = kg;
                batch.EffectiveWaterKg += kg;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Pulse su verilerini kaydet
        /// </summary>
        private async Task RecordPulseWaterData(PlcDataSnapshot snapshot)
        {
            // "Mixerde" status'taki tüm batch'lere pulse su verilerini ekle
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            if (mixerBatches.Count == 0) return;

            var kg = GetWordValue(snapshot, "M2_SuPulseKg");
            foreach (var batch in mixerBatches)
            {
                batch.PulseWaterKg = kg;
                batch.EffectiveWaterKg += kg;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Batch'i tamamla
        /// </summary>
        private async Task CompleteBatch(PlcDataSnapshot snapshot)
        {
            // "Mixerde" status'taki tüm batch'leri tamamla
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            if (mixerBatches.Count == 0) return;

            foreach (var batch in mixerBatches)
            {
                // Status'u tamamlandı yap
                batch.Status = "Tamamlandı";
                batch.CompletedAt = DateTime.UtcNow;

                // Nem oranını hesapla
                if (batch.TotalCementKg > 0)
                {
                    batch.WaterCementRatio = batch.EffectiveWaterKg / batch.TotalCementKg;
                    batch.MoisturePercent = (batch.EffectiveWaterKg / (batch.TotalCementKg + batch.TotalAggregateKg)) * 100;
                }
            }

            await _context.SaveChangesAsync();
            OnFlowEvent?.Invoke($"🎉 {mixerBatches.Count} Mixer2 batch tamamlandı - Status: Tamamlandı");
        }

        /// <summary>
        /// Belirli status'taki batch'leri getir (çoklu batch sistemi)
        /// </summary>
        private async Task<List<ConcreteBatch2>> GetBatchesByStatus(string status)
        {
            return await _context.ConcreteBatch2s
                .Where(b => b.PlantCode == "MIXER2" && b.Status == status)
                .OrderByDescending(b => b.OccurredAt)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli status'taki tek batch'i getir (tek batch sistemi)
        /// </summary>
        private async Task<ConcreteBatch2?> GetSingleBatchByStatus(string status)
        {
            return await _context.ConcreteBatch2s
                .Where(b => b.PlantCode == "MIXER2" && b.Status == status)
                .OrderByDescending(b => b.OccurredAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Batch status'unu güncelle
        /// </summary>
        private async Task UpdateBatchStatus(string currentStatus, string newStatus)
        {
            // Belirli status'taki tüm batch'leri yeni status'a güncelle
            var batches = await GetBatchesByStatus(currentStatus);
            if (batches.Count == 0) return;

            foreach (var batch in batches)
            {
                batch.Status = newStatus;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Bit değerini oku
        /// </summary>
        private bool GetBitValue(PlcDataSnapshot snapshot, string registerName)
        {
            return registerName switch
            {
                "YatayKovadaMalzemeVar" => snapshot.HorizontalHasMaterial,
                "DikeyKovadaMalzemeVar" => snapshot.VerticalHasMaterial,
                "BeklemeBunkerindeMalzemeVar" => snapshot.WaitingBunkerHasMaterial,
                "MixerdeAggregaVar" => snapshot.MixerHasAggregate,
                "MixerdeCimentoVar" => snapshot.MixerHasCement,
                "MixerdeLoadcellSuVar" => snapshot.MixerHasWaterLoadcell,
                "MixerdeKatkiVar" => snapshot.MixerHasAdmixture,
                "MixerdePulseSuVar" => snapshot.MixerHasWaterPulse,
                "HarçHazır" => snapshot.BatchReadySignal,
                "M2_Agrega1Aktif" => snapshot.Aggregate1Active,
                "M2_Agrega2Aktif" => snapshot.Aggregate2Active,
                "M2_Agrega3Aktif" => snapshot.Aggregate3Active,
                "M2_Agrega4Aktif" => snapshot.Aggregate4Active,
                "M2_Agrega5Aktif" => snapshot.Aggregate5Active,
                "M2_Agrega6Aktif" => snapshot.Aggregate6Active,
                "M2_Agrega7Aktif" => snapshot.Aggregate7Active,
                "M2_Agrega8Aktif" => snapshot.Aggregate8Active,
                "M2_Cimento1Aktif" => snapshot.Cement1Active,
                "M2_Cimento1TartimOk" => snapshot.Cement1TartimOk,
                "M2_Cimento2Aktif" => snapshot.Cement2Active,
                "M2_Cimento2TartimOk" => snapshot.Cement2TartimOk,
                "M2_Cimento3Aktif" => snapshot.Cement3Active,
                "M2_Cimento3TartimOk" => snapshot.Cement3TartimOk,
                "M2_Katki1Aktif" => snapshot.Admixture1Active,
                "M2_Katki2Aktif" => snapshot.Admixture2Active,
                "M2_Katki3Aktif" => snapshot.Admixture3Active,
                "M2_Katki4Aktif" => snapshot.Admixture4Active,
                "M2_KatkiGrupAktif" => snapshot.AdmixtureGroupActive,
                "M2_Katki1TartimOk" => snapshot.Admixture1TartimOk,
                "M2_Katki1SuTartimOk" => snapshot.Admixture1WaterTartimOk,
                "M2_Katki2TartimOk" => snapshot.Admixture2TartimOk,
                "M2_Katki2SuTartimOk" => snapshot.Admixture2WaterTartimOk,
                "M2_Katki3TartimOk" => snapshot.Admixture3TartimOk,
                "M2_Katki3SuTartimOk" => snapshot.Admixture3WaterTartimOk,
                "M2_Katki4TartimOk" => snapshot.Admixture4TartimOk,
                "M2_Katki4SuTartimOk" => snapshot.Admixture4WaterTartimOk,
                "H31.2" => snapshot.PigmentGroupActive,
                "H31.10" => snapshot.Pigment1Active,
                "H31.3" => snapshot.Pigment1TartimOk,
                "H32.10" => snapshot.Pigment2Active,
                "H32.3" => snapshot.Pigment2TartimOk,
                "H33.10" => snapshot.Pigment3Active,
                "H33.3" => snapshot.Pigment3TartimOk,
                "H34.10" => snapshot.Pigment4Active,
                "H34.3" => snapshot.Pigment4TartimOk,
                _ => false
            };
        }

        /// <summary>
        /// Word değerini oku
        /// </summary>
        private double GetWordValue(PlcDataSnapshot snapshot, string registerName)
        {
            return registerName switch
            {
                "M2_Agrega1Kg" => snapshot.Aggregate1Amount,
                "M2_Agrega2Kg" => snapshot.Aggregate2Amount,
                "M2_Agrega3Kg" => snapshot.Aggregate3Amount,
                "M2_Agrega4Kg" => snapshot.Aggregate4Amount,
                "M2_Agrega5Kg" => snapshot.Aggregate5Amount,
                "M2_Agrega6Kg" => snapshot.Aggregate6Amount,
                "M2_Agrega7Kg" => snapshot.Aggregate7Amount,
                "M2_Agrega8Kg" => snapshot.Aggregate8Amount,
                "M2_Cimento1Kg" => snapshot.Cement1Amount,
                "M2_Cimento2Kg" => snapshot.Cement2Amount,
                "M2_Cimento3Kg" => snapshot.Cement3Amount,
                "M2_SuLoadcellKg" => snapshot.Water1Amount,
                "M2_SuPulseKg" => snapshot.Water2Amount,
                "M2_Katki1KimyasalKg" => snapshot.Admixture1ChemicalAmount,
                "M2_Katki1SuKg" => snapshot.Admixture1WaterAmount,
                "M2_Katki2KimyasalKg" => snapshot.Admixture2ChemicalAmount,
                "M2_Katki2SuKg" => snapshot.Admixture2WaterAmount,
                "M2_Katki3KimyasalKg" => snapshot.Admixture3ChemicalAmount,
                "M2_Katki3SuKg" => snapshot.Admixture3WaterAmount,
                "M2_Katki4KimyasalKg" => snapshot.Admixture4ChemicalAmount,
                "M2_Katki4SuKg" => snapshot.Admixture4WaterAmount,
                "DM308" => snapshot.Pigment1Amount,
                "DM310" => snapshot.Pigment2Amount,
                "DM312" => snapshot.Pigment3Amount,
                "DM314" => snapshot.Pigment4Amount,
                _ => 0
            };
        }

        /// <summary>
        /// Mixer pigment sinyali kontrolü - Pigment verilerini kaydet
        /// </summary>
        private async Task CheckMixerPigmentSignal(PlcDataSnapshot snapshot)
        {
            // Pigment register güncelleme kaldırıldı - sadece katkı sinyalleri gösterilecek
            
            // Debug: Tüm status'lardaki batch'leri kontrol et
            var yatayKovaBatches = await GetBatchesByStatus("Yatay Kovada");
            var dikeyKovaBatches = await GetBatchesByStatus("Dikey Kovada");
            var beklemeBatches = await GetBatchesByStatus("Bekleme Bunkerinde");
            var mixerBatches = await GetBatchesByStatus("Mixerde");
            
            OnFlowEvent?.Invoke($"🔍 Pigment Debug - Yatay: {yatayKovaBatches.Count}, Dikey: {dikeyKovaBatches.Count}, Bekleme: {beklemeBatches.Count}, Mixer: {mixerBatches.Count}");
            
            // Sadece "Yatay Kovada" status'taki batch'lere pigment ekle
            if (yatayKovaBatches.Count == 0) 
            {
                OnFlowEvent?.Invoke($"🎨 Pigment kayıt atlandı - Yatay Kovada batch yok");
                return;
            }

            // Her batch için pigment verilerini kaydet
            foreach (var batch in yatayKovaBatches)
            {
                await RecordPigmentData(batch, snapshot);
            }
        }

        // UpdatePigmentRegisterDisplay fonksiyonu kaldırıldı - pigment sinyalleri artık gösterilmiyor

        /// <summary>
        /// Pigment verilerini kaydet - Basitleştirilmiş mantık
        /// Sadece "yatay kovada" status'ta ve pigment grubu aktifse kaydet
        /// </summary>
        private async Task RecordPigmentData(ConcreteBatch2 batch, PlcDataSnapshot snapshot)
        {
            // 🔍 DEBUG: Pigment debug bilgisi
            var pigmentGrupAktif = GetBitValue(snapshot, "H31.2");
            OnFlowEvent?.Invoke($"🎨 Pigment Debug - Batch Status: {batch.Status}, Grup Aktif: {pigmentGrupAktif}");
            
            // 1. Şart: Batch status'u "yatay kovada" olmalı
            if (batch.Status != "Yatay Kovada")
            {
                OnFlowEvent?.Invoke($"🎨 Pigment kayıt atlandı - Status: {batch.Status} (Yatay Kovada olmalı)");
                return; // Sadece yatay kovada iken pigment kaydet
            }

            // 2. Şart: Pigment grubu aktif mi kontrol et
            if (!pigmentGrupAktif)
            {
                OnFlowEvent?.Invoke($"🎨 Pigment kayıt atlandı - Grup aktif değil");
                return; // Pigment grubu aktif değilse hiçbir şey yapma
            }

            // Pigment alias'larını yükle
            var pigmentAliases = await _context.Pigment2Aliases
                .Where(a => a.IsActive)
                .ToDictionaryAsync(a => a.Slot, a => a.Name);

            // 3. Şart: Aktif pigment var mı + Tartım OK sinyali geldi mi?
            // Pigment1 kontrolü
            var pigment1Aktif = GetBitValue(snapshot, "H31.10");
            var pigment1TartimOk = GetBitValue(snapshot, "H31.3");
            var pigment1Kg = GetWordValue(snapshot, "DM308");
            
            OnFlowEvent?.Invoke($"🎨 Pigment1: Aktif={pigment1Aktif}, TartimOk={pigment1TartimOk}, RawKg={pigment1Kg:F2}, AfterDiv100={pigment1Kg/100.0:F2}");
            
            if (pigment1Aktif && pigment1TartimOk) // Edge detection kaldırıldı
            {
                var kg = GetWordValue(snapshot, "DM308");
                if (kg > 0.1) // Eşik kontrolü
                {
                    batch.Pigment1Kg = kg / 100.0; // KG değerini 100.0 ile böl
                    // TotalPigmentKg'yi yeniden hesapla (toplam yerine)
                    batch.TotalPigmentKg = batch.Pigment1Kg + batch.Pigment2Kg + batch.Pigment3Kg + batch.Pigment4Kg;
                    
                    // Alias ismi kullan
                    var displayName = pigmentAliases.TryGetValue(1, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : "Pigment1";
                    
                    OnFlowEvent?.Invoke($"🎨 {displayName} kaydedildi: {batch.Pigment1Kg:F2}kg (Batch {batch.Id})");
                }
            }

            // Pigment2 kontrolü
            var pigment2Aktif = GetBitValue(snapshot, "H32.10");
            var pigment2TartimOk = GetBitValue(snapshot, "H32.3");
            var pigment2Kg = GetWordValue(snapshot, "DM310");
            
            OnFlowEvent?.Invoke($"🎨 Pigment2: Aktif={pigment2Aktif}, TartimOk={pigment2TartimOk}, RawKg={pigment2Kg:F2}, AfterDiv100={pigment2Kg/100.0:F2}");
            
            if (pigment2Aktif && pigment2TartimOk) // Edge detection kaldırıldı
            {
                var kg = GetWordValue(snapshot, "DM310");
                if (kg > 0.1) // Eşik kontrolü
                {
                    batch.Pigment2Kg = kg / 100.0; // KG değerini 100.0 ile böl
                    // TotalPigmentKg'yi yeniden hesapla (toplam yerine)
                    batch.TotalPigmentKg = batch.Pigment1Kg + batch.Pigment2Kg + batch.Pigment3Kg + batch.Pigment4Kg;
                    
                    // Alias ismi kullan
                    var displayName = pigmentAliases.TryGetValue(2, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : "Pigment2";
                    
                    OnFlowEvent?.Invoke($"🎨 {displayName} kaydedildi: {batch.Pigment2Kg:F2}kg (Batch {batch.Id})");
                }
            }

            // Pigment3 kontrolü
            var pigment3Aktif = GetBitValue(snapshot, "H33.10");
            var pigment3TartimOk = GetBitValue(snapshot, "H33.3");
            var pigment3Kg = GetWordValue(snapshot, "DM312");
            
            OnFlowEvent?.Invoke($"🎨 Pigment3: Aktif={pigment3Aktif}, TartimOk={pigment3TartimOk}, RawKg={pigment3Kg:F2}, AfterDiv100={pigment3Kg/100.0:F2}");
            
            if (pigment3Aktif && pigment3TartimOk) // Edge detection kaldırıldı
            {
                var kg = GetWordValue(snapshot, "DM312");
                if (kg > 0.1) // Eşik kontrolü
                {
                    batch.Pigment3Kg = kg / 100.0; // KG değerini 100.0 ile böl
                    // TotalPigmentKg'yi yeniden hesapla (toplam yerine)
                    batch.TotalPigmentKg = batch.Pigment1Kg + batch.Pigment2Kg + batch.Pigment3Kg + batch.Pigment4Kg;
                    
                    // Alias ismi kullan
                    var displayName = pigmentAliases.TryGetValue(3, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : "Pigment3";
                    
                    OnFlowEvent?.Invoke($"🎨 {displayName} kaydedildi: {batch.Pigment3Kg:F2}kg (Batch {batch.Id})");
                }
            }

            // Pigment4 kontrolü
            var pigment4Aktif = GetBitValue(snapshot, "H34.10");
            var pigment4TartimOk = GetBitValue(snapshot, "H34.3");
            var pigment4Kg = GetWordValue(snapshot, "DM314");
            
            OnFlowEvent?.Invoke($"🎨 Pigment4: Aktif={pigment4Aktif}, TartimOk={pigment4TartimOk}, RawKg={pigment4Kg:F2}, AfterDiv100={pigment4Kg/100.0:F2}");
            
            if (pigment4Aktif && pigment4TartimOk) // Edge detection kaldırıldı
            {
                var kg = GetWordValue(snapshot, "DM314");
                if (kg > 0.1) // Eşik kontrolü
                {
                    batch.Pigment4Kg = kg / 100.0; // KG değerini 100.0 ile böl
                    // TotalPigmentKg'yi yeniden hesapla (toplam yerine)
                    batch.TotalPigmentKg = batch.Pigment1Kg + batch.Pigment2Kg + batch.Pigment3Kg + batch.Pigment4Kg;
                    
                    // Alias ismi kullan
                    var displayName = pigmentAliases.TryGetValue(4, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                        ? aliasName
                        : "Pigment4";
                    
                    OnFlowEvent?.Invoke($"🎨 {displayName} kaydedildi: {batch.Pigment4Kg:F2}kg (Batch {batch.Id})");
                }
            }

            // Değişiklik varsa kaydet
            if (batch.Pigment1Kg > 0 || batch.Pigment2Kg > 0 || batch.Pigment3Kg > 0 || batch.Pigment4Kg > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
