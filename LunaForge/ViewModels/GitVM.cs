using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace LunaForge.ViewModels;

public enum GitTab { Changes, History, Branches }

// God please have mercy. This was PAINFUL.
public partial class GitVM : ObservableObject
{
    private GitService _git;

    public GitVM() { }

    public GitVM(string projectRoot)
    {
        _git = new GitService(projectRoot);
        Refresh();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(
        nameof(RefreshCommand),
        nameof(StageSelectedCommand),
        nameof(UnstageSelectedCommand),
        nameof(StageAllCommand),
        nameof(UnstageAllCommand),
        nameof(CommitCommand),
        nameof(CreateBranchCommand),
        nameof(CheckoutBranchCommand),
        nameof(SaveConfigCommand),
        nameof(PushCommand),
        nameof(PullCommand))]
    private bool isBusy;
    public bool IsNotBusy => !IsBusy;

    public bool IsInitialized
    {
        get => field;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsNotInitialized));
                InitRepoCommand.NotifyCanExecuteChanged();
                RefreshCommand.NotifyCanExecuteChanged();
                StageAllCommand.NotifyCanExecuteChanged();
                UnstageAllCommand.NotifyCanExecuteChanged();
                CommitCommand.NotifyCanExecuteChanged();
                CreateBranchCommand.NotifyCanExecuteChanged();
                CheckoutBranchCommand.NotifyCanExecuteChanged();
                SaveConfigCommand.NotifyCanExecuteChanged();
                PushCommand.NotifyCanExecuteChanged();
                PullCommand.NotifyCanExecuteChanged();
            }
        }
    }
    public bool IsNotInitialized => !IsInitialized;

    [ObservableProperty]
    private bool isError;
    [ObservableProperty]
    private string statusMessage = string.Empty;
    [ObservableProperty]
    private string currentBranch = "(no branch)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsChangesTab),
        nameof(IsHistoryTab),
        nameof(IsBranchesTab))]
    private GitTab activeTab = GitTab.Changes;

    public bool IsChangesTab
    {
        get => ActiveTab == GitTab.Changes;
        set { if (value) ActiveTab = GitTab.Changes; }
    }

    public bool IsHistoryTab
    {
        get => ActiveTab == GitTab.History;
        set { if (value) ActiveTab = GitTab.History; }
    }

    public bool IsBranchesTab
    {
        get => ActiveTab == GitTab.Branches;
        set { if (value) ActiveTab = GitTab.Branches; }
    }

    public ObservableCollection<GitFileEntry> StagedFiles { get; } = [];
    public ObservableCollection<GitFileEntry> UnstagedFiles { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StageSelectedCommand))]
    private GitFileEntry? _selectedUnstagedFile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnstageSelectedCommand))]
    private GitFileEntry? selectedStagedFile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private string commitMessage = string.Empty;

    public ObservableCollection<GitCommitEntry> CommitLog { get; } = [];
    public ObservableCollection<GitBranchEntry> Branches { get; } = [];

    [ObservableProperty]
    private string newBranchName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private string authorName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private string authorEmail = string.Empty;

    [ObservableProperty]
    private string remoteName = "origin";

    [ObservableProperty]
    private string remoteUrl = string.Empty;

    private bool CanUseRepo() => IsInitialized && !IsBusy;
    private bool CanInit() => !IsInitialized && !IsBusy;
    private bool CanCommitCheck() =>
        IsInitialized && !IsBusy &&
        !string.IsNullOrWhiteSpace(CommitMessage) &&
        StagedFiles.Any() &&
        !string.IsNullOrWhiteSpace(AuthorName) &&
        !string.IsNullOrWhiteSpace(AuthorEmail);
    private bool CanStageSelected() => SelectedUnstagedFile != null && !IsBusy;
    private bool CanUnstageSelected() => SelectedStagedFile != null && !IsBusy;
    private bool CanStageAll() => IsInitialized && UnstagedFiles.Any() && !IsBusy;
    private bool CanUnstageAll() => IsInitialized && StagedFiles.Any() && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanInit))]
    private void InitRepo()
    {
        RunSafe(() =>
        {
            _git.InitRepository();
            IsInitialized = _git.IsInitialized;
            RefreshCollections();
            SetStatus("Repository Initialized.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    public void Refresh()
    {
        IsInitialized = _git.IsInitialized;
        if (!IsInitialized) return;

        RunSafe(RefreshCollections);
    }

    [RelayCommand(CanExecute = nameof(CanStageSelected))]
    private void StageSelected()
    {
        RunSafe(() =>
        {
            _git.StageFile(SelectedUnstagedFile!.Path);
            RefreshCollections();
        });
    }

    [RelayCommand(CanExecute = nameof(CanUnstageSelected))]
    private void UnstageSelected()
    {
        RunSafe(() =>
        {
            _git.UnstageFile(SelectedStagedFile!.Path);
            RefreshCollections();
        });
    }

    [RelayCommand(CanExecute = nameof(CanStageAll))]
    private void StageAll()
    {
        RunSafe(() => { _git.StageAll(); RefreshCollections(); });
    }

    [RelayCommand(CanExecute = nameof(CanUnstageAll))]
    private void UnstageAll()
    {
        RunSafe(() => { _git.UnstageAll(); RefreshCollections(); });
    }

    [RelayCommand(CanExecute = nameof(CanCommitCheck))]
    private void Commit()
    {
        RunSafe(() =>
        {
            _git.Commit(CommitMessage.Trim(), AuthorName, AuthorEmail);
            Application.Current.Dispatcher.Invoke(() => CommitMessage = string.Empty);
            RefreshCollections();
            SetStatus("Committed successfully.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    private void CreateBranch()
    {
        if (string.IsNullOrWhiteSpace(NewBranchName)) return;
        RunSafe(() =>
        {
            _git.CreateBranch(NewBranchName.Trim());
            Application.Current.Dispatcher.Invoke(() => NewBranchName = string.Empty);
            RefreshCollections();
            SetStatus("Branch created.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    private void CheckoutBranch(GitBranchEntry? branch)
    {
        if (branch == null) return;
        RunSafe(() =>
        {
            _git.CheckoutBranch(branch.Name);
            RefreshCollections();
            SetStatus($"Switched to '{branch.Name}'.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    private void SaveConfig()
    {
        RunSafe(() =>
        {
            _git.SetUserConfig(AuthorName, AuthorEmail);
            SetStatus("Config saved.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    private void Push()
    {
        RunSafe(() =>
        {
            _git.Push(RemoteName, _git.GetCurrentBranchName());
            SetStatus("Push complete.", false);
        });
    }

    [RelayCommand(CanExecute = nameof(CanUseRepo))]
    private void Pull()
    {
        RunSafe(() =>
        {
            _git.Pull(RemoteName, AuthorName, AuthorEmail);
            RefreshCollections();
            SetStatus("Pull complete.", false);
        });
    }

    private void RefreshCollections()
    {
        var status = _git.GetStatus();
        Application.Current.Dispatcher.Invoke(() =>
        {
            StagedFiles.Clear();
            UnstagedFiles.Clear();
            foreach (var f in status)
            {
                if (f.IsStaged)
                    StagedFiles.Add(f);
                else
                    UnstagedFiles.Add(f);
            }
            StageAllCommand.NotifyCanExecuteChanged();
            UnstageAllCommand.NotifyCanExecuteChanged();
            CommitCommand.NotifyCanExecuteChanged();
        });

        var log = _git.GetLog();
        Application.Current.Dispatcher.Invoke(() =>
        {
            CommitLog.Clear();
            foreach (var c in log) CommitLog.Add(c);
        });

        var branches = _git.GetBranches();
        Application.Current.Dispatcher.Invoke(() =>
        {
            Branches.Clear();
            foreach (var b in branches) Branches.Add(b);
            CurrentBranch = _git.GetCurrentBranchName();
        });

        var (name, email) = _git.GetUserConfig();
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(AuthorName) && !string.IsNullOrEmpty(name))
                AuthorName = name;
            if (string.IsNullOrEmpty(AuthorEmail) && !string.IsNullOrEmpty(email))
                AuthorEmail = email;
        });
    }

    private void RunSafe(Action action)
    {
        IsBusy = true;
        IsError = false;
        try
        {
            action();    
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string msg, bool isError)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = msg;
            IsError = isError;
        });
    }

    public void Dispose() => _git.Dispose();
}
