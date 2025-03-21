using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NovaLauncher
{
    public partial class InstallCompleted: UserControl
    {
        public InstallCompleted()
        {
            InitializeComponent();
        }

        public void cancelButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void status_Click(object sender, EventArgs e)
        {

        }

        private void instructions_Click(object sender, EventArgs e)
        {

        }
    }
}
