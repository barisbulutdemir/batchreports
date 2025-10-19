using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// Mixer2 batch işlemleri servisi
    /// </summary>
    public class ConcreteBatch2Service
    {
        private readonly ProductionDbContext _context;
        private readonly CementConsumptionService _cementService;

        public ConcreteBatch2Service(ProductionDbContext context)
        {
            _context = context;
            _cementService = new CementConsumptionService(context);
        }

        /// <summary>
        /// Yeni Mixer2 batch oluştur
        /// </summary>
        public async Task<ConcreteBatch2> CreateBatchAsync(string operatorName, string? recipeCode = null)
        {
            // PlantCode'u operatör adına göre belirle
            string plantCode = operatorName == "M1_Operator" ? "MIXER1" : "MIXER2";
            
            var batch = new ConcreteBatch2
            {
                OccurredAt = DateTime.UtcNow,
                OperatorName = operatorName,
                RecipeCode = recipeCode ?? "DEFAULT",
                IsSimulated = true,
                PlantCode = plantCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.ConcreteBatch2s.Add(batch);
            await _context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Yeni batch oluşturuldu: {batch.Id}");
            return batch;
        }

        /// <summary>
        /// Batch'e agrega ekle
        /// </summary>
        public async Task AddAggregateAsync(int batchId, short slot, string name, double weightKg)
        {
            var aggregate = new ConcreteBatch2Aggregate
            {
                BatchId = batchId,
                Slot = slot,
                Name = name,
                WeightKg = weightKg
            };

            _context.ConcreteBatch2Aggregates.Add(aggregate);
            
            // Batch toplam ağırlığını güncelle
            await UpdateBatchTotalsAsync(batchId);
            
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Agrega eklendi: Batch {batchId}, {name}, {weightKg}kg");
        }

        /// <summary>
        /// Batch'e çimento ekle
        /// </summary>
        public async Task AddCementAsync(int batchId, short slot, string cementType, double weightKg)
        {
            var cement = new ConcreteBatch2Cement
            {
                BatchId = batchId,
                Slot = slot,
                CementType = cementType,
                WeightKg = weightKg
            };

            _context.ConcreteBatch2Cements.Add(cement);
            
            // Batch toplam ağırlığını güncelle
            await UpdateBatchTotalsAsync(batchId);
            
            await _context.SaveChangesAsync();
            
            // Çimento tüketimini kaydet
            var batch = await _context.ConcreteBatch2s.FirstAsync(b => b.Id == batchId);
            await _cementService.RecordMixer2ConsumptionAsync(batch);
            
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Çimento eklendi: Batch {batchId}, {cementType}, {weightKg}kg");
        }

        /// <summary>
        /// Batch'e katkı ekle
        /// </summary>
        public async Task AddAdmixtureAsync(int batchId, short slot, string name, double chemicalKg, double waterKg)
        {
            var admixture = new ConcreteBatch2Admixture
            {
                BatchId = batchId,
                Slot = slot,
                Name = name,
                ChemicalKg = chemicalKg,
                WaterKg = waterKg
            };

            _context.ConcreteBatch2Admixtures.Add(admixture);
            
            // Batch toplam ağırlığını güncelle
            await UpdateBatchTotalsAsync(batchId);
            
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Katkı eklendi: Batch {batchId}, {name}, {chemicalKg}+{waterKg}kg");
        }

        /// <summary>
        /// Batch su miktarlarını güncelle
        /// </summary>
        public async Task UpdateWaterAmountsAsync(int batchId, double loadcellWaterKg, double pulseWaterKg)
        {
            var batch = await _context.ConcreteBatch2s.FirstAsync(b => b.Id == batchId);
            
            batch.LoadcellWaterKg = loadcellWaterKg;
            batch.PulseWaterKg = pulseWaterKg;
            batch.EffectiveWaterKg = loadcellWaterKg + pulseWaterKg;
            
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Su miktarları güncellendi: Batch {batchId}");
        }

        /// <summary>
        /// Batch pigment miktarlarını güncelle
        /// </summary>
        public async Task UpdatePigmentAmountsAsync(int batchId, double pigment1Kg, double pigment2Kg, 
            double pigment3Kg, double pigment4Kg)
        {
            var batch = await _context.ConcreteBatch2s.FirstAsync(b => b.Id == batchId);
            
            batch.Pigment1Kg = pigment1Kg;
            batch.Pigment2Kg = pigment2Kg;
            batch.Pigment3Kg = pigment3Kg;
            batch.Pigment4Kg = pigment4Kg;
            batch.TotalPigmentKg = pigment1Kg + pigment2Kg + pigment3Kg + pigment4Kg;
            
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Pigment miktarları güncellendi: Batch {batchId}");
        }

        /// <summary>
        /// Batch toplam değerlerini güncelle
        /// </summary>
        private async Task UpdateBatchTotalsAsync(int batchId)
        {
            var batch = await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .FirstAsync(b => b.Id == batchId);

            batch.TotalAggregateKg = batch.Aggregates?.Sum(a => a.WeightKg) ?? 0;
            batch.TotalCementKg = batch.Cements?.Sum(c => c.WeightKg) ?? 0;
            batch.TotalAdmixtureKg = batch.Admixtures?.Sum(a => a.ChemicalKg + a.WaterKg) ?? 0;
            
            // Su-çimento oranını hesapla
            if (batch.TotalCementKg > 0)
            {
                batch.WaterCementRatio = batch.EffectiveWaterKg / batch.TotalCementKg;
            }
        }

        /// <summary>
        /// Batch tamamla
        /// </summary>
        public async Task CompleteBatchAsync(int batchId, double? moisturePercent = null)
        {
            var batch = await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .FirstAsync(b => b.Id == batchId);

            if (moisturePercent.HasValue)
            {
                batch.MoisturePercent = moisturePercent.Value;
            }

            // Final toplam hesaplamaları
            await UpdateBatchTotalsAsync(batchId);
            
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Batch tamamlandı: {batchId}");
        }

        /// <summary>
        /// Son batch'leri getir
        /// </summary>
        public async Task<List<ConcreteBatch2>> GetRecentBatchesAsync(int count = 10)
        {
            return await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .OrderByDescending(b => b.OccurredAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Belirli status'taki batch'leri getir
        /// </summary>
        public async Task<List<ConcreteBatch2>> GetBatchesByStatusAsync(string status)
        {
            return await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .Where(b => b.Status == status)
                .OrderBy(b => b.OccurredAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Batch status'unu güncelle
        /// </summary>
        public async Task UpdateBatchStatusAsync(int batchId, string newStatus)
        {
            var batch = await _context.ConcreteBatch2s.FindAsync(batchId);
            if (batch != null)
            {
                var oldStatus = batch.Status;
                batch.Status = newStatus;
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Batch {batchId} status güncellendi: {oldStatus} -> {newStatus}");
            }
        }

        /// <summary>
        /// Aktif batch'leri getir (tamamlanmamış)
        /// </summary>
        public async Task<List<ConcreteBatch2>> GetActiveBatchesAsync()
        {
            return await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .Where(b => b.Status != "Tamamlandı")
                .OrderBy(b => b.OccurredAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Batch'e su ekle
        /// </summary>
        public async Task AddWaterAsync(int batchId, short slot, string name, double weightKg)
        {
            // Su için özel bir model yok, LoadcellWaterKg ve PulseWaterKg alanlarını kullan
            var batch = await _context.ConcreteBatch2s.FindAsync(batchId);
            if (batch != null)
            {
                if (slot == 1) // Loadcell su
                {
                    batch.LoadcellWaterKg = weightKg;
                }
                else if (slot == 2) // Pulse su
                {
                    batch.PulseWaterKg = weightKg;
                }
                
                // EffectiveWaterKg'yi güncelle
                batch.EffectiveWaterKg = batch.LoadcellWaterKg + batch.PulseWaterKg;
                
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Batch {batchId}'e su eklendi: {name} - {weightKg}kg");
            }
        }

        /// <summary>
        /// Batch'i ve tüm ilişkili verilerini sil
        /// </summary>
        public async Task DeleteBatchAsync(int batchId)
        {
            var batch = await _context.ConcreteBatch2s
                .Include(b => b.Aggregates)
                .Include(b => b.Cements)
                .Include(b => b.Admixtures)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch != null)
            {
                // İlişkili verileri sil (Cascade delete ile otomatik silinir ama emin olmak için)
                _context.ConcreteBatch2Aggregates.RemoveRange(batch.Aggregates);
                _context.ConcreteBatch2Cements.RemoveRange(batch.Cements);
                _context.ConcreteBatch2Admixtures.RemoveRange(batch.Admixtures);
                
                // Batch'i sil
                _context.ConcreteBatch2s.Remove(batch);
                
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatch2Service] Batch {batchId} ve ilişkili verileri silindi");
            }
            else
            {
                throw new ArgumentException($"Batch {batchId} bulunamadı");
            }
        }
    }
}
