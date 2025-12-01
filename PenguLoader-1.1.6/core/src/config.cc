#include "pengu.h"
#include <fstream>
#include <unordered_map>
#include "include/cef_version.h"
#include <Windows.h>

#if OS_WIN
EXTERN_C IMAGE_DOS_HEADER __ImageBase;
#elif OS_MAC
#include <dlfcn.h>
#include <libgen.h>
#endif

path config::loader_dir()
{
#if OS_WIN
    static std::wstring path;
    if (path.empty())
    {
        // Get this dll path.
        WCHAR thisPath[2048];
        GetModuleFileNameW((HINSTANCE)&__ImageBase, thisPath, ARRAYSIZE(thisPath) - 1);

        DWORD attr = GetFileAttributesW(thisPath);
        if ((attr & FILE_ATTRIBUTE_REPARSE_POINT) != FILE_ATTRIBUTE_REPARSE_POINT)
        {
            path = thisPath;
            return path = path.substr(0, path.find_last_of(L"/\\"));
        }

        OFSTRUCT of{};
        WCHAR finalPath[2048];
        // Get final path.
        HANDLE file = CreateFileW(thisPath, GENERIC_READ, 0x1, NULL, OPEN_EXISTING, 0, NULL);
        DWORD pathLength = GetFinalPathNameByHandleW(file, finalPath, 2048, FILE_NAME_OPENED);
        CloseHandle(file);

        std::wstring dir{ finalPath, pathLength };
        // Remove prepended '\\?\' by GetFinalPathNameByHandle()
        if (dir.rfind(L"\\\\?\\", 0) == 0)
            dir.erase(0, 4);

        // Get parent folder.
        return path = dir.substr(0, dir.find_last_of(L"/\\"));
    }
#elif OS_MAC
    static std::string path;
    if (path.empty())
    {
        Dl_info info;
        if (dladdr((const void *)&loader_dir, &info))
        {
            path = info.dli_fname;
            path = path.substr(0, path.rfind('/'));
        }
    }
#endif
    return path;
}

path config::datastore_path()
{
    return loader_dir() / "datastore";
}

path config::cache_dir()
{
#if OS_WIN
    wchar_t appdata_path[2048];
    size_t length = GetEnvironmentVariableW(L"LOCALAPPDATA", appdata_path, _countof(appdata_path));

    if (length == 0)
    {
        path leaguePath = league_dir();
        if (!leaguePath.empty())
        {
            OutputDebugStringA("[Pengu Loader] config::cache_dir() using league_dir() fallback (LOCALAPPDATA not available)\n");
            return leaguePath / "Cache";
        }
        OutputDebugStringA("[Pengu Loader] ERROR: config::cache_dir() - Cannot determine cache path (no LOCALAPPDATA and no league_dir)\n");
    }

    lstrcatW(appdata_path, L"\\Riot Games\\League of Legends\\Cache");
    return appdata_path;
#else
    // inside the RiotClient folder 
    return "/Users/Shared/Riot Games/League Client/Cache";
#endif
}

static std::wstring get_rose_league_path()
{
    wchar_t appdata[2048];
    DWORD len = GetEnvironmentVariableW(L"LOCALAPPDATA", appdata, 2048);
    if (len == 0)
        return L"";

    std::wstring cfg = std::wstring(appdata) + L"\\Rose\\config.ini";

    wchar_t value[2048];
    DWORD out = GetPrivateProfileStringW(
        L"General",         // INI section
        L"leaguepath",      // key
        L"",                // default
        value,
        2048,
        cfg.c_str()
    );

    if (out == 0)
        return L""; // not found

    return std::wstring(value);
}

