using System;
using System.Globalization;
using takip.Services;

namespace takip.Utils
{
    /// <summary>
    /// Saat dilimi dönüşümleri için yardımcı sınıf
    /// Veritabanındaki UTC tarihlerini seçilen saat dilimine çevirir
    /// </summary>
    public static class TimeZoneHelper
    {
        private static readonly SettingsService _settingsService = new SettingsService();
        private static TimeZoneInfo? _cachedTimeZone;
        private static string? _cachedTimeZoneId;

        /// <summary>
        /// UTC tarihini seçilen saat dilimine çevirir
        /// </summary>
        /// <param name="utcDateTime">UTC tarihi</param>
        /// <returns>Yerel saat dilimindeki tarih</returns>
        public static DateTime ConvertUtcToLocal(DateTime utcDateTime)
        {
            try
            {
                var timeZone = GetSelectedTimeZone();
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            }
            catch
            {
                // Hata durumunda UTC'yi döndür
                return utcDateTime;
            }
        }

        /// <summary>
        /// Yerel tarihi UTC'ye çevirir
        /// </summary>
        /// <param name="localDateTime">Yerel tarih</param>
        /// <returns>UTC tarihi</returns>
        public static DateTime ConvertLocalToUtc(DateTime localDateTime)
        {
            try
            {
                var timeZone = GetSelectedTimeZone();
                return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
            }
            catch
            {
                // Hata durumunda DateTime.Now'ı UTC'ye çevir
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Seçilen saat dilimini alır (cache'lenmiş)
        /// </summary>
        /// <returns>Seçilen saat dilimi</returns>
        public static TimeZoneInfo GetSelectedTimeZone()
        {
            try
            {
                var timeZoneId = _settingsService.GetSetting("SelectedTimeZone");
                
                // Cache kontrolü
                if (_cachedTimeZone != null && _cachedTimeZoneId == timeZoneId)
                {
                    return _cachedTimeZone;
                }

                if (string.IsNullOrEmpty(timeZoneId))
                {
                    timeZoneId = "Turkey Standard Time"; // Varsayılan saat dilimi
                }

                _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                _cachedTimeZoneId = timeZoneId;
                
                return _cachedTimeZone;
            }
            catch
            {
                // Hata durumunda Türkiye saatini döndür
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
        }

        /// <summary>
        /// Tarihi formatlanmış string olarak döndürür
        /// </summary>
        /// <param name="utcDateTime">UTC tarihi</param>
        /// <param name="format">Tarih formatı (varsayılan: "dd.MM.yyyy HH:mm:ss")</param>
        /// <returns>Formatlanmış tarih string'i</returns>
        public static string FormatDateTime(DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm:ss")
        {
            try
            {
                var localDateTime = ConvertUtcToLocal(utcDateTime);
                return localDateTime.ToString(format);
            }
            catch
            {
                return utcDateTime.ToString(format);
            }
        }

        /// <summary>
        /// Sadece tarihi formatlanmış string olarak döndürür
        /// </summary>
        /// <param name="utcDateTime">UTC tarihi</param>
        /// <param name="format">Tarih formatı (varsayılan: "dd.MM.yyyy")</param>
        /// <returns>Formatlanmış tarih string'i</returns>
        public static string FormatDate(DateTime utcDateTime, string format = "dd.MM.yyyy")
        {
            try
            {
                var localDateTime = ConvertUtcToLocal(utcDateTime);
                return localDateTime.ToString(format);
            }
            catch
            {
                return utcDateTime.ToString(format);
            }
        }

        /// <summary>
        /// Sadece saati formatlanmış string olarak döndürür
        /// </summary>
        /// <param name="utcDateTime">UTC tarihi</param>
        /// <param name="format">Saat formatı (varsayılan: "HH:mm:ss")</param>
        /// <returns>Formatlanmış saat string'i</returns>
        public static string FormatTime(DateTime utcDateTime, string format = "HH:mm:ss")
        {
            try
            {
                var localDateTime = ConvertUtcToLocal(utcDateTime);
                return localDateTime.ToString(format);
            }
            catch
            {
                return utcDateTime.ToString(format);
            }
        }

        /// <summary>
        /// Cache'i temizler (ayarlar değiştiğinde çağrılmalı)
        /// </summary>
        public static void ClearCache()
        {
            _cachedTimeZone = null;
            _cachedTimeZoneId = null;
        }

        /// <summary>
        /// Otomatik saat dilimi dönüşümünün aktif olup olmadığını kontrol eder
        /// </summary>
        /// <returns>Otomatik dönüşüm aktif mi?</returns>
        public static bool IsAutoConvertEnabled()
        {
            try
            {
                var setting = _settingsService.GetSetting("AutoTimeZoneConvert");
                return string.IsNullOrEmpty(setting) || setting.ToLower() == "true";
            }
            catch
            {
                return true; // Varsayılan olarak aktif
            }
        }

        /// <summary>
        /// Mevcut UTC zamanını yerel saat dilimine çevirir
        /// </summary>
        /// <returns>Yerel saat dilimindeki mevcut zaman</returns>
        public static DateTime Now()
        {
            return ConvertUtcToLocal(DateTime.UtcNow);
        }

        /// <summary>
        /// Saat dilimi bilgilerini string olarak döndürür
        /// </summary>
        /// <returns>Saat dilimi bilgisi</returns>
        public static string GetTimeZoneInfo()
        {
            try
            {
                var timeZone = GetSelectedTimeZone();
                var offset = timeZone.BaseUtcOffset;
                var offsetString = offset.ToString(@"hh\:mm");
                if (offset >= TimeSpan.Zero)
                    offsetString = "+" + offsetString;

                return $"UTC{offsetString} - {timeZone.DisplayName}";
            }
            catch
            {
                return "Saat dilimi bilgisi alınamadı";
            }
        }
    }
}
