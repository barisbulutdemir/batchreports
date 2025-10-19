using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// Çimento tüketimi yönetimi servisi
    /// </summary>
    public class CementConsumptionService
    {
        private readonly ProductionDbContext _context;

        public CementConsumptionService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Mixer1 için çimento tüketimi kaydet
        /// </summary>
        public async Task RecordMixer1ConsumptionAsync(ConcreteBatch batch)
        {
            try
            {
                if (batch.Cements == null || !batch.Cements.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[CementConsumptionService] Mixer1 batch'te çimento bulunamadı");
                    return;
                }

                foreach (var cement in batch.Cements)
                {
                    if (cement.WeightKg <= 0) continue;

                    // Bu batch için bu slot'ta zaten tüketim kaydı var mı kontrol et
                    // Önce siloyu bul
                    var silo = await FindSiloBySlotAsync(cement.Slot);
                    if (silo == null)
                    {
                        // Fallback: Çimento türüne göre eşle
                        silo = await FindSiloByCementTypeAsync(cement.CementType);
                    }
                    if (silo == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer1 için {cement.CementType} silosu bulunamadı");
                        continue;
                    }

                    var existingConsumption = await _context.CementConsumptions
                        .FirstOrDefaultAsync(c => c.BatchId == batch.Id && c.MixerId == 1 && c.SiloId == silo.Id);
                    
                    if (existingConsumption != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer1 - Batch {batch.Id} için Slot {cement.Slot} zaten tüketim kaydı var ({existingConsumption.ConsumedAmount}kg) - tekrar kayıt atlandı");
                        continue;
                    }


                    // Tüketim kaydı oluştur
                    var consumption = new CementConsumption
                    {
                        SiloId = silo.Id,
                        ConsumedAmount = cement.WeightKg,
                        ConsumptionType = "Mixer1 Production",
                        BatchId = batch.Id,
                        ConsumedAt = batch.OccurredAt,
                        MixerId = 1,
                        RemainingAmount = silo.CurrentAmount - cement.WeightKg,
                        Notes = $"Mixer1 üretimi - Batch: {batch.Id}"
                    };

                    _context.CementConsumptions.Add(consumption);

                    // Silo seviyesini güncelle
                    silo.CurrentAmount = Math.Max(0, silo.CurrentAmount - cement.WeightKg);

                    System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer1 çimento tüketimi: {cement.CementType} - {cement.WeightKg}kg");
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer1 çimento tüketimi başarıyla kaydedildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer1 çimento tüketimi hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Inner Exception: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Mixer2 için çimento tüketimi kaydet
        /// </summary>
        public async Task RecordMixer2ConsumptionAsync(ConcreteBatch2 batch)
        {
            try
            {
                if (batch.Cements == null || !batch.Cements.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[CementConsumptionService] Mixer2 batch'te çimento bulunamadı");
                    return;
                }

                foreach (var cement in batch.Cements)
                {
                    if (cement.WeightKg <= 0) continue;

                    // Bu batch için bu slot'ta zaten tüketim kaydı var mı kontrol et
                    // Önce siloyu bul
                    var silo = await FindSiloBySlotAsync(cement.Slot);
                    if (silo == null)
                    {
                        // Fallback: Çimento türüne göre eşle
                        silo = await FindSiloByCementTypeAsync(cement.CementType);
                    }
                    if (silo == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer2 için {cement.CementType} silosu bulunamadı");
                        continue;
                    }

                    var existingConsumption = await _context.CementConsumptions
                        .FirstOrDefaultAsync(c => c.BatchId == batch.Id && c.MixerId == 2 && c.SiloId == silo.Id);
                    
                    if (existingConsumption != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer2 - Batch {batch.Id} için Slot {cement.Slot} zaten tüketim kaydı var ({existingConsumption.ConsumedAmount}kg) - tekrar kayıt atlandı");
                        continue;
                    }

                    // Tüketim kaydı oluştur
                    var consumption = new CementConsumption
                    {
                        SiloId = silo.Id,
                        ConsumedAmount = cement.WeightKg,
                        ConsumptionType = "Mixer2 Production",
                        BatchId = batch.Id,
                        ConsumedAt = batch.OccurredAt,
                        MixerId = 2, // Mixer2 için MixerId = 2
                        RemainingAmount = silo.CurrentAmount - cement.WeightKg,
                        Notes = $"Mixer2 üretimi - Batch: {batch.Id}"
                    };

                    _context.CementConsumptions.Add(consumption);

                    // Silo seviyesini güncelle (Ortak silo kullanımı)
                    silo.CurrentAmount = Math.Max(0, silo.CurrentAmount - cement.WeightKg);

                    System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer2 çimento tüketimi: {cement.CementType} - {cement.WeightKg}kg");
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Mixer2 çimento tüketimi hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Çimento türüne göre silo bul - BASİT EŞLEŞTİRME: Silo 1 = cimento1, Silo 2 = cimento2, Silo 3 = cimento3
        /// </summary>
        private async Task<CementSilo?> FindSiloByCementTypeAsync(string cementType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Çimento türü aranıyor: '{cementType}'");
                
                // Tüm siloları listele
                var allSilos = await _context.CementSilos.ToListAsync();
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Toplam silo sayısı: {allSilos.Count}");
                
                if (!allSilos.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[CementConsumptionService] ❌ HİÇ SİLO BULUNAMADI!");
                    return null;
                }

                // Çimento türünü normalize et
                var normalizedType = cementType.ToLower().Trim();
                
                // BASİT EŞLEŞTİRME: Standard -> Silo 1, Beyaz -> Silo 2, Siyah -> Silo 3
                CementSilo? targetSilo = normalizedType switch
                {
                    "standard" => allSilos.FirstOrDefault(s => s.SiloNumber == 1),
                    "beyaz" => allSilos.FirstOrDefault(s => s.SiloNumber == 2),
                    "siyah" => allSilos.FirstOrDefault(s => s.SiloNumber == 3),
                    // Eski format desteği
                    "cimento1" or "cement1" => allSilos.FirstOrDefault(s => s.SiloNumber == 1),
                    "cimento2" or "cement2" => allSilos.FirstOrDefault(s => s.SiloNumber == 2),
                    "cimento3" or "cement3" => allSilos.FirstOrDefault(s => s.SiloNumber == 3),
                    _ => null
                };

                if (targetSilo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] ✅ EŞLEŞME BULUNDU: '{cementType}' -> Silo {targetSilo.SiloNumber} ({targetSilo.CementType})");
                    return targetSilo;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] ❌ '{cementType}' için uygun silo bulunamadı. Desteklenen türler: standard, beyaz, siyah, cimento1, cimento2, cimento3");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] ❌ Silo bulma hatası: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Slot numarasına göre silo bul (1->Silo1, 2->Silo2, 3->Silo3)
        /// </summary>
        private async Task<CementSilo?> FindSiloBySlotAsync(short slot)
        {
            try
            {
                var silos = await _context.CementSilos
                    .Where(s => s.SiloNumber == slot)
                    .ToListAsync();
                return silos.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Silo seviyelerini yenile - Mevcut seviyeleri koruyarak sadece tüketim ve doldurma verilerine göre güncelle
        /// </summary>
        public async Task RefreshSiloLevelsAsync()
        {
            try
            {
                var silos = await _context.CementSilos.ToListAsync();
                foreach (var silo in silos)
                {
                    // Mevcut seviyeyi koru - sadece veritabanından gelen değerleri kullan
                    // RefreshSiloLevelsAsync artık mevcut seviyeleri değiştirmiyor
                    System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Silo {silo.SiloNumber} mevcut seviye korundu: {silo.CurrentAmount} kg");
                }

                // Sadece değişiklik yapılmadığı için SaveChanges çağrılmıyor
                System.Diagnostics.Debug.WriteLine("[CementConsumptionService] Silo seviyeleri korundu - değişiklik yapılmadı");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementConsumptionService] Silo seviyeleri yenileme hatası: {ex.Message}");
            }
        }
    }
}
