using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NovaLauncher.Helpers
{
	internal class LauncherForm
	{
		private static Main instance;
		public Installer installer;
		public InstallCompleted installCompleted;
		public Uninstaller uninstaller;

		public LauncherForm(Form f)
		{
			instance = (f as Main);
			installer = new Installer();
			installCompleted = new InstallCompleted();
			uninstaller = new Uninstaller();
		}

		internal static void Close() => Application.Exit();

		internal static void CreateBackgroundTask(DoWorkEventHandler doWorkHandler, RunWorkerCompletedEventHandler finishWorkHandler)
		{
			BackgroundWorker bgWorker = new BackgroundWorker();
			bgWorker.DoWork += doWorkHandler;
			bgWorker.RunWorkerCompleted += finishWorkHandler;
			bgWorker.RunWorkerAsync();
		}

		internal static void UpdateTextWithLog(Control c, string text)
		{
			Action f = new Action(() =>
			{
				c.Text = text;
				Program.logger.Log($"{c.Name}: {text}");
			});
			if (c.InvokeRequired) c.Invoke(f); else f();

		}

		internal static void DoThingsWInvoke(Action f)
		{
			if (instance.InvokeRequired) instance.Invoke(f); else f();
		}

		#region Installer
		internal class Installer
		{
			private LaunchData launchData;
			private WebClient webClient;
			private GameClient gameClient;
			private LatestLauncherInfo latestLauncherInfo;
			private bool LauncherUpgraded = false;

			public void Init()
			{
				instance.actionBtn.Click += (s, e) =>
				{
					webClient?.CancelAsync();
					if (latestLauncherInfo?.Launcher?.Version == null) Close();
				};

				PerformLauncherCheck();
			}

			private void Cancel(string tempPath)
			{
				UpdateTextWithLog(instance.statusLbl, "Cancelling...");
				instance.progressBar.Style = ProgressBarStyle.Marquee;
				instance.actionBtn.Enabled = false;
				webClient.Dispose();
				File.Delete(tempPath);
				Close();
			}

#if NET35
			private bool SwitchToNewNET() {
				try
				{
					RegistryKey NET4 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP\v4\Full");
					if (NET4 != null)
					{
						return (string.Compare((string)NET4.GetValue(@"Version"), "4.8.00000") == 1);
					}
				}
				catch { }
				return false;
			}
#endif

			#region Launcher

			internal void PerformLauncherCheck()
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

				UpdateTextWithLog(instance.statusLbl, $"Connecting to {Config.AppShortName}...");
				string launcherVersion = App.GetInstalledVersion();

				string netFX = App.GetNETVersion()[1];

				CreateBackgroundTask(
					(s, ev) =>
					{
						// Let's determine the best server.
						Program.logger.Log("serverSelector: Finding server...");
						if (!Web.FindBestServer())
						{
							MessageBox.Show(Error.GetErrorMsg(Error.Installer.ConnectFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
							Close();
							return;
						}

#if NET35
					if (SwitchToNewNET()) {
						Program.logger.Log("launcher: Detected .NET Framework 3.5 & .NET Framework 4.8; alerting user of switchover to .NET Framework 4.8 version.");
						MessageBox.Show("Hello! This is the Novarin Launcher.\nWe see you are using the .NET Framework 3.5 Launcher and you have .NET Framework 4.8 installed.\nFor the best experience, we'll now switch to the .NET Framework 4.8 version for you :)", Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
						netFX = "net48";
					};
#endif

						// Now, check the Launcher version.

						ev.Result = Web.GetLatestServerVersionInfo<LatestLauncherInfo>(
							string.Join("", new string[] {
							Config.SelectedServer,
							Config.LauncherSetup,
							$"?f={netFX}",
#if DEBUG
							$"&r={new Random().Next()}"
#endif
						}));
					},
					(s, ev) =>
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
						if (latestLauncherInfo.Alerts.Count > 0)
						{
							Program.logger.Log($"launcher: Sorting through {latestLauncherInfo.Alerts.Count} alerts.");
							foreach (LauncherAlert alert in latestLauncherInfo.Alerts)
							{
								bool alertActive = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds > alert.ActiveUntil;
								bool launcherAffected = alert.VersionsAffected.Count == 0 || alert.VersionsAffected.Contains(launcherVersion);
								if (launcherAffected)
								{
									if (alert.CanContinue)
									{
										UpdateTextWithLog(instance.statusLbl, "Showing alert...");
										MessageBox.Show(alert.Message, Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
									}
									else
									{
										DoThingsWInvoke(new Action(() => {
											instance.progressBar.Visible = false;
											instance.progressLbl.Visible = false;
											UpdateTextWithLog(instance.statusLbl, alert.Message);
											instance.actionBtn.Text = "Close";
											instance.actionBtn.Enabled = true;
											instance.actionBtn.Visible = true;
											instance.actionBtn.Click += (se, e) => Close();
										}));
										return;
									}
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

						if (App.GetNETVersion()[1] != netFX || Program.cliArgs.UpdateLauncher)
						{
							launcherUpdateInfo.IsUpgrade = App.IsRunningFromInstall();
							Update(launcherUpdateInfo);
							return;
						}
						else if (!App.IsRunningFromInstall())
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
					}
				);
				return;
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
						if (!App.IsRunningWine()) CreateProtocolOpenKeys(Config.BaseInstallPath);

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
				UpdateTextWithLog(instance.statusLbl, $"Migrating your install, this may take a moment.");

				CreateBackgroundTask(
					(s, ev) => { Thread.Sleep(1000); },
					(s, ev) =>
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
					}
				);
			}
#endregion

			#region Client
			private void PerformClientCheck()
			{
				CreateBackgroundTask(
					(s, e) =>
					{
						e.Result = false;
						CreateUninstallKeys(Config.BaseInstallPath);
						if (!App.IsRunningWine())
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

						// Launcher is all good.
						if (Program.cliArgs.Token == null)
						{
							// Okay, we weren't launching a client. We'll stop here.
							if (LauncherUpgraded) instance.currentInstance.installCompleted.Init();
							else
							{
								UpdateTextWithLog(instance.statusLbl, $"Opening {Config.AppShortName}...");
								try { Process.Start(latestLauncherInfo.Launcher.Urls.Base); }
								catch { }
								Close();
							}
							return;
						}

						// Since we are launching a client, let's continue.
						UpdateTextWithLog(instance.statusLbl, "Checking version...");
						instance.progressBar.Style = ProgressBarStyle.Marquee;

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

							string installPath = Config.BaseInstallPath + @"\clients\" + serverClientVersion;
							if (serverClient.Status == LauncherClientStatus.REMOVED && Directory.Exists(installPath))
							{
								Program.logger.Log($"clientCheck: {serverClient.Name} marked as REMOVED, purging...");
								try { App.PurgeFilesAndFolders(installPath); Directory.Delete(installPath); } catch { };
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
							InstallPath = $@"{Config.BaseInstallPath}\clients\{launchData.Version}",
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
							gameClient.Version = App.GetInstalledVersion(launchData.Version);
							Program.logger.Log($"clientCheck: gameClient Version is {gameClient.Version}");
						}

						string exeName;
						switch (launchData.LaunchType)
						{
							case "host":
								exeName = gameClient.HostExecutable;
								break;
							case "studio":
							case "build":
								exeName = gameClient.StudioExecutable;
								break;
							default:
								exeName = gameClient.Executable;
								break;
						};
						if (gameClient.Version != null && !File.Exists($@"{gameClient.InstallPath}\{exeName}"))
						{
							MessageBox.Show(Error.GetErrorMsg(Error.Installer.RPBNotFound, new Dictionary<string, string> { { "{EXENAME}", exeName } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
							gameClient.Version = null;
						}

						LatestClientInfo latestClientInfo = client.Status == LauncherClientStatus.PAUSED ? null : Helpers.Web.GetLatestServerVersionInfo<LatestClientInfo>(client.Info);

						if (latestClientInfo == null)
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

						e.Result = true;
						return;
					},
					(s, e) =>
					{
						if ((bool)e.Result)
						{
							Program.logger.Log($"clientCheck: gameClient up to date.");
							PerformClientStart();
						};
						return;
					}
				);
			}

			private void PerformClientStart()
			{
				UpdateTextWithLog(instance.statusLbl, $"Starting {gameClient.Name}...");
				Program.logger.Log($"clientStart: Launch {gameClient.Name} as {launchData.LaunchType}");
				instance.actionBtn.Enabled = false;
				instance.actionBtn.Visible = false;

				CreateBackgroundTask(
					(s, e) =>
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
							instance.Invoke(new Action(() => { instance.progressBar.Value = 50; }));
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
							instance.Invoke(new Action(() => { instance.progressBar.Value = 50; }));
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
							instance.Invoke(new Action(() => { instance.progressBar.Value = 50; }));

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
								if (waited >= stop_waiting && !process.HasExited) process.Kill(); // Attempt to kill it (ain't going anywhere...)
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

								if (helpARobloxOut) App.BringToFront(process.MainWindowTitle);
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
					},
					(s, e) =>
					{
						instance.Invoke(new Action(() => { instance.progressBar.Value = 100; }));
						Close();
						return;
					}
				);
				return;
			}
			#endregion

			#region Download/Install/Upgrade/etc.
			private void Update(UpdateInfo updateInfo)
			{
				CreateBackgroundTask(
					(s, e) =>
					{
						Thread.Sleep(150);
						DoThingsWInvoke(new Action(() =>
						{
							if (!updateInfo.IsLauncher)
							{
								UpdateTextWithLog(instance.statusLbl, $"Waiting for Roblox process(es) to close...");
								if (!App.KillAllBlox())
								{
									Close();
								}
							}

							UpdateTextWithLog(instance.statusLbl, $"{(updateInfo.IsUpgrade ? "Upgrading" : "Downloading the latest")} {updateInfo.Name}...");
							instance.progressBar.Style = ProgressBarStyle.Continuous;
							instance.actionBtn.Enabled = true;

							webClient = new WebClient();
							webClient.Headers.Add("user-agent", Web.GetUserAgent());
							updateInfo.DownloadedPath = Path.GetTempPath() + updateInfo.Name + ".zip";

							Stopwatch downWatch = new Stopwatch();
							webClient.DownloadProgressChanged += (ws, we) =>
							{
								instance.progressLbl.Visible = true;

								if (!downWatch.IsRunning) downWatch.Start();
								int progress = we.ProgressPercentage;

								double bytesRecv = we.BytesReceived;
								double bytesTotal = we.TotalBytesToReceive;
								double secsElp = downWatch.Elapsed.TotalSeconds;
								double speed = secsElp > 0 ? bytesRecv / secsElp : 0;

								double etaSecs = speed > 0 ? (bytesTotal - bytesRecv) / speed : 0;
								TimeSpan eta = TimeSpan.FromSeconds(etaSecs);
								string etaStr = $"{eta.Hours:00}:{eta.Minutes:00}:{eta.Seconds:00}";

								// Update everything!
								instance.progressLbl.Text = Config.Debug
									? $"{progress}% ({Web.FormatBytes(bytesRecv)}/{Web.FormatBytes(bytesTotal)} | {Web.FormatBytes(speed)}/s)  |  ETA: {etaStr}"
									: $"{progress}% ({Web.FormatBytes(speed)}/s)  |  ETA: {etaStr}";
								instance.progressBar.Value = progress;
							};
							webClient.DownloadFileCompleted += (ws, we) =>
							{
								instance.progressLbl.Visible = false;

								if (we.Cancelled)
								{
									Cancel(updateInfo.DownloadedPath);
									return;
								}
								if (we.Error != null)
								{
									Program.logger.Log($"update: Failed to download: {we.Error.Message}\n{we.Error.StackTrace}");
									MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadFailed, new Dictionary<string, string>() { { "{ERROR}", we.Error.Message } }), Config.AppEXE, MessageBoxButtons.OK, MessageBoxIcon.Error);
									Cancel(updateInfo.DownloadedPath);
									return;
								}

								CreateBackgroundTask(
									(se, ev) =>
									{
										int checkCode = App.IsDownloadOK(updateInfo);
										if (checkCode > 0)
										{
											MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadCorrupted, new Dictionary<string, string>() { { "{CHECKSUMCODE}", checkCode.ToString() } }), updateInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
											Cancel(updateInfo.DownloadedPath);
											return;
										}
									},
									(se, ev) =>
									{
										if (updateInfo.IsLauncher)
										{
											if (!updateInfo.IsUpgrade && App.IsRunningWine() && !Program.cliArgs.HideWineMessage)
											{
												Program.logger.Log($"update: Wine message triggered");
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
									}
								);
							};

							try
							{
								webClient.DownloadFileAsync(new Uri(updateInfo.Url), updateInfo.DownloadedPath);
							}
							catch (Exception ex)
							{
								Program.logger.Log($"update: Failed to start download: {ex.Message}\n{ex.StackTrace}");
								MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadStartFail));
								Cancel(updateInfo.DownloadedPath);
							}
						}));
					},
					(s, e) => { }
				);
			}

			private void CreateProtocolOpenKeys(string installPath)
			{
				try
				{
					RegistryKey classesKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

					// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
					Registry.RemoveRegKeys(classesKey, "novarin12");
					Registry.RemoveRegKeys(classesKey, "novarin15");
					Registry.RemoveRegKeys(classesKey, "novarin16");
					// -- END LEGACY KEY REMOVAL --

					Registry.RemoveRegKeys(classesKey, "novarin"); // Delete old one

					string fullPath = $@"{installPath}\{Config.AppEXE}";

					RegistryKey key = classesKey.CreateSubKey(Config.AppProtocol);
					key.CreateSubKey("DefaultIcon").SetValue("", fullPath);
					key.SetValue("", $"URL:{Config.AppProtocol}");
					key.SetValue("URL Protocol", "");
					key.CreateSubKey(@"shell\open\command").SetValue("", $"\"{fullPath}\" --token \"%1\"");
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
					RegistryKey uninstallKey = (App.IsOlderWindows() || App.IsRunningWine()) ? Microsoft.Win32.Registry.LocalMachine : Microsoft.Win32.Registry.CurrentUser;
					uninstallKey = uninstallKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);

					// -- START LEGACY KEY REMOVAL -- WE ARE AIO SO LET'S CLEAN UP OUR ORIGINAL MESS
					Registry.RemoveRegKeys(uninstallKey, "Novarin 2012");
					Registry.RemoveRegKeys(uninstallKey, "Novarin 2015");
					Registry.RemoveRegKeys(uninstallKey, "Novarin 2016");
					// -- END LEGACY KEY REMOVAL --

					RegistryKey key = uninstallKey.CreateSubKey(Config.AppName);
					key.SetValue("DisplayName", Config.AppName);
					key.SetValue("DisplayIcon", installPath + @"\" + Config.AppEXE);
					key.SetValue("Publisher", "Novarin");

					key.SetValue("DisplayVersion", App.GetInstalledVersion());
					int[] versionElements = Array.ConvertAll(App.GetInstalledVersion().Split('.'), int.Parse);
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
					key.SetValue("EstimatedSize", (int)App.CalculateDirectorySize(installPath) / 1024, RegistryValueKind.DWord);

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
				UpdateTextWithLog(instance.statusLbl, $"Installing {updateInfo.Name}...");
				instance.progressBar.Style = ProgressBarStyle.Marquee;
				instance.actionBtn.Enabled = false;

				CreateBackgroundTask(
					(s, e) =>
					{
						Thread.Sleep(500);
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
										ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath, new string[] { Config.AppEXE });
										ZIP.ExtractSingleFileFromZip(updateInfo.DownloadedPath, Path.GetTempPath(), Config.AppEXE);

										// Because we're about to replace the current EXE, we need to restart the launcher.
										updateInfo.IsUpgrade = false; // We already did this step.
										string[] reUpInfo = {
											Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updateInfo))),
											Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(latestLauncherInfo)))
										};
										string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

										string toPath = App.IsRunningFromInstall()
											? (App.IsWindows() ? Process.GetCurrentProcess().MainModule.FileName : Assembly.GetExecutingAssembly().Location)
											: $@"{updateInfo.InstallPath}\{Config.AppEXE}";

										string[] cmds =
										{
											$"ping -n 2 127.0.0.1 >nul", // Give us ~2 seconds to make sure the launcher closes.
											$"move /Y \"{Path.GetTempPath()}\\{Config.AppEXE}\" \"{toPath}\"",
											$"\"{toPath}\" {string.Join(" ", args)} --upinfo {string.Join("_", reUpInfo)}"
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
								};
							}
							else
							{
								if (Directory.Exists(updateInfo.InstallPath)) Directory.Delete(updateInfo.InstallPath, true);
							};

							DoThingsWInvoke(new Action(() =>
							{
								if (Config.Debug) instance.progressLbl.Visible = true;
								ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath, null,
									delegate (string file, string sizeData)
									{
										// parts[0] = currentFile
										// parts[1] = totalFiles
										// parts[2] = compressedSize
										// parts[3] = uncompressedSize
										string[] parts = sizeData.Split('|');

										UpdateTextWithLog(instance.progressLbl, $"Processing ({parts[0]}/{parts[1]}): {file} (c: {Web.FormatBytes(long.Parse(parts[2]))} u: {Web.FormatBytes(long.Parse(parts[3]))})");
									}
								);

								if (Config.Debug) instance.progressLbl.Visible = false;
							}));

							if (File.Exists(updateInfo.DownloadedPath)) File.Delete(updateInfo.DownloadedPath);
						}
						catch (Exception exc)
						{
							e.Result = exc;
						};
					},
					(s, e) =>
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

						UpdateTextWithLog(instance.statusLbl, $"Configuring {updateInfo.Name}...");

						if (!App.IsRunningWine())
						{
							try
							{
								Program.logger.Log($"configure: Creating shortcuts...");
								App.CreateShortcut(Config.AppName, $"{Config.AppShortName} Launcher", updateInfo.InstallPath + @"\" + Config.AppEXE, "");
								if (
									gameClient != null &&
									File.Exists(updateInfo.InstallPath + @"\" + gameClient.StudioExecutable)
								)
									App.CreateShortcut($"{gameClient.Name} Studio", $"Launches {gameClient.Name} Studio", updateInfo.InstallPath + @"\" + gameClient.StudioExecutable, "");
							}
							catch (Exception ex)
							{
								MessageBox.Show(Error.GetErrorMsg(Error.Installer.ProtocolShortcutFailed, new Dictionary<string, string>() { { "{ERROR}", ex.Message }, { "{PROTOSHORT}", "Shortcuts" } }), Config.AppEXE, MessageBoxButtons.OK, MessageBoxIcon.Error);
							}
						};

						instance.Invoke(new Action(() => {
							Program.logger.Log($"install: Completed {(updateInfo.IsUpgrade ? "upgrade" : "install")} of {updateInfo.Name}.");
							if (updateInfo.IsLauncher)
							{
								LauncherUpgraded = true;
#if NET35
							if (Program.cliArgs.UpdateInfo != null && SwitchToNewNET())
							{
								Program.cliArgs.UpdateInfo = null;
								PerformLauncherCheck();
								return;
							}
#endif
								if (CheckMigrate()) MigrateInstall();
								else PerformClientCheck();
							}
							else PerformClientStart();
						}));
					}
				);
				return;
			}
			#endregion


		}
