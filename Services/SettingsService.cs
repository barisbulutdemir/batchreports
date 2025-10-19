using System.Text.Json;
using takip.Models;
using System;
using System.Collections.Generic;

namespace takip.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            _settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }

        public PlcSettings LoadPlcSettings()
        {
            try
            {
                if (!System.IO.File.Exists(_settingsPath))
                {
                    return new PlcSettings();
                }
                var json = System.IO.File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("PlcSettings", out var plcNode))
                {
                    var settings = plcNode.Deserialize<PlcSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settings != null)
                    {
                        // JSON'daki property isimlerini model ile eşleştir
                        if (plcNode.TryGetProperty("IpAddress", out var ipProp))
                            settings.PlcIp = ipProp.GetString() ?? settings.PlcIp;
                        if (plcNode.TryGetProperty("Port", out var portProp))
                            settings.PlcPort = portProp.GetInt32();
                    }
                    return settings ?? new PlcSettings();
                }
            }
            catch { }
            return new PlcSettings();
        }

        public void SavePlcSettings(PlcSettings settings)
        {
            try
            {
                var json = System.IO.File.Exists(_settingsPath) ? System.IO.File.ReadAllText(_settingsPath) : "{}";
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
                dict["PlcSettings"] = settings;
                var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_settingsPath, output);
            }
            catch { }
        }

        // Back-compat: PlcService bu metodları kullanıyor
        public int GetLastPlcValue()
        {
            try
            {
                if (!System.IO.File.Exists(_settingsPath)) return 0;
                var json = System.IO.File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("LastPlcValue", out var v))
                {
                    return v.GetInt32();
                }
            }
            catch { }
            return 0;
        }

        public DateTime GetLastPlcValueUpdateTime()
        {
            try
            {
                if (!System.IO.File.Exists(_settingsPath)) return DateTime.UtcNow;
                var json = System.IO.File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("LastPlcValueUpdateTime", out var v))
                {
                    if (DateTime.TryParse(v.GetString(), out var dt)) return dt;
                }
            }
            catch { }
            return DateTime.UtcNow;
        }

        public void SaveLastPlcValue(int value)
        {
            try
            {
                var json = System.IO.File.Exists(_settingsPath) ? System.IO.File.ReadAllText(_settingsPath) : "{}";
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
                dict["LastPlcValue"] = value;
                dict["LastPlcValueUpdateTime"] = DateTime.UtcNow.ToString("o");
                var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_settingsPath, output);
            }
            catch { }
        }

        #region Saat Dilimi Ayarları

        public string GetSetting(string key)
        {
            try
            {
                if (!System.IO.File.Exists(_settingsPath)) return string.Empty;
                var json = System.IO.File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                
                // TimeZoneSettings içinde ara
                if (doc.RootElement.TryGetProperty("TimeZoneSettings", out var timeZoneNode))
                {
                    if (timeZoneNode.TryGetProperty(key, out var value))
                    {
                        return value.GetString() ?? string.Empty;
                    }
                }
                
                // Root seviyede ara
                if (doc.RootElement.TryGetProperty(key, out var rootValue))
                {
                    return rootValue.GetString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        public void SetSetting(string key, string value)
        {
            try
            {
                var json = System.IO.File.Exists(_settingsPath) ? System.IO.File.ReadAllText(_settingsPath) : "{}";
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
                
                // TimeZoneSettings bölümünü kontrol et
                if (!dict.ContainsKey("TimeZoneSettings"))
                {
                    dict["TimeZoneSettings"] = new Dictionary<string, object>();
                }
                
                if (dict["TimeZoneSettings"] is Dictionary<string, object> timeZoneSettings)
                {
                    timeZoneSettings[key] = value;
                }
                else
                {
                    // Eğer TimeZoneSettings bir Dictionary değilse, yeniden oluştur
                    var newTimeZoneSettings = new Dictionary<string, object>();
                    newTimeZoneSettings[key] = value;
                    dict["TimeZoneSettings"] = newTimeZoneSettings;
                }
                
                var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_settingsPath, output);
            }
            catch { }
        }

        public TimeZoneSettings LoadTimeZoneSettings()
        {
            try
            {
                if (!System.IO.File.Exists(_settingsPath))
                {
                    return new TimeZoneSettings
                    {
                        SelectedTimeZone = "Turkey Standard Time",
                        AutoTimeZoneConvert = true,
                        DefaultTimeZone = "Turkey Standard Time"
                    };
                }
                
                var json = System.IO.File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("TimeZoneSettings", out var timeZoneNode))
                {
                    var settings = timeZoneNode.Deserialize<TimeZoneSettings>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return settings ?? new TimeZoneSettings
                    {
                        SelectedTimeZone = "Turkey Standard Time",
                        AutoTimeZoneConvert = true,
                        DefaultTimeZone = "Turkey Standard Time"
                    };
                }
            }
            catch { }
            
            return new TimeZoneSettings
            {
                SelectedTimeZone = "Turkey Standard Time",
                AutoTimeZoneConvert = true,
                DefaultTimeZone = "Turkey Standard Time"
            };
        }

        public void SaveTimeZoneSettings(TimeZoneSettings settings)
        {
            try
            {
                var json = System.IO.File.Exists(_settingsPath) ? System.IO.File.ReadAllText(_settingsPath) : "{}";
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
                dict["TimeZoneSettings"] = settings;
                var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_settingsPath, output);
            }
            catch { }
        }

        #endregion
    }

    // Saat dilimi ayarları için model sınıfı
    public class TimeZoneSettings
    {
        public string SelectedTimeZone { get; set; } = "Turkey Standard Time";
        public bool AutoTimeZoneConvert { get; set; } = true;
        public string DefaultTimeZone { get; set; } = "Turkey Standard Time";
    }
}
