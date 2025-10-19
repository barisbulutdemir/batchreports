# Mixer1 Batch Kaydı Test Senaryosu

## Test Adımları (Sıralı)

### 1. Mixer1TestWindow'u Aç
- MainWindow'dan "🧪 Mixer1 Test" butonuna tıkla

### 2. Çoklu Agrega Batch Oluşturma Testi (Yeni Sistem)
**Amaç:** Tüm aktif agregalar için batch kaydı oluşturmak

**Senaryo A: Tek Agrega (Basit Test)**
1. **Reset** butonuna tıkla (temiz başlangıç)
2. **Agrega1Aktif** checkbox'ını işaretle
3. **Agrega1TartimOk** checkbox'ını işaretle (Rising edge)
4. **Test** butonuna tıkla
5. Log'da "🔍 Aktif Agregalar (1): A1:true/true" mesajını kontrol et
6. Log'da "🔥 BATCH KOŞULU SAĞLANDI!" mesajını kontrol et
7. Log'da "✅ Yeni batch oluşturuldu" mesajını kontrol et

**Senaryo B: Çoklu Agrega (Gelişmiş Test)**
1. **Reset** butonuna tıkla (temiz başlangıç)
2. **Agrega1Aktif**, **Agrega2Aktif**, **Agrega3Aktif** checkbox'larını işaretle
3. **Agrega1TartimOk** checkbox'ını işaretle (İlk rising edge)
4. **Test** butonuna tıkla
5. Log'da "⏳ Tartım OK bekleniyor: A2, A3" mesajını kontrol et
6. **Agrega2TartimOk** checkbox'ını işaretle
7. **Test** butonuna tıkla
8. Log'da "⏳ Tartım OK bekleniyor: A3" mesajını kontrol et
9. **Agrega3TartimOk** checkbox'ını işaretle (Son agrega)
10. **Test** butonuna tıkla
11. Log'da "🔥 BATCH KOŞULU SAĞLANDI!" mesajını kontrol et
12. Log'da "✅ Batch oluşturuldu - 3 agrega kaydedildi" mesajını kontrol et

### 3. Bekleme Bunker Geçişi
**Amaç:** Batch'i "Bekleme Bunkerinde" durumuna geçirmek

**Adımlar:**
1. **WaitingBunker** checkbox'ını işaretle
2. **Test** butonuna tıkla
3. Log'da "Tartım kovası → Bekleme bunkeri" mesajını kontrol et

### 4. Mixer İçine Geçiş
**Amaç:** Batch'i "Mixerde" durumuna geçirmek

**Adımlar:**
1. **WaitingBunker** checkbox'ını kaldır (pasif yap)
2. **MixerAgregaVar** checkbox'ını işaretle
3. **Test** butonuna tıkla
4. Log'da "Bekleme bunkeri → Mixer içi" mesajını kontrol et

### 5. Çimento Ekleme
**Amaç:** Çimento verilerini batch'e kaydetmek

**Adımlar:**
1. **Cimento1Aktif** checkbox'ını işaretle
2. **MixerCimentoVar** checkbox'ını işaretle
3. **Test** butonuna tıkla
4. Log'da "Mixer içi çimento tespit edildi - Kaydedildi" mesajını kontrol et

### 6. Katkı Ekleme (Opsiyonel)
**Amaç:** Katkı verilerini batch'e kaydetmek

**Adımlar:**
1. **Katki1Aktif** checkbox'ını işaretle
2. **MixerKatkiVar** checkbox'ını işaretle
3. **Test** butonuna tıkla
4. Log'da "Mixer içi katkı tespit edildi - Kaydedildi" mesajını kontrol et

### 7. Su Ekleme (Opsiyonel)
**Amaç:** Su verilerini batch'e kaydetmek

**Adımlar:**
1. **MixerLoadcellSuVar** checkbox'ını işaretle
2. **Test** butonuna tıkla
3. Log'da "Mixer içi loadcell su tespit edildi - Kaydedildi" mesajını kontrol et

### 8. Batch Tamamlama
**Amaç:** Batch'i tamamlamak

**Adımlar:**
1. **HarcHazir** checkbox'ını işaretle
2. **Test** butonuna tıkla
3. Log'da "Harç hazır - Batch tamamlandı! Status: Tamamlandı" mesajını kontrol et

## Beklenen Sonuçlar

### Database Kontrolleri
1. **ConcreteBatches** tablosunda yeni kayıt oluşmalı
2. **ConcreteBatchAggregates** tablosunda agrega kaydı olmalı
3. **ConcreteBatchCements** tablosunda çimento kaydı olmalı
4. **ConcreteBatchAdmixtures** tablosunda katkı kaydı olmalı (eğer eklendiyse)

### Status Geçişleri
1. "Tartım Kovasında" → "Bekleme Bunkerinde" → "Mixerde" → "Tamamlandı"

### Log Mesajları
- ✅ "AGREGA1 RISING EDGE DETECTED!"
- ✅ "Yeni batch oluşturuldu"
- ✅ "Tartım kovası → Bekleme bunkeri"
- ✅ "Bekleme bunkeri → Mixer içi"
- ✅ "Mixer içi çimento tespit edildi"
- ✅ "Harç hazır - Batch tamamlandı!"

## Sorun Giderme

### Batch Oluşmuyorsa:
- Edge detection sıfırla: Mixer1StatusBasedProcessor.ResetEdgeDetection()
- Agrega1TartimOk'u önce false, sonra true yap (rising edge)

### Database Hatası Alıyorsa:
- ProductionDbContext bağlantısını kontrol et
- ConcreteBatches tablosunun var olduğunu kontrol et

### Log Görünmüyorsa:
- Mixer1StatusBasedProcessor.OnFlowEvent event'inin bağlı olduğunu kontrol et
- Test penceresi log alanını kontrol et

## Test Sonrası Kontroller

### 1. Batch Tracking Window
- "📊 Birleşik Batch Takip" penceresini aç
- Yeni oluşturulan batch'i listede gör
- Batch detaylarını kontrol et

### 2. Database Sorgusu
```sql
SELECT * FROM ConcreteBatches WHERE PlantCode = 'MIXER1' ORDER BY OccurredAt DESC LIMIT 1;
```

### 3. Concrete Reporting
- "🏗️ Mixer1 Raporlama" penceresini aç
- Son batch'leri listede gör
