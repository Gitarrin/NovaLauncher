using System;

namespace NovaLauncher
{
    partial class Installer
    {
        /// <summary> 
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
			this.components = new System.ComponentModel.Container();
			this.status = new System.Windows.Forms.Label();
			this.cancelButton = new System.Windows.Forms.Button();
			this.appIcon = new System.Windows.Forms.PictureBox();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.progressDebugLbl = new System.Windows.Forms.Label();
			this.loadTimer = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// status
			// 
			this.status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.status.Font = new System.Drawing.Font("Microsoft YaHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.status.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.status.Location = new System.Drawing.Point(12, 202);
			this.status.Name = "status";
			this.status.Size = new System.Drawing.Size(473, 37);
			this.status.TabIndex = 2;
			this.status.Text = "Please Wait...";
			this.status.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			this.status.UseCompatibleTextRendering = true;
			// 
			// cancelButton
			// 
			this.cancelButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.cancelButton.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.cancelButton.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.cancelButton.Location = new System.Drawing.Point(204, 277);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(90, 28);
			this.cancelButton.TabIndex = 4;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseCompatibleTextRendering = true;
			this.cancelButton.UseVisualStyleBackColor = false;
			this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
			// 
			// appIcon
			// 
			this.appIcon.BackgroundImage = global::NovaLauncher.Properties.Resources.logo;
			this.appIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.appIcon.Location = new System.Drawing.Point(194, 60);
			this.appIcon.Name = "appIcon";
			this.appIcon.Size = new System.Drawing.Size(110, 104);
			this.appIcon.TabIndex = 3;
			this.appIcon.TabStop = false;
			// 
			// progressBar
			// 
			this.progressBar.Location = new System.Drawing.Point(12, 242);
			this.progressBar.MarqueeAnimationSpeed = 10;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(473, 21);
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar.TabIndex = 1;
			// 
			// progressDebugLbl
			// 
			this.progressDebugLbl.AutoSize = true;
			this.progressDebugLbl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
			this.progressDebugLbl.Location = new System.Drawing.Point(10, 266);
			this.progressDebugLbl.Name = "progressDebugLbl";
			this.progressDebugLbl.Size = new System.Drawing.Size(66, 12);
			this.progressDebugLbl.TabIndex = 5;
			this.progressDebugLbl.Text = "0% (0 KB/s)";
			this.progressDebugLbl.Visible = false;
			// 
			// loadTimer
			// 
			this.loadTimer.Tick += new System.EventHandler(this.loadTimer_Tick);
			// 
			// Installer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.Controls.Add(this.progressDebugLbl);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.appIcon);
			this.Controls.Add(this.status);
			this.Controls.Add(this.progressBar);
			this.DoubleBuffered = true;
			this.Name = "Installer";
			this.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.Size = new System.Drawing.Size(498, 326);
			this.Load += new System.EventHandler(this.Installer_Load);
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        private void Installer_Load(object sender, EventArgs e)
        {
            this.loadTimer.Start();
        }

        private void loadTimer_Tick(object sender, EventArgs e)
        {
            this.loadTimer.Stop();
            this.Form1_Shown(sender, e);
        }


        #endregion
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.PictureBox appIcon;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.Label progressDebugLbl;
		private System.Windows.Forms.Timer loadTimer;
	}
}
