using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.ServiceProcess;
using UniGuardLib;

namespace SiteWelfareChecking
{
    class SiteWelfareChecking
    {
        public static bool running;
        private static Timer timer;

        public SiteWelfareChecking()
        {
            running = false;
            // Set up the interval timer and start it
            timer = new Timer();
            timer.Interval  = 2000;
            timer.Elapsed  += new ElapsedEventHandler(this.OnTimedEvent);
            timer.AutoReset = false;
            timer.Enabled   = true;
        }

        /// <summary>
        /// This is the timer tick event for the timer loop
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12SiteWelfare"))
                {
                    ServiceController sc = new ServiceController("UniGuard12SiteWelfare");
                    sc.Stop();
                }
                else
                {
                    if (running) return;
                    running = true;
                    // Run the method
                    this.MonitorSites();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Welfare checking error: " + ex.ToString());
            }
            finally
            {
                running = false;
                // Restart the timer
                timer.Start();
            }
        }

        /// <summary>
        /// Loops over databases and performs welfare checks on sites
        /// </summary>
        private void MonitorSites()
        {
            string[] databases = LocalData.GetAllDatabases();

            // Loop over all databases
            for (int i = 0; i < databases.Length; ++i)
            {
                SiteWelfareData welfare = new SiteWelfareData(databases[i]);
                welfare.Run();
                System.Threading.Thread.Sleep(100);
            }
        }

    }
}
