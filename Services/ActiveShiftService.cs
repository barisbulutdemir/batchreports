using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Utils;

namespace takip.Services
{
    /// <summary>
    /// Aktif vardiya durumu yönetim servisi
    /// Program kapanıp açıldığında vardiya durumunu korumak için
    /// </summary>
    public class ActiveShiftService
    {
        private readonly ProductionDbContext _context;

        public ActiveShiftService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Aktif vardiya başlat
        /// </summary>
        public async Task<bool> StartActiveShift(int shiftRecordId, string operatorName, DateTime shiftStartTime, int startTotalProduction = 0, int? startDm452Value = null, int startFireProductCount = 0)
        {
            try
            {
                // Önceki aktif vardiyayı kapat
                await DeactivateAllActiveShifts();

                // Yeni aktif vardiya oluştur
                DetailedLogger.LogInfo($"🔧 ActiveShiftService.StartActiveShift - startTotalProduction: {startTotalProduction}, startDm452Value: {startDm452Value}");
                var activeShift = new ActiveShift
                {
                    ShiftRecordId = shiftRecordId,
                    OperatorName = operatorName,
                    ShiftStartTime = shiftStartTime,
                    StartTotalProduction = startTotalProduction,
                    StartDm452Value = startDm452Value,
                    StartFireProductCount = startFireProductCount,
                    IdleTimeSeconds = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                DetailedLogger.LogInfo($"🔧 ActiveShift oluşturuldu - StartTotalProduction: {activeShift.StartTotalProduction}, StartDm452Value: {activeShift.StartDm452Value}");

                _context.ActiveShifts.Add(activeShift);
                await _context.SaveChangesAsync();

                DetailedLogger.LogInfo($"Aktif vardiya başlatıldı - ShiftRecordId: {shiftRecordId}, Operatör: {operatorName}");
                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya başlatılırken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktif vardiyayı güncelle
        /// </summary>
        public async Task<bool> UpdateActiveShift(int shiftRecordId, DateTime? productionStartTime = null, int? currentTotalProduction = null, int? currentDm452Value = null, int? currentFireProductCount = null, int? currentIdleTimeSeconds = null)
        {
            try
            {
                var activeShift = await _context.ActiveShifts
                    .FirstOrDefaultAsync(s => s.ShiftRecordId == shiftRecordId && s.IsActive);

                if (activeShift == null)
                {
                    DetailedLogger.LogWarning($"Aktif vardiya bulunamadı - ShiftRecordId: {shiftRecordId}");
                    return false;
                }

                if (productionStartTime.HasValue)
                    activeShift.ProductionStartTime = productionStartTime.Value;

                // Mevcut değerleri persist et ki uygulama kapanıp açıldığında kaybolmasın
                if (currentTotalProduction.HasValue)
                    activeShift.StartTotalProduction = currentTotalProduction.Value;

                if (currentDm452Value.HasValue)
                    activeShift.StartDm452Value = currentDm452Value.Value;

                if (currentFireProductCount.HasValue)
                    activeShift.StartFireProductCount = currentFireProductCount.Value;

                if (currentIdleTimeSeconds.HasValue)
                    activeShift.IdleTimeSeconds = currentIdleTimeSeconds.Value;

                activeShift.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                DetailedLogger.LogInfo($"Aktif vardiya güncellendi - ShiftRecordId: {shiftRecordId}");
                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya güncellenirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktif vardiyayı bitir
        /// </summary>
        public async Task<bool> EndActiveShift(int shiftRecordId)
        {
            try
            {
                var activeShift = await _context.ActiveShifts
                    .FirstOrDefaultAsync(s => s.ShiftRecordId == shiftRecordId && s.IsActive);

                if (activeShift == null)
                {
                    DetailedLogger.LogWarning($"Aktif vardiya bulunamadı - ShiftRecordId: {shiftRecordId}");
                    return false;
                }

                activeShift.IsActive = false;
                activeShift.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                DetailedLogger.LogInfo($"Aktif vardiya bitirildi - ShiftRecordId: {shiftRecordId}");
                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya bitirilirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tüm aktif vardiyaları kapat
        /// </summary>
        public async Task<bool> DeactivateAllActiveShifts()
        {
            try
            {
                var activeShifts = await _context.ActiveShifts
                    .Where(s => s.IsActive)
                    .ToListAsync();

                foreach (var shift in activeShifts)
                {
                    shift.IsActive = false;
                    shift.UpdatedAt = DateTime.UtcNow;
                }

                if (activeShifts.Any())
                {
                    await _context.SaveChangesAsync();
                    DetailedLogger.LogInfo($"{activeShifts.Count} aktif vardiya kapatıldı");
                }

                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiyalar kapatılırken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktif vardiya bilgilerini al
        /// </summary>
        public async Task<ActiveShift?> GetActiveShift()
        {
            try
            {
                var activeShift = await _context.ActiveShifts
                    .FirstOrDefaultAsync(s => s.IsActive);

                if (activeShift != null)
                {
                    DetailedLogger.LogInfo($"🔍 GetActiveShift bulundu - StartTotalProduction: {activeShift.StartTotalProduction}, StartDm452Value: {activeShift.StartDm452Value}");
                }
                else
                {
                    DetailedLogger.LogInfo("🔍 GetActiveShift - Aktif vardiya bulunamadı");
                }

                return activeShift;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya alınırken hata: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Aktif vardiya var mı kontrol et
        /// </summary>
        public async Task<bool> HasActiveShift()
        {
            try
            {
                var hasActiveShift = await _context.ActiveShifts
                    .AnyAsync(s => s.IsActive);

                return hasActiveShift;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya kontrol edilirken hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Mevcut aktif vardiyayı temizle (debug amaçlı)
        /// </summary>
        public async Task<bool> ClearActiveShift()
        {
            try
            {
                var activeShifts = await _context.ActiveShifts
                    .Where(s => s.IsActive)
                    .ToListAsync();

                foreach (var shift in activeShifts)
                {
                    shift.IsActive = false;
                    shift.UpdatedAt = DateTime.UtcNow;
                }

                if (activeShifts.Any())
                {
                    await _context.SaveChangesAsync();
                    DetailedLogger.LogInfo($"{activeShifts.Count} aktif vardiya temizlendi");
                }

                return true;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Aktif vardiya temizlenirken hata: {ex.Message}");
                return false;
            }
        }
    }
}
