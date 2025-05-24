
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
			this.SuspendLayout();
			// 
			// verLbl
			// 
			this.verLbl.AutoSize = true;
			this.verLbl.Font = new System.Drawing.Font("Arial", 6F);
			this.verLbl.ForeColor = System.Drawing.Color.White;
			this.verLbl.Location = new System.Drawing.Point(12, 307);
			this.verLbl.Name = "verLbl";
			this.verLbl.Size = new System.Drawing.Size(53, 10);
			this.verLbl.TabIndex = 0;
			this.verLbl.Text = "NovaLauncher";
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.ClientSize = new System.Drawing.Size(498, 326);
			this.ControlBox = false;
			this.Controls.Add(this.verLbl);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Main";
			this.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.TransparencyKey = System.Drawing.Color.Transparent;
			this.ResumeLayout(false);
			this.PerformLayout();

        }

		#endregion

		private System.Windows.Forms.Label verLbl;
	}
}

