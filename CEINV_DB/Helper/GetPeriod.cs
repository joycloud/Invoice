using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class GetPeriod
    {
        public static int get_period(string Period)
        {
            int period = 0;
            // 若沒傳Period值，就抓現在期別
            if (string.IsNullOrEmpty(Period))
                Period = DateTime.Now.ToString("MM");

            switch (Period)
            {
                case "01":
                case "02":
                    period = 0;
                    break;
                case "03":
                case "04":
                    period = 1;
                    break;
                case "05":
                case "06":
                    period = 2;
                    break;
                case "07":
                case "08":
                    period = 3;
                    break;
                case "09":
                case "10":
                    period = 4;
                    break;
                case "11":
                case "12":
                    period = 5;
                    break;
            }
            return period;
        }
    }
}
