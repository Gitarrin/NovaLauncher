using System.Windows.Forms;

namespace NovaLauncher
{
	public partial class Main : Form
	{
		private static Panel panelContainer;

		public Main()
		{
			InitializeComponent();

			panelContainer = new Panel
			{
				Dock = DockStyle.Fill
			};
			this.Controls.Add(panelContainer);
			verLbl.Visible = false;
			this.Text = Config.AppName;

			this.WindowState = FormWindowState.Minimized;
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

			if (!Config.Debug && Control.ModifierKeys == Keys.Shift) Config.Debug = true;
			if (Config.Debug) verLbl.Visible = true;

			verLbl.Text = $"NovaLauncher {Helpers.App.GetInstalledVersion()}  - Running {(Config.Debug ? " in debug" : "normal")}.";

			if (Program.cliArgs.Uninstall)
			{
				LoadScreen(new Uninstaller());
				return;
			}
			LoadScreen(new Installer());
		}

		public static void LoadScreen(UserControl screen)
		{
			Program.logger.Log($"Screen switch: {(panelContainer.Controls.Count > 0 ? ((panelContainer.Controls[0] as UserControl).Name + " -> " + screen.Name) : screen.Name)}");
			panelContainer.Controls.Clear();
			screen.Dock = DockStyle.Fill;
			panelContainer.Controls.Add(screen);
		}
	}
}
