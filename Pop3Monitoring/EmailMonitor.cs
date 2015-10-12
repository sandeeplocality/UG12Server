using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Text.RegularExpressions;
using UniGuardLib;
using AE.Net.Mail;
using AE.Net.Mail.Imap;

namespace Pop3Monitoring
{
    public class EmailMonitor
    {
        public static bool running;
        private static System.Timers.Timer timer;

        private static string Host = "mail.valutronics.com.au";
        private static string UserName = "uniguard.exports@valutronics.com.au";
        private static string Password = "Un1GuardExp0rt5!";
        private static int Port = 143;
        private static string ImportPath = @"C:\HostingSpaces\Portal\imports\";

        public EmailMonitor()
        {
            running = false;
            // Set up the interval timer and start it
            timer = new System.Timers.Timer();
            timer.Interval = 10000;
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
                if (!Utility.ServiceRunning("UniGuard12Server") && Utility.ServiceRunning("UniGuard12Pop3"))
                {
                    ServiceController sc = new ServiceController("UniGuard12Pop3");
                    sc.Stop();
                }
                else
                {
                    if (running) return;
                    running = true;

                    // Run the method
                    this.MonitorPop3();
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
        /// This is the main method of this class, it monitors the incoming emails from
        /// EXPORTEMAIL. The Uploader software on client machines will email export files.
        /// </summary>
        private void MonitorPop3()
        {
            string dbName      = null;
            string[] databases = LocalData.GetAllDatabases();
            bool match;

            try
            {
                MailMessage[] messages = GetEmails();

                foreach (MailMessage message in messages)
                {
                    // Start match as false
                    match = false;

                    // Check if subject contains database string
                    string[] subjectArray = message.Subject.Split(' ');
                    foreach (string str in subjectArray)
                    {
                        foreach (string db in databases)
                        {
                            if (db.ToLower() == str.ToLower())
                            {
                                match  = true;
                                dbName = db;
                            }
                        }
                    }

                    // Get the message itself
                    if (match)
                    {
                        MailMessage msg = GetMessage(message.Uid);

                        // Get attachments
                        int attachmentCount = msg.Attachments.Count;
                        foreach (Attachment attachment in msg.Attachments)
                        {
                            string saveDir = ImportPath + dbName + @"\";
                            string savePath = saveDir + attachment.Filename;

                            // Create directory if it does not exist
                            DirectoryInfo dir = new DirectoryInfo(saveDir);
                            if (!dir.Exists)
                                dir.Create();

                            attachment.Save(savePath);
                        }

                        // Delete message
                        DeleteMessage(msg.Uid);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error("Email Monitoring Error: " + ex.Message);
            }
        }

        private static MailMessage[] GetEmails()
        {
            List<MailMessage> response = new List<MailMessage>();

            using (var imap = new ImapClient(Host, UserName, Password, ImapClient.AuthMethods.Login, Port, false))
            {
                imap.SelectMailbox("INBOX");

                // Get message count
                var messageCount = imap.GetMessageCount();

                if (messageCount == 0)
                    return response.ToArray();

                var msgs = imap.GetMessages(0, (messageCount - 1)).ToArray();

                foreach (MailMessage msg in msgs)
                {
                    var flags = msg.Flags.ToString();
                    if (!flags.Contains("Deleted"))
                        response.Add(msg);
                }
            }

            return response.ToArray();
        }

        private static MailMessage GetMessage(string uid)
        {
            MailMessage response = new MailMessage();

            using (var imap = new ImapClient(Host, UserName, Password, ImapClient.AuthMethods.Login, Port, false))
            {
                response = imap.GetMessage(uid, false);
            }

            return response;
        }

        private static void DeleteMessage(string uid)
        {
            using (var imap = new ImapClient(Host, UserName, Password, ImapClient.AuthMethods.Login, Port, false))
            {
                imap.DeleteMessage(uid);
            }
        }
    }
}
