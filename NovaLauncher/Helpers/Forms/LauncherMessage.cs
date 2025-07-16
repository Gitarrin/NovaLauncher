using System.Drawing;
using System.Windows.Forms;

namespace NovaLauncher.Helpers.Forms
{
	internal class LauncherMessage
	{
		private Base helperBase;

		public void Init(Base helperBaseF, string title, string desc = null, string btnText = "OK")
		{
			helperBase = helperBaseF;

			helperBase.DoThingsWInvoke(() =>
			{
				int sH = helperBase.instance.statusLbl.Height;
				helperBase.instance.statusLbl.Font = new Font("Arial", 16, FontStyle.Regular, GraphicsUnit.Point);
				foreach (string line in title.Split('\n'))
					helperBase.instance.statusLbl.Height += sH;
				helperBase.instance.statusLbl.Text = title;
				helperBase.instance.statusLbl.Location = new Point(helperBase.instance.statusLbl.Location.X, helperBase.instance.appIcon.Bottom + ((desc != null ? helperBase.instance.progressLbl.Top : helperBase.instance.actionBtn.Top) - helperBase.instance.appIcon.Bottom - helperBase.instance.statusLbl.Height) / 2);
				helperBase.instance.statusLbl.Visible = true;

				if (desc != null)
				{
					helperBase.instance.progressLbl.AutoSize = true;
					helperBase.instance.progressLbl.Font = new Font("Comic Sans MS", 12, FontStyle.Regular, GraphicsUnit.Point);
					helperBase.instance.progressLbl.Text = desc;
					helperBase.instance.progressLbl.Location = new Point((helperBase.instance.ClientSize.Width - helperBase.instance.progressLbl.Width) / 2, helperBase.instance.statusLbl.Bottom);
					helperBase.instance.progressLbl.Visible = true;
				} else helperBase.instance.progressLbl.Visible = false;

				helperBase.instance.progressBar.Visible = false;

				int aH = helperBase.instance.actionBtn.Height;
				int aW = helperBase.instance.actionBtn.Width;
				helperBase.instance.actionBtn.AutoSize = true;
				helperBase.instance.actionBtn.BackColor = Color.FromArgb(65, 197, 96);
				helperBase.instance.actionBtn.ForeColor = Color.WhiteSmoke;
				helperBase.instance.actionBtn.FlatStyle = FlatStyle.Flat;
				helperBase.instance.actionBtn.FlatAppearance.BorderSize = 0;
				helperBase.instance.actionBtn.Text = btnText;
				aW = helperBase.instance.actionBtn.Width >= aW ? helperBase.instance.actionBtn.Width : aW;
				helperBase.instance.actionBtn.AutoSize = false;
				helperBase.instance.actionBtn.Height = aH;
				helperBase.instance.actionBtn.Width = aW;
				helperBase.instance.actionBtn.Location = new Point((helperBase.instance.ClientSize.Width - helperBase.instance.actionBtn.Width) / 2, helperBase.instance.actionBtn.Location.Y);
				helperBase.instance.actionBtn.Enabled = true;
				helperBase.instance.actionBtn.Visible = true;
			});
		}
	}
}
