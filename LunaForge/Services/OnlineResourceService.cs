using LunaForge.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace LunaForge.Services;

public class OnlineResourceService
{
    private static readonly ILogger Logger = CoreLogger.Create("Online Resources");
    private static readonly HttpClient httpClient = new();

    private string serverUrl;
    private static ResourceManifest? cachedManifest;
    private static DateTime? lastFetchTime;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromMinutes(30);

    public OnlineResourceService()
    {
        serverUrl = EditorConfig.Default.Get<string>("TemplateServerUrl")?.Value
            ?? "https://lunaforge.rulholos.fr"; // biased lmao
    }

    private async Task<ResourceManifest?> GetManifest()
    {
        if (cachedManifest != null && lastFetchTime.HasValue && DateTime.Now - lastFetchTime.Value < cacheExpiration)
        {
            return cachedManifest;
        }

        try
        {
            string manifestUrl = $"{serverUrl.TrimEnd('/')}/manifest.json";
            Logger.Information($"Fetching resource manifest from: {manifestUrl}");

            var response = await httpClient.GetAsync(manifestUrl);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();
            cachedManifest = JsonConvert.DeserializeObject<ResourceManifest>(jsonContent);
            lastFetchTime = DateTime.Now;

            Logger.Information($"Successfully fetched manifest version: {cachedManifest?.Version}");
            return cachedManifest;
        }
        catch (HttpRequestException ex)
        {
            Logger.Warning($"Could not reach resource server: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching or parsing manifest: {ex}");
            return null;
        }
    }

    public static void ClearCache()
    {
        cachedManifest = null;
        lastFetchTime = null;
        Logger.Information("Resource manifest cache cleared");
    }

    public void UpdateServerUrl(string newUrl)
    {
        serverUrl = newUrl;
        ClearCache();
        Logger.Information($"Server URL updated to: {newUrl}");
    }

    public async Task<List<OnlineProjectTemplate>> GetAvailableProjectTemplatesAsync()
    {
        try
        {
            var manifest = await GetManifest();
            return manifest?.Templates?.Projects ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch online project templates. Reason:\n{ex}");
            return [];
        }
    }

    public async Task<List<OnlineFileTemplate>> GetAvailableFileTemplatesAsync()
    {
        try
        {
            var manifest = await GetManifest();
            return manifest?.Templates?.Files ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch online file templates. Reason:\n{ex}");
            return [];
        }
    }

    public async Task<List<OnlinePlugin>> GetAvailablePluginsAsync()
    {
        try
        {
            var manifest = await GetManifest();
            return manifest?.Plugins ?? [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch online plugins. Reason:\n{ex}");
            return [];
        }
    }

    public async Task<string?> DownloadAsync(string downloadUrl, string destinationPath, IProgress<double>? progress = null)
    {
        try
        {
            string fullUrl = downloadUrl;
            if (!Uri.IsWellFormedUriString(downloadUrl, UriKind.Absolute))
            {
                fullUrl = $"{serverUrl.TrimEnd('/')}/{downloadUrl.TrimStart('/')}";
            }

            Logger.Information($"Downloading resource from: {fullUrl}");

            using var response = await httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && progress != null;

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                if (canReportProgress)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes * 100;
                    progress!.Report(progressPercentage);
                }
            }

            Logger.Information($"Resource downloaded successfully to: {destinationPath}");
            return destinationPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download resource: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadTemplateAsync(string downloadUrl, string destinationPath)
    {
        return await DownloadAsync(downloadUrl, destinationPath);
    }

    public async Task<string?> DownloadPluginAsync(string downloadUrl, string destinationPath, IProgress<double>? progress = null)
    {
        return await DownloadAsync(downloadUrl, destinationPath, progress);
    }
}

#region Manifest Models

public class ResourceManifest
{
    public string Version { get; set; } = "1.0";
    public TemplateCollection? Templates { get; set; }
    public List<OnlinePlugin> Plugins { get; set; } = [];
}

public class TemplateCollection
{
    public List<OnlineProjectTemplate> Projects { get; set; } = [];
    public List<OnlineFileTemplate> Files { get; set; } = [];
}

#endregion

#region Template Models

public class OnlineProjectTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public long Size { get; set; }
    public List<string> Tags { get; set; } = [];

    [JsonIgnore]
    public bool IsOnline => true;
    [JsonIgnore]
    public string? LocalZipPath { get; set; }
}

public class OnlineFileTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string FileType { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
}

#endregion

#region Plugin Models

public class OnlinePlugin
{
    public string Name { get; set; } = "";
    public string LibraryName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public long Size { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? MinimumLunaForgeVersion { get; set; }

    [JsonIgnore]
    public bool IsOnline => true;
}

#endregion
