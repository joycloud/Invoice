using CEINV_DB.EFModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class ErrMeg
    {
        // rtcode、rtmsg
        public static string ErrString(string rtcode, string meg)
        {
            string rtmsg = GetErrMeg(rtcode);
            rtmsg = Convert.ToBase64String(Encoding.UTF8.GetBytes(rtmsg + "  " + meg));
            return "rtcode=" + rtcode + "&rtmsg=" + rtmsg;

            //return "[代碼:" + rtcode + "  訊息:" + rtmsg + "  " + meg + "]";
        }

        // rtcode、rtmsg、status
        public static string StatusString(string rtcode, string meg, string sta)
        {
            string rtmsg = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetErrMeg(rtcode) + "  " + meg));
            string status = Convert.ToBase64String(Encoding.UTF8.GetBytes(sta));

            return "rtcode=" + rtcode + "&rtmsg=" + rtmsg + "&status=" + status;
        }

        // rtcode、rtmsg、trackinfo、size
        public static string StatusString2(string rtcode, string meg, string trackinfo, int size)
        {
            string rtmsg = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetErrMeg(rtcode) + "  " + meg));

            return "rtcode=" + rtcode + "&rtmsg=" + rtmsg + "&trackinfo=" + trackinfo + "&size=" + size;
        }

        //// 多筆發票號碼回傳錯誤
        //public static string PluralInvoice(string meg)
        //{
        //    string rtcode = "";
        //    string rtmsg = "";

        //    foreach (var item in meg)
        //    {
        //        string[] array = item.ToString().Split(';');
        //        array = array[0].Split(',');

        //    }


        //    return "rtcode=" + rtcode + "&rtmsg=" + rtmsg;
        //}

        public static string ShowErrorMessage(string rtcode, string id, string meg, bool Email)
        {
            einvoiceEntities db = new einvoiceEntities();
            string rtmsg = "";
            string Exrtmsg = "";
            try
            {
                string mmesg = ErrString(rtcode, meg);

                // string mmesg = "[代碼:" + rtcode + "  訊息:" + rtmsg + "  " + meg + "]";
                rtmsg = GetErrMeg(rtcode);
                Exrtmsg = "rtcode=" + rtcode + "&rtmsg=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(rtmsg + "  " + meg));

                if (Email)
                {
                    //發送通知信件給營業人&加值中心管理者
                    string send_to = db.EI_BAS_COMPANY.Where(o => o.tax_number == id).Select(o => o.comp_email).First();
                    string mail_content =
                            "您好:剛才貴公司上傳的發票內容有誤，檢查其內容後重新上傳，謝謝！<br/>" +
                            "<span style=" + "\"color: rgb(235, 55, 55); font-size: 18px; " + "\">" + mmesg + "</span>";
                    SendMail(send_to, "[上傳發票內容發生錯誤]通知", mail_content);
                }
            }
            catch (Exception ex)
            {
                return Exrtmsg;
                throw;
            }
            return Exrtmsg;
        }
        private static String GetErrMeg(string code)
        {
            einvoiceEntities db = new einvoiceEntities();

            var data = db.EI_ERR_MESSAGE.Where(o => o.code == code).Select(o => o).ToList();

            string Errmes = "";
            if (data.Count > 0)
                Errmes = data[0].message;

            return Errmes;
        }

        // Mail
        public static void SendMail(string send_to, string mail_subject, string mail_content)
        {
            string serverName = ConfigurationManager.AppSettings["serverName"];
            string sendmail = ConfigurationManager.AppSettings["mail-einvoice"];
            string sendBcc = ConfigurationManager.AppSettings["mail-rd"];
            string machineName = ConfigurationManager.AppSettings["kind"] == "n" ? "測試機" : "正式機";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(sendmail, serverName);//寄件人
            mail.To.Add(send_to);//收件人einv_merchant@payware.com.tw
            mail.Bcc.Add(sendBcc);
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
