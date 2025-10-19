# Beton Santrali Sistem Mimarisi

## Mixer1 vs Mixer2 Bağımsız Sistemler

### Ortak Kullanılan Sistemler
- ✅ **Çimento Siloları** (`CementSilo`, `CementConsumption`)
- ✅ **PLC Ayarları** (`PlcSettings`)
- ✅ **Operatör Yönetimi** (`Operator`)

### Mixer1 Sistemi (Mevcut)
- **Model:** `ConcreteBatch`, `ConcreteBatchCement`, `ConcreteBatchAggregate`, `ConcreteBatchAdmixture`
- **Özellikler:** 5 agrega, 3 çimento, 2 su, 4 katkı, 1 pigment
- **Servisi:** `ConcreteSimulation`, `ConcretePlcReader.GetSnapshotAsync()`
- **Alias Sistemi:** `CementAlias`, `AggregateAlias`, `AdmixtureAlias`, `PigmentAlias`

### Mixer2 Sistemi (Geri Getirilecek)
- **Model:** `ConcreteBatch2`, `ConcreteBatch2Cement`, `ConcreteBatch2Aggregate`, `ConcreteBatch2Admixture`
- **Özellikler:** 8 agrega, 3 çimento, 2 su, 4 katkı, 4 pigment
- **Servisi:** `ConcretePlc2Simulator`, `ConcretePlcReader.GetSnapshot2Async()`
- **Alias Sistemi:** `Cement2Alias`, `Aggregate2Alias`, `Admixture2Alias`, `Pigment2Alias`
- **Akış Takibi:** `Material`, `MaterialEvent` (Yatay Kova → Dikey Kova → Bunker → Mixer)

### Çimento Silo Paylaşımı
- **Mixer1:** `MixerId = 1` ile CementConsumption kaydı
- **Mixer2:** `MixerId = 2` ile CementConsumption kaydı
- **Aynı CementSilo tablosundan çimento çekiliyor**

### Doğru Yaklaşım
1. Mixer2 modellerini ve servislerini geri getir
2. Çimento silo paylaşımını koru
3. Her mixer'ın kendi batch ID'si olsun
4. Akış takibi sadece Mixer2 için çalışsın (8 agrega ile)
