using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// Çimento silo yönetimi servisi
    /// </summary>
    public class CementSiloService
    {
        /// <summary>
        /// Tüm çimento silolarını getir
        /// </summary>
        /// <returns>Çimento silo listesi</returns>
        public async Task<List<CementSilo>> GetAllSilosAsync()
        {
            using var context = new ProductionDbContext();
            return await context.CementSilos.ToListAsync();
        }

        /// <summary>
        /// ID'ye göre çimento silosu getir
        /// </summary>
        /// <param name="id">Silo ID</param>
        /// <returns>Çimento silo</returns>
        public async Task<CementSilo?> GetSiloByIdAsync(int id)
        {
            using var context = new ProductionDbContext();
            return await context.CementSilos.FindAsync(id);
        }

        /// <summary>
        /// Çimento silosu ekle
        /// </summary>
        /// <param name="silo">Çimento silo</param>
        /// <returns>Eklenen silo</returns>
        public async Task<CementSilo> AddSiloAsync(CementSilo silo)
        {
            try
            {
                using var context = new ProductionDbContext();
                context.CementSilos.Add(silo);
                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[CementSiloService] Çimento silosu başarıyla eklendi: Silo {silo.SiloNumber}, {silo.CementType}");
                return silo;
            }
            catch (Exception ex)
            {
                var errorMessage = $"[CementSiloService] Çimento silosu ekleme hatası: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $"\nInner Inner Exception: {ex.InnerException.InnerException.Message}";
                    }
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"[CementSiloService] Stack Trace: {ex.StackTrace}");
                
                // MessageBox ile detaylı hata göster
                var fullErrorMessage = $"Çimento silosu ekleme hatası:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    fullErrorMessage += $"\n\nDetay: {ex.InnerException.Message}";
                    if (ex.InnerException.InnerException != null)
                    {
                        fullErrorMessage += $"\n\nDaha detaylı: {ex.InnerException.InnerException.Message}";
                    }
                }
                
                System.Windows.MessageBox.Show(fullErrorMessage, "Çimento Silo Hatası", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Çimento silosu güncelle
        /// </summary>
        /// <param name="silo">Çimento silo</param>
        /// <returns>Güncellenen silo</returns>
        public async Task<CementSilo> UpdateSiloAsync(CementSilo silo)
        {
            using var context = new ProductionDbContext();
            context.CementSilos.Update(silo);
            await context.SaveChangesAsync();
            return silo;
        }

        /// <summary>
        /// Çimento silosu sil
        /// </summary>
        /// <param name="id">Silo ID</param>
        /// <returns>Silme işlemi başarılı mı</returns>
        public async Task<bool> DeleteSiloAsync(int id)
        {
            using var context = new ProductionDbContext();
            var silo = await context.CementSilos.FindAsync(id);
            if (silo == null) return false;

            context.CementSilos.Remove(silo);
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Silo doldurma işlemi
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <param name="amount">Doldurma miktarı</param>
        /// <param name="operatorName">Operatör adı</param>
        /// <param name="shipmentNumber">Sevkiyat numarası</param>
        /// <param name="supplier">Tedarikçi</param>
        /// <param name="notes">Notlar</param>
        /// <returns>Doldurma işlemi başarılı mı</returns>
        public async Task<bool> RefillSiloAsync(int siloId, decimal amount, string operatorName, string? shipmentNumber = null, string? supplier = null, string? notes = null)
        {
            using var context = new ProductionDbContext();
            var silo = await context.CementSilos.FindAsync(siloId);
            if (silo == null) return false;

            // Kapasite kontrolü - fazla çimento uyarısı
            var newAmount = silo.CurrentAmount + (double)amount;
            if (newAmount > silo.Capacity)
            {
                // Kapasite aşımı durumunda işlemi durdur
                System.Diagnostics.Debug.WriteLine($"[CementSiloService] Silo {silo.SiloNumber} kapasite aşımı: {newAmount} > {silo.Capacity}");
                return false;
            }

            // Silo seviyesini güncelle
            silo.CurrentAmount = newAmount;
            silo.LastRefillDate = DateTime.Now;

            // Doldurma kaydı ekle
            var refill = new CementRefill
            {
                SiloId = siloId,
                AddedAmount = (double)amount,
                OperatorName = operatorName,
                RefilledAt = DateTime.Now,
                ShipmentNumber = shipmentNumber,
                Supplier = supplier,
                Notes = notes,
                PreviousAmount = silo.CurrentAmount - (double)amount,
                NewAmount = silo.CurrentAmount
            };

            context.CementRefills.Add(refill);
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Silo tüketim kaydı ekle
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <param name="amount">Tüketim miktarı</param>
        /// <param name="batchId">Batch ID</param>
        /// <returns>Tüketim kaydı başarılı mı</returns>
        public async Task<bool> AddConsumptionAsync(int siloId, decimal amount, int? batchId = null)
        {
            using var context = new ProductionDbContext();
            var silo = await context.CementSilos.FindAsync(siloId);
            if (silo == null) return false;

            // Silo seviyesini güncelle
            silo.CurrentAmount = Math.Max(0, silo.CurrentAmount - (double)amount);

            // Tüketim kaydı ekle
            var consumption = new CementConsumption
            {
                SiloId = siloId,
                ConsumedAmount = (double)amount,
                ConsumedAt = DateTime.Now,
                BatchId = batchId
            };

            context.CementConsumptions.Add(consumption);
            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Silo detaylarını getir (tüketim ve doldurma geçmişi ile)
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <returns>Silo detayları</returns>
        public async Task<CementSilo?> GetSiloDetailsAsync(int siloId)
        {
            using var context = new ProductionDbContext();
            return await context.CementSilos
                .FirstOrDefaultAsync(s => s.Id == siloId);
        }

        /// <summary>
        /// Silo istatistiklerini getir
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Silo istatistikleri</returns>
        public async Task<object> GetSiloStatisticsAsync(int siloId, DateTime startDate, DateTime endDate)
        {
            using var context = new ProductionDbContext();
            
            var refills = await context.CementRefills
                .Where(r => r.SiloId == siloId && r.RefillDate >= startDate && r.RefillDate <= endDate)
                .ToListAsync();

            var consumptions = await context.CementConsumptions
                .Where(c => c.SiloId == siloId && c.ConsumptionDate >= startDate && c.ConsumptionDate <= endDate)
                .ToListAsync();

            return new
            {
                TotalRefilled = refills.Sum(r => r.Amount),
                TotalConsumed = consumptions.Sum(c => c.Amount),
                RefillCount = refills.Count,
                ConsumptionCount = consumptions.Count,
                AverageRefillAmount = refills.Any() ? refills.Average(r => r.Amount) : 0,
                AverageConsumptionAmount = consumptions.Any() ? consumptions.Average(c => c.Amount) : 0
            };
        }

        /// <summary>
        /// Siloları başlat (InitializeSilosAsync için alias)
        /// </summary>
        public async Task InitializeSilosAsync()
        {
            using var context = new ProductionDbContext();
            
            // ÖNCE TÜM SİLOLARI VE TÜKETİMLERİ TEMİZLE
            var allConsumptions = await context.CementConsumptions.ToListAsync();
            var allRefills = await context.CementRefills.ToListAsync();
            var allSilos = await context.CementSilos.ToListAsync();
            
            context.CementConsumptions.RemoveRange(allConsumptions);
            context.CementRefills.RemoveRange(allRefills);
            context.CementSilos.RemoveRange(allSilos);
            await context.SaveChangesAsync();
            
            // YENİ SİLOLARI OLUŞTUR
            var defaultSilos = new List<CementSilo>
            {
                new CementSilo { SiloNumber = 1, CementType = "Standard", CurrentAmount = 1000, Capacity = 10000, MinLevel = 1000, IsActive = true },
                new CementSilo { SiloNumber = 2, CementType = "Beyaz", CurrentAmount = 1000, Capacity = 10000, MinLevel = 1000, IsActive = true },
                new CementSilo { SiloNumber = 3, CementType = "Siyah", CurrentAmount = 1000, Capacity = 10000, MinLevel = 1000, IsActive = true }
            };

            context.CementSilos.AddRange(defaultSilos);
            await context.SaveChangesAsync();
            
            // TEST TÜKETİMİ EKLE (Silo 1 için)
            var silo1 = defaultSilos[0];
            var testConsumption = new CementConsumption
            {
                SiloId = silo1.Id,
                ConsumedAmount = 50.0,
                ConsumedAt = DateTime.UtcNow.AddHours(-2),
                BatchId = 999, // Test batch ID
                MixerId = 1,
                RemainingAmount = silo1.CurrentAmount - 50.0,
                ConsumptionType = "Test Consumption",
                Notes = "Test tüketimi - silo sistemini test etmek için"
            };
            
            context.CementConsumptions.Add(testConsumption);
            
            // Silo 1'in seviyesini güncelle
            silo1.CurrentAmount = 950.0;
            context.CementSilos.Update(silo1);
            
            // MIXER1 VE MIXER2 TEST BATCH'LARI EKLE
            var testBatch1 = new ConcreteBatch
            {
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                Status = "Mixerde",
                TotalCementKg = 100.0,
                OperatorName = "Test Operator"
            };
            context.ConcreteBatches.Add(testBatch1);
            
            var testBatch2 = new ConcreteBatch2
            {
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                Status = "Mixerde",
                TotalCementKg = 150.0,
                OperatorName = "Test Operator"
            };
            context.ConcreteBatch2s.Add(testBatch2);
            
            await context.SaveChangesAsync();
            
            // Test batch'larına çimento kayıtları ekle
            var cement1 = new ConcreteBatchCement
            {
                BatchId = testBatch1.Id,
                Slot = 1,
                CementType = "Standard",
                WeightKg = 100.0
            };
            context.ConcreteBatchCements.Add(cement1);
            
            var cement2 = new ConcreteBatch2Cement
            {
                BatchId = testBatch2.Id,
                Slot = 1,
                CementType = "Beyaz",
                WeightKg = 150.0
            };
            context.ConcreteBatch2Cements.Add(cement2);
            
            await context.SaveChangesAsync();
            
            // Test batch'larından silo tüketimi kaydet
            var consumptionService = new CementConsumptionService(context);
            await consumptionService.RecordMixer1ConsumptionAsync(testBatch1);
            await consumptionService.RecordMixer2ConsumptionAsync(testBatch2);
            
            System.Diagnostics.Debug.WriteLine("[CementSiloService] Silolar tamamen temizlendi, yeniden oluşturuldu ve test batch'ları ile tüketimler eklendi");
        }

        /// <summary>
        /// Son tüketimleri getir
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <param name="count">Kayıt sayısı</param>
        /// <returns>Son tüketimler</returns>
        public async Task<List<CementConsumption>> GetRecentConsumptionsAsync(int siloId, int count = 10)
        {
            using var context = new ProductionDbContext();
            return await context.CementConsumptions
                .Where(c => c.SiloId == siloId)
                .OrderByDescending(c => c.ConsumptionDate)
                .Take(count)
                .ToListAsync();
        }

        // Overload: tüm silolar için son tüketimler
        public async Task<List<CementConsumption>> GetRecentConsumptionsAsync(int count = 10)
        {
            using var context = new ProductionDbContext();
            return await context.CementConsumptions
                .OrderByDescending(c => c.ConsumptionDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Son doldurmaları getir
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <param name="count">Kayıt sayısı</param>
        /// <returns>Son doldurmalar</returns>
        public async Task<List<CementRefill>> GetRecentRefillsAsync(int siloId, int count = 10)
        {
            using var context = new ProductionDbContext();
            return await context.CementRefills
                .Where(r => r.SiloId == siloId)
                .OrderByDescending(r => r.RefillDate)
                .Take(count)
                .ToListAsync();
        }

        // Overload: tüm silolar için son dolumlar
        public async Task<List<CementRefill>> GetRecentRefillsAsync(int count = 10)
        {
            using var context = new ProductionDbContext();
            return await context.CementRefills
                .OrderByDescending(r => r.RefillDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Silo geçmişini temizle (yalnızca geçmiş kayıtları siler, silo seviyelerine dokunmaz)
        /// </summary>
        /// <param name="siloId">Opsiyonel. Belirtilirse sadece bu silonun geçmişi silinir</param>
        /// <returns>Silinen kayıt sayıları</returns>
        public async Task<(int deletedConsumptions, int deletedRefills)> ClearHistoryAsync(int? siloId = null)
        {
            using var context = new ProductionDbContext();

            // Seçim
            var consumptionsQuery = context.CementConsumptions.AsQueryable();
            var refillsQuery = context.CementRefills.AsQueryable();
            if (siloId.HasValue)
            {
                consumptionsQuery = consumptionsQuery.Where(c => c.SiloId == siloId.Value);
                refillsQuery = refillsQuery.Where(r => r.SiloId == siloId.Value);
            }

            // Silinecekleri al (önce sayıları ölçmek için)
            var consumptions = await consumptionsQuery.ToListAsync();
            var refills = await refillsQuery.ToListAsync();

            context.CementConsumptions.RemoveRange(consumptions);
            context.CementRefills.RemoveRange(refills);
            await context.SaveChangesAsync();

            return (consumptions.Count, refills.Count);
        }

        /// <summary>
        /// Silo durumunu kontrol et
        /// </summary>
        /// <param name="siloId">Silo ID</param>
        /// <returns>Silo durumu</returns>
        public async Task<string> CheckSiloStatusAsync(int siloId)
        {
            var silo = await GetSiloByIdAsync(siloId);
            return silo?.Status ?? "Bilinmiyor";
        }
    }
}