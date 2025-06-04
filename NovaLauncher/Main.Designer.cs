
namespace NovaLauncher
{
	partial class Main
	{
		/// <summary>
		/// Required designer variable.
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.verLbl = new System.Windows.Forms.Label();
			this.statusLbl = new System.Windows.Forms.Label();
			this.actionBtn = new System.Windows.Forms.Button();
			this.appIcon = new System.Windows.Forms.PictureBox();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.progressLbl = new System.Windows.Forms.Label();
			this.devWarningLbl = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// verLbl
			// 
			this.verLbl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.verLbl.Font = new System.Drawing.Font("Microsoft YaHei", 6F);
			this.verLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
			this.verLbl.Location = new System.Drawing.Point(12, 306);
			this.verLbl.Name = "verLbl";
			this.verLbl.Size = new System.Drawing.Size(473, 11);
			this.verLbl.TabIndex = 6;
			this.verLbl.Text = "NovaLauncher";
			this.verLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// statusLbl
			// 
			this.statusLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.statusLbl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.statusLbl.Font = new System.Drawing.Font("Microsoft YaHei", 11.25F);
			this.statusLbl.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.statusLbl.Location = new System.Drawing.Point(12, 188);
			this.statusLbl.Name = "statusLbl";
			this.statusLbl.Size = new System.Drawing.Size(473, 22);
			this.statusLbl.TabIndex = 2;
			this.statusLbl.Text = "Please wait...";
			this.statusLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.statusLbl.UseCompatibleTextRendering = true;
			// 
			// actionBtn
			// 
			this.actionBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.actionBtn.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.actionBtn.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
			this.actionBtn.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.actionBtn.Location = new System.Drawing.Point(203, 267);
			this.actionBtn.Name = "actionBtn";
			this.actionBtn.Size = new System.Drawing.Size(90, 28);
			this.actionBtn.TabIndex = 4;
			this.actionBtn.Text = "Cancel";
			this.actionBtn.UseCompatibleTextRendering = true;
			this.actionBtn.UseVisualStyleBackColor = false;
			this.actionBtn.Click += new System.EventHandler(this.CancelButton_Click);
			// 
			// appIcon
			// 
			this.appIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.appIcon.BackgroundImage = global::NovaLauncher.Properties.Resources.logo;
			this.appIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.appIcon.Image = global::NovaLauncher.Properties.Resources.logo;
			this.appIcon.Location = new System.Drawing.Point(193, 64);
			this.appIcon.Name = "appIcon";
			this.appIcon.Size = new System.Drawing.Size(110, 104);
			this.appIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.appIcon.TabIndex = 3;
			this.appIcon.TabStop = false;
			// 
			// progressBar
			// 
			this.progressBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.progressBar.Location = new System.Drawing.Point(12, 219);
			this.progressBar.MarqueeAnimationSpeed = 10;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(473, 21);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar.TabIndex = 1;
			// 
			// progressLbl
			// 
			this.progressLbl.AutoEllipsis = true;
			this.progressLbl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.progressLbl.Font = new System.Drawing.Font("Microsoft YaHei", 6F);
			this.progressLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
			this.progressLbl.Location = new System.Drawing.Point(12, 243);
			this.progressLbl.Name = "progressLbl";
			this.progressLbl.Size = new System.Drawing.Size(473, 11);
			this.progressLbl.TabIndex = 5;
			this.progressLbl.Text = "0% (0.00 KB/0.00 KB | 0.00 KB/s)  |  ETA: 00:00:00";
			this.progressLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.progressLbl.Visible = false;
			// 
			// devWarningLbl
			// 
			this.devWarningLbl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.devWarningLbl.Font = new System.Drawing.Font("Microsoft YaHei", 6F);
			this.devWarningLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
			this.devWarningLbl.Location = new System.Drawing.Point(12, 9);
			this.devWarningLbl.Name = "devWarningLbl";
			this.devWarningLbl.Size = new System.Drawing.Size(473, 11);
			this.devWarningLbl.TabIndex = 7;
			this.devWarningLbl.Text = "DEVELOPMENT VIEW -- MAY NOT BE REPRESENTATIVE OF FINAL PRODUCT";
			this.devWarningLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.devWarningLbl.Visible = false;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.ClientSize = new System.Drawing.Size(498, 326);
			this.ControlBox = false;
			this.Controls.Add(this.devWarningLbl);
			this.Controls.Add(this.progressLbl);
			this.Controls.Add(this.actionBtn);
			this.Controls.Add(this.appIcon);
			this.Controls.Add(this.statusLbl);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.verLbl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Main";
			this.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Launcher";
			this.TransparencyKey = System.Drawing.Color.Transparent;
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label verLbl;
		private System.Windows.Forms.PictureBox appIcon;
		private System.Windows.Forms.Label devWarningLbl;
		internal System.Windows.Forms.Label statusLbl;
		internal System.Windows.Forms.Button actionBtn;
		internal System.Windows.Forms.ProgressBar progressBar;
		internal System.Windows.Forms.Label progressLbl;
	}
}
