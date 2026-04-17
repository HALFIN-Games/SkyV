using System;
using System.IO;
using Microsoft.Win32;

namespace SkyV.Launcher;

public static class SkyrimInstallLocator
{
    public static string? TryFindSkyrimInstallPath()
    {
        var fromRegistry =
            TryReadReg(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Bethesda Softworks\Skyrim Special Edition", "Installed Path") ??
            TryReadReg(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Bethesda Softworks\Skyrim Special Edition", "InstalledPath") ??
            TryReadReg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Bethesda Softworks\Skyrim Special Edition", "Installed Path") ??
            TryReadReg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Bethesda Softworks\Skyrim Special Edition", "InstalledPath");

        if (IsValidSkyrimDir(fromRegistry)) return Normalize(fromRegistry!);

        var defaultSteam = @"C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition";
        if (IsValidSkyrimDir(defaultSteam)) return defaultSteam;

        return null;
    }

    private static string? TryReadReg(string key, string valueName)
    {
        try
        {
            return Registry.GetValue(key, valueName, null) as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidSkyrimDir(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return false;
        if (!Directory.Exists(dir)) return false;
        return File.Exists(Path.Combine(dir, "SkyrimSE.exe"));
    }

    private static string Normalize(string path)
    {
        return path.Trim().TrimEnd('\\');
    }
}

