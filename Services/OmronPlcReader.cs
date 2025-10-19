using HslCommunication;
using HslCommunication.Profinet.Omron;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using takip.Services;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// OMRON PLC H Register Okuma Servisi
    /// </summary>
    public class OmronPlcReader : IDisposable
    {
        #region Private Fields

        private OmronFinsNet? _omronFins;
        private bool _isConnected = false;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly SettingsService _settingsService = new SettingsService();

        // H Register bit adresleri ve açıklamaları
        private readonly Dictionary<string, string> _hRegisterDescriptions = new Dictionary<string, string>
        {
            // Mixer1 İçi Durum Sinyalleri (H70.x)
            {"H70.0", "M1_MixerAgregaVar (Mixer1'de Agrega Var)"},
            {"H70.1", "M1_MixerCimentoVar (Mixer1'de Çimento Var)"},
            {"H70.2", "M1_MixerKatkiVar (Mixer1'de Katkı Var)"},
            {"H70.3", "M1_MixerLoadcellSuVar (Mixer1'de Loadcell Su Var)"},
            {"H70.4", "M1_MixerPulseSuVar (Mixer1'de Pulse Su Var)"},
            {"H70.5", "M1_HarcHazir (Mixer1 Harç Hazır)"},
            {"H70.7", "M1_WaitingBunker (Mixer1 Bekleme Bunkeri - H70.7)"},
            {"H70.9", "M1_WaitingBunker_Alt (Mixer1 Bekleme Bunkeri - H70.9)"},
            
            // Ana Mixer Sinyalleri (H71.x - Eski sistem / Mixer2)
            {"H71.1", "MixerCementRegister (Mixer Çimento Sinyali)"},
            {"H71.2", "MixerAdmixtureRegister (Mixer Katkı Sinyali)"},
            {"H71.3", "MixerWaterLoadcellRegister (Mixer Su Loadcell Sinyali)"},
            {"H71.4", "MixerWaterPulseRegister (Mixer Su Pulse Sinyali)"},
            {"H71.5", "HarçHazırRegister (Harç Hazır Sinyali - Eski)"},
            {"H71.7", "HorizontalBucketRegister (Yatay Kova Sinyali)"},
            {"H71.10", "VerticalBucketRegister (Dikey Kova Sinyali)"},
            {"H71.11", "WaitingBunkerRegister (Bekleme Bunker Sinyali - Eski)"},
            
            // Çimento Register'ları
            {"H65.0", "CementGroupRegister (Çimento Grup Sinyali)"},
            {"H65.2", "Cement1Register (Çimento 1 Sinyali)"},
            {"H65.7", "Cement1TartimOk (Çimento 1 Tartım OK)"},
            {"H66.2", "Cement2Register (Çimento 2 Sinyali)"},
            {"H66.7", "Cement2TartimOk (Çimento 2 Tartım OK)"},
            {"H67.2", "Cement3Register (Çimento 3 Sinyali)"},
            {"H67.7", "Cement3TartimOk (Çimento 3 Tartım OK)"},
            
            // Agrega Register'ları
            {"H52.2", "Aggregate2Register (Agrega 2 Sinyali)"},
            {"H53.2", "Aggregate3Register (Agrega 3 Sinyali)"},
            {"H54.2", "Aggregate4Register (Agrega 4 Sinyali)"},
            {"H55.2", "Aggregate5Register (Agrega 5 Sinyali)"},
            {"H56.2", "Aggregate6Register (Agrega 6 Sinyali)"},
            {"H57.2", "Aggregate7Register (Agrega 7 Sinyali)"},
            {"H58.2", "Aggregate8Register (Agrega 8 Sinyali)"},
            
            // Su Register'ları
            {"H61.2", "WaterGroupRegister (Su Grup Sinyali)"},
            {"H61.0", "WaterLoadcellRegister (Su Loadcell Sinyali)"},
            {"H14.0", "WaterPulseRegister (Su Pulse Sinyali)"},
            
            // Katkı Register'ları
            {"H39.10", "AdmixtureGroupRegister (Katkı Grup Sinyali)"},
            {"H39.0", "Admixture1Register (Katkı 1 Sinyali)"},
            {"H40.0", "Admixture2Register (Katkı 2 Sinyali)"},
            {"H41.0", "Admixture3Register (Katkı 3 Sinyali)"},
            {"H42.0", "Admixture4Register (Katkı 4 Sinyali)"},
            
            // Pigment Register'ları
            {"H31.2", "PigmentGroupRegister (Pigment Grup Sinyali)"},
            {"H31.10", "Pigment1Register (Pigment 1 Sinyali)"},
            {"H31.3", "Pigment1TartimOk (Pigment 1 Tartım OK)"},
            {"H32.10", "Pigment2Register (Pigment 2 Sinyali)"},
            {"H32.3", "Pigment2TartimOk (Pigment 2 Tartım OK)"},
            {"H33.10", "Pigment3Register (Pigment 3 Sinyali)"},
            {"H33.3", "Pigment3TartimOk (Pigment 3 Tartım OK)"},
            {"H34.10", "Pigment4Register (Pigment 4 Sinyali)"},
            {"H34.3", "Pigment4TartimOk (Pigment 4 Tartım OK)"},
            
            // Mixer1 Özel Register'ları
            {"H30", "M1PigmentGroupData (Mixer1 Pigment Grup)"},
            {"H35", "M1AdmixtureGroupData (Mixer1 Katkı Grup)"},
            {"H45", "M1AggregateGroupData (Mixer1 Agrega Grup)"},
            {"H60", "M1WaterGroupData (Mixer1 Su Grup)"},
            {"H60.6", "M1_SuLoadcellTartimOk (Mixer1 Su Loadcell Tartım OK)"},
            {"H62", "M1CementGroupData (Mixer1 Çimento Grup)"},
            {"H63", "M1Cement2Data (Mixer1 Çimento 2)"},
            {"H64", "M1Cement3Data (Mixer1 Çimento 3)"},
            
            // Mixer1 Çimento Aktif Sinyalleri
            {"H62.2", "M1_Cimento1Aktif (Mixer1 Çimento 1 Aktif)"},
            {"H62.7", "M1_Cimento1TartimOk (Mixer1 Çimento 1 Tartım OK)"},
            {"H63.2", "M1_Cimento2Aktif (Mixer1 Çimento 2 Aktif)"},
            {"H63.7", "M1_Cimento2TartimOk (Mixer1 Çimento 2 Tartım OK)"},
            {"H64.2", "M1_Cimento3Aktif (Mixer1 Çimento 3 Aktif)"},
            {"H64.7", "M1_Cimento3TartimOk (Mixer1 Çimento 3 Tartım OK)"},
            
            // Mixer1 Agrega Aktif Sinyalleri
            {"H45.2", "M1Aggregate1ActiveRegister (Mixer1 Agrega 1 Aktif)"},
            {"H46.2", "M1Aggregate2ActiveRegister (Mixer1 Agrega 2 Aktif)"},
            {"H47.2", "M1Aggregate3ActiveRegister (Mixer1 Agrega 3 Aktif)"},
            {"H48.2", "M1Aggregate4ActiveRegister (Mixer1 Agrega 4 Aktif)"},
            {"H49.2", "M1Aggregate5ActiveRegister (Mixer1 Agrega 5 Aktif)"},
            
            // Mixer1 Agrega Tartım OK Sinyalleri
            {"H45.7", "M1Aggregate1TartimOkRegister (Mixer1 Agrega 1 Tartım OK)"},
            {"H46.7", "M1Aggregate2TartimOkRegister (Mixer1 Agrega 2 Tartım OK)"},
            {"H47.7", "M1Aggregate3TartimOkRegister (Mixer1 Agrega 3 Tartım OK)"},
            {"H48.7", "M1Aggregate4TartimOkRegister (Mixer1 Agrega 4 Tartım OK)"},
            {"H49.7", "M1Aggregate5TartimOkRegister (Mixer1 Agrega 5 Tartım OK)"},
            
            // Mixer1 Su Aktif Sinyalleri
            {"H60.0", "M1_SuLoadcellAktif (Mixer1 Su Loadcell Aktif)"},
            {"H60.3", "M1_SuGrupAktif (Mixer1 Su Grubu Aktif)"},
            {"H04.0", "M1_SuPulseAktif (Mixer1 Su Pulse Aktif)"},
            
            // Mixer1 Katkı Aktif Sinyalleri
            {"H35.0", "M1_Katki1Aktif (Mixer1 Katkı 1 Aktif)"},
            {"H36.0", "M1_Katki2Aktif (Mixer1 Katkı 2 Aktif)"},
            {"H37.0", "M1_Katki3Aktif (Mixer1 Katkı 3 Aktif)"},
            {"H38.0", "M1_Katki4Aktif (Mixer1 Katkı 4 Aktif)"}
        };

        // D Register adresleri ve açıklamaları (KG değerleri için)
        private readonly Dictionary<string, string> _dRegisterDescriptions = new Dictionary<string, string>
        {
            // Çimento KG Register'ları
            {"DM4434", "Cement1Kg (Çimento 1 KG)"},
            {"DM4444", "Cement2Kg (Çimento 2 KG)"},
            {"DM4454", "Cement3Kg (Çimento 3 KG)"},
            
            // Agrega KG Register'ları
            {"DM4704", "Aggregate1Kg (Agrega 1 KG)"},
            {"DM4714", "Aggregate2Kg (Agrega 2 KG)"},
            {"DM4724", "Aggregate3Kg (Agrega 3 KG)"},
            {"DM4734", "Aggregate4Kg (Agrega 4 KG)"},
            {"DM4744", "Aggregate5Kg (Agrega 5 KG)"},
            {"DM4754", "Aggregate6Kg (Agrega 6 KG)"},
            {"DM4764", "Aggregate7Kg (Agrega 7 KG)"},
            {"DM4774", "Aggregate8Kg (Agrega 8 KG)"},
            
            // Su KG Register'ları
            {"DM304", "WaterLoadcellKg (Su Loadcell KG)"},
            {"DM306", "WaterPulseKg (Su Pulse KG)"},
            
            // Katkı KG Register'ları
            {"DM4604", "Admixture1ChemicalKg (Katkı 1 Kimyasal KG)"},
            {"DM4605", "Admixture1WaterKg (Katkı 1 Su KG)"},
            {"DM4614", "Admixture2ChemicalKg (Katkı 2 Kimyasal KG)"},
            {"DM4615", "Admixture2WaterKg (Katkı 2 Su KG)"},
            {"DM4624", "Admixture3ChemicalKg (Katkı 3 Kimyasal KG)"},
            {"DM4625", "Admixture3WaterKg (Katkı 3 Su KG)"},
            {"DM4634", "Admixture4ChemicalKg (Katkı 4 Kimyasal KG)"},
            {"DM4635", "Admixture4WaterKg (Katkı 4 Su KG)"},
            
            // Pigment KG Register'ları
            {"DM308", "Pigment1Kg (Pigment 1 KG)"},
            {"DM310", "Pigment2Kg (Pigment 2 KG)"},
            {"DM312", "Pigment3Kg (Pigment 3 KG)"},
            {"DM314", "Pigment4Kg (Pigment 4 KG)"},
            
            // Mixer1 KG Register'ları
            {"DM4204", "M1Aggregate1Kg (Mixer1 Agrega 1 KG)"},
            {"DM4214", "M1Aggregate2Kg (Mixer1 Agrega 2 KG)"},
            {"DM4224", "M1Aggregate3Kg (Mixer1 Agrega 3 KG)"},
            {"DM4234", "M1Aggregate4Kg (Mixer1 Agrega 4 KG)"},
            {"DM4244", "M1Aggregate5Kg (Mixer1 Agrega 5 KG)"},
            {"DM4404", "M1Cement1Kg (Mixer1 Çimento 1 KG)"},
            {"DM4414", "M1Cement2Kg (Mixer1 Çimento 2 KG)"},
            {"DM4424", "M1Cement3Kg (Mixer1 Çimento 3 KG)"},
            {"DM204", "M1WaterLoadcellKg (Mixer1 Su Loadcell KG)"},
            {"DM130", "M1WaterPulseKg (Mixer1 Su Pulse KG)"},
            {"DM210", "M1WaterPulseKg_OLD (Mixer1 Su Pulse KG - Eski)"},
            {"DM208", "M1Pigment1Kg (Mixer1 Pigment 1 KG)"},
            
            // Diğer Register'lar
            {"DM120", "M1Humidity (Mixer1 Nem %)"},
            {"DM122", "M2Humidity (Mixer2 Nem %)"}
        };

        #endregion

        #region Events

        public event EventHandler<PlcDataChangedEventArgs>? DataChanged;

        #endregion

        #region Constructor

        public OmronPlcReader(string ipAddress = "192.168.250.10", int port = 9600)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// PLC'ye bağlan
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _omronFins = new OmronFinsNet(_ipAddress, _port);
                _omronFins.ConnectTimeOut = 3000;
                _omronFins.ReceiveTimeOut = 3000;

                var connectResult = await Task.Run(() => _omronFins.ConnectServer());

                if (connectResult.IsSuccess)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    _isConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                System.Diagnostics.Debug.WriteLine($"PLC bağlantı hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PLC bağlantısını kes
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _isConnected = false;
                _omronFins?.ConnectClose();
                _omronFins = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PLC bağlantı kesme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Bağlantı durumunu kontrol et
        /// </summary>
        public bool IsConnected => _isConnected && _omronFins != null;

        /// <summary>
        /// Tüm H register verilerini paket olarak oku
        /// </summary>
        public async Task<Dictionary<string, PlcRegisterData>> ReadAllHRegistersAsync()
        {
            var result = new Dictionary<string, PlcRegisterData>();

            if (!IsConnected)
            {
                return result;
            }

            try
            {
                // H register'ları paket olarak oku (H14'ten H71'e kadar)
                var readResult = await Task.Run(() => _omronFins!.ReadUInt16("H14", 58)); // H14-H71 arası 58 word
                
                if (readResult.IsSuccess && readResult.Content != null)
                {
                    var wordData = readResult.Content;
                    var readTime = DateTime.Now;
                    
                    // Ayarlardan gelen H* adreslerini de dahil et
                    var settings = _settingsService.LoadPlcSettings();
                    var configuredH = settings.Registers.Values
                        .OfType<string>()
                        .Where(v => v.StartsWith("H", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var targetRegisters = new HashSet<string>(_hRegisterDescriptions.Keys);
                    foreach (var addr in configuredH) targetRegisters.Add(addr);

                    // Her register için bit'leri parse et
                    foreach (var register in targetRegisters)
                    {
                        try
                        {
                            var bitValue = ParseBitFromWordData(register, wordData);
                            
                            var registerData = new PlcRegisterData
                            {
                                Address = register,
                                Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                                Value = bitValue,
                                IsSuccess = true,
                                ReadTime = readTime
                            };

                            result[register] = registerData;
                        }
                        catch (Exception ex)
                        {
                            // Paket okuma başarısız, bu register'ı tek tek oku
                            try
                            {
                                var individualResult = await Task.Run(() => _omronFins!.ReadBool(register));
                                
                                var registerData = new PlcRegisterData
                                {
                                    Address = register,
                                    Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                                    Value = individualResult.IsSuccess ? individualResult.Content : false,
                                    IsSuccess = individualResult.IsSuccess,
                                    ReadTime = readTime,
                                    ErrorMessage = individualResult.IsSuccess ? null : individualResult.Message
                                };

                                result[register] = registerData;
                            }
                            catch (Exception ex2)
                            {
                                var registerData = new PlcRegisterData
                                {
                                    Address = register,
                                    Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                                    Value = false,
                                    IsSuccess = false,
                                    ReadTime = readTime,
                                    ErrorMessage = $"Paket: {ex.Message}, Tekil: {ex2.Message}"
                                };

                                result[register] = registerData;
                            }
                        }
                    }
                }
                else
                {
                    // Paket okuma başarısız, tüm register'ları tek tek oku
                    var readTime = DateTime.Now;
                    var settings = _settingsService.LoadPlcSettings();
                    var configuredH = settings.Registers.Values
                        .OfType<string>()
                        .Where(v => v.StartsWith("H", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var targetRegisters = new HashSet<string>(_hRegisterDescriptions.Keys);
                    foreach (var addr in configuredH) targetRegisters.Add(addr);
                    foreach (var register in targetRegisters)
                    {
                        try
                        {
                            var individualResult = await Task.Run(() => _omronFins!.ReadBool(register));
                            
                            var registerData = new PlcRegisterData
                            {
                                Address = register,
                                Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                                Value = individualResult.IsSuccess ? individualResult.Content : false,
                                IsSuccess = individualResult.IsSuccess,
                                ReadTime = readTime,
                                ErrorMessage = individualResult.IsSuccess ? null : individualResult.Message
                            };

                            result[register] = registerData;
                        }
                        catch (Exception ex)
                        {
                            var registerData = new PlcRegisterData
                            {
                                Address = register,
                                Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                                Value = false,
                                IsSuccess = false,
                                ReadTime = readTime,
                                ErrorMessage = ex.Message
                            };

                            result[register] = registerData;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Genel hata durumu
                var readTime = DateTime.Now;
                var settings = _settingsService.LoadPlcSettings();
                var configuredH = settings.Registers.Values
                    .OfType<string>()
                    .Where(v => v.StartsWith("H", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var targetRegisters = new HashSet<string>(_hRegisterDescriptions.Keys);
                foreach (var addr in configuredH) targetRegisters.Add(addr);
                foreach (var register in targetRegisters)
                {
                    var registerData = new PlcRegisterData
                    {
                        Address = register,
                        Description = _hRegisterDescriptions.TryGetValue(register, out var desc) ? desc : "Configured",
                        Value = false,
                        IsSuccess = false,
                        ReadTime = readTime,
                        ErrorMessage = ex.Message
                    };

                    result[register] = registerData;
                }
            }

            return result;
        }

        /// <summary>
        /// Tüm D register verilerini oku (KG değerleri için)
        /// </summary>
        public async Task<Dictionary<string, PlcRegisterData>> ReadAllDRegistersAsync()
        {
            var result = new Dictionary<string, PlcRegisterData>();

            if (!IsConnected)
            {
                return result;
            }

            try
            {
                var readTime = DateTime.Now;

                // DM adreslerini topla ve sayısal değerlere dönüştür (ör: "DM4704" -> 4704)
                var dmAddresses = _dRegisterDescriptions.Keys
                    .Where(k => k.StartsWith("DM", StringComparison.OrdinalIgnoreCase))
                    .Select(k => (Key: k, Addr: int.Parse(k.Substring(2))))
                    .OrderBy(x => x.Addr)
                    .ToList();

                // Kümeleme: yüzlük aralığa göre (örn: 300-399, 4400-4499, 4700-4799)
                var clusters = dmAddresses
                    .GroupBy(x => x.Addr / 100)
                    .ToList();

                foreach (var cluster in clusters)
                {
                    var clusterList = cluster.OrderBy(x => x.Addr).ToList();
                    var minAddr = clusterList.First().Addr;
                    var maxAddr = clusterList.Last().Addr;
                    var wordCount = (ushort)(maxAddr - minAddr + 1);

                    try
                    {
                        // Toplu okuma: "DM{min}" başlangıçtan itibaren wordCount kadar
                        var startAddressString = $"DM{minAddr}";
                        var readResult = await Task.Run(() => _omronFins!.ReadUInt16(startAddressString, wordCount));

                        if (readResult.IsSuccess && readResult.Content != null)
                        {
                            var data = readResult.Content;
                            foreach (var (Key, Addr) in clusterList)
                            {
                                var offset = Addr - minAddr;
                                ushort value = 0;
                                var success = offset >= 0 && offset < data.Length;
                                if (success)
                                {
                                    value = NormalizeDmValue(Key, data[offset]);
                                }

                                var registerData = new PlcRegisterData
                                {
                                    Address = Key,
                                    Description = _dRegisterDescriptions[Key],
                                    Value = success ? value > 0 : false,
                                    NumericValue = success ? value : (ushort)0,
                                    IsSuccess = success,
                                    ReadTime = readTime,
                                    ErrorMessage = success ? null : "Toplu okuma aralığı dışında"
                                };
                                result[Key] = registerData;
                            }
                        }
                        else
                        {
                            // Toplu okuma başarısızsa, bu kümedeki adresleri tek tek oku
                            foreach (var (Key, Addr) in clusterList)
                            {
                                try
                                {
                                    var single = await Task.Run(() => _omronFins!.ReadUInt16(Key));
                                    var norm = single.IsSuccess ? NormalizeDmValue(Key, single.Content) : (ushort)0;
                                    var registerData = new PlcRegisterData
                                    {
                                        Address = Key,
                                        Description = _dRegisterDescriptions[Key],
                                        Value = single.IsSuccess ? norm > 0 : false,
                                        NumericValue = norm,
                                        IsSuccess = single.IsSuccess,
                                        ReadTime = readTime,
                                        ErrorMessage = single.IsSuccess ? null : single.Message
                                    };
                                    result[Key] = registerData;
                                }
                                catch (Exception ex)
                                {
                                    var registerData = new PlcRegisterData
                                    {
                                        Address = Key,
                                        Description = _dRegisterDescriptions[Key],
                                        Value = false,
                                        NumericValue = 0,
                                        IsSuccess = false,
                                        ReadTime = readTime,
                                        ErrorMessage = ex.Message
                                    };
                                    result[Key] = registerData;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Küme genel hatası - hepsini tek tek dene
                        foreach (var (Key, Addr) in clusterList)
                        {
                            try
                            {
                                var single = await Task.Run(() => _omronFins!.ReadUInt16(Key));
                                var norm = single.IsSuccess ? NormalizeDmValue(Key, single.Content) : (ushort)0;
                                var registerData = new PlcRegisterData
                                {
                                    Address = Key,
                                    Description = _dRegisterDescriptions[Key],
                                    Value = single.IsSuccess ? norm > 0 : false,
                                    NumericValue = norm,
                                    IsSuccess = single.IsSuccess,
                                    ReadTime = readTime,
                                    ErrorMessage = single.IsSuccess ? null : single.Message
                                };
                                result[Key] = registerData;
                            }
                            catch (Exception ex2)
                            {
                                var registerData = new PlcRegisterData
                                {
                                    Address = Key,
                                    Description = _dRegisterDescriptions[Key],
                                    Value = false,
                                    NumericValue = 0,
                                    IsSuccess = false,
                                    ReadTime = readTime,
                                    ErrorMessage = $"Küme: {ex.Message}, Tekil: {ex2.Message}"
                                };
                                result[Key] = registerData;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Genel hata durumu
                var readTime = DateTime.Now;
                foreach (var register in _dRegisterDescriptions.Keys)
                {
                    var registerData = new PlcRegisterData
                    {
                        Address = register,
                        Description = _dRegisterDescriptions[register],
                        Value = false,
                        NumericValue = 0,
                        IsSuccess = false,
                        ReadTime = readTime,
                        ErrorMessage = ex.Message
                    };

                    result[register] = registerData;
                }
            }

            return result;
        }

        /// <summary>
        /// DM değerlerindeki eksi-değer/overflow (65000+) sapmalarını normalize et
        /// Sadece kilo değerlerini (DM44xx, DM47xx gibi) ve su/pigment/admixture kilo adreslerini düzeltir
        /// </summary>
        private static ushort NormalizeDmValue(string key, ushort value)
        {
            // 65000+ civarı wrap-around sapmalarını 0'a çek
            // Eşik: 65000 ve üzeri
            if (value >= 65000)
            {
                return 0;
            }

            return value;
        }

        /// <summary>
        /// Word verisinden belirli bir bit'i parse et
        /// </summary>
        private bool ParseBitFromWordData(string registerAddress, ushort[] wordData)
        {
            try
            {
                // Register adresini parse et (örn: "H70.0" -> wordIndex=56, bitIndex=0)
                var parts = registerAddress.Split('.');
                var wordAddress = parts[0]; // "H70"
                var bitIndex = parts.Length > 1 ? int.Parse(parts[1]) : 0; // "0" veya 0
                
                // H14 = index 0, H15 = index 1, ..., H71 = index 57
                var wordNumber = int.Parse(wordAddress.Substring(1)); // H70 -> 70
                var wordIndex = wordNumber - 14; // H70 -> 70-14=56
                
                if (wordIndex >= 0 && wordIndex < wordData.Length)
                {
                    var word = wordData[wordIndex];
                    return (word & (1 << bitIndex)) != 0;
                }
                
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// PLC verilerinden PlcDataSnapshot oluştur
        /// </summary>
        public async Task<PlcDataSnapshot> CreateSnapshotAsync(string operatorName = "SYSTEM", string recipeCode = "AUTO")
        {
            try
            {
                var hRegisters = await ReadAllHRegistersAsync();
                var dmRegisters = await ReadAllDRegistersAsync();
                
                var snapshot = new PlcDataSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Operator = operatorName,
                    RecipeCode = recipeCode,
                    
                    // Grup aktiflik durumları
                    AggregateGroupActive = GetBoolValue(hRegisters, "H45.0"),
                    WaterGroupActive = GetBoolValue(hRegisters, "H60.0"),
                    CementGroupActive = GetBoolValue(hRegisters, "H62.0"),
                    AdmixtureGroupActive = GetBoolValue(hRegisters, "H35.0"),
                    PigmentGroupActive = GetBoolValue(hRegisters, "H31.2"),
                    
                    // Agrega verileri (8 slot)
                    Aggregate1Active = GetBoolValue(hRegisters, "H45.1"),
                    Aggregate1Amount = GetDoubleValue(dmRegisters, "DM4700"),
                    Aggregate1TartimOk = GetBoolValue(hRegisters, "H45.2"),
                    
                    Aggregate2Active = GetBoolValue(hRegisters, "H45.3"),
                    Aggregate2Amount = GetDoubleValue(dmRegisters, "DM4702"),
                    Aggregate2TartimOk = GetBoolValue(hRegisters, "H45.4"),
                    
                    Aggregate3Active = GetBoolValue(hRegisters, "H45.5"),
                    Aggregate3Amount = GetDoubleValue(dmRegisters, "DM4704"),
                    Aggregate3TartimOk = GetBoolValue(hRegisters, "H45.6"),
                    
                    Aggregate4Active = GetBoolValue(hRegisters, "H45.7"),
                    Aggregate4Amount = GetDoubleValue(dmRegisters, "DM4706"),
                    Aggregate4TartimOk = GetBoolValue(hRegisters, "H45.8"),
                    
                    Aggregate5Active = GetBoolValue(hRegisters, "H46.1"),
                    Aggregate5Amount = GetDoubleValue(dmRegisters, "DM4708"),
                    Aggregate5TartimOk = GetBoolValue(hRegisters, "H46.2"),
                    
                    Aggregate6Active = GetBoolValue(hRegisters, "H46.3"),
                    Aggregate6Amount = GetDoubleValue(dmRegisters, "DM4710"),
                    
                    Aggregate7Active = GetBoolValue(hRegisters, "H46.5"),
                    Aggregate7Amount = GetDoubleValue(dmRegisters, "DM4712"),
                    
                    Aggregate8Active = GetBoolValue(hRegisters, "H46.7"),
                    Aggregate8Amount = GetDoubleValue(dmRegisters, "DM4714"),
                    
                    // Su verileri (2 slot)
                    Water1Active = GetBoolValue(hRegisters, "H60.1"),
                    Water1Amount = GetDoubleValue(dmRegisters, "DM4720"),
                    
                    Water2Active = GetBoolValue(hRegisters, "H60.3"),
                    Water2Amount = GetDoubleValue(dmRegisters, "DM4722"),
                    
                    // Çimento verileri (3 slot)
                    Cement1Active = GetBoolValue(hRegisters, "H62.1"),
                    Cement1Amount = GetDoubleValue(dmRegisters, "DM4730"),
                    
                    Cement2Active = GetBoolValue(hRegisters, "H62.3"),
                    Cement2Amount = GetDoubleValue(dmRegisters, "DM4732"),
                    
                    Cement3Active = GetBoolValue(hRegisters, "H62.5"),
                    Cement3Amount = GetDoubleValue(dmRegisters, "DM4734"),
                    
                    // Katkı verileri (4 slot + tartım ok)
                    Admixture1Active = GetBoolValue(hRegisters, "H39.0"),
                    Admixture1TartimOk = GetBoolValue(hRegisters, "H39.3"),
                    Admixture1WaterTartimOk = GetBoolValue(hRegisters, "H39.4"),
                    Admixture1ChemicalAmount = GetDoubleValue(dmRegisters, "DM4604"),
                    Admixture1WaterAmount = GetDoubleValue(dmRegisters, "DM4605"),
                    
                    Admixture2Active = GetBoolValue(hRegisters, "H40.0"),
                    Admixture2TartimOk = GetBoolValue(hRegisters, "H40.3"),
                    Admixture2WaterTartimOk = GetBoolValue(hRegisters, "H40.4"),
                    Admixture2ChemicalAmount = GetDoubleValue(dmRegisters, "DM4614"),
                    Admixture2WaterAmount = GetDoubleValue(dmRegisters, "DM4615"),
                    
                    Admixture3Active = GetBoolValue(hRegisters, "H41.0"),
                    Admixture3TartimOk = GetBoolValue(hRegisters, "H41.3"),
                    Admixture3WaterTartimOk = GetBoolValue(hRegisters, "H41.4"),
                    Admixture3ChemicalAmount = GetDoubleValue(dmRegisters, "DM4624"),
                    Admixture3WaterAmount = GetDoubleValue(dmRegisters, "DM4625"),
                    
                    Admixture4Active = GetBoolValue(hRegisters, "H42.0"),
                    Admixture4TartimOk = GetBoolValue(hRegisters, "H42.3"),
                    Admixture4WaterTartimOk = GetBoolValue(hRegisters, "H42.4"),
                    Admixture4ChemicalAmount = GetDoubleValue(dmRegisters, "DM4634"),
                    Admixture4WaterAmount = GetDoubleValue(dmRegisters, "DM4635"),
                    
                    // Pigment verileri (4 slot + tartım ok)
                    Pigment1Active = GetBoolValue(hRegisters, "H31.10"),
                    Pigment1TartimOk = GetBoolValue(hRegisters, "H31.3"),
                    Pigment1Amount = GetDoubleValue(dmRegisters, "DM308"),
                    
                    Pigment2Active = GetBoolValue(hRegisters, "H32.10"),
                    Pigment2TartimOk = GetBoolValue(hRegisters, "H32.3"),
                    Pigment2Amount = GetDoubleValue(dmRegisters, "DM310"),
                    
                    Pigment3Active = GetBoolValue(hRegisters, "H33.10"),
                    Pigment3TartimOk = GetBoolValue(hRegisters, "H33.3"),
                    Pigment3Amount = GetDoubleValue(dmRegisters, "DM312"),
                    
                    Pigment4Active = GetBoolValue(hRegisters, "H34.10"),
                    Pigment4TartimOk = GetBoolValue(hRegisters, "H34.3"),
                    Pigment4Amount = GetDoubleValue(dmRegisters, "DM314"),
                    
                    // Nem verisi
                    MoisturePercent = GetDoubleValue(dmRegisters, "DM4800"),
                    
                    // Harç Hazır sinyali
                    BatchReadySignal = GetBoolValue(hRegisters, "H70.0"),
                    
                    // Konveyör/kova durumu sinyalleri
                    HorizontalHasMaterial = GetBoolValue(hRegisters, "H50.0"),
                    VerticalHasMaterial = GetBoolValue(hRegisters, "H50.1"),
                    WaitingBunkerHasMaterial = GetBoolValue(hRegisters, "H50.2"),
                    MixerHasAggregate = GetBoolValue(hRegisters, "H50.3"),
                    
                    // Mixer içerik sinyalleri
                    MixerHasCement = GetBoolValue(hRegisters, "H50.4"),
                    MixerHasAdmixture = GetBoolValue(hRegisters, "H50.5"),
                    MixerHasWaterLoadcell = GetBoolValue(hRegisters, "H50.6"),
                    MixerHasWaterPulse = GetBoolValue(hRegisters, "H50.7"),
                    
                    // Ham veri (JSON)
                    RawDataJson = System.Text.Json.JsonSerializer.Serialize(new { 
                        HRegisters = hRegisters, 
                        DMRegisters = dmRegisters,
                        Timestamp = DateTime.UtcNow
                    })
                };
                
                return snapshot;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlcDataSnapshot oluşturma hatası: {ex.Message}");
                return new PlcDataSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Operator = operatorName,
                    RecipeCode = recipeCode,
                    RawDataJson = $"{{\"error\": \"{ex.Message}\", \"timestamp\": \"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\"}}"
                };
            }
        }

        /// <summary>
        /// H register'larından bool değer al
        /// </summary>
        private bool GetBoolValue(Dictionary<string, PlcRegisterData> registers, string address)
        {
            return registers.TryGetValue(address, out var data) && data.IsSuccess && data.Value;
        }

        /// <summary>
        /// DM register'larından double değer al
        /// </summary>
        private double GetDoubleValue(Dictionary<string, PlcRegisterData> registers, string address)
        {
            if (registers.TryGetValue(address, out var data) && data.IsSuccess)
            {
                return data.NumericValue; // DM register'ları için NumericValue kullan
            }
            return 0.0;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// PLC Register veri modeli
    /// </summary>
    public class PlcRegisterData
    {
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Value { get; set; }
        public ushort NumericValue { get; set; } = 0; // D register'lar için sayısal değer
        public bool IsSuccess { get; set; }
        public DateTime ReadTime { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Önceki değer takibi için
        public bool? PreviousValue { get; set; }
        public ushort? PreviousNumericValue { get; set; }
        public DateTime? PreviousReadTime { get; set; }
        
        // Hesaplanan özellikler
        public bool HasChanged => PreviousValue.HasValue && PreviousValue.Value != Value;
        public bool HasNumericChanged => PreviousNumericValue.HasValue && PreviousNumericValue.Value != NumericValue;
        public string ChangeIndicator => HasChanged ? "✓" : "✗";
        public string StatusText => Value ? "🟢 AKTİF" : "🔴 PASİF";
        public string PreviousStatusText => PreviousValue == true ? "🟢 AKTİF" : "🔴 PASİF";
        public string NumericText => $"{NumericValue}";
        public string PreviousNumericText => PreviousNumericValue?.ToString() ?? "-";
    }

    /// <summary>
    /// PLC veri değişiklik event argümanları
    /// </summary>
    public class PlcDataChangedEventArgs : EventArgs
    {
        public Dictionary<string, PlcRegisterData> CurrentData { get; set; } = new();
        public Dictionary<string, PlcRegisterData> PreviousData { get; set; } = new();
        public List<string> ChangedRegisters { get; set; } = new();
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    #endregion
}
