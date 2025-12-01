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

        static string GetRoseLeaguePath()
        {
            try
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string configPath = Path.Combine(appdata, "Rose", "config.ini");
                
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[Pengu Loader] ERROR: Rose config.ini not found at: {configPath}");
                    return null;
                }
                
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
                    
                    // Check for leaguepath key in [General] section
                    if (inGeneralSection && trimmed.StartsWith("leaguepath=", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = trimmed.Substring("leaguepath=".Length).Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            // Remove trailing slash if present
                            if (value.EndsWith("\\") || value.EndsWith("/"))
                                value = value.TrimEnd('\\', '/');
                            
                            Console.WriteLine($"[Pengu Loader] League path loaded from Rose config.ini: {value}");
                            return value;
                        }
                    }
                }
                
                Console.WriteLine($"[Pengu Loader] ERROR: leaguepath key not found in [General] section of Rose config.ini");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pengu Loader] ERROR: Failed to read Rose config.ini: {ex.Message}");
            }
            
            return null;
        }

        public static string LeaguePath
        {
            get
            {
                // Get from Rose config.ini only - no fallback
                string rosePath = GetRoseLeaguePath();
                if (string.IsNullOrEmpty(rosePath))
                {
                    Console.WriteLine("[Pengu Loader] ERROR: League path is required from Rose config.ini but was not found!");
                    throw new InvalidOperationException("League path not found in Rose config.ini. Please set leaguepath in [General] section of %LOCALAPPDATA%\\Rose\\config.ini");
                }
                
                // Log each time the path is accessed, showing it came from Rose config
                Console.WriteLine($"[Pengu Loader] Config.LeaguePath accessed - using League path from Rose config.ini: {rosePath}");
                return rosePath;
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
    }
}