using System;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using UniGuardLib;

namespace HighRiskCheckpoiont
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "-i":
                            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                            break;

                        case "-u":
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                            break;
                    }
                }
            }
            else
            {
                ServiceBase.Run(new MyService());
            }
        }
    }
}
