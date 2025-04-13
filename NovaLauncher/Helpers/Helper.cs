using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
		public static void ExtractZipFile(string archiveFilenameIn, string outFolder, string[] skipOver = null)
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
						if (skipOver == null || !skipOver.Contains(entry.Name)) Directory.CreateDirectory(directoryName);
					}

					if (!entry.IsDirectory && (skipOver == null || !skipOver.Contains(entry.Name)))
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
				if (client == null) return Assembly.GetExecutingAssembly().GetName().Version.ToString();

				string versionJson = File.ReadAllText(Config.BaseInstallPath + $"\\{client}\\version.json");
				VersionJSON version = JsonConvert.DeserializeObject<VersionJSON>(versionJson);
				return version.Version;
			}
			catch { return ""; }
		}

		public static bool IsRunningFromInstall()
		{
			string currPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
				SHA256 sha256 = SHA256.Create();
				byte[] checksumB;
				using (FileStream stream = File.OpenRead(info.DownloadedPath))
				{
					checksumB = sha256.ComputeHash(stream);
					string checksum = BitConverter.ToString(checksumB).Replace("-", "").ToLower();
					if (checksum != info.Sha256)
					{
						Console.Error.WriteLine($"ERROR: Downloaded file SHA256 Checksum does not match. Expected: {info.Sha256} Got: {checksum}");
						return 2;
					}
				}
			}

			// Lookin' good.
			return 0;
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
	}
	#endregion
}