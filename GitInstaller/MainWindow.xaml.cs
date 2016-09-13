using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GitInstaller
{
	public partial class MainWindow : Window
	{
		private string filesPath = "install-files";

		private string tortoiseGitInstaller;
		private string gitInstaller;
		private string gitLfsInstaller;
		private string diffMarginInstaller;

		private Version tortoiseGitCurrentVersion;
		private Version gitCurrentVersion;
		private Version gitLfsCurrentVersion;

		private Version tortoiseGitNewVersion;
		private Version gitNewVersion;
		private Version gitLfsNewVersion;

		public MainWindow()
		{
			InitializeComponent();

			string myVersion = System.Reflection.Assembly.GetExecutingAssembly()
				.GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false)
				.OfType<System.Reflection.AssemblyFileVersionAttribute>()
				.FirstOrDefault()
				?.Version;
			versionLabel.Text = "v" + myVersion;

			FindInstallers();
			FindCurrentVersions();

			if (GetBeyondComparePath() == null)
			{
				beyondCompareCheckBox.IsEnabled = false;
				ToolTipService.SetShowOnDisabled(beyondCompareCheckBox, true);
				beyondCompareCheckBox.ToolTip = "Beyond Compare 4 not installed";
			}
			else
			{
				beyondCompareCheckBox.IsChecked = true;
			}

			if (GetNotepadPPPath() == null)
			{
				notepadPPCheckBox.IsEnabled = false;
				ToolTipService.SetShowOnDisabled(notepadPPCheckBox, true);
				notepadPPCheckBox.ToolTip = "Notepad++ not installed";
			}
			else
			{
				notepadPPCheckBox.IsChecked = true;
			}

			if (diffMarginInstaller != null)
			{
				diffMarginCheckBox.IsChecked = true;
			}
			else
			{
				diffMarginCheckBox.IsEnabled = false;
				ToolTipService.SetShowOnDisabled(diffMarginCheckBox, true);
				diffMarginCheckBox.ToolTip = "Installer missing";
				diffMarginVersionLabel.Text = "(Missing)";
				diffMarginVersionLabel.Foreground = Brushes.RoyalBlue;
			}

			if (!(tortoiseGitCurrentVersion?.Major > 0))
			{
				resetTGitConfigCheckBox.IsEnabled = false;
				resetTGitConfigCheckBox.IsChecked = true;
				ToolTipService.SetShowOnDisabled(resetTGitConfigCheckBox, true);
				resetTGitConfigCheckBox.ToolTip = "No configuration found";
			}

			SetVersionLabel(tortoiseGitVersionLabel, tortoiseGitCurrentVersion, tortoiseGitNewVersion);
			SetVersionLabel(gitVersionLabel, gitCurrentVersion, gitNewVersion);
			SetVersionLabel(gitLfsVersionLabel, gitLfsCurrentVersion, gitLfsNewVersion);
		}

		private void Window_Loaded(object sender, RoutedEventArgs args)
		{
			Top = SystemParameters.WorkArea.Bottom - Height;
			Left = (SystemParameters.WorkArea.Left + SystemParameters.WorkArea.Right) / 2 - Width / 2;

			if (Environment.OSVersion.Version.Major >= 10)
			{
				// Compensate Windows 10's extra invisible border
				Top += 7;
			}
		}

		private async void StartButton_Click(object sender, RoutedEventArgs args)
		{
			startButton.IsEnabled = false;
			progressBar.Value = 0;

			double progress1 = 17;
			double progress2 = progress1 + 43;
			double progress3 = progress2 + 17;

			try
			{
				IncreaseProgress(progress1);
				if (resetTGitConfigCheckBox.IsChecked == true && !await InstallTortoiseGitConfig())
				{
					MessageBox.Show("TortoiseGit config could not be installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				if (beyondCompareCheckBox.IsChecked == true)
				{
					InstallTortoiseGitBCConfig();
				}
				if (notepadPPCheckBox.IsChecked == true)
				{
					InstallTortoiseGitNPPConfig();
				}
				if (tortoiseGitInstaller != null &&
					(tortoiseGitCurrentVersion == null || tortoiseGitNewVersion > tortoiseGitCurrentVersion) &&
					!await InstallTortoiseGit())
				{
					MessageBox.Show("TortoiseGit could not be installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				tortoiseGitCurrentVersion = tortoiseGitNewVersion;   // Can't be determined until TortoiseGit was started
				progressBar.Value = progress1;

				if (gitInstaller != null &&
					(gitCurrentVersion == null || gitNewVersion > gitCurrentVersion))
				{
					IncreaseProgress(progress2);
					InstallGitSetupConfig();
					if (!await InstallGit())
					{
						MessageBox.Show("Git could not be installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}

					try
					{
						Process.Start("git.exe", "config --global tgit.projectlanguage -1");
					}
					catch (Exception ex)
					{
						MessageBox.Show("Global Git configuration could not be set. " + ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
				progressBar.Value = progress2;

				if (gitLfsInstaller != null &&
					(gitLfsCurrentVersion == null || gitLfsNewVersion > gitLfsCurrentVersion))
				{
					IncreaseProgress(progress3);
					if (!await InstallGitLfs())
					{
						MessageBox.Show("Git LFS could not be installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
				}
				progressBar.Value = progress3;

				if (diffMarginCheckBox.IsChecked == true)
				{
					IncreaseProgress(100);
					if (!await InstallDiffMargin())
					{
						MessageBox.Show("Git Diff Margin for Visual Studio could not be installed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
				}
				progressBar.Value = 100;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				startButton.IsEnabled = true;
			}
			finally
			{
				FindCurrentVersions(true);
				SetVersionLabel(tortoiseGitVersionLabel, tortoiseGitCurrentVersion, tortoiseGitNewVersion);
				SetVersionLabel(gitVersionLabel, gitCurrentVersion, gitNewVersion);
				SetVersionLabel(gitLfsVersionLabel, gitLfsCurrentVersion, gitLfsNewVersion);
			}

			if (progressBar.Value == 100)
			{
				if (tortoiseGitInstaller == null ||
					gitInstaller == null ||
					gitLfsInstaller == null)
				{
					MessageBox.Show(
						"Installation complete, but one or more components were missing and have not been installed or upgraded.",
						"Git Installer",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				}
				else
				{
					MessageBox.Show(
						"Installation complete.",
						"Git Installer",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}
				Close();
			}
		}

		private void FindInstallers()
		{
			tortoiseGitInstaller = FindHighestVersionFile(@"^TortoiseGit-#-64bit\.msi$", out tortoiseGitNewVersion);
			gitInstaller = FindHighestVersionFile(@"^Git-#-64-bit\.exe$", out gitNewVersion);
			gitLfsInstaller = FindHighestVersionFile(@"^git-lfs-windows-#\.exe$", out gitLfsNewVersion);
			Version vsDiffMarginVersion;
			diffMarginInstaller = FindHighestVersionFile(@"^GitDiffMargin-#.*\.vsix$", out vsDiffMarginVersion);
			if (diffMarginInstaller == null)
			{
				diffMarginInstaller = FindHighestVersionFile(@"^GitDiffMargin\.vsix$", out vsDiffMarginVersion);
			}
		}

		private void FindCurrentVersions(bool afterInstall = false)
		{
			if (!afterInstall)
			{
				using (var key = Registry.CurrentUser.OpenSubKey(@"Software\TortoiseGit"))
				{
					string version = key?.GetValue("CurrentVersion") as string;
					if (!string.IsNullOrEmpty(version))
					{
						tortoiseGitCurrentVersion = SimplifyVersion(Version.Parse(version));
					}
				}
			}

			using (var key = Registry.LocalMachine.OpenSubKey(@"Software\GitForWindows"))
			{
				string version = key?.GetValue("CurrentVersion") as string;
				if (!string.IsNullOrEmpty(version))
				{
					gitCurrentVersion = SimplifyVersion(Version.Parse(version));
				}
			}

			using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{286391DE-F778-44EA-9375-1B21AAA04FF0}_is1"))
			{
				string version = key?.GetValue("DisplayVersion") as string;
				if (!string.IsNullOrEmpty(version))
				{
					gitLfsCurrentVersion = SimplifyVersion(Version.Parse(version));
				}
			}
		}

		private static string GetBeyondComparePath()
		{
			string path;
			using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Scooter Software\Beyond Compare 4"))
			{
				path = key?.GetValue("ExePath") as string;
				if (!string.IsNullOrEmpty(path))
					return path;
			}
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Scooter Software\Beyond Compare 4"))
			{
				path = key?.GetValue("ExePath") as string;
				if (!string.IsNullOrEmpty(path))
					return path;
			}
			return null;
		}

		private static string GetNotepadPPPath()
		{
			string path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				"Notepad++",
				"notepad++.exe");
			if (File.Exists(path))
				return path;

			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				"Notepad++",
				"notepad++.exe");
			if (File.Exists(path))
				return path;

			return null;
		}

		private void SetVersionLabel(TextBlock textBlock, Version currentVersion, Version newVersion)
		{
			if (currentVersion?.Major > 0)
			{
				// Already installed
				if (newVersion?.Major > 0)
				{
					// Installer available
					if (newVersion > currentVersion)
					{
						// Update available
						textBlock.Text = currentVersion.ToString() + " → " + newVersion.ToString();
						textBlock.Foreground = Brushes.DarkOrange;
					}
					else
					{
						// No update available
						textBlock.Text = currentVersion.ToString() + " (Current)";
						textBlock.Foreground = Brushes.Green;
					}
				}
				else
				{
					// Installer not found
					textBlock.Text = currentVersion.ToString() + " (Missing)";
					textBlock.Foreground = Brushes.RoyalBlue;
				}
			}
			else
			{
				// Not installed
				if (newVersion?.Major > 0)
				{
					// Installer available
					textBlock.Text = "New: " + newVersion.ToString();
					textBlock.Foreground = Brushes.DarkOrange;
				}
				else
				{
					// Installer not found
					textBlock.Text = "Missing";
					textBlock.Foreground = Brushes.Red;
					startButton.IsEnabled = false;
				}
			}
		}

		private string FindHighestVersionFile(string pattern, out Version version)
		{
			try
			{
				if (pattern.Contains("#"))
				{
					pattern = pattern.Replace("#", "([0-9.]+)");
					var result = Directory.GetFiles(filesPath)
						.Where(fileName => Regex.IsMatch(Path.GetFileName(fileName), pattern, RegexOptions.IgnoreCase))
						.Select(fileName => new
						{
							FileName = fileName,
							Version = Version.Parse(Regex.Match(Path.GetFileName(fileName), pattern).Groups[1].Value)
						})
						.OrderByDescending(x => x.Version)
						.FirstOrDefault();

					if (result != null)
					{
						version = SimplifyVersion(result.Version);
						return result.FileName;
					}
				}
				else
				{
					var result = Directory.GetFiles(filesPath)
						.Where(fileName => Regex.IsMatch(Path.GetFileName(fileName), pattern, RegexOptions.IgnoreCase))
						.Select(fileName => new
						{
							FileName = fileName,
							Version = new Version()
						})
						.OrderByDescending(x => x.FileName)
						.FirstOrDefault();

					if (result != null)
					{
						version = result.Version;
						return result.FileName;
					}
				}
			}
			catch (DirectoryNotFoundException)
			{
			}

			version = new Version();
			return null;
		}

		private static Version SimplifyVersion(Version version)
		{
			if (version.Revision <= 0 && version.Build <= 0)
			{
				version = new Version(version.Major, version.Minor);
			}
			else if (version.Revision <= 0)
			{
				version = new Version(version.Major, version.Minor, version.Build);
			}
			return version;
		}

		private void IncreaseProgress(double to)
		{
			double from = progressBar.Value;
			double step = 1;
			DispatcherTimer timer = null;
			timer = new DispatcherTimer(
				TimeSpan.FromMilliseconds(50),
				DispatcherPriority.Normal,
				(s, a) =>
				{
					step += 0.01;
					double newValue = (1 - (1 / step)) * (to - from) + from;
					if (progressBar.Value < newValue)
					{
						// Increase by timer
						progressBar.Value = newValue;
					}
					else
					{
						// Already increased elsewhere, stop the timer
						timer?.Stop();
					}
				},
				Dispatcher);
		}

		private void InstallGitSetupConfig()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1", true))
			{
				key.SetValue("Inno Setup: Setup Type", "default");
				key.SetValue("Inno Setup: Selected Components", "assoc,assoc_sh");
				key.SetValue("Inno Setup: Deselected Components", @"icons,icons\desktop,ext,ext\shellhere,ext\guihere,consolefont");
				key.SetValue("Inno Setup: Language", "default");
				key.SetValue("Inno Setup CodeFile: Path Option", "Cmd");
				key.SetValue("Inno Setup CodeFile: SSH Option", "Plink");
				key.SetValue("Inno Setup CodeFile: CRLF Option", "CRLFCommitAsIs");
				key.SetValue("Inno Setup CodeFile: Bash Terminal Option", "ConHost");
				key.SetValue("Inno Setup CodeFile: Performance Tweaks FSCache", "Enabled");
			}
		}

		private Task<bool> InstallTortoiseGitConfig()
		{
			return StartProcessAsync("regedit.exe", $@"/s ""{Path.Combine(filesPath, "TortoiseGit-Config.reg")}""");
		}

		private void InstallTortoiseGitBCConfig()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\TortoiseGit", true))
			{
				key.SetValue("Diff", GetBeyondComparePath());
				key.SetValue("DiffViewer", $@"""{GetBeyondComparePath()}"" /fv=""Text Patch""");
				key.SetValue("Merge", $@"""{GetBeyondComparePath()}"" %mine %theirs %base %merged");
			}
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\TortoiseGit\DiffTools", true))
			{
				key.SetValue(".bmp", $@"""{GetBeyondComparePath()}"" %base %mine");
				key.SetValue(".gif", $@"""{GetBeyondComparePath()}"" %base %mine");
				key.SetValue(".ico", $@"""{GetBeyondComparePath()}"" %base %mine");
				key.SetValue(".jpg", $@"""{GetBeyondComparePath()}"" %base %mine");
				key.SetValue(".png", $@"""{GetBeyondComparePath()}"" %base %mine");
				key.SetValue(".tif", $@"""{GetBeyondComparePath()}"" %base %mine");
			}
		}

		private void InstallTortoiseGitNPPConfig()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(@"Software\TortoiseGit", true))
			{
				key.SetValue("AlternativeEditor", GetNotepadPPPath());
			}
		}

		private Task<bool> InstallTortoiseGit()
		{
			return StartProcessAsync("msiexec.exe", $@"/i ""{tortoiseGitInstaller}"" /passive /norestart");
		}

		private Task<bool> InstallGit()
		{
			return StartProcessAsync(gitInstaller, "/silent");
		}

		private Task<bool> InstallGitLfs()
		{
			return StartProcessAsync(gitLfsInstaller, "/silent");
		}

		private Task<bool> InstallDiffMargin()
		{
			// VS2015
			string vsixInstaller = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\VSIXInstaller.exe";
			if (File.Exists(vsixInstaller))
			{
				// Can install silently
				return StartProcessAsync(vsixInstaller, $@"/q ""{diffMarginInstaller}""");
			}
			else
			{
				// Try interactive method
				return StartProcessAsync(diffMarginInstaller, "");
			}
		}

		private Task<bool> StartProcessAsync(string fileName, string arguments)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = fileName,
					Arguments = arguments
				},
				EnableRaisingEvents = true
			};

			var tcs = new TaskCompletionSource<bool>();
			process.Exited += (sender, args) =>
			{
				tcs.SetResult(process.ExitCode == 0);
				process.Dispose();
			};
			process.Start();

			return tcs.Task;
		}
	}
}
