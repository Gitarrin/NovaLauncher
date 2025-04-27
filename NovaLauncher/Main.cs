using System;
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

			this.WindowState = FormWindowState.Minimized;
			this.Show();
			this.WindowState = FormWindowState.Normal;

			if (Program.cliArgs.Uninstall)
			{
				LoadScreen(new Uninstaller());
				return;
			}
			LoadScreen(new Installer());
		}

		public static void LoadScreen(UserControl screen)
		{
			panelContainer.Controls.Clear();
			screen.Dock = DockStyle.Fill;
			panelContainer.Controls.Add(screen);
		}
	}
}
