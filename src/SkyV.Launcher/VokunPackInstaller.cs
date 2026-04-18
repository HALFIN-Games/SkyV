using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SkyV.Launcher;

public sealed class VokunPackInstaller
{
    private readonly HttpClient http;

    public VokunPackInstaller(HttpClient http)
    {
        this.http = http;
    }

    public async Task EnsureInstalledAsync(string skyrimRoot, string packUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(skyrimRoot)) throw new Exception("Skyrim path is missing.");
        if (!Directory.Exists(skyrimRoot)) throw new Exception("Skyrim path does not exist.");
        if (!File.Exists(Path.Combine(skyrimRoot, "SkyrimSE.exe"))) throw new Exception("SkyrimSE.exe not found in the selected folder.");

        if (string.IsNullOrWhiteSpace(packUrl)) throw new Exception("Pack URL is missing.");

        var cacheDir = GetCacheDir();
        Directory.CreateDirectory(cacheDir);

        var zipPath = Path.Combine(cacheDir, "VokunPack.zip");
        await DownloadValidatedZipAsync(packUrl, zipPath, ct);

        var extractDir = Path.Combine(cacheDir, "extract");
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
        Directory.CreateDirectory(extractDir);

        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var srcDataDir = Path.Combine(extractDir, "Data");
        if (!Directory.Exists(srcDataDir)) throw new Exception("Pack is missing Data folder.");

        TryDelete(Path.Combine(skyrimRoot, "Data", "Platform", "PluginsDev", "skymp5-client.js"));
        TryDelete(Path.Combine(skyrimRoot, "Data", "Platform", "PluginsDev", "skymp5-client-settings.txt"));

        var installed = new List<string>();
        CopyDirectory(srcDataDir, Path.Combine(skyrimRoot, "Data"), installed);

        var state = new PackState
        {
            InstalledAtUtc = DateTime.UtcNow,
            PackUrl = packUrl,
            Files = installed,
        };
        File.WriteAllText(GetStatePath(), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task DownloadAsync(string url, string outPath, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", "SkyV.Launcher");
        req.Headers.TryAddWithoutValidation("Accept", "application/octet-stream");

        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength;

        await using var input = await resp.Content.ReadAsStreamAsync(ct);
        await using var output = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, useAsync: true);

        var buffer = new byte[1024 * 128];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
        }

        if (total.HasValue && total.Value > 0 && readTotal != total.Value)
        {
            throw new Exception($"Pack download incomplete. Expected {total.Value} bytes, got {readTotal} bytes.");
        }
    }

    private async Task DownloadValidatedZipAsync(string url, string zipPath, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var tmp = zipPath + ".download";
            try
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                await DownloadAsync(url, tmp, ct);
                ValidateZipFile(tmp);

                if (File.Exists(zipPath)) File.Delete(zipPath);
                File.Move(tmp, zipPath);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                try
                {
                    if (File.Exists(tmp)) File.Delete(tmp);
                }
                catch
                {
                }
            }
        }

        throw new Exception($"Failed to download a valid pack zip after multiple attempts: {last?.Message}", last);
    }

    private static void ValidateZipFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Pack zip not found after download", path);

        using (var fs = File.OpenRead(path))
        {
            var sig = new byte[4];
            if (fs.Read(sig, 0, 4) < 4 || sig[0] != 0x50 || sig[1] != 0x4B)
            {
                throw new Exception("Downloaded file is not a ZIP archive.");
            }
        }

        using var archive = ZipFile.OpenRead(path);
        var hasData = archive.Entries.Any(e =>
            e.FullName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.StartsWith("Data\\", StringComparison.OrdinalIgnoreCase));
        if (!hasData) throw new Exception("Downloaded zip does not contain a Data folder.");
    }

    private static void CopyDirectory(string sourceDir, string destDir, List<string> installed)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            var dest = Path.Combine(destDir, name);
            File.Copy(file, dest, true);
            installed.Add(NormalizeRelative(dest));
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(destDir, name), installed);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private static string NormalizeRelative(string fullPath)
    {
        return fullPath.Replace('\\', '/');
    }

    private static string GetCacheDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "SkyV", "cache");
    }

    private static string GetStatePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "SkyV", "pack_state.json");
    }

    private sealed class PackState
    {
        public DateTime InstalledAtUtc { get; set; }
        public string PackUrl { get; set; } = "";
        public List<string> Files { get; set; } = new();
    }
}
