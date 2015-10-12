using System;
using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration.Install;

namespace HighRiskCheckpoint
{
    [RunInstaller(true)]
    public class MyServiceInstaller : Installer
    {
        private ServiceProcessInstaller ServiceProcessInstaller1;
        private ServiceInstaller ServiceInstaller1;

        public MyServiceInstaller()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.ServiceProcessInstaller1 = new ServiceProcessInstaller();
            this.ServiceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller1.Username = null;
            this.ServiceProcessInstaller1.Password = null;
            this.ServiceInstaller1 = new ServiceInstaller();
            this.ServiceInstaller1.Description = "The UniGuard 12 High Risk Checkpoint Service scans all checkpoints which are flagged as high risk and active and checks that they are being checked constantly.";
            this.ServiceInstaller1.DisplayName = "UniGuard 12 High Risk Checkpoint Service";
            this.ServiceInstaller1.ServiceName = "UniGuard12HighRiskCheckpoint";
            this.ServiceInstaller1.StartType = ServiceStartMode.Manual;
            this.Installers.AddRange(new Installer[] { this.ServiceProcessInstaller1, this.ServiceInstaller1 });
        }
    }
}
