using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip
{
    /// <summary>
    /// Vardiya geçmişi penceresi - Liste formatında
    /// </summary>
    public partial class ShiftHistoryWindow : Window
    {
        // DbContext field'ı kaldırıldı - her işlemde yeni instance oluşturulacak
        private PdfExportService _pdfExportService = new PdfExportService();
        private List<ShiftRecord> _allShiftRecords = new List<ShiftRecord>();

        public ShiftHistoryWindow()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        /// <summary>
        /// Veritabanını başlat
        /// </summary>
        private async void InitializeDatabase()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    await context.Database.EnsureCreatedAsync();
                }
                
                // Veritabanı hazır olduğunda otomatik yükle
                await LoadShiftHistoryAsync();
                await LoadOperatorsForFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı hatası: {ex.Message}", "Hata", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Operatörleri filtre için yükle
        /// </summary>
        private async Task LoadOperatorsForFilter()
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var operatorService = new OperatorService(context);
                    var operators = await operatorService.GetAllOperatorsAsync();
                    
                    var operatorList = new List<string> { "Tümü" };
                    operatorList.AddRange(operators.Select(o => o.Name));
                    
                    OperatorFilterComboBox.ItemsSource = operatorList;
                    OperatorFilterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception)
            {
                // Operatör listesi yükleme hatası - sessizce geç
            }
        }

        /// <summary>
        /// Vardiya geçmişini yükle
        /// </summary>
        private async Task LoadShiftHistoryAsync()
        {
            try
            {
                // Loading göster
                LoadingPanel.Visibility = Visibility.Visible;
                HistoryScrollViewer.Visibility = Visibility.Collapsed;
                ListHeaderBorder.Visibility = Visibility.Collapsed;

                ShiftHistoryPanel.Children.Clear();
                
                // Thread-safe: her işlemde yeni DbContext kullan
                using (var context = new ProductionDbContext())
                {
                    var shiftRecordService = new ShiftRecordService(context);
                    var shiftRecords = await shiftRecordService.GetRecentShiftRecordsAsync(100);
                    _allShiftRecords = shiftRecords.ToList();

                // Filtreleme uygula
                var filteredRecords = ApplyFilters(shiftRecords);

                    if (!filteredRecords.Any())
                    {
                        var noDataText = new TextBlock
                        {
                            Text = "Filtre kriterlerine uygun vardiya kaydı bulunmuyor.",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 50, 0, 0)
                        };
                        ShiftHistoryPanel.Children.Add(noDataText);
                    }
                    else
                    {
                        // Liste başlığını göster
                        ListHeaderBorder.Visibility = Visibility.Visible;
                        
                        foreach (var record in filteredRecords)
                        {
                            var recordBorder = new Border
                            {
                                Background = new SolidColorBrush(Colors.White),
                                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(0, 0, 0, 1),
                                Padding = new Thickness(10)
                            };

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Tarih
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Saat
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Operatör
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Süre
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Üretim
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });  // PDF

                            // Tarih
                            var dateText = new TextBlock
                            {
                                Text = record.ShiftStartTime.ToLocalTime().ToString("dd.MM.yyyy"),
                                FontSize = 12,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            Grid.SetColumn(dateText, 0);
                            grid.Children.Add(dateText);

                            // Saat
                            var timeText = new TextBlock
                            {
                                Text = record.ShiftStartTime.ToLocalTime().ToString("HH:mm"),
                                FontSize = 12,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            Grid.SetColumn(timeText, 1);
                            grid.Children.Add(timeText);

                            // Operatör
                            var operatorText = new TextBlock
                            {
                                Text = record.OperatorName,
                                FontSize = 12,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontWeight = FontWeights.Medium
                            };
                            Grid.SetColumn(operatorText, 2);
                            grid.Children.Add(operatorText);

                            // Süre
                            var durationText = new TextBlock
                            {
                                Text = $"{record.ShiftDurationMinutes / 60:F1}h",
                                FontSize = 12,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush(Colors.DarkOrange)
                            };
                            Grid.SetColumn(durationText, 3);
                            grid.Children.Add(durationText);

                            // Üretim
                            var productionText = new TextBlock
                            {
                                Text = $"{record.TotalProduction:N0}",
                                FontSize = 12,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush(Colors.DarkGreen),
                                FontWeight = FontWeights.Medium
                            };
                            Grid.SetColumn(productionText, 4);
                            grid.Children.Add(productionText);

                            // PDF Butonu
                            var pdfButton = new Button
                            {
                                Content = "PDF",
                                FontSize = 10,
                                Padding = new Thickness(5, 2, 5, 2),
                                Background = new SolidColorBrush(Colors.Orange),
                                Foreground = new SolidColorBrush(Colors.White),
                                BorderThickness = new Thickness(0),
                                Cursor = Cursors.Hand
                            };
                            pdfButton.Click += (s, e) => ExportSpecificShiftPdf(record.Id);
                            Grid.SetColumn(pdfButton, 5);
                            grid.Children.Add(pdfButton);

                            recordBorder.Child = grid;
                            ShiftHistoryPanel.Children.Add(recordBorder);
                        }
                    }

                    // Kayıt sayısını güncelle
                    RecordCountText.Text = $"{filteredRecords.Count()} kayıt gösteriliyor";
                    
                    // İstatistikleri güncelle
                    UpdateSummaryStatistics(filteredRecords);
                }

                // Loading gizle, listeyi göster
                LoadingPanel.Visibility = Visibility.Collapsed;
                HistoryScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // Loading gizle, hata göster
                LoadingPanel.Visibility = Visibility.Collapsed;
                HistoryScrollViewer.Visibility = Visibility.Visible;
                
                MessageBox.Show($"Vardiya geçmişi yükleme hatası: {ex.Message}", "Hata", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Özet istatistikleri güncelle
        /// </summary>
        private void UpdateSummaryStatistics(IEnumerable<ShiftRecord> records)
        {
            try
            {
                var recordList = records.ToList();
                var totalShifts = recordList.Count;
                var totalProduction = recordList.Sum(r => r.TotalProduction);
                var uniqueOperators = recordList.Select(r => r.OperatorName).Distinct().Count();
                
                // Tarih aralığı
                var startDate = recordList.Min(r => r.ShiftStartTime.ToLocalTime().Date);
                var endDate = recordList.Max(r => r.ShiftStartTime.ToLocalTime().Date);
                var dateRange = startDate == endDate ? 
                    startDate.ToString("dd.MM.yyyy") : 
                    $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                
                SummaryText.Text = $"Tarih Aralığı: {dateRange} | Filtrelenmiş Kayıtlar";
                TotalShiftsText.Text = $"Toplam Vardiya: {totalShifts:N0}";
                TotalProductionText.Text = $"Toplam Üretim: {totalProduction:N0} palet";
                ActiveOperatorsText.Text = $"Aktif Operatör: {uniqueOperators}";
            }
            catch (Exception)
            {
                SummaryText.Text = "İstatistik hesaplama hatası";
                TotalShiftsText.Text = "Toplam Vardiya: -";
                TotalProductionText.Text = "Toplam Üretim: -";
                ActiveOperatorsText.Text = "Aktif Operatör: -";
            }
        }

        /// <summary>
        /// Filtreleri uygula
        /// </summary>
        private IEnumerable<ShiftRecord> ApplyFilters(IEnumerable<ShiftRecord> records)
        {
            var filtered = records.AsEnumerable();

            // Tarih filtresi
            if (StartDatePicker.SelectedDate.HasValue)
            {
                filtered = filtered.Where(r => r.ShiftStartTime.ToLocalTime().Date >= StartDatePicker.SelectedDate.Value);
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                filtered = filtered.Where(r => r.ShiftStartTime.ToLocalTime().Date <= EndDatePicker.SelectedDate.Value);
            }

            // Operatör filtresi
            if (OperatorFilterComboBox.SelectedItem != null && 
                OperatorFilterComboBox.SelectedItem.ToString() != "Tümü")
            {
                var selectedOperator = OperatorFilterComboBox.SelectedItem.ToString();
                filtered = filtered.Where(r => r.OperatorName == selectedOperator);
            }

            return filtered.OrderByDescending(r => r.ShiftStartTime);
        }

        /// <summary>
        /// Operatör filtresi değiştiğinde
        /// </summary>
        private async void OperatorFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadShiftHistoryAsync();
        }

        /// <summary>
        /// Filtreleri temizle
        /// </summary>
        private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            OperatorFilterComboBox.SelectedIndex = 0;
            await LoadShiftHistoryAsync();
        }

        /// <summary>
        /// Yenile butonu
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadShiftHistoryAsync();
        }

        /// <summary>
        /// Belirli vardiya için PDF çıktı al
        /// </summary>
        private async void ExportSpecificShiftPdf(int shiftRecordId)
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var shiftRecordService = new ShiftRecordService(context);
                    var shiftRecord = await shiftRecordService.GetShiftRecordByIdAsync(shiftRecordId);
                    if (shiftRecord == null)
                    {
                        MessageBox.Show("Vardiya kaydı bulunamadı!", "Hata", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pdfPath = _pdfExportService.CreateShiftReportPdf(shiftRecord);
                    if (!string.IsNullOrEmpty(pdfPath))
                    {
                        _pdfExportService.OpenPdfFile(pdfPath);
                        MessageBox.Show("PDF raporu oluşturuldu ve açıldı!", "Başarılı", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF çıktı hatası: {ex.Message}", "Hata", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// PDF çıktı butonu - Son vardiya
        /// </summary>
        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new ProductionDbContext())
                {
                    var shiftRecordService = new ShiftRecordService(context);
                    var lastShiftRecord = await shiftRecordService.GetLastShiftRecordAsync();
                    if (lastShiftRecord == null)
                    {
                        MessageBox.Show("Son vardiya kaydı bulunamadı!", "Hata", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var pdfPath = _pdfExportService.CreateShiftReportPdf(lastShiftRecord);
                    if (!string.IsNullOrEmpty(pdfPath))
                    {
                        _pdfExportService.OpenPdfFile(pdfPath);
                        MessageBox.Show("Son vardiya PDF raporu oluşturuldu ve açıldı!", "Başarılı", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF çıktı hatası: {ex.Message}", "Hata", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// PDF klasörünü aç butonu
        /// </summary>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pdfExportService.OpenExportFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Klasör açma hatası: {ex.Message}", "Hata", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
