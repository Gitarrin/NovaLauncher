using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CommandLine;
using CommandLine.Text;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace NovaLauncher
{
	public class Helper
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
			public static string FormatSpeed(double bytesPerSecond)
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

			public static object GetLatestServerVersionInfo(string location)
			{
				try
				{
					using (WebClient client = new WebClient())
					{
						client.Headers.Add("user-agent", GetUserAgent());
						string recvData = client.DownloadString(location);
						return JsonConvert.DeserializeObject<LatestClientInfo>(recvData);
					}
				}
				catch { return null; }
			}
		}
		#endregion

		#region ZIP
		public static class ZIP
		{
			public static void ExtractZipFile(string archiveFilenameIn, string outFolder)
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

			public static bool IsDownloadOK(string path, LatestClientInfo data, bool doCheck)
			{
				// First, let's check the filesize.
				long downloadedSize = new FileInfo(path).Length;
				if (downloadedSize != data.Size)
				{
					Console.Error.WriteLine($"ERROR: Downloaded file size does not match. Expected: {data.Size} Got: {downloadedSize}");
					return false;
				}

				// Second, let's check the hash.
				if (doCheck)
				{
					SHA256 sha256 = SHA256.Create();
					byte[] checksumB;
					using (FileStream stream = File.OpenRead(path))
					{
						checksumB = sha256.ComputeHash(stream);
						string checksum = BitConverter.ToString(checksumB).Replace("-", "").ToLower();
						if (checksum != data.Sha256)
						{
							Console.Error.WriteLine($"ERROR: Downloaded file SHA256 Checksum does not match. Expected: {data.Sha256} Got: {checksum}");
							return false;
						}
					}
				}

				// Lookin' good.
				return true;
			}
		}
		#endregion
	}

	public class GameClient
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public string InstallPath { get; set; }
		public string Executable { get; set; }
		public string HostExecutable { get; set; }
		public string StudioExecutable { get; set; }
		public string Arguments { get; set; }
		public bool ClientCheck { get; set; }
		public bool DiscordRPC { get; set; }
	}

	public class LatestClientInfo
	{
		public string Version { get; set; }
		public string Url { get; set; }
		public string Sha256 { get; set; }
		public long Size { get; set; }
	}
	public class LaunchData
	{
		public string JoinScript { get; set; }
		public string Ticket { get; set; }
		public string LaunchType { get; set; }
		public string LaunchToken { get; set; }
		public string PlaceId { get; set; }
		public string JobId { get; set; }
		public string Version { get; set; }
	}
	public class VersionJSON
	{
		public string Version { get; set; }
	}
	public sealed class CLIArgs
	{
		[Option('t', "token", Required = false, HelpText = "Used in launching roblox. Authentication.")]
		public string Token { get; set; }

		[Option('u', "update", Required = false, HelpText = "Forces an update to the launcher.")]
		public bool UpdateLauncher { get; set; }

		[Option("updateclient", Required = false, HelpText = "Forces an update to the selected client.")]
		public bool UpdateClient { get; set; }

		[Option("uninstall", Required = false, HelpText = "Uninstalls Novarin :(")]
		public bool Uninstall { get; set; }

		[Option("upinfo", Required = false)]
		public string UpdateInfo { get; set; } // Used to get the update info

		[Option('w', "hide-wine-message", Required = false, HelpText = "Disables a warning when using Wine")]
		public bool HideWineMessage { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}