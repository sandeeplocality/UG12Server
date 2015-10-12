using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HighRiskCheckpoiont
{
    public class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => new HighRiskCheck());
        }

        protected override void OnStart(string[] args)
        {
            HighRiskCheck.running = true;
        }

        protected override void OnStop()
        {
            HighRiskCheck.running = false;
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12HighRiskCheckpoint";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12HighRiskCheckpoint";
        }
    }
}