#endregion

		#region Install Completed
		internal class InstallCompleted
		{
			public void Init()
			{

				Action f = () =>
				{
					instance.statusLbl.AutoSize = true;
					instance.statusLbl.Font = new Font("Microsoft YaHei", 16, FontStyle.Regular, GraphicsUnit.Point);
					instance.statusLbl.Text = "NOVARIN IS SUCCESSFULLY INSTALLED!";
					instance.statusLbl.Location = new Point((instance.ClientSize.Width - instance.statusLbl.Width) / 2, instance.statusLbl.Location.Y);
					instance.statusLbl.Visible = true;

					instance.progressLbl.AutoSize = true;
					instance.progressLbl.Font = new Font("Comic Sans MS", 12, FontStyle.Regular, GraphicsUnit.Point);
					instance.progressLbl.Text = "Click the 'Join' button on any game to join the action!";
					instance.progressLbl.Location = new Point((instance.ClientSize.Width - instance.progressLbl.Width) / 2, instance.progressBar.Location.Y);
					instance.progressLbl.Visible = true;

					instance.progressBar.Visible = false;

					instance.actionBtn.BackColor = Color.FromArgb(65, 197, 96);
					instance.actionBtn.ForeColor = Color.WhiteSmoke;
					instance.actionBtn.FlatStyle = FlatStyle.Flat;
					instance.actionBtn.FlatAppearance.BorderSize = 0;
					instance.actionBtn.Text = "OK";
					instance.actionBtn.Enabled = true; 
					instance.actionBtn.Visible = true;
					
				};
				if (instance.InvokeRequired) instance.Invoke(f); else f();
			}
		}
		#endregion

		#region Uninstaller
		internal class Uninstaller
		{
			public void Init()
			{
				UpdateTextWithLog(instance.statusLbl, "Uninstalling...");
				instance.progressBar.Style = ProgressBarStyle.Marquee;

				CreateBackgroundTask(
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
						Close();
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
					if (!Directory.Exists(shortcutPath)) Directory.Delete(shortcutPath);
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
		#endregion
	}
}
