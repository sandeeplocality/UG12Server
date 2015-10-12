using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pop3Monitoring
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => new EmailMonitor());
        }

        protected override void OnStart(string[] args)
        {
            this.EventLog.WriteEntry("MyService Service Has Started");
        }

        protected override void OnStop()
        {
            this.EventLog.WriteEntry("MyService Service Has Stopped");
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12Pop3";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "Uniguard12Pop3";
        }
    }
}
