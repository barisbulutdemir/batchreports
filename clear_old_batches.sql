-- Eski Batch Kayıtlarını Temizleme Script'i
-- ==========================================
-- Bu script tüm test/eski batch kayıtlarını siler
-- UYARI: Bu işlem geri alınamaz!

BEGIN;

-- 1. Mixer1 Batch'larını sil (CASCADE ile ilgili tablolar da silinir)
DELETE FROM "ConcreteBatches";
-- Otomatik silinen tablolar:
--   - ConcreteBatchCements
--   - ConcreteBatchAggregates  
--   - ConcreteBatchAdmixtures

-- 2. Mixer2 Batch'larını sil (CASCADE ile ilgili tablolar da silinir)
DELETE FROM "ConcreteBatches2";
-- Otomatik silinen tablolar:
--   - ConcreteBatch2Cements
--   - ConcreteBatch2Aggregates
--   - ConcreteBatch2Admixtures

-- 3. Auto-increment ID'leri sıfırla (yeni batch'lar 1'den başlasın)
ALTER SEQUENCE "ConcreteBatches_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "ConcreteBatches2_Id_seq" RESTART WITH 1;

-- 4. PlcDataSnapshots tablosunu temizle (opsiyonel - ham PLC verileri)
DELETE FROM "PlcDataSnapshots";
ALTER SEQUENCE "PlcDataSnapshots_Id_seq" RESTART WITH 1;

COMMIT;

-- ✅ Başarıyla tamamlandı!
-- Artık tüm eski batch kayıtları temizlendi.

