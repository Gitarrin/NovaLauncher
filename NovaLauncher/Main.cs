using System;
using System.Threading;
using System.Windows.Forms;
using NovaLauncher.Helpers;

namespace NovaLauncher
{
	public partial class Main : Form
	{
		internal LauncherForm currentInstance;
		public Main()
		{
			InitializeComponent();

#if NET48
			DoubleBuffered = true;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
#endif

			Text = Config.AppName;
			WindowState = FormWindowState.Minimized;
			Show();
			WindowState = FormWindowState.Normal;

			// Check for Debug
			if (!Config.Debug && ModifierKeys == Keys.Shift) Config.Debug = true;
			verLbl.Text = $"NovaLauncher {App.GetInstalledVersion()}{(Config.Debug ? " - Running in debug" : "")}";

#if DEBUG
			devWarningLbl.Visible = true;
#endif

			LauncherForm.CreateBackgroundTask(
				(s, e) =>
				{
					currentInstance = new LauncherForm(this);
					Thread.Sleep(500);

					if (Program.cliArgs.Uninstall) currentInstance.uninstaller.Init();
					else currentInstance.installer.Init();
				},
				(s, e) => { }
			);
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
