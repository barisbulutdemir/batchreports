# Başka Bilgisayarda Veritabanı Düzeltme Scripti
# Bu script eksik tabloları ve kolonları otomatik düzeltecek

Write-Host "=== TAKIP VERİTABANI DÜZELTME SCRIPTİ ===" -ForegroundColor Green

# PostgreSQL bağlantı bilgileri
$host = "localhost"
$database = "takip_db"
$username = "postgres"
$password = "632536"

# PostgreSQL'in kurulu olup olmadığını kontrol et
Write-Host "PostgreSQL kontrol ediliyor..." -ForegroundColor Yellow
try {
    $psqlPath = Get-Command psql -ErrorAction Stop
    Write-Host "✅ PostgreSQL bulundu: $($psqlPath.Source)" -ForegroundColor Green
} catch {
    Write-Host "❌ PostgreSQL bulunamadı! Lütfen PostgreSQL'i kurun." -ForegroundColor Red
    Write-Host "İndirme linki: https://www.postgresql.org/download/windows/" -ForegroundColor Cyan
    exit 1
}

# Veritabanı bağlantısını test et
Write-Host "Veritabanı bağlantısı test ediliyor..." -ForegroundColor Yellow
$env:PGPASSWORD = $password

try {
    $testConnection = psql -h $host -U $username -d $database -c "SELECT 1;" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Veritabanı bağlantısı başarılı" -ForegroundColor Green
    } else {
        Write-Host "❌ Veritabanı bağlantısı başarısız!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Veritabanı bağlantısı hatası: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Eksik tabloları ve kolonları düzelt
Write-Host "`nEksik tablolar ve kolonlar düzeltiliyor..." -ForegroundColor Yellow

$fixScript = @"
-- Eksik kolonları ekle
DO \$\$
BEGIN
    -- ShiftRecords tablosuna eksik kolonları ekle
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'MoldProductionJson') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "MoldProductionJson" TEXT;
        RAISE NOTICE 'MoldProductionJson kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'Mixer1MaterialsJson') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "Mixer1MaterialsJson" TEXT;
        RAISE NOTICE 'Mixer1MaterialsJson kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'Mixer2MaterialsJson') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "Mixer2MaterialsJson" TEXT;
        RAISE NOTICE 'Mixer2MaterialsJson kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'TotalMaterialsJson') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "TotalMaterialsJson" TEXT;
        RAISE NOTICE 'TotalMaterialsJson kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'TotalProduction') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "TotalProduction" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'TotalProduction kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'ProductionStartTime') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "ProductionStartTime" TIMESTAMP WITH TIME ZONE;
        RAISE NOTICE 'ProductionStartTime kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'ShiftDurationMinutes') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "ShiftDurationMinutes" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'ShiftDurationMinutes kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ShiftRecords' AND column_name = 'ProductionDurationMinutes') THEN
        ALTER TABLE "ShiftRecords" ADD COLUMN "ProductionDurationMinutes" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'ProductionDurationMinutes kolonu eklendi';
    END IF;
    
    -- Molds tablosuna eksik kolonları ekle
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Molds' AND column_name = 'IsActive') THEN
        ALTER TABLE "Molds" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT FALSE;
        RAISE NOTICE 'Molds.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Molds' AND column_name = 'TotalPrints') THEN
        ALTER TABLE "Molds" ADD COLUMN "TotalPrints" INTEGER NOT NULL DEFAULT 0;
        RAISE NOTICE 'Molds.TotalPrints kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Molds' AND column_name = 'UpdatedAt') THEN
        ALTER TABLE "Molds" ADD COLUMN "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW();
        RAISE NOTICE 'Molds.UpdatedAt kolonu eklendi';
    END IF;
    
    -- ConcreteBatches tablosuna eksik kolonları ekle
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'LoadcellWaterKg') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "LoadcellWaterKg" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'ConcreteBatches.LoadcellWaterKg kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'PulseWaterKg') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "PulseWaterKg" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'ConcreteBatches.PulseWaterKg kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'TotalCementKg') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "TotalCementKg" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'ConcreteBatches.TotalCementKg kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'TotalAggregateKg') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "TotalAggregateKg" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'ConcreteBatches.TotalAggregateKg kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'TotalAdmixtureKg') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "TotalAdmixtureKg" DOUBLE PRECISION NOT NULL DEFAULT 0;
        RAISE NOTICE 'ConcreteBatches.TotalAdmixtureKg kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatches' AND column_name = 'CompletedAt') THEN
        ALTER TABLE "ConcreteBatches" ADD COLUMN "CompletedAt" TIMESTAMP WITH TIME ZONE;
        RAISE NOTICE 'ConcreteBatches.CompletedAt kolonu eklendi';
    END IF;
    
    -- ConcreteBatch2s tablosuna eksik kolonları ekle
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatch2s' AND column_name = 'Status') THEN
        ALTER TABLE "ConcreteBatch2s" ADD COLUMN "Status" VARCHAR(50) NOT NULL DEFAULT 'yatay_kovada';
        RAISE NOTICE 'ConcreteBatch2s.Status kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ConcreteBatch2s' AND column_name = 'CompletedAt') THEN
        ALTER TABLE "ConcreteBatch2s" ADD COLUMN "CompletedAt" TIMESTAMP WITH TIME ZONE;
        RAISE NOTICE 'ConcreteBatch2s.CompletedAt kolonu eklendi';
    END IF;
    
    -- Alias tablolarına IsActive kolonları ekle
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CementAliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "CementAliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'CementAliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AggregateAliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "AggregateAliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'AggregateAliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AdmixtureAliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "AdmixtureAliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'AdmixtureAliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cement2Aliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "Cement2Aliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'Cement2Aliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Aggregate2Aliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "Aggregate2Aliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'Aggregate2Aliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Admixture2Aliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "Admixture2Aliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'Admixture2Aliases.IsActive kolonu eklendi';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Pigment2Aliases' AND column_name = 'IsActive') THEN
        ALTER TABLE "Pigment2Aliases" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
        RAISE NOTICE 'Pigment2Aliases.IsActive kolonu eklendi';
    END IF;
    
    -- NULL değerleri boş string ile güncelle
    UPDATE "ShiftRecords" SET "MoldProductionJson" = '' WHERE "MoldProductionJson" IS NULL;
    UPDATE "ShiftRecords" SET "Mixer1MaterialsJson" = '' WHERE "Mixer1MaterialsJson" IS NULL;
    UPDATE "ShiftRecords" SET "Mixer2MaterialsJson" = '' WHERE "Mixer2MaterialsJson" IS NULL;
    UPDATE "ShiftRecords" SET "TotalMaterialsJson" = '' WHERE "TotalMaterialsJson" IS NULL;
    
    RAISE NOTICE 'Tüm eksik kolonlar kontrol edildi ve eklendi';
