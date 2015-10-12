using System;
using System.Threading.Tasks;
using System.Timers;
using System.ServiceProcess;
using UniGuardLib;
using FileMonitoring;

namespace UniGuard12Server
{
    public static class ServiceManager
    {
        // Set the running state
        private static bool running = false;
        // Set timer element statically
        private static Timer timer;

        /// <summary>
        /// Start the services process
        /// </summary>
        public static void Start()
        {
            ServiceManager.running = true;
            // Loop over all the services
            if (Services.ServiceList() != null)
            {
                foreach (string service in Services.ServiceList())
                {
                    if (Utility.ServiceExists(service))
                        Task.Factory.StartNew(() => Utility.StartService(service));
                    else
                        Log.Error("The service " + service + " is not installed on this machine.");
                }
                // Make sure all services are kept alive
                Task.Factory.StartNew(() => KeepServicesAlive());
            }
        }

        /// <summary>
        /// Stop the services process
        /// </summary>
        public static void Stop()
        {
            // Now tag the service manager as not running
            ServiceManager.running = false;
            // Stop all services
            if (Services.ServiceList() != null)
            {
                foreach (string service in Services.ServiceList())
                {
                    if (Utility.ServiceExists(service))
                        Task.Factory.StartNew(() => Utility.StopService(service));
                    else
                        Log.Error("The service " + service + " is not installed on this machine.");
                }
            }
        }

        /// <summary>
        /// Ensures that mission critical services are kept alive
        /// </summary>
        private static void KeepServicesAlive()
        {
            timer = new Timer(5000);
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Enabled = true;
        }

        /// <summary>
        /// This is the timer unit for the KeepServicesAlive method
        /// </summary>
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();

                foreach (string service in Services.AllServices())
                {
                    if (Array.IndexOf(Services.ServiceList(), service) != -1)
                    {
                        // Run stopped services which are active
                        if (!Utility.ServiceRunning(service) && ServiceManager.running)
                            Utility.StartService(service);
                    }

                    // Stop running services which are inactive
                    else
                    {
                        if (Utility.ServiceRunning(service) && ServiceManager.running)
                            Utility.StopService(service);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions in the stack
                Log.Error("Task monitoring error:\r\n" + ex.ToString());
            }
            finally
            {
                if (ServiceManager.running) timer.Start();
            }
        }
    }
}
