using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Threading;

namespace NovaLauncher
{
	public partial class Installer : UserControl
	{
		LaunchData launchData;
		WebClient webClient;
		GameClient gameClient;
		LatestLauncherInfo latestLauncherInfo;

		public Installer()
		{
			InitializeComponent();
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
				// We were in the middle of an update! Send right along!
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

			// We are no longer using "Novarizz"
			if (Directory.Exists(Config.BaseLegacyInstallPath) && !Directory.Exists(Config.BaseInstallPath))
			{
				MigrateInstall();
				return;
			} else if (Directory.Exists(Config.BaseLegacyInstallPath) && Directory.Exists(Config.BaseInstallPath))
			{
				try { Directory.Delete(Config.BaseLegacyInstallPath); }
				catch (Exception Ex)
				{
					DialogResult dr = MessageBox.Show("Migrate failed.\n" + Ex.Message + "\nAbort to exit. Retry to start migration again. Ignore to continue.", Config.AppName, MessageBoxButtons.AbortRetryIgnore);
					if (dr == DialogResult.Retry)
					{
						MigrateInstall();
						return;
					}
					else if (dr == DialogResult.Abort)
					{
						Close();
						return;
					}
				}
			}

			// Now, we need to check the Launcher version first.
			status.Text = $"Connecting to {Config.AppShortName}...";
			string launcherVersion = Helpers.App.GetInstalledVersion();

			BackgroundWorker versionWorker = new BackgroundWorker();
			versionWorker.DoWork += (s, ev) =>
			{
				ev.Result = Helpers.Web.GetLatestServerVersionInfo<LatestLauncherInfo>(Config.LauncherSetup);
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
				foreach (LauncherAlert alert in latestLauncherInfo.Alerts)
				{
					bool alertActive = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds > alert.ActiveUntil;
					bool launcherAffected = alert.VersionsAffected.Count == 0 || alert.VersionsAffected.Contains(launcherVersion);
					if (launcherAffected)
					{
						if (alert.CanContinue)
						{
							MessageBox.Show(alert.Message, Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
						} else
						{
							progressBar.Visible = false;
							progressDebugLbl.Visible = false;
							status.Text = alert.Message;
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

				if (Program.cliArgs.UpdateLauncher)
				{
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
						launcherUpdateInfo.IsUpgrade = true;
						Update(launcherUpdateInfo);
					}
					else PerformClientCheck();
				}
				catch
				{
					if (launcherUpdateInfo.Version != launcherVersion)
					{
						launcherUpdateInfo.IsUpgrade = true;
						Update(launcherUpdateInfo);
					}
					else PerformClientCheck();
				}

				return;
			};
			versionWorker.RunWorkerAsync();
		}
		private void MigrateInstall()
		{
			status.Text = $"Migrating your install, this may take a moment.";

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

			if (latestLauncherInfo.Clients[launchData.Version] == null)
			{
				MessageBox.Show(Error.GetErrorMsg(launchData.Version == null ? Error.Installer.LaunchClientNoVersion : Error.Installer.LaunchClientFailed, new Dictionary<string, string>() { { "{CLIENT}", launchData.Version } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
				return;
			}

			// Decode server-side client data.
			LauncherClient client = latestLauncherInfo.Clients[launchData.Version];
			if (client.Status == LauncherClientStatus.NO_RELEASE || client.Status == LauncherClientStatus.REMOVED)
			{
				if (client.Status == LauncherClientStatus.REMOVED)
				{
					string installPath = Config.BaseInstallPath + @"\" + launchData.Version;
					try { Helpers.App.PurgeFilesAndFolders(installPath); Directory.Delete(installPath); } catch { };
				};
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

			if (File.Exists(gameClient.InstallPath + @"\version.json"))
			{
				gameClient.Version = Helpers.App.GetInstalledVersion(launchData.Version);
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
				}
				else if (clientUpdateInfo.Version != gameClient.Version)
				{
					clientUpdateInfo.IsUpgrade = true;
					Update(clientUpdateInfo);
				}
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
							Arguments = $"-a \"{gameClient.GameBase}/Login/Negotiate.ashx\" -t \"{launchData.Ticket}\" -j \"{launchData.JoinScript}\"",
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
				try { if (gameClient.Name.Contains("2012")) Helpers.App.BringToFront(process.MainWindowTitle); } catch { };

				// Discord RPC
				Process rpcProcess;
				if (launchData.LaunchType == "client" && File.Exists(Config.BaseInstallPath + @"\NovarinRPCManager.exe"))
				{
					rpcProcess = new Process()
					{
						StartInfo =
						{
							FileName = Config.BaseInstallPath + @"\NovarinRPCManager.exe",
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
		private void Update(UpdateInfo updateInfo)
		{
			status.Text = $"{(updateInfo.IsUpgrade ? "Upgrading" : "Downloading the latest")} {updateInfo.Name}...";
			progressBar.Style = ProgressBarStyle.Continuous;
			cancelButton.Enabled = true;

			webClient = new WebClient();
			webClient.Headers.Add("user-agent", Helpers.Web.GetUserAgent());
			updateInfo.DownloadedPath = Path.GetTempPath() + updateInfo.Name + ".zip";

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
				progressDebugLbl.Text = $"{progress}% ({Helpers.Web.FormatSpeed(speed)}/s)";
				progressDebugLbl.Visible = isShiftDown;
				progressBar.Value = progress;
			};
			webClient.DownloadFileCompleted += (s, e) =>
			{
				progressDebugLbl.Visible = false;

				if (e.Cancelled)
				{
					Cancel(updateInfo.DownloadedPath);
					return;
				}
				if (e.Error != null)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadFailed), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
					}
					Install(updateInfo);

				};
				worker.RunWorkerAsync();
			};
			try
			{
				webClient.DownloadFileAsync(new Uri(updateInfo.Url), updateInfo.DownloadedPath);
			}
			catch
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.DownloadStartFail));
				Cancel(updateInfo.DownloadedPath);
			}
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
			catch
			{
				MessageBox.Show(Error.GetErrorMsg(Error.Installer.CreateUninstallKeys), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Install(UpdateInfo updateInfo)
		{
			status.Text = $"Installing {updateInfo.Name}...";
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
								Helpers.ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath);
							}
						}

						this.Invoke(new Action(() => { status.Text = $"Configuring {updateInfo.Name}..."; }));

						if (!Helpers.App.IsRunningWine()) CreateProtocolOpenKeys(updateInfo.InstallPath);
						Helpers.App.CreateShortcut(Config.AppName, $"{Config.AppShortName} Launcher", updateInfo.InstallPath + @"\" + Config.AppEXE, "");
					}
					else
					{
						if (Directory.Exists(updateInfo.InstallPath)) Directory.Delete(updateInfo.InstallPath, true);

						Helpers.ZIP.ExtractZipFile(updateInfo.DownloadedPath, updateInfo.InstallPath);

						if (File.Exists(updateInfo.InstallPath + @"\" + gameClient.StudioExecutable)) Helpers.App.CreateShortcut($"{gameClient.Name} Studio", $"Launches {gameClient.Name} Studio", updateInfo.InstallPath + @"\" + gameClient.StudioExecutable, "");

						if (File.Exists(updateInfo.DownloadedPath)) File.Delete(updateInfo.DownloadedPath);
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
					DialogResult retry = MessageBox.Show(Error.GetErrorMsg(Error.Installer.ExtractFailed, new Dictionary<string, string>() { { "{INSTALLPATH}", updateInfo.InstallPath } }), Config.AppEXE, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
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

				this.Invoke(new Action(() => {
					if (updateInfo.IsLauncher) PerformClientCheck();
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
			if (latestLauncherInfo?.Launcher?.Version == null) Close();
		}
		private void Close()
		{
			this.ParentForm?.Close();
		}
		#endregion
	}
}