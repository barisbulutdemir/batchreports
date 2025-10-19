-- Mold tablosunu oluştur
CREATE TABLE IF NOT EXISTS "Molds" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "IsActive" BOOLEAN NOT NULL DEFAULT FALSE,
    "TotalPrints" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Description" VARCHAR(500)
);

-- Index oluştur
CREATE INDEX IF NOT EXISTS "IX_Molds_Code" ON "Molds" ("Code");

-- Örnek veri ekle
INSERT INTO "Molds" ("Name", "Code", "IsActive", "TotalPrints", "Description") VALUES
('Standart Kalıp', 'MOLD001', true, 0, 'Standart beton kalıbı'),
('Büyük Kalıp', 'MOLD002', false, 0, 'Büyük boyutlu beton kalıbı'),
('Küçük Kalıp', 'MOLD003', false, 0, 'Küçük boyutlu beton kalıbı')
ON CONFLICT ("Code") DO NOTHING;





