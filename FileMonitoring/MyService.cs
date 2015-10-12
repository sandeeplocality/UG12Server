using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FileMonitoring
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            // Start the 
            Task.Factory.StartNew(() => new FileMonitor());
        }

        protected override void OnStart(string[] args)
        {
            FileMonitor.running = true;
        }

        protected override void OnStop()
        {
            FileMonitor.running = false;
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12FileMonitoring";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12FileMonitoring";
        }
    }
}
