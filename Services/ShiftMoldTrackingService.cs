using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// Vardiya içinde kalıp takip servisi
    /// Vardiya başlangıcında aktif kalıbı kaydeder ve kalıp değişikliklerini takip eder
    /// </summary>
    public class ShiftMoldTrackingService
    {
        private readonly ProductionDbContext _context;

        public ShiftMoldTrackingService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Vardiya başlangıcında aktif kalıbı kaydet
        /// </summary>
        public async Task<int> StartShiftMoldTracking(int shiftId, string operatorName)
        {
            try
            {
                // Aktif kalıbı bul
                var activeMold = await _context.Molds
                    .FirstOrDefaultAsync(m => m.IsActive);

                if (activeMold == null)
                {
                    DetailedLogger.LogWarning($"Vardiya başlatıldı ama aktif kalıp bulunamadı - ShiftId: {shiftId}");
                    return 0;
                }

                // Vardiya kalıp kaydı oluştur
                var shiftMoldRecord = new ShiftMoldRecord
                {
                    ShiftId = shiftId,
                    MoldId = activeMold.Id,
                    MoldName = activeMold.Name,
                    OperatorName = operatorName,
                    StartTime = DateTime.UtcNow,
                    StartProductionCount = activeMold.TotalPrints, // Başlangıç üretim sayısı
                    IsActive = true
                };

                _context.ShiftMoldRecords.Add(shiftMoldRecord);
                await _context.SaveChangesAsync();

                DetailedLogger.LogInfo($"Vardiya kalıp takibi başlatıldı - ShiftId: {shiftId}, Kalıp: {activeMold.Name}, KayıtId: {shiftMoldRecord.Id}");

                return shiftMoldRecord.Id;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Vardiya kalıp takibi başlatılırken hata: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Kalıp değişikliğini kaydet
        /// </summary>
        public async Task<bool> RecordMoldChange(int shiftId, int newMoldId, string operatorName)
        {
            try
            {
                // Önceki aktif kaydı bitir
                var previousRecord = await _context.ShiftMoldRecords
                    .FirstOrDefaultAsync(r => r.ShiftId == shiftId && r.IsActive);

                if (previousRecord != null)
                {
                    // Önceki kalıbın son üretim sayısını al
                    var previousMold = await _context.Molds.FindAsync(previousRecord.MoldId);
                    if (previousMold != null)
                    {
                        previousRecord.EndProductionCount = previousMold.TotalPrints;
                        previousRecord.ProductionCount = previousRecord.EndProductionCount - previousRecord.StartProductionCount;
                    }

                    previousRecord.EndTime = DateTime.UtcNow;
                    previousRecord.IsActive = false;

                    DetailedLogger.LogInfo($"Önceki kalıp kaydı bitirildi - Kalıp: {previousRecord.MoldName}, Üretim: {previousRecord.ProductionCount}");
                }

                // Yeni kalıp kaydı oluştur
                var newMold = await _context.Molds.FindAsync(newMoldId);
                if (newMold == null)
                {
                    DetailedLogger.LogError($"Kalıp bulunamadı - MoldId: {newMoldId}");
                    return false;
                }

                var newRecord = new ShiftMoldRecord
                {
                    ShiftId = shiftId,
                    MoldId = newMoldId,
                    MoldName = newMold.Name,
                    OperatorName = operatorName,
                    StartTime = DateTime.UtcNow,
                    StartProductionCount = newMold.TotalPrints,
                    IsActive = true
                };

                _context.ShiftMoldRecords.Add(newRecord);
                await _context.SaveChangesAsync();

                DetailedLogger.LogInfo($"Yeni kalıp kaydı oluşturuldu - Kalıp: {newMold.Name}, KayıtId: {newRecord.Id}");

                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Kalıp değişikliği kaydedilirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vardiya bitiminde tüm kalıp kayıtlarını tamamla
        /// </summary>
        public async Task<List<ShiftMoldRecord>> CompleteShiftMoldTracking(int shiftId)
        {
            try
            {
                var activeRecords = await _context.ShiftMoldRecords
                    .Where(r => r.ShiftId == shiftId && r.IsActive)
                    .ToListAsync();

                foreach (var record in activeRecords)
                {
                    // Son üretim sayısını al
                    var mold = await _context.Molds.FindAsync(record.MoldId);
                    if (mold != null)
                    {
                        record.EndProductionCount = mold.TotalPrints;
                        record.ProductionCount = record.EndProductionCount - record.StartProductionCount;
                    }

                    record.EndTime = DateTime.UtcNow;
                    record.IsActive = false;

                    DetailedLogger.LogInfo($"Vardiya kalıp kaydı tamamlandı - Kalıp: {record.MoldName}, Üretim: {record.ProductionCount}");
                }

                await _context.SaveChangesAsync();

                // Tüm kayıtları döndür
                var allRecords = await _context.ShiftMoldRecords
                    .Where(r => r.ShiftId == shiftId)
                    .OrderBy(r => r.StartTime)
                    .ToListAsync();

                return allRecords;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Vardiya kalıp takibi tamamlanırken hata: {ex.Message}");
                return new List<ShiftMoldRecord>();
            }
        }

        /// <summary>
        /// Vardiya kalıp üretim özetini JSON formatında döndür
        /// </summary>
        public async Task<string> GetShiftMoldProductionSummary(int shiftId)
        {
            try
            {
                var records = await _context.ShiftMoldRecords
                    .Where(r => r.ShiftId == shiftId)
                    .OrderBy(r => r.StartTime)
                    .ToListAsync();

                Console.WriteLine($"[ShiftMoldTracking] ShiftId {shiftId} için {records.Count} kalıp kaydı bulundu");
                
                var summary = records.Select(r => new MoldProductionData
                {
                    MoldName = r.MoldName,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    ProductionCount = r.ProductionCount,
                    DurationMinutes = r.EndTime.HasValue ? (int)(r.EndTime.Value - r.StartTime).TotalMinutes : 0
                }).ToList();

                var json = JsonSerializer.Serialize(summary);
                Console.WriteLine($"[ShiftMoldTracking] JSON oluşturuldu: {json}");
                return json;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Vardiya kalıp özeti alınırken hata: {ex.Message}");
                return "[]";
            }
        }
    }

    /// <summary>
    /// Kalıp üretim verisi için yardımcı sınıf
    /// </summary>
    public class MoldProductionData
    {
        public string MoldName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ProductionCount { get; set; }
        public int DurationMinutes { get; set; }
    }
}
