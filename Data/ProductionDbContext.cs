using Microsoft.EntityFrameworkCore;
using takip.Models;
using System.IO;

namespace takip.Data
{
    /// <summary>
    /// Veritabanı bağlamı - Python projesindeki gibi basit yapı
    /// </summary>
    public class ProductionDbContext : DbContext
    {
        /// <summary>
        /// Üretim kayıtları tablosu
        /// </summary>
        public DbSet<Production> Productions { get; set; } = null!;
        
        /// <summary>
        /// Üretim logları tablosu
        /// </summary>
        public DbSet<ProductionLog> ProductionLogs { get; set; } = null!;
        
        /// <summary>
        /// Kalıplar tablosu
        /// </summary>
        public DbSet<Mold> Molds { get; set; } = null!;
        
        /// <summary>
        /// Vardiyalar tablosu
        /// </summary>
        public DbSet<Shift> Shifts { get; set; } = null!;
        
        /// <summary>
        /// Vardiya kayıtları tablosu (geçmiş vardiyalar)
        /// </summary>
        public DbSet<ShiftRecord> ShiftRecords { get; set; } = null!;
        
        /// <summary>
        /// Operatörler tablosu
        /// </summary>
        public DbSet<Operator> Operators { get; set; } = null!;
        
        /// <summary>
        /// Üretim notları tablosu
        /// </summary>
        public DbSet<ProductionNote> ProductionNotes { get; set; } = null!;

        /// <summary>
        /// Vardiya kalıp kayıtları tablosu
        /// </summary>
        public DbSet<ShiftMoldRecord> ShiftMoldRecords { get; set; } = null!;

        /// <summary>
        /// Aktif vardiya durumu tablosu
        /// </summary>
        public DbSet<ActiveShift> ActiveShifts { get; set; } = null!;

        // Concrete batching - Mixer 1
        public DbSet<ConcreteBatch> ConcreteBatches { get; set; } = null!;
        public DbSet<ConcreteBatchCement> ConcreteBatchCements { get; set; } = null!;
        public DbSet<ConcreteBatchAggregate> ConcreteBatchAggregates { get; set; } = null!;
        public DbSet<ConcreteBatchAdmixture> ConcreteBatchAdmixtures { get; set; } = null!;
        public DbSet<ConcreteBatchPigment> ConcreteBatchPigments { get; set; } = null!;
        public DbSet<AggregateAlias> AggregateAliases { get; set; } = null!;
        public DbSet<AdmixtureAlias> AdmixtureAliases { get; set; } = null!;

        public DbSet<CementAlias> CementAliases { get; set; } = null!;
        public DbSet<PigmentAlias> PigmentAliases { get; set; } = null!;

        // Mixer2 Concrete batching
        public DbSet<ConcreteBatch2> ConcreteBatch2s { get; set; } = null!;
        public DbSet<ConcreteBatch2Cement> ConcreteBatch2Cements { get; set; } = null!;
        public DbSet<ConcreteBatch2Aggregate> ConcreteBatch2Aggregates { get; set; } = null!;
        public DbSet<ConcreteBatch2Admixture> ConcreteBatch2Admixtures { get; set; } = null!;
        public DbSet<Aggregate2Alias> Aggregate2Aliases { get; set; } = null!;
        public DbSet<Admixture2Alias> Admixture2Aliases { get; set; } = null!;
        public DbSet<Cement2Alias> Cement2Aliases { get; set; } = null!;
        public DbSet<Pigment2Alias> Pigment2Aliases { get; set; } = null!;

        // Çimento Silo Yönetimi
        public DbSet<CementSilo> CementSilos { get; set; } = null!;
        public DbSet<CementConsumption> CementConsumptions { get; set; } = null!;
        public DbSet<CementRefill> CementRefills { get; set; } = null!;
        
        // Mixer2 Flow System - KALDIRILDI (kullanılmayan modeller)
        // public DbSet<Material> Materials { get; set; } = null!;
        // public DbSet<MaterialEvent> MaterialEvents { get; set; } = null!;
        // public DbSet<Bunker> Bunkers { get; set; } = null!;
        // public DbSet<Mixer> Mixers { get; set; } = null!;
        public DbSet<PlcDataSnapshot> PlcDataSnapshots { get; set; } = null!;

        // Recipe Management - KALDIRILDI (kullanılmayan modeller)
        // public DbSet<ProcessStep> ProcessSteps { get; set; } = null!;
        // public DbSet<ProductionBatch> ProductionBatches { get; set; } = null!;
        // public DbSet<Recipe> Recipes { get; set; } = null!;

