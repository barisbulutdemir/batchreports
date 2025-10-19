using System;
using System.Data;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using takip.Data;

namespace takip
{
    public class DatabaseChecker
    {
        public static void CheckDatabase()
        {
            var connectionString = "Host=localhost;Database=takip_db;Username=postgres;Password=632536";
            
            try
            {
                DetailedLogger.LogInfo("=== VERİTABANI KONTROLÜ ===");
                DetailedLogger.LogInfo($"Bağlantı string: {connectionString}");
                
                // PostgreSQL bağlantısını test et
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                DetailedLogger.LogInfo("✅ PostgreSQL bağlantısı başarılı!");
                
                // Mevcut tabloları listele
                DetailedLogger.LogInfo("\n=== MEVCUT TABLOLAR ===");
                var tableCommand = new NpgsqlCommand(@"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    ORDER BY table_name;", connection);
                
                using var reader = tableCommand.ExecuteReader();
                var tables = new List<string>();
                while (reader.Read())
                {
                    var tableName = reader.GetString(0);
                    tables.Add(tableName);
                    DetailedLogger.LogInfo($"- {tableName}");
                }
                reader.Close();
                
                // Çimento silo tablolarını kontrol et
                DetailedLogger.LogInfo("\n=== ÇİMENTO SİLO TABLOLARI KONTROLÜ ===");
                var cementTables = new[] { "CementSilos", "CementConsumptions", "CementRefills" };
                
                foreach (var table in cementTables)
                {
                    if (tables.Contains(table))
                    {
                        DetailedLogger.LogInfo($"✅ {table} tablosu mevcut");
                        
                        // Tablo yapısını kontrol et
                        var structureCommand = new NpgsqlCommand($@"
                            SELECT column_name, data_type, is_nullable 
                            FROM information_schema.columns 
                            WHERE table_name = '{table}' 
                            ORDER BY ordinal_position;", connection);
                        
                        using var structureReader = structureCommand.ExecuteReader();
                        DetailedLogger.LogInfo($"   Sütunlar:");
                        while (structureReader.Read())
                        {
                            var columnName = structureReader.GetString(0);
                            var dataType = structureReader.GetString(1);
                            var isNullable = structureReader.GetString(2);
                            DetailedLogger.LogInfo($"   - {columnName}: {dataType} (nullable: {isNullable})");
                        }
                        structureReader.Close();
                    }
                    else
                    {
                        DetailedLogger.LogInfo($"❌ {table} tablosu EKSİK!");
                    }
                }
                
                // Entity Framework ile de test et
                DetailedLogger.LogInfo("\n=== ENTITY FRAMEWORK TEST ===");
                using var context = new ProductionDbContext();
                var canConnect = context.Database.CanConnect();
                DetailedLogger.LogInfo($"Entity Framework bağlantısı: {(canConnect ? "✅ Başarılı" : "❌ Başarısız")}");
                
                if (canConnect)
                {
                    try
                    {
                        var siloCount = context.CementSilos.Count();
                        DetailedLogger.LogInfo($"CementSilos tablosunda {siloCount} kayıt var");
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogError($"❌ CementSilos tablosu hatası: {ex.Message}", ex);
                    }
                }
                
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"❌ HATA: {ex.Message}", ex);
            }
        }
    }
}
