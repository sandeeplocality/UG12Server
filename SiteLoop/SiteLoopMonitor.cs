using System;
using System.IO;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UniGuardLib;

namespace SiteLoop
{
    class SiteLoopMonitor
    {
        public static bool running;
        private static System.Timers.Timer timer;

        public SiteLoopMonitor()
        {
            running = false;
            // Set up the interval timer and start it
            timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimedEvent);
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        /// <summary>
        /// This is the timer tick event for the timer loop
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                // Stop the timer
                timer.Stop();
                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12SiteLoop"))
                {
                    ServiceController sc = new ServiceController("UniGuard12SiteLoop");
                    sc.Stop();
                }
                else
                {
                    if (running) return;
                    running = true;

                    // Run the method
                    this.MonitorLoops();
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions in the stack
                Log.Error(ex.ToString());
            }
            finally
            {
                running = false;
                // Restart the timer
                timer.Start();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void MonitorLoops()
        {
            string[] databases = LocalData.GetAllDatabases();

            // Loop over all databases
            for (int i = 0; i < databases.Length; ++i)
            {
                SiteLoop loop = new SiteLoop(databases[i]);
                loop.Run();
                System.Threading.Thread.Sleep(100);
            }
        }

    }
}
