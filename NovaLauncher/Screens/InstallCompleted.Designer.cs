namespace NovaLauncher.Screens
{
    partial class InstallCompleted
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
			this.status = new System.Windows.Forms.Label();
			this.CloseButton = new System.Windows.Forms.Button();
			this.appIcon = new System.Windows.Forms.PictureBox();
			this.instructions = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).BeginInit();
			this.SuspendLayout();
			// 
			// status
			// 
			this.status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.status.Font = new System.Drawing.Font("Microsoft YaHei", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.status.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.status.Location = new System.Drawing.Point(12, 201);
			this.status.Name = "status";
			this.status.Size = new System.Drawing.Size(473, 37);
			this.status.TabIndex = 2;
			this.status.Text = "NOVARIN IS SUCCESSFULLY INSTALLED!";
			this.status.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			this.status.UseCompatibleTextRendering = true;
			// 
			// CloseButton
			// 
			this.CloseButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(197)))), ((int)(((byte)(96)))));
			this.CloseButton.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.CloseButton.FlatAppearance.BorderSize = 0;
			this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.CloseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CloseButton.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.CloseButton.Location = new System.Drawing.Point(174, 270);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(148, 35);
			this.CloseButton.TabIndex = 4;
			this.CloseButton.Text = "OK";
			this.CloseButton.UseCompatibleTextRendering = true;
			this.CloseButton.UseVisualStyleBackColor = false;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// appIcon
			// 
			this.appIcon.BackColor = System.Drawing.Color.Transparent;
			this.appIcon.BackgroundImage = global::NovaLauncher.Properties.Resources.logo;
			this.appIcon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.appIcon.Image = global::NovaLauncher.Properties.Resources.logo;
			this.appIcon.Location = new System.Drawing.Point(194, 60);
			this.appIcon.Name = "appIcon";
			this.appIcon.Size = new System.Drawing.Size(110, 104);
			this.appIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.appIcon.TabIndex = 3;
			this.appIcon.TabStop = false;
			// 
			// instructions
			// 
			this.instructions.AutoSize = true;
			this.instructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.instructions.ForeColor = System.Drawing.Color.WhiteSmoke;
			this.instructions.Location = new System.Drawing.Point(75, 238);
			this.instructions.Name = "instructions";
			this.instructions.Size = new System.Drawing.Size(352, 18);
			this.instructions.TabIndex = 5;
			this.instructions.Text = "Click the \'Join\' button on any game to join the action!";
			// 
			// InstallCompleted
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.Controls.Add(this.instructions);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.appIcon);
			this.Controls.Add(this.status);
			this.DoubleBuffered = true;
			this.Name = "InstallCompleted";
			this.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.Size = new System.Drawing.Size(498, 326);
			((System.ComponentModel.ISupportInitialize)(this.appIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.PictureBox appIcon;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Label instructions;
    }
}
