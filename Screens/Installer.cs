using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NovaLauncher
{
	public partial class Installer : UserControl
	{
		LaunchData launchData;
		WebClient webClient;
		GameClient gameClient;
		private static T Get<T>(Type type, string field) => (T)(type.GetField(field)?.GetValue(null) ?? default(T));

		public Installer()
		{
			InitializeComponent();
		}

		#region Launcher
		private void Form1_Shown(object sender, EventArgs e) => PerformLauncherCheck();

		private void PerformLauncherCheck()
		{
			// Strange to see launch data here, but its important, so we can see the protocol.
			try
			{
				if (Program.cliArgs.Token != null)
					launchData = JsonConvert.DeserializeObject<LaunchData>(Encoding.UTF8.GetString(Convert.FromBase64String(Program.cliArgs.Token.Split(':')[1])));
			}
			catch { }; // We'll see if this is still uninit'd later.

			if (Program.cliArgs.UpdateInfo != null)
			{
				// We were in the middle of an update! Send right along!
				try
				{
					string updateInfoJson = Encoding.UTF8.GetString(Convert.FromBase64String(Program.cliArgs.UpdateInfo));
					LatestClientInfo latestClientInfo = JsonConvert.DeserializeObject<LatestClientInfo>(updateInfoJson);

					InstallWithWorker(Config.AppName, null, Config.BaseInstallPath, latestClientInfo, true);
					return;
				}
				catch
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConfigureFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				};
			}

			// Now, we need to check the Launcher version first.
			status.Text = "Checking version...";
			string launcherVersion = Helper.App.GetInstalledVersion();

			BackgroundWorker versionWorker = new BackgroundWorker();
			versionWorker.DoWork += (s, ev) =>
			{
				ev.Result = Helper.Web.GetLatestServerVersionInfo(WebConfig.LauncherSetup);
			};
			versionWorker.RunWorkerCompleted += (s, ev) =>
			{
				if (!(ev.Result is LatestClientInfo latestLauncherInfo))
				{
					status.Text = "Error: Can't connect.";
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				if (Program.cliArgs.UpdateLauncher)
				{
					UpdateWithWorker(Config.AppName, Config.BaseInstallPath, latestLauncherInfo, false, true);
					return;
				}
				else if (!Helper.App.IsRunningFromInstall())
				{
					if (!File.Exists(Config.BaseInstallPath + @"\" + Config.AppEXE))
					{
						UpdateWithWorker(Config.AppName, Config.BaseInstallPath, latestLauncherInfo, false, true);
						return;
					}

					FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Config.BaseInstallPath + @"\" + Config.AppEXE);
					launcherVersion = fvi.ProductVersion ?? fvi.FileVersion;
				}

				if (latestLauncherInfo.Version != launcherVersion) UpdateWithWorker(Config.AppName, Config.BaseInstallPath, latestLauncherInfo, true, true);
				else PerformClientCheck();

				return;
			};
			versionWorker.RunWorkerAsync();
		}
		#endregion

		#region Client
		private void PerformClientCheck()
		{
			CreateUninstallKeys(Config.BaseInstallPath);

			// Launcher is all good.
			if (Program.cliArgs.Token == null)
			{
				// Okay, we weren't launching a client. We'll stop here.
				InstallerGUI.LoadScreen(new Screens.InstallCompleted());
				return;
			}

			// Since we are launching a client, let's continue.
			status.Text = "Checking version...";
			progressBar.Style = ProgressBarStyle.Marquee;

			Type clientType = typeof(Config).GetNestedType("Client" + launchData.Version, BindingFlags.Public | BindingFlags.Static);
			if (clientType == null)
			{
				MessageBox.Show(Error.GetErrorMsg(launchData.Version == null ? Error.Installer.LaunchClientNoVersion : Error.Installer.LaunchClientFailed, new Dictionary<string, string>() { { "{CLIENT}", launchData.Version } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			// Setting up client.
			gameClient = new GameClient
			{
				Name = Get<string>(clientType, "Name"),
				InstallPath = Get<string>(clientType, "InstallPath"),
				Executable = Get<string>(clientType, "Executable"),
				HostExecutable = Get<string>(clientType, "HostExecutable"),
				StudioExecutable = Get<string>(clientType, "StudioExecutable"),
				ClientCheck = Get<bool>(clientType, "SHA256Check"),
				DiscordRPC = Get<bool>(clientType, "DiscordRPC"),
				Version = Helper.App.GetInstalledVersion(launchData.Version)
			};

			BackgroundWorker versionWorker = new BackgroundWorker();
			versionWorker.DoWork += (s, ev) =>
			{
				ev.Result = Helper.Web.GetLatestServerVersionInfo($"{WebConfig.ClientSetup}/{launchData.Version}");
			};
			versionWorker.RunWorkerCompleted += (s, ev) =>
			{
				if (!(ev.Result is LatestClientInfo latestClientInfo))
				{
					status.Text = "Error: Can't connect.";
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), gameClient.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				if (gameClient.Version == "" || Program.cliArgs.UpdateClient) UpdateWithWorker(gameClient.Name, gameClient.InstallPath, latestClientInfo, false, false);
				else if (latestClientInfo.Version != gameClient.Version) UpdateWithWorker(gameClient.Name, gameClient.InstallPath, latestClientInfo, true, false);
				else PerformClientStart();

				return;
			};
			versionWorker.RunWorkerAsync();
		}

		private void PerformClientStart()
		{
			status.Text = $"Starting {gameClient.Name}...";
			cancelButton.Enabled = false;
			cancelButton.Visible = false;

			BackgroundWorker worker = new BackgroundWorker();

			worker.DoWork += (s, e) =>
			{
				Process process;
				if (launchData.LaunchType == "host")
				{
					process = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\" + gameClient.HostExecutable,
							Arguments = $"-t {launchData.LaunchToken}",
							WindowStyle = ProcessWindowStyle.Normal,
							WorkingDirectory = gameClient.InstallPath
						}
					};
				}
				else if (launchData.LaunchType == "studio" || launchData.LaunchType == "build")
				{
					process = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\" + gameClient.StudioExecutable,
							Arguments = $"-{(launchData.LaunchType == "build" ? "build" : "ide")} -script \"{launchData.JoinScript}\"",
							WindowStyle = ProcessWindowStyle.Normal,
							WorkingDirectory = gameClient.InstallPath
						}
					};
				}
				else
				{
					process = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\" + gameClient.Executable,
							Arguments = $"-a \"{WebConfig.GameBase}/Login/Negotiate.ashx\" -t \"{launchData.Ticket}\" -j \"{launchData.JoinScript}\"",
							WindowStyle = ProcessWindowStyle.Maximized,
							WorkingDirectory = gameClient.InstallPath
						}
					};
				}
				process.Start();
				this.Invoke(new Action(() => { progressBar.Value = 50; }));

				int waited = 0;
				int stop_waiting = 60000;
				while (true)
				{
					if (!string.IsNullOrEmpty(process.MainWindowTitle)) break; // The game actually launched!
					if (waited >= stop_waiting) break; // Timeout
					if (process.HasExited) break; // Process died for some reason
					System.Threading.Thread.Sleep(1000);
					process.Refresh();
					waited += 1000;
				}
				if (waited >= stop_waiting || process.HasExited)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientTimeout, new Dictionary<string, string>() { { "{CLIENT}", gameClient.Name } }), gameClient.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				// Roblox 2012 needs a little help sometimes.
				try { if (gameClient.Name == Config.Client2012.Name) Helper.App.BringToFront(process.MainWindowTitle); } catch { };

				// Discord RPC
				Process rpcProcess;
				if (gameClient.DiscordRPC && File.Exists(gameClient.InstallPath + @"\NovarinRPCManager.exe"))
				{
					rpcProcess = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\NovarinRPCManager.exe",
							Arguments = $"-j {launchData.JobId} -g {launchData.PlaceId} -l {Config.AppProtocol} -p {process.Id}",
							WindowStyle = ProcessWindowStyle.Hidden,
							WorkingDirectory = gameClient.InstallPath
						}
					};
					rpcProcess.Start();
				};
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				this.Invoke(new Action(() => { progressBar.Value = 100; }));
				Close();
				return;
			};
			worker.RunWorkerAsync();
		}
		#endregion

		#region Download/Install/Upgrade/etc.
		private void UpdateWithWorker(string name, string installPath, LatestClientInfo latestInfo, bool upgrade = false, bool isForLauncher = false)
		{
			status.Text = $"{(upgrade ? "Upgrading" : "Downloading the latest")} {name}...";
			progressBar.Style = ProgressBarStyle.Continuous;
			cancelButton.Enabled = true;

			webClient = new WebClient();
			webClient.Headers.Add("user-agent", Helper.Web.GetUserAgent());
			string tempPath = Path.GetTempPath() + (isForLauncher ? Config.AppEXE : name) + (isForLauncher ? "" : ".zip");

			Stopwatch downWatch = new Stopwatch();
			webClient.DownloadProgressChanged += (s, e) =>
			{
				bool isShiftDown = (ModifierKeys & Keys.Shift) == Keys.Shift;

				if (!downWatch.IsRunning) downWatch.Start();
				int progress = e.ProgressPercentage;

				double bytesRecv = e.BytesReceived;
				double secsElp = downWatch.Elapsed.TotalSeconds;
				double speed = secsElp > 0 ? bytesRecv / secsElp : 0;

				// Update everything!
				progressDebugLbl.Text = $"{progress}% ({Helper.Web.FormatSpeed(speed)}/s)";
				progressDebugLbl.Visible = isShiftDown;
				progressBar.Value = progress;
			};
			webClient.DownloadFileCompleted += (s, e) =>
			{
				progressDebugLbl.Visible = false;

				if (e.Cancelled)
				{
					Cancel(tempPath);
					return;
				}
				if (e.Error != null)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Cancel(tempPath);
					return;
				}

				BackgroundWorker worker = new BackgroundWorker();
				worker.DoWork += (s1, ev1) =>
				{
					if (!Helper.App.IsDownloadOK(tempPath, latestInfo, isForLauncher ? Config.AppSHA256Check : gameClient.ClientCheck))
					{
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadCorrupted), name, MessageBoxButtons.OK, MessageBoxIcon.Error);
						Cancel(tempPath);
						return;
					}
				};
				worker.RunWorkerCompleted += (s1, ev1) =>
				{
					if (isForLauncher)
					{
						if (!upgrade && Helper.App.IsRunningWine() && !Program.cliArgs.HideWineMessage)
						{
							string[] wineMessage = new string[]
							{
									"We have detected that you are installing Novarin via Wine. We will attempt to make your experience as smooth as possible (like RPC being native probably), but some configuration is required.",
									"",
									"To get Novarin working via Wine, here's what you need to do:",
									$" 1. Create a .desktop file that handles the protocol '{Config.AppProtocol}://token123', which calls {Config.AppEXE} (found in Appdata/Local/Novarizz) like '{Config.AppEXE.Replace(".exe", "")} --token token123' with token123 being whatever is passed thru to the protocol.",
									" 2. Install DVXK thru something like winetricks.",
									"",
									"If you do those two things correctly, you should be able to play Novarin.",
									"",
									"P.S. If your scripting this, you can pass in -w to the setup to hide this warning.",
									"",
									"Stay safe on your Linux travels!"
							};
							MessageBox.Show(string.Join("\n", wineMessage), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						}

						if (upgrade)
						{
							// Because we're about to replace the current EXE, we need to restart the launcher.
							string updateInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(latestInfo)));
							string[] args = Environment.GetCommandLineArgs();
							string[] cmds =
							{
								$"ping -n 2 127.0.0.1 >nul", // Give us ~2 seconds to make sure the launcher closes.
								$"move /Y \"{tempPath}\" \"{Config.BaseInstallPath}\\{Config.AppEXE}\"",
								$"{string.Join(" ", args)} --upinfo {updateInfo}"
							};
							Process.Start(new ProcessStartInfo
							{
								FileName = "cmd.exe",
								Arguments = $"/c {string.Join(" && ", cmds)}",
								WindowStyle = ProcessWindowStyle.Hidden,
								CreateNoWindow = true
							});
							Close();
							return;
						}
					}
					InstallWithWorker(name, tempPath, installPath, latestInfo, isForLauncher);

				};
				worker.RunWorkerAsync();
			};
			try
			{
				webClient.DownloadFileAsync(new Uri(latestInfo.Url), tempPath);
			}
			catch
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadStartFail));
				Cancel(tempPath);
			}
		}

		private void CreateProtocolOpenKeys(string installPath)
		{
			try
			{
				RegistryKey classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

				// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
				Helper.Registry.RemoveRegKeys(classesKey, "novarin12");
				Helper.Registry.RemoveRegKeys(classesKey, "novarin15");
				Helper.Registry.RemoveRegKeys(classesKey, "novarin16");
				// -- END LEGACY KEY REMOVAL --

				RegistryKey key = classesKey.CreateSubKey(Config.AppProtocol);
				key.CreateSubKey("DefaultIcon").SetValue("", installPath + @"\" + Config.AppEXE);
				key.SetValue("", Config.AppProtocol + ":Protocol");
				key.SetValue("URL Protocol", "");
				key.CreateSubKey(@"shell\open\command").SetValue("", installPath + @"\" + Config.AppEXE + " --token %1");
				key.Close();
			}
			catch
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConfigureFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CreateUninstallKeys(string installPath)
		{
			try
			{
				RegistryKey uninstallKey = (Helper.App.IsOlderWindows() || Helper.App.IsRunningWine()) ? Registry.LocalMachine : Registry.CurrentUser;
				uninstallKey = uninstallKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);

				// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
				Helper.Registry.RemoveRegKeys(uninstallKey, "Novarin 2012");
				Helper.Registry.RemoveRegKeys(uninstallKey, "Novarin 2015");
				Helper.Registry.RemoveRegKeys(uninstallKey, "Novarin 2016");
				// -- END LEGACY KEY REMOVAL --

				RegistryKey key = uninstallKey.CreateSubKey(Config.AppName);
				key.SetValue("DisplayName", Config.AppName);
				key.SetValue("DisplayIcon", installPath + @"\" + Config.AppEXE);
				key.SetValue("Publisher", "Novarin");

				key.SetValue("DisplayVersion", Helper.App.GetInstalledVersion());
				int[] versionElements = Array.ConvertAll(Helper.App.GetInstalledVersion().Split('.'), int.Parse);
				if (!(versionElements.Length < 3))
				{
					key.SetValue("VersionMajor", versionElements[0]);
					key.SetValue("VersionMinor", versionElements[1]);
					key.SetValue("Version", versionElements[2]);
				}

				key.SetValue("AppReadme", WebConfig.GameBase);
				key.SetValue("URLUpdateInfo", WebConfig.UpdateInfo);
				key.SetValue("URLInfoAbout", WebConfig.AboutInfo);
				key.SetValue("HelpLink", WebConfig.HelpInfo);
				if (key.GetValue("InstallDate") == null) key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
				key.SetValue("EstimatedSize", (int)Helper.App.CalculateDirectorySize(installPath) / 1024, RegistryValueKind.DWord);

				key.SetValue("UninstallString", installPath + @"\" + Config.AppEXE + " --uninstall");
				key.SetValue("InstallLocation", installPath);
				key.SetValue("NoModify", 1);
				key.SetValue("NoRepair", 1);

				key.Close();
			}
			catch
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.CreateUninstallKeys), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CreateShortcut(string title, string desc, string path, string args = "")
		{
			string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\" + Config.AppShortName;
			if (!Directory.Exists(shortcutPath)) Directory.CreateDirectory(shortcutPath);

			string shortcutLink = shortcutPath + @"\" + title + ".lnk";

			Type shellType = Type.GetTypeFromProgID("WScript.Shell");
			object shell = Activator.CreateInstance(shellType);
			object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, shell, new object[] { shortcutLink });
			Type shortcutType = shortcut.GetType();

			shortcutType.InvokeMember("TargetPath", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { path });
			shortcutType.InvokeMember("Arguments", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { args });
			shortcutType.InvokeMember("Description", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { desc });
			shortcutType.InvokeMember("WindowStyle", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { 1 });
			shortcutType.InvokeMember("WorkingDirectory", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { Path.GetDirectoryName(path) });
			shortcutType.InvokeMember("IconLocation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut,
				new object[] { path });
			shortcutType.InvokeMember("Save", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, shortcut, null);
		}

		private void InstallWithWorker(string name, string tempPath, string installPath, LatestClientInfo latestInfo, bool isForLauncher = false)
		{
			status.Text = $"Installing {name}...";
			progressBar.Style = ProgressBarStyle.Marquee;
			cancelButton.Enabled = false;

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				try
				{
					if (isForLauncher)
					{
						if (tempPath != null)
						{
							if (!Directory.Exists(installPath)) Directory.CreateDirectory(installPath);
							File.Copy(tempPath, installPath + @"\" + Config.AppEXE, true);
						};

						this.Invoke(new Action(() => { status.Text = $"Configuring {name}..."; }));

						if (!Helper.App.IsRunningWine()) CreateProtocolOpenKeys(installPath);
						CreateShortcut(Config.AppName, $"{Config.AppShortName} Launcher", installPath + @"\" + Config.AppEXE, "");
					}
					else
					{
						if (Directory.Exists(installPath)) Directory.Delete(installPath, true);

						Helper.ZIP.ExtractZipFile(tempPath, installPath);

						CreateShortcut($"{gameClient.Name} Studio", $"Launches {gameClient.Name} Studio", installPath + @"\" + gameClient.StudioExecutable, "");

						if (File.Exists(tempPath)) File.Delete(tempPath);
					};
				}
				catch (Exception exc)
				{
					e.Result = exc;
					return;
				};
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				if (e.Result != null)
				{
					DialogResult retry = MessageBox.Show(Error.GetErrorMsg(Error.Installer.ExtractFailed, new Dictionary<string, string>() { { "{INSTALLPATH}", installPath } }), Config.AppEXE, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (retry == DialogResult.Retry)
					{
						InstallWithWorker(name, tempPath, installPath, latestInfo, isForLauncher);
						return;
					}
					else
					{
						if (File.Exists(tempPath)) File.Delete(tempPath);

						Close();
						return;
					}
				}
				if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath);

				this.Invoke(new Action(() => {
					if (isForLauncher) PerformClientCheck();
					else PerformClientStart();
				}));
			};
			worker.RunWorkerAsync();
		}
		#endregion

		#region Form
		private void Cancel(string tempPath)
		{
			status.Text = "Cancelling...";
			progressBar.Style = ProgressBarStyle.Marquee;
			cancelButton.Enabled = false;
			webClient.Dispose();
			File.Delete(tempPath);
			Close();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			webClient?.CancelAsync();
		}
		private void Close()
		{
			this.ParentForm?.Close();
		}
		#endregion
	}
}