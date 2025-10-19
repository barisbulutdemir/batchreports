using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using takip.Data;
using takip.Models;
using takip.Services;

namespace takip
{
    public partial class CementSiloWindow : Window
    {
        private readonly ProductionDbContext _context;
        private readonly CementSiloService _cementSiloService;
        private ObservableCollection<CementSilo> _silos = new ObservableCollection<CementSilo>();

        public CementSiloWindow()
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };

            _context = new ProductionDbContext();
            _cementSiloService = new CementSiloService();

            // Converter'lar kaldırıldı - basit yaklaşım kullanılacak

            _ = LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Siloları yükle (varsa)
                var silos = await _cementSiloService.GetAllSilosAsync();
                
                // Eğer hiç silo yoksa, o zaman başlat
                if (!silos.Any())
                {
                    await _cementSiloService.InitializeSilosAsync();
                    silos = await _cementSiloService.GetAllSilosAsync();
                }
                _silos.Clear();
                foreach (var silo in silos)
                {
                    _silos.Add(silo);
                }
                
                // Basit silo kartları oluştur
                CreateSimpleSiloCards(silos);

                // Uyarıları yükle
                await LoadWarnings();

                // Son işlemleri yükle
                await LoadRecentActivities();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Veri yükleme hatası: {ex.Message}");
            }
        }

        private void CreateSimpleSiloCards(List<CementSilo> silos)
        {
            try
            {
                SiloCardsContainer.ItemsSource = null;
                
                var stackPanel = new StackPanel();
                
                foreach (var silo in silos)
                {
                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        CornerRadius = new System.Windows.CornerRadius(10),
                        Padding = new System.Windows.Thickness(20),
                        Margin = new System.Windows.Thickness(0, 0, 0, 15),
                        BorderBrush = System.Windows.Media.Brushes.LightGray,
                        BorderThickness = new System.Windows.Thickness(1)
                    };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Başlık
                    var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };
                    titlePanel.Children.Add(new TextBlock { Text = "🏗️", FontSize = 20, Margin = new System.Windows.Thickness(0, 0, 10, 0) });
                    titlePanel.Children.Add(new TextBlock { Text = silo.CementType, FontSize = 18, FontWeight = FontWeights.Bold });
                    titlePanel.Children.Add(new TextBlock { Text = " Silo ", FontSize = 18, FontWeight = FontWeights.Bold, Margin = new System.Windows.Thickness(5, 0, 0, 0) });
                    titlePanel.Children.Add(new TextBlock { Text = silo.SiloNumber.ToString(), FontSize = 18, FontWeight = FontWeights.Bold });
                    
                    Grid.SetRow(titlePanel, 0);
                    grid.Children.Add(titlePanel);

                    // Durum
                    var statusColor = GetStatusColor(silo.Status);
                    var statusBorder = new Border
                    {
                        Background = statusColor,
                        CornerRadius = new System.Windows.CornerRadius(15),
                        Padding = new System.Windows.Thickness(10, 5, 10, 5),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new System.Windows.Thickness(0, 5, 0, 0)
                    };
                    statusBorder.Child = new TextBlock { Text = silo.Status, Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold };
                    
                    Grid.SetRow(statusBorder, 0);
                    grid.Children.Add(statusBorder);

                    // Silo görseli
                    var siloVisual = CreateSiloVisual(silo);
                    Grid.SetRow(siloVisual, 1);
                    grid.Children.Add(siloVisual);

                    // Bilgiler
                    var infoGrid = new Grid();
                    infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var currentInfo = new StackPanel { Margin = new System.Windows.Thickness(0, 0, 10, 0) };
                    currentInfo.Children.Add(new TextBlock { Text = "Mevcut Miktar", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray });
                    currentInfo.Children.Add(new TextBlock { Text = $"{silo.CurrentAmount:F0} kg", FontSize = 16, FontWeight = FontWeights.Bold });
                    Grid.SetColumn(currentInfo, 0);
                    infoGrid.Children.Add(currentInfo);

                    var capacityInfo = new StackPanel { Margin = new System.Windows.Thickness(5, 0, 5, 0) };
                    capacityInfo.Children.Add(new TextBlock { Text = "Kapasite", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray });
                    capacityInfo.Children.Add(new TextBlock { Text = $"{silo.Capacity:F0} kg", FontSize = 16, FontWeight = FontWeights.Bold });
                    Grid.SetColumn(capacityInfo, 1);
                    infoGrid.Children.Add(capacityInfo);

                    var minInfo = new StackPanel { Margin = new System.Windows.Thickness(10, 0, 0, 0) };
                    minInfo.Children.Add(new TextBlock { Text = "Min. Seviye", FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray });
                    minInfo.Children.Add(new TextBlock { Text = $"{silo.MinLevel:F0} kg", FontSize = 16, FontWeight = FontWeights.Bold });
                    Grid.SetColumn(minInfo, 2);
                    infoGrid.Children.Add(minInfo);

                    Grid.SetRow(infoGrid, 2);
                    grid.Children.Add(infoGrid);

                    // Butonlar
                    var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new System.Windows.Thickness(0, 15, 0, 0) };
                    
                    var refillButton = new Button
                    {
                        Content = "Çimento Ekle",
                        Width = 120,
                        Height = 35,
                        Margin = new System.Windows.Thickness(0, 0, 10, 0),
                        Background = new SolidColorBrush(System.Windows.Media.Colors.Green),
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    refillButton.Tag = silo.Id;
                    refillButton.Click += RefillButton_Click;
                    buttonPanel.Children.Add(refillButton);

                    var detailsButton = new Button
                    {
                        Content = "Detaylar",
                        Width = 100,
                        Height = 35,
                        Background = new SolidColorBrush(System.Windows.Media.Colors.Blue),
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    detailsButton.Tag = silo.Id;
                    detailsButton.Click += DetailsButton_Click;
                    buttonPanel.Children.Add(detailsButton);

                    Grid.SetRow(buttonPanel, 3);
                    grid.Children.Add(buttonPanel);

                    border.Child = grid;
                    stackPanel.Children.Add(border);
                }

                SiloCardsContainer.ItemsSource = new List<object> { stackPanel };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Silo kartları oluşturma hatası: {ex.Message}");
            }
        }

        private System.Windows.Media.Brush GetStatusColor(string status)
        {
            return status switch
            {
                "Boş" => new SolidColorBrush(System.Windows.Media.Colors.Red),
                "Kritik" => new SolidColorBrush(System.Windows.Media.Colors.DarkRed),
                "Düşük" => new SolidColorBrush(System.Windows.Media.Colors.Orange),
                "Dolu" => new SolidColorBrush(System.Windows.Media.Colors.Green),
                "Normal" => new SolidColorBrush(System.Windows.Media.Colors.Blue),
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        private FrameworkElement CreateSiloVisual(CementSilo silo)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                CornerRadius = new System.Windows.CornerRadius(8),
                Height = 250,
                Margin = new System.Windows.Thickness(0, 15, 0, 15),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Colors.LightGray),
                BorderThickness = new System.Windows.Thickness(1)
            };

            var canvas = new Canvas
            {
                Width = 200,
                Height = 200,
                Margin = new System.Windows.Thickness(25, 25, 25, 25)
            };

            // Silo gövdesi (dikdörtgen)
            var siloBody = new Rectangle
            {
                Width = 120,
                Height = 140,
                Fill = new SolidColorBrush(System.Windows.Media.Colors.LightGray),
                Stroke = new SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };
            Canvas.SetLeft(siloBody, 40);
            Canvas.SetTop(siloBody, 40);
            canvas.Children.Add(siloBody);

            // Silo konisi (üst kısım)
            var siloCone = new Polygon
            {
                Fill = new SolidColorBrush(System.Windows.Media.Colors.LightGray),
                Stroke = new SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                StrokeThickness = 2
            };
            siloCone.Points = new PointCollection
            {
                new System.Windows.Point(40, 40),   // Sol üst
                new System.Windows.Point(160, 40),   // Sağ üst
                new System.Windows.Point(100, 20)  // Tepe
            };
            canvas.Children.Add(siloCone);

            // Çimento seviyesi (silo içinde)
            var fillHeight = Math.Max(5, (silo.FillPercentage / 100.0) * 140);
            var fillColor = GetFillColor(silo.FillPercentage);
            
            var cementLevel = new Rectangle
            {
                Width = 116,
                Height = fillHeight,
                Fill = fillColor,
                Stroke = new SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                StrokeThickness = 1,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(cementLevel, 42);
            Canvas.SetBottom(cementLevel, 42); // Alt kısımdan başla
            canvas.Children.Add(cementLevel);

            // Seviye yüzdesi göstergesi
            var levelText = new TextBlock
            {
                Text = $"{silo.FillPercentage:F1}%",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(levelText, 50);
            Canvas.SetTop(levelText, 90);
            levelText.Width = 100;
            levelText.Height = 20;
            canvas.Children.Add(levelText);

            // Silo numarası
            var siloNumberText = new TextBlock
            {
                Text = $"Silo {silo.SiloNumber}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.DarkBlue),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(siloNumberText, 50);
            Canvas.SetTop(siloNumberText, 200);
            siloNumberText.Width = 100;
            siloNumberText.Height = 20;
            canvas.Children.Add(siloNumberText);

            // Alt kısım (çimento çıkışı)
            var bottomOutlet = new Rectangle
            {
                Width = 20,
                Height = 15,
                Fill = new SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                Stroke = new SolidColorBrush(System.Windows.Media.Colors.Black),
                StrokeThickness = 1
            };
            Canvas.SetLeft(bottomOutlet, 90);
            Canvas.SetTop(bottomOutlet, 185);
            canvas.Children.Add(bottomOutlet);

            border.Child = canvas;
            return border;
        }

        private System.Windows.Media.Brush GetFillColor(double percentage)
        {
            if (percentage <= 0) return new SolidColorBrush(System.Windows.Media.Colors.Red);
            if (percentage <= 20) return new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
            if (percentage <= 40) return new SolidColorBrush(System.Windows.Media.Colors.Orange);
            if (percentage <= 80) return new SolidColorBrush(System.Windows.Media.Colors.Yellow);
            return new SolidColorBrush(System.Windows.Media.Colors.Green);
        }

        private async Task LoadWarnings()
        {
            try
            {
                var warnings = new List<string>();
                var silos = await _cementSiloService.GetAllSilosAsync();
                foreach (var silo in silos)
                {
                    var status = await _cementSiloService.CheckSiloStatusAsync(silo.Id);
                    if (status == "Kritik" || status == "Düşük")
                    {
                        warnings.Add($"Silo {silo.SiloNumber} ({silo.CementType}): {status}");
                    }
                }
                WarningsContainer.ItemsSource = warnings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Uyarı yükleme hatası: {ex.Message}");
            }
        }

        private async Task LoadRecentActivities()
        {
            try
            {
                var consumptions = await _cementSiloService.GetRecentConsumptionsAsync(10);
                ConsumptionGrid.ItemsSource = consumptions;

                var refills = await _cementSiloService.GetRecentRefillsAsync(10);
                RefillGrid.ItemsSource = refills;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] İşlem yükleme hatası: {ex.Message}");
            }
        }

        private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Tüm siloların tüketim ve dolum geçmişini silmek istediğinize emin misiniz?\n\nBu işlem geri alınamaz.",
                    "Onay",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                int deletedConsumptions = 0;
                int deletedRefills = 0;
                using (var ctx = new ProductionDbContext())
                {
                    var allConsumptions = ctx.CementConsumptions.ToList();
                    var allRefills = ctx.CementRefills.ToList();
                    deletedConsumptions = allConsumptions.Count;
                    deletedRefills = allRefills.Count;
                    ctx.CementConsumptions.RemoveRange(allConsumptions);
                    ctx.CementRefills.RemoveRange(allRefills);
                    await ctx.SaveChangesAsync();
                }

                MessageBox.Show($"Silinen tüketim: {deletedConsumptions}\nSilinen dolum: {deletedRefills}",
                    "Geçmiş Temizlendi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geçmiş temizleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefillButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var siloId = (int)button!.Tag;

                var refillDialog = new CementRefillDialog(siloId, _cementSiloService);
                refillDialog.ShowDialog();

                // Verileri yenile
                _ = LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Çimento ekleme dialog hatası: {ex.Message}");
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var siloId = (int)button!.Tag;

                var detailsDialog = new CementSiloDetailsDialog(siloId, _cementSiloService);
                detailsDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Detay dialog hatası: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Ayarlar butonu click event'i
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Çimento silo ayarları artık ana sayfada bulunmaktadır.", "Bilgi", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CementSiloWindow] Ayarlar penceresi açma hatası: {ex.Message}");
                MessageBox.Show($"Ayarlar penceresi açılırken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
    }

}
