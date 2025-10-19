using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace takip.Services
{
    /// <summary>
    /// PLC veri yönetimi ve arka plan polling servisi
    /// </summary>
    public class PlcDataService : IDisposable
    {
        #region Private Fields

        private readonly OmronPlcReader _plcReader;
        private readonly DispatcherTimer _pollingTimer;
        private Dictionary<string, PlcRegisterData> _lastData = new();
        private bool _isRunning = false;
        private readonly object _lockObject = new object();

        #endregion

        #region Events

        public event EventHandler<PlcDataChangedEventArgs>? DataChanged;
        public event EventHandler<string>? LogMessage;

        #endregion

        #region Constructor

        public PlcDataService()
        {
            _plcReader = new OmronPlcReader();
            _plcReader.DataChanged += OnPlcReaderDataChanged;

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // 2 saniye aralıklarla oku
            };
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Servisi başlat
        /// </summary>
        public async Task<bool> StartAsync()
        {
            try
            {
                LogMessage?.Invoke(this, "PLC servisi başlatılıyor...");

                // PLC'ye bağlan
                var connected = await _plcReader.ConnectAsync();
                if (!connected)
                {
                    LogMessage?.Invoke(this, "PLC'ye bağlanılamadı!");
                    return false;
                }

                LogMessage?.Invoke(this, $"PLC'ye başarıyla bağlanıldı ({_plcReader.GetType().Name})");

                // İlk veri okumasını yap
                await ReadAndProcessData();

                // Timer'ı başlat
                _pollingTimer.Start();
                _isRunning = true;

                LogMessage?.Invoke(this, "PLC veri okuma servisi başlatıldı (1 saniye aralıklarla)");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"PLC servisi başlatma hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Servisi durdur
        /// </summary>
        public void Stop()
        {
            try
            {
                _isRunning = false;
                _pollingTimer.Stop();
                _plcReader.Disconnect();
                LogMessage?.Invoke(this, "PLC servisi durduruldu");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"PLC servisi durdurma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Servis çalışıyor mu?
        /// </summary>
        public bool IsRunning => _isRunning && _plcReader.IsConnected;

        /// <summary>
        /// Son okunan verileri al
        /// </summary>
        public Dictionary<string, PlcRegisterData> GetLastData()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, PlcRegisterData>(_lastData);
            }
        }

        #endregion

        #region Private Methods

        private async void PollingTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                await ReadAndProcessData();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Veri okuma hatası: {ex.Message}");
            }
        }

        private async Task ReadAndProcessData()
        {
            try
            {
                // H register'ları oku
                var hRegisterData = await _plcReader.ReadAllHRegistersAsync();
                
                // D register'ları oku
                var dRegisterData = await _plcReader.ReadAllDRegistersAsync();
                
                // İki veri setini birleştir
                var currentData = new Dictionary<string, PlcRegisterData>();
                foreach (var kvp in hRegisterData)
                {
                    currentData[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in dRegisterData)
                {
                    currentData[kvp.Key] = kvp.Value;
                }
                
                // 🔍 DEBUG: H register log'u kaldırıldı (çok fazla gürültü yapıyordu)
                // Mixer1 sistemi kendi log'larını üretiyor
                
                lock (_lockObject)
                {
                    // Değişiklikleri tespit et
                    var changedRegisters = new List<string>();
                    var previousData = new Dictionary<string, PlcRegisterData>(_lastData);

                    foreach (var kvp in currentData)
                    {
                        var address = kvp.Key;
                        var current = kvp.Value;

                        if (_lastData.ContainsKey(address))
                        {
                            var previous = _lastData[address];
                            
                            // Önceki değerleri kaydet
                            current.PreviousValue = previous.Value;
                            current.PreviousNumericValue = previous.NumericValue;
                            current.PreviousReadTime = previous.ReadTime;
                            
                            // Değişiklik kontrolü (hem boolean hem numeric)
                            if (previous.Value != current.Value || previous.NumericValue != current.NumericValue)
                            {
                                changedRegisters.Add(address);
                            }
                        }
                        else
                        {
                            // İlk okuma
                            changedRegisters.Add(address);
                        }
                    }

                    // Değişiklik varsa event fırlat
                    if (changedRegisters.Count > 0)
                    {
                        var eventArgs = new PlcDataChangedEventArgs
                        {
                            CurrentData = new Dictionary<string, PlcRegisterData>(currentData),
                            PreviousData = previousData,
                            ChangedRegisters = changedRegisters,
                            ChangeTime = DateTime.Now
                        };

                        DataChanged?.Invoke(this, eventArgs);

                        // Log mesajı kaldırıldı - çok fazla gürültü yapıyordu
                        // Mixer1 sistemi kendi log'larını üretiyor
                    }

                    // Son verileri güncelle
                    _lastData = new Dictionary<string, PlcRegisterData>(currentData);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Veri işleme hatası: {ex.Message}");
            }
        }

        private void OnPlcReaderDataChanged(object? sender, PlcDataChangedEventArgs e)
        {
            // Bu event şu an kullanılmıyor, gelecekte kullanılabilir
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Stop();
            _plcReader?.Dispose();
            _pollingTimer?.Stop();
        }

        #endregion
    }
}
