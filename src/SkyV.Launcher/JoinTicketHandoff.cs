using System;
using System.IO;
using System.Text.Json;

namespace SkyV.Launcher;

public static class JoinTicketHandoff
{
    public static void WriteTicket(string skyrimRoot, string ticket, string serverId)
    {
        if (string.IsNullOrWhiteSpace(skyrimRoot)) throw new Exception("Skyrim path is missing.");
        if (string.IsNullOrWhiteSpace(ticket)) throw new Exception("Join ticket is missing.");
        if (string.IsNullOrWhiteSpace(serverId)) throw new Exception("Server id is missing.");

        var dir = Path.Combine(skyrimRoot, "Data", "Platform", "PluginsNoLoad");
        Directory.CreateDirectory(dir);

        var payload = new
        {
            ticket,
            serverId,
            createdAtUtc = DateTime.UtcNow,
        };

        var content = "//" + JsonSerializer.Serialize(payload);
        var path = Path.Combine(dir, "skyv-join-ticket-no-load.js");
        File.WriteAllText(path, content);
    }
}
