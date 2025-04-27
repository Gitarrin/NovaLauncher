using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace NovaLauncher
{
	public partial class Uninstaller : UserControl
	{
		public Uninstaller()
		{
			InitializeComponent();
		}

		public void DeleteUninstallerKeys()
		{
			try
			{
				if (Helpers.App.IsOlderWindows() || Helpers.App.IsRunningWine())
				{
					// Use LocalMachine for older versions of Windows
					Helpers.Registry.RemoveRegKeys(Registry.LocalMachine, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{Config.AppName}");
				}
				else
				{
					// Use CurrentUser for newer versions of Windows
					Helpers.Registry.RemoveRegKeys(Registry.CurrentUser, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{Config.AppName}");
				}
			} catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.DeleteUninstallerKeysFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		public void PurgeInstallationDirectory()
		{
			// This removes all files and directories in the installation directory.
			// We must be careful, because the installer is also in here
			string installationDirectory = Config.BaseInstallPath;
			string uninstallerPath = $@"{Config.BaseInstallPath}\{Config.AppEXE}";

			try
			{
				Helpers.App.PurgeFilesAndFolders(installationDirectory, new string[] { uninstallerPath });
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.PurgeInstallDirFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		public void EndRunningProcesses()
		{
			// Makes sure that the app is closed before uninstalling
			Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
			foreach (Process process in processes)
			{
				process.Kill();
			}
		}

		private void DeleteProtocolOpenKeys()
		{
			try
			{
				RegistryKey classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
				Helpers.Registry.RemoveRegKeys(classesKey, Config.AppProtocol);
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.DeleteProtocolKeysFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		private void DeleteShortcuts()
		{
			try
			{
				string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\" + Config.AppShortName;
				if (!Directory.Exists(shortcutPath)) Directory.Delete(shortcutPath);
			}
			catch { };
		}


		public void CompleteUninstallation()
		{
			string installPath = Config.BaseInstallPath;
			string exePath = $@"{installPath}\{Config.AppName}";

			// Create a batch file to handle the uninstallation
			string batchFilePath = Path.Combine(Path.GetTempPath(), Config.AppName.Replace(" ", "") + "Uninstaller.bat");

			string cmdCommand = $@"
@echo off
setlocal
set max_retries=10
set retry_count=0
ping -n 1 127.0.0.1 >nul

:delete_exe
del /f /q ""{exePath}"" 2>nul
if %errorlevel% neq 0 (
	set /a retry_count+=1
	if %retry_count% geq %max_retries% (
		echo Failed to delete NovaLauncher.exe after %max_retries% retries.
		exit /b 1
	)
	ping -n 2 127.0.0.1 >nul
	goto delete_exe
)

:delete_directory
rmdir /s /q ""{installPath}"" 2>nul
if %errorlevel% neq 0 (
	echo Failed to delete the installation directory.
	exit /b 1
)

:delete_uninstaller
del /f /q ""{batchFilePath}"" 2>nul
if %errorlevel% neq 0 (
	echo Failed to delete the uninstaller batch file.
	exit /b 1
)

echo Uninstallation completed successfully.
exit /b 0
";

			// Write the batch file to disk
			try
			{
				File.WriteAllText(batchFilePath, cmdCommand);
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.CreateCleanupFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Execute the batch file and exit the uninstaller
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c \"{batchFilePath}\"",
					WindowStyle = ProcessWindowStyle.Hidden,
					CreateNoWindow = true
				});

				// Exit the uninstaller immediately
				Application.Exit();
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.StartCleanupFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		private void Uninstaller_Shown(object sender, EventArgs e)
		{
			this.status.Text = "Uninstalling...";
			this.progressBar.Style = ProgressBarStyle.Marquee;
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, ev) =>
			{
				try
				{
					EndRunningProcesses();
					DeleteProtocolOpenKeys();
					PurgeInstallationDirectory();
					DeleteUninstallerKeys();
					DeleteShortcuts();
					MessageBox.Show("Uninstallation complete.", Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
					CompleteUninstallation();
				}
				catch (Exception exc)
				{
					ev.Result = exc;
					return;
				};
			};
			worker.RunWorkerCompleted += (s, ev) =>
			{
				Close();
			};

			worker.RunWorkerAsync();
		}

		private void Close()
		{
			this.ParentForm?.Close();
		}
	}
}
