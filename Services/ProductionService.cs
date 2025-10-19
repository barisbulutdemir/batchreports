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
    /// Üretim servisi - Python projesindeki ProductionService'in C# versiyonu
    /// </summary>
    public class ProductionService
    {
        private int? _currentProductionId = null;
        private string _lastStoneName = "";
        private int _lastCount = 0;

        /// <summary>
        /// Üretimi başlat
        /// </summary>
        public async Task<bool> StartProductionAsync(string operatorName)
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Aktif üretim var mı kontrol et
                var activeProduction = await context.Productions
                    .FirstOrDefaultAsync(p => p.OperatorName == operatorName && p.IsActive);

                if (activeProduction != null)
                {
                    return false; // Zaten aktif üretim var
                }

                // Yeni üretim kaydı oluştur
                var production = new Production
                {
                    OperatorName = operatorName,
                    StoneName = "", // İlk taş geldiğinde doldurulacak
                    Count = 0,
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };

                context.Productions.Add(production);
                await context.SaveChangesAsync();

                _currentProductionId = production.Id;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Üretimi sonlandır
        /// </summary>
        public async Task<bool> EndProductionAsync()
        {
            try
            {
                if (!_currentProductionId.HasValue)
                    return false;

                using var context = new ProductionDbContext();
                
                var production = await context.Productions
                    .FirstOrDefaultAsync(p => p.Id == _currentProductionId.Value);

                if (production != null)
                {
                    production.EndTime = DateTime.UtcNow;
                    production.IsActive = false;
                    await context.SaveChangesAsync();
                }

                _currentProductionId = null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Taş verilerini işle - Python projesindeki process_stone_data metodunun C# versiyonu
        /// </summary>
        public async Task<bool> ProcessStoneDataAsync(string stoneName, int count)
        {
            try
            {
                // İlk taş geldiğinde üretimi başlat
                if (!_currentProductionId.HasValue && count > 0)
                {
                    if (!await StartProductionAsync("Varsayılan Operatör"))
                        return false;
                }

                if (!_currentProductionId.HasValue)
                    return false;

                // Yeni taş mı kontrol et
                if (stoneName != _lastStoneName)
                {
                    _lastStoneName = stoneName;
                }

                // Sayacı kontrol et - Python'daki gibi fark hesaplama
                if (count > _lastCount)
                {
                    var increment = count - _lastCount;
                    _lastCount = count;

                    // Veritabanını güncelle
                    return await UpdateProductionCountAsync(stoneName, increment, count);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Üretim sayısını güncelle - Python'daki _update_production_count metodunun C# versiyonu
        /// </summary>
        private async Task<bool> UpdateProductionCountAsync(string stoneName, int increment, int totalCount)
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Üretim kaydını güncelle
                var production = await context.Productions
                    .FirstOrDefaultAsync(p => p.Id == _currentProductionId!.Value);

                if (production != null)
                {
                    production.StoneName = stoneName;
                    production.Count = totalCount;
                    await context.SaveChangesAsync();

                    // Log kaydı oluştur - Batch sisteme uyarlanmış
                    var log = new ProductionLog
                    {
                        ProductionId = production.Id,
                        StoneName = stoneName,
                        Count = increment,
                        TotalCount = totalCount,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    context.ProductionLogs.Add(log);
                    await context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Hata detayını log'a yaz
                System.Diagnostics.Debug.WriteLine($"ProductionService güncelleme hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İç hata: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Günlük üretim verilerini al - Python'daki get_daily_production metodunun C# versiyonu
        /// </summary>
        public async Task<Dictionary<string, object>> GetDailyProductionAsync(DateTime? targetDate = null)
        {
            try
            {
                if (!targetDate.HasValue)
                    targetDate = DateTime.Today;

                using var context = new ProductionDbContext();
                
                var startDate = targetDate.Value.Date;
                var endDate = startDate.AddDays(1);

                var productions = await context.Productions
                    .Where(p => p.StartTime >= startDate && p.StartTime < endDate)
                    .ToListAsync();

                var totalCount = productions.Sum(p => p.Count);
                var activeCount = productions.Count(p => p.IsActive);

                // Taş bazında dağılım - Python'daki stone_distribution gibi
                var stoneDistribution = new Dictionary<string, int>();
                foreach (var production in productions)
                {
                    if (!string.IsNullOrEmpty(production.StoneName))
                    {
                        if (!stoneDistribution.ContainsKey(production.StoneName))
                            stoneDistribution[production.StoneName] = 0;
                        stoneDistribution[production.StoneName] += production.Count;
                    }
                }

                return new Dictionary<string, object>
                {
                    { "date", targetDate.Value.ToString("yyyy-MM-dd") },
                    { "total_count", totalCount },
                    { "active_productions", activeCount },
                    { "stone_distribution", stoneDistribution }
                };
            }
            catch (Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Mevcut üretim verilerini al
        /// </summary>
        public async Task<Production?> GetCurrentProductionAsync()
        {
            try
            {
                if (!_currentProductionId.HasValue)
                    return null;

                using var context = new ProductionDbContext();
                return await context.Productions
                    .FirstOrDefaultAsync(p => p.Id == _currentProductionId!.Value);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
