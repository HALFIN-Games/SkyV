using System;
using System.Collections.Generic;

namespace SkyV.Launcher;

public sealed record JoinIntent(string ServerId, string Code, string? Nonce, string RawUri)
{
    public static JoinIntent? TryParse(string[] args)
    {
        if (args is null || args.Length == 0) return null;

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg)) continue;
            if (!arg.StartsWith("skyv://", StringComparison.OrdinalIgnoreCase)) continue;

            if (!Uri.TryCreate(arg, UriKind.Absolute, out var uri)) return null;

            var query = ParseQuery(uri.Query);
            if (!query.TryGetValue("server", out var serverId)) return null;
            if (!query.TryGetValue("code", out var code)) return null;

            query.TryGetValue("nonce", out var nonce);
            return new JoinIntent(serverId, code, nonce, arg);
        }

        return null;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query)) return dict;

        var q = query.StartsWith("?") ? query[1..] : query;
        var parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            var k = Uri.UnescapeDataString(kv[0]);
            var v = kv.Length == 2 ? Uri.UnescapeDataString(kv[1]) : "";
            if (k.Length == 0) continue;
            dict[k] = v;
        }

        return dict;
    }
}

