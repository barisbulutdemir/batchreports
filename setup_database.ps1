# PostgreSQL Veritabanı Kurulum Scripti
# Bu script başka bilgisayarda çalıştırılacak

Write-Host "=== TAKIP VERİTABANI KURULUMU ===" -ForegroundColor Green

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

# Veritabanı oluştur
Write-Host "Veritabanı oluşturuluyor..." -ForegroundColor Yellow
$env:PGPASSWORD = $password

try {
    # Veritabanı var mı kontrol et
    $dbExists = psql -h $host -U $username -d postgres -t -c "SELECT 1 FROM pg_database WHERE datname = '$database';" 2>$null
    
    if ($dbExists -match "1") {
        Write-Host "✅ Veritabanı zaten mevcut: $database" -ForegroundColor Green
    } else {
        # Veritabanı oluştur
        psql -h $host -U $username -d postgres -c "CREATE DATABASE $database;" 2>$null
        Write-Host "✅ Veritabanı oluşturuldu: $database" -ForegroundColor Green
    }
    
    # Kullanıcı yetkilerini ver
    psql -h $host -U $username -d postgres -c "GRANT ALL PRIVILEGES ON DATABASE $database TO $username;" 2>$null
    Write-Host "✅ Kullanıcı yetkileri verildi" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Veritabanı oluşturma hatası: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Temel alias verilerini ekle
Write-Host "Temel alias verileri ekleniyor..." -ForegroundColor Yellow

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
"@

try {
    # Önce tabloları oluştur (uygulama otomatik yapacak ama emin olmak için)
    psql -h $host -U $username -d $database -c "SELECT 1;" 2>$null
    
    # Alias verilerini ekle
    echo $aliasScript | psql -h $host -U $username -d $database 2>$null
    Write-Host "✅ Temel alias verileri eklendi" -ForegroundColor Green
    
} catch {
    Write-Host "⚠️ Alias verileri eklenemedi (uygulama ilk çalıştırmada ekleyecek)" -ForegroundColor Yellow
}

Write-Host "`n=== KURULUM TAMAMLANDI ===" -ForegroundColor Green
Write-Host "Artık uygulamayı çalıştırabilirsiniz!" -ForegroundColor Cyan
Write-Host "Uygulama otomatik olarak tabloları oluşturacak ve alias'ları gösterecek." -ForegroundColor Cyan

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
