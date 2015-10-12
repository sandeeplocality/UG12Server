using System;
using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperSocket.SocketEngine.Configuration;

namespace UniGuard12Server
{
    public class MyService : ServiceBase
    {

        public MyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Task.Factory.StartNew(() => ServiceManager.Start());
            this.EventLog.WriteEntry("UniGuard 12 Server started.");
        }

        protected override void OnStop()
        {
            Task.Factory.StartNew(() => ServiceManager.Stop());
            this.EventLog.WriteEntry("UniGuard 12 Server stopped.");
        }

        private void InitializeComponent()
        {
            this.ServiceName = "UniGuard12Server";
            this.CanStop = true;
            this.AutoLog = false;
            this.EventLog.Log = "Application";
            this.EventLog.Source = "UniGuard12Server";
        }

    }

}
