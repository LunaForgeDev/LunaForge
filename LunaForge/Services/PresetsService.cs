using LunaForge.Helpers;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using LunaForge.Nodes.General;
using LunaForge.ViewModels;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace LunaForge.Services;

public static class PresetsService
{
    private readonly static ILogger Logger = CoreLogger.Create("Presets");

    private readonly static string presetsDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LunaForge",
            ".presets"
        );

    public static List<string> PresetsPaths { get; set; } = [];

    public static void LoadPresets()
    {
        Directory.CreateDirectory(presetsDirectory).Attributes |= FileAttributes.Directory | FileAttributes.Hidden;

        string[] files = Directory.GetFiles(presetsDirectory, "*.lfpt");
        PresetsPaths = [.. files];

        Logger.Information("Preset list refreshed");
    }

    public static TreeNode? LoadPreset(string filePath)
    {
        try
        {
            TreeNode node = null;
            if (MainWindowModel.Instance.SelectedFile is DocumentFileLFD lfd)
                node = DocumentFileLFD.CreateNodeFromFile(filePath, lfd);
            else
                Logger.Error($"Current selected file is not a LFD file. Cannot insert preset.");
            return node;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't load preset from file. Reason:\n{ex}");
            return null;
        }
    }

    public static void SavePreset(TreeNode node)
    {
        TreeNode t = new RootFolder(null);
        TreeNode selected = (TreeNode)node.Clone();
        t.AddChild(selected);

        SaveFileDialog dialog = new()
        {
            Filter = "LunaForge Preset|*.lfpt",
            InitialDirectory = presetsDirectory,
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                string path = dialog.FileName;
                using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
                using StreamWriter sw = new(fs, Encoding.UTF8);
                t.SerializeFile(sw, 0);
                Logger.Information("Preset saved.");

                LoadPresets();
            }
            catch (Exception ex)
            {
                Logger.Error($"Couldn't save preset. Reason:\n{ex}");
                MessageBox.Show($"Couldn't save preset. See the logs for more info.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public static bool InsertPreset(string? filePath = null)
    {
        if (filePath == null)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "LunaForge Preset|*.lfpt",
                InitialDirectory = presetsDirectory,
            };
            if (dialog.ShowDialog() == false)
                return false;

            filePath = dialog.FileName;
        }

        try
        {
            TreeNode? node = LoadPreset(filePath);
            if (node == null)
            {
                Logger.Error("Failed to insert preset: Preset empty or malformed");
                return false;
            }

            if (node.Children == null || node.Children.Count < 1 || node.Children[0] == null)
                throw new Exception("Preset empty");

            MainWindowModel.Instance.InsertNode(node.Children[0], node.Children[0].NodeName);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to insert preset. Reason:\n{ex}");
            return false;
        }
    }
}
