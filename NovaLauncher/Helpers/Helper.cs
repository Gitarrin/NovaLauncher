using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NovaLauncher.Helpers
{
	#region Registry
	public static class Registry
	{
		public static void RemoveRegKeys(RegistryKey location, string path)
		{
			RegistryKey key = location.OpenSubKey(path, true);
			if (key != null)
			{
				key.Close();
				location.DeleteSubKeyTree(path);
			}
		}
	}
	#endregion

	#region Web
	public static class Web
	{
		public static string FormatBytes(double bytesPerSecond)
		{
			string[] sizes = { "B", "KB", "MB", "GB" };
			int order = 0;

			while (bytesPerSecond >= 1024 && order < sizes.Length - 1)
			{
				order++;
				bytesPerSecond /= 1024;
			}

			return $"{bytesPerSecond:0.00} {sizes[order]}";
		}

		public static string GetUserAgent()
		{
			var version = App.GetInstalledVersion();
			return $"NovarinLauncher/{version}";
		}

		public static bool FindBestServer()
		{
			for (int serverIndex = 1; serverIndex <= Config.Servers.Length; serverIndex++)
			{
				string server = Config.Servers[serverIndex - 1];
				Program.logger.Log($"serverSelector: Try {serverIndex}/{Config.Servers.Length}");
				try
				{
					HttpWebRequest client = (HttpWebRequest)WebRequest.Create(server);
					client.UserAgent = GetUserAgent();
					client.Method = "GET";
					client.AllowAutoRedirect = false;

					using (HttpWebResponse response = (HttpWebResponse)client.GetResponse())
					{
						string recvData = new StreamReader(response.GetResponseStream()).ReadToEnd();
						if (
							response.StatusCode == HttpStatusCode.OK &&
							recvData != null &&
							!recvData.Contains("Server Down") &&
							!recvData.Contains("Error")
						)
						{
							Program.logger.Log($"serverSelector: {serverIndex} selected!");
							Config.SelectedServer = server;
							return true;
						};
						Program.logger.Log($"serverSelector: {serverIndex} will not select: got string {recvData}");
					}

					client = null;
				}
				catch (Exception Ex)
				{
					Program.logger.Log($"serverSelector: {serverIndex} will not select: got error {Ex.Message}");
				}
			}

			// Couldn't find one :(, assume blocked/DNS issue/all servers down/etc.
			Program.logger.Log($"serverSelector: Out of servers to select. Bailing...!");
			return false;
		}

		public static T GetLatestServerVersionInfo<T>(string location)
		{
			try
			{
				using (WebClient client = new WebClient())
				{
					client.Headers.Add("user-agent", GetUserAgent());
					string recvData = client.DownloadString(location);
					return JsonConvert.DeserializeObject<T>(recvData);
				}
			}
			catch { return default; }
		}
	}
	#endregion

	#region ZIP
	public static class ZIP
	{
		public static void ExtractZipFile(string archiveFilenameIn, string outFolder, string[] skipOver = null, Action<string, string> onBeginUncompression = null)
		{
			int currentFile = 0;
			int totalFiles = 0;

			// Get total files to process
			using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(archiveFilenameIn)))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					if (!entry.IsDirectory && (skipOver == null || !skipOver.Contains(entry.Name)))
						totalFiles++;
				}
				zipStream.Close();
			}

			// Actually extract
			using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(archiveFilenameIn))) {
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					string filePath = Path.Combine(outFolder, entry.Name);
					string directoryName = Path.GetDirectoryName(filePath);

					if (directoryName.Length > 0 && !Directory.Exists(directoryName))
					{
						if (skipOver == null || !skipOver.Contains(entry.Name)) Directory.CreateDirectory(directoryName);
					}

					if (!entry.IsDirectory && (skipOver == null || !skipOver.Contains(entry.Name)))
					{
						currentFile++;
						onBeginUncompression?.Invoke(entry.Name, $"{currentFile}|{totalFiles}|{entry.CompressedSize}|{entry.Size}");

						string filePathTmp = $"{filePath}.tmp";
						if (File.Exists(filePathTmp)) File.Delete(filePathTmp);

						using (FileStream fileStream = File.Create(filePathTmp))
						{
							byte[] buffer = new byte[2048];
							int size;

							while ((size = zipStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								fileStream.Write(buffer, 0, size);
							}
						}

						if (File.Exists(filePath))
						{
							if (App.CreateChecksum(filePath) != App.CreateChecksum(filePathTmp)) File.Move(filePathTmp, filePath);
							else File.Delete(filePathTmp);
						} else File.Move(filePathTmp, filePath);
					}
				}
				zipStream.Close();
			}
		}

		public static void ExtractSingleFileFromZip(string archiveFilenameIn, string outFolder, string fileNameToExtract)
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
	}
	#endregion

	#region App
	public class App
	{
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		public static void BringToFront(string title)
		{
			IntPtr handle = FindWindow(null, title);
			if (handle == IntPtr.Zero) return;

			SetForegroundWindow(handle);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		public static bool IsRunningWine()
		{
			IntPtr hModule = GetModuleHandle("ntdll.dll");
			if (hModule != IntPtr.Zero)
			{
				IntPtr procAddress = GetProcAddress(hModule, "wine_get_version");
				return procAddress != IntPtr.Zero;
			}
			return false;
		}

		public static bool IsOlderWindows()
		{
			if (!IsWindows()) return false;

			// Checking for Windows 7 and earlier
			Version osVersion = Environment.OSVersion.Version;
			return osVersion.Major < 6 || (osVersion.Major == 6 && osVersion.Minor < 2);
		}

		public static long CalculateDirectorySize(string path)
		{
			long size = 0;

			foreach (string file in Directory.GetFiles(path))
			{
				FileInfo fileInfo = new FileInfo(file);
				size += fileInfo.Length;
			}

			foreach (string dir in Directory.GetDirectories(path)) { size += CalculateDirectorySize(dir); }

			return size;
		}

		public static string GetInstalledVersion(string client = null)
		{
			try
			{
				if (client == null)
				{
					return IsWindows()
						? Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion.ToString()
						: Assembly.GetExecutingAssembly().GetName().Version.ToString();
				};

				string versionJson = File.ReadAllText($"{Config.BaseInstallPath}\\clients\\{client}\\version.json");
				VersionJSON version = JsonConvert.DeserializeObject<VersionJSON>(versionJson);
				return version.Version;
			}
			catch { return ""; }
		}

		public static bool IsWindows() => !IsRunningWine() && Environment.OSVersion.Platform == PlatformID.Win32NT;

		public static string[] GetNETVersion()
		{
			// Get the CLR version | Get the targeted version
			string[] netData = new string[] { Environment.Version.ToString(), "UNKNOWN" };
#if NET35
			netData[1] = "net35";
#else
			netData[1] = "net48";
#endif
			return netData;
		}

		public static bool IsRunningFromInstall()
		{
			string currPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			return (currPath == Config.BaseInstallPath);
		}

		public static int IsDownloadOK(UpdateInfo info)
		{
			// First, let's check the filesize.
			long downloadedSize = new FileInfo(info.DownloadedPath).Length;
			if (downloadedSize != info.Size)
			{
				Console.Error.WriteLine($"ERROR: Downloaded file size does not match. Expected: {info.Size} Got: {downloadedSize}");
				return 1;
			}

			// Second, let's check the hash.
			if (info.DoSHACheck)
			{
				string checksum = CreateChecksum(info.DownloadedPath);
				if (checksum != info.Sha256)
				{
					Console.Error.WriteLine($"ERROR: Downloaded file SHA256 Checksum does not match. Expected: {info.Sha256} Got: {checksum}");
					return 2;
				}
			}

			// Lookin' good.
			return 0;
		}

		public static string CreateChecksum(string filePath)
		{
			string checksum = null;
			
			SHA256 sha256 = SHA256.Create();
			byte[] checksumB;

			using (FileStream stream = File.OpenRead(filePath))
			{
				checksumB = sha256.ComputeHash(stream);
				checksum = BitConverter.ToString(checksumB).Replace("-", "").ToLower();
			}

			return checksum;
		}

		public static void PurgeFilesAndFolders(string location, string[] exclude = null)
		{
			string[] files = Directory.GetFiles(location);
			string[] directories = Directory.GetDirectories(location);

			// Explode files
			foreach (string file in files)
			{
				if (exclude == null || !exclude.Contains(file))
				{
					File.Delete(file);
				}
			}

			// Now explode directories
			foreach (string directory in directories)
			{
				if (exclude == null || !exclude.Contains(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}

		public static void CreateShortcut(string title, string desc, string path, string args = "")
		{
			string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\" + Config.AppShortName;
			if (!Directory.Exists(shortcutPath)) Directory.CreateDirectory(shortcutPath);

			string shortcutLink = $@"{shortcutPath}\{title}.lnk";

			Shortcut sc = Shortcut.CreateInstance(shortcutLink);

			sc.TargetPath = path ?? sc.TargetPath;
			sc.Arguments = args ?? sc.Arguments;
			sc.Description = desc ?? sc.Description;
			sc.WorkingDirectory = path != null ? Path.GetDirectoryName(path) : sc.WorkingDirectory;
			sc.IconLocation = path ?? sc.TargetPath;

			Shortcut.SaveShortcut(sc);
		}

		public static void DeleteShortcut(string title)
		{
			string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs\" + Config.AppShortName;
			if (!Directory.Exists(shortcutPath)) return;

			string shortcutLink = $@"{shortcutPath}\{title}.lnk";
			if (!File.Exists(shortcutLink)) return;
			File.Delete(shortcutLink);
		}

		public static string GetOS()
		{
			return $"{new Microsoft.VisualBasic.Devices.ComputerInfo().OSFullName}";

		}

		public static bool KillAllBlox()
		{
			string[] processToAxe = new string[] { "RobloxPlayerBeta", "NovarinPlayerBeta", "NovaHost", "RobloxStudioBeta", "NovarinStudioBeta", "NovarinRPCManager" };
			foreach (string processName in processToAxe)
			{
				Process[] processes = Process.GetProcessesByName(processName);
				foreach (Process process in processes)
				{
					// Don't kill Roblox that isn't ours.
					if (!process.MainModule.FileName.StartsWith(Config.BaseInstallPath, StringComparison.OrdinalIgnoreCase)) continue;

					int waited = 0;
					int stop_waiting = 20000;
					while (true)
					{
						if (process.HasExited) break; // The thing closed!
						if (waited >= stop_waiting) break; // Timeout
						Thread.Sleep(1000);
						process.CloseMainWindow(); // Try to gracefully exit ROBLOX process(es)
						waited += 1000;
					}
					if (waited >= stop_waiting || !process.HasExited)
					{
						Program.logger.Log($"KillAllBlox: Failed to close because: {(waited >= stop_waiting ? "timeout" : "process not exited")}");
						MessageBox.Show(Error.GetErrorMsg(Error.Installer.KillTimeout), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}
			}
			return true;
		}
	}
	#endregion
}