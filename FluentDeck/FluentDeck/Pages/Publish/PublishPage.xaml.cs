using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluentDeck.Pages.Publish;

public sealed partial class PublishPage : Page
{
    private readonly string _repoPath;
    private List<string> _detectedApps = new();
    private bool _hasLocalChanges = false;

    public PublishPage()
    {
        this.InitializeComponent();
        var mainWindow = App.MainWindowInstance;
        string? readmePath = mainWindow?.FindReadmePath();
        _repoPath = !string.IsNullOrEmpty(readmePath) ? Path.GetDirectoryName(readmePath) ?? "" : "";

        Loaded += PublishPage_Loaded;
    }

    private async void PublishPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_repoPath) || !Directory.Exists(_repoPath))
        {
            LogConsole("Error: Repository path not resolved.", true);
            PublishInfoBar.Title = "Repository Error";
            PublishInfoBar.Message = "Could not resolve repository path.";
            PublishInfoBar.Severity = InfoBarSeverity.Error;
            PublishInfoBar.IsOpen = true;
            ScanningSection.Visibility = Visibility.Collapsed;
            RepositoryStatusPanel.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            await CheckGitStatusAsync();
        }
        catch (Exception ex)
        {
            LogConsole($"Error during status check: {ex.Message}", true);
        }
        finally
        {
            ScanningSection.Visibility = Visibility.Collapsed;
            RepositoryStatusPanel.Visibility = Visibility.Visible;
            CommitPanel.Visibility = _hasLocalChanges ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async Task CheckGitStatusAsync()
    {
        // 1. Run git status --porcelain to see modified files
        var (statusExit, statusOut, statusErr) = await RunGitCommandAsync("status --porcelain");
        if (statusExit != 0)
        {
            LogConsole($"git status failed:\n{statusErr}", true);
            return;
        }

        var modifiedFiles = new List<string>();
        var lines = statusOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Contains("README.md") || trimmed.Contains("apps_logo.yml"))
            {
                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    modifiedFiles.Add(parts[^1]);
                }
            }
        }

        if (modifiedFiles.Count == 0)
        {
            _hasLocalChanges = false;
            NoChangesText.Visibility = Visibility.Visible;
            ChangesPanel.Visibility = Visibility.Collapsed;
            TerminalConsoleGrid.Visibility = Visibility.Collapsed;
            LogConsole("No modifications detected in catalog files.");
            return;
        }

        _hasLocalChanges = true;
        NoChangesText.Visibility = Visibility.Collapsed;
        ChangesPanel.Visibility = Visibility.Visible;
        ModifiedFilesText.Text = string.Join(", ", modifiedFiles);

        // 2. Parse diff to find added app entries
        var (diffExit, diffOut, diffErr) = await RunGitCommandAsync("diff --unified=0 README.md");
        if (diffExit == 0)
        {
            _detectedApps = ParseAddedAppsFromDiff(diffOut);
        }

        if (_detectedApps.Count > 0)
        {
            AddedAppsText.Text = string.Join("\n", _detectedApps.Select(a => $"• {a}"));
            CommitMsgInput.Text = $"Add {string.Join(", ", _detectedApps)} to list";
        }
        else
        {
            AddedAppsText.Text = "No new app entries detected (only formatting or logo updates).";
            CommitMsgInput.Text = "Update catalog assets";
        }

        PublishBtn.IsEnabled = true;
        TerminalConsoleGrid.Visibility = _detectedApps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        LogConsole($"Scan complete. Found {modifiedFiles.Count} modified file(s) and {_detectedApps.Count} new app(s).");
    }

    private List<string> ParseAddedAppsFromDiff(string diffContent)
    {
        var addedApps = new List<string>();
        var deletedApps = new List<string>();
        var lines = diffContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("+++") || line.StartsWith("---")) continue;

            if (line.StartsWith("+"))
            {
                var match = Regex.Match(line, @"\[([^\]]+)\]\(([^)]+)\)");
                if (match.Success)
                {
                    string appName = match.Groups[1].Value.Trim();
                    if (!IsIgnoredAppName(appName))
                    {
                        addedApps.Add(appName);
                    }
                }
            }
            else if (line.StartsWith("-"))
            {
                var match = Regex.Match(line, @"\[([^\]]+)\]\(([^)]+)\)");
                if (match.Success)
                {
                    string appName = match.Groups[1].Value.Trim();
                    if (!IsIgnoredAppName(appName))
                    {
                        deletedApps.Add(appName);
                    }
                }
            }
        }

        return addedApps.Except(deletedApps).Distinct().ToList();
    }

    private bool IsIgnoredAppName(string name)
    {
        return name.Contains("Table Of Contents") || name.Contains("Contributing") || name.Contains("Newly Added Apps");
    }

    private async void PublishButton_Click(object sender, RoutedEventArgs e)
    {
        PublishBtn.IsEnabled = false;
        PublishInfoBar.IsOpen = false;
        TerminalConsoleGrid.Visibility = Visibility.Visible;

        try
        {
            LogConsole("\nStarting sync & publish flow (Option 1)...");

            // 1. Git add
            LogConsole("Executing: git add README.md apps_logo.yml...");
            var (addExit, addOut, addErr) = await RunGitCommandAsync("add README.md apps_logo.yml");
            if (addExit != 0)
            {
                LogConsole($"Error during add:\n{addErr}", true);
                throw new Exception("Git add failed.");
            }

            // 2. Git commit
            string commitMsg = string.IsNullOrWhiteSpace(CommitMsgInput.Text) ? "Update catalog" : CommitMsgInput.Text.Trim();
            LogConsole($"Executing: git commit -m \"{commitMsg}\"...");
            var (commitExit, commitOut, commitErr) = await RunGitCommandAsync($"commit -m \"{commitMsg}\"");
            LogConsole(commitOut);
            if (commitExit != 0)
            {
                LogConsole($"Error during commit:\n{commitErr}", true);
                throw new Exception("Git commit failed. Make sure you have staged changes.");
            }

            // 3. Git pull --rebase
            LogConsole("Executing: git pull --rebase...");
            var (pullExit, pullOut, pullErr) = await RunGitCommandAsync("pull --rebase");
            LogConsole(pullOut);
            if (pullExit != 0)
            {
                LogConsole($"Error during pull:\n{pullErr}", true);
                throw new Exception("Git pull failed. There may be merge conflicts. Please resolve conflicts manually in your git terminal.");
            }

            // 4. Git push
            LogConsole("Executing: git push...");
            var (pushExit, pushOut, pushErr) = await RunGitCommandAsync("push");
            LogConsole(pushOut);
            LogConsole(pushErr);
            if (pushExit != 0)
            {
                LogConsole($"Error during push:\n{pushErr}", true);
                throw new Exception("Git push failed. Verify your remote credentials are set up.");
            }

            LogConsole("\nSUCCESS! Changes successfully published to GitHub.", false);
            PublishInfoBar.Title = "Published";
            PublishInfoBar.Message = "Changes successfully committed and pushed to GitHub.";
            PublishInfoBar.Severity = InfoBarSeverity.Success;
            PublishInfoBar.IsOpen = true;

            // Refresh state
            await CheckGitStatusAsync();
        }
        catch (Exception ex)
        {
            LogConsole($"\nFAILED: {ex.Message}", true);
            PublishInfoBar.Title = "Publish Failed";
            PublishInfoBar.Message = ex.Message;
            PublishInfoBar.Severity = InfoBarSeverity.Error;
            PublishInfoBar.IsOpen = true;
            PublishBtn.IsEnabled = true;
        }
    }

    private void CommitMsgInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        PublishBtn.IsEnabled = _hasLocalChanges && !string.IsNullOrWhiteSpace(CommitMsgInput.Text);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        ConsoleOutput.Text = "Console cleared.";
    }

    private async Task<(int ExitCode, string Output, string Error)> RunGitCommandAsync(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    private void LogConsole(string message, bool isError = false)
    {
        if (string.IsNullOrEmpty(message)) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            string prefix = isError ? "[ERROR] " : "";
            ConsoleOutput.Text += $"\n{prefix}{message}";
            ConsoleScrollViewer.ChangeView(null, ConsoleScrollViewer.ScrollableHeight, null);
        });
    }
}
