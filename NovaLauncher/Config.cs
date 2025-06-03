using System;

namespace NovaLauncher
{
	internal static class Config
	{
		public static readonly string[] Servers = new string[]
		{
			"http://n.termy.lol", // Main
			"http://wowlauncher.novarin.co", // Mirror
			"http://termy.nekos.sh" // Mirror backup
		};
		public static string SelectedServer = "";
		public static string LauncherSetup = "/client/launcher/";

		public static string AppShortName = "Novarin";
		public static string AppName = $"{AppShortName} Launcher";
		public static string AppEXE = "NovaLauncher.exe";
		public static string AppProtocol = "novarin";
		public static bool Debug = false;

		public static string BaseInstallPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{AppShortName}";
		public static string BaseLegacyInstallPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Novarizz";
	}
}
