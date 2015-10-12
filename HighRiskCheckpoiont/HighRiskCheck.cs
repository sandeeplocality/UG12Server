using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UniGuardLib;

namespace HighRiskCheckpoiont
{
    class HighRiskCheck
    {
        public static bool running;
        private static Timer timer;

        public HighRiskCheck()
        {
            running = false;
            // Set up the interval timer and start it
            timer = new Timer();

            timer.Interval = 2000;
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
                timer.Stop();


                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12HighRiskCheckpoint"))
                {
                    ServiceController sc = new ServiceController("UniGuard12HighRiskCheckpoint");
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
                Log.Error("High Risk Checkpoint Error: " + ex.ToString());
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
            //string[] databases = LocalData.GetAllDatabases();
            //NGVr0U1bBJ8vnAaFcgw9 geo group
            string[] databases = new string[] { "unidemodb", "NGVr0U1bBJ8vnAaFcgw9" };

            foreach(string dbName in databases)
            {
                HighRiskData highrisk = new HighRiskData(dbName);
                highrisk.RetrieveData();

                //only process if data is found
                if (highrisk.HighRiskDataListCount != 0)
                {
                    highrisk.ProcessData();
                }
            }

        }

    }
}
