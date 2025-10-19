using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip.Services
{
    public class ConcreteSnapshot
    {
        public (string type, int active, double kg)? Cement { get; set; }
        public List<(short slot, string? name, double kg)> Aggregates { get; set; } = new();
        public (double loadcellKg, double pulseKg) Water { get; set; }
        public double? MoisturePercent { get; set; }
        public double PigmentKg { get; set; }
        public List<(short slot, string? name, double chemicalKg, double waterKg)> Admixtures { get; set; } = new();
        public bool IsSimulated { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IConcretePlcReader
    {
        Task<ConcreteSnapshot> GetSnapshotAsync();
    }

    public class ConcretePlcSimulator : IConcretePlcReader
    {
        private readonly Random _random = new Random();
        private readonly ProductionDbContext _context;

        public ConcretePlcSimulator(ProductionDbContext context)
        {
            _context = context;
        }

        public Task<ConcreteSnapshot> GetSnapshotAsync()
        {
            var agg1 = 1000 + _random.NextDouble() * 300;
            var agg2 = 200 + _random.NextDouble() * 200;
            var agg3 = 200 + _random.NextDouble() * 200;

            // Alias isimlerini al
            var aggAliases = _context.AggregateAliases.Where(a => a.IsActive).ToDictionary(a => a.Slot, a => a.Name);
            var admAliases = _context.AdmixtureAliases.Where(a => a.IsActive).ToDictionary(a => a.Slot, a => a.Name);
            var cemAliases = _context.CementAliases.Where(a => a.IsActive).OrderBy(a => a.Slot).ToList();
            
            // Debug log
            System.Diagnostics.Debug.WriteLine($"[Simulator] {LocalizationService.Instance.GetString("ConcreteSimulation.AggregateAliases")}: {string.Join(", ", aggAliases.Select(kv => $"{kv.Key}={kv.Value}"))}");
            System.Diagnostics.Debug.WriteLine($"[Simulator] {LocalizationService.Instance.GetString("ConcreteSimulation.AdmixtureAliases")}: {string.Join(", ", admAliases.Select(kv => $"{kv.Key}={kv.Value}"))}");

            // Aktif çimento adı seç (varsayılan: "standard")
            var cementName = cemAliases.Count > 0 ? cemAliases.First().Name : "standard";

            var shot = new ConcreteSnapshot
            {
                Cement = (cementName, 1, 320 + _random.NextDouble() * 60),
                Aggregates = new List<(short, string?, double)>
                {
                    (1, aggAliases.ContainsKey(1) ? aggAliases[1] : "agrega1", Math.Round(agg1,1)),
                    (2, aggAliases.ContainsKey(2) ? aggAliases[2] : "agrega2", Math.Round(agg2,1)),
                    (3, aggAliases.ContainsKey(3) ? aggAliases[3] : "agrega3", Math.Round(agg3,1))
                },
                Water = (Math.Round(100 + _random.NextDouble()*40,1), Math.Round(20 + _random.NextDouble()*20,1)),
                // Nem tam sayı: 30-45 arası örnek
                MoisturePercent = (double)_random.Next(30, 46),
                PigmentKg = Math.Round(_random.NextDouble() < 0.3 ? _random.NextDouble()*3 : 0,1),
                Admixtures = new List<(short, string?, double, double)>
                {
                    (1, admAliases.ContainsKey(1) ? admAliases[1] : "katki1", 0.3 + _random.NextDouble()*0.4, 2.5 + _random.NextDouble()*1.5),
                    (2, admAliases.ContainsKey(2) ? admAliases[2] : "katki2", 0.2 + _random.NextDouble()*0.4, 2.0 + _random.NextDouble()*1.0)
                },
                IsSimulated = true
            };
            return Task.FromResult(shot);
        }
    }

    public class ConcreteBatchService
    {
        private readonly ProductionDbContext _context;
        private readonly IConcretePlcReader _reader;
        private readonly CementConsumptionService _cementConsumptionService;

        public ConcreteBatchService(ProductionDbContext context, IConcretePlcReader reader)
        {
            _context = context;
            _reader = reader;
            _cementConsumptionService = new CementConsumptionService(context);
        }

        public async Task<int> CaptureBatchAsync()
        {
            var snapshot = await _reader.GetSnapshotAsync();

            // Tam sayı çimento (kg)
            var totalCement = snapshot.Cement.HasValue && snapshot.Cement.Value.active == 1
                ? Math.Round(snapshot.Cement.Value.kg, 0)
                : 0;

            // Agrega toplamı tam sayı (kg)
            var totalAgg = snapshot.Aggregates.Where(a => a.kg > 0).Sum(a => Math.Round(a.kg, 0));
            var totalAdmChem = snapshot.Admixtures.Where(a => a.chemicalKg > 0 || a.waterKg > 0)
                                                 .Sum(a => a.chemicalKg);
            var admWater = snapshot.Admixtures.Sum(a => a.waterKg);

            // Toplam su (Mixer1): LC + Pulse (katkı suyu hariç)
            var effectiveWater = Math.Round((snapshot.Water.loadcellKg) + (snapshot.Water.pulseKg), 1);
            double? wc = null;
            if (totalCement > 0.0001)
            {
                wc = Math.Round(effectiveWater / totalCement, 3);
            }

            var batch = new ConcreteBatch
            {
                OccurredAt = snapshot.OccurredAtUtc,
                IsSimulated = snapshot.IsSimulated,
                MoisturePercent = snapshot.MoisturePercent,
                LoadcellWaterKg = snapshot.Water.loadcellKg,
                PulseWaterKg = snapshot.Water.pulseKg,
                PigmentKg = snapshot.PigmentKg,
                TotalCementKg = Math.Round(totalCement, 3),
                TotalAggregateKg = Math.Round(totalAgg, 3),
                TotalAdmixtureKg = Math.Round(totalAdmChem, 3),
                EffectiveWaterKg = Math.Round(effectiveWater, 3), // Toplam Su
                WaterCementRatio = wc,
                RawPayloadJson = JsonSerializer.Serialize(snapshot)
            };

            // Children
            if (snapshot.Cement.HasValue && snapshot.Cement.Value.active == 1 && snapshot.Cement.Value.kg > 0)
            {
                batch.Cements.Add(new ConcreteBatchCement
                {
                    Slot = 1, // Mixer1'de tek çimento slot'u
                    CementType = snapshot.Cement.Value.type,
                    WeightKg = Math.Round(snapshot.Cement.Value.kg, 0)
                });
            }

            // Mixer1: Aynı slottan gelen birden çok kayıt/okuma varsa çiftlemeyi önlemek için slot bazında grupla
            var aggregatedBySlot = snapshot.Aggregates
                .Where(x => x.kg > 0.1)
                .GroupBy(x => x.slot)
                .Select(g => new
                {
                    Slot = g.Key,
                    Name = g.Select(x => x.name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    Kg = g.Sum(x => x.kg)
                })
                .OrderBy(x => x.Slot)
                .ToList();

            // Alias'ları yükle
            var aggregateAliases = _context.AggregateAliases
                .Where(a => a.IsActive)
                .ToDictionary(x => x.Slot, x => x.Name);

            foreach (var a in aggregatedBySlot)
            {
                // Alias varsa kullan, yoksa orijinal ismi kullan
                var displayName = aggregateAliases.TryGetValue(a.Slot, out var aliasName) && !string.IsNullOrWhiteSpace(aliasName)
                    ? aliasName
                    : (!string.IsNullOrWhiteSpace(a.Name) ? a.Name : $"Agrega{a.Slot}");
                
                batch.Aggregates.Add(new ConcreteBatchAggregate
                {
                    Slot = a.Slot,
                    Name = displayName, // Alias ismi kullan
                    WeightKg = Math.Round(a.Kg, 0)
                });
            }

            foreach (var adm in snapshot.Admixtures.Where(x => x.chemicalKg > 0 || x.waterKg > 0))
            {
                batch.Admixtures.Add(new ConcreteBatchAdmixture
                {
                    Slot = adm.slot,
                    Name = adm.name,
                    ChemicalKg = Math.Round(adm.chemicalKg, 3),
                    WaterKg = Math.Round(adm.waterKg, 3)
                });
            }

            try
            {
                // Sadece problematik entity'leri detach et
                var trackedEntities = _context.ChangeTracker.Entries()
                    .Where(e => e.Entity is ConcreteBatchAdmixture && e.State == EntityState.Added)
                    .ToList();
                
                foreach (var entity in trackedEntities)
                {
                    entity.State = EntityState.Detached;
                }
                
                _context.ConcreteBatches.Add(batch);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] {LocalizationService.Instance.GetString("ConcreteSimulation.Mixer1BatchSaved")}: {batch.Id}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"[ConcreteBatchService] {LocalizationService.Instance.GetString("ConcreteSimulation.Mixer1BatchSaveError")}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n{LocalizationService.Instance.GetString("ConcreteSimulation.InnerException")}: {ex.InnerException.Message}";
                }
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] Stack Trace: {ex.StackTrace}");
                
                // MessageBox ile hata göster
                System.Windows.MessageBox.Show(errorMessage, LocalizationService.Instance.GetString("ConcreteSimulation.Mixer1BatchError"), 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }

            // Çimento tüketimini kaydet
            try
            {
                // Batch'i çimento koleksiyonu ile birlikte yeniden yükle
                var batchWithCements = await _context.ConcreteBatches
                    .Include(b => b.Cements)
                    .FirstOrDefaultAsync(b => b.Id == batch.Id);
                
                if (batchWithCements != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] Batch {batch.Id} {LocalizationService.Instance.GetString("ConcreteSimulation.BatchLoadedWithCements")}: {batchWithCements.Cements?.Count ?? 0}");
                    
                    // Çimento bilgilerini debug için yazdır
                    if (batchWithCements.Cements != null)
                    {
                        foreach (var cement in batchWithCements.Cements)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] {LocalizationService.Instance.GetString("ConcreteSimulation.Cement")}: {cement.CementType} - {cement.WeightKg}kg");
                        }
                    }
                    
                    await _cementConsumptionService.RecordMixer1ConsumptionAsync(batchWithCements);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] Batch {batch.Id} {LocalizationService.Instance.GetString("ConcreteSimulation.BatchReloadFailed")}!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] {LocalizationService.Instance.GetString("ConcreteSimulation.CementConsumptionSaveError")}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] {LocalizationService.Instance.GetString("ConcreteSimulation.InnerException")}: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[ConcreteBatchService] Stack Trace: {ex.StackTrace}");
                // Çimento tüketimi hatası batch'i etkilemesin
            }

            return batch.Id;
        }
    }
}
