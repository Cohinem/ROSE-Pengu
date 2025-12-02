using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PenguLoader.Main
{
    static class Config
    {
        public static string ConfigPath => GetPath("config");
        public static string DataStorePath => GetPath("datastore");
        public static string PluginsDir => GetPath("plugins");

        static Dictionary<string, string> _data;

        static Config()
        {
            Utils.EnsureDirectoryExists(PluginsDir);
            Utils.EnsureFileExists(ConfigPath);
            Utils.EnsureFileExists(DataStorePath);

            _data = new Dictionary<string, string>();

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllLines(ConfigPath);

                foreach (string line in lines)
                {
                    var parts = line.Split(new[] { '=' }, 2);

                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        _data[key] = value;
                    }
                }
            }
        }

        static void Save()
        {
            var sb = new StringBuilder();

            foreach (var kv in _data)
            {
                var key = kv.Key;
                var value = kv.Value.Trim();

                var line = $"{key}={value}";
                sb.AppendLine(line);
            }

            File.WriteAllText(ConfigPath, sb.ToString());
        }

        public static string LeaguePath
        {
            get
            {
                // First, try to get from Rose config.ini
                var rosePath = GetRoseConfigPath();
                if (!string.IsNullOrWhiteSpace(rosePath))
                    return rosePath;

                // Fallback to local config
                return Get("LeaguePath");
            }
            set => Set("LeaguePath", value);
        }

        public static bool UseSymlink
        {
            get => GetBool("UseSymlink", false);
            set => SetBool("UseSymlink", value);
        }

        public static string Language
        {
            get => Get("Language", "English");
            set => Set("Language", value);
        }

        public static bool OptimizeClient
        {
            get => GetBool("OptimizeClient", true);
            set => SetBool("OptimizeClient", value);
        }

        public static bool SuperLowSpecMode
        {
            get => GetBool("SuperLowSpecMode", false);
            set => SetBool("SuperLowSpecMode", value);
        }

        static string GetPath(string subpath)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subpath);
        }

        static string Get(string key, string @default = "")
        {
            if (_data.ContainsKey(key))
                return _data[key];

            return @default;
        }

        static void Set(string key, string value)
        {
            _data[key] = value;
            Save();
        }

        static bool GetBool(string key, bool @default)
        {
            var value = Get(key).ToLower();

            if (value == "true" || value == "1")
                return true;
            else if (value == "false" || value == "0")
                return false;

            return @default;
        }

        static void SetBool(string key, bool value)
        {
            Set(key, value ? "true" : "false");
        }

        static string GetRoseConfigPath()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppData, "Rose", "config.ini");
                
                if (!File.Exists(configPath))
                    return string.Empty;

                var lines = File.ReadAllLines(configPath);
                bool inGeneralSection = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Check for section headers
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        inGeneralSection = trimmed.Equals("[General]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    // Only process lines in [General] section
                    if (inGeneralSection)
                    {
                        var parts = trimmed.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            if (key.Equals("clientpath", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.IsNullOrWhiteSpace(value))
                                    return string.Empty;

                                // Remove trailing slash
                                value = value.TrimEnd('\\', '/');

                                // Append \LeagueClient if not already present
                                var leagueClient = "\\LeagueClient";
                                if (value.Length < leagueClient.Length || 
                                    !value.EndsWith(leagueClient, StringComparison.OrdinalIgnoreCase))
                                {
                                    value += leagueClient;
                                }

                                return value;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors reading config file
            }

            return string.Empty;
        }
    }
}