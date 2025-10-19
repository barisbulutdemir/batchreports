using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace takip
{
    public partial class ConcreteReporting2Window : Window
    {
        // DbContext'i thread-safe kullanmak için her işlemde yeni instance oluştur
        private List<ConcreteBatch2> _currentBatches = new List<ConcreteBatch2>();
        
        // Pagination için gerekli değişkenler
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalRecords = 0;
        private int _totalPages = 0;

        public ConcreteReporting2Window()
        {
            InitializeComponent();
            InitializeEventHandlers();
            SetDefaultDates();
            _ = LoadDataAsync();
        }

        private void InitializeEventHandlers()
        {
            LoadDataButton.Click += LoadDataButton_Click;
            RefreshButton.Click += RefreshButton_Click;
            ExportButton.Click += ExportButton_Click;
            DeleteButton.Click += DeleteButton_Click; // 🔥 Delete button event handler
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
            
            // Pagination event handlers
            PageSizeCombo.SelectionChanged += PageSizeCombo_SelectionChanged;
            FirstPageButton.Click += FirstPageButton_Click;
            PrevPageButton.Click += PrevPageButton_Click;
            NextPageButton.Click += NextPageButton_Click;
            LastPageButton.Click += LastPageButton_Click;
        }

        private void SetDefaultDates()
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentBatches == null || _currentBatches.Count == 0)
                {
                    MessageBox.Show("Dışa aktarılacak veri bulunamadı.", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // PdfExportService kullanarak geçici PDF oluştur ve ekranda göster
                var pdfService = new Services.PdfExportService();
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Today;
                var endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
                
                var filePath = pdfService.CreateMixer2ReportPdf(_currentBatches, "Mixer2", startDate, endDate);
                
                MessageBox.Show("PDF created and opened on screen.\nWill be automatically deleted in 5 minutes.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF creation error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf(List<ConcreteBatch2> batches, string fileName)
        {
            Document document = new Document(PageSize.A4.Rotate(), 20, 20, 20, 20);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(fileName, FileMode.Create));
            
            document.Open();
            
            // Başlık
            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            Paragraph title = new Paragraph("MIXER2 CONCRETE PRODUCTION REPORT", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);
            
            // Tarih
            Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            Paragraph date = new Paragraph($"Report Date: {DateTime.Now:dd.MM.yyyy HH:mm}", dateFont);
            date.Alignment = Element.ALIGN_RIGHT;
            date.SpacingAfter = 20;
            document.Add(date);
            
            // Tablo
            PdfPTable table = new PdfPTable(9);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 1f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f });
            
            // Başlık satırı
            Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            string[] headers = { "ID", "Date", "Recipe", "Status", "Cement (kg)", "Aggregate (kg)", "Water (kg)", "Pigment (kg)", "Moisture (%)" };
            
            foreach (string header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 5;
                table.AddCell(cell);
            }
            
            // Veri satırları
            Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
            foreach (var batch in batches)
            {
                table.AddCell(new PdfPCell(new Phrase(batch.Id.ToString(), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.OccurredAt.ToString("dd.MM.yyyy HH:mm"), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.RecipeCode ?? "", dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.Status ?? "", dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.TotalCementKg.ToString("F1"), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.TotalAggregateKg.ToString("F1"), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.TotalWaterKg.ToString("F1"), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.TotalPigmentKg.ToString("F1"), dataFont)));
                table.AddCell(new PdfPCell(new Phrase(batch.MoisturePercent?.ToString("F1") ?? "", dataFont)));
            }
            
            document.Add(table);
            document.Close();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BatchesDataGrid.SelectedItem is ConcreteBatch2 selectedBatch)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the selected batch?\n\n" +
                        $"ID: {selectedBatch.Id}\n" +
                        $"Tarih: {selectedBatch.OccurredAt:dd.MM.yyyy HH:mm}\n" +
                        $"Operatör: {selectedBatch.OperatorName}",
                        "Batch Deletion Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Thread-safe: Her işlem için yeni DbContext
                        using var context = new ProductionDbContext();
                        
                        // Optimistic concurrency için en güncel entity'yi al
                        var batchToDelete = await context.ConcreteBatch2s.FindAsync(selectedBatch.Id);
                        if (batchToDelete != null)
                        {
                            context.ConcreteBatch2s.Remove(batchToDelete);
                            await context.SaveChangesAsync();
                            
                            StatusLabel.Content = $"Batch {selectedBatch.Id} deleted";
                            await LoadDataAsync(); // Verileri yenile
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select the batch you want to delete.", "Warning", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch deletion error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ConcreteReporting2Window] Batch silme hatası: {ex}");
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                StatusLabel.Content = "Loading data...";
                
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddDays(-7);
                var endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
                
                var start = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Local).ToUniversalTime();
                var end = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime();

                // Thread-safe: Her işlem için yeni DbContext
                using var context = new ProductionDbContext();
                
                // Önce toplam kayıt sayısını al
                _totalRecords = await context.ConcreteBatch2s
                    .Where(b => b.PlantCode == "MIXER2" && b.OccurredAt >= start && b.OccurredAt <= end)
                    .CountAsync();
                
                // Toplam sayfa sayısını hesapla
                _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                
                // Sayfa verilerini al
                _currentBatches = await context.ConcreteBatch2s
                    .Include(b => b.Cements)
                    .Include(b => b.Aggregates)
                    .Include(b => b.Admixtures)
                    .Where(b => b.PlantCode == "MIXER2" && b.OccurredAt >= start && b.OccurredAt <= end) // 🔥 SADECE MIXER2
                    .OrderByDescending(b => b.OccurredAt)
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                BatchesDataGrid.ItemsSource = _currentBatches;
                UpdatePaginationInfo();
                StatusLabel.Content = $"Data loaded ({_currentBatches.Count} records)";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Data loading error";
                MessageBox.Show($"Error loading Mixer2 data:\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ConcreteReporting2Window] Veri yükleme hatası: {ex}");
            }
        }

        // Pagination event handlers
        private async void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                _pageSize = int.Parse(selectedItem.Content.ToString());
                _currentPage = 1; // İlk sayfaya dön
                await LoadDataAsync();
            }
        }

        private async void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                await LoadDataAsync();
            }
        }

        private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadDataAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadDataAsync();
            }
        }

        private async void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage = _totalPages;
                await LoadDataAsync();
            }
        }

        private void UpdatePaginationInfo()
        {
            // Sayfa bilgilerini güncelle
            PageInfoLabel.Content = $"Sayfa {_currentPage} / {_totalPages}";
            CountLabel.Content = $"Toplam: {_totalRecords} batch (Sayfa: {_currentBatches.Count})";
            
            // Buton durumlarını güncelle
            FirstPageButton.IsEnabled = _currentPage > 1;
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;
        }

        private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is ConcreteBatch2 batch)
                {
                    var detailWindow = new ConcreteBatch2DetailWindow(batch);
                    detailWindow.Owner = this;
                    detailWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening detail window: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ConcreteReporting2Window] Detay penceresi hatası: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // DbContext artık her işlemde yeni oluşturuluyor, dispose etmeye gerek yok
            base.OnClosed(e);
        }
    }
}
