using System;
using System.IO;

namespace PenguLoader.Main
{
    static class Module
    {
        private static string ModuleName => "core.dll";
        private static string TargetName => LCU.ClientUxProcessName;
        private static string ModulePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModuleName);
        private static string DebuggerValue => $"rundll32 \"{ModulePath}\", #6000 ";

        private static string SymlinkName => "version.dll";
        private static string SymlinkPath => Path.Combine(Config.LeaguePath, SymlinkName);

        public static bool IsFound => File.Exists(ModulePath);

        public static bool IsLoaded => Utils.IsFileInUse(ModulePath);

        public static bool IsActivated
        {
            get
            {
                if (Config.UseSymlink)
                {
                    string leaguePath = Config.LeaguePath;
                    Console.WriteLine($"[Pengu Loader] Module.IsActivated: Checking symlink status using League path: {leaguePath}");
                    
                    if (!LCU.IsValidDir(leaguePath))
                    {
                        Console.WriteLine($"[Pengu Loader] Module.IsActivated: League path is invalid: {leaguePath}");
                        return false;
                    }

                    var resolved = Utils.NormalizePath(Symlink.Resolve(SymlinkPath));
                    var modulePath = Utils.NormalizePath(ModulePath);
                    
                    bool isActive = string.Compare(resolved, modulePath, false) == 0;
                    Console.WriteLine($"[Pengu Loader] Module.IsActivated: Symlink resolved to '{resolved}', module at '{modulePath}', active={isActive}");
                    return isActive;
                }
                else
                {
                    var param = IFEO.GetDebugger(TargetName);
                    bool isActive = DebuggerValue.Equals(param, StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine($"[Pengu Loader] Module.IsActivated: Using IFEO debugger method, active={isActive}");
                    return isActive;
                }
            }
        }

        public static bool SetActive(bool active)
        {
            if (IsActivated == active)
                return true;

            if (Config.UseSymlink)
            {
                string leaguePath = Config.LeaguePath;
                var path = SymlinkPath;
                Console.WriteLine($"[Pengu Loader] Module.SetActive: Using symlink method. League path: {leaguePath}, Symlink target: {path}");
                
                Utils.DeletePath(path);

                if (active)
                {
                    Console.WriteLine($"[Pengu Loader] Module.SetActive: Creating symlink '{path}' -> '{ModulePath}'");
                    Symlink.Create(path, ModulePath);
                    Console.WriteLine($"[Pengu Loader] Module.SetActive: Symlink created successfully");
                }
                else
                {
                    Console.WriteLine($"[Pengu Loader] Module.SetActive: Symlink removed");
                }
            }
            else
            {
                Console.WriteLine($"[Pengu Loader] Module.SetActive: Using IFEO debugger method, active={active}");
                if (active)
                {
                    IFEO.SetDebugger(TargetName, DebuggerValue);
                    Console.WriteLine($"[Pengu Loader] Module.SetActive: IFEO debugger set");
                }
                else
                {
                    IFEO.RemoveDebugger(TargetName);
                    Console.WriteLine($"[Pengu Loader] Module.SetActive: IFEO debugger removed");
                }
            }

            return IsActivated == active;
        }
    }
}