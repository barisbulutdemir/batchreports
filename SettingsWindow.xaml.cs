using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using takip.Models;
using takip.Services;
using takip.Data;
using takip.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace takip
{
    public partial class SettingsWindow : Window
    {
        private ObservableCollection<Mold> _molds = new ObservableCollection<Mold>();
        private ObservableCollection<Operator> _operators = new ObservableCollection<Operator>();
        private SettingsService _settingsService = new SettingsService();
        
        // Saat dilimi yönetimi için
        private DispatcherTimer _timeUpdateTimer;
        private TimeZoneInfo _selectedTimeZone;
        private bool _autoConvertEnabled = true;

        public SettingsWindow()
        {
            InitializeComponent();
            PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) Close(); };
            SaveAliasesButton.Click += SaveAliasesButton_Click; // Malzeme isimleri kaydetme
            InitializeAliasesButton.Click += InitializeAliasesButton_Click; // Temel alias verilerini ekleme
            
            // Saat dilimi ayarlarını başlat
            InitializeTimeZoneSettings();
            
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                await LoadMolds();
                await LoadOperators();
                LoadPlcSettings();
                LoadMaterialAliases(); // Malzeme isimlerini yükle
                LoadTimeZoneSettings(); // Saat dilimi ayarlarını yükle
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.DataLoadError"), ex.Message), 
                                   LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #region Kalıp Yönetimi

        private async Task LoadMolds()
        {
            try
            {
                using var context = new ProductionDbContext();
                var molds = await context.Molds
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                _molds.Clear();
                foreach (var mold in molds)
                {
                    _molds.Add(mold);
                }

                Dispatcher.Invoke(() =>
                {
                    MoldsDataGrid.ItemsSource = _molds;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldListLoadError"), ex.Message), 
                                   LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async void DeleteMoldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMold = MoldsDataGrid.SelectedItem as Mold;
                if (selectedMold == null)
                {
                    MessageBox.Show(LocalizationService.Instance.GetString("SettingsWindow.PleaseSelectMoldToDelete"), 
                                   LocalizationService.Instance.GetString("Common.Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldDeleteConfirmationText"), selectedMold.Name),
                    LocalizationService.Instance.GetString("SettingsWindow.MoldDeleteConfirmation"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var context = new ProductionDbContext();
                    var moldToDelete = await context.Molds.FindAsync(selectedMold.Id);
                    if (moldToDelete != null)
                    {
                        context.Molds.Remove(moldToDelete);
                        await context.SaveChangesAsync();
                        
                        await LoadMolds(); // Listeyi yenile
                        MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldDeletedSuccessfully"), selectedMold.Name), 
                                       LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldDeleteError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshMoldsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMolds();
        }

        #endregion

        #region Operatör Yönetimi

        private async Task LoadOperators()
        {
            try
            {
                using var context = new ProductionDbContext();
                var operators = await context.Operators
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                _operators.Clear();
                foreach (var op in operators)
                {
                    _operators.Add(op);
                }

                Dispatcher.Invoke(() =>
                {
                    OperatorsDataGrid.ItemsSource = _operators;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorListLoadError"), ex.Message), 
                                   LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void AddOperatorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddOperatorDialog();
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    // Operatör listesini yenile
                    LoadOperators();
                    
                    // Ana sayfayı yenile - MainWindow'a operatör listesi güncellendiğini bildir
                    if (Owner is MainWindow mainWindow)
                    {
                        _ = mainWindow.RefreshOperatorList();
                    }
                    
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorAddedSuccessfully"), dialog.OperatorName), 
                                   LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorAddError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteOperatorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedOperator = OperatorsDataGrid.SelectedItem as Operator;
                if (selectedOperator == null)
                {
                    MessageBox.Show(LocalizationService.Instance.GetString("SettingsWindow.PleaseSelectOperatorToDelete"), 
                                   LocalizationService.Instance.GetString("Common.Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorDeleteConfirmationText"), selectedOperator.Name),
                    LocalizationService.Instance.GetString("SettingsWindow.OperatorDeleteConfirmation"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var context = new ProductionDbContext();
                    var operatorToDelete = await context.Operators.FindAsync(selectedOperator.Id);
                    if (operatorToDelete != null)
                    {
                        context.Operators.Remove(operatorToDelete);
                        await context.SaveChangesAsync();
                        
                        await LoadOperators(); // Listeyi yenile
                        
                        // Ana sayfayı yenile - MainWindow'a operatör listesi güncellendiğini bildir
                        if (Owner is MainWindow mainWindow)
                        {
                            _ = mainWindow.RefreshOperatorList();
                        }
                        
                        MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorDeletedSuccessfully"), selectedOperator.Name), 
                                       LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.OperatorDeleteError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshOperatorsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadOperators();
        }

        #endregion

        #region PLC Test

        private async void TestPlcButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Input validasyonu
                if (!ValidatePlcInputs())
                    return;

                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.ConnectingToPlc");
                TestPlcButton.IsEnabled = false;

                // PLC bilgilerini al
                string plcIp = PlcIpTextBox.Text.Trim();
                int plcPort = int.Parse(PlcPortTextBox.Text.Trim());
                ushort registerAddress = ushort.Parse(RegisterAddressTextBox.Text.Trim());
                ushort wordCount = ushort.Parse(WordCountTextBox.Text.Trim());

                // PLC test et
                await TestPlcConnection(plcIp, plcPort, registerAddress, wordCount);
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"{LocalizationService.Instance.GetString("SettingsWindow.ErrorOccurred")}: {ex.Message}\n\n{LocalizationService.Instance.GetString("SettingsWindow.Details")}: {ex}";
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.ErrorOccurred");
            }
            finally
            {
                TestPlcButton.IsEnabled = true;
            }
        }

        private bool ValidatePlcInputs()
        {
            // IP adresi kontrolü
            if (string.IsNullOrWhiteSpace(PlcIpTextBox.Text))
            {
                ResultTextBox.Text = LocalizationService.Instance.GetString("SettingsWindow.PlcIpEmptyError");
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidIpAddress");
                return false;
            }

            if (!System.Net.IPAddress.TryParse(PlcIpTextBox.Text.Trim(), out _))
            {
                ResultTextBox.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidIpFormatError");
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidIpAddress");
                return false;
            }

            // Port kontrolü
            if (!int.TryParse(PlcPortTextBox.Text.Trim(), out int port) || port <= 0 || port > 65535)
            {
                ResultTextBox.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidPortError");
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidPort");
                return false;
            }

            // Register adresi kontrolü
            if (!ushort.TryParse(RegisterAddressTextBox.Text.Trim(), out ushort regAddr))
            {
                ResultTextBox.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidRegisterError");
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidRegisterAddress");
                return false;
            }

            // Word sayısı kontrolü
            if (!ushort.TryParse(WordCountTextBox.Text.Trim(), out ushort wordCnt) || wordCnt <= 0 || wordCnt > 100)
            {
                ResultTextBox.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidWordCountError");
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.InvalidWordCount");
                return false;
            }

            return true;
        }

        private async Task TestPlcConnection(string plcIp, int plcPort, ushort registerAddress, ushort wordCount)
        {
            try
            {
                ResultTextBox.Text = $"{LocalizationService.Instance.GetString("SettingsWindow.PlcTestStarting")}\n";
                ResultTextBox.Text += $"IP: {plcIp}\n";
                ResultTextBox.Text += string.Format(LocalizationService.Instance.GetString("SettingsWindow.Port"), plcPort) + "\n";
                ResultTextBox.Text += string.Format(LocalizationService.Instance.GetString("SettingsWindow.Register"), registerAddress) + "\n";
                ResultTextBox.Text += string.Format(LocalizationService.Instance.GetString("SettingsWindow.WordCount"), wordCount) + "\n\n";

                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.ConnectingToPlc");

                using var client = new FinsUdpClient(plcIp, plcPort);
                client.Connect();

                ResultTextBox.Text += $"{LocalizationService.Instance.GetString("SettingsWindow.PlcConnectionSuccessful")}\n\n";
                StatusTextBlock.Text = "Register okunuyor...";

                // Register'ı oku
                var rawData = await client.ReadDmAsync(registerAddress, wordCount, 5000);
                
                ResultTextBox.Text += $"📊 Okunan Veri ({rawData.Length} byte):\n";
                
                for (int i = 0; i < rawData.Length; i += 2)
                {
                    if (i + 1 < rawData.Length)
                    {
                        ushort value = (ushort)((rawData[i] << 8) | rawData[i + 1]);
                        ResultTextBox.Text += string.Format(LocalizationService.Instance.GetString("SettingsWindow.Word"), i / 2, value) + "\n";
                    }
                }

                ResultTextBox.Text += $"\n✅ {LocalizationService.Instance.GetString("SettingsWindow.TestCompleted")}\n";
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.TestSuccessful");
            }
            catch (Exception ex)
            {
                ResultTextBox.Text += $"\n❌ {LocalizationService.Instance.GetString("SettingsWindow.PlcTestError")}: {ex.Message}\n";
                StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.TestFailed");
            }
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Clear();
            StatusTextBlock.Text = LocalizationService.Instance.GetString("SettingsWindow.ReadyForTest");
        }

        #endregion

        #region Kalıp Ekleme

        private async void AddMoldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddMoldDialog();
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    var mold = new Mold
                    {
                        Name = dialog.MoldName,
                        Code = dialog.MoldCode,
                        IsActive = false,
                        Description = LocalizationService.Instance.GetString("SettingsWindow.NewMold"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    using (var context = new ProductionDbContext())
                    {
                        context.Molds.Add(mold);
                        await context.SaveChangesAsync();
                    }
                    
                    // Kalıp eklendikten sonra listeyi yenile
                    await LoadMolds();
                    
                    // Ana sayfayı yenile - MainWindow'a kalıp listesi güncellendiğini bildir
                    if (Owner is MainWindow mainWindow)
                    {
                        mainWindow.RefreshMoldsList(); // Ana sayfadaki kalıp listesini yenile
                    }
                    
                    MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldAddedSuccessfully"), dialog.MoldName), 
                                   LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MoldAddError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ClearSiloHistoryFromSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    LocalizationService.Instance.GetString("SettingsWindow.ClearAllSiloHistoryConfirmation"),
                    LocalizationService.Instance.GetString("SettingsWindow.Confirmation"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

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
                    LocalizationService.Instance.GetString("SettingsWindow.HistoryCleared"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.HistoryClearError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region PLC Ayarları

        private void LoadPlcSettings()
        {
            try
            {
                var settings = _settingsService.LoadPlcSettings();
                
                // Üretim PLC ayarları
                ProductionPlcIpTextBox.Text = settings.ProductionPlc.IpAddress;
                ProductionPlcPortTextBox.Text = settings.ProductionPlc.Port.ToString();
                    MachineCountRegisterTextBox.Text = settings.ProductionPlc.MachineCountRegister.ToString();
                    StoneCountRegisterTextBox.Text = settings.ProductionPlc.StoneCountRegister.ToString();
                    StoneNameTextBox.Text = settings.ProductionPlc.StoneName;
                    FireProductRegisterTextBox.Text = settings.ProductionPlc.FireProductRegister.ToString();
                
                // Beton santrali PLC ayarları
                ConcretePlcIpTextBox.Text = settings.ConcretePlc.IpAddress;
                ConcretePlcPortTextBox.Text = settings.ConcretePlc.Port.ToString();
                PollIntervalTextBox.Text = settings.ConcretePlc.PollIntervalSeconds.ToString();
                MinBatchIntervalTextBox.Text = settings.ConcretePlc.MinBatchIntervalSeconds.ToString();
                
                // Register adresleri
                var registerItems = settings.Registers.Select(kvp => new { 
                    Key = kvp.Key, 
                    Value = kvp.Value.ToString(),
                    Type = GetRegisterType(kvp.Value.ToString())
                }).ToList();
                RegisterDataGrid.ItemsSource = registerItems;
                
                // Debug: Mixer1 bekleme bunkeri değerini kontrol et
                if (settings.Registers.ContainsKey("M1_WaitingBunker"))
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsWindow] M1_WaitingBunker değeri: {settings.Registers["M1_WaitingBunker"]}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.PlcSettingsLoadError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePlcSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = _settingsService.LoadPlcSettings();
                
                // Üretim PLC ayarları
                settings.ProductionPlc.IpAddress = ProductionPlcIpTextBox.Text;
                if (int.TryParse(ProductionPlcPortTextBox.Text, out int productionPort))
                    settings.ProductionPlc.Port = productionPort;
                if (int.TryParse(MachineCountRegisterTextBox.Text, out int machineCount))
                    settings.ProductionPlc.MachineCountRegister = machineCount;
                    if (int.TryParse(StoneCountRegisterTextBox.Text, out int stoneCount))
                        settings.ProductionPlc.StoneCountRegister = stoneCount;
                    settings.ProductionPlc.StoneName = StoneNameTextBox.Text;
                    if (int.TryParse(FireProductRegisterTextBox.Text, out int fireProduct))
                        settings.ProductionPlc.FireProductRegister = fireProduct;
                
                // Beton santrali PLC ayarları
                settings.ConcretePlc.IpAddress = ConcretePlcIpTextBox.Text;
                if (int.TryParse(ConcretePlcPortTextBox.Text, out int concretePort))
                    settings.ConcretePlc.Port = concretePort;
                if (int.TryParse(PollIntervalTextBox.Text, out int pollInterval))
                    settings.ConcretePlc.PollIntervalSeconds = pollInterval;
                if (int.TryParse(MinBatchIntervalTextBox.Text, out int minBatchInterval))
                    settings.ConcretePlc.MinBatchIntervalSeconds = minBatchInterval;
                
                // Register adresleri
                settings.Registers.Clear();
                if (RegisterDataGrid.ItemsSource != null)
                {
                    foreach (var item in RegisterDataGrid.ItemsSource)
                    {
                        try
                        {
                            var keyProperty = item.GetType().GetProperty("Key");
                            var valueProperty = item.GetType().GetProperty("Value");
                            var typeProperty = item.GetType().GetProperty("Type");
                            
                            if (keyProperty != null && valueProperty != null)
                            {
                                string? key = keyProperty.GetValue(item)?.ToString();
                                string? value = valueProperty.GetValue(item)?.ToString();
                                string? type = typeProperty?.GetValue(item)?.ToString() ?? "DM";
                                
                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                {
                                    // Register tipine göre kaydet - hepsini object olarak sakla
                                    settings.Registers[key] = value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Hata durumunda devam et
                            System.Diagnostics.Debug.WriteLine($"Register kaydetme hatası: {ex.Message}");
                        }
                    }
                }
                
                _settingsService.SavePlcSettings(settings);
                MessageBox.Show(LocalizationService.Instance.GetString("SettingsWindow.PlcSettingsSavedSuccessfully"), 
                               LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.PlcSettingsSaveError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRegisterType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "DM";
                
            if (value.StartsWith("H"))
                return "H";
            else if (value.StartsWith("DM"))
                return "DM";
            else
                return "DM"; // Varsayılan olarak DM
        }

        #endregion

        #region Malzeme İsimleri

        private void LoadMaterialAliases()
        {
            try
            {
                using var context = new ProductionDbContext();
                
                // Mixer1 malzeme isimleri
                AggAliasGrid.ItemsSource = context.AggregateAliases.OrderBy(a => a.Slot).ToList();
                AdmAliasGrid.ItemsSource = context.AdmixtureAliases.OrderBy(a => a.Slot).ToList();
                CementAliasGrid.ItemsSource = context.CementAliases.OrderBy(a => a.Slot).ToList();
                
                // Mixer2 malzeme isimleri
                AggAliasGrid2.ItemsSource = context.Aggregate2Aliases.OrderBy(a => a.Slot).ToList();
                AdmAliasGrid2.ItemsSource = context.Admixture2Aliases.OrderBy(a => a.Slot).ToList();
                CementAliasGrid2.ItemsSource = context.Cement2Aliases.OrderBy(a => a.Slot).ToList();
                PigmentAliasGrid2.ItemsSource = context.Pigment2Aliases.OrderBy(a => a.Slot).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MaterialNamesLoadError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[SettingsWindow] Malzeme isimleri yükleme hatası: {ex}");
            }
        }

        private void SaveAliasesButton_Click(object sender, RoutedEventArgs e)
        {
            SaveMaterialAliases();
        }

        private void InitializeAliasesButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeDefaultAliases();
        }

        private void SaveMaterialAliases()
        {
            try
            {
                using var context = new ProductionDbContext();
                // DataGrid'lerde yapılan değişiklikler ItemsSource üzerinden gelir; context'e attach edip Modified işaretleyelim
                void UpsertRange<T>(System.Collections.IEnumerable items) where T : class
                {
                    foreach (var obj in items)
                    {
                        if (obj is T entity)
                        {
                            context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        }
                    }
                }

                // Mixer1
                UpsertRange<AggregateAlias>(AggAliasGrid.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());
                UpsertRange<AdmixtureAlias>(AdmAliasGrid.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());
                UpsertRange<CementAlias>(CementAliasGrid.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());

                // Mixer2
                UpsertRange<Aggregate2Alias>(AggAliasGrid2.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());
                UpsertRange<Admixture2Alias>(AdmAliasGrid2.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());
                UpsertRange<Cement2Alias>(CementAliasGrid2.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());
                UpsertRange<Pigment2Alias>(PigmentAliasGrid2.ItemsSource as System.Collections.IEnumerable ?? Array.Empty<object>());

                context.SaveChanges();
                MessageBox.Show(LocalizationService.Instance.GetString("SettingsWindow.MaterialNamesSavedSuccessfully"), 
                               LocalizationService.Instance.GetString("Common.Successful"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.MaterialNamesSaveError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[SettingsWindow] Malzeme isimleri kaydetme hatası: {ex}");
            }
        }

        private void InitializeDefaultAliases()
        {
            try
            {
                var result = MessageBox.Show(
                    "Bu işlem temel malzeme isimlerini ekleyecek. Mevcut veriler korunacak.\n\nDevam etmek istiyor musunuz?",
                    "Temel Alias Verilerini Ekle",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                using var context = new ProductionDbContext();

                // Mixer1 Alias'ları
                AddDefaultAliasIfNotExists(context.CementAliases, new List<(int Slot, string Name)>
                {
                    (1, "Çimento 1"),
                    (2, "Çimento 2"),
                    (3, "Çimento 3")
                });

                AddDefaultAliasIfNotExists(context.AggregateAliases, new List<(int Slot, string Name)>
                {
                    (1, "Agrega 1"),
                    (2, "Agrega 2"),
                    (3, "Agrega 3"),
                    (4, "Agrega 4"),
                    (5, "Agrega 5")
                });

                AddDefaultAliasIfNotExists(context.AdmixtureAliases, new List<(int Slot, string Name)>
                {
                    (1, "Katkı 1"),
                    (2, "Katkı 2"),
                    (3, "Katkı 3"),
                    (4, "Katkı 4")
                });

                // Mixer2 Alias'ları
                AddDefaultAliasIfNotExists(context.Cement2Aliases, new List<(int Slot, string Name)>
                {
                    (1, "Çimento 1"),
                    (2, "Çimento 2"),
                    (3, "Çimento 3")
                });

                AddDefaultAliasIfNotExists(context.Aggregate2Aliases, new List<(int Slot, string Name)>
                {
                    (1, "Agrega 1"),
                    (2, "Agrega 2"),
                    (3, "Agrega 3"),
                    (4, "Agrega 4"),
                    (5, "Agrega 5"),
                    (6, "Agrega 6"),
                    (7, "Agrega 7"),
                    (8, "Agrega 8")
                });

                AddDefaultAliasIfNotExists(context.Admixture2Aliases, new List<(int Slot, string Name)>
                {
                    (1, "Katkı 1"),
                    (2, "Katkı 2"),
                    (3, "Katkı 3"),
                    (4, "Katkı 4")
                });

                AddDefaultAliasIfNotExists(context.Pigment2Aliases, new List<(int Slot, string Name)>
                {
                    (1, "Pigment 1"),
                    (2, "Pigment 2"),
                    (3, "Pigment 3"),
                    (4, "Pigment 4")
                });

                context.SaveChanges();

                MessageBox.Show(
                    "✅ Temel malzeme isimleri başarıyla eklendi!\n\nAyarlar penceresini yenilemek için kapatıp tekrar açın.",
                    "Başarılı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Verileri yeniden yükle
                LoadMaterialAliases();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Temel alias verileri eklenirken hata oluştu:\n\n{ex.Message}",
                               "Hata",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[SettingsWindow] Temel alias ekleme hatası: {ex}");
            }
        }

        private void AddDefaultAliasIfNotExists<T>(DbSet<T> dbSet, List<(int Slot, string Name)> aliases) where T : class
        {
            foreach (var (slot, name) in aliases)
            {
                // Slot'a göre kontrol et
                var existing = dbSet.AsEnumerable().FirstOrDefault(a => 
                    (int)a.GetType().GetProperty("Slot")!.GetValue(a)! == slot);

                if (existing == null)
                {
                    // Yeni alias oluştur
                    var newAlias = Activator.CreateInstance<T>();
                    newAlias.GetType().GetProperty("Slot")!.SetValue(newAlias, (short)slot); // ✅ int'i short'a cast et
                    newAlias.GetType().GetProperty("Name")!.SetValue(newAlias, name);
                    newAlias.GetType().GetProperty("IsActive")!.SetValue(newAlias, true);
                    
                    dbSet.Add(newAlias);
                }
            }
        }

        #endregion

        #region Saat Dilimi Yönetimi

        private void InitializeTimeZoneSettings()
        {
            // Saat güncelleme timer'ını başlat
            _timeUpdateTimer = new DispatcherTimer();
            _timeUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            _timeUpdateTimer.Tick += UpdateTimeDisplay;
            _timeUpdateTimer.Start();

            // ComboBox'a saat dilimi seçimi ekle
            TimeZoneComboBox.SelectionChanged += TimeZoneComboBox_SelectionChanged;
        }

        private void LoadTimeZoneSettings()
        {
            try
            {
                // Saat dilimi listesini yükle
                var timeZones = TimeZoneInfo.GetSystemTimeZones()
                    .OrderBy(tz => tz.BaseUtcOffset)
                    .ToList();

                TimeZoneComboBox.ItemsSource = timeZones;

                // Kaydedilmiş saat dilimi ayarını yükle
                var savedTimeZoneId = _settingsService.GetSetting("SelectedTimeZone");
                if (!string.IsNullOrEmpty(savedTimeZoneId))
                {
                    try
                    {
                        _selectedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(savedTimeZoneId);
                        TimeZoneComboBox.SelectedValue = savedTimeZoneId;
                    }
                    catch
                    {
                        // Geçersiz saat dilimi ID'si, varsayılan olarak Türkiye saatini kullan
                        _selectedTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                        TimeZoneComboBox.SelectedValue = "Turkey Standard Time";
                    }
                }
                else
                {
                    // Varsayılan olarak Türkiye saatini seç
                    _selectedTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                    TimeZoneComboBox.SelectedValue = "Turkey Standard Time";
                }

                // Otomatik dönüşüm ayarını yükle
                var autoConvertSetting = _settingsService.GetSetting("AutoTimeZoneConvert");
                _autoConvertEnabled = string.IsNullOrEmpty(autoConvertSetting) || autoConvertSetting.ToLower() == "true";
                AutoConvertCheckBox.IsChecked = _autoConvertEnabled;

                UpdateTimeDisplay(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.TimeZoneSettingsLoadError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TimeZoneComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TimeZoneComboBox.SelectedItem is TimeZoneInfo selectedTimeZone)
            {
                _selectedTimeZone = selectedTimeZone;
                UpdateTimeZoneDescription();
                UpdateTimeDisplay(null, null);
            }
        }

        private void UpdateTimeZoneDescription()
        {
            if (_selectedTimeZone != null)
            {
                var offset = _selectedTimeZone.BaseUtcOffset;
                var offsetString = offset.ToString(@"hh\:mm");
                if (offset >= TimeSpan.Zero)
                    offsetString = "+" + offsetString;

                TimeZoneDescriptionTextBlock.Text = 
                    $"UTC{offsetString} - {_selectedTimeZone.DisplayName}\n" +
                    string.Format(LocalizationService.Instance.GetString("SettingsWindow.DaylightSavingTime"), 
                        _selectedTimeZone.SupportsDaylightSavingTime ? 
                            LocalizationService.Instance.GetString("SettingsWindow.Yes") : 
                            LocalizationService.Instance.GetString("SettingsWindow.No"));
            }
        }

        private void UpdateTimeDisplay(object sender, EventArgs e)
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                SystemUtcTimeTextBlock.Text = utcNow.ToString("dd.MM.yyyy HH:mm:ss") + " UTC";

                if (_selectedTimeZone != null)
                {
                    SelectedTimeZoneTextBlock.Text = _selectedTimeZone.DisplayName;
                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _selectedTimeZone);
                    LocalTimeTextBlock.Text = localTime.ToString("dd.MM.yyyy HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsWindow] Saat güncelleme hatası: {ex}");
            }
        }

        private void TestTimeZoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var testResults = new List<string>();

                testResults.Add("=== SAAT DİLİMİ TEST SONUÇLARI ===");
                testResults.Add($"Test Zamanı (UTC): {utcNow:dd.MM.yyyy HH:mm:ss}");
                testResults.Add("");

                if (_selectedTimeZone != null)
                {
                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _selectedTimeZone);
                    testResults.Add($"Seçilen Saat Dilimi: {_selectedTimeZone.DisplayName}");
                    testResults.Add($"Yerel Saat: {localTime:dd.MM.yyyy HH:mm:ss}");
                    testResults.Add($"UTC Farkı: {_selectedTimeZone.BaseUtcOffset:hh\\:mm}");
                    testResults.Add($"Yaz Saati Aktif: {_selectedTimeZone.IsDaylightSavingTime(utcNow)}");
                    testResults.Add("");
                }

                // Veritabanından örnek tarih testi
                testResults.Add("=== VERİTABANI TEST ===");
                try
                {
                    using var context = new ProductionDbContext();
                    var latestBatch = context.ConcreteBatches
                        .OrderByDescending(b => b.CreatedAt)
                        .FirstOrDefault();

                    if (latestBatch != null)
                    {
                        testResults.Add($"Son Batch UTC: {latestBatch.CreatedAt:dd.MM.yyyy HH:mm:ss}");
                        if (_selectedTimeZone != null)
                        {
                            var localBatchTime = TimeZoneInfo.ConvertTimeFromUtc(latestBatch.CreatedAt, _selectedTimeZone);
                            testResults.Add($"Son Batch Yerel: {localBatchTime:dd.MM.yyyy HH:mm:ss}");
                        }
                    }
                    else
                    {
                        testResults.Add("Veritabanında batch bulunamadı.");
                    }
                }
                catch (Exception ex)
                {
                    testResults.Add($"Veritabanı test hatası: {ex.Message}");
                }

                TimeZoneTestResultTextBox.Text = string.Join("\n", testResults);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.TimeZoneTestError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTimeZoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedTimeZone != null)
                {
                    // Saat dilimi ayarını kaydet
                    _settingsService.SetSetting("SelectedTimeZone", _selectedTimeZone.Id);
                    _settingsService.SetSetting("AutoTimeZoneConvert", _autoConvertEnabled.ToString());

                    // TimeZoneHelper cache'ini temizle
                    TimeZoneHelper.ClearCache();

                    MessageBox.Show(
                        string.Format(LocalizationService.Instance.GetString("SettingsWindow.TimeZoneSettingsSavedSuccessfully"), 
                            _selectedTimeZone.DisplayName, 
                            _autoConvertEnabled ? 
                                LocalizationService.Instance.GetString("SettingsWindow.Active") : 
                                LocalizationService.Instance.GetString("SettingsWindow.Passive")),
                        LocalizationService.Instance.GetString("Common.Successful"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LocalizationService.Instance.GetString("SettingsWindow.PleaseSelectTimeZone"), 
                                   LocalizationService.Instance.GetString("Common.Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(LocalizationService.Instance.GetString("SettingsWindow.TimeZoneSettingsSaveError"), ex.Message), 
                               LocalizationService.Instance.GetString("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Saat dilimi dönüşümü için yardımcı metodlar
        public static DateTime ConvertUtcToLocal(DateTime utcDateTime, string? timeZoneId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(timeZoneId))
                {
                    // Varsayılan saat dilimi ayarını yükle
                    var settingsService = new SettingsService();
                    timeZoneId = settingsService.GetSetting("SelectedTimeZone");
                    if (string.IsNullOrEmpty(timeZoneId))
                        timeZoneId = "Turkey Standard Time";
                }

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            }
            catch
            {
                // Hata durumunda UTC'yi döndür
                return utcDateTime;
            }
        }

        public static DateTime ConvertLocalToUtc(DateTime localDateTime, string? timeZoneId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(timeZoneId))
                {
                    // Varsayılan saat dilimi ayarını yükle
                    var settingsService = new SettingsService();
                    timeZoneId = settingsService.GetSetting("SelectedTimeZone");
                    if (string.IsNullOrEmpty(timeZoneId))
                        timeZoneId = "Turkey Standard Time";
                }

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
            }
            catch
            {
                // Hata durumunda DateTime.Now'ı UTC'ye çevir
                return DateTime.UtcNow;
            }
        }

        #endregion
    }
}




