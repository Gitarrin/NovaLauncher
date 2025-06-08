using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NovaLauncher.Helpers.Forms
{
	internal class Uninstaller
	{
		private Base helperBase;

		public void Init(Base helperBaseF)
		{
			helperBase = helperBaseF;

			helperBase.UpdateTextWithLog(helperBase.instance.statusLbl, "Uninstalling...");
			helperBase.DoThingsWInvoke(() => helperBase.instance.progressBar.Style = ProgressBarStyle.Marquee);

			helperBase.CreateBackgroundTask(
				(s, e) =>
				{
					try
					{
						if (!App.KillAllBlox()) throw new Exception("KillAllBlox failed.");
						DeleteProtocolOpenKeys();
						DeleteUninstallerKeys();
						DeleteShortcuts();
						if (App.IsRunningFromInstall()) Program.logger.Shutdown();
						PurgeInstallationDirectory();

						MessageBox.Show($"{Config.AppName} has been uninstalled", Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
						CompleteUninstallation();
					}
					catch (Exception exc)
					{
						e.Result = exc;
						return;
					};
				},
				(s, e) =>
				{
					helperBase.Close();
				}
			);
		}

		internal void DeleteUninstallerKeys()
		{
			try
			{
				if (App.IsOlderWindows() || App.IsRunningWine())
				{
					// Use LocalMachine for older versions of Windows
					Registry.RemoveRegKeys(Microsoft.Win32.Registry.LocalMachine, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{Config.AppName}");
				}
				else
				{
					// Use CurrentUser for newer versions of Windows
					Registry.RemoveRegKeys(Microsoft.Win32.Registry.CurrentUser, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{Config.AppName}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.DeleteUninstallerKeysFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		internal void PurgeInstallationDirectory()
		{
			// This removes all files and directories in the installation directory.
			// We must be careful, because the installer is also in here
			string installationDirectory = Config.BaseInstallPath;
			string uninstallerPath = $@"{Config.BaseInstallPath}\{Config.AppEXE}";

			try
			{
				App.PurgeFilesAndFolders(installationDirectory, new string[] { uninstallerPath });
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.PurgeInstallDirFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		internal void DeleteProtocolOpenKeys()
		{
			try
			{
				RegistryKey classesKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);
				Registry.RemoveRegKeys(classesKey, Config.AppProtocol);
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.DeleteProtocolKeysFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw ex;
			}
		}

		internal void DeleteShortcuts()
		{
			try
			{
				string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\" + Config.AppShortName;
				if (Directory.Exists(shortcutPath)) Directory.Delete(shortcutPath);
			}
			catch { };
		}

		internal void CompleteUninstallation()
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
	}
}
