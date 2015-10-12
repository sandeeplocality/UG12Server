using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ScheduledTask
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => new ScheduledTask());
        }

        protected override void OnStart(string[] args)
        {
            ScheduledTask.running = true;
        }

        protected override void OnStop()
        {
            ScheduledTask.running = false;
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12ScheduledTask";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12ScheduledTask";
        }
    }
}
