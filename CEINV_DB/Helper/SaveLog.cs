using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class SaveLog
    {
        public static void Invoice(string api_name, string functionname, dynamic DataObject, string type, string state, string errormess)
        {
            // 抓ipAddress
            IPAddress[] ipaddress = Dns.GetHostAddresses(Dns.GetHostName());
            string ipAddress = ipaddress.Where(ip => ip.AddressFamily ==
                               System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();

            // 2020.08.26 宗翰 create Folder : Log=> yyyy=> MM
            string DIRNAME = AppDomain.CurrentDomain.BaseDirectory + @"\App_Data\Log\";

            // create Log Folder
            if (!Directory.Exists(DIRNAME))
                Directory.CreateDirectory(DIRNAME);

            string year = DIRNAME + DateTime.Now.ToString("yyyy") + @"\";
            string MM = year + DateTime.Now.ToString("MM") + @"\";

            // create year、MM Folder
            if (!Directory.Exists(year))
                Directory.CreateDirectory(year);
            if (!Directory.Exists(MM))
                Directory.CreateDirectory(MM);

            string FILENAME, msg = "";
            if (string.IsNullOrEmpty(type))
            {
                FILENAME = MM + DateTime.Now.ToString("yyyyMMdd") + "_api" + ".txt";
                msg = "===========================" + functionname + "[" + DateTime.Now.ToString("HH:mm:ss") + "]=============================" +
                      "\r\n [from_ip]:  " + ipAddress +
                      "\r\n [api_name]:  " + api_name + "=>" + functionname +
                      "\r\n [api_params]: \r\n" + JsonConvert.SerializeObject(DataObject) +
                      Environment.NewLine + Environment.NewLine;
            }
            else
            {
                string errormess2 = errormess.Replace("[Message]:", "[Message]:  \r\n").
                                              Replace("[StackTrace]:", "\r\n[StackTrace]:  \r\n").
                                              Replace("[data]:", "\r\n[data]:  \r\n");

                FILENAME = MM + DateTime.Now.ToString("yyyyMMdd") + "_api_" + type + ".txt";
                msg = "===========================" + functionname + "-" + state + "[" + DateTime.Now.ToString("HH:mm:ss") + "]=============================\r\n" +
                       errormess2 + Environment.NewLine + Environment.NewLine;
            }

            ecLog.Log.Record(ecLog.Log.InfoType.Error, "", "");


            if (!File.Exists(FILENAME))
                File.Create(FILENAME).Close();

            // 避免log同時咬住，這裡只做寫入
            using (FileStream LogFile = new FileStream(FILENAME, FileMode.Append, FileAccess.Write, FileShare.Write/*, 4096, true*/))
            {
                byte[] ld_Content = Encoding.UTF8.GetBytes(msg);
                LogFile.Write(ld_Content, 0, ld_Content.Length);
                LogFile.Flush();
                LogFile.Close();
            }

            //File.AppendAllText(FILENAME, msg);

            // 錯誤send mail
            if (!string.IsNullOrEmpty(type))
            {
                errormess = "=============" + functionname + "-" + state +
                            "[" + DateTime.Now.ToString("HH:mm:ss") + "]================<br/><br/>" +
                            errormess.Replace("[Message]:", @"<div style=""color: rgb(50, 50, 204);"">[Message]:<br/>").
                                      Replace("[StackTrace]:", @"<br/></div><br/><div style = ""color: rgb(6, 104, 99);"" >[StackTrace]:<br/>").
                                      Replace("[data]:", @"</div><br/><div style = ""color: rgb(170, 30, 30);"">[data]:<br/>") + "</div>";

                ErrSendMail.SendMail(ConfigurationManager.AppSettings["mail-rd"], "[" + state + "]通知" +
                            DateTime.Now.ToString("HH:mm:ss"), errormess);
            }
        }
    }
}