        /// <summary>
        /// Veritabanı bağlantı ayarları
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // PostgreSQL kullan - mevcut raporlama veritabanı
            var connectionString = GetConnectionString();
            optionsBuilder.UseNpgsql(connectionString);
            
            // Genel ayarlar
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning));
        }


        /// <summary>
        /// Bağlantı string'ini al
        /// </summary>
        private string GetConnectionString()
        {
            // Önce appsettings.json'dan oku
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (config != null && config.ContainsKey("ConnectionStrings"))
                    {
                        var connectionStrings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                            config["ConnectionStrings"].ToString() ?? "{}");
                        if (connectionStrings != null && connectionStrings.ContainsKey("DefaultConnection"))
                        {
                            return connectionStrings["DefaultConnection"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB] appsettings.json okuma hatası: {ex.Message}");
                }
            }

            // Varsayılan bağlantı string'i
            return "Host=localhost;Database=takip_db;Username=postgres;Password=632536";
        }

        /// <summary>
        /// Model yapılandırması
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PostgreSQL DateTime konfigürasyonu - UTC olarak kaydet
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetColumnType("timestamp with time zone");
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v.Value) : null,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null));
                    }
                }
            }

            // Üretim modeli yapılandırması
            modelBuilder.Entity<Production>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperatorName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StoneName).IsRequired().HasMaxLength(100);
                
                // Indexler
                entity.HasIndex(e => e.OperatorName).HasDatabaseName("IX_Production_OperatorName");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Production_IsActive");
                entity.HasIndex(e => e.StartTime).HasDatabaseName("IX_Production_StartTime");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Production_CreatedAt");
            });

            // Üretim log modeli yapılandırması
            modelBuilder.Entity<ProductionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StoneName).IsRequired().HasMaxLength(100);
                
                // Foreign key ilişkisi
                entity.HasOne<Production>()
                      .WithMany()
                      .HasForeignKey(e => e.ProductionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Indexler
                entity.HasIndex(e => e.ProductionId).HasDatabaseName("IX_ProductionLog_ProductionId");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ProductionLog_CreatedAt");
                entity.HasIndex(e => e.StoneName).HasDatabaseName("IX_ProductionLog_StoneName");
            });

            // Kalıp modeli yapılandırması
            modelBuilder.Entity<Mold>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Vardiya modeli yapılandırması
            modelBuilder.Entity<Shift>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OperatorName).HasMaxLength(100);
            });

            // Vardiya kayıt modeli yapılandırması
            modelBuilder.Entity<ShiftRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperatorName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StoneProductionJson).HasMaxLength(2000);
                
                // Indexler
                entity.HasIndex(e => e.ShiftStartTime).HasDatabaseName("IX_ShiftRecord_ShiftStartTime");
                entity.HasIndex(e => e.OperatorName).HasDatabaseName("IX_ShiftRecord_OperatorName");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ShiftRecord_CreatedAt");
            });

            // Aktif vardiya modeli yapılandırması
            modelBuilder.Entity<ActiveShift>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperatorName).IsRequired().HasMaxLength(100);
                
                // Indexler
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ActiveShift_IsActive");
                entity.HasIndex(e => e.ShiftStartTime).HasDatabaseName("IX_ActiveShift_ShiftStartTime");
                entity.HasIndex(e => e.OperatorName).HasDatabaseName("IX_ActiveShift_OperatorName");
            });

            // Operatör modeli yapılandırması
            modelBuilder.Entity<Operator>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                
                // Indexler
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_Operator_Name");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Operator_IsActive");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Operator_CreatedAt");
            });

            // ConcreteBatch configuration
            modelBuilder.Entity<ConcreteBatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PlantCode).HasMaxLength(50);
                entity.Property(e => e.OperatorName).HasMaxLength(100);
                entity.Property(e => e.RecipeCode).HasMaxLength(100);
                entity.Property(e => e.RawPayloadJson).HasMaxLength(8000);

                entity.HasIndex(e => e.OccurredAt).HasDatabaseName("IX_ConcreteBatch_OccurredAt");
                entity.HasIndex(e => e.RecipeCode).HasDatabaseName("IX_ConcreteBatch_RecipeCode");
            });

            modelBuilder.Entity<ConcreteBatchCement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CementType).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Cements)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ConcreteBatchAggregate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Aggregates)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.BatchId, e.Slot }).HasDatabaseName("IX_CBA_Batch_Slot");
            });

            modelBuilder.Entity<ConcreteBatchAdmixture>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Admixtures)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.BatchId, e.Slot }).HasDatabaseName("IX_CBM_Batch_Slot");
            });

            modelBuilder.Entity<ConcreteBatchPigment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Pigments)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.BatchId, e.Slot }).HasDatabaseName("IX_CBP_Batch_Slot");
            });

            modelBuilder.Entity<AggregateAlias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_AggregateAlias_Slot");
            });

            modelBuilder.Entity<AdmixtureAlias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_AdmixtureAlias_Slot");
            });

            // Mixer2 modelleri
            modelBuilder.Entity<ConcreteBatch2>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PlantCode).HasMaxLength(50);
                entity.Property(e => e.OperatorName).HasMaxLength(100);
                entity.Property(e => e.RecipeCode).HasMaxLength(50);
                entity.Property(e => e.RawPayloadJson).HasMaxLength(8000);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("yatay_kovada");
                entity.HasIndex(e => e.OccurredAt).HasDatabaseName("IX_CB2_OccurredAt");
                entity.HasIndex(e => e.RecipeCode).HasDatabaseName("IX_CB2_RecipeCode");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_CB2_Status");
            });

            modelBuilder.Entity<ConcreteBatch2Cement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CementType).HasMaxLength(50);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Cements)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ConcreteBatch2Aggregate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Aggregates)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.BatchId, e.Slot }).HasDatabaseName("IX_CB2A_Batch_Slot");
            });

            modelBuilder.Entity<ConcreteBatch2Admixture>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.HasOne(e => e.Batch)
                      .WithMany(b => b.Admixtures)
                      .HasForeignKey(e => e.BatchId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.BatchId, e.Slot }).HasDatabaseName("IX_CB2M_Batch_Slot");
            });

            modelBuilder.Entity<CementAlias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_CementAlias_Slot");
            });

            modelBuilder.Entity<PigmentAlias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_PigmentAlias_Slot");
            });

            // Mixer2 alias tabloları
            modelBuilder.Entity<Aggregate2Alias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_Aggregate2Alias_Slot");
            });

            modelBuilder.Entity<Admixture2Alias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_Admixture2Alias_Slot");
            });

            modelBuilder.Entity<Cement2Alias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_Cement2Alias_Slot");
            });

            modelBuilder.Entity<Pigment2Alias>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Slot).HasDatabaseName("IX_Pigment2Alias_Slot");
            });

            // Cement Silo Management
            modelBuilder.Entity<CementSilo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CementType).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.SiloNumber).HasDatabaseName("IX_CementSilo_SiloNumber");
                entity.HasIndex(e => e.CementType).HasDatabaseName("IX_CementSilo_CementType");
            });

            modelBuilder.Entity<CementConsumption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConsumptionType).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasOne(e => e.Silo)
                      .WithMany()
                      .HasForeignKey(e => e.SiloId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ConsumedAt).HasDatabaseName("IX_CementConsumption_ConsumedAt");
                entity.HasIndex(e => e.MixerId).HasDatabaseName("IX_CementConsumption_MixerId");
            });

            modelBuilder.Entity<CementRefill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperatorName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ShipmentNumber).HasMaxLength(100);
                entity.Property(e => e.Supplier).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasOne(e => e.Silo)
                      .WithMany()
                      .HasForeignKey(e => e.SiloId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.RefilledAt).HasDatabaseName("IX_CementRefill_RefilledAt");
            });

            // Mixer2 Flow System - KALDIRILDI (kullanılmayan modeller)
            // modelBuilder.Entity<Material>(entity => { ... });
            // modelBuilder.Entity<MaterialEvent>(entity => { ... });
            // modelBuilder.Entity<Bunker>(entity => { ... });
            // modelBuilder.Entity<Mixer>(entity => { ... });

            modelBuilder.Entity<PlcDataSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Operator).HasMaxLength(100);
                entity.Property(e => e.RecipeCode).HasMaxLength(50);
                entity.Property(e => e.RawDataJson).HasMaxLength(8000);
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_PlcDataSnapshot_Timestamp");
                entity.HasIndex(e => e.Operator).HasDatabaseName("IX_PlcDataSnapshot_Operator");
            });

            // CementSilo navigation properties
            modelBuilder.Entity<CementSilo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasMany<CementRefill>()
                      .WithOne(r => r.Silo)
                      .HasForeignKey(r => r.SiloId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany<CementConsumption>()
                      .WithOne(c => c.Silo)
                      .HasForeignKey(c => c.SiloId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}