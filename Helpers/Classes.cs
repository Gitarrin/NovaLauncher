using System.Collections.Generic;
using CommandLine.Text;
using CommandLine;

namespace NovaLauncher
{
	#region App
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

	public class UpdateInfo
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public string Url { get; set; }
		public long Size { get; set; }
		public bool IsUpgrade { get; set; }
		public bool IsLauncher { get; set; }
		public string Sha256 { get; set; }
		public bool DoSHACheck { get; set; }
		public string DownloadedPath { get; set; }
		public string InstallPath { get; set; }
	}
	#endregion

	#region Launcher
	public class LatestLauncherInfo
	{
		public LauncherInfo Launcher { get; set; }
		public Dictionary<string, LauncherClient> Clients { get; set; }
		public List<LauncherAlert> Alerts { get; set; }
	}

	public class LauncherInfo
	{
		public string Version { get; set; }
		public string Url { get; set; }
		public string Sha256 { get; set; }
		public long Size { get; set; }
		public LauncherInfoUrls Urls { get; set; }
	}
	public class LauncherInfoUrls
	{
		public string Base { get; set; }
		public string UpdateInfo { get; set; }
		public string AboutInfo { get; set; }
		public string HelpInfo { get; set; }
		public string Downloaded { get; set; }
	}

	public enum LauncherClientStatus
	{
		NO_RELEASE = -1,
		OK = 0,
		PAUSED = 1,
		REMOVED = 2
	}
	public class LauncherClient
	{
		public LauncherClientStatus Status { get; set; }
		public string Info { get; set; }
		public string Name { get; set; }
		public LauncherClientExecutables Executables { get; set; }
		public bool Checksum { get; set; }
	}
	public class LauncherClientExecutables
	{
		public string Player { get; set; }
		public string Host { get; set; }
		public string Studio { get; set; }
	}

	public class LauncherAlert
	{
		public string Id { get; set; }
		public string Message { get; set; }
		public List<string> VersionsAffected { get; set; }
		public bool CanContinue { get; set; }
		public long ActiveUntil { get; set; }
	}
	#endregion

	public class VersionJSON
	{
		public string Version { get; set; }
	}

	#region Clients
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
		public string GameBase {  get; set; }
	}


	public class LatestClientInfo
	{
		public string Version { get; set; }
		public string Url { get; set; }
		public string Sha256 { get; set; }
		public long Size { get; set; }
	}
	#endregion
}
