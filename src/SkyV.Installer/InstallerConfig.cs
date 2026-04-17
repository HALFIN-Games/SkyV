using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyV.Installer;

public sealed record InstallerConfig(string MsixUrl, string CerUrl, bool AutoStart)
{
    public string ReleasePageUrl => "https://github.com/HALFIN-Games/SkyV/releases/latest";

    public static InstallerConfig FromArgs(string[] args)
    {
        var dict = ParseArgs(args ?? Array.Empty<string>());

        var msixUrl = dict.TryGetValue("msix-url", out var m)
            ? m
            : "https://github.com/HALFIN-Games/SkyV/releases/latest/download/VokunWL.msix";

        var cerUrl = dict.TryGetValue("cer-url", out var c)
            ? c
            : "https://github.com/HALFIN-Games/SkyV/releases/latest/download/VokunWL_TestCert.cer";

        var auto = dict.ContainsKey("auto");

        return new InstallerConfig(msixUrl, cerUrl, auto);
    }

    public string RebuildArgs(bool autoStart)
    {
        var parts = new List<string>
        {
            $"--msix-url={Quote(MsixUrl)}",
            $"--cer-url={Quote(CerUrl)}",
        };
        if (autoStart) parts.Add("--auto");
        return string.Join(" ", parts);
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in args.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (a.Equals("--auto", StringComparison.OrdinalIgnoreCase))
            {
                dict["auto"] = "1";
                continue;
            }

            if (!a.StartsWith("--", StringComparison.OrdinalIgnoreCase)) continue;
            var kv = a[2..].Split('=', 2);
            if (kv.Length == 2 && kv[0].Length > 0)
            {
                dict[kv[0]] = kv[1].Trim('"');
            }
        }
        return dict;
    }

    private static string Quote(string s)
    {
        if (s.Contains(' ') || s.Contains('"'))
        {
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }
        return s;
    }
}

