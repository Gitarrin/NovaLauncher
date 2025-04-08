using System;

namespace NovaLauncher
{
	internal static class WebConfig
	{
		public static string SetupBase = "http://n.termy.lol";
		public static string LauncherSetup = $"{SetupBase}/client/launcher";
		public static string ClientSetup = $"{SetupBase}/client/setup";
		public static string DownloadComplete = $"{SetupBase}/app/downloaded";

		public static string GameBase = "http://novarin.cc";
		public static string UpdateInfo = $"{GameBase}/app/downloads";
		public static string AboutInfo = $"{GameBase}/app/forum/";
		public static string HelpInfo = $"{GameBase}/app/wiki/";
	}

	internal static class Config
	{
		public static string BaseInstallPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Novarizz";
		public static string AppShortName = "Novarin";
		public static string AppName = $"{AppShortName} Launcher";
		public static string AppEXE = "NovaLauncher.exe";
		public static string AppProtocol = "novarin";
		public static bool AppSHA256Check = true;

		public static class Client2012
		{
			public static string Name = "Novarin 2012";
			public static string InstallPath = $"{BaseInstallPath}\\2012";
			public static string Executable = $"RobloxPlayerBeta.exe";
			public static string HostExecuteable = $"NovaHost.exe";
			public static string StudioExecutable = $"NovarinStudioBeta.exe";
			public static bool SHA256Check = false;
			public static bool DiscordRPC = true;
		}
		public static class Client2015
		{
			public static string Name = "Novarin 2015";
			public static string InstallPath = $"{BaseInstallPath}\\2015";
			public static string Executable = $"RobloxPlayerBeta.exe";
			public static string HostExecuteable = $"NovaHost.exe";
			public static string StudioExecutable = $"NovarinStudioBeta.exe";
			public static bool SHA256Check = false;
			public static bool DiscordRPC = true;
		}
		public static class Client2016
		{
			public static string Name = "Novarin 2016";
			public static string InstallPath = $"{BaseInstallPath}\\2016";
			public static string Executable = $"RobloxPlayerBeta.exe";
			public static string HostExecuteable = $"NovaHost.exe";
			public static string StudioExecutable = $"NovarinStudioBeta.exe";
			public static bool SHA256Check = false;
			public static bool DiscordRPC = true;
		}
	}
}
