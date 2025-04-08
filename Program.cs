using System;
using System.Windows.Forms;

namespace NovaLauncher
{
	static class Program
	{
		internal static CLIArgs cliArgs = new CLIArgs();

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
				MessageBox.Show("ya dun goofed up the poor poor arguments :(");
				Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
			}

			Application.Run(new InstallerGUI()); // Ensure InstallerGUI is the startup form
		}
	}
}
