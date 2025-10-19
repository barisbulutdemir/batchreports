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
    /// Vardiya kayıt servisi - Vardiya geçmişi yönetimi
    /// </summary>
    public class ShiftRecordService
    {
        private readonly ProductionDbContext _context;

        public ShiftRecordService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Yeni vardiya kaydı oluştur
        /// </summary>
        public async Task<ShiftRecord> CreateShiftRecordAsync(
            DateTime shiftStartTime,
            DateTime shiftEndTime,
            string operatorName,
            int totalProduction,
            DateTime? productionStartTime,
            Dictionary<string, int> stoneProduction,
            int mixer1BatchCount = 0,
            double mixer1CementTotal = 0,
            string mixer1CementTypesJson = "",
            int mixer2BatchCount = 0,
            double mixer2CementTotal = 0,
            string mixer2CementTypesJson = "",
            string? moldProductionJson = "",
            string mixer1MaterialsJson = "",
            string mixer2MaterialsJson = "",
            string totalMaterialsJson = "",
            int fireProductCount = 0,
            int idleTimeSeconds = 0)
        {
            try
            {
                Console.WriteLine($"[ShiftRecordService] MoldProductionJson alınıyor: '{moldProductionJson}'");
                
                var shiftRecord = new ShiftRecord
                {
                    ShiftStartTime = shiftStartTime,
                    ShiftEndTime = shiftEndTime,
                    OperatorName = operatorName ?? "Bilinmeyen Operatör",
                    TotalProduction = totalProduction,
                    ProductionStartTime = productionStartTime,
                    ShiftDurationMinutes = (int)(shiftEndTime - shiftStartTime).TotalMinutes,
                    ProductionDurationMinutes = productionStartTime.HasValue 
                        ? (int)(shiftEndTime - productionStartTime.Value).TotalMinutes 
                        : 0,
                    StoneProductionJson = JsonSerializer.Serialize(stoneProduction),
                    Mixer1BatchCount = mixer1BatchCount,
                    Mixer1CementTotal = mixer1CementTotal,
                    Mixer1CementTypesJson = mixer1CementTypesJson,
                    Mixer2BatchCount = mixer2BatchCount,
                    Mixer2CementTotal = mixer2CementTotal,
                    Mixer2CementTypesJson = mixer2CementTypesJson,
                    MoldProductionJson = moldProductionJson,
                    Mixer1MaterialsJson = mixer1MaterialsJson,
                    Mixer2MaterialsJson = mixer2MaterialsJson,
                    TotalMaterialsJson = totalMaterialsJson,
                    FireProductCount = fireProductCount,
                    IdleTimeSeconds = idleTimeSeconds,
                    CreatedAt = DateTime.UtcNow
                };
                
                Console.WriteLine($"[ShiftRecordService] ShiftRecord oluşturuldu, MoldProductionJson: '{shiftRecord.MoldProductionJson}'");

                _context.ShiftRecords.Add(shiftRecord);
                await _context.SaveChangesAsync();

                await CleanupOldRecordsAsync();

                return shiftRecord;
            }
            catch (Exception ex)
            {
                throw new Exception($"Vardiya kaydı oluşturma hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Son 30 vardiya kaydını getir
        /// </summary>
        public async Task<List<ShiftRecord>> GetRecentShiftRecordsAsync(int count = 30)
        {
            try
            {
                return await _context.ShiftRecords
                    .OrderByDescending(s => s.ShiftStartTime)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Vardiya kayıtları getirme hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Belirli tarih aralığındaki vardiya kayıtlarını getir
        /// </summary>
        public async Task<List<ShiftRecord>> GetShiftRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.ShiftRecords
                    .Where(s => s.ShiftStartTime.Date >= startDate.Date && s.ShiftStartTime.Date <= endDate.Date)
                    .OrderByDescending(s => s.ShiftStartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Tarih aralığı vardiya kayıtları getirme hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Belirli operatörün vardiya kayıtlarını getir
        /// </summary>
        public async Task<List<ShiftRecord>> GetShiftRecordsByOperatorAsync(string operatorName)
        {
            try
            {
                return await _context.ShiftRecords
                    .Where(s => s.OperatorName == operatorName)
                    .OrderByDescending(s => s.ShiftStartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Operatör vardiya kayıtları getirme hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vardiya kaydındaki taş üretim verilerini parse et
        /// </summary>
        public Dictionary<string, int> ParseStoneProduction(string stoneProductionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(stoneProductionJson))
                    return new Dictionary<string, int>();

                return JsonSerializer.Deserialize<Dictionary<string, int>>(stoneProductionJson) 
                    ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Eski kayıtları temizle (son 30'dan fazlasını sil)
        /// </summary>
        private async Task CleanupOldRecordsAsync()
        {
            try
            {
                var totalRecords = await _context.ShiftRecords.CountAsync();
                if (totalRecords <= 30) return;

                var recordsToDelete = await _context.ShiftRecords
                    .OrderByDescending(s => s.ShiftStartTime)
                    .Skip(30)
                    .ToListAsync();

                if (recordsToDelete.Any())
                {
                    _context.ShiftRecords.RemoveRange(recordsToDelete);
                    await _context.SaveChangesAsync();
                    
                    await _context.Database.ExecuteSqlRawAsync("VACUUM");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Eski kayıt temizleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Vardiya istatistiklerini getir
        /// </summary>
        public async Task<Dictionary<string, object>> GetShiftStatisticsAsync()
        {
            try
            {
                var records = await _context.ShiftRecords.ToListAsync();
                
                if (!records.Any())
                {
                    return new Dictionary<string, object>
                    {
                        ["totalShifts"] = 0,
                        ["totalProduction"] = 0,
                        ["averageProduction"] = 0,
                        ["totalOperators"] = 0
                    };
                }

                var totalShifts = records.Count;
                var totalProduction = records.Sum(r => r.TotalProduction);
                var averageProduction = totalShifts > 0 ? totalProduction / totalShifts : 0;
                var totalOperators = records.Select(r => r.OperatorName).Distinct().Count();

                return new Dictionary<string, object>
                {
                    ["totalShifts"] = totalShifts,
                    ["totalProduction"] = totalProduction,
                    ["averageProduction"] = averageProduction,
                    ["totalOperators"] = totalOperators
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Vardiya istatistikleri getirme hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID'ye göre vardiya kaydı getir
        /// </summary>
        public async Task<ShiftRecord?> GetShiftRecordByIdAsync(int id)
        {
            return await _context.ShiftRecords
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        /// <summary>
        /// Son vardiya kaydını getir
        /// </summary>
        public async Task<ShiftRecord?> GetLastShiftRecordAsync()
        {
            return await _context.ShiftRecords
                .OrderByDescending(sr => sr.ShiftStartTime)
                .FirstOrDefaultAsync();
        }
    }
}
