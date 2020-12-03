using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class ErrSendMail
    {
        public static void SendMail(string send_to, string mail_subject, string mail_content)
        {
            string serverName = ConfigurationManager.AppSettings["serverName"];
            string machineName = ConfigurationManager.AppSettings["machineName"];
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("einvoice@payware.com.tw", serverName);
            mail.To.Add(send_to);
            //mail.Bcc.Add(SystemVariables.SYS_ADMIN_MAIL);
            mail.Subject = mail_subject + "-" + machineName;
            mail.Body = mail_content;
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("192.168.65.33");
            smtp.Credentials = new System.Net.NetworkCredential("mis.smtp", "mis.smtp28597542");

            try
            {
                smtp.Send(mail);
                mail.Dispose();
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}
