using System;
using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration.Install;

namespace SiteWelfareChecking
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
            this.ServiceInstaller1.Description = "The UniGuard 12 Site Welfare Checking Service continuously scans the UniGuard databases in attempts to find exceptions to alloted site welfare rules.";
            this.ServiceInstaller1.DisplayName = "UniGuard 12 Site Welfare Checking Service";
            this.ServiceInstaller1.ServiceName = "UniGuard12SiteWelfare";
            this.ServiceInstaller1.StartType = ServiceStartMode.Manual;
            this.Installers.AddRange(new Installer[] { this.ServiceProcessInstaller1, this.ServiceInstaller1 });
        }
    }
}
