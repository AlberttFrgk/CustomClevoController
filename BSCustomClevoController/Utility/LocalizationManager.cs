using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace BSCustomClevoController.Utility
{
    public sealed class LanguageOption
    {
        public string Code { get; }
        public string DisplayName { get; }

        public LanguageOption(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }

    public static class LocalizationManager
    {
        public static event Action LanguageChanged;

        private static readonly (string Code, string DefaultName)[] BuiltInOrder =
        {
            ("en-us", "English"),
            ("id-id", "Bahasa Indonesia"),
            ("es", "Español"),
            ("de", "Deutsch"),
            ("it", "Italiano"),
            ("fr-fr", "Français (France)"),
            ("fr-ca", "Français (Canada)"),
            ("pt-br", "Português (Brasil)"),
            ("pt-pt", "Português (Portugal)"),
            ("ru", "Русский"),
            ("tr-tr", "Türkçe"),
            ("ja", "日本語"),
            ("ko", "한국어"),
            ("zh-cn", "简体中文"),
            ("zh-tw", "繁體中文"),
            ("th-th", "ไทย"),
            ("vi-vn", "Tiếng Việt")
        };

        // Emergency fallback if Localization\*.ini files are missing or unreadable.
        private static readonly Dictionary<string, string> EmergencyFallback = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AppTitle"] = "Control Center",
            ["NavBrand"] = "Clevo Control",
            ["NavPerformance"] = "Performance",
            ["NavFan"] = "Fan Control",
            ["NavKeyboard"] = "Keyboard Lighting",
            ["NavBattery"] = "Battery",
            ["NavTools"] = "System Tools",
            ["NavSettings"] = "Settings",
            ["NavAbout"] = "About",
            ["SettingsSupported"] = "Supported",
            ["SettingsNotSupported"] = "Not Supported",
            ["CommonNotSupported"] = "Not supported on this model",
            ["CommonResetDefaults"] = "Reset to Defaults",
            ["CommonNotice"] = "Notice"
        };

        private const long MaxIniFileBytes = 1024 * 1024;
        private const int MaxIniLineLength = 4096;

        private static readonly object SyncLock = new object();
        private static List<LanguageOption> _supportedLanguages = BuildDefaultLanguageList();
        private static Dictionary<string, string> _englishBase = new Dictionary<string, string>(EmergencyFallback, StringComparer.Ordinal);
        private static Dictionary<string, string> _activeStrings = new Dictionary<string, string>(EmergencyFallback, StringComparer.Ordinal);
        private static ResourceDictionary _activeDictionary;

        public static IReadOnlyList<LanguageOption> SupportedLanguages
        {
            get
            {
                lock (SyncLock)
                {
                    return _supportedLanguages;
                }
            }
        }

        public static string CurrentLanguage { get; private set; } = "en-us";

        public static string LocalizationFolder
        {
            get
            {
                string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                string assemblyLocation = typeof(LocalizationManager).Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    string assemblyDir = Path.GetDirectoryName(assemblyLocation);
                    if (!string.IsNullOrEmpty(assemblyDir))
                    {
                        string assemblyCandidate = Path.Combine(assemblyDir, "Localization");
                        if (Directory.Exists(assemblyCandidate))
                        {
                            return assemblyCandidate;
                        }
                    }
                }

                return candidate;
            }
        }

        public static bool IsValidLanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string trimmed = code.Trim();
            if (trimmed.Length == 0 || trimmed.Length > 32) return false;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool isAlphaNum = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                bool isSeparator = (c == '-' || c == '_') && i > 0 && i < trimmed.Length - 1;
                if (!isAlphaNum && !isSeparator)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool TryResolveLanguagePackPath(string code, out string fullPath)
        {
            fullPath = null;
            if (!IsValidLanguageCode(code)) return false;

            try
            {
                string baseDir = Path.GetFullPath(LocalizationFolder)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string candidate = Path.GetFullPath(Path.Combine(baseDir, code.Trim().ToLowerInvariant() + ".ini"));
                string prefix = baseDir + Path.DirectorySeparatorChar;
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                fullPath = candidate;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Initialize()
        {
            ReloadAvailableLanguages();
            var reg = new RWReg();
            string code = reg.Check_language();
            ApplyLanguage(code, saveToRegistry: true);
        }

        public static void SetLanguage(string code)
        {
            ReloadAvailableLanguages();
            ApplyLanguage(code, saveToRegistry: true);
        }

        public static string T(string key)
        {
            return T(key, key);
        }

        public static string T(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            lock (SyncLock)
            {
                if (_activeStrings.TryGetValue(key, out string localized))
                {
                    return localized;
                }

                if (_englishBase.TryGetValue(key, out string baseValue))
                {
                    return baseValue;
                }
            }

            return EmergencyFallback.TryGetValue(key, out string emergency) ? emergency : fallback;
        }

        private static string _cachedCultureCode;
        private static CultureInfo _cachedCulture;

        public static CultureInfo CurrentCulture
        {
            get
            {
                string code = CurrentLanguage;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return CultureInfo.CurrentCulture;
                }

                if (_cachedCulture != null && string.Equals(_cachedCultureCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    return _cachedCulture;
                }

                CultureInfo resolved = CultureInfo.CurrentCulture;
                try
                {
                    resolved = CultureInfo.ReadOnly(CultureInfo.CreateSpecificCulture(code));
                }
                catch (CultureNotFoundException)
                {
                }
                catch (ArgumentException)
                {
                }

                _cachedCultureCode = code;
                _cachedCulture = resolved;
                return resolved;
            }
        }

        public static string ToUpper(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return CurrentCulture.TextInfo.ToUpper(text);
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = T(key);
            if (args == null || args.Length == 0) return pattern;

            try
            {
                return string.Format(CurrentCulture, pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        private static List<LanguageOption> BuildDefaultLanguageList()
        {
            var list = new List<LanguageOption>(BuiltInOrder.Length);
            foreach (var item in BuiltInOrder)
            {
                list.Add(new LanguageOption(item.Code, item.DefaultName));
            }
            return list;
        }

        private static void ReloadAvailableLanguages()
        {
            string folder = LocalizationFolder;
            var discovered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (string filePath in Directory.GetFiles(folder, "*.ini"))
                    {
                        string code = Path.GetFileNameWithoutExtension(filePath)?.Trim().ToLowerInvariant();
                        if (!IsValidLanguageCode(code)) continue;

                        string displayName = ReadDisplayName(filePath) ?? code;
                        discovered[code] = displayName;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("LocalizationManager.ReloadAvailableLanguages", ex);
            }

            var ordered = new List<LanguageOption>();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var builtIn in BuiltInOrder)
            {
                string displayName = discovered.TryGetValue(builtIn.Code, out string fileDisplayName)
                    ? fileDisplayName
                    : builtIn.DefaultName;
                ordered.Add(new LanguageOption(builtIn.Code, displayName));
                added.Add(builtIn.Code);
            }

            var extras = new List<LanguageOption>();
            foreach (var kvp in discovered)
            {
                if (!added.Contains(kvp.Key))
                {
                    extras.Add(new LanguageOption(kvp.Key, kvp.Value));
                }
            }
            extras.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            ordered.AddRange(extras);

            Dictionary<string, string> enBase = TryResolveLanguagePackPath("en-us", out string enPath)
                ? LoadIniDictionary(enPath)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            var mergedBase = new Dictionary<string, string>(EmergencyFallback, StringComparer.Ordinal);
            foreach (var kvp in enBase)
            {
                if (!string.Equals(kvp.Key, "_Name", StringComparison.Ordinal))
                {
                    mergedBase[kvp.Key] = kvp.Value;
                }
            }

            lock (SyncLock)
            {
                _supportedLanguages = ordered;
                _englishBase = mergedBase;
            }
        }

        private static void ApplyLanguage(string code, bool saveToRegistry)
        {
            string normalized = NormalizeCode(code);
            CurrentLanguage = normalized;

            if (saveToRegistry)
            {
                try
                {
                    var reg = new RWReg();
                    reg.WriteLanguage(normalized);
                    reg.Check_language();
                }
                catch (Exception ex)
                {
                    AppLogger.Log("LocalizationManager.ApplyLanguage", ex);
                }
            }

            Dictionary<string, string> mergedStrings;
            lock (SyncLock)
            {
                mergedStrings = new Dictionary<string, string>(_englishBase, StringComparer.Ordinal);
            }

            if (!string.Equals(normalized, "en-us", StringComparison.OrdinalIgnoreCase) &&
                TryResolveLanguagePackPath(normalized, out string filePath))
            {
                Dictionary<string, string> overlay = LoadIniDictionary(filePath);
                foreach (var kvp in overlay)
                {
                    if (!string.Equals(kvp.Key, "_Name", StringComparison.Ordinal))
                    {
                        mergedStrings[kvp.Key] = kvp.Value;
                    }
                }
            }

            lock (SyncLock)
            {
                _activeStrings = mergedStrings;
            }

            if (Application.Current != null)
            {
                var dict = new ResourceDictionary();
                foreach (var kvp in mergedStrings)
                {
                    dict["Loc." + kvp.Key] = kvp.Value;
                }

                var merged = Application.Current.Resources.MergedDictionaries;
                if (_activeDictionary != null)
                {
                    merged.Remove(_activeDictionary);
                }
                merged.Add(dict);
                _activeDictionary = dict;
            }

            LanguageChanged?.Invoke();
        }

        private static string NormalizeCode(string code)
        {
            if (!IsValidLanguageCode(code)) return "en-us";
            string lower = code.Trim().ToLowerInvariant();

            lock (SyncLock)
            {
                foreach (var opt in _supportedLanguages)
                {
                    if (string.Equals(opt.Code, lower, StringComparison.OrdinalIgnoreCase))
                    {
                        return opt.Code;
                    }
                }
            }

            return "en-us";
        }

        private static string ReadDisplayName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length > MaxIniFileBytes) return null;

                using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length > MaxIniLineLength) continue;
                        if (!TryParseLine(line, out string key, out string val)) continue;
                        if (string.Equals(key, "_Name", StringComparison.Ordinal))
                        {
                            string trimmed = val.Trim();
                            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("LocalizationManager.ReadDisplayName", ex);
            }

            return null;
        }

        private static Dictionary<string, string> LoadIniDictionary(string filePath)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(filePath))
            {
                return result;
            }

            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length > MaxIniFileBytes)
                {
                    return result;
                }

                using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length > MaxIniLineLength) continue;
                        if (TryParseLine(line, out string key, out string val))
                        {
                            result[key] = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("LocalizationManager.LoadIniDictionary", ex);
            }

            return result;
        }

        private static bool TryParseLine(string rawLine, out string key, out string value)
        {
            key = null;
            value = null;
            if (string.IsNullOrWhiteSpace(rawLine)) return false;

            string trimmedStart = rawLine.TrimStart();
            if (trimmedStart.StartsWith("#", StringComparison.Ordinal) ||
                trimmedStart.StartsWith(";", StringComparison.Ordinal) ||
                trimmedStart.StartsWith("[", StringComparison.Ordinal))
            {
                return false;
            }

            int eq = rawLine.IndexOf('=');
            if (eq <= 0) return false;

            key = rawLine.Substring(0, eq).Trim();
            if (string.IsNullOrEmpty(key)) return false;

            string rawVal = rawLine.Substring(eq + 1);
            value = UnescapeValue(rawVal);
            return true;
        }

        private static string UnescapeValue(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.IndexOf('\\') < 0)
            {
                return raw ?? string.Empty;
            }

            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                if (ch == '\\' && i + 1 < raw.Length)
                {
                    char next = raw[i + 1];
                    switch (next)
                    {
                        case 'n':
                            sb.Append('\n');
                            i++;
                            continue;
                        case 'r':
                            sb.Append('\r');
                            i++;
                            continue;
                        case 't':
                            sb.Append('\t');
                            i++;
                            continue;
                        case '\\':
                            sb.Append('\\');
                            i++;
                            continue;
                    }
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
