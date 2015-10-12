using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace UniGuardLib
{
    public static class Services
    {
        private static List<string> servicesList;

        // List available services here, these can be switched off from the Settings app
        public static string[] ServiceList()
        {
            // Initialize it
            servicesList = new List<string>();

            // Add the necessary rows
            if (RegEdit.Read("FILEMONITOR") != null)    servicesList.Add("UniGuard12FileMonitoring");
            if (RegEdit.Read("TASKSCHEDULER") != null)  servicesList.Add("UniGuard12ScheduledTask");
            if (RegEdit.Read("GPRS") != null)           servicesList.Add("UniGuard12GPRS");
            if (RegEdit.Read("WELFARECHECK") != null)   servicesList.Add("UniGuard12SiteWelfare");
            if (RegEdit.Read("SITELOOP") != null)       servicesList.Add("UniGuard12SiteLoop");
            if (RegEdit.Read("POP3") != null)           servicesList.Add("UniGuard12Pop3");
            if (RegEdit.Read("HIGHRISK") != null)       servicesList.Add("UniGuard12HighRiskCheckpoint");

            return servicesList.Count > 0 ? servicesList.ToArray() : null;
        }
        
        // List all the dependent services here
        public static string[] AllServices()
        {
            return new string[] {
                "UniGuard12GPRS",
                "UniGuard12FileMonitoring",
                "UniGuard12ScheduledTask",
                "UniGuard12SiteWelfare",
                "UniGuard12SiteLoop",
                "UniGuard12Pop3",
                "UniGuard12HighRiskCheckpoint"
            };
        }

        // List all paths to services
        public static string[] servicePaths = {
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\UniGuard12Server.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FileMonitoring.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\SiteWelfareChecking.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\SuperSocket.SocketService.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ScheduledTask.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\SiteLoop.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Pop3Monitoring.exe",
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\HighRiskCheckpoiont.exe"
        };
    }
}