path config::league_dir()
{
    // Get league path from Rose config.ini
    std::wstring rosePath = get_rose_league_path();
    if (!rosePath.empty())
    {
        // Remove trailing slash if present
        if (rosePath.back() == L'\\' || rosePath.back() == L'/')
            rosePath.pop_back();

        // Log when league_dir() is called and returns the Rose path
        // Convert to UTF-8 for console output
        int size_needed = WideCharToMultiByte(CP_UTF8, 0, rosePath.c_str(), -1, nullptr, 0, nullptr, nullptr);
        if (size_needed > 0)
        {
            std::string pathStr(size_needed, 0);
            WideCharToMultiByte(CP_UTF8, 0, rosePath.c_str(), -1, &pathStr[0], size_needed, nullptr, nullptr);
            pathStr.pop_back(); // Remove null terminator
            OutputDebugStringA(("[Pengu Loader] config::league_dir() returning Rose config path: " + pathStr + "\n").c_str());
        }

        return rosePath;
    }

    // No fallback - return empty path if not found
    OutputDebugStringA("[Pengu Loader] ERROR: config::league_dir() - League path not found in Rose config.ini!\n");
    return path();
}

static void trim_tring(std::string &str)
{
    str.erase(str.find_last_not_of(' ') + 1);
    str.erase(0, str.find_first_not_of(' '));
}

static auto get_config_map()
{
    static bool cached = false;
    static std::unordered_map<std::string, std::string> map;

    if (!cached)
    {
        auto path = config::loader_dir() / "config";
        std::ifstream file(path);

        if (file.is_open())
        {
            std::string line;
            while (std::getline(file, line))
            {
                // ignore empty line or comment
                if (line.empty() || line[0] == ';' || line[0] == '#')
                    continue;

                size_t pos = line.find('=');
                if (pos != std::string::npos)
                {
                    std::string key = line.substr(0, pos);
                    std::string value = line.substr(pos + 1);

                    trim_tring(key);
                    trim_tring(value);

                    map[key] = value;
                }
            }
            file.close();
        }

        cached = true;
    }

    return map;
}

static std::string get_config_value(const char *key, const char *fallback)
{
    auto map = get_config_map();
    auto it = map.find(key);
    std::string value = fallback;

    if (it != map.end())
        value = it->second;

    return value;
}

static bool get_config_value_bool(const char *key, bool fallback)
{
    auto map = get_config_map();
    auto it = map.find(key);
    bool value = fallback;

    if (it != map.end())
    {
        if (it->second == "0" || it->second == "false")
            value = false;
        else if (it->second == "1" || it->second == "true")
            value = true;
    }

    return value;
}

static int get_config_value_int(const char *key, int fallback)
{
    auto map = get_config_map();
    auto it = map.find(key);
    int value = fallback;

    if (it != map.end())
        value = std::stoi(it->second);

    return value;
}

path config::plugins_dir()
{
    std::string cpath = get_config_value(__func__, "");
    if (!cpath.empty())
        return (const char8_t *)cpath.c_str();

    return loader_dir() / "plugins";
}

std::string config::disabled_plugins()
{
    return get_config_value(__func__, "");
}

namespace config::options
{
    bool use_hotkeys()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return true;
#endif
        return get_config_value_bool(__func__, true);
    }

    bool optimized_client()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return get_config_value_bool("OptimizeClient", true);
#endif
        return get_config_value_bool(__func__, true);
    }

    bool super_potato()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return get_config_value_bool("SuperLowSpecMode", false);
#endif
        return get_config_value_bool(__func__, false);
    }

    bool silent_mode()
    {
        return get_config_value_bool(__func__, false);
    }

    bool isecure_mode()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return get_config_value_bool("DisableWebSecurity", false);
#endif
        return get_config_value_bool(__func__, false);
    }

    bool use_devtools()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return true;
#endif
        return get_config_value_bool(__func__, false);
    }

    bool use_riotclient()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return true;
#endif
        return get_config_value_bool(__func__, false);
    }

    bool use_proxy()
    {
        return get_config_value_bool(__func__, false);
    }

    int debug_port()
    {
#if 1 // CEF_VERSION_MAJOR == 91
        return get_config_value_int("RemoteDebuggingPort", 0);
#endif
        return get_config_value_int(__func__, 0);
    }
}