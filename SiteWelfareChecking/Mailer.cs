using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using UniGuardLib;

namespace SiteWelfareChecking
{
    public class Mailer
    {
        // Mail details
        private string[] recipients;
        private string fromAddress  = "no.reply@uniguard.com.au";
        private string smtpHost     = "smtp.gmail.com";
        private string smtpUser     = "no.reply@uniguard.com.au";
        private string smtpPassword = "B6gYu9uGtr4xuG1!";

        // Last checkpoint visit details
        private string visitDate;
        private string visitTime;
        private string visitCheckpoint;
        private string visitSite;
        private string visitRegion;

        // Recorder details
        private double recorderSerial;
        private string recorderName;

        public List<string> Recipients
        {
            set { this.recipients = value.ToArray(); }
        }

        public object[] Visit
        {
            set
            {
                this.visitDate       = (String)value[0];
                this.visitTime       = (String)value[1];
                this.recorderSerial  = Convert.ToDouble(value[2]);
                this.visitCheckpoint = (String)value[3];
                this.visitSite       = (String)value[4];
                this.visitRegion     = (String)value[5];
                this.recorderName    = (String)value[6];
            }
        }

        public bool sendMail()
        {
            try
            {
                MailMessage mail  = new MailMessage();
                SmtpClient smtp   = new SmtpClient(this.smtpHost);
                //Attachment attach = new Attachment(fileName);

                //mail.Attachments.Add(attach);
                mail.From = new MailAddress(this.fromAddress);
                // Add recipients
                for (int i = 0; i < this.recipients.Length; ++i)
                    mail.To.Add(this.recipients[i]);

                mail.Subject     = "UniGuard 12 Welfare Check: Breach of time allowance.";
                mail.Body        = this.CreateEmailBody();
                smtp.Port        = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                //attempt to fix 5.5.1 Authentication required
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(this.smtpUser, this.smtpPassword);

                // Send it
                smtp.Send(mail);
                //attach.Dispose();

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

            body.AppendLine("*** URGENT NOTICE :: UNIGUARD EMPLOYEE WELFARE CHECK BREACHED ***" + Environment.NewLine);
            body.AppendLine("An employee has breached their time allowance welfare check, ");
            body.Append("please see detail below of the last event before the breach was identified." + Environment.NewLine);
            body.AppendLine("And exception has been made and logged onto the system." + Environment.NewLine);
            body.AppendLine(" Last visit details:");
            body.AppendLine(" -------------------");
            body.AppendLine("   Checkpoint: " + this.visitCheckpoint);
            body.AppendLine("         Site: " + this.visitSite);
            body.AppendLine("       Region: " + this.visitRegion);
            body.AppendLine(" Date visited: " + this.visitDate);
            body.AppendLine(" Time visited: " + this.visitTime + Environment.NewLine);
            body.AppendLine(" Recorder details:");
            body.AppendLine(" -----------------");
            body.AppendLine("   Recorder name: " + this.recorderName);
            body.AppendLine(" Recorder serial: " + this.recorderSerial.ToString());

            return body.ToString();
        }
    }
}
