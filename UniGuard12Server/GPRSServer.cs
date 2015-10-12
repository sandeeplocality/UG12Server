using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketEngine.Configuration;
using System.Configuration;
using System.Timers;
using System.ServiceProcess;
using UniGuardLib;

namespace UniGuard12Server
{
    public class GPRSServer : AppServer<GPRSSession, BinaryCommandInfo>
    {
        private Timer timer;

        public GPRSServer() : base(new GPRSCustomProtocol())
        {
            // Let's make sure that UniGuard 12 Server is running
            timer = new Timer(500);
            timer.Elapsed += new ElapsedEventHandler(this.OnTimedEvent);
            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12GPRS"))
                {
                    ServiceController sc = new ServiceController("UniGuard12GPRS");
                    sc.Stop();
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions in the stack
                Log.Error("GPRS Server error:\r\n" + ex.ToString());
            }
            finally
            {
                timer.Start();
            }
        }

    }
}
