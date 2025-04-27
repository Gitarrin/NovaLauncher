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

			if (Control.ModifierKeys == Keys.Shift) {
				Config.Debug = true;
			}

			logger = new Logger();
			logger.Log($"{Config.AppName}  -  v{Helpers.App.GetInstalledVersion()} on {Helpers.App.GetOS()}");
			logger.Log($" - Running from Base Install path? {Helpers.App.IsRunningFromInstall()}");
			logger.Log($" - Running Wine? {Helpers.App.IsRunningWine()}");
			logger.Log($" - Running older Windows? {Helpers.App.IsOlderWindows()}");

			AppDomain.CurrentDomain.ProcessExit += (s, e) =>
			{
				logger.Log("Shutting down!");
				
				// ... Stop other tasks, if needed

				// Stop da log!
				logger.Log("Stopping & flushing logs...");
				logger.Shutdown();
			};

			Application.Run(new Main()); // Ensure Main is the startup form
		}
	}
}
