using CEINV_DB.EFModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class CheckUserData
    {
        public static string CheckUser(string id,string user, string passwd)
        {
            einvoiceEntities db = new einvoiceEntities();
            var data = (from p in db.EI_SYS_USERDATA
                        where p.tax_number == id && p.user_id == user
                        select p).ToList();

            if(data.Count > 0)
            {
                if (data[0].user_pwd != passwd)
                    return ErrMeg.ErrString("1004", "");                
            }
            else
                return ErrMeg.ErrString("1001", "查無使用者資料");

            return "";
        }
    }
}
