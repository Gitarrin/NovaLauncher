using System;
using System.Drawing;
using System.Windows.Forms;

namespace NovaLauncher.Helpers.Forms
{
	internal class InstallCompleted
	{
		private Base helperBase;

		public void Init(Base helperBaseF)
		{
			helperBase = helperBaseF;

			helperBase.DoThingsWInvoke(() =>
			{
				helperBase.instance.statusLbl.AutoSize = true;
				helperBase.instance.statusLbl.Font = new Font("Microsoft YaHei", 16, FontStyle.Regular, GraphicsUnit.Point);
				helperBase.instance.statusLbl.Text = "NOVARIN IS SUCCESSFULLY INSTALLED!";
				helperBase.instance.statusLbl.Location = new Point((helperBase.instance.ClientSize.Width - helperBase.instance.statusLbl.Width) / 2, helperBase.instance.statusLbl.Location.Y);
				helperBase.instance.statusLbl.Visible = true;

				helperBase.instance.progressLbl.AutoSize = true;
				helperBase.instance.progressLbl.Font = new Font("Comic Sans MS", 12, FontStyle.Regular, GraphicsUnit.Point);
				helperBase.instance.progressLbl.Text = "Click the 'Join' button on any game to join the action!";
				helperBase.instance.progressLbl.Location = new Point((helperBase.instance.ClientSize.Width - helperBase.instance.progressLbl.Width) / 2, helperBase.instance.progressBar.Location.Y);
				helperBase.instance.progressLbl.Visible = true;

				helperBase.instance.progressBar.Visible = false;

				helperBase.instance.actionBtn.BackColor = Color.FromArgb(65, 197, 96);
				helperBase.instance.actionBtn.ForeColor = Color.WhiteSmoke;
				helperBase.instance.actionBtn.FlatStyle = FlatStyle.Flat;
				helperBase.instance.actionBtn.FlatAppearance.BorderSize = 0;
				helperBase.instance.actionBtn.Text = "OK";
				helperBase.instance.actionBtn.Enabled = true;
				helperBase.instance.actionBtn.Visible = true;
			});
		}
	}
}
