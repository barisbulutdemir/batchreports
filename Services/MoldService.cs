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
    /// Kalıp Yönetimi Servisi
    /// </summary>
    public class MoldService
    {
        private readonly ProductionDbContext _context;

        public MoldService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Yeni kalıp ekle
        /// </summary>
        public async Task<Mold> AddMoldAsync(string name, string code)
        {
            try
            {
                var mold = new Mold
                {
                    Name = name,
                    Code = code,
                    IsActive = false,
                    TotalPrints = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Molds.Add(mold);
                await _context.SaveChangesAsync();
                return mold;
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp ekleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tüm kalıpları getir
        /// </summary>
        public async Task<List<Mold>> GetAllMoldsAsync()
        {
            try
            {
                return await _context.Molds
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp listesi getirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kalıp adına göre getir
        /// </summary>
        public async Task<Mold> GetMoldByNameAsync(string name)
        {
            try
            {
                return await _context.Molds
                    .FirstOrDefaultAsync(m => m.Name == name) ?? new Mold { Name = name };
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp getirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kalıp durumunu değiştir
        /// </summary>
        public async Task ToggleMoldStatusAsync(int moldId)
        {
            try
            {
                var mold = await _context.Molds.FindAsync(moldId);
                if (mold == null) return;

                if (mold.IsActive)
                {
                    // Kalıbı pasif yap
                    mold.IsActive = false;
                    mold.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Diğer aktif kalıpları pasif yap
                    var activeMolds = await _context.Molds
                        .Where(m => m.IsActive)
                        .ToListAsync();

                    foreach (var activeMold in activeMolds)
                    {
                        activeMold.IsActive = false;
                        activeMold.UpdatedAt = DateTime.UtcNow;
                    }

                    // Bu kalıbı aktif yap
                    mold.IsActive = true;
                    mold.CreatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp durumu değiştirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kalıp sil
        /// </summary>
        public async Task DeleteMoldAsync(int moldId)
        {
            try
            {
                var mold = await _context.Molds.FindAsync(moldId);
                if (mold != null)
                {
                    _context.Molds.Remove(mold);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp silme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktif kalıbı getir
        /// </summary>
        public async Task<Mold> GetActiveMoldAsync()
        {
            try
            {
                return await _context.Molds
                    .FirstOrDefaultAsync(m => m.IsActive) ?? new Mold { Name = "Varsayılan Kalıp", IsActive = true };
            }
            catch (Exception ex)
            {
                throw new Exception($"Aktif kalıp getirme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kalıp baskı sayısını güncelle
        /// </summary>
        public async Task UpdateMoldPrintsAsync(int moldId, int additionalPrints)
        {
            try
            {
                var mold = await _context.Molds.FindAsync(moldId);
                if (mold != null)
                {
                    mold.TotalPrints += additionalPrints;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Kalıp baskı sayısı güncelleme hatası: {ex.Message}");
            }
        }
    }
}
