using System;
using System.Collections.Generic;

namespace NovaLauncher
{
	internal class Error
	{
		public static ErrorBase Default { get; } = new ErrorBase("999-0000", "An unknown error occurred.");

		public static class Installer {
			public static ErrorBase ConnectFailed { get; } = new ErrorBase("120-0001", "We were unable to connect to our servers. Check your internet connection.");
			public static ErrorBase ConfigureFailed { get; } = new ErrorBase("120-0002", "Error while configuring {APPNAME}.");
			public static ErrorBase LaunchFailed { get; } = new ErrorBase("120-0003", "Failed to launch {APPNAME}.");
			public static ErrorBase CreateUninstallKeys { get; } = new ErrorBase("120-0004", "Failed to create uninstall keys.");
			public static ErrorBase GoofedArgs { get; } = new ErrorBase("120-0005", "ya dun goofed up the poor poor arguments :(");
			public static ErrorBase ExtractFailed { get; } = new ErrorBase("120-0006", "An error occurred while attempting to extract the client.\n{INSTALLPATH}");
			public static ErrorBase DownloadFailed { get; } = new ErrorBase("120-0007", "An error occurred while trying to download {APPNAME}!\nThe installer will now close.");
			public static ErrorBase DownloadCorrupted { get; } = new ErrorBase("120-0008", "Downloaded file is corrupted. Please try again.\n(Got Code {CHECKSUMCODE})");
			public static ErrorBase DownloadStartFail { get; } = new ErrorBase("120-0009", "Failed to start download.");
			public static ErrorBase RPBNotFound { get; } = new ErrorBase("120-0010", "Hello. We cannot Find RobloxPlayerBeta.exe. It has ran away. We will try to reinstall now.");
			public static ErrorBase LaunchClientNoVersion { get; } = new ErrorBase("120-0011", "No version provided for launch! That's a little weird...");
			public static ErrorBase LaunchClientFailed { get; } = new ErrorBase("120-0012", "The provided client ({CLIENT}) is not supported on this build of {APPNAME} or is invalid.");
			public static ErrorBase LaunchClientTimeout { get; } = new ErrorBase("120-0013", "{CLIENT} did not launch in time.");
			public static ErrorBase LaunchClientNotAvailable { get; } = new ErrorBase("120-0014", "{CLIENT} is no longer available.");
		}

		public static class Uninstaller
		{
			public static ErrorBase PurgeInstallDirFailed { get; } = new ErrorBase("120-0011", "Failed to purge installation directory.\n{ERROR}");
			public static ErrorBase CreateCleanupFailed { get; } = new ErrorBase("120-0012", "Failed to create cleanup file.\n{ERROR}");
			public static ErrorBase StartCleanupFailed { get; } = new ErrorBase("120-0013", "Failed to start cleanup file.\n{ERROR}");
			public static ErrorBase DeleteProtocolKeysFailed { get; } = new ErrorBase("120-0014", "Failed to remove open protocol keys.\n{ERROR}");
			public static ErrorBase DeleteUninstallerKeysFailed { get; } = new ErrorBase("120-0015", "Failed to remove uninstaller keys.\n{ERROR}");
		}

		internal static readonly Dictionary<string, string> DefaultReplacements = new Dictionary<string, string>()
		{
			{ "{APPNAME}", Config.AppName }
		};

		public static string GetErrorMsg(ErrorBase error, Dictionary<string, string> moreReplacements = null)
		{
			if (error == null) return "An unknown error occurred!";
			string message = error.Message ?? "An error occurred!";

			Dictionary<string, string> replacementsMerged = new Dictionary<string, string>(DefaultReplacements);
			if (moreReplacements != null)
			{
				foreach (KeyValuePair<string,string> pair in moreReplacements)
				{
					replacementsMerged[pair.Key] = pair.Value;
				};
			};

			foreach (KeyValuePair<string, string> pair in replacementsMerged)
			{
				message = message.Replace(pair.Key, pair.Value);
			};
			return $"{message}\nError {error.Code}";
		}

		internal class ErrorBase
		{
			public string Code { get; set; }
			public string Message { get; set; }

			public ErrorBase(string code, string message)
			{
				Code = code;
				Message = message;
			}
		}
	}
}
