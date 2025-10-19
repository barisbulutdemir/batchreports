# Takip - Beton Üretim Takip Sistemi

## 📋 Proje Hakkında

Bu proje, beton üretim tesislerinde Mixer 1 ve Mixer 2 sistemlerinin takibini yapan kapsamlı bir WPF uygulamasıdır. PLC verilerini okuyarak gerçek zamanlı üretim takibi, malzeme tüketimi analizi ve vardiya raporları oluşturur.

## 🚀 Özellikler

### ✅ Son Güncellemeler (v1.0.0)
- **Mixer 2 Pigment Kayıt Sistemi:** Debug logları eklendi, pigment kg değerleri kontrol edilebilir
- **Vardiya Sonu Raporları:** Eksik malzeme detayları (agrega, katkı) düzeltildi
- **Alias Yönetimi:** Ayarlar penceresinde "Initialize Default Names" butonu eklendi
- **Tip Uyumsuzluğu:** Alias verilerinde int/short tip hatası düzeltildi

### 🔧 Ana Özellikler
- **Gerçek Zamanlı PLC Veri Okuma:** Omron PLC'den H ve DM register'larını okuma
- **İki Mixer Desteği:** Mixer 1 ve Mixer 2 için ayrı takip sistemleri
- **Malzeme Takibi:** Çimento, agrega, katkı, pigment tüketimi
- **Vardiya Yönetimi:** Otomatik vardiya başlatma/bitirme
- **PDF Raporları:** Detaylı üretim ve malzeme raporları
- **Alias Sistemi:** Malzeme isimlerini özelleştirme
- **Çoklu Dil Desteği:** Türkçe/İngilizce

## 🛠️ Teknoloji Stack

- **Framework:** .NET 8.0 WPF
- **Veritabanı:** SQLite (Entity Framework Core)
- **PLC İletişimi:** Omron FINS/UDP
- **PDF Oluşturma:** iTextSharp
- **UI:** XAML, Material Design

## 📦 Kurulum

### Gereksinimler
- Windows 10/11
- .NET 8.0 Runtime (Framework-Dependent için)

### İndirme Seçenekleri

#### Framework-Dependent (~11MB)
- .NET 8.0 Runtime gerekli
- Küçük boyut, hızlı indirme
- [İndir](https://github.com/barisbulutdemir/batchreports/releases)

#### Self-Contained (~77MB)
- Hiçbir ek runtime gerekmez
- Bağımsız çalışır
- [İndir](https://github.com/barisbulutdemir/batchreports/releases)

## 🚀 İlk Kurulum

1. **Programı İndir ve Çalıştır**
2. **Ayarlar Penceresini Aç**
3. **"Material Names" Sekmesine Git**
4. **"🚀 Initialize Default Names" Butonuna Tıkla**
5. **Onay Dialog'unda "Yes" De**

## 📁 Proje Yapısı

```
takip/
├── Data/                    # Veritabanı context
├── Models/                  # Veri modelleri
├── Services/                # İş mantığı servisleri
├── Migrations/              # EF Core migrations
├── Resources/               # Çoklu dil kaynakları
├── Enums/                   # Enum tanımları
├── Utils/                   # Yardımcı sınıflar
├── *.xaml                   # UI tanımları
├── *.xaml.cs               # UI kod-behind
└── takip.csproj            # Proje dosyası
```

## 🔧 Geliştirme

### Gereksinimler
- Visual Studio 2022
- .NET 8.0 SDK
- SQLite

### Build
```bash
dotnet build
```

### Publish
```bash
# Framework-Dependent
dotnet publish -c Release -r win-x64 --self-contained false

# Self-Contained
dotnet publish -c Release -r win-x64 --self-contained true
```

## 📊 Sistem Mimarisi

### Mixer 1
- 3 Çimento slotu
- 5 Agrega slotu
- 4 Katkı slotu
- 1 Pigment slotu

### Mixer 2
- 3 Çimento slotu
- 8 Agrega slotu
- 4 Katkı slotu
- 4 Pigment slotu

## 🔗 PLC Register Mapping

### H Register'ları
- `H31.2`: Pigment Group Active
- `H31.10-H34.10`: Pigment Active (1-4)
- `H31.3-H34.3`: Pigment TartimOk (1-4)

### DM Register'ları
- `DM308`: Pigment1Kg
- `DM310`: Pigment2Kg
- `DM312`: Pigment3Kg
- `DM314`: Pigment4Kg

## 📝 Lisans

Bu proje özel kullanım için geliştirilmiştir.

## 👨‍💻 Geliştirici

**Barış Bulut Demir**
- GitHub: [@barisbulutdemir](https://github.com/barisbulutdemir)

## 📞 Destek

Sorunlar için GitHub Issues kullanın veya geliştirici ile iletişime geçin.
