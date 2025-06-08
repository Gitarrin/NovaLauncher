using System;
using System.Threading;
using System.Windows.Forms;
using NovaLauncher.Helpers;
using NovaLauncher.Helpers.Forms;

namespace NovaLauncher
{
	public partial class Main : Form
	{
		internal Base currentInstance;
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

			currentInstance = new Base(this);

			currentInstance.CreateBackgroundTask(
				(s, e) =>
				{
					Thread.Sleep(500);
				},
				(s, e) => {
					if (Program.cliArgs.Uninstall) currentInstance.uninstaller.Init(currentInstance);
					else currentInstance.installer.Init(currentInstance);
				}
			);
		}

		private void CancelButton_Click(object sender, EventArgs e) => Close();
	}
}