END
\$\$;
"@

try {
    echo $fixScript | psql -h $host -U $username -d $database
    Write-Host "✅ Eksik kolonlar düzeltildi" -ForegroundColor Green
} catch {
    Write-Host "❌ Kolon düzeltme hatası: $($_.Exception.Message)" -ForegroundColor Red
}

# Temel alias verilerini ekle
Write-Host "`nTemel alias verileri ekleniyor..." -ForegroundColor Yellow

$aliasScript = @"
-- Mixer1 Alias'ları
INSERT INTO "CementAliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Çimento 1', true),
(2, 'Çimento 2', true),
(3, 'Çimento 3', true)
ON CONFLICT ("Slot") DO NOTHING;

INSERT INTO "AggregateAliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Agrega 1', true),
(2, 'Agrega 2', true),
(3, 'Agrega 3', true),
(4, 'Agrega 4', true),
(5, 'Agrega 5', true)
ON CONFLICT ("Slot") DO NOTHING;

INSERT INTO "AdmixtureAliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Katkı 1', true),
(2, 'Katkı 2', true),
(3, 'Katkı 3', true),
(4, 'Katkı 4', true)
ON CONFLICT ("Slot") DO NOTHING;

-- Mixer2 Alias'ları
INSERT INTO "Cement2Aliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Çimento 1', true),
(2, 'Çimento 2', true),
(3, 'Çimento 3', true)
ON CONFLICT ("Slot") DO NOTHING;

