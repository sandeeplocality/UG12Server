using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using UniGuardLib;
using System.Threading;

namespace Settings
{
    public partial class Form1 : Form
    {

        private bool installed;
        private bool started;

        public Form1()
        {
            InitializeComponent();
            this.CheckServices();
            this.CheckSettings();
            this.Resize += new EventHandler(Form1_Resize);
        }

        /// <summary>
        /// Hide to system tray and show notify icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.notifyIcon1.Visible = true;
                this.Hide();
            }
            else if (WindowState == FormWindowState.Normal)
            {
                this.notifyIcon1.Visible = false;
                this.Show();
            }
        }

        /// <summary>
        /// Check services and adjust labels, buttons, etc.
        /// </summary>
        private void CheckServices()
        {
            this.installed = true;
            this.started   = true;

            try
            {
                // Check that services are installed
                foreach (string service in Services.AllServices())
                {
                    // Check if its installed
                    if (!Utility.ServiceExists(service))
                    {
                        this.installed = false;
                    }
                }

                // Check if installed services are running
                if (this.installed && Services.ServiceList() != null)
                {
                    foreach (string service in Services.ServiceList())
                    {
                        // Check if its running
                        if (!Utility.ServiceRunning(service))
                        {
                            this.started = false;
                        }
                    }
                }
                else
                {
                    this.started = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error checking services:\r\n" + ex.ToString());
            }

            // Check that services are installed
            if (!installed)
            {
                this.servicesInstalledLabel.ForeColor = Color.Red;
                this.servicesInstalledLabel.Text = "UniGuard 12 Services are not installed.";
                this.installServicesMenuStripItem.Text = "Install Services";
            }
            if (installed)
            {
                this.servicesInstalledLabel.ForeColor = Color.Green;
                this.servicesInstalledLabel.Text = "UniGuard 12 Services are installed.";
                this.installServicesMenuStripItem.Text = "Uninstall Services";
            }
            if (!started)
            {
                this.servicesStartedLabel.ForeColor = Color.Red;
                this.servicesStartedLabel.Text = "UniGuard 12 Services are stopped.";
                this.startServicesButton.Enabled = !installed ? false : true;
                this.stopServicesButton.Enabled = false;
            }
            if (started)
            {
                this.servicesStartedLabel.ForeColor = Color.Green;
                this.servicesStartedLabel.Text = "UniGuard 12 Services are running.";
                this.startServicesButton.Enabled = false;
                this.stopServicesButton.Enabled = !installed ? false : true;
            }
        }

        /// <summary>
        /// Checks checkboxes if settings are set.
        /// </summary>
        private void CheckSettings()
        {
            // Check Services
            fileMonitorService.Checked    = RegEdit.Read("FILEMONITOR") == null ? false : true;
            gprsService.Checked           = RegEdit.Read("GPRS") == null ? false : true;
            taskSchedulerService.Checked  = RegEdit.Read("TASKSCHEDULER") == null ? false : true;
            welfareCheckService.Checked   = RegEdit.Read("WELFARECHECK") == null ? false : true;
            loopService.Checked           = RegEdit.Read("SITELOOP") == null ? false : true;
            pop3Service.Checked           = RegEdit.Read("POP3") == null ? false : true;
            hrcService.Checked            = RegEdit.Read("HIGHRISK") == null ? false : true;
        }

        /// <summary>
        /// Checks to see if services are running and returns true or false
        /// </summary>
        /// <returns>True if services are installed and running, false if not.</returns>
        private bool ServicesRunning()
        {
            if (Services.ServiceList() != null)
            {
                foreach (string service in Services.ServiceList())
                {
                    // Check services
                    if (!Utility.ServiceExists(service)) return false;
                    if (!Utility.ServiceRunning(service)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Exit application
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Install or uninstall services
        /// </summary>
        private void installServicesMenuStripItem_Click(object sender, EventArgs e)
        {
            if (this.installed)
            {
                // Check if services are running
                if (this.ServicesRunning())
                    MessageBox.Show("Please stop the services first, before uninstalling them.");
                // Only uninstall if they are not running
                if (!this.started)
                {
                    // Change cursor
                    Cursor.Current = Cursors.WaitCursor;
                    // Loop over service paths
                    foreach (string service in Services.servicePaths)
                    {
                        ProcessStartInfo start = new ProcessStartInfo();
                        start.CreateNoWindow = true;
                        start.WindowStyle = ProcessWindowStyle.Hidden;
                        start.FileName = service;
                        start.Arguments = "-u";
                        start.UseShellExecute = false;
                        start.ErrorDialog = false;
                        Process process = new Process();
                        process.StartInfo = start;
                        process.Start();
                        process.WaitForExit();
                    }
                    this.CheckServices();
                    // Change cursor back
                    Cursor.Current = Cursors.Default;
                    return;
                }
            }
            if (!this.installed)
            {
                // Change cursor
                Cursor.Current = Cursors.WaitCursor;
                // Loop over service paths
                foreach (string service in Services.servicePaths)
                {
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.CreateNoWindow = true;
                    start.WindowStyle = ProcessWindowStyle.Hidden;
                    start.FileName = service;
                    start.Arguments = "-i";
                    start.UseShellExecute = false;
                    start.ErrorDialog = false;
                    Process process = new Process();
                    process.StartInfo = start;
                    process.Start();
                    process.WaitForExit();
                }
                this.CheckServices();
                // Change cursor back
                Cursor.Current = Cursors.Default;
                return;
            }
        }

        /// <summary>
        /// Start all services
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startServicesButton_Click(object sender, EventArgs e)
        {
            // Check if no services available
            if (Services.ServiceList() == null)
            {
                MessageBox.Show(
                    "No services available to start, please check the options under Services.",
                    "No Services Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            bool servicesStarted = false;
            // Change cursor
            Cursor.Current = Cursors.WaitCursor;
            Utility.StartService("UniGuard12Server");
            // Let's check the services for the next 10 seconds
            for (int i = 0; i < 10; ++i)
            {
                if (this.ServicesRunning())
                {
                    servicesStarted = true;
                    continue;
                }
                Thread.Sleep(1000);
            }
            // Change cursor back
            Cursor.Current = Cursors.Default;
            if (servicesStarted) this.CheckServices();
        }

        /// <summary>
        /// Stop all services
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopServicesButton_Click(object sender, EventArgs e)
        {
            bool servicesStarted = true;
            // Change cursor
            Cursor.Current = Cursors.WaitCursor;
            Utility.StopService("UniGuard12Server");
            // Let's check the services for the next 10 seconds
            for (int i = 0; i < 10; ++i)
            {
                if (!this.ServicesRunning())
                {
                    servicesStarted = false;
                    continue;
                }
                Thread.Sleep(1000);
            }
            // Change cursor back
            Cursor.Current = Cursors.Default;
            if (!servicesStarted) this.CheckServices();
        }

        /// <summary>
        /// Save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveSettingsButton_Click(object sender, EventArgs e)
        {
            // Adjust file monitoring
            if (fileMonitorService.CheckState == CheckState.Checked)
                RegEdit.Write("FILEMONITOR", true);
            else
                if (RegEdit.Read("FILEMONITOR") != null)
                    RegEdit.Delete("FILEMONITOR");

            // Adjust gprs server
            if (gprsService.CheckState == CheckState.Checked)
                RegEdit.Write("GPRS", true);
            else
                if (RegEdit.Read("GPRS") != null)
                    RegEdit.Delete("GPRS");

            // Adjust task scheduler
            if (taskSchedulerService.CheckState == CheckState.Checked)
                RegEdit.Write("TASKSCHEDULER", true);
            else
                if (RegEdit.Read("TASKSCHEDULER") != null)
                    RegEdit.Delete("TASKSCHEDULER");

            // Adjust welfare check
            if (welfareCheckService.CheckState == CheckState.Checked)
                RegEdit.Write("WELFARECHECK", true);
            else
                if (RegEdit.Read("WELFARECHECK") != null)
                    RegEdit.Delete("WELFARECHECK");

            // Adjust Site Loop
            if (loopService.CheckState == CheckState.Checked)
                RegEdit.Write("SITELOOP", true);
            else
                if (RegEdit.Read("SITELOOP") != null)
                    RegEdit.Delete("SITELOOP");

            // Adjust Pop 3
            if (pop3Service.CheckState == CheckState.Checked)
                RegEdit.Write("POP3", true);
            else
                if (RegEdit.Read("POP3") != null)
                    RegEdit.Delete("POP3");

            // Adjust HighRisk Checkpoints
            if (hrcService.CheckState == CheckState.Checked)
                RegEdit.Write("HIGHRISK", true);
            else
                if (RegEdit.Read("HIGHRISK") != null)
                    RegEdit.Delete("HIGHRISK");

            // Change setting
            MessageBox.Show("Settings changed successfully!");
        }

        /// <summary>
        /// Show form again on notifyIcon double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

    }
}
