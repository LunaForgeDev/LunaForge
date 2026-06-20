using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunaForge.Services;

// Warning: Smelly garbage coming. It's very not refactored and the UI is whatever I could build in a timely matter.
// I'll need to HEAVILY rework this.

public class GitFileEntry
{
    public string Path { get; set; } = string.Empty;
    public FileStatus Status { get; set; }
    public bool IsStaged { get; set; }

    public string StatusLabel => Status switch
    {
        FileStatus.NewInWorkdir => "Untracked",
        FileStatus.ModifiedInWorkdir => "Modified",
        FileStatus.DeletedFromWorkdir => "Deleted",
        FileStatus.RenamedInWorkdir => "Renamed",
        FileStatus.NewInIndex => "Added",
        FileStatus.ModifiedInIndex => "Staged",
        FileStatus.DeletedFromIndex => "Removed",
        FileStatus.RenamedInIndex => "Renamed",
        _ => Status.ToString()
    };

    public string StatusIcon => IsStaged
        ? Status switch
        {
            FileStatus.NewInIndex => "+",
            FileStatus.ModifiedInIndex => "*",
            FileStatus.DeletedFromIndex => "X",
            FileStatus.RenamedInIndex => "->",
            _ => "*"
        }
        : Status switch
        {
            FileStatus.NewInWorkdir => "?",
            FileStatus.ModifiedInWorkdir => "*",
            FileStatus.DeletedFromWorkdir => "X",
            _ => "*"
        };
}

public class GitCommitEntry
{
    public string ShortSha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTimeOffset When { get; set; }
    public string WhenDisplay => When.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public class GitBranchEntry
{
    public string Name { get; set; } = string.Empty;
    public bool IsCurrentBranch { get; set; }
    public bool IsRemote { get; set; }
}

public class GitService : IDisposable
{
    private Repository? _repo;
    private readonly string _projectRoot;
    private const string LunaForgeIgnore = ".lunaforge";

    public bool IsInitialized => _repo != null;
    public string ProjectRoot => _projectRoot;

    public GitService(string projectRoot)
    {
        _projectRoot = projectRoot;
        TryOpenRepository();
    }

    private void TryOpenRepository()
    {
        try
        {
            var repoPath = Repository.Discover(_projectRoot);
            if (!string.IsNullOrEmpty(repoPath))
            {
                _repo = new Repository(repoPath);
            }
        }
        catch (RepositoryNotFoundException)
        {
            // Not a git repo. Ignore.
        }
    }

    public void InitRepository()
    {
        Repository.Init(_projectRoot);
        TryOpenRepository();
        EnsureGitIgnore();
    }

