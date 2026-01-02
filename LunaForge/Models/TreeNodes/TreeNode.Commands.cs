using CommunityToolkit.Mvvm.Input;
using LunaForge.Backend.EditorCommands;
using LunaForge.Models.Documents;
using LunaForge.Services;
using LunaForge.ViewModels;
using LunaForge.Views;
using System.Linq;
using System.Windows;

namespace LunaForge.Models.TreeNodes;

public abstract partial class TreeNode
{
    private bool IsNodeSelected() => MainWindowModel.Instance.SelectedFile?.SelectedNode != null;

    [RelayCommand(CanExecute = nameof(IsNodeSelected))]
    public void Copy()
    {
        MainWindowModel.Instance.TreeNodeClipboard = (TreeNode)MainWindowModel.Instance.SelectedFile.SelectedNode!.Clone();
    }

    [RelayCommand(CanExecute = nameof(IsNodeSelected))]
    public void Cut()
    {
        MainWindowModel.Instance.TreeNodeClipboard = (TreeNode)MainWindowModel.Instance.SelectedFile.SelectedNode!.Clone();
        TreeNode prev = MainWindowModel.Instance.SelectedFile.SelectedNode!.GetNearestEdited();
        MainWindowModel.Instance.SelectedFile.AddAndExecuteCommand(new DeleteNodeCommand(this));
        if (prev != null)
            MainWindowModel.Instance.SelectedFile.RevealTreeNode(prev);
    }

    [RelayCommand(CanExecute = nameof(CanPaste))]
    public void Paste()
    {
        try
        {
            TreeNode node = (TreeNode)MainWindowModel.Instance.TreeNodeClipboard.Clone();
            node.FixParentDoc((DocumentFileLFD)ParentTree);
            MainWindowModel.Instance.InsertNode(node, node.NodeName);
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't paste the node. Reason:\n{ex}");
            MessageBox.Show("Couldn't paste the node. See the logs for the reason.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private bool CanPaste() => MainWindowModel.Instance.TreeNodeClipboard != null;

    [RelayCommand(CanExecute = nameof(CanLogicallyDelete))]
    public void Delete()
    {
        TreeNode? prev = ParentTree.SelectedNode?.GetNearestEdited();
        if (ParentTree.SelectedNode == null)
            return;
        ParentTree.AddAndExecuteCommand(new DeleteNodeCommand(this));
        if (prev != null)
            ParentTree.RevealTreeNode(prev);
    }

    public bool CanSwitchBan() => !MetaData.CannotBeBanned;

    [RelayCommand(CanExecute = nameof(CanSwitchBan))]
    public void SwitchBan()
    {
        if (ParentTree.AddAndExecuteCommand(new SwitchBanCommand(this, !IsBanned)))
        {
            return;
        }
    }

    [RelayCommand]
    private void ViewCode()
    {
        try
        {
            ProjectCompilerService compiler = new(MainWindowModel.Project);
            (bool, string) res = compiler.CompileLFDFile(ParentTree.FilePath, true).Result;

            if (res.Item1)
            {
                string code = res.Item2;
                CodePreviewWindow preview = new(code);
                preview.ShowDialog();
            }
            else
            {
                Logger.Error($"Failed to compile code.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to compile code. Reason:\n{ex}");
            MessageBox.Show($"Couldn't compile code for node. See logs.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
