using System;

namespace NovaLauncher
{

	internal static class Config
	{
		public static string LauncherSetup = "http://n.termy.lol/client/launcher";
		public static string BaseInstallPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Novarizz";
		public static string AppShortName = "Novarin";
		public static string AppName = $"{AppShortName} Launcher";
		public static string AppEXE = "NovaLauncher.exe";
		public static string AppProtocol = "novarin";
	}
}
