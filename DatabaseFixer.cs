using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using takip.Data;

namespace takip
{
    public static class DatabaseFixer
    {
        public static void FixDatabase()
        {
            try
            {
                DetailedLogger.LogInfo("[DatabaseFixer] Veritabanı düzeltme başlatılıyor...");
                Console.WriteLine("[DatabaseFixer] Veritabanı düzeltme başlatılıyor...");
                
                DetailedLogger.LogInfo("[DatabaseFixer] ProductionDbContext oluşturuluyor...");
                using var context = new ProductionDbContext();
                DetailedLogger.LogInfo("[DatabaseFixer] ProductionDbContext oluşturuldu");
                
                DetailedLogger.LogInfo("[DatabaseFixer] Database connection alınıyor...");
                var connection = context.Database.GetDbConnection();
                DetailedLogger.LogInfo("[DatabaseFixer] Database connection alındı");
                
                DetailedLogger.LogInfo($"[DatabaseFixer] Connection state: {connection.State}");
                if (connection.State != ConnectionState.Open)
                {
                    DetailedLogger.LogInfo("[DatabaseFixer] Connection açılıyor...");
                    connection.Open();
                    DetailedLogger.LogInfo("[DatabaseFixer] Connection açıldı");
                }

                // PlcDataSnapshots tablosunun var olup olmadığını kontrol et
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'PlcDataSnapshots';";
                
                bool tableExists = false;
                using (var reader = checkTableCommand.ExecuteReader())
                {
                    tableExists = reader.Read();
                }
                
                if (!tableExists)
                {
                    // PlcDataSnapshots tablosunu oluştur
                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = @"
                        CREATE TABLE ""PlcDataSnapshots"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""Timestamp"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            ""Operator"" VARCHAR(100),
                            ""RecipeCode"" VARCHAR(50),
                            ""AggregateGroupActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""WaterGroupActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""CementGroupActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""AdmixtureGroupActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""PigmentGroupActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate1Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate1Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate2Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate2Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate3Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate3Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate4Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate4Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate5Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate5Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate6Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate6Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate7Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate7Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Aggregate8Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Aggregate8Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Water1Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Water1Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Water2Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Water2Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Cement1Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Cement1Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Cement2Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Cement2Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Cement3Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Cement3Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture1Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Admixture1ChemicalAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture1WaterAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture2Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Admixture2ChemicalAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture2WaterAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture3Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Admixture3ChemicalAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture3WaterAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture4Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Admixture4ChemicalAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Admixture4WaterAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment1Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Pigment1Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment2Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Pigment2Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment3Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Pigment3Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment4Active"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""Pigment4Amount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""MoisturePercent"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""BatchReadySignal"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""HorizontalHasMaterial"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""VerticalHasMaterial"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""WaitingBunkerHasMaterial"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MixerHasAggregate"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MixerHasCement"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MixerHasAdmixture"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MixerHasWaterLoadcell"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MixerHasWaterPulse"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""RawDataJson"" VARCHAR(8000)
                        );";
                    createTableCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("PlcDataSnapshots tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("PlcDataSnapshots tablosu zaten mevcut.");
                }

                // ConcreteBatch2s tablosunun var olup olmadığını kontrol et
                var checkBatchTableCommand = connection.CreateCommand();
                checkBatchTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ConcreteBatch2s';";
                
                bool batchTableExists = false;
                using (var reader = checkBatchTableCommand.ExecuteReader())
                {
                    batchTableExists = reader.Read();
                }
                
                if (!batchTableExists)
                {
                    // ConcreteBatch2s tablosunu oluştur
                    var createBatchTableCommand = connection.CreateCommand();
                    createBatchTableCommand.CommandText = @"
                        CREATE TABLE ""ConcreteBatch2s"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""OccurredAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            ""PlantCode"" VARCHAR(50),
                            ""OperatorName"" VARCHAR(100),
                            ""RecipeCode"" VARCHAR(50),
                            ""IsSimulated"" BOOLEAN NOT NULL DEFAULT FALSE,
                            ""MoisturePercent"" DOUBLE PRECISION,
                            ""LoadcellWater1Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""LoadcellWater2Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""PulseWater1Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""PulseWater2Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment1Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment2Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment3Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Pigment4Kg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""TotalCementKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""TotalAggregateKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""TotalAdmixtureKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""TotalPigmentKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""EffectiveWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""WaterCementRatio"" DOUBLE PRECISION,
                            ""RawPayloadJson"" VARCHAR(8000),
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                            ""Status"" VARCHAR(50) NOT NULL DEFAULT 'yatay_kovada'
                        );";
                    createBatchTableCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("ConcreteBatch2s tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("ConcreteBatch2s tablosu zaten mevcut.");
                }

                // ConcreteBatch2Aggregates tablosunun var olup olmadığını kontrol et
                var checkAggregatesTableCommand = connection.CreateCommand();
                checkAggregatesTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ConcreteBatch2Aggregates';";
                
                bool aggregatesTableExists = false;
                using (var reader = checkAggregatesTableCommand.ExecuteReader())
                {
                    aggregatesTableExists = reader.Read();
                }
                
                if (!aggregatesTableExists)
                {
                    // ConcreteBatch2Aggregates tablosunu oluştur
                    var createAggregatesTableCommand = connection.CreateCommand();
                    createAggregatesTableCommand.CommandText = @"
                        CREATE TABLE ""ConcreteBatch2Aggregates"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""BatchId"" INTEGER NOT NULL,
                            ""Slot"" SMALLINT NOT NULL,
                            ""Name"" VARCHAR(100),
                            ""WeightKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatch2s""(""Id"") ON DELETE CASCADE
                        );";
                    createAggregatesTableCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("ConcreteBatch2Aggregates tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("ConcreteBatch2Aggregates tablosu zaten mevcut.");
                }

                // ConcreteBatch2Cements tablosunun var olup olmadığını kontrol et
                var checkCementsTableCommand = connection.CreateCommand();
                checkCementsTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ConcreteBatch2Cements';";
                
                bool cementsTableExists = false;
                using (var reader = checkCementsTableCommand.ExecuteReader())
                {
                    cementsTableExists = reader.Read();
                }
                
                if (!cementsTableExists)
                {
                    // ConcreteBatch2Cements tablosunu oluştur
                    var createCementsTableCommand = connection.CreateCommand();
                    createCementsTableCommand.CommandText = @"
                        CREATE TABLE ""ConcreteBatch2Cements"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""BatchId"" INTEGER NOT NULL,
                            ""Slot"" SMALLINT NOT NULL,
                            ""CementType"" VARCHAR(50) NOT NULL,
                            ""WeightKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatch2s""(""Id"") ON DELETE CASCADE
                        );";
                    createCementsTableCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("ConcreteBatch2Cements tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("ConcreteBatch2Cements tablosu zaten mevcut.");
                }

                // ConcreteBatch2Admixtures tablosunun var olup olmadığını kontrol et
                var checkAdmixturesTableCommand = connection.CreateCommand();
                checkAdmixturesTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ConcreteBatch2Admixtures';";
                
                bool admixturesTableExists = false;
                using (var reader = checkAdmixturesTableCommand.ExecuteReader())
                {
                    admixturesTableExists = reader.Read();
                }
                
                if (!admixturesTableExists)
                {
                    // ConcreteBatch2Admixtures tablosunu oluştur
                    var createAdmixturesTableCommand = connection.CreateCommand();
                    createAdmixturesTableCommand.CommandText = @"
                        CREATE TABLE ""ConcreteBatch2Admixtures"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""BatchId"" INTEGER NOT NULL,
                            ""Slot"" SMALLINT NOT NULL,
                            ""Name"" VARCHAR(100),
                            ""ChemicalKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""WaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatch2s""(""Id"") ON DELETE CASCADE
                        );";
                    createAdmixturesTableCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("ConcreteBatch2Admixtures tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("ConcreteBatch2Admixtures tablosu zaten mevcut.");
                }

                // Status sütununun var olup olmadığını kontrol et (PostgreSQL)
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ConcreteBatch2s' AND column_name = 'Status';";
                
                bool statusColumnExists = false;
                using (var reader = checkCommand.ExecuteReader())
                {
                    statusColumnExists = reader.Read();
                }
                
                if (!statusColumnExists)
                {
                    // Status sütununu ekle (PostgreSQL)
                    var addColumnCommand = connection.CreateCommand();
                    addColumnCommand.CommandText = "ALTER TABLE \"ConcreteBatch2s\" ADD COLUMN \"Status\" VARCHAR(50) DEFAULT 'yatay_kovada';";
                    addColumnCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("Status sütunu başarıyla eklendi.");
                }
                else
                {
                    Console.WriteLine("Status sütunu zaten mevcut.");
                }

                // Alias tablolarını oluştur
                CreateAliasTables(connection);
                
                // Çimento silo tablolarını oluştur
                Console.WriteLine("[DatabaseFixer] Çimento silo tabloları oluşturuluyor...");
                CreateCementSiloTables(connection);
                
                // Diğer gerekli tabloları oluştur
                CreateOtherTables(connection);
                
                // Eksik sütunları ekle
                AddMissingColumns(connection);
                DetailedLogger.LogInfo("[DatabaseFixer] Veritabanı düzeltme tamamlandı.");
                Console.WriteLine("[DatabaseFixer] Veritabanı düzeltme tamamlandı.");
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError("[DatabaseFixer] Veritabanı düzeltme hatası", ex);
                Console.WriteLine($"[DatabaseFixer] Veritabanı düzeltme hatası: {ex.Message}");
                Console.WriteLine($"[DatabaseFixer] Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"[DatabaseFixer] Stack Trace: {ex.StackTrace}");
            }
        }

        private static void CreateAliasTables(IDbConnection connection)
        {
            try
            {
                // Aggregate2Aliases tablosu
                CreateTableIfNotExists(connection, "Aggregate2Aliases", @"
                    CREATE TABLE ""Aggregate2Aliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" INTEGER NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // Admixture2Aliases tablosu
                CreateTableIfNotExists(connection, "Admixture2Aliases", @"
                    CREATE TABLE ""Admixture2Aliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" INTEGER NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // Cement2Aliases tablosu
                CreateTableIfNotExists(connection, "Cement2Aliases", @"
                    CREATE TABLE ""Cement2Aliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" INTEGER NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // Pigment2Aliases tablosu
                CreateTableIfNotExists(connection, "Pigment2Aliases", @"
                    CREATE TABLE ""Pigment2Aliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" INTEGER NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // Mixer1 alias tabloları da oluştur
                CreateTableIfNotExists(connection, "AggregateAliases", @"
                    CREATE TABLE ""AggregateAliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(50) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                    );");

                CreateTableIfNotExists(connection, "AdmixtureAliases", @"
                    CREATE TABLE ""AdmixtureAliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(50) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                    );");

                CreateTableIfNotExists(connection, "CementAliases", @"
                    CREATE TABLE ""CementAliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                    );");

                CreateTableIfNotExists(connection, "PigmentAliases", @"
                    CREATE TABLE ""PigmentAliases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                    );");

                Console.WriteLine("Alias tabloları başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alias tabloları oluşturulurken hata: {ex.Message}");
            }
        }

        private static void CreateCementSiloTables(IDbConnection connection)
        {
            try
            {
                // CementSilos tablosu
                CreateTableIfNotExists(connection, "CementSilos", @"
                    CREATE TABLE ""CementSilos"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""SiloNumber"" INTEGER NOT NULL,
                        ""CementType"" VARCHAR(50) NOT NULL,
                        ""CurrentAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""LastRefillDate"" TIMESTAMP WITH TIME ZONE,
                        ""Capacity"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""MinLevel"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""LastUpdated"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // CementConsumptions tablosu
                CreateTableIfNotExists(connection, "CementConsumptions", @"
                    CREATE TABLE ""CementConsumptions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""SiloId"" INTEGER NOT NULL,
                        ""ConsumedAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""ConsumedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""BatchId"" INTEGER,
                        ""MixerId"" INTEGER NOT NULL,
                        ""RemainingAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""ConsumptionType"" VARCHAR(50) NOT NULL DEFAULT 'Production',
                        ""Notes"" VARCHAR(500)
                    );");

                // CementRefills tablosu
                CreateTableIfNotExists(connection, "CementRefills", @"
                    CREATE TABLE ""CementRefills"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""SiloId"" INTEGER NOT NULL,
                        ""RefillAmount"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""RefillDate"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""RefillType"" VARCHAR(50) NOT NULL DEFAULT 'Manual',
                        ""Notes"" VARCHAR(500)
                    );");

                Console.WriteLine("Çimento silo tabloları başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Çimento silo tabloları oluşturulurken hata: {ex.Message}");
            }
        }

        private static void CreateOtherTables(IDbConnection connection)
        {
            try
            {
                // Operators tablosu
                CreateTableIfNotExists(connection, "Operators", @"
                    CREATE TABLE ""Operators"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // Productions tablosu
                CreateTableIfNotExists(connection, "Productions", @"
                    CREATE TABLE ""Productions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""OperatorName"" VARCHAR(100) NOT NULL,
                        ""StoneName"" VARCHAR(100) NOT NULL,
                        ""StartTime"" TIMESTAMP WITH TIME ZONE NOT NULL,
                        ""EndTime"" TIMESTAMP WITH TIME ZONE,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // ProductionLogs tablosu
                CreateTableIfNotExists(connection, "ProductionLogs", @"
                    CREATE TABLE ""ProductionLogs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""ProductionId"" INTEGER NOT NULL,
                        ""StoneName"" VARCHAR(100) NOT NULL,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        FOREIGN KEY (""ProductionId"") REFERENCES ""Productions""(""Id"") ON DELETE CASCADE
                    );");

                // Molds tablosu
                CreateTableIfNotExists(connection, "Molds", @"
                    CREATE TABLE ""Molds"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""Code"" VARCHAR(50) NOT NULL,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""TotalPrints"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        ""Description"" VARCHAR(500)
                    );");

                // Shifts tablosu
                CreateTableIfNotExists(connection, "Shifts", @"
                    CREATE TABLE ""Shifts"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(100) NOT NULL,
                        ""OperatorName"" VARCHAR(100),
                        ""StartTime"" TIMESTAMP WITH TIME ZONE,
                        ""EndTime"" TIMESTAMP WITH TIME ZONE,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // ShiftRecords tablosu
                CreateTableIfNotExists(connection, "ShiftRecords", @"
                    CREATE TABLE ""ShiftRecords"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""OperatorName"" VARCHAR(100) NOT NULL,
                        ""ShiftStartTime"" TIMESTAMP WITH TIME ZONE NOT NULL,
                        ""ShiftEndTime"" TIMESTAMP WITH TIME ZONE,
                        ""TotalProduction"" INTEGER NOT NULL DEFAULT 0,
                        ""ProductionStartTime"" TIMESTAMP WITH TIME ZONE,
                        ""ShiftDurationMinutes"" INTEGER NOT NULL DEFAULT 0,
                        ""ProductionDurationMinutes"" INTEGER NOT NULL DEFAULT 0,
                        ""StoneProductionJson"" VARCHAR(2000),
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // ConcreteBatches tablosu (Mixer1)
                CreateTableIfNotExists(connection, "ConcreteBatches", @"
                    CREATE TABLE ""ConcreteBatches"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""OccurredAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                        ""PlantCode"" VARCHAR(50),
                        ""OperatorName"" VARCHAR(100),
                        ""RecipeCode"" VARCHAR(100),
                        ""IsSimulated"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""MoisturePercent"" DOUBLE PRECISION,
                        ""LoadcellWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""PulseWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""PigmentKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""TotalCementKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""TotalAggregateKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""TotalAdmixtureKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""EffectiveWaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""WaterCementRatio"" DOUBLE PRECISION,
                        ""RawPayloadJson"" VARCHAR(8000),
                        ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                    );");

                // ConcreteBatchCements tablosu (Mixer1)
                CreateTableIfNotExists(connection, "ConcreteBatchCements", @"
                    CREATE TABLE ""ConcreteBatchCements"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""BatchId"" INTEGER NOT NULL,
                        ""Slot"" SMALLINT NOT NULL,
                        ""CementType"" VARCHAR(50) NOT NULL,
                        ""WeightKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatches""(""Id"") ON DELETE CASCADE
                    );");

                // ConcreteBatchAggregates tablosu (Mixer1)
                CreateTableIfNotExists(connection, "ConcreteBatchAggregates", @"
                    CREATE TABLE ""ConcreteBatchAggregates"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""BatchId"" INTEGER NOT NULL,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(50),
                        ""WeightKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatches""(""Id"") ON DELETE CASCADE
                    );");

                // ConcreteBatchAdmixtures tablosu (Mixer1)
                CreateTableIfNotExists(connection, "ConcreteBatchAdmixtures", @"
                    CREATE TABLE ""ConcreteBatchAdmixtures"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""BatchId"" INTEGER NOT NULL,
                        ""Slot"" SMALLINT NOT NULL,
                        ""Name"" VARCHAR(50),
                        ""ChemicalKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        ""WaterKg"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                        FOREIGN KEY (""BatchId"") REFERENCES ""ConcreteBatches""(""Id"") ON DELETE CASCADE
                    );");

                Console.WriteLine("Diğer tablolar başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Diğer tablolar oluşturulurken hata: {ex.Message}");
            }
        }

        private static void AddMissingColumns(IDbConnection connection)
        {
            try
            {
                // Operators tablosuna UpdatedAt sütunu ekle
                AddColumnIfNotExists(connection, "Operators", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                // Molds tablosuna eksik sütunları ekle
                AddColumnIfNotExists(connection, "Molds", "IsActive", "BOOLEAN NOT NULL DEFAULT FALSE");
                AddColumnIfNotExists(connection, "Molds", "TotalPrints", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "Molds", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                // ShiftRecords tablosuna eksik sütunları ekle
                AddColumnIfNotExists(connection, "ShiftRecords", "TotalProduction", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ShiftRecords", "ProductionStartTime", "TIMESTAMP WITH TIME ZONE");
                AddColumnIfNotExists(connection, "ShiftRecords", "ShiftDurationMinutes", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ShiftRecords", "ProductionDurationMinutes", "INTEGER NOT NULL DEFAULT 0");
                
                // ConcreteBatches tablosuna eksik sütunları ekle (Mixer1)
                AddColumnIfNotExists(connection, "ConcreteBatches", "LoadcellWaterKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "PulseWaterKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "PigmentKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "TotalCementKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "TotalAggregateKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "TotalAdmixtureKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "EffectiveWaterKg", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "ConcreteBatches", "WaterCementRatio", "DOUBLE PRECISION");
                AddColumnIfNotExists(connection, "ConcreteBatches", "RawPayloadJson", "VARCHAR(8000)");
                AddColumnIfNotExists(connection, "ConcreteBatches", "CreatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "ConcreteBatches", "CompletedAt", "TIMESTAMP WITH TIME ZONE");
                
                // ConcreteBatchCements tablosuna eksik sütunları ekle (Mixer1)
                AddColumnIfNotExists(connection, "ConcreteBatchCements", "Slot", "SMALLINT NOT NULL DEFAULT 1");
                
                // Alias tablolarına eksik sütunları ekle
                AddColumnIfNotExists(connection, "AggregateAliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "AdmixtureAliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "CementAliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "PigmentAliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                
                AddColumnIfNotExists(connection, "Aggregate2Aliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "Aggregate2Aliases", "CreatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "Aggregate2Aliases", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                AddColumnIfNotExists(connection, "Admixture2Aliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "Admixture2Aliases", "CreatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "Admixture2Aliases", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                AddColumnIfNotExists(connection, "Cement2Aliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "Cement2Aliases", "CreatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "Cement2Aliases", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                AddColumnIfNotExists(connection, "Pigment2Aliases", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "Pigment2Aliases", "CreatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "Pigment2Aliases", "UpdatedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                // Çimento silo tablolarına eksik sütunları ekle
                AddColumnIfNotExists(connection, "CementSilos", "SiloNumber", "INTEGER NOT NULL DEFAULT 1");
                AddColumnIfNotExists(connection, "CementSilos", "CementType", "VARCHAR(50) NOT NULL DEFAULT 'Standard'");
                AddColumnIfNotExists(connection, "CementSilos", "CurrentAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementSilos", "LastRefillDate", "TIMESTAMP WITH TIME ZONE");
                AddColumnIfNotExists(connection, "CementSilos", "Capacity", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementSilos", "MinLevel", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementSilos", "IsActive", "BOOLEAN NOT NULL DEFAULT TRUE");
                AddColumnIfNotExists(connection, "CementSilos", "LastUpdated", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                
                // ConcreteBatch2 tablosuna eksik sütunları ekle
                AddColumnIfNotExists(connection, "ConcreteBatch2s", "Status", "VARCHAR(50) NOT NULL DEFAULT 'yatay_kovada'");
                AddColumnIfNotExists(connection, "ConcreteBatch2s", "CompletedAt", "TIMESTAMP WITH TIME ZONE");
                
                AddColumnIfNotExists(connection, "CementConsumptions", "SiloId", "INTEGER NOT NULL");
                AddColumnIfNotExists(connection, "CementConsumptions", "ConsumedAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementConsumptions", "ConsumedAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "CementConsumptions", "BatchId", "INTEGER");
                AddColumnIfNotExists(connection, "CementConsumptions", "MixerId", "INTEGER NOT NULL");
                AddColumnIfNotExists(connection, "CementConsumptions", "RemainingAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementConsumptions", "ConsumptionType", "VARCHAR(50) NOT NULL DEFAULT 'Production'");
                AddColumnIfNotExists(connection, "CementConsumptions", "Notes", "VARCHAR(500)");
                
                AddColumnIfNotExists(connection, "CementRefills", "SiloId", "INTEGER NOT NULL");
                AddColumnIfNotExists(connection, "CementRefills", "AddedAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementRefills", "RefilledAt", "TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()");
                AddColumnIfNotExists(connection, "CementRefills", "PreviousAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementRefills", "NewAmount", "DOUBLE PRECISION NOT NULL DEFAULT 0");
                AddColumnIfNotExists(connection, "CementRefills", "OperatorName", "VARCHAR(100)");
                AddColumnIfNotExists(connection, "CementRefills", "ShipmentNumber", "VARCHAR(100)");
                AddColumnIfNotExists(connection, "CementRefills", "Supplier", "VARCHAR(100)");
                AddColumnIfNotExists(connection, "CementRefills", "Notes", "VARCHAR(500)");
                
                // ShiftRecords tablosuna MoldProductionJson sütunu ekle
                AddColumnIfNotExists(connection, "ShiftRecords", "MoldProductionJson", "TEXT");
                
                // ShiftRecords tablosuna malzeme detay sütunları ekle
                AddColumnIfNotExists(connection, "ShiftRecords", "Mixer1MaterialsJson", "TEXT");
                AddColumnIfNotExists(connection, "ShiftRecords", "Mixer2MaterialsJson", "TEXT");
                AddColumnIfNotExists(connection, "ShiftRecords", "TotalMaterialsJson", "TEXT");
                
                // Mevcut kayıtlarda NULL olan malzeme JSON sütunlarını boş string ile güncelle
                UpdateNullColumnsToEmptyString(connection, "ShiftRecords", "Mixer1MaterialsJson");
                UpdateNullColumnsToEmptyString(connection, "ShiftRecords", "Mixer2MaterialsJson");
                UpdateNullColumnsToEmptyString(connection, "ShiftRecords", "TotalMaterialsJson");
                
                // Mevcut batch'larda CompletedAt değerini OccurredAt ile doldur
                UpdateCompletedAtFromOccurredAt(connection, "ConcreteBatches");
                UpdateCompletedAtFromOccurredAt(connection, "ConcreteBatch2s");
                
                Console.WriteLine("Eksik sütunlar kontrol edildi ve eklendi.");
                
                // ShiftMoldRecord tablosunu oluştur
                CreateShiftMoldRecordTable(connection);
                
                // ActiveShift tablosunu oluştur
                CreateActiveShiftTable(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eksik sütunlar eklenirken hata: {ex.Message}");
            }
        }

        /// <summary>
        /// Mevcut batch'larda CompletedAt değerini OccurredAt ile doldur
        /// </summary>
        private static void UpdateCompletedAtFromOccurredAt(IDbConnection connection, string tableName)
        {
            try
            {
                var updateCommand = connection.CreateCommand();
                // PostgreSQL için tablo adını tırnak içine alarak case sensitivity sorununu çöz
                updateCommand.CommandText = $"UPDATE \"{tableName}\" SET \"CompletedAt\" = \"OccurredAt\" WHERE \"CompletedAt\" IS NULL AND \"Status\" = 'Tamamlandı'";
                var rowsAffected = updateCommand.ExecuteNonQuery();
                Console.WriteLine($"Updated {rowsAffected} rows in {tableName}.CompletedAt from OccurredAt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating CompletedAt in {tableName}: {ex.Message}");
            }
        }

        /// <summary>
        /// NULL olan sütunları boş string ile günceller
        /// </summary>
        private static void UpdateNullColumnsToEmptyString(IDbConnection connection, string tableName, string columnName)
        {
            try
            {
                var updateCommand = connection.CreateCommand();
                // PostgreSQL için tablo adını tırnak içine alarak case sensitivity sorununu çöz
                updateCommand.CommandText = $"UPDATE \"{tableName}\" SET \"{columnName}\" = '' WHERE \"{columnName}\" IS NULL";
                updateCommand.ExecuteNonQuery();
                Console.WriteLine($"Updated NULL values in {tableName}.{columnName} to empty strings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating NULL values in {tableName}.{columnName}: {ex.Message}");
            }
        }

        private static void CreateShiftMoldRecordTable(IDbConnection connection)
        {
            try
            {
                Console.WriteLine("[DatabaseFixer] ShiftMoldRecord tablosu kontrol ediliyor...");
                
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ShiftMoldRecords';";
                
                bool tableExists = false;
                using (var reader = checkTableCommand.ExecuteReader())
                {
                    tableExists = reader.Read();
                }
                
                if (!tableExists)
                {
                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = @"
                        CREATE TABLE ""ShiftMoldRecords"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""ShiftId"" INTEGER NOT NULL,
                            ""MoldId"" INTEGER NOT NULL,
                            ""MoldName"" VARCHAR(100) NOT NULL,
                            ""OperatorName"" VARCHAR(100) NOT NULL,
                            ""StartTime"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            ""EndTime"" TIMESTAMP WITH TIME ZONE,
                            ""StartProductionCount"" INTEGER NOT NULL DEFAULT 0,
                            ""EndProductionCount"" INTEGER NOT NULL DEFAULT 0,
                            ""ProductionCount"" INTEGER NOT NULL DEFAULT 0,
                            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                        );";
                    
                    createTableCommand.ExecuteNonQuery();
                    Console.WriteLine("[DatabaseFixer] ShiftMoldRecords tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("[DatabaseFixer] ShiftMoldRecords tablosu zaten mevcut.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseFixer] ShiftMoldRecord tablosu oluşturulurken hata: {ex.Message}");
            }
        }

        private static void CreateActiveShiftTable(IDbConnection connection)
        {
            try
            {
                Console.WriteLine("[DatabaseFixer] ActiveShift tablosu kontrol ediliyor...");
                
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = 'ActiveShifts';";
                
                bool tableExists = false;
                using (var reader = checkTableCommand.ExecuteReader())
                {
                    tableExists = reader.Read();
                }
                
                if (!tableExists)
                {
                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = @"
                        CREATE TABLE ""ActiveShifts"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""ShiftStartTime"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            ""OperatorName"" VARCHAR(100) NOT NULL,
                            ""ShiftRecordId"" INTEGER NOT NULL,
                            ""ProductionStartTime"" TIMESTAMP WITH TIME ZONE,
                            ""StartTotalProduction"" INTEGER NOT NULL DEFAULT 0,
                            ""StartDm452Value"" INTEGER,
                            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                        );";
                    
                    createTableCommand.ExecuteNonQuery();
                    Console.WriteLine("[DatabaseFixer] ActiveShifts tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine("[DatabaseFixer] ActiveShifts tablosu zaten mevcut.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseFixer] ActiveShift tablosu oluşturulurken hata: {ex.Message}");
            }
        }

        private static void AddColumnIfNotExists(IDbConnection connection, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = $@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = '{tableName}' AND column_name = '{columnName}';";
                
                bool columnExists = false;
                using (var reader = checkCommand.ExecuteReader())
                {
                    columnExists = reader.Read();
                }
                
                if (!columnExists)
                {
                    var addColumnCommand = connection.CreateCommand();
                    addColumnCommand.CommandText = $@"ALTER TABLE ""{tableName}"" ADD COLUMN ""{columnName}"" {columnDefinition};";
                    addColumnCommand.ExecuteNonQuery();
                    Console.WriteLine($"{tableName}.{columnName} sütunu başarıyla eklendi.");
                }
                else
                {
                    Console.WriteLine($"{tableName}.{columnName} sütunu zaten mevcut.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{tableName}.{columnName} sütunu eklenirken hata: {ex.Message}");
            }
        }

        private static void CreateTableIfNotExists(IDbConnection connection, string tableName, string createSql)
        {
            try
            {
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = $@"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_name = '{tableName}';";
                
                bool tableExists = false;
                using (var reader = checkCommand.ExecuteReader())
                {
                    tableExists = reader.Read();
                }
                
                if (!tableExists)
                {
                    var createCommand = connection.CreateCommand();
                    createCommand.CommandText = createSql;
                    createCommand.ExecuteNonQuery();
                    Console.WriteLine($"{tableName} tablosu başarıyla oluşturuldu.");
                }
                else
                {
                    Console.WriteLine($"{tableName} tablosu zaten mevcut.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{tableName} tablosu oluşturulurken hata: {ex.Message}");
            }
        }
    }
}
