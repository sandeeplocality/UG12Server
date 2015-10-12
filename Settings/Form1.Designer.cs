namespace Settings
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.installServicesMenuStripItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuStripItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.servicesStartedLabel = new System.Windows.Forms.Label();
            this.servicesInstalledLabel = new System.Windows.Forms.Label();
            this.startServicesButton = new System.Windows.Forms.Button();
            this.stopServicesButton = new System.Windows.Forms.Button();
            this.saveSettingsButton = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.servicesGroup = new System.Windows.Forms.GroupBox();
            this.hrcService = new System.Windows.Forms.CheckBox();
            this.pop3Service = new System.Windows.Forms.CheckBox();
            this.loopService = new System.Windows.Forms.CheckBox();
            this.welfareCheckService = new System.Windows.Forms.CheckBox();
            this.taskSchedulerService = new System.Windows.Forms.CheckBox();
            this.gprsService = new System.Windows.Forms.CheckBox();
            this.fileMonitorService = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.servicesGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuStrip});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(360, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuStrip
            // 
            this.menuStrip.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.installServicesMenuStripItem,
            this.exitMenuStripItem});
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(37, 20);
            this.menuStrip.Text = "File";
            // 
            // installServicesMenuStripItem
            // 
            this.installServicesMenuStripItem.Name = "installServicesMenuStripItem";
            this.installServicesMenuStripItem.Size = new System.Drawing.Size(150, 22);
            this.installServicesMenuStripItem.Text = "Install Services";
            this.installServicesMenuStripItem.Click += new System.EventHandler(this.installServicesMenuStripItem_Click);
            // 
            // exitMenuStripItem
            // 
            this.exitMenuStripItem.Name = "exitMenuStripItem";
            this.exitMenuStripItem.Size = new System.Drawing.Size(150, 22);
            this.exitMenuStripItem.Text = "Exit";
            this.exitMenuStripItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.servicesStartedLabel);
            this.groupBox1.Controls.Add(this.servicesInstalledLabel);
            this.groupBox1.Location = new System.Drawing.Point(16, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(219, 68);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "UniGuard 12 Service Status";
            // 
            // servicesStartedLabel
            // 
            this.servicesStartedLabel.AutoSize = true;
            this.servicesStartedLabel.Location = new System.Drawing.Point(8, 42);
            this.servicesStartedLabel.Name = "servicesStartedLabel";
            this.servicesStartedLabel.Size = new System.Drawing.Size(173, 13);
            this.servicesStartedLabel.TabIndex = 1;
            this.servicesStartedLabel.Text = "UniGuard 12 Services are stopped.";
            // 
            // servicesInstalledLabel
            // 
            this.servicesInstalledLabel.AutoSize = true;
            this.servicesInstalledLabel.Location = new System.Drawing.Point(8, 25);
            this.servicesInstalledLabel.Name = "servicesInstalledLabel";
            this.servicesInstalledLabel.Size = new System.Drawing.Size(191, 13);
            this.servicesInstalledLabel.TabIndex = 0;
            this.servicesInstalledLabel.Text = "UniGuard 12 Services are not installed.";
            // 
            // startServicesButton
            // 
            this.startServicesButton.Location = new System.Drawing.Point(243, 38);
            this.startServicesButton.Name = "startServicesButton";
            this.startServicesButton.Size = new System.Drawing.Size(101, 26);
            this.startServicesButton.TabIndex = 2;
            this.startServicesButton.Text = "Start Services";
            this.startServicesButton.UseVisualStyleBackColor = true;
            this.startServicesButton.Click += new System.EventHandler(this.startServicesButton_Click);
            // 
            // stopServicesButton
            // 
            this.stopServicesButton.Location = new System.Drawing.Point(243, 70);
            this.stopServicesButton.Name = "stopServicesButton";
            this.stopServicesButton.Size = new System.Drawing.Size(101, 26);
            this.stopServicesButton.TabIndex = 3;
            this.stopServicesButton.Text = "Stop Services";
            this.stopServicesButton.UseVisualStyleBackColor = true;
            this.stopServicesButton.Click += new System.EventHandler(this.stopServicesButton_Click);
            // 
            // saveSettingsButton
            // 
            this.saveSettingsButton.Location = new System.Drawing.Point(248, 276);
            this.saveSettingsButton.Name = "saveSettingsButton";
            this.saveSettingsButton.Size = new System.Drawing.Size(96, 23);
            this.saveSettingsButton.TabIndex = 2;
            this.saveSettingsButton.Text = "Save Changes";
            this.saveSettingsButton.UseVisualStyleBackColor = true;
            this.saveSettingsButton.Click += new System.EventHandler(this.saveSettingsButton_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "UniGuard 12 Server";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // servicesGroup
            // 
            this.servicesGroup.Controls.Add(this.hrcService);
            this.servicesGroup.Controls.Add(this.pop3Service);
            this.servicesGroup.Controls.Add(this.loopService);
            this.servicesGroup.Controls.Add(this.welfareCheckService);
            this.servicesGroup.Controls.Add(this.taskSchedulerService);
            this.servicesGroup.Controls.Add(this.gprsService);
            this.servicesGroup.Controls.Add(this.fileMonitorService);
            this.servicesGroup.Location = new System.Drawing.Point(16, 104);
            this.servicesGroup.Name = "servicesGroup";
            this.servicesGroup.Size = new System.Drawing.Size(328, 166);
            this.servicesGroup.TabIndex = 5;
            this.servicesGroup.TabStop = false;
            this.servicesGroup.Text = "Services";
            // 
            // hrcService
            // 
            this.hrcService.AutoSize = true;
            this.hrcService.Location = new System.Drawing.Point(11, 97);
            this.hrcService.Name = "hrcService";
            this.hrcService.Size = new System.Drawing.Size(134, 17);
            this.hrcService.TabIndex = 10;
            this.hrcService.Text = "High Risk Checkpoints";
            this.hrcService.UseVisualStyleBackColor = true;
            // 
            // pop3Service
            // 
            this.pop3Service.AutoSize = true;
            this.pop3Service.Location = new System.Drawing.Point(166, 74);
            this.pop3Service.Name = "pop3Service";
            this.pop3Service.Size = new System.Drawing.Size(93, 17);
            this.pop3Service.TabIndex = 9;
            this.pop3Service.Text = "POP3 Service";
            this.pop3Service.UseVisualStyleBackColor = true;
            // 
            // loopService
            // 
            this.loopService.AutoSize = true;
            this.loopService.Location = new System.Drawing.Point(11, 74);
            this.loopService.Name = "loopService";
            this.loopService.Size = new System.Drawing.Size(94, 17);
            this.loopService.TabIndex = 8;
            this.loopService.Text = "Loops Service";
            this.loopService.UseVisualStyleBackColor = true;
            // 
            // welfareCheckService
            // 
            this.welfareCheckService.AutoSize = true;
            this.welfareCheckService.Location = new System.Drawing.Point(166, 27);
            this.welfareCheckService.Name = "welfareCheckService";
            this.welfareCheckService.Size = new System.Drawing.Size(150, 17);
            this.welfareCheckService.TabIndex = 7;
            this.welfareCheckService.Text = "Welfare Checking Service";
            this.welfareCheckService.UseVisualStyleBackColor = true;
            // 
            // taskSchedulerService
            // 
            this.taskSchedulerService.AutoSize = true;
            this.taskSchedulerService.Location = new System.Drawing.Point(166, 50);
            this.taskSchedulerService.Name = "taskSchedulerService";
            this.taskSchedulerService.Size = new System.Drawing.Size(140, 17);
            this.taskSchedulerService.TabIndex = 6;
            this.taskSchedulerService.Text = "Task Scheduler Service";
            this.taskSchedulerService.UseVisualStyleBackColor = true;
            // 
            // gprsService
            // 
            this.gprsService.AutoSize = true;
            this.gprsService.Location = new System.Drawing.Point(11, 50);
            this.gprsService.Name = "gprsService";
            this.gprsService.Size = new System.Drawing.Size(129, 17);
            this.gprsService.TabIndex = 5;
            this.gprsService.Text = "GPRS Server Service";
            this.gprsService.UseVisualStyleBackColor = true;
            // 
            // fileMonitorService
            // 
            this.fileMonitorService.AutoSize = true;
            this.fileMonitorService.Location = new System.Drawing.Point(11, 27);
            this.fileMonitorService.Name = "fileMonitorService";
            this.fileMonitorService.Size = new System.Drawing.Size(133, 17);
            this.fileMonitorService.TabIndex = 4;
            this.fileMonitorService.Text = "File Monitoring Service";
            this.fileMonitorService.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 311);
            this.Controls.Add(this.servicesGroup);
            this.Controls.Add(this.saveSettingsButton);
            this.Controls.Add(this.stopServicesButton);
            this.Controls.Add(this.startServicesButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "UniGuard 12 Server Settings";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.servicesGroup.ResumeLayout(false);
            this.servicesGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuStrip;
        private System.Windows.Forms.ToolStripMenuItem installServicesMenuStripItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuStripItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label servicesStartedLabel;
        private System.Windows.Forms.Label servicesInstalledLabel;
        private System.Windows.Forms.Button startServicesButton;
        private System.Windows.Forms.Button stopServicesButton;
        private System.Windows.Forms.Button saveSettingsButton;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.GroupBox servicesGroup;
        private System.Windows.Forms.CheckBox welfareCheckService;
        private System.Windows.Forms.CheckBox taskSchedulerService;
        private System.Windows.Forms.CheckBox gprsService;
        private System.Windows.Forms.CheckBox fileMonitorService;
        private System.Windows.Forms.CheckBox loopService;
        private System.Windows.Forms.CheckBox pop3Service;
        private System.Windows.Forms.CheckBox hrcService;
    }
}