    private void EnsureGitIgnore()
    {
        var gitignorePath = Path.Combine(_projectRoot, ".gitignore");
        var entry = $"/{LunaForgeIgnore}\n.git/\n";

        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, $"# Lunaforge metadata - Do not track\n{entry}\n");
        }
        else
        {
            var content = File.ReadAllText(gitignorePath);
            if (!content.Contains(LunaForgeIgnore))
                File.AppendAllText(gitignorePath, $"\n# LunaForge metadata\n{entry}\n");
        }
    }

    public IReadOnlyList<GitFileEntry> GetStatus()
    {
        if (_repo == null)
            return [];
        List<GitFileEntry> results = [];

        foreach (var entry in _repo.RetrieveStatus(new StatusOptions()))
        {
            if (entry.FilePath.StartsWith(LunaForgeIgnore,
                StringComparison.OrdinalIgnoreCase)) continue;

            var indexFlags = entry.State & (
                FileStatus.NewInIndex |
                FileStatus.ModifiedInIndex |
                FileStatus.DeletedFromIndex |
                FileStatus.RenamedInIndex);

            if (indexFlags != FileStatus.Unaltered)
            {
                results.Add(new GitFileEntry
                {
                    Path = entry.FilePath,
                    Status = indexFlags,
                    IsStaged = true
                });
            }

            var wtFlags = entry.State & (
                FileStatus.NewInWorkdir |
                FileStatus.ModifiedInWorkdir |
                FileStatus.DeletedFromWorkdir |
                FileStatus.RenamedInWorkdir);

            if (wtFlags != FileStatus.Unaltered)
            {
                results.Add(new GitFileEntry
                {
                    Path = entry.FilePath,
                    Status = wtFlags,
                    IsStaged = false
                });
            }
        }

        return results;
    }

    public void StageFile(string relativePath)
    {
        if (_repo == null) return;
        Commands.Stage(_repo, relativePath);
    }

    public void StageAll()
    {
        if (_repo == null) return;
        Commands.Stage(_repo, "*");
    }

    public void UnstageFile(string relativePath)
    {
        if (_repo == null) return;
        Commands.Unstage(_repo, relativePath);
    }

    public void UnstageAll()
    {
        if (_repo == null) return;
        var staged = GetStatus().Where(e => e.IsStaged).Select(e => e.Path).ToList();
        foreach (var path in staged)
            Commands.Unstage(_repo, path);
    }

    public Commit Commit(string message, string authorName, string authorEmail)
    {
        if (_repo == null)
            throw new InvalidOperationException("Repository not initialized.");

        var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);
        return _repo.Commit(message, sig, sig);
    }

    public IReadOnlyList<GitCommitEntry> GetLog(int maxCount = 50)
    {
        if (_repo == null)
            return [];

        return [.. _repo.Commits
            .QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time })
            .Take(maxCount)
            .Select(c => new GitCommitEntry
            {
                ShortSha = c.Sha[..7],
                Message = c.MessageShort,
                Author = c.Author.Name,
                When = c.Author.When
            })];
    }

    public IReadOnlyList<GitBranchEntry> GetBranches()
    {
        if (_repo == null)
            return [];

        return [.. _repo.Branches
            .Select(b => new GitBranchEntry
            {
                Name = b.FriendlyName,
                IsCurrentBranch = b.IsCurrentRepositoryHead,
                IsRemote = b.IsRemote
            })];
    }

    public string GetCurrentBranchName()
    {
        return _repo?.Head?.FriendlyName ?? "(no branch)";
    }

    public void CreateBranch(string name)
    {
        if (_repo == null)
            return;
        _repo.CreateBranch(name);
    }

    public void CheckoutBranch(string name)
    {
        if (_repo == null) return;
        var branch = _repo.Branches[name]
            ?? throw new ArgumentException($"Branch '{name}' not found.");
        Commands.Checkout(_repo, branch);
    }

    public IReadOnlyList<string> GetRemoteNames()
    {
        if (_repo == null) return [];
        return [.. _repo.Network.Remotes.Select(r => r.Name)];
    }

    public void AddRemote(string name, string url)
    {
        if (_repo == null) return;
        _repo.Network.Remotes.Add(name, url);
    }

    public void Fetch(string remoteName, CredentialsHandler? credentialsHandler = null)
    {
        if (_repo == null) return;

        var remote = _repo.Network.Remotes[remoteName];
        var refSpecs = remote.FetchRefSpecs.Select(rs => rs.Specification);

        var options = new FetchOptions();
        if (credentialsHandler != null)
            options.CredentialsProvider = credentialsHandler;

        Commands.Fetch(_repo, remote.Name, refSpecs, options, $"Fetch from {remoteName}");
    }

    public MergeResult Pull(string remoteName, string authorName, string authorEmail, CredentialsHandler? credentialsHandler = null)
    {
        if (_repo == null)
            throw new InvalidOperationException("Repository not initialised.");

        var sig = new Signature(authorName, authorEmail, DateTimeOffset.Now);

        var options = new PullOptions
        {
            FetchOptions = new FetchOptions()
        };
        if (credentialsHandler != null)
            options.FetchOptions.CredentialsProvider = credentialsHandler;

        return Commands.Pull(_repo, sig, options);
    }

    public void Push(string remoteName, string branchName, CredentialsHandler? credentialsHandler = null)
    {
        if (_repo == null) return;

        var remote = _repo.Network.Remotes[remoteName];
        var options = new PushOptions();
        if (credentialsHandler != null)
            options.CredentialsProvider = credentialsHandler;

        _repo.Network.Push(remote,
            $"refs/heads/{branchName}:refs/heads/{branchName}", options);
    }

    public (string name, string email) GetUserConfig()
    {
        if (_repo == null) return (string.Empty, string.Empty);

        var cfg = _repo.Config;
        var name = cfg.Get<string>("user.name")?.Value ?? string.Empty;
        var email = cfg.Get<string>("user.email")?.Value ?? string.Empty;
        return (name, email);
    }

    public void SetUserConfig(string name, string email)
    {
        if (_repo == null) return;
        _repo.Config.Set("user.name", name, ConfigurationLevel.Local);
        _repo.Config.Set("user.email", email, ConfigurationLevel.Local);
    }

    public void Dispose() => _repo?.Dispose();
}