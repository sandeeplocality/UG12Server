using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SiteWelfareChecking
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => new SiteWelfareChecking());
        }

        protected override void OnStart(string[] args)
        {
            SiteWelfareChecking.running = true;
        }

        protected override void OnStop()
        {
            SiteWelfareChecking.running = false;
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12SiteWelfare";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12SiteWelfare";
        }
    }
}
