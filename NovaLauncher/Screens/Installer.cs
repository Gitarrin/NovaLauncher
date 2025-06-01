using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NovaLauncher
{
	public partial class Installer : UserControl
	{
		private LaunchData launchData;
		private WebClient webClient;
		private GameClient gameClient;
		private LatestLauncherInfo latestLauncherInfo;
		private bool LauncherUpgraded = false;

		private bool AllowShowingDebugLbl = false;
		private readonly System.Windows.Forms.Timer holdingShiftTimer = new System.Windows.Forms.Timer()
		{
			Interval = 100
		};

		public Installer()
		{
			InitializeComponent();
			holdingShiftTimer.Tick += (s, e) =>
			{
				if (Config.Debug && AllowShowingDebugLbl)
				{
					progressDebugLbl.Visible = (ModifierKeys & Keys.Shift) == Keys.Shift;
				} else {
					progressDebugLbl.Visible = false;
				};

			};
			holdingShiftTimer.Start();
		}

		private void UpdateStatus(string text)
		{
			Action f = () => {
				status.Text = text;
				Program.logger.Log($"statusText: {text}");
			};
			if (this.InvokeRequired) this.Invoke(f); else f();
		}

		private void UpdateDebug(string text)
		{
			Action f = () => {
				progressDebugLbl.Text = text;
				Program.logger.Log($"debugLbl: {text}");
			};
			if (this.InvokeRequired) this.Invoke(f); else f();
		}


		#region Launcher

		private void PerformLauncherCheck()
		{
			latestLauncherInfo = new LatestLauncherInfo();

			// Strange to see launch data here, but its important, so we can see the protocol.
			try
			{
				if (Program.cliArgs.Token != null)
					launchData = JsonConvert.DeserializeObject<LaunchData>(Encoding.UTF8.GetString(Convert.FromBase64String(Program.cliArgs.Token.Split(':')[1])));
			}
			catch { }; // We'll see if this is still uninit'd later.

			if (Program.cliArgs.UpdateInfo != null)
			{
				Program.logger.Log("launcherCheck: We were in the middle of an update! Send right along!");
				try
				{
					string[] recvUpInfo = Program.cliArgs.UpdateInfo.Split('_');
					string updateInfoJson = Encoding.UTF8.GetString(Convert.FromBase64String(recvUpInfo[0]));
					UpdateInfo updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(updateInfoJson);

					string latestLauncherJson = Encoding.UTF8.GetString(Convert.FromBase64String(recvUpInfo[1]));
					latestLauncherInfo = JsonConvert.DeserializeObject<LatestLauncherInfo>(latestLauncherJson);

					Install(updateInfo);
					return;
				}
				catch
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConfigureFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				};
			}

			if (CheckMigrate())
			{
				MigrateInstall();
				return;
			}

			UpdateStatus($"Connecting to {Config.AppShortName}...");
			string launcherVersion = Helpers.App.GetInstalledVersion();

			string netFX = Helpers.App.GetNETVersion()[1];

			BackgroundWorker versionWorker = new BackgroundWorker();
			versionWorker.DoWork += (s, ev) =>
			{
				// Let's determine the best server.
				Program.logger.Log("serverSelector: Finding server...");
				if (!Helpers.Web.FindBestServer())
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

#if NET35
				try
				{
					RegistryKey NET4 = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP\v4\Full");
					if (NET4 != null)
					{
						if (string.Compare((string)NET4.GetValue(@"Version"), "4.8.00000") == 1)
						{
							Program.logger.Log("launcher: Detected .NET Framework 3.5 & .NET Framework 4.8; alerting user of switchover to .NET Framework 4.8 version.");
							MessageBox.Show("Hello! This is the Novarin Launcher.\nWe see you are using the .NET Framework 3.5 Launcher and you have .NET Framework 4.8 installed.\nFor the best experience, we'll now switch to the .NET Framework 4.8 version for you :)", Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
							netFX = "net48";
						}
					}
				} catch { }
#endif

				// Now, check the Launcher version.
				ev.Result = Helpers.Web.GetLatestServerVersionInfo<LatestLauncherInfo>($"{Config.SelectedServer}{Config.LauncherSetup}?f={netFX}");
			};
			versionWorker.RunWorkerCompleted += (s, ev) =>
			{
				if (!(ev.Result is LatestLauncherInfo))
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}
				latestLauncherInfo = ev.Result as LatestLauncherInfo;

				if (File.Exists(Config.BaseInstallPath + @"\" + Config.AppEXE))
				{
					FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Config.BaseInstallPath + @"\" + Config.AppEXE);
					launcherVersion = fvi.ProductVersion ?? fvi.FileVersion;
				};

				// Alerts stuff first
				Program.logger.Log($"launcher: Sorting through {latestLauncherInfo.Alerts.Count} alerts.");
				foreach (LauncherAlert alert in latestLauncherInfo.Alerts)
				{
					bool alertActive = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds > alert.ActiveUntil;
					bool launcherAffected = alert.VersionsAffected.Count == 0 || alert.VersionsAffected.Contains(launcherVersion);
					if (launcherAffected)
					{
						if (alert.CanContinue)
						{
							UpdateStatus($"Showing alert...");
							MessageBox.Show(alert.Message, Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
						} else
						{
							progressBar.Visible = false;
							AllowShowingDebugLbl = false;
							UpdateStatus(alert.Message);
							cancelButton.Text = "Close";
							cancelButton.Enabled = true;
							cancelButton.Visible = true;
							cancelButton.Click += (se, e) => Close();
							return;
						}
					}
				}

				UpdateInfo launcherUpdateInfo = new UpdateInfo
				{
					Name = Config.AppName,
					Version = latestLauncherInfo.Launcher.Version,
					Url = latestLauncherInfo.Launcher.Url,
					Size = latestLauncherInfo.Launcher.Size,
					IsUpgrade = false,
					IsLauncher = true,
					Sha256 = latestLauncherInfo.Launcher.Sha256,
					DoSHACheck = true,
					DownloadedPath = "",
					InstallPath = Config.BaseInstallPath,
				};

				if (Helpers.App.GetNETVersion()[1] != netFX || Program.cliArgs.UpdateLauncher)
				{
					launcherUpdateInfo.IsUpgrade = Helpers.App.IsRunningFromInstall();
					Update(launcherUpdateInfo);
					return;
				}
				else if (!Helpers.App.IsRunningFromInstall())
				{
					if (!File.Exists(Config.BaseInstallPath + @"\" + Config.AppEXE))
					{
						Update(launcherUpdateInfo);
						return;
					}
				}

				try
				{
					int launchUp = int.Parse(launcherUpdateInfo.Version.Replace(".", ""));
					int launchCurr = int.Parse(launcherVersion.Replace(".", ""));

					if (launchCurr < launchUp)
					{
						Program.logger.Log($"launcherCheck: Launcher update required: c: {launchCurr} s: {launchUp}");
						launcherUpdateInfo.IsUpgrade = true;
						Update(launcherUpdateInfo);
						return;
					}
				}
				catch
				{
					if (launcherVersion != launcherUpdateInfo.Version)
					{
						Program.logger.Log($"launcherCheck: Launcher update required: c: {launcherVersion} s: {launcherUpdateInfo.Version}");
						launcherUpdateInfo.IsUpgrade = true;
						Update(launcherUpdateInfo);
						return;
					}
				}

				Program.logger.Log($"launcherCheck: Launcher up to date.");
				PerformClientCheck();
				return;
			};
			versionWorker.RunWorkerAsync();
		}
		private bool CheckMigrate()
		{
			// We are no longer using "Novarizz"
			if (Directory.Exists(Config.BaseLegacyInstallPath) && !Directory.Exists(Config.BaseInstallPath))
			{
				Program.logger.Log($"migrate: Legacy exists but Base doesn't.");
				return true;
			}
			else if (Directory.Exists(Config.BaseLegacyInstallPath) && Directory.Exists(Config.BaseInstallPath))
			{
				Program.logger.Log($"migrate: Finish up.");
				try
				{
					Directory.Delete(Config.BaseLegacyInstallPath);

					// Update shortcuts & protocols. (Uninstall keys created later on)
					if (!Helpers.App.IsRunningWine()) CreateProtocolOpenKeys(Config.BaseInstallPath);

					string shortcutPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\{Config.AppShortName}";
					if (Directory.Exists(shortcutPath))
					{
						foreach (string file in Directory.GetFiles(shortcutPath))
						{
							Shortcut sc = Shortcut.CreateInstance(file);
							sc.TargetPath = sc.TargetPath.Replace(Config.BaseLegacyInstallPath, Config.BaseInstallPath);
							Shortcut.SaveShortcut(sc);
						}
					}
				}
				catch (Exception Ex)
				{
					Program.logger.Log($"migrate: Failed: {Ex.Message}.");
					DialogResult dr = MessageBox.Show("Migrate failed.\n" + Ex.Message + "\nAbort to exit. Retry to start migration again. Ignore to continue.", Config.AppName, MessageBoxButtons.AbortRetryIgnore);
					if (dr == DialogResult.Retry) return true;
					else if (dr == DialogResult.Abort)
					{
						Close();
						return false;
					}
				}
			}
			return false;
		}
		private void MigrateInstall()
		{
			UpdateStatus($"Migrating your install, this may take a moment.");

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				Thread.Sleep(1000);
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				if (Directory.Exists(Config.BaseInstallPath)) Directory.Delete(Config.BaseInstallPath, true);

				// Do the migration
				string[] args = Environment.GetCommandLineArgs();
				args[0] = Config.BaseInstallPath + @"\" + Config.AppEXE;
				string[] cmds =
				{
					$"ping -n 2 127.0.0.1 >nul", // Give us ~2 seconds to make sure the launcher closes.
					$"ren \"{Config.BaseLegacyInstallPath}\" \"{Config.BaseInstallPath.Replace(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\", "")}\"",
					$"mkdir \"{Config.BaseLegacyInstallPath}\"", // so we can trigger second stage
					$"ping -n 2 127.0.0.1 >nul",
					$"{string.Join(" ", args)}"
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
			};
			worker.RunWorkerAsync();
		}
		#endregion

		#region Client
		private void PerformClientCheck()
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				UpdateStatus($"Configuring {Config.AppName}...");
				CreateUninstallKeys(Config.BaseInstallPath);
				if (!Helpers.App.IsRunningWine())
				{
					try
					{
						CreateProtocolOpenKeys(Config.BaseInstallPath);
					}
					catch (Exception ex)
					{
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.ProtocolShortcutFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message }, { "{PROTOSHORT}", "Protocol Keys" } }), Config.AppEXE, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					}
				};
			};
			worker.RunWorkerCompleted += (s1, e1) =>
			{
				// Launcher is all good.
				if (Program.cliArgs.Token == null)
				{
					// Okay, we weren't launching a client. We'll stop here.
					if (Helpers.App.IsRunningFromInstall() && LauncherUpgraded) Main.LoadScreen(new Screens.InstallCompleted());
					else
					{
						UpdateStatus($"Opening {Config.AppShortName}...");
						try { Process.Start(latestLauncherInfo.Launcher.Urls.Base); }
						catch { }
						Close();
					}
					return;
				}

				// Since we are launching a client, let's continue.
				UpdateStatus("Checking version...");
				progressBar.Style = ProgressBarStyle.Marquee;

				if (launchData.Version == null)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientNoVersion), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				if (latestLauncherInfo.Clients[launchData.Version] == null)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientFailed, new Dictionary<string, string>() { { "{CLIENT}", launchData.Version } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				// Decode server-side client data.
				foreach (KeyValuePair<string, LauncherClient> kvp in latestLauncherInfo.Clients)
				{
					string serverClientVersion = kvp.Key;
					LauncherClient serverClient = kvp.Value;

					string installPath = Config.BaseInstallPath + @"\" + serverClientVersion;
					if (serverClient.Status == LauncherClientStatus.REMOVED && Directory.Exists(installPath))
					{
						Program.logger.Log($"clientCheck: {serverClient.Name} marked as REMOVED, purging...");
						try { Helpers.App.PurgeFilesAndFolders(installPath); Directory.Delete(installPath); } catch { };
					};
				}

				LauncherClient client = latestLauncherInfo.Clients[launchData.Version];
				if (client.Status == LauncherClientStatus.NO_RELEASE || client.Status == LauncherClientStatus.REMOVED)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientNotAvailable, new Dictionary<string, string>() { { "{CLIENT}", client.Name } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Close();
					return;
				}

				// Setting up client-side client. Heh, get it..? OK, I'm not funny.
				gameClient = new GameClient
				{
					Name = client.Name,
					InstallPath = Config.BaseInstallPath + @"\" + launchData.Version,
					Executable = client.Executables.Player,
					StudioExecutable = client.Executables.Studio,
					HostExecutable = client.Executables.Host,
					ClientCheck = client.Checksum,
					Version = null,
					GameBase = latestLauncherInfo.Launcher.Urls.Base
				};
				Program.logger.Log($"clientCheck: Looks like we're launching {gameClient.Name}");

				if (File.Exists(gameClient.InstallPath + @"\version.json"))
				{
					gameClient.Version = Helpers.App.GetInstalledVersion(launchData.Version);
					Program.logger.Log($"clientCheck: gameClient Version is {gameClient.Version}");
				}

				BackgroundWorker versionWorker = new BackgroundWorker();
				versionWorker.DoWork += (s, ev) =>
				{
					ev.Result = client.Status == LauncherClientStatus.PAUSED ? null : Helpers.Web.GetLatestServerVersionInfo<LatestClientInfo>(client.Info);
				};
				versionWorker.RunWorkerCompleted += (s, ev) =>
				{
					if (!(ev.Result is LatestClientInfo))
					{
						if (!Program.cliArgs.UpdateClient && gameClient.Version != null && client.Status == LauncherClientStatus.PAUSED) PerformClientStart();
						else if (client.Status == LauncherClientStatus.PAUSED)
						{
							MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientNotAvailable, new Dictionary<string, string>() { { "{CLIENT}", client.Name } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
							Close();
						}
						else
						{
							MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), client.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
							Close();
						}
						return;
					}
					LatestClientInfo latestClientInfo = ev.Result as LatestClientInfo;

					UpdateInfo clientUpdateInfo = new UpdateInfo
					{
						Name = gameClient.Name,
						Version = latestClientInfo.Version,
						Url = latestClientInfo.Url,
						Size = latestClientInfo.Size,
						IsUpgrade = false,
						IsLauncher = false,
						Sha256 = latestClientInfo.Sha256,
						DoSHACheck = gameClient.ClientCheck,
						DownloadedPath = "",
						InstallPath = gameClient.InstallPath,
					};

					if (Program.cliArgs.UpdateClient || gameClient.Version == null)
					{
						Update(clientUpdateInfo);
						return;
					}
					else if (clientUpdateInfo.Version != gameClient.Version)
					{
						Program.logger.Log($"clientCheck: gameClient update required: c: {gameClient.Version} s: {clientUpdateInfo.Version}");
						clientUpdateInfo.IsUpgrade = true;
						Update(clientUpdateInfo);
						return;
					}

					Program.logger.Log($"clientCheck: gameClient up to date.");
					PerformClientStart();
					return;
				};
				versionWorker.RunWorkerAsync();
			};
			worker.RunWorkerAsync();
		}

		private void PerformClientStart()
		{
			UpdateStatus($"Starting {gameClient.Name}...");
			Program.logger.Log($"clientStart: Launch {gameClient.Name} as {launchData.LaunchType}");
			cancelButton.Enabled = false;
			cancelButton.Visible = false;

			BackgroundWorker worker = new BackgroundWorker();

			worker.DoWork += (s, e) =>
			{
				Process process;
				bool useRPC = false;
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

					process.Start();
					this.Invoke(new Action(() => { progressBar.Value = 50; }));
				}
				else if (launchData.LaunchType == "studio" || launchData.LaunchType == "build")
				{
					process = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\" + gameClient.StudioExecutable,
							Arguments = $"-url \"{gameClient.GameBase}/Login/Negotiate.ashx\" -ticket \"{launchData.Ticket}\" -{(launchData.LaunchType == "build" ? "build" : "ide")} -script \"{launchData.JoinScript}\"",
							WindowStyle = ProcessWindowStyle.Normal,
							WorkingDirectory = gameClient.InstallPath
						}
					};

					process.Start();
					this.Invoke(new Action(() => { progressBar.Value = 50; }));
				}
				else
				{
					useRPC = true;

					process = new Process()
					{
						StartInfo =
						{
							FileName = gameClient.InstallPath + @"\" + gameClient.Executable,
							Arguments = $"-a \"{gameClient.GameBase}/Login/Negotiate.ashx\" -t \"{launchData.Ticket}\" -j \"{launchData.JoinScript}\"",
							WindowStyle = ProcessWindowStyle.Maximized,
							WorkingDirectory = gameClient.InstallPath
						}
					};

					process.Start();
					this.Invoke(new Action(() => { progressBar.Value = 50; }));

					int waited = 0;
					int stop_waiting = 60000;
					while (true)
					{
						if (!string.IsNullOrEmpty(process.MainWindowTitle)) break; // The game actually launched!
						if (waited >= stop_waiting) break; // Timeout
						if (process.HasExited) break; // Process died for some reason
						Thread.Sleep(1000);
						process.Refresh();
						waited += 1000;
					}
					if (waited >= stop_waiting || process.HasExited)
					{
						Program.logger.Log($"clientStart: Failed to start because: {(waited >= stop_waiting ? "timeout" : "process exited")}");
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.LaunchClientTimeout, new Dictionary<string, string>() { { "{CLIENT}", gameClient.Name } }), gameClient.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
						Close();
						return;
					}

					try
					{
						// Some old Robloxs' needs a little help sometimes.

						int[] bringYears = { 2008, 2009, 2010, 2011, 2012, 2013, 2014 };
						bool helpARobloxOut = false;

						foreach (int year in bringYears)
						{
							if (gameClient.Name.Contains(year.ToString()))
							{
								helpARobloxOut = true;
								break;
							}
						}

						if (helpARobloxOut) Helpers.App.BringToFront(process.MainWindowTitle);
					}
					catch { };
				};

				// Discord RPC
				Process rpcProcess;
				if (useRPC && File.Exists(Config.BaseInstallPath + @"\NovarinRPCManager.exe"))
				{
					Program.logger.Log("clientStart: Starting RPC Manager...");
					rpcProcess = new Process()
					{
						StartInfo =
							{
								FileName = Config.BaseInstallPath + @"\NovarinRPCManager.exe",
								Arguments = $"-j {launchData.JobId} -g {launchData.PlaceId} -l {Config.AppProtocol} -p {process.Id}",
								WindowStyle = ProcessWindowStyle.Hidden,
								WorkingDirectory = Config.BaseInstallPath
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
		private void Update(UpdateInfo updateInfo)
		{
			BackgroundWorker bloxWorker = new BackgroundWorker();

			bloxWorker.DoWork += (s, e) =>
			{
				if (!updateInfo.IsLauncher)
				{
					UpdateStatus($"Waiting for Roblox process(es) to close...");
					if (!Helpers.App.KillAllBlox())
					{
						Close();
					}
				}
			};
			bloxWorker.RunWorkerCompleted += (bs, be) =>
			{
				UpdateStatus($"{(updateInfo.IsUpgrade ? "Upgrading" : "Downloading the latest")} {updateInfo.Name}...");
				progressBar.Style = ProgressBarStyle.Continuous;
				cancelButton.Enabled = true;

				webClient = new WebClient();
				webClient.Headers.Add("user-agent", Helpers.Web.GetUserAgent());
				updateInfo.DownloadedPath = Path.GetTempPath() + updateInfo.Name + ".zip";

				Stopwatch downWatch = new Stopwatch();
				webClient.DownloadProgressChanged += (s, e) =>
				{
					AllowShowingDebugLbl = true;

					if (!downWatch.IsRunning) downWatch.Start();
					int progress = e.ProgressPercentage;

					double bytesRecv = e.BytesReceived;
					double bytesTotal = e.TotalBytesToReceive;
					double secsElp = downWatch.Elapsed.TotalSeconds;
					double speed = secsElp > 0 ? bytesRecv / secsElp : 0;

					double etaSecs = speed > 0 ? (bytesTotal - bytesRecv) / speed : 0;
					TimeSpan eta = TimeSpan.FromSeconds(etaSecs);
					string etaStr = $"{eta.Hours:00}:{eta.Minutes:00}:{eta.Seconds:00}";

					// Update everything!
					progressDebugLbl.Text = $"{progress}% ({Helpers.Web.FormatBytes(bytesRecv)}/{Helpers.Web.FormatBytes(bytesTotal)} | {Helpers.Web.FormatBytes(speed)}/s)  |  ETA: {etaStr}";
					progressBar.Value = progress;
				};
				webClient.DownloadFileCompleted += (s, e) =>
				{
					AllowShowingDebugLbl = false;

					if (e.Cancelled)
					{
						Cancel(updateInfo.DownloadedPath);
						return;
					}
					if (e.Error != null)
					{
						Program.logger.Log($"update: Failed to download: {e.Error.Message}\n{e.Error.StackTrace}");
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadFailed, new Dictionary<string, string>() { { "{ERROR}", e.Error.Message} }), Config.AppEXE, MessageBoxButtons.OK, MessageBoxIcon.Error);
						Cancel(updateInfo.DownloadedPath);
						return;
					}
					 
					BackgroundWorker worker = new BackgroundWorker();
					worker.DoWork += (s1, ev1) =>
					{
						int checkCode = Helpers.App.IsDownloadOK(updateInfo);
						if (checkCode > 0)
						{
							MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadCorrupted, new Dictionary<string, string>() { { "{CHECKSUMCODE}", checkCode.ToString() } }), updateInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
							Cancel(updateInfo.DownloadedPath);
							return;
						}
					};
					worker.RunWorkerCompleted += (s1, ev1) =>
					{
						if (updateInfo.IsLauncher)
						{
							if (!updateInfo.IsUpgrade && Helpers.App.IsRunningWine() && !Program.cliArgs.HideWineMessage)
							{
								Program.logger.Log($"Wine message triggered");
								string[] wineMessage = new string[]
								{
									"We have detected that you are installing Novarin via Wine. We will attempt to make your experience as smooth as possible (like RPC being native probably), but some configuration is required.",
									"",
									"To get Novarin working via Wine, here's what you need to do:",
									$" 1. Create a .desktop file that handles the protocol '{Config.AppProtocol}://token123', which calls {Config.AppEXE} (found in Appdata/Local/{Config.AppShortName}) like '{Config.AppEXE.Replace(".exe", "")} --token token123' with token123 being whatever is passed thru to the protocol.",
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
						}
						Install(updateInfo);

					};
					worker.RunWorkerAsync();
				};
				try
				{
					webClient.DownloadFileAsync(new Uri(updateInfo.Url), updateInfo.DownloadedPath);
				}
				catch (Exception e)
				{
					Program.logger.Log($"update: Failed to start download: {e.Message}\n{e.StackTrace}");
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadStartFail));
					Cancel(updateInfo.DownloadedPath);
				}

			};
			bloxWorker.RunWorkerAsync();
		}

		private void CreateProtocolOpenKeys(string installPath)
		{
			try
			{
				RegistryKey classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

				// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
				Helpers.Registry.RemoveRegKeys(classesKey, "novarin12");
				Helpers.Registry.RemoveRegKeys(classesKey, "novarin15");
				Helpers.Registry.RemoveRegKeys(classesKey, "novarin16");
				// -- END LEGACY KEY REMOVAL --

				Helpers.Registry.RemoveRegKeys(classesKey, "novarin"); // Delete old one

				RegistryKey key = classesKey.CreateSubKey(Config.AppProtocol);
				key.CreateSubKey("DefaultIcon").SetValue("", installPath + @"\" + Config.AppEXE);
				key.SetValue("", Config.AppProtocol + ":Protocol");
				key.SetValue("URL Protocol", "");
				key.CreateSubKey(@"shell\open\command").SetValue("", installPath + @"\" + Config.AppEXE + " --token %1");
				key.Close();
			}
			catch (Exception Ex)
			{
				Program.logger.Log($"Failed creating protocol open keys: {Ex.Message}");
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConfigureFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CreateUninstallKeys(string installPath)
		{
			try
			{
				RegistryKey uninstallKey = (Helpers.App.IsOlderWindows() || Helpers.App.IsRunningWine()) ? Registry.LocalMachine : Registry.CurrentUser;
				uninstallKey = uninstallKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);

				// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
				Helpers.Registry.RemoveRegKeys(uninstallKey, "Novarin 2012");
				Helpers.Registry.RemoveRegKeys(uninstallKey, "Novarin 2015");
				Helpers.Registry.RemoveRegKeys(uninstallKey, "Novarin 2016");
				// -- END LEGACY KEY REMOVAL --

				RegistryKey key = uninstallKey.CreateSubKey(Config.AppName);
				key.SetValue("DisplayName", Config.AppName);
				key.SetValue("DisplayIcon", installPath + @"\" + Config.AppEXE);
				key.SetValue("Publisher", "Novarin");

				key.SetValue("DisplayVersion", Helpers.App.GetInstalledVersion());
				int[] versionElements = Array.ConvertAll(Helpers.App.GetInstalledVersion().Split('.'), int.Parse);
				if (!(versionElements.Length < 3))
				{
					key.SetValue("VersionMajor", versionElements[0]);
					key.SetValue("VersionMinor", versionElements[1]);
					key.SetValue("Version", versionElements[2]);
				}

				key.SetValue("AppReadme", latestLauncherInfo.Launcher.Urls.Base);
				key.SetValue("URLUpdateInfo", latestLauncherInfo.Launcher.Urls.UpdateInfo);
				key.SetValue("URLInfoAbout", latestLauncherInfo.Launcher.Urls.AboutInfo);
				key.SetValue("HelpLink", latestLauncherInfo.Launcher.Urls.HelpInfo);
				if (key.GetValue("InstallDate") == null) key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
				key.SetValue("EstimatedSize", (int)Helpers.App.CalculateDirectorySize(installPath) / 1024, RegistryValueKind.DWord);

				key.SetValue("UninstallString", installPath + @"\" + Config.AppEXE + " --uninstall");
				key.SetValue("InstallLocation", installPath);
				key.SetValue("NoModify", 1);
				key.SetValue("NoRepair", 1);

				key.Close();
			}
			catch (Exception Ex)
			{
				Program.logger.Log($"Failed creating uninstall keys: {Ex.Message}");
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.CreateUninstallKeys), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Install(UpdateInfo updateInfo)
		{
			UpdateStatus($"Installing {updateInfo.Name}...");
			progressBar.Style = ProgressBarStyle.Marquee;
			cancelButton.Enabled = false;

			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				try
				{
					if (updateInfo.IsLauncher)
					{
						if (Program.cliArgs.UpdateInfo == null)
						{
							// Cleanup files in the Launcher directory (or create if not exist) & Extract the Launcher stuff
							if (Directory.Exists(updateInfo.InstallPath))
							{
								string[] files = Directory.GetFiles(updateInfo.InstallPath);
								foreach (string file in files)
								{
									if (file == updateInfo.InstallPath + @"\" + Config.AppEXE) continue;
									File.Delete(file);
								}
							}
							else Directory.CreateDirectory(updateInfo.InstallPath);

							if (updateInfo.IsUpgrade)
							{
								Helpers.ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath, new string[] { Config.AppEXE });
								Helpers.ZIP.ExtractSingleFileFromZip(updateInfo.DownloadedPath, Path.GetTempPath(), Config.AppEXE);

								// Because we're about to replace the current EXE, we need to restart the launcher.
								updateInfo.IsUpgrade = false; // We already did this step.
								string[] reUpInfo = {
									Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updateInfo))),
									Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(latestLauncherInfo)))
								};
								string[] args = Environment.GetCommandLineArgs();
								string[] cmds =
								{
									$"ping -n 2 127.0.0.1 >nul", // Give us ~2 seconds to make sure the launcher closes.
									$"move /Y \"{Path.GetTempPath()}\\{Config.AppEXE}\" \"{updateInfo.InstallPath}\\{Config.AppEXE}\"",
									$"{string.Join(" ", args)} --upinfo {string.Join("_", reUpInfo)}"
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
							else
							{
								AllowShowingDebugLbl = true;
								Helpers.ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath, null,
									delegate (string file, string sizeData) {
										// parts[0] = currentFile
										// parts[1] = totalFiles
										// parts[2] = compressedSize
										// parts[3] = uncompressedSize
										string[] parts = sizeData.Split('|');

										UpdateDebug($"Processing ({parts[0]}/{parts[1]}): {file} (c: {Helpers.Web.FormatBytes(long.Parse(parts[2]))} u: {Helpers.Web.FormatBytes(long.Parse(parts[3]))})");
									}
								);
								AllowShowingDebugLbl = false;
							}
						};
					}
					else
					{
						if (Directory.Exists(updateInfo.InstallPath)) Directory.Delete(updateInfo.InstallPath, true);

						AllowShowingDebugLbl = true;
						Helpers.ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath, null,
							delegate (string file, string sizeData) {
								// parts[0] = currentFile
								// parts[1] = totalFiles
								// parts[2] = compressedSize
								// parts[3] = uncompressedSize
								string[] parts = sizeData.Split('|');

								UpdateDebug($"Processing ({parts[0]}/{parts[1]}): {file} (c: {Helpers.Web.FormatBytes(long.Parse(parts[2]))} u: {Helpers.Web.FormatBytes(long.Parse(parts[3]))})");
							}
						);
						AllowShowingDebugLbl = false;

						if (File.Exists(updateInfo.DownloadedPath)) File.Delete(updateInfo.DownloadedPath);
					};
				}
				catch (Exception exc)
				{
					e.Result = exc;
				};
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				Exception exc = e.Result is Exception ? (Exception)e.Result : e.Error;
				if (exc != null)
				{
					Program.logger.Log($"install: Failed to extract: {exc.Message}{(exc.StackTrace != null ? "\n" + exc.StackTrace : "")}");
					DialogResult retry = MessageBox.Show(Error.GetErrorMsg(Error.Installer.ExtractFailed, new Dictionary<string, string>() { { "{ERROR}", exc.Message }, { "{INSTALLPATH}", updateInfo.InstallPath } }), Config.AppEXE, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (retry == DialogResult.Retry)
					{
						Install(updateInfo);
						return;
					}
					else
					{
						if (File.Exists(updateInfo.DownloadedPath)) File.Delete(updateInfo.DownloadedPath);

						Close();
						return;
					}
				}
				if (updateInfo.DownloadedPath != null && File.Exists(updateInfo.DownloadedPath)) File.Delete(updateInfo.DownloadedPath);

				UpdateStatus($"Configuring {updateInfo.Name}...");

				if (!Helpers.App.IsRunningWine())
				{
					try
					{
						Helpers.App.CreateShortcut(Config.AppName, $"{Config.AppShortName} Launcher", updateInfo.InstallPath + @"\" + Config.AppEXE, "");
						if (File.Exists(updateInfo.InstallPath + @"\" + gameClient.StudioExecutable)) Helpers.App.CreateShortcut($"{gameClient.Name} Studio", $"Launches {gameClient.Name} Studio", updateInfo.InstallPath + @"\" + gameClient.StudioExecutable, "");
					}
					catch (Exception ex)
					{
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.ProtocolShortcutFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message }, { "{PROTOSHORT}", "Shortcuts" } }), Config.AppEXE, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					}
				};

				this.Invoke(new Action(() => {
					if (updateInfo.IsLauncher)
					{
						LauncherUpgraded = true;
						if (CheckMigrate()) MigrateInstall();
						else PerformClientCheck();
					}
					else PerformClientStart();
				}));
			};
			worker.RunWorkerAsync();
		}
		#endregion

		#region Form
		private void Cancel(string tempPath)
		{
			UpdateStatus("Cancelling...");
			progressBar.Style = ProgressBarStyle.Marquee;
			cancelButton.Enabled = false;
			webClient.Dispose();
			File.Delete(tempPath);
			Close();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			webClient?.CancelAsync();
			if (latestLauncherInfo?.Launcher?.Version == null) Close();
		}
		private void Close()
		{
			this.Invoke(new Action(() => this.ParentForm?.Close()));
		}
		#endregion
	}
}
