
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using UniGuardLib;

namespace HighRiskCheckpoiont
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
        private string visitTagNumber;
        private string visitSite;
        private string visitRegion;

        public List<string> Recipients
        {
            set { this.recipients = value.ToArray(); }
        }

        public object[] Visit
        {
            set
            {
                this.visitDate = (String)value[0];
                this.visitTime = (String)value[1];
                this.visitCheckpoint = (String)value[2];
                this.visitTagNumber = (String)value[3];
                this.visitSite = (String)value[4];
                this.visitRegion = (String)value[5];
            }
            //added accessor to allow to check for empty query return
            get
            {
                return this.Visit;
            }
        }

        public bool sendMail()
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtp  = new SmtpClient(this.smtpHost);

                // Mail params
                mail.From = new MailAddress(this.fromAddress);
                for (int i = 0; i < this.recipients.Length; ++i)
                    mail.To.Add(this.recipients[i]);

                mail.Subject = "UniGuard 12 High Risk Checkpoint Check: Breach of time allowance.";
                mail.Body = this.CreateEmailBody();
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
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

            body.AppendLine("*** URGENT NOTICE :: UNIGUARD EMPLOYEE HIGH RISK CHECKPOINT CHECK BREACHED ***" + Environment.NewLine);
            body.AppendLine("An employee has breached their time allowance for a high risk checkpoint check, ");
            body.AppendLine("An exception has been made and logged onto the system." + Environment.NewLine + Environment.NewLine);
            body.AppendLine(" Last visit details:");
            body.AppendLine(" -------------------");
            body.AppendLine("   Checkpoint: " + this.visitCheckpoint);
            body.AppendLine("   Tag Number: " + this.visitTagNumber);
            body.AppendLine("         Site: " + this.visitSite);
            body.AppendLine("       Region: " + this.visitRegion);
            body.AppendLine("  Date missed: " + this.visitDate);
            body.AppendLine("  Time missed: " + this.visitTime + Environment.NewLine);

            return body.ToString();
        }
    }
}
