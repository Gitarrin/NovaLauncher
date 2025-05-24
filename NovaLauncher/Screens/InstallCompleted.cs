using System;
using System.Windows.Forms;

namespace NovaLauncher.Screens
{
	public partial class InstallCompleted: UserControl
	{
		public InstallCompleted()
		{
			InitializeComponent();
		}

		public void CloseButton_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
