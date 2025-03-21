using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace NovaLauncher
{
    public partial class Uninstaller : UserControl
    {
        public Uninstaller()
        {
            InitializeComponent();
        }

        public void RemoveProtocolRegistryKeys()
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"Software\Classes\novaprotocol", true);

            if (key != null)
            {
                // Delete the key
                key.Close();
                Registry.ClassesRoot.DeleteSubKeyTree(@"Software\Classes\novaprotocol");
            }
        }

        public void DeleteUninstallerKeys()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\NovaLauncher", true);
            if (key != null)
            {
                // Delete the key
                key.Close();
                Registry.LocalMachine.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\NovaLauncher");
            }
        }

        public void PurgeInstallationDirectory()
        {
            // This removes all files and directories in the installation directory.
            // We must be careful, because the installer is also in here
            string installationDirectory = Config.installPath;
            string uninstallerPath = Config.installPath + "\\NovaLauncher.exe";

            try
            {
                // Get all files and directories in the installation directory
                var files = Directory.GetFiles(installationDirectory);
                var directories = Directory.GetDirectories(installationDirectory);

                // Delete all files except the uninstaller
                foreach (var file in files)
                {
                    if (file != uninstallerPath)
                    {
                        File.Delete(file);
                    }
                }

                // Delete all directories and their contents
                foreach (var directory in directories)
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error, show a message to the user)
                MessageBox.Show($"An error occurred while purging the installation directory:\n{ex.Message}\nError 120-0012", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
        }

        public void CompleteUninstallation()
        {
            string installPath = Config.installPath;
            string exePath = System.IO.Path.Combine(installPath, "NovaLauncher.exe");

            // Create a batch file to handle the uninstallation
            string batchFilePath = System.IO.Path.Combine(Path.GetTempPath(), Config.RevName + "Uninstaller.bat");

            string cmdCommand = $@"
@echo off
setlocal
set max_retries=10
set retry_count=0

:delete_exe
del /f /q ""{exePath}"" 2>nul
if %errorlevel% neq 0 (
    set /a retry_count+=1
    if %retry_count% geq %max_retries% (
        echo Failed to delete NovaLauncher.exe after %max_retries% retries.
        exit /b 1
    )
    ping -n 2 127.0.0.1 >nul
    goto delete_exe
)

:delete_directory
rmdir /s /q ""{installPath}"" 2>nul
if %errorlevel% neq 0 (
    echo Failed to delete the installation directory.
    exit /b 1
)

:delete_uninstaller
del /f /q ""{batchFilePath}"" 2>nul
if %errorlevel% neq 0 (
    echo Failed to delete the uninstaller batch file.
    exit /b 1
)

echo Uninstallation completed successfully.
exit /b 0
";

            // Write the batch file to disk
            try
            {
                File.WriteAllText(batchFilePath, cmdCommand);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create the cleanup file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Execute the batch file and exit the uninstaller
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFilePath}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                // Exit the uninstaller immediately
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start the cleanup file:\n {ex.Message}\nError 120-0013", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void EndRunningProcesses()
        {
            // Makes sure that the app is closed before uninstalling
            Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (Process process in processes)
            {
                process.Kill();
            }
        }

        private void Uninstaller_Shown(object sender, EventArgs e)
        {
            this.status.Text = "Uninstalling...";
            this.progressBar.Style = ProgressBarStyle.Marquee;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                EndRunningProcesses();
                RemoveProtocolRegistryKeys();
                PurgeInstallationDirectory();
                DeleteUninstallerKeys();
                MessageBox.Show("Uninstallation complete.", Config.RevName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                CompleteUninstallation();
            };
            worker.RunWorkerAsync();
        }
        private void Close()
        {
            if (this.ParentForm != null)
            {
                this.ParentForm.Close();
            }
        }
    }
}
