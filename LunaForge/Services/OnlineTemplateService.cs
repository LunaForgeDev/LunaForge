using LunaForge.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using ILogger = Serilog.ILogger;

namespace LunaForge.Services;

public class OnlineTemplateService
{
    private static readonly ILogger Logger = CoreLogger.Create("Online Templates");
    private static readonly HttpClient httpClient = new();

    private string serverUrl;
    public static TemplateManifest? cachedManifest;
    public static DateTime? lastFetchTime;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromMinutes(30);

    public OnlineTemplateService()
    {
        serverUrl = EditorConfig.Default.Get<string>("TemplateServerUrl")?.Value
            ?? "https://lunaforge.rulholos.fr";
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
            Logger.Error($"Failed to fetch online templates. Reason:\n{ex}");
            return [];
        }
    }

    private async Task<TemplateManifest?> GetManifest()
    {
        if (cachedManifest != null && lastFetchTime.HasValue && DateTime.Now - lastFetchTime.Value < cacheExpiration)
        {
            return cachedManifest;
        }

        try
        {
            string manifestUrl = $"{serverUrl.TrimEnd('/')}/manifest.json";
            Logger.Information($"Fetching manifest from: {manifestUrl}");

            var response = await httpClient.GetAsync(manifestUrl);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();
            cachedManifest = JsonConvert.DeserializeObject<TemplateManifest>(jsonContent);
            lastFetchTime = DateTime.Now;

            Logger.Information($"Successfully fetched manifest version: {cachedManifest?.Version}");
            return cachedManifest;
        }
        catch (HttpRequestException ex)
        {
            Logger.Warning($"Could not reach template server: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching or parsing manifest: {ex}");
            return null;
        }
    }

    public async Task<string?> DownloadTemplateAsync(string downloadUrl, string destinationPath)
    {
        try
        {
            Logger.Information($"Downloading template from: {downloadUrl}");

            var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            await using FileStream fs = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            Logger.Information($"Template downloaded successfully to: {destinationPath}");
            return destinationPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download template: {ex.Message}");
            return null;
        }
    }

    public void UpdateServerUrl(string newUrl)
    {
        serverUrl = newUrl;
        cachedManifest = null;
        lastFetchTime = null;
    }
}

public class TemplateManifest
{
    public string Version { get; set; } = "1.0";
    public TemplateCollection? Templates { get; set; }
}

public class TemplateCollection
{
    public List<OnlineProjectTemplate> Projects { get; set; } = [];
    public List<OnlineFileTemplate> Files { get; set; } = [];
}

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