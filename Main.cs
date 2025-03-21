using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Threading;
using Newtonsoft.Json;
namespace NovaLauncher
{
    public partial class InstallerGUI : Form
    {
        private static Panel panelContainer; // Add this line to declare panelContainer

        public InstallerGUI()
        {
            InitializeComponent();
            panelContainer = new Panel(); // Initialize panelContainer
            panelContainer.Dock = DockStyle.Fill; // Ensure the panel fills the form
            this.Controls.Add(panelContainer); // Add the panel to the form's controls
            string[] args = Environment.GetCommandLineArgs();

            CLIArgs options = new CLIArgs();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                MessageBox.Show("ya dun goofed up the poor poor arguments :(");
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
            if (options.Uninstall)
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
