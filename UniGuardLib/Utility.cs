using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.ServiceProcess;
using System.Collections.Generic;

namespace UniGuardLib
{
    public static class Utility
    {
        private static TimeSpan timeout = TimeSpan.FromMilliseconds(5000);

        /// <summary>
        /// Return bool based upon wether a service is running or not
        /// </summary>
        /// <param name="serviceName">Name of service to check</param>
        /// <returns>Returns true if serviceName exists as a windows service or false otherwise</returns>
        public static bool ServiceExists(string serviceName)
        {
            // http://stackoverflow.com/questions/4554116/how-to-check-if-a-windows-service-is-installed-in-c-sharp
            ServiceController service = ServiceController.GetServices().Where(
                s => s.ServiceName == serviceName
            ).FirstOrDefault();
            return service == null ? false : true;
        }

        /// <summary>
        /// Checks to see if a service is running or not, and returns a boolean.
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <returns>Returns true if service is running or false if it is not.</returns>
        public static bool ServiceRunning(string serviceName)
        {
            ServiceController sc = new ServiceController(serviceName);
            try
            {
                switch (sc.Status)
                {
                    case ServiceControllerStatus.ContinuePending:
                    case ServiceControllerStatus.Stopped:
                    case ServiceControllerStatus.Paused:
                    case ServiceControllerStatus.StopPending:
                    case ServiceControllerStatus.PausePending:
                        return false;

                    default:
                        return true;

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Safely start service
        /// </summary>
        /// <param name="serviceName">Service name to start</param>
        public static void StartService(string serviceName)
        {
            ServiceController sc = new ServiceController(serviceName);
            try
            {
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Stopped:
                    case ServiceControllerStatus.Paused:
                        // Start it
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }

        /// <summary>
        /// Safely stops a service
        /// </summary>
        /// <param name="serviceName">Service name to be stopped</param>
        public static void StopService(string serviceName)
        {
            ServiceController sc = new ServiceController(serviceName);
            try
            {
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        // Stop it
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Service error (" + serviceName + ")\r\n:" + ex.ToString());
            }
        }

        /// <summary>
        /// Returns the total number of lines with context in a text file. Empty lines
        /// will not add to the incrementing counter so will not be counted.
        /// </summary>
        /// <param name="filePath">Full file name and path</param>
        /// <returns>Number of lines</returns>
        public static int TotalLines(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                int i = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Do not count empty lines
                    if (line != String.Empty) { i++; }
                }
                reader.Close();
                return i;
            }
        }

        /// <summary>
        /// Parses a date using timecycle_id values and adds the amount of time.
        /// </summary>
        /// <param name="dateTime">String representation of MySQL datetime stamp</param>
        /// <param name="timecycleId">Integer, timecycleId</param>
        /// <returns>Returns a string representation of the parsed date.</returns>
        public static string AddTimeCycle(string dateTime, int timecycleId)
        {
            DateTime dt = Convert.ToDateTime(dateTime);
            DateTime output;

            switch (timecycleId)
            {
                case 1:  output = dt.AddDays(1);   break;
                case 2:  output = dt.AddDays(7);   break;
                case 3:  output = dt.AddDays(14);  break;
                case 4:  output = dt.AddMonths(1); break;
                case 5:  output = dt.AddMonths(3); break;
                default: output = dt;              break;
            }

            return output.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Turns a hexadecimal to an integer
        /// </summary>
        /// <param name="hx"></param>
        /// <returns></returns>
        public static string HexToDec(string hx)
        {
            int output;
            try
            {
                output = int.Parse(hx, NumberStyles.HexNumber);
            }
            catch (OverflowException)
            {
                try
                {
                    // If overflow exception caught, use only last 6 digits of number
                    output = int.Parse(hx.Substring((hx.Length - 6), 6), NumberStyles.HexNumber);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format("Hex parse error ({0}){1}{2}", hx.ToString(), Environment.NewLine, ex.ToString())
                    );
                }
            }

            return output.ToString();
        }

        /// <summary>
        /// Returns an array of strings containing paths to each of the directories inside the
        /// path passed to the method.
        /// </summary>
        /// <param name="path">Windows path to check</param>
        /// <returns>Returns all paths to subdirectories</returns>
        public static string[] GetAllDirectories(string path)
        {
            // Declare a list, then put an empty array in it
            var listOfDirectories = new List<string>();
            string[] directories = listOfDirectories.ToArray();

            // Try and get all the directories in the specified path, catch exceptions
            try
            {
                directories = Directory.GetDirectories(path);
            }
            catch (Exception ex)
            {
                // Log to Windows Events
                Log.EventError(ex.Message);
            }

            return directories;
        }

        /// <summary>
        /// Returns all the files .uef & .export from a directory of choice
        /// </summary>
        /// <param name="path">Path to scan for files</param>
        /// <param name="extensions">Array with all the extensions to look for</param>
        /// <returns>Returns array of FileInfo objects</returns>
        public static FileInfo[] GetallFilesFromDirectory(string path, string[] extensions = null)
        {
            // Declare a list for the files
            var files = new List<FileInfo>();

            // Go to the directory, if it does not exist, exit out
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists) dir.Create();

            // If extensions is null, check them all
            if (extensions == null) extensions = new string[] { "*.*" };

            // Loop over extensions
            foreach (string ext in extensions)
            {
                // Get files that match the extension required
                foreach (FileInfo file in dir.GetFiles(ext))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }

        /// <summary>
        /// Returns the datetime of Now (with the gmtOffset)
        /// </summary>
        /// <param name="gmtOffset">GMT Offset in seconds</param>
        /// <returns>Returns a DateTime object</returns>
        public static DateTime Now(double gmtOffset = 0)
        {
            return DateTime.UtcNow.AddSeconds(gmtOffset);
        }

        public static bool IsFileReady(String sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
