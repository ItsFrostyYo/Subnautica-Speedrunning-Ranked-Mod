using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Windows.Forms;
using SubnauticaSpeedrunningMod.Shared;

namespace SubnauticaSpeedrunningMod.Updater;

internal sealed class UpdateProgressWindow : Form
{
    private static readonly string[] PackageRootFiles =
    {
        ".doorstop_version",
        "doorstop_config.ini",
        "Launch Mod.cmd",
        "winhttp.dll"
    };

    private static readonly string[] LegacyRootLauncherFiles =
    {
        "Launch Mod.exe",
        "Launch Mod.dll",
        "Launch Mod.deps.json",
        "Launch Mod.runtimeconfig.json"
    };

    private static readonly string[] PreservedModDirectories =
    {
        "Config",
        "Logs",
        "Client"
    };

    private readonly UpdateArguments _options;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;

    public int ExitCode { get; private set; }

    public UpdateProgressWindow(UpdateArguments options)
    {
        _options = options;
        Text = "Subnautica Speedrunning Mod Updater";
        Width = 560;
        Height = 150;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        _statusLabel = new Label
        {
            Left = 18,
            Top = 18,
            Width = 500,
            Height = 36,
            Text = "Preparing update..."
        };

        _progressBar = new ProgressBar
        {
            Left = 18,
            Top = 64,
            Width = 500,
            Height = 22,
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous
        };

        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        try
        {
            await RunUpdateAsync();
            ExitCode = 0;
            Close();
        }
        catch (Exception ex)
        {
            ExitCode = 1;
            MessageBox.Show(
                "The Subnautica Speedrunning Mod update failed." + Environment.NewLine + Environment.NewLine + ex.Message,
                "Update Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1);
            Close();
        }
    }

    private async Task RunUpdateAsync()
    {
        SetStatus("Waiting for launcher to close...", 3);
        await WaitForLauncherExitAsync();

        string workingRoot = Path.Combine(Path.GetTempPath(), "SubnauticaSpeedrunningMod", "UpdateWork", Guid.NewGuid().ToString("N"));
        string zipPath = Path.Combine(workingRoot, "mod-update.zip");
        string extractRoot = Path.Combine(workingRoot, "extract");
        Directory.CreateDirectory(workingRoot);

        try
        {
            await DownloadUpdateAsync(zipPath);
            SetStatus("Extracting update package...", 74);
            ZipFile.ExtractToDirectory(zipPath, extractRoot);
            ApplyExtractedFiles(extractRoot, _options.InstallRoot);
            RelaunchLauncher();
        }
        finally
        {
            TryDeleteDirectory(workingRoot);
        }
    }

    private async Task WaitForLauncherExitAsync()
    {
        try
        {
            using Process process = Process.GetProcessById(_options.WaitForPid);
            while (!process.HasExited)
            {
                await Task.Delay(100);
            }
        }
        catch
        {
            // The launcher is already gone.
        }
    }

    private async Task DownloadUpdateAsync(string zipPath)
    {
        SetStatus("Downloading " + (_options.VersionLabel.Length > 0 ? _options.VersionLabel : ModClientRelease.DisplayVersion) + "...", 5);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SubnauticaSpeedrunningMod-Updater/" + ModClientRelease.DisplayVersion);

        using HttpResponseMessage response = await httpClient.GetAsync(_options.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long totalBytes = response.Content.Headers.ContentLength ?? -1;
        using Stream contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int read;
        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            downloadedBytes += read;

            if (totalBytes > 0)
            {
                int progress = 5 + (int)(65L * downloadedBytes / totalBytes);
                SetStatus("Downloading update... " + (downloadedBytes * 100L / totalBytes) + "%", progress);
            }
        }
    }

    private void ApplyExtractedFiles(string extractRoot, string installRoot)
    {
        SetStatus("Applying files...", 80);

        string packageRoot = FindPackageRoot(extractRoot);
        ValidatePackage(packageRoot);

        string extractedModRoot = Path.Combine(packageRoot, "SubnauticaSpeedrunningMod");
        string installedModRoot = Path.Combine(installRoot, "SubnauticaSpeedrunningMod");
        string transactionId = Guid.NewGuid().ToString("N");
        string stagedModRoot = Path.Combine(installRoot, ".SubnauticaSpeedrunningMod.update-" + transactionId);
        string backupModRoot = Path.Combine(installRoot, ".SubnauticaSpeedrunningMod.backup-" + transactionId);
        string backupRootFiles = Path.Combine(installRoot, ".SubnauticaSpeedrunningMod.root-backup-" + transactionId);
        var previouslyExistingRootFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool oldModMoved = false;
        bool newModInstalled = false;

        try
        {
            SetStatus("Preparing updated mod folder...", 82);
            CopyDirectoryContents(extractedModRoot, stagedModRoot);
            CopyPreservedInstallData(installedModRoot, stagedModRoot);

            Directory.CreateDirectory(backupRootFiles);
            string[] rootFilesTouched = GetRootFilesTouched();
            for (int i = 0; i < rootFilesTouched.Length; i++)
            {
                string fileName = rootFilesTouched[i];
                string installedPath = Path.Combine(installRoot, fileName);
                if (!File.Exists(installedPath))
                {
                    continue;
                }

                previouslyExistingRootFiles.Add(fileName);
                File.Copy(installedPath, Path.Combine(backupRootFiles, fileName), true);
            }

            SetStatus("Installing updated mod folder...", 88);
            if (Directory.Exists(installedModRoot))
            {
                Directory.Move(installedModRoot, backupModRoot);
                oldModMoved = true;
            }

            Directory.Move(stagedModRoot, installedModRoot);
            newModInstalled = true;

            for (int i = 0; i < PackageRootFiles.Length; i++)
            {
                string fileName = PackageRootFiles[i];
                File.Copy(Path.Combine(packageRoot, fileName), Path.Combine(installRoot, fileName), true);
            }

            DeleteLegacyRootLauncherFiles(installRoot);
            SetStatus("Finishing update...", 99);
        }
        catch
        {
            SetStatus("Restoring previous installation...", 95);
            if (newModInstalled && Directory.Exists(installedModRoot))
            {
                Directory.Delete(installedModRoot, true);
            }

            if (oldModMoved && Directory.Exists(backupModRoot))
            {
                Directory.Move(backupModRoot, installedModRoot);
                oldModMoved = false;
            }

            RestoreRootFiles(installRoot, backupRootFiles, previouslyExistingRootFiles);
            throw;
        }
        finally
        {
            TryDeleteDirectory(stagedModRoot);
            if (!oldModMoved)
            {
                TryDeleteDirectory(backupModRoot);
            }
            else if (newModInstalled)
            {
                TryDeleteDirectory(backupModRoot);
            }

            TryDeleteDirectory(backupRootFiles);
        }
    }

    private static string FindPackageRoot(string extractRoot)
    {
        var candidates = new List<string>();
        AddPackageRootCandidate(candidates, extractRoot);

        string[] directories = Directory.GetDirectories(extractRoot, "*", SearchOption.AllDirectories);
        for (int i = 0; i < directories.Length; i++)
        {
            AddPackageRootCandidate(candidates, directories[i]);
        }

        if (candidates.Count == 0)
        {
            throw new InvalidDataException("The update package does not contain a SubnauticaSpeedrunningMod folder.");
        }

        if (candidates.Count > 1)
        {
            throw new InvalidDataException("The update package contains multiple possible installation roots.");
        }

        return candidates[0];
    }

    private static void AddPackageRootCandidate(List<string> candidates, string path)
    {
        if (Directory.Exists(Path.Combine(path, "SubnauticaSpeedrunningMod")))
        {
            candidates.Add(path);
        }
    }

    private static void ValidatePackage(string packageRoot)
    {
        string[] requiredFiles =
        {
            @"SubnauticaSpeedrunningMod\Launch Mod.exe",
            @"SubnauticaSpeedrunningMod\Bootstrap\SubnauticaSpeedrunningMod.Bootstrap.dll",
            @"SubnauticaSpeedrunningMod\Runtime\SubnauticaSpeedrunningMod.Runtime.dll",
            @"SubnauticaSpeedrunningMod\Updater\Mod Updater.exe"
        };

        for (int i = 0; i < requiredFiles.Length; i++)
        {
            string requiredPath = Path.Combine(packageRoot, requiredFiles[i]);
            if (!File.Exists(requiredPath))
            {
                throw new InvalidDataException("The update package is incomplete. Missing: " + requiredFiles[i]);
            }
        }

        for (int i = 0; i < PackageRootFiles.Length; i++)
        {
            if (!File.Exists(Path.Combine(packageRoot, PackageRootFiles[i])))
            {
                throw new InvalidDataException("The update package is incomplete. Missing: " + PackageRootFiles[i]);
            }
        }
    }

    private static void CopyPreservedInstallData(string installedModRoot, string stagedModRoot)
    {
        if (!Directory.Exists(installedModRoot))
        {
            return;
        }

        for (int i = 0; i < PreservedModDirectories.Length; i++)
        {
            string name = PreservedModDirectories[i];
            string sourcePath = Path.Combine(installedModRoot, name);
            if (Directory.Exists(sourcePath))
            {
                CopyDirectoryContents(sourcePath, Path.Combine(stagedModRoot, name));
            }
        }

        string seedStateSource = Path.Combine(installedModRoot, "Seeds", "State");
        if (Directory.Exists(seedStateSource))
        {
            CopyDirectoryContents(seedStateSource, Path.Combine(stagedModRoot, "Seeds", "State"));
        }
    }

    private static void RestoreRootFiles(string installRoot, string backupRootFiles, HashSet<string> previouslyExistingRootFiles)
    {
        string[] rootFilesTouched = GetRootFilesTouched();
        for (int i = 0; i < rootFilesTouched.Length; i++)
        {
            string fileName = rootFilesTouched[i];
            string installedPath = Path.Combine(installRoot, fileName);
            if (previouslyExistingRootFiles.Contains(fileName))
            {
                string backupPath = Path.Combine(backupRootFiles, fileName);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, installedPath, true);
                }
            }
            else if (File.Exists(installedPath))
            {
                File.Delete(installedPath);
            }
        }
    }

    private static string[] GetRootFilesTouched()
    {
        var files = new string[PackageRootFiles.Length + LegacyRootLauncherFiles.Length];
        Array.Copy(PackageRootFiles, 0, files, 0, PackageRootFiles.Length);
        Array.Copy(LegacyRootLauncherFiles, 0, files, PackageRootFiles.Length, LegacyRootLauncherFiles.Length);
        return files;
    }

    private static void DeleteLegacyRootLauncherFiles(string installRoot)
    {
        for (int i = 0; i < LegacyRootLauncherFiles.Length; i++)
        {
            string path = Path.Combine(installRoot, LegacyRootLauncherFiles[i]);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void CopyDirectoryContents(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        string[] directories = Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories);
        for (int i = 0; i < directories.Length; i++)
        {
            string directory = directories[i];
            string relativePath = directory.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(destinationRoot, relativePath));
        }

        string[] files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string sourcePath = files[i];
            string relativePath = sourcePath.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destinationPath = Path.Combine(destinationRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationRoot);
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    private void RelaunchLauncher()
    {
        string launcherPath = Path.Combine(_options.InstallRoot, _options.LauncherRelativePath);
        if (!File.Exists(launcherPath))
        {
            throw new FileNotFoundException("Updated launcher executable was not found.", launcherPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = launcherPath,
            WorkingDirectory = Path.GetDirectoryName(launcherPath) ?? _options.InstallRoot,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(_options.RelaunchArgument))
        {
            startInfo.ArgumentList.Add(_options.RelaunchArgument);
        }

        Process.Start(startInfo);
    }

    private void SetStatus(string text, int progress)
    {
        _statusLabel.Text = text;
        _progressBar.Value = Math.Max(_progressBar.Minimum, Math.Min(_progressBar.Maximum, progress));
        _statusLabel.Refresh();
        _progressBar.Refresh();
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
