using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using takip.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace takip.Services
{
    /// <summary>
    /// PDF export servisi - iTextSharp ile güvenilir PDF oluşturma
    /// </summary>
    public class PdfExportService
    {
        private readonly string _exportPath;

        public PdfExportService()
        {
            _exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Vardiya_Raporlari");
            
            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }
            
            // Program başlangıcında eski geçici dosyaları temizle
            CleanupOldTempFiles();
        }

        /// <summary>
        /// Program başlangıcında eski geçici PDF dosyalarını temizle
        /// </summary>
        private void CleanupOldTempFiles()
        {
            try
            {
                var tempFiles = Directory.GetFiles(_exportPath, "Temp_*.pdf");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                        DetailedLogger.LogInfo($"Eski geçici PDF dosyası silindi: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogWarning($"Geçici PDF silme hatası: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Geçici dosya temizleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Vardiya raporu PDF'i oluştur (2 sayfalı)
        /// </summary>
        public string CreateShiftReportPdf(ShiftRecord shiftRecord, List<ProductionNote>? notes = null)
        {
            try
            {
                var fileName = $"Vardiya_Raporu_{shiftRecord.ShiftStartTime:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_exportPath, fileName);

                DetailedLogger.LogInfo($"PDF oluşturuluyor: {filePath}");
                
                // Dosya yazma izni kontrolü
                try
                {
                    using (var testFile = File.Create(Path.Combine(_exportPath, "test_write.tmp")))
                    {
                        testFile.WriteByte(1);
                    }
                    File.Delete(Path.Combine(_exportPath, "test_write.tmp"));
                    DetailedLogger.LogInfo("Dosya yazma izni kontrol edildi: OK");
                }
                catch (Exception ex)
                {
                    DetailedLogger.LogError($"Dosya yazma izni hatası: {ex.Message}");
                    throw new Exception($"Dosya yazma izni yok: {ex.Message}", ex);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var document = new Document(PageSize.A4, 50, 50, 25, 25);
                    var writer = PdfWriter.GetInstance(document, fileStream);
                    
                    document.Open();

                    // SAYFA 1: Vardiya Bilgileri
                    CreateShiftReportPage1(document, shiftRecord, notes);

                    // SAYFA 2: Mixer Malzeme Detayları
                    document.NewPage();
                    CreateShiftReportPage2(document, shiftRecord);

                    document.Close();
                }

                DetailedLogger.LogInfo($"PDF başarıyla oluşturuldu: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"PDF oluşturma hatası: {ex.Message}");
                DetailedLogger.LogError($"Inner Exception: {ex.InnerException?.Message ?? "Yok"}");
                DetailedLogger.LogError($"Stack Trace: {ex.StackTrace}");
                throw new Exception($"PDF oluşturma hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sayfa 1: Vardiya bilgileri, taş üretimi ve notlar
        /// </summary>
        private void CreateShiftReportPage1(Document document, ShiftRecord shiftRecord, List<ProductionNote>? notes)
        {
            // Başlık
            Font titleFont;
            try
            {
                titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Font oluşturma hatası: {ex.Message}");
                titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 18, BaseColor.DARK_GRAY);
            }
            var title = new Paragraph("SHIFT PRODUCTION REPORT", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    document.Add(title);

                    // Vardiya bilgileri tablosu
                    var infoTable = CreateInfoTable(shiftRecord);
                    document.Add(infoTable);

                    // Taş üretim bilgileri
                    if (!string.IsNullOrEmpty(shiftRecord.StoneProductionJson))
                    {
                        var stoneProduction = ParseStoneProduction(shiftRecord.StoneProductionJson);
                        if (stoneProduction.Any())
                        {
                    document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
                            var stoneTable = CreateStoneProductionTable(stoneProduction);
                            document.Add(stoneTable);
                        }
                    }

            // Kalıp üretim bilgileri
            if (!string.IsNullOrEmpty(shiftRecord.MoldProductionJson))
            {
                document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
                var moldTable = CreateMoldProductionTable(shiftRecord.MoldProductionJson);
                document.Add(moldTable);
            }

                    // Vardiya notları
                    if (notes != null && notes.Any())
                    {
                document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
                        var notesTable = CreateNotesTable(notes);
                        document.Add(notesTable);
                    }

                    // Alt bilgi
            document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
                    var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);
                    var footer = new Paragraph(FixTurkishCharacters($"Rapor Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}"), footerFont)
                    {
                        Alignment = Element.ALIGN_RIGHT
                    };
                    document.Add(footer);
        }

        /// <summary>
        /// Sayfa 2: Mixer malzeme detayları
        /// </summary>
        private void CreateShiftReportPage2(Document document, ShiftRecord shiftRecord)
        {
            // Başlık
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
            var title = new Paragraph(FixTurkishCharacters("MIXER MALZEME DETAYLARI"), titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Mixer1 malzeme detayları
            if (!string.IsNullOrEmpty(shiftRecord.Mixer1MaterialsJson))
            {
                var mixer1Materials = ParseMaterialDetails(shiftRecord.Mixer1MaterialsJson);
                var mixer1Table = CreateMaterialDetailsTable("Mixer1", mixer1Materials);
                document.Add(mixer1Table);
                document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            }

            // Mixer2 malzeme detayları
            if (!string.IsNullOrEmpty(shiftRecord.Mixer2MaterialsJson))
            {
                var mixer2Materials = ParseMaterialDetails(shiftRecord.Mixer2MaterialsJson);
                var mixer2Table = CreateMaterialDetailsTable("Mixer2", mixer2Materials);
                document.Add(mixer2Table);
                document.Add(new Paragraph(" ", FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            }

            // Toplam malzeme detayları
            if (!string.IsNullOrEmpty(shiftRecord.TotalMaterialsJson))
            {
                var totalMaterials = ParseMaterialDetails(shiftRecord.TotalMaterialsJson);
                var totalTable = CreateMaterialDetailsTable("Toplam Malzeme Kullanımı", totalMaterials);
                document.Add(totalTable);
            }
        }

        /// <summary>
        /// Kalıp üretim tablosu oluştur
        /// </summary>
        private PdfPTable CreateMoldProductionTable(string moldProductionJson)
        {
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = 15
            };

            Font headerFont, dataFont;
            try
            {
                headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
                dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"Font oluşturma hatası: {ex.Message}");
                headerFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.WHITE);
                dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            }

            // Tablo başlığı
            var headerCell = new PdfPCell(new Phrase("MOLD-BASED PRODUCTION", headerFont))
            {
                BackgroundColor = BaseColor.DARK_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Colspan = 2,
                Padding = 8
            };
            table.AddCell(headerCell);

            try
            {
                var moldData = System.Text.Json.JsonSerializer.Deserialize<List<MoldProductionData>>(moldProductionJson);
                if (moldData != null && moldData.Any())
                {
                    foreach (var mold in moldData.OrderByDescending(m => m.ProductionCount))
                    {
                        AddTableRow(table, $"{mold.MoldName}:", 
                            $"{mold.ProductionCount:N0} pallets ({mold.DurationMinutes:N0} min)", dataFont);
                    }
                }
            }
            catch (Exception ex)
            {
                AddTableRow(table, "Error:", $"Mold data could not be read: {ex.Message}", dataFont);
            }

            return table;
        }

        /// <summary>
        /// Malzeme detaylarını JSON'dan parse et
        /// </summary>
        private MaterialDetails ParseMaterialDetails(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<MaterialDetails>(json) ?? new MaterialDetails();
            }
            catch
            {
                return new MaterialDetails();
            }
        }

        /// <summary>
        /// Malzeme detayları tablosu oluştur
        /// </summary>
        private PdfPTable CreateMaterialDetailsTable(string title, MaterialDetails materials)
        {
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = 15
            };

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);

            // Başlık
            var headerCell = new PdfPCell(new Phrase(FixTurkishCharacters(title), headerFont))
            {
                BackgroundColor = BaseColor.DARK_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Colspan = 2,
                Padding = 8
            };
            table.AddCell(headerCell);

            // Çimento detayları
            if (materials.Cements.Any())
            {
                AddTableRow(table, "Cement:", "", headerFont);
                foreach (var cement in materials.Cements.OrderByDescending(x => x.Value))
                {
                    AddTableRow(table, $"  {cement.Key}:", $"{cement.Value:F1} kg", dataFont);
                }
                AddTableRow(table, "Total Cement:", $"{materials.TotalCementKg:F1} kg", headerFont);
                AddTableRow(table, "", "", dataFont); // Boş satır
            }

            // Agrega detayları
            if (materials.Aggregates.Any())
            {
                AddTableRow(table, "Aggregate:", "", headerFont);
                foreach (var aggregate in materials.Aggregates.OrderByDescending(x => x.Value))
                {
                    AddTableRow(table, $"  {aggregate.Key}:", $"{aggregate.Value:F1} kg", dataFont);
                }
                AddTableRow(table, "Total Aggregate:", $"{materials.TotalAggregateKg:F1} kg", headerFont);
                AddTableRow(table, "", "", dataFont); // Boş satır
            }

            // Katkı detayları
            if (materials.Admixtures.Any())
            {
                AddTableRow(table, "Admixture:", "", headerFont);
                foreach (var admixture in materials.Admixtures.OrderByDescending(x => x.Value))
                {
                    AddTableRow(table, $"  {admixture.Key}:", $"{admixture.Value:F1} kg", dataFont);
                }
                AddTableRow(table, "Total Admixture:", $"{materials.TotalAdmixtureKg:F1} kg", headerFont);
                AddTableRow(table, "", "", dataFont); // Boş satır
            }

            // Pigment detayları
            if (materials.Pigments.Any())
            {
                AddTableRow(table, "Pigment:", "", headerFont);
                foreach (var pigment in materials.Pigments.OrderByDescending(x => x.Value))
                {
                    AddTableRow(table, $"  {pigment.Key}:", $"{pigment.Value:F1} kg", dataFont);
                }
                AddTableRow(table, "Total Pigment:", $"{materials.TotalPigmentKg:F1} kg", headerFont);
                AddTableRow(table, "", "", dataFont); // Boş satır
            }

            // Su miktarı
            if (materials.TotalWaterKg > 0)
            {
                AddTableRow(table, "Total Water:", $"{materials.TotalWaterKg:F1} kg", headerFont);
            }

            return table;
        }

        /// <summary>
        /// Vardiya bilgileri tablosu oluştur
        /// </summary>
        private PdfPTable CreateInfoTable(ShiftRecord shiftRecord)
        {
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = 15
            };

            // Türkçe karakter desteği için font ayarları
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, BaseColor.BLACK);

            // Tablo başlığı
            var headerCell = new PdfPCell(new Phrase("SHIFT INFORMATION", headerFont))
            {
                BackgroundColor = BaseColor.DARK_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Colspan = 2,
                Padding = 8
            };
            table.AddCell(headerCell);

            // Vardiya başlama tarihi
            AddTableRow(table, "Shift Start Time:", 
                shiftRecord.ShiftStartTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), dataFont);

            // Vardiya bitiş tarihi
            AddTableRow(table, "Shift End Time:", 
                shiftRecord.ShiftEndTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), dataFont);

            // Operatör adı
            AddTableRow(table, "Operator Name:", 
                FixTurkishCharacters(shiftRecord.OperatorName), dataFont);

            // Gerçek toplam üretim (net + fire)
            var actualTotalProduction = shiftRecord.TotalProduction + shiftRecord.FireProductCount;
            AddTableRow(table, "Total Production Count:", 
                actualTotalProduction.ToString("N0") + " pallets", dataFont);
            
            // Fire mal sayısı
            AddTableRow(table, "Defective Product Count:", 
                shiftRecord.FireProductCount.ToString("N0") + " pieces", dataFont);
            
            // Net üretim (toplam - fire)
            AddTableRow(table, "Net Production Count:", 
                shiftRecord.TotalProduction.ToString("N0") + " pallets", dataFont);

            // Üretim başlama tarihi
            if (shiftRecord.ProductionStartTime.HasValue)
            {
                AddTableRow(table, "Production Start Time:", 
                    shiftRecord.ProductionStartTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), dataFont);
            }

            // Vardiya süresi
            AddTableRow(table, "Shift Duration:", 
                $"{shiftRecord.ShiftDurationMinutes} minutes ({shiftRecord.ShiftDurationMinutes / 60:F1} hours)", dataFont);

            // Üretim süresi
            if (shiftRecord.ProductionDurationMinutes > 0)
            {
                AddTableRow(table, "Production Duration:", 
                    $"{shiftRecord.ProductionDurationMinutes} minutes ({shiftRecord.ProductionDurationMinutes / 60:F1} hours)", dataFont);
            }

            // Batch bilgileri - Mixer1
            AddTableRow(table, "Mixer1 Batch Count:", 
                shiftRecord.Mixer1BatchCount.ToString("N0") + " batches", dataFont);
            
            // Mixer1 çimento bilgileri
            if (!string.IsNullOrEmpty(shiftRecord.Mixer1CementTypesJson))
            {
                var cementTypes = ParseCementTypes(shiftRecord.Mixer1CementTypesJson);
                if (cementTypes.Any())
                {
                    if (cementTypes.Count == 1)
                    {
                        // Tek çimento türü
                        var singleCement = cementTypes.First();
                        AddTableRow(table, "Mixer1 Total Cement:", 
                            $"{shiftRecord.Mixer1CementTotal:F0} kg ({singleCement.Key} cement)", dataFont);
                    }
                    else
                    {
                        // Birden fazla çimento türü
                        var cementDetails = string.Join(" / ", cementTypes.Select(c => $"{c.Key} cement {c.Value:F0}"));
                        AddTableRow(table, "Mixer1 Total Cement:", 
                            $"{shiftRecord.Mixer1CementTotal:F0} kg ({cementDetails})", dataFont);
                    }
                }
            }
            else
            {
                AddTableRow(table, "Mixer1 Total Cement:", 
                    shiftRecord.Mixer1CementTotal.ToString("N0") + " kg", dataFont);
            }

            // Batch bilgileri - Mixer2
            AddTableRow(table, "Mixer2 Batch Count:", 
                shiftRecord.Mixer2BatchCount.ToString("N0") + " batches", dataFont);
            
            // Mixer2 çimento bilgileri
            if (!string.IsNullOrEmpty(shiftRecord.Mixer2CementTypesJson))
            {
                var cementTypes = ParseCementTypes(shiftRecord.Mixer2CementTypesJson);
                if (cementTypes.Any())
                {
                    if (cementTypes.Count == 1)
                    {
                        // Tek çimento türü
                        var singleCement = cementTypes.First();
                        AddTableRow(table, "Mixer2 Total Cement:", 
                            $"{shiftRecord.Mixer2CementTotal:F0} kg ({singleCement.Key} cement)", dataFont);
                    }
                    else
                    {
                        // Birden fazla çimento türü
                        var cementDetails = string.Join(" / ", cementTypes.Select(c => $"{c.Key} cement {c.Value:F0}"));
                        AddTableRow(table, "Mixer2 Total Cement:", 
                            $"{shiftRecord.Mixer2CementTotal:F0} kg ({cementDetails})", dataFont);
                    }
                }
            }
            else
            {
                AddTableRow(table, "Mixer2 Total Cement:", 
                    shiftRecord.Mixer2CementTotal.ToString("N0") + " kg", dataFont);
            }

            // Fire mal sayısı ve boşta geçen süre bilgileri
            AddTableRow(table, "Fire Mal Sayısı:", 
                shiftRecord.FireProductCount.ToString("N0") + " adet", dataFont);
            
            // Boşta geçen süreyi saat:dakika:saniye formatında göster
            var idleHours = shiftRecord.IdleTimeSeconds / 3600;
            var idleMinutes = (shiftRecord.IdleTimeSeconds % 3600) / 60;
            var idleSeconds = shiftRecord.IdleTimeSeconds % 60;
            var idleTimeFormatted = $"{idleHours:D2}:{idleMinutes:D2}:{idleSeconds:D2}";
            AddTableRow(table, "Boşta Geçen Süre:", 
                idleTimeFormatted + " (saat:dakika:saniye)", dataFont);

            // Kalıp bazında üretim bilgileri
            Console.WriteLine($"[PDF] MoldProductionJson kontrol ediliyor: '{shiftRecord.MoldProductionJson}'");
            if (!string.IsNullOrEmpty(shiftRecord.MoldProductionJson))
            {
                var moldProductions = ParseMoldProductions(shiftRecord.MoldProductionJson);
                Console.WriteLine($"[PDF] Parse edilen kalıp sayısı: {moldProductions.Count}");
                if (moldProductions.Any())
                {
                    AddTableRow(table, "Kalıp Bazında Üretim:", "", dataFont);
                    
                    foreach (var moldProd in moldProductions)
                    {
                        var startTime = moldProd.StartTime.ToString("HH:mm");
                        var endTime = moldProd.EndTime?.ToString("HH:mm") ?? "Devam ediyor";
                        var duration = moldProd.EndTime.HasValue 
                            ? $"({(moldProd.EndTime.Value - moldProd.StartTime).TotalMinutes:F0} dk)"
                            : "";
                        
                        Console.WriteLine($"[PDF] Kalıp ekleniyor: {moldProd.MoldName} - {moldProd.ProductionCount} palet");
                        AddTableRow(table, $"  {moldProd.MoldName}:", 
                            $"{moldProd.ProductionCount} palet ({startTime}-{endTime}) {duration}", dataFont);
                    }
                }
                else
                {
                    Console.WriteLine("[PDF] Kalıp bilgileri bulunamadı veya boş");
                }
            }
            else
            {
                Console.WriteLine("[PDF] MoldProductionJson boş veya null");
            }

            return table;
        }

        /// <summary>
        /// Taş üretim tablosu oluştur
        /// </summary>
        private PdfPTable CreateStoneProductionTable(Dictionary<string, int> stoneProduction)
        {
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = 15
            };

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

            // Tablo başlığı
            var headerCell = new PdfPCell(new Phrase(FixTurkishCharacters("STONE PRODUCTION DETAILS"), headerFont))
            {
                BackgroundColor = BaseColor.DARK_GRAY,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Colspan = 2,
                Padding = 8
            };
            table.AddCell(headerCell);

            // Taş üretim verileri
            foreach (var stone in stoneProduction.OrderByDescending(s => s.Value))
            {
                AddTableRow(table, $"{stone.Key}:", 
                    $"{stone.Value:N0} pallets", dataFont);
            }

            return table;
        }

        /// <summary>
        /// Tabloya satır ekle
        /// </summary>
        private void AddTableRow(PdfPTable table, string label, string value, Font font)
        {
            var labelCell = new PdfPCell(new Phrase(FixTurkishCharacters(label), font))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                Padding = 6,
                Border = Rectangle.BOX
            };
            table.AddCell(labelCell);

            var valueCell = new PdfPCell(new Phrase(FixTurkishCharacters(value), font))
            {
                Padding = 6,
                Border = Rectangle.BOX
            };
            table.AddCell(valueCell);
        }

        /// <summary>
        /// Taş üretim JSON'ını parse et
        /// </summary>
        private Dictionary<string, int> ParseStoneProduction(string stoneProductionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(stoneProductionJson))
                    return new Dictionary<string, int>();

                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(stoneProductionJson) 
                    ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// PDF dosyasını aç
        /// </summary>
        public void OpenPdfFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF dosyası açma hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export klasörünü aç
        /// </summary>
        public void OpenExportFolder()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _exportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Export klasörü açma hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vardiya notları tablosu oluştur
        /// </summary>
        private PdfPTable CreateNotesTable(List<ProductionNote> notes)
        {
            var table = new PdfPTable(3) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 1, 3, 1 });

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // Başlık satırı
            var titleCell = new PdfPCell(new Phrase(FixTurkishCharacters("SHIFT NOTES"), headerFont))
            {
                BackgroundColor = BaseColor.DARK_GRAY,
                Padding = 8,
                Border = Rectangle.BOX,
                Colspan = 3,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(titleCell);

            // Sütun başlıkları
            table.AddCell(new PdfPCell(new Phrase("Date", headerFont))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                Padding = 6,
                Border = Rectangle.BOX,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
            table.AddCell(new PdfPCell(new Phrase("Note", headerFont))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                Padding = 6,
                Border = Rectangle.BOX,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
            table.AddCell(new PdfPCell(new Phrase("Operator", headerFont))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                Padding = 6,
                Border = Rectangle.BOX,
                HorizontalAlignment = Element.ALIGN_CENTER
            });

            // Not satırları
            foreach (var note in notes)
            {
                table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(note.CreatedAt.ToString("HH:mm")), cellFont))
                {
                    Padding = 6,
                    Border = Rectangle.BOX,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(note.Note), cellFont))
                {
                    Padding = 6,
                    Border = Rectangle.BOX
                });
                table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(note.CreatedBy), cellFont))
                {
                    Padding = 6,
                    Border = Rectangle.BOX,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }

            return table;
        }

        /// <summary>
        /// Kalıp üretim JSON'unu parse et
        /// </summary>
        private List<MoldProductionData> ParseMoldProductions(string moldProductionJson)
        {
            try
            {
                if (string.IsNullOrEmpty(moldProductionJson))
                    return new List<MoldProductionData>();

                return System.Text.Json.JsonSerializer.Deserialize<List<MoldProductionData>>(moldProductionJson) 
                    ?? new List<MoldProductionData>();
            }
            catch
            {
                return new List<MoldProductionData>();
            }
        }


        /// <summary>
        /// Çimento türleri JSON'unu parse et
        /// </summary>
        private Dictionary<string, double> ParseCementTypes(string cementTypesJson)
        {
            try
            {
                if (string.IsNullOrEmpty(cementTypesJson))
                    return new Dictionary<string, double>();

                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(cementTypesJson) 
                    ?? new Dictionary<string, double>();
            }
            catch
            {
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Türkçe karakterleri PDF uyumlu hale getir
        /// </summary>
        private string FixTurkishCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                // Özel Türkçe karakterler - öncelik sırasına göre
                .Replace("İ", "I")  // Büyük İ -> I
                .Replace("ı", "i")  // Küçük ı -> i
                .Replace("Ğ", "G")  // Büyük Ğ -> G
                .Replace("ğ", "g")  // Küçük ğ -> g
                .Replace("Ü", "U")  // Büyük Ü -> U
                .Replace("ü", "u")  // Küçük ü -> u
                .Replace("Ş", "S")  // Büyük Ş -> S
                .Replace("ş", "s")  // Küçük ş -> s
                .Replace("Ö", "O")  // Büyük Ö -> O
                .Replace("ö", "o")  // Küçük ö -> o
                .Replace("Ç", "C")  // Büyük Ç -> C
                .Replace("ç", "c")  // Küçük ç -> c
                // Diğer Türkçe karakterler
                .Replace("Â", "A")  // Büyük Â -> A
                .Replace("â", "a")  // Küçük â -> a
                .Replace("Ê", "E")  // Büyük Ê -> E
                .Replace("ê", "e")  // Küçük ê -> e
                .Replace("Î", "I")  // Büyük Î -> I
                .Replace("î", "i")  // Küçük î -> i
                .Replace("Ô", "O")  // Büyük Ô -> O
                .Replace("ô", "o")  // Küçük ô -> o
                .Replace("Û", "U")  // Büyük Û -> U
                .Replace("û", "u")  // Küçük û -> u
                .Trim(); // Boşlukları temizle
        }

        /// <summary>
        /// Mixer raporu PDF'i oluştur ve ekranda göster (geçici dosya)
        /// </summary>
        public string CreateMixerReportPdf(List<ConcreteBatch> batches, string mixerName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var fileName = $"Temp_{mixerName}_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_exportPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var document = new Document(PageSize.A4, 50, 50, 25, 25);
                    var writer = PdfWriter.GetInstance(document, fileStream);
                    
                    document.Open();

                    // Başlık
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
                    var title = new Paragraph(FixTurkishCharacters($"{mixerName} PRODUCTION REPORT"), titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    document.Add(title);

                    // Tarih aralığı
                    var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
                    var dateRange = new Paragraph(FixTurkishCharacters($"Date Range: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}"), dateFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    document.Add(dateRange);

                    // Toplam batch sayısı
                    var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLUE);
                    var totalInfo = new Paragraph(FixTurkishCharacters($"Total Batch Count: {batches.Count}"), totalFont)
                    {
                        SpacingAfter = 20
                    };
                    document.Add(totalInfo);

                    // Batch listesi tablosu
                    var table = new PdfPTable(8) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 1, 2, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f });

                    // Başlık satırı
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                    var headerCells = new[]
                    {
                        "ID", "Date", "Recipe", "Status", "Cement (kg)", "Aggregate (kg)", "Water (kg)", "Pigment (kg)"
                    };

                    foreach (var header in headerCells)
                    {
                        var cell = new PdfPCell(new Phrase(FixTurkishCharacters(header), headerFont))
                        {
                            BackgroundColor = BaseColor.DARK_GRAY,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        };
                        table.AddCell(cell);
                    }

                    // Veri satırları
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                    foreach (var batch in batches.Take(50)) // İlk 50 batch'i göster
                    {
                        table.AddCell(new PdfPCell(new Phrase(batch.Id.ToString(), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.OccurredAtLocalFull), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.RecipeCode ?? ""), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.StatusTranslated ?? ""), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalCementKg.ToString("N0"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalAggregateKg.ToString("N0"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalWaterKg.ToString("N1"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.PigmentKg.ToString("N1"), dataFont)) { Padding = 3 });
                    }

                    document.Add(table);

                    // Toplam değerler
                    if (batches.Any())
                    {
                        document.Add(new Paragraph(" ") { SpacingAfter = 20 });
                        
                        var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.DARK_GRAY);
                        var summary = new Paragraph(FixTurkishCharacters("TOPLAM DEĞERLER"), summaryFont)
                        {
                            SpacingAfter = 10
                        };
                        document.Add(summary);

                        var totalCement = batches.Sum(b => b.TotalCementKg);
                        var totalAggregate = batches.Sum(b => b.TotalAggregateKg);
                        var totalWater = batches.Sum(b => b.TotalWaterKg);
                        var totalPigment = batches.Sum(b => b.PigmentKg);

                        var summaryText = FixTurkishCharacters(
                            $"Toplam Çimento: {totalCement:N0} kg\n" +
                            $"Toplam Agrega: {totalAggregate:N0} kg\n" +
                            $"Toplam Su: {totalWater:N1} kg\n" +
                            $"Toplam Pigment: {totalPigment:N1} kg"
                        );

                        var summaryParagraph = new Paragraph(summaryText, dataFont)
                        {
                            SpacingAfter = 20
                        };
                        document.Add(summaryParagraph);
                    }

                    // Alt bilgi
                    var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, BaseColor.GRAY);
                    var footer = new Paragraph(FixTurkishCharacters($"Rapor Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}"), footerFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(footer);

                    document.Close();
                }

                // PDF'i ekranda aç
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                // 5 dakika sonra dosyayı sil
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5 * 60 * 1000); // 5 dakika bekle
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            DetailedLogger.LogInfo($"Geçici PDF dosyası silindi: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogError($"Geçici PDF silme hatası: {ex.Message}");
                    }
                });

                return filePath;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"PDF oluşturma hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mixer2 raporu PDF'i oluştur ve ekranda göster (geçici dosya)
        /// </summary>
        public string CreateMixer2ReportPdf(List<ConcreteBatch2> batches, string mixerName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var fileName = $"Temp_{mixerName}_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_exportPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var document = new Document(PageSize.A4, 50, 50, 25, 25);
                    var writer = PdfWriter.GetInstance(document, fileStream);
                    
                    document.Open();

                    // Başlık
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
                    var title = new Paragraph(FixTurkishCharacters($"{mixerName} PRODUCTION REPORT"), titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    document.Add(title);

                    // Tarih aralığı
                    var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
                    var dateRange = new Paragraph(FixTurkishCharacters($"Date Range: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}"), dateFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    document.Add(dateRange);

                    // Toplam batch sayısı
                    var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLUE);
                    var totalInfo = new Paragraph(FixTurkishCharacters($"Total Batch Count: {batches.Count}"), totalFont)
                    {
                        SpacingAfter = 20
                    };
                    document.Add(totalInfo);

                    // Batch listesi tablosu
                    var table = new PdfPTable(8) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 1, 2, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f });

                    // Başlık satırı
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                    var headerCells = new[]
                    {
                        "ID", "Date", "Recipe", "Status", "Cement (kg)", "Aggregate (kg)", "Water (kg)", "Pigment (kg)"
                    };

                    foreach (var header in headerCells)
                    {
                        var cell = new PdfPCell(new Phrase(FixTurkishCharacters(header), headerFont))
                        {
                            BackgroundColor = BaseColor.DARK_GRAY,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        };
                        table.AddCell(cell);
                    }

                    // Veri satırları
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                    foreach (var batch in batches.Take(50)) // İlk 50 batch'i göster
                    {
                        table.AddCell(new PdfPCell(new Phrase(batch.Id.ToString(), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.OccurredAtLocalFull), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.RecipeCode ?? ""), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(FixTurkishCharacters(batch.StatusTranslated ?? ""), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalCementKg.ToString("N0"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalAggregateKg.ToString("N0"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalWaterKg.ToString("N1"), dataFont)) { Padding = 3 });
                        table.AddCell(new PdfPCell(new Phrase(batch.TotalPigmentKg.ToString("N1"), dataFont)) { Padding = 3 });
                    }

                    document.Add(table);

                    // Toplam değerler
                    if (batches.Any())
                    {
                        document.Add(new Paragraph(" ") { SpacingAfter = 20 });
                        
                        var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.DARK_GRAY);
                        var summary = new Paragraph(FixTurkishCharacters("TOPLAM DEĞERLER"), summaryFont)
                        {
                            SpacingAfter = 10
                        };
                        document.Add(summary);

                        var totalCement = batches.Sum(b => b.TotalCementKg);
                        var totalAggregate = batches.Sum(b => b.TotalAggregateKg);
                        var totalWater = batches.Sum(b => b.TotalWaterKg);
                        var totalPigment = batches.Sum(b => b.TotalPigmentKg);

                        var summaryText = FixTurkishCharacters(
                            $"Toplam Çimento: {totalCement:N0} kg\n" +
                            $"Toplam Agrega: {totalAggregate:N0} kg\n" +
                            $"Toplam Su: {totalWater:N1} kg\n" +
                            $"Toplam Pigment: {totalPigment:N1} kg"
                        );

                        var summaryParagraph = new Paragraph(summaryText, dataFont)
                        {
                            SpacingAfter = 20
                        };
                        document.Add(summaryParagraph);
                    }

                    // Alt bilgi
                    var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, BaseColor.GRAY);
                    var footer = new Paragraph(FixTurkishCharacters($"Rapor Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}"), footerFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(footer);

                    document.Close();
                }

                // PDF'i ekranda aç
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                // 5 dakika sonra dosyayı sil
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5 * 60 * 1000); // 5 dakika bekle
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            DetailedLogger.LogInfo($"Geçici PDF dosyası silindi: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DetailedLogger.LogError($"Geçici PDF silme hatası: {ex.Message}");
                    }
                });

                return filePath;
            }
            catch (Exception ex)
            {
                DetailedLogger.LogError($"PDF oluşturma hatası: {ex.Message}");
                throw;
            }
        }
    }
}