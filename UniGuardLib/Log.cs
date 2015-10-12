using System;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;

namespace UniGuardLib
{
    public static class Log
    {
        private const string LOGPATH     = @"C:\HostingSpaces\logs\";
        private const string EVENTSOURCE = "UniGuard 12 Server";
        private const string EVENTLOG    = "Application";

        /// <summary>
        /// Attempts to write to a log file using StreamWriter
        /// </summary>
        /// <param name="logFile">File to write to, with full path</param>
        /// <param name="output">Text to output to log</param>
        private static void WriteToLog(string logFile, string output)
        {
            DirectoryInfo dir = new DirectoryInfo(LOGPATH);
            if (!dir.Exists) dir.Create();
            
            try
            {
                // Write the string to the file
                StreamWriter file = new StreamWriter(logFile, true);
                file.WriteLine(Log.Timestamp() + output + "\r\n");
                file.Close();
            }
            catch (IOException)
            {
                // The file exists and is read-only, or the disk may be full: Write to the event log with an error.
                Log.EventError("Could not write to log file. It may be read-only or the disk might be full.");
            }
        }

        /// <summary>
        /// Returns the current date/time stamp as a string header
        /// </summary>
        /// <returns>Returns a string with today's date/time as a header</returns>
        private static string Timestamp()
        {
            return DateTime.Now.ToString() + ":\r\n=======================\r\n";
        }

        /// <summary>
        /// Write to the error.log file
        /// </summary>
        /// <param name="message">Message to output</param>
        public static void Error(string message)
        {
            string logFile = LOGPATH + "error.log";
            Log.WriteToLog(logFile, message);
        }

        /// <summary>
        /// Write to the information.log file
        /// </summary>
        /// <param name="message">Message to output</param>
        public static void Info(string message)
        {
            string logFile = LOGPATH + "information.log";
            Log.WriteToLog(logFile, message);
        }

        /// <summary>
        /// Write to the warning.log file
        /// </summary>
        /// <param name="message">Message to output</param>
        public static void Warning(string message)
        {
            string logFile = LOGPATH + "warning.log";
            Log.WriteToLog(logFile, message);
        }

        /// <summary>
        /// Write to the testing.log file
        /// </summary>
        /// <param name="testTitle">Title for the test</param>
        /// <param name="message">Message to output</param>
        public static void Test(string testTitle, string message)
        {
            string logFile = LOGPATH + "testing.log";
            Log.WriteToLog(logFile, testTitle.ToUpper() + ": " + Environment.NewLine + message);
        }

        /// <summary>
        /// Write to the Application events log with a warning type
        /// </summary>
        /// <param name="message">Message for the Events Viewer</param>
        public static void EventWarning(string message)
        {
            EventLog.WriteEntry(EVENTSOURCE, message, EventLogEntryType.Warning);
        }

        /// <summary>
        /// Write to the Application events log with an error type
        /// </summary>
        /// <param name="message">Message for the Events Viewer</param>
        public static void EventError(string message)
        {
            EventLog.WriteEntry(EVENTSOURCE, message, EventLogEntryType.Error);
        }

        /// <summary>
        /// Write to the Application events log with an information type
        /// </summary>
        /// <param name="message">Message for the Events Viewer</param>
        public static void EventInfo(string message)
        {
            EventLog.WriteEntry(EVENTSOURCE, message, EventLogEntryType.Information);
        }

    }
}