INSERT INTO "Aggregate2Aliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Agrega 1', true),
(2, 'Agrega 2', true),
(3, 'Agrega 3', true),
(4, 'Agrega 4', true),
(5, 'Agrega 5', true),
(6, 'Agrega 6', true),
(7, 'Agrega 7', true),
(8, 'Agrega 8', true)
ON CONFLICT ("Slot") DO NOTHING;

INSERT INTO "Admixture2Aliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Katkı 1', true),
(2, 'Katkı 2', true),
(3, 'Katkı 3', true),
(4, 'Katkı 4', true)
ON CONFLICT ("Slot") DO NOTHING;

INSERT INTO "Pigment2Aliases" ("Slot", "Name", "IsActive") VALUES 
(1, 'Pigment 1', true),
(2, 'Pigment 2', true),
(3, 'Pigment 3', true),
(4, 'Pigment 4', true)
ON CONFLICT ("Slot") DO NOTHING;

-- Temel kalıp verileri
INSERT INTO "Molds" ("Name", "Code", "IsActive", "TotalPrints", "Description") VALUES
('Standart Kalıp', 'MOLD001', true, 0, 'Standart beton kalıbı'),
('Büyük Kalıp', 'MOLD002', false, 0, 'Büyük boyutlu beton kalıbı'),
('Küçük Kalıp', 'MOLD003', false, 0, 'Küçük boyutlu beton kalıbı')
ON CONFLICT ("Code") DO NOTHING;
"@

try {
    echo $aliasScript | psql -h $host -U $username -d $database
    Write-Host "✅ Temel alias verileri eklendi" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Alias verileri eklenemedi: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Veritabanı durumunu kontrol et
Write-Host "`nVeritabanı durumu kontrol ediliyor..." -ForegroundColor Yellow

$checkScript = @"
-- Tablo sayılarını kontrol et
SELECT 'CementAliases' as table_name, COUNT(*) as count FROM "CementAliases"
UNION ALL
SELECT 'AggregateAliases', COUNT(*) FROM "AggregateAliases"
UNION ALL
SELECT 'AdmixtureAliases', COUNT(*) FROM "AdmixtureAliases"
UNION ALL
SELECT 'Cement2Aliases', COUNT(*) FROM "Cement2Aliases"
UNION ALL
SELECT 'Aggregate2Aliases', COUNT(*) FROM "Aggregate2Aliases"
UNION ALL
SELECT 'Admixture2Aliases', COUNT(*) FROM "Admixture2Aliases"
UNION ALL
SELECT 'Pigment2Aliases', COUNT(*) FROM "Pigment2Aliases"
UNION ALL
SELECT 'Molds', COUNT(*) FROM "Molds"
ORDER BY table_name;
"@

try {
    Write-Host "`nTablo Durumu:" -ForegroundColor Cyan
    echo $checkScript | psql -h $host -U $username -d $database
} catch {
    Write-Host "⚠️ Durum kontrolü yapılamadı" -ForegroundColor Yellow
}

Write-Host "`n=== DÜZELTME TAMAMLANDI ===" -ForegroundColor Green
Write-Host "Artık uygulamayı çalıştırabilirsiniz!" -ForegroundColor Cyan
Write-Host "Alias'lar ve shift raporları artık çalışacak." -ForegroundColor Cyan

# appsettings.json kontrolü
$appSettingsPath = "appsettings.json"
if (Test-Path $appSettingsPath) {
    Write-Host "`n✅ appsettings.json dosyası mevcut" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ appsettings.json dosyası bulunamadı!" -ForegroundColor Yellow
    Write-Host "Uygulama varsayılan ayarları kullanacak." -ForegroundColor Cyan
}

Write-Host "`nBağlantı bilgileri:" -ForegroundColor White
Write-Host "Host: $host" -ForegroundColor White
Write-Host "Database: $database" -ForegroundColor White
Write-Host "Username: $username" -ForegroundColor White
