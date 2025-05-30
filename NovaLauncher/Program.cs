using System;
using System.Windows.Forms;

namespace NovaLauncher
{
	static class Program
	{
		internal static CLIArgs cliArgs = new CLIArgs();
		internal static Logger logger = null;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Do cli args processing early here.
			string[] args = Environment.GetCommandLineArgs();
			if (!CommandLine.Parser.Default.ParseArguments(args, cliArgs))
			{
				MessageBox.Show(Error.GetErrorMsg(Error.GoofedArgs));
				Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
			}

			if (cliArgs.Token != null)
			{
				string[] launchData = cliArgs.Token.Split(':');
				if (launchData[0] != Config.AppProtocol)
				{
					MessageBox.Show(Error.GetErrorMsg(Error.GoofedArgs));
					Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
				}

				if (launchData[1] == "test")
				{
					MessageBox.Show("Hello! This is the Novarin Launcher.\nThis tests to confirm you have URL protocols working! Yay!\nHave fun!", Config.AppName, MessageBoxButtons.OK);
					Environment.Exit(0);
				}
				else
				{
					try
					{
						Convert.FromBase64String(launchData[1]);
					}
					catch
					{
						MessageBox.Show(Error.GetErrorMsg(Error.GoofedArgs));
						Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
					}
				};
			};

			// Enable debug either by CLI or by holding SHIFT
			if (cliArgs.ForceDebug) Config.Debug = true;
			else if (Control.ModifierKeys == Keys.Shift) Config.Debug = true;

			logger = new Logger();
			logger.Log($"{Config.AppName}  -  v{Helpers.App.GetInstalledVersion()} on {Helpers.App.GetOS()}");
			logger.Log($".NET Framework CLR: {Helpers.App.GetNETVersion()[0]} | .NET Framework Target: {Helpers.App.GetNETVersion()[1]}");
			logger.Log($" - Running from Base Install path? {Helpers.App.IsRunningFromInstall()}");
			logger.Log(" - Running Windows? " + (
				Helpers.App.IsWindows()
					? $"Yes, on {(Helpers.App.IsOlderWindows() ? "older" : "newer")} Windows"
					: $"No and {(Helpers.App.IsRunningWine() ? "running" : "not running")} Wine"
			));

			AppDomain.CurrentDomain.ProcessExit += (s, e) =>
			{
				logger.Log("Shutting down!");
				
				// ... Stop other tasks, if needed

				// Stop da log!
				logger.Log("Stopping & flushing logs...");
				logger.Shutdown();
			};
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				logger.Log("Unhandled Exception!");

				MessageBox.Show(Error.GetErrorMsg(Error.UnhandledException, new System.Collections.Generic.Dictionary<string, string> { { "{ERROR}", e.ExceptionObject.ToString() } }), Config.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-2);
			};

			Application.Run(new Main()); // Ensure Main is the startup form
		}
	}
}
