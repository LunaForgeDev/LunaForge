using LunaForge.Models;
using LunaForge.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace LunaForge.Services;

public class GameRunnerService
{
    private static readonly ILogger Logger = CoreLogger.Create("GameRunner");

    private readonly Project proj;
    private Process? process;

    public bool IsRunning => process is { HasExited: false };

    public event Action<bool>? RunningStateChanged;

    public GameRunnerService(Project proj)
    {
        this.proj = proj;
    }

    /// <summary>
    /// Compiles the project and launches the LuaSTG instance of that specific project.
    /// Redirects stdout and stderr to the debug panel.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> RunAsync()
    {
        if (IsRunning)
        {
            Logger.Warning("A LuaSTG instance is already running.");
            AppendLine("[Runner] A LuaSTG instance is already running.");
            return false;
        }

        AppendLine("[Runner] Compiling project...");
        bool compiled = await proj.Compiler.Compile();
        if (!compiled)
        {
            AppendLine("[Runner] Compilation failed. Check the compiler output for details.");
            Logger.Error("Compilation failed when trying to run LuaSTG");
            return false;
        }
        AppendLine("[Runner] Compilation successful.");

        string executablePath = proj.ProjectConfig.Get<string>("LuaSTGExecutablePath").Value;
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            Logger.Error("LuaSTG executable not found at: {path}", executablePath);
            AppendLine("[Runner] ERROR: LuaSTG executable path is not configured or does not exist.");
            MessageBox.Show("The LuaSTG executable path is not configured or does not exist.\nPlease set it in your project settings.",
                "Run Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += OnOutputReceived;
            process.ErrorDataReceived += OnErrorReceived;
            process.Exited += OnProcessExited;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Logger.Information("LuaSTG launched (PID: {0})", process.Id);
            AppendLine($"[Runner] LuaSTG launched (PID: {process.Id})");
            RunningStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start LuaSTG process.");
            AppendLine($"[Runner] ERROR: Failed to start LuaSTG: {ex.Message}");
            process = null;
            return false;
        }

        return true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        try
        {
            process!.Kill(entireProcessTree: true);
            AppendLine("[Runner] LuaSTG process terminated.");
            Logger.Information("LuaSTG process (PID: {0}) terminated by user.", process.Id);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to terminate LuaSTG process.");
        }
    }

    private void OnOutputReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null) return;
        AppendLine(e.Data);
    }

    private void OnErrorReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null) return;
        AppendLine($"[ERR] {e.Data}");
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        int exitCode = -1;
        try {
            exitCode = process?.ExitCode ?? -1;
        }
        catch { }

        AppendLine($"[Runner] LuaSTG exited with code {exitCode}.");
        Logger.Information("LuaSTG exited with code {exitCode}", exitCode);

        process = null;
        RunningStateChanged?.Invoke(false);
    }

    private static void AppendLine(string line)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MainWindowModel.Instance?.AppendToGameLog(line);
        });
    }
}
