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

        private System.Windows.Forms.Timer loadTimer;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.status = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.appIcon = new System.Windows.Forms.PictureBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.loadTimer = new System.Windows.Forms.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.appIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // status
            // 
            this.status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.status.Font = new System.Drawing.Font("Microsoft YaHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.status.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.status.Location = new System.Drawing.Point(12, 219);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(473, 40);
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
            this.cancelButton.Location = new System.Drawing.Point(204, 300);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(90, 30);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseCompatibleTextRendering = true;
            this.cancelButton.UseVisualStyleBackColor = false;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // appIcon
            // 
            this.appIcon.BackgroundImage = global::NovaLauncher.Properties.Resources.finoob64;
            this.appIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.appIcon.Location = new System.Drawing.Point(196, 68);
            this.appIcon.Name = "appIcon";
            this.appIcon.Size = new System.Drawing.Size(110, 113);
            this.appIcon.TabIndex = 3;
            this.appIcon.TabStop = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 262);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(473, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 1;
            // 
            // loadTimer
            // 
            this.loadTimer.Interval = 100; // Set the interval to 100 milliseconds
            this.loadTimer.Tick += new System.EventHandler(this.loadTimer_Tick);
            // 
            // Installer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.appIcon);
            this.Controls.Add(this.status);
            this.Controls.Add(this.progressBar);
            this.Name = "Installer";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Size = new System.Drawing.Size(498, 353);
            this.Load += new System.EventHandler(this.Installer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.appIcon)).EndInit();
            this.ResumeLayout(false);
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
    }
}
