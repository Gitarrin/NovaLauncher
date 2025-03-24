using System;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Reflection;

namespace NovaLauncher
{
    public partial class Installer : UserControl
    {
        string defaulterr = "An error occurred!. Error: ";
        WebClient webClient;
        CLIArgs options;

        public Installer()
        {
            InitializeComponent();
        }

        private string GetUserAgent()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return $"NovarinLauncher/{version}";
        }

        private void ExtractZipFile(string archiveFilenameIn, string outFolder)
        {
            using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(archiveFilenameIn)))
            {
                ZipEntry entry;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    string filePath = Path.Combine(outFolder, entry.Name);
                    string directoryName = Path.GetDirectoryName(filePath);

                    if (directoryName.Length > 0 && !Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    if (!entry.IsDirectory)
                    {
                        using (FileStream fileStream = File.Create(filePath))
                        {
                            byte[] buffer = new byte[2048];
                            int size;

                            while ((size = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, size);
                            }
                        }
                    }
                }
            }
        }

        private void ExtractSingleFileFromZip(string archiveFilenameIn, string outFolder, string fileNameToExtract)
        {
            using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(archiveFilenameIn)))
            {
                ZipEntry entry;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entry.Name.Equals(fileNameToExtract, StringComparison.OrdinalIgnoreCase))
                    {
                        string filePath = Path.Combine(outFolder, entry.Name);
                        string directoryName = Path.GetDirectoryName(filePath);

                        if (directoryName.Length > 0 && !Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        if (!entry.IsDirectory)
                        {
                            using (FileStream fileStream = File.Create(filePath))
                            {
                                byte[] buffer = new byte[2048];
                                int size;

                                while ((size = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fileStream.Write(buffer, 0, size);
                                }
                            }
                        }
                        break; // Exit the loop once the file is found and extracted
                    }
                }
            }
        }

        public string GetInstalledVersion()
        {
            try
            {
                string versionJson = File.ReadAllText(Config.installPath + "\\version.json");
                VersionJSON version = JsonConvert.DeserializeObject<VersionJSON>(versionJson);
                return version.Version;
            } catch
            {
                return "";
            }
            
        }

        public bool CheckIfDownloadCorrect(string path, LatestClientInfo data)
        {
            // First check, file size.
            long downloadedSize = new FileInfo(path).Length;
            if (downloadedSize != data.Size)
            {
                Console.Error.WriteLine($"ERROR: Downloaded file size does not match the expected size. Expected {data.Size} != Actual {downloadedSize}");
                return false;
            }

            // Second Check, SHA256 (if enabled)
            if (Config.doSha256Check)
            {
                SHA256 cSha256 = SHA256.Create();
                byte[] checksumBytes;
                using (FileStream stream = File.OpenRead(path))
                {
                    checksumBytes = cSha256.ComputeHash(stream);
                    string checksum = BitConverter.ToString(checksumBytes).Replace("-", "").ToLower();
                    if (checksum != data.Sha256)
                    {
                        Console.Error.WriteLine($"ERROR: SHA256 Checksum does not match. Expected {data.Sha256} != Actual {checksum}");
                        return false;
                    }

                }

            }
            return true;
        }

        public void CreateProtocolOpenKeys(string installPath)
        {
            try
            {
                var classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

                var key = classesKey.CreateSubKey(Config.Protocol);
                key.CreateSubKey("DefaultIcon").SetValue("", installPath + "\\NovaLauncher.exe");
                key.SetValue("", Config.Protocol + ":Protocol");
                key.SetValue("URL Protocol", "");
                key.CreateSubKey(@"shell\open\command").SetValue("", installPath + "\\NovaLauncher.exe --token %1");
                key.Close();
            }
            catch
            {
                MessageBox.Show("Error while configuring " + Config.RevName+" 120-0002", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CreateUninstallKeys(string installPath, LatestClientInfo latestClientInfo)
        {
            // Create uninstall keys to uninstall the client.
            try
            {
                var uninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true);
                var key = uninstallKey.CreateSubKey(Config.RevName);
                key.SetValue("DisplayName", Config.RevName);
                key.SetValue("DisplayIcon", installPath + "\\NovaLauncher.exe");
                key.SetValue("Publisher", "Novarin");

                key.SetValue("DisplayVersion", latestClientInfo.Version);
                int[] versionElements = Array.ConvertAll(latestClientInfo.Version.Split('.'), int.Parse);
                if (!(versionElements.Length < 3))
                {
                    key.SetValue("VersionMajor", versionElements[0]);
                    key.SetValue("VersionMinor", versionElements[1]);
                    key.SetValue("Version", versionElements[2]);
                }

                key.SetValue("AppReadme", "http://novarin.cc/");
                key.SetValue("URLUpdateInfo", "https://novarin.cc/app/downloads");
                key.SetValue("URLInfoAbout", "https://novarin.cc/app/forum/");
                key.SetValue("HelpLink", "https://novarin.cc/app/wiki/");
                key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                key.SetValue("EstimatedSize", (int)CalculateDirectorySize(installPath)/1024, RegistryValueKind.DWord);

                key.SetValue("UninstallString", installPath + "\\NovaLauncher.exe --uninstall");
                key.SetValue("InstallLocation", installPath);
                key.SetValue("NoModify", 1);
                key.SetValue("NoRepair", 1);

                key.Close();
            }
            catch
            {
                MessageBox.Show("Failed to create uninstall keys. 120-0004", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        static long CalculateDirectorySize(string path)
        {
            long size = 0;

            // Get all files in the directory
            foreach (string file in Directory.GetFiles(path))
            {
                FileInfo fileInfo = new FileInfo(file);
                size += fileInfo.Length; // Add file size to total
            }

            // Recursively calculate size of subdirectories
            foreach (string dir in Directory.GetDirectories(path))
            {
                size += CalculateDirectorySize(dir);
            }

            return size;
        }

        public void Cancel(string tempZipArchivePath)
        {
            status.Text = "Cancelling...";
            progressBar.Style = ProgressBarStyle.Marquee;
            cancelButton.Enabled = false;
            //cleanup delete partial file
            webClient.Dispose();
            File.Delete(tempZipArchivePath);
            Close();
        }

        public void CreateRunTempInstaller(string tempZipPath, LatestClientInfo latestClientInfo)
        {
            // This makes a copy of the installer, and runs it.
            // This is for updating the client.
            ExtractSingleFileFromZip(tempZipPath, Path.GetTempPath() + "NovaLauncher\\" + Config.client, "NovaLauncher.exe");

            // Serialize latestClientInfo to JSON and encode it in base64
            string latestClientInfoJson = JsonConvert.SerializeObject(latestClientInfo);
            string latestClientInfoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(latestClientInfoJson));

            // Encode tempZipPath in base64
            string tempZipPathBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tempZipPath));

            // Get the command line arguments
            string[] args = Environment.GetCommandLineArgs();
            string argsString = string.Join(" ", args);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetTempPath() + "NovaLauncher\\" + Config.client + "\\NovaLauncher.exe",
                    Arguments = $"{argsString} --updateinfojsonbase64 {latestClientInfoBase64} --tempzippathbase64 {tempZipPathBase64}",
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = Path.GetTempPath() + "NovaLauncher\\" + Config.client,
                }
            };

            process.Start();
            Close();
            return;
        }

        public bool CheckIfRunningInInstallationDir()
        {
            string currentExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string currentFolderPath = Path.GetDirectoryName(currentExecutablePath);
            if (currentFolderPath != Config.installPath)
            {
                return true;
            }
            return false;
        }


        public void LaunchClient(CLIArgs launchArgs)
        {
            LaunchData launchData = null;

            status.Text = "Starting " + Config.RevName + "...";
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                launchData = JsonConvert.DeserializeObject<LaunchData>(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(launchArgs.Token.Split(':')[1])));
            }
            catch
            {
                MessageBox.Show("Failed to launch" + Config.RevName + "! Error 120-0003.", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
            Process process;
            if (launchData.LaunchType == "host")
            {
                process = new Process
                {
                    StartInfo =
                        {
                            FileName = Config.installPath + "\\NovaHost.exe",
                            Arguments = "-t " + launchData.LaunchToken,
                            WindowStyle = ProcessWindowStyle.Normal,
                            WorkingDirectory = Config.installPath,
                        }
                };
            }
            else
            {
                process = new Process
                {
                    StartInfo =
                        {
                            FileName = Config.installPath + "\\RobloxPlayerBeta.exe",
                            Arguments = "-a \"http://novarin.cc/Login/Negotiate.ashx\" -t \"" + launchData.Ticket + "\"  -j \"" + launchData.JoinScript + "\"",
                            //args[2].Split(':')[1] + "')"
                            WindowStyle = ProcessWindowStyle.Maximized,
                            WorkingDirectory = Config.installPath,
                        }
                };
            }
            process.Start();

            // RPC
            Process rpcProcess = new Process
            {
                StartInfo =
                        {
                            FileName = Config.installPath + "\\NovarinRPCManager.exe",
                            Arguments = $"-j {launchData.JobId} -g {launchData.PlaceId} -l {Config.Protocol} -p {process.Id}",
                            WindowStyle = ProcessWindowStyle.Maximized,
                            WorkingDirectory = Config.installPath,
                        }
            };
            rpcProcess.Start();

            progressBar.Value = 100;
            Close();
            return;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            options = new CLIArgs();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                MessageBox.Show("ya dun goofed up the poor poor arguments :( Error 120-0005", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            // If this passes, that means we are in the middle of an update!
            if (options.TempZipPath != null && options.UpdateInfo != null)
            {
                // Decode update stuff
                string updateInfoJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(options.UpdateInfo));
                string tempZipPath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(options.TempZipPath));

                // Deserialize the update info
                LatestClientInfo latestClientInfo = JsonConvert.DeserializeObject<LatestClientInfo>(updateInfoJson);
                InstallClientWithWorker(tempZipPath, Config.installPath, latestClientInfo);
            }
            else
            {
                // If the token args start with `discordrpc+`, then we are launching a web browser to launch the game.
                if (options.Token != null && options.Token.StartsWith("discordrpc+"))
                {
                    string[] splitToken = options.Token.Split('+');
                    string url = $"https://novarin.cc/discord-redirect-place?id={splitToken[1]}&autojoinJob={splitToken[2]}";
                    Process.Start(url);
                    Close();
                    return;
                }

                status.Text = "Checking version...";

                // Get version.json
                string installedVersion = GetInstalledVersion();

                BackgroundWorker versionWorker = new BackgroundWorker();
                versionWorker.DoWork += (s, ev) =>
                {
                    ev.Result = GetLatestServerVersionInfo();
                };
                
                versionWorker.RunWorkerCompleted += (s, ev) =>
                {
                    LatestClientInfo latestClientInfo = ev.Result as LatestClientInfo;
                    if (latestClientInfo == null)
                    {
                        status.Text = "Error: Can't connect.";
                        MessageBox.Show("We were unable to connect to our servers. Check your internet connection. Error 120-0001", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                        return;
                    }

                    if (installedVersion == "")
                    {
                        UpdateClientWithWorker(latestClientInfo, false);
                    }
                    else if (options.Update)
                    {
                        UpdateClientWithWorker(latestClientInfo, false);
                    }
                    else if (latestClientInfo.Version != installedVersion)
                    {
                        UpdateClientWithWorker(latestClientInfo, true);
                    }
                    else
                    {
                        ContinueClientStart(latestClientInfo);
                    }
                };
                
                versionWorker.RunWorkerAsync();
            }
        }

        private LatestClientInfo GetLatestServerVersionInfo()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", GetUserAgent());
                    string receivedClientData = client.DownloadString(Config.baseUrl + "/" + Config.client);
                    return JsonConvert.DeserializeObject<LatestClientInfo>(receivedClientData);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private void InstallClientWithWorker(string zipPath, string installPath, LatestClientInfo clientInfo)
        {
            status.Text = "Installing " + Config.RevName;
            progressBar.Style = ProgressBarStyle.Marquee;
            cancelButton.Enabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                try
                {
                    if (Directory.Exists(installPath))
                    {
                        Directory.Delete(installPath, true);
                    }



                    ExtractZipFile(zipPath, installPath);
                }
                catch (Exception exc)
                {
                    e.Result = exc;
                    return;
                }

                status.Text = "Configuring " + Config.RevName + "...";

                CreateProtocolOpenKeys(installPath);
                CreateUninstallKeys(installPath, clientInfo);

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

            };

            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Result != null) {
                    DialogResult retry = MessageBox.Show("Error occurred while attempting to extract the client. Error 120-0006" + installPath, Config.RevName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (retry == DialogResult.Retry)
                    {
                        InstallClientWithWorker(zipPath, installPath, clientInfo);
                        return;
                    } else
                    {
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                        Close();
                        return;
                    }
                }
                ContinueClientStart(clientInfo);
            };

            worker.RunWorkerAsync();
        }

        private void UpdateClientWithWorker(LatestClientInfo latestClientInfo, bool upgrade = false)
        {
            if (upgrade)
            {
                status.Text = "Upgrading " + Config.RevName + "...";
            }
            else
            {
                status.Text = "Downloading the latest " + Config.RevName + "...";
            }
            progressBar.Style = ProgressBarStyle.Continuous;
            cancelButton.Enabled = true;

            webClient = new WebClient();
            webClient.Headers.Add("user-agent", GetUserAgent());
            string tempZipArchivePath = Path.GetTempPath() + Config.RevName + ".zip";

            webClient.DownloadProgressChanged += (s, e) =>
            {
                progressBar.Value = e.ProgressPercentage;
            };

            webClient.DownloadFileCompleted += (s, e) =>
            {
                if (e.Cancelled)
                {
                    status.Text = "Cancelling...";
                    Cancel(tempZipArchivePath);
                    return;
                }

                if (e.Error != null)
                {
                    MessageBox.Show("An error occured while trying to download " + Config.RevName + "!\n\nThe installer will now close.\nError 120-0007", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Cancel(tempZipArchivePath);
                    return;
                }

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s1, ev1) =>
                {
                    if (!CheckIfDownloadCorrect(tempZipArchivePath, latestClientInfo))
                    {
                        MessageBox.Show("Downloaded file is corrupted. Please try again. Error 120-0003", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Cancel(tempZipArchivePath);
                        return;
                    }
                };

                worker.RunWorkerCompleted += (s1, ev1) =>
                {
                    if (upgrade)
                    {
                        CreateRunTempInstaller(tempZipArchivePath, latestClientInfo);
                        return;
                    }
                    InstallClientWithWorker(tempZipArchivePath, Config.installPath, latestClientInfo);
                };
                worker.RunWorkerAsync();
            };

            try
            {
                webClient.DownloadFileAsync(new Uri(latestClientInfo.Url), tempZipArchivePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start download! Error 120-0010");
                Cancel(tempZipArchivePath);
            }
        }

        public void ContinueClientStart(LatestClientInfo latestClientInfo)
        {
            if (options.Token == null)
            {
                InstallerGUI.LoadScreen(new InstallCompleted());
                return;
            }

            // oops! corrupted!
            if (!File.Exists(Config.installPath + "\\RobloxPlayerBeta.exe"))
            {
                DialogResult r = MessageBox.Show(Config.RevName + " has run away or something, and we need that. can we reinstall? please?\nError 120-0009", Config.RevName, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (r == DialogResult.OK)
                {
                    UpdateClientWithWorker(latestClientInfo);
                } else
                {
                    Close();
                    return;
                }
            }

            LaunchClient(options);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (webClient != null)
            {
                webClient.CancelAsync();
            }
        }
        private void Close()
        {
            if (this.ParentForm != null)
            {
                this.ParentForm.Close();
            }
        }
    }
}