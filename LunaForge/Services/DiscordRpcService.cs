using DiscordRPC;
using DiscordRPC.Logging;
using LunaForge.Models;
using ILogger = Serilog.ILogger;

namespace LunaForge.Services;

public class DiscordRPCService : IDisposable
{
    private static readonly ILogger Logger = CoreLogger.Create("DiscordRPC");
    private static DiscordRPCService? _instance;
    private static readonly object _lock = new();

    public static DiscordRPCService? Default
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DiscordRPCService();
                }
            }
            return _instance;
        }
    }

    private DiscordRpcClient? _client;
    private bool _isEnabled;
    private bool _disposed;

    private const string APPLICATION_ID = "1451909705176715379";

    public bool IsInitialized => _client?.IsInitialized ?? false;

    public DiscordRPCService()
    {
        _isEnabled = EditorConfig.Default.Get<bool>("UseDiscordRPC").Value;

        if (_isEnabled)
        {
            Initialize();
        }
        else
        {
            Logger.Information("DiscordRPC is disabled in settings");
        }
    }

    /// <summary>
    /// Call this when you change the value of the UseDiscordRPC setting to enable it.
    /// </summary>
    public void ReInitialize()
    {
        _isEnabled = EditorConfig.Default.Get<bool>("UseDiscordRPC").Value;

        if (_isEnabled)
            Initialize();
    }

    private void Initialize()
    {
        try
        {
            _client = new(APPLICATION_ID)
            {
                Logger = new ConsoleLogger() { Level = LogLevel.Warning }
            };
            _client.OnReady += (sender, e) =>
            {
                Logger.Information($"Discord RPC Ready: {e.User.Username}");
            };
            _client.OnPresenceUpdate += (sender, e) =>
            {
                Logger.Debug($"Discord RPC Updated");
            };
            _client.OnError += (sender, e) =>
            {
                Logger.Error($"Discord RPC Error: {e.Message}");
            };

            _client.Initialize();
            SetDefaultPresence();

            Logger.Information("DiscordRPC service initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to initialize Discord RPC: {ex.Message}");
            _isEnabled = false;
        }
    }

    public void SetDefaultPresence()
    {
        if (!_isEnabled || _client == null)
            return;

        try
        {
            _client.SetPresence(new RichPresence()
            {
                Details = "In Launcher",
                State = "Browsing projects",
                Assets = new Assets()
                {
                    LargeImageKey = "LunaForge_logo",
                    LargeImageText = "LunaForge Editor",
                },
                Timestamps = Timestamps.Now
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set default presence: {ex.Message}");
        }
    }

    public void SetProjectPresence(string projectName)
    {
        if (!_isEnabled || _client == null)
            return;

        try
        {
            _client.SetPresence(new RichPresence()
            {
                Details = $"Editing: {projectName}",
                State = "Working on project",
                Assets = new Assets()
                {
                    LargeImageKey = "LunaForge_logo",
                    LargeImageText = "LunaForge Editor",
                    SmallImageKey = "editing",
                    SmallImageText = "Editing"
                },
                Timestamps = Timestamps.Now
            });

            Logger.Debug($"Updated presence for project: {projectName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set project presence: {ex.Message}");
        }
    }

    public void SetEditingFilePresence(string projectName, string fileName)
    {
        if (!_isEnabled || _client == null)
            return;

        try
        {
            _client.SetPresence(new RichPresence()
            {
                Details = $"Project: {projectName}",
                State = $"Editing: {fileName}",
                Assets = new Assets()
                {
                    LargeImageKey = "LunaForge_logo",
                    LargeImageText = "LunaForge Editor",
                    SmallImageKey = "editing",
                    SmallImageText = "Editing"
                },
                Timestamps = Timestamps.Now
            });

            Logger.Debug($"Updated presence for file: {fileName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set editing file presence: {ex.Message}");
        }
    }

    public void SetDebuggingPresence(string projectName)
    {
        if (!_isEnabled || _client == null)
            return;

        try
        {
            _client.SetPresence(new RichPresence()
            {
                Details = $"Project: {projectName}",
                State = "Debugging",
                Assets = new Assets()
                {
                    LargeImageKey = "LunaForge_logo",
                    LargeImageText = "LunaForge Editor",
                    SmallImageKey = "debugging",
                    SmallImageText = "Debugging"
                },
                Timestamps = Timestamps.Now
            });

            Logger.Debug($"Updated presence for debugging: {projectName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set debugging presence: {ex.Message}");
        }
    }

    public void ClearPresence()
    {
        if (!_isEnabled || _client == null)
            return;

        try
        {
            _client.ClearPresence();
            Logger.Debug("Cleared Discord presence");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to clear presence: {ex.Message}");
        }
    }

    public void Enable()
    {
        if (_isEnabled)
            return;

        _isEnabled = true;
        Initialize();
    }

    public void Disable()
    {
        if (!_isEnabled)
            return;

        _isEnabled = false;
        ClearPresence();
        _client?.Dispose();
        _client = null;

        Logger.Information("DiscordRPC service disabled");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearPresence();
        _client?.Dispose();
        _disposed = true;

        Logger.Information("DiscordRPC service disposed");
    }
}
