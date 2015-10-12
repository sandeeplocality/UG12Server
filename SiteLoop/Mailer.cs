using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using UniGuardLib;

namespace SiteLoop
{
    public class Mailer
    {
        // Mail details
        private string[] recipients;
        private string fromAddress = "no.reply@uniguard.com.au";
        private string smtpHost = "smtp.gmail.com";
        private string smtpUser = "no.reply@uniguard.com.au";
        private string smtpPassword = "B6gYu9uGtr4xuG1!";
        private bool preWarning = false;

        public Mailer(bool isPreWarning)
        {
            this.preWarning = isPreWarning;
        }

        public List<string> Recipients
        {
            set { this.recipients = value.ToArray(); }
        }

        public List<MissingCheckpoint> MissingCheckpoints { get; set; }

        public bool sendMail()
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtp = new SmtpClient(this.smtpHost);

                mail.From = new MailAddress(this.fromAddress);
                // Add recipients
                for (int i = 0; i < this.recipients.Length; ++i)
                    mail.To.Add(this.recipients[i]);

                mail.Subject = this.preWarning ? "Site Loop Alert: Loop not yet complete, please complete before alert is raised!" : "Site Loop Alert: Loop not completed.";
                mail.Body = this.CreateEmailBody();
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //attempt to fix 5.5.1 Authentication required
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(this.smtpUser, this.smtpPassword);

                // Send it
                smtp.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Welfare check mail error:\r\n" + ex.ToString());
                return false;
            }
        }

        public string CreateEmailBody()
        {
            StringBuilder body = new StringBuilder();

            string title = this.preWarning ? "ALERT :: SITE LOOP NOT YET COMPLETE, PLEASE VISIT MISSING CHECKPOINTS LISTED BELOW" : "*** ALERT :: SITE LOOP WAS NOT COMPLETED ***";

            body.AppendLine(title + Environment.NewLine);

            body.AppendLine(" List of missed checkpoints for site loop:");
            body.AppendLine(" -----------------------------------------" + Environment.NewLine);

            foreach (var mchp in MissingCheckpoints)
            {
                body.AppendLine("       Checkpoint: " + mchp.Description);
                body.AppendLine("             Site: " + mchp.SiteName);
                body.AppendLine("Checkpoint Number: " + mchp.TagSerial + Environment.NewLine);
            }

            return body.ToString();
        }
    }
}