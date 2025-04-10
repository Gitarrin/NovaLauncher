using System;
using System.Windows.Forms;
namespace NovaLauncher
{
	public partial class InstallerGUI : Form
	{
		private static Panel panelContainer;

		public InstallerGUI()
		{
			InitializeComponent();
			panelContainer = new Panel
			{
				Dock = DockStyle.Fill
			};
			this.Controls.Add(panelContainer);

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
