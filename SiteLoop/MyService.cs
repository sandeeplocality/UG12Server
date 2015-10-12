using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SiteLoop
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => new SiteLoopMonitor());
        }

        protected override void OnStart(string[] args)
        {
            SiteLoopMonitor.running = true;
        }

        protected override void OnStop()
        {
            SiteLoopMonitor.running = false;
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12SiteLoop";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12SiteLoop";
        }
    }
}
