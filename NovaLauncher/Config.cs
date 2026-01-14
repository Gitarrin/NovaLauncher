using System;

namespace NovaLauncher
{
	internal static class Config
	{
		public static readonly string[] Servers = new string[]
		{
			"http://dl.novarin.cc"
		};
		public static string SelectedServer = "";
		public static string LauncherSetup = "/client/launcher/";

		public static string AppShortName = "Novarin (test)";
		public static string AppName = $"{AppShortName} Launcher (test)";
		public static string AppEXE = "NovaLauncher.exe";
		public static string AppProtocol = AppShortName.ToLower();
		public static bool Debug = false;

		public static string BaseInstallPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\{AppShortName}";
	}
}
