using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

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
				if (Directory.Exists(shortcutPath)) Directory.Delete(shortcutPath, true);
			}
			catch { };
		}

		internal void CompleteUninstallation()
		{
			string exePath = $@"{Config.BaseInstallPath}\{Config.AppEXE}";

			string tmpBat = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName().Split('.')[0]}.bat");
			string[] uninstallBat = new[] {
				"@ech off",
				"setlocal",
				"set max_retries=10",
				"set retry_count=0",
				"ping -n 1 127.0.0.1 >nul",
				"",
				":delete_exe",
				$"del /f /q \"{exePath}\" 2>nul",
				"if %errorlevel% neq 0 (",
				"\tset /a retry_count+=1",
				"\tif %retry_count% geq %max_retries% (",
				$"\t\techo Failed to delete {Config.AppEXE} after %max_retries% retries.",
				"\t\texit /b 1",
				"\t)",
				"\tping -n 2 127.0.0.1 >nul",
				"\tgoto delete_exe",
				")",
				"",
				":delete_installdir",
				$"rmdir /s /q \"{Config.BaseInstallPath}\" 2>nul",
				"if %errorlevel% neq 0 (",
				"\techo Failed to delete the installation directory.",
				"\texit /b 1",
				")",
				"",
				":delete_uninstaller",
				$"del /f /q \"{tmpBat}\" 2>nul",
				"if %errorlevel% neq 0 (",
				"\techo Failed to delete the installation directory.",
				"\texit /b 1",
				")",
				"",
				"del \"%~f0\""
			};

			try
			{
				File.WriteAllLines(tmpBat, uninstallBat);
			}
			catch (Exception ex)
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Uninstaller.CreateCleanupFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = tmpBat,
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
