using CEINV_DB.EFModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEINV_DB.Helper
{
    public class CheckInvoice
    {
        // 判斷發票存在
        public static bool FindInvoice(string tax_number, string invoice)
        {
            einvoiceEntities db = new einvoiceEntities();
            bool result = false;

            var data = (from p in db.EI_INV_MASTERDATA
                        where p.tax_number == tax_number && p.inv_no == invoice
                        select p).ToList();

            if (data.Count() > 0)
                return true;

            return result;
        }

        // 判斷發票重覆
        public static bool checkinvoice(string tax_number, string invoice)
        {
            einvoiceEntities db = new einvoiceEntities();
            bool result = false;
           
            var data = (from p in db.EI_INV_MASTERDATA
                        where p.tax_number == tax_number && p.inv_no == invoice
                        && (int.Parse(p.inv_type) != 3 && int.Parse(p.inv_status) != 99)
                        select p).Take(1).ToList();

            if (data.Count() > 0)
                result = true;

            return result;
        }

        // 判斷當期字軌
        public static bool CurrentCurrentWordTrack(string year, string period, string header)
        {
            einvoiceEntities db = new einvoiceEntities();
            bool result = false;

            if (String.IsNullOrEmpty(period))
            {
                var data = db.EI_INV_YEARDATA.Where(o => o.year == year && o.header == header).ToList();

                if (data.Count > 0)
                    result = true;
            }
            else
            {
                var data = db.EI_INV_YEARDATA.Where(o => o.year == year && o.header == header && o.period == period).ToList();

                if (data.Count > 0)
                    result = true;
            }

            return result;

            #region
            //myConn.Open();
            //string strSQL = "Select * From EI_INV_YEARDATA Where year = @year And period = @period And header = @header ";
            //SqlCommand cmd = new SqlCommand(strSQL, myConn);
            //cmd.Parameters.AddWithValue("@year", year);
            //cmd.Parameters.AddWithValue("@period", period);
            //cmd.Parameters.AddWithValue("@header", header);
            //SqlDataReader reader = cmd.ExecuteReader();

            //if (reader.HasRows)
            //    result = true;

            //reader.Close();
            //cmd.Dispose();
            //myConn.Close();
            //return result;
            #endregion
        }

        //字軌期別與發票開立日期不符
        public static bool CheckWordTrackDate(string id, string year, string period, string header)
        {
            bool found = true;
            einvoiceEntities db = new einvoiceEntities();
            var data = db.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                       o.year == year &&
                                                       o.period == period &&
                                                       o.header == header).ToList();

            if (data.Count() == 0)
                found = false;

            return found;
        }

        //發票號碼不在可開立字軌區間內
        public static bool CheckRange(string id, string user, string year, string period, string header, string inv_no_t)
        {
            bool found = false;
            einvoiceEntities db = new einvoiceEntities();
            var data_account = db.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                     o.year == year &&
                                                     o.period == period &&
                                                     o.header == header &&
                                                     o.user_account == user).ToList();

            var data_All = db.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                       o.year == year &&
                                                       o.period == period &&
                                                       o.header == header &&
                                                       o.user_account == "ALL").ToList();

            // 判斷user_account
            int inv_no = int.Parse(inv_no_t);
            if (data_account.Count() > 0)
            {
                // 可能開好幾本，依序去對應，例如YY00000000~YY00000999、YY00001000~YY99999999
                foreach (var item in data_account)
                {
                    int begin_no = int.Parse(item.begin_no);
                    int end_no = int.Parse(item.end_no);
                    if (inv_no >= begin_no && inv_no <= end_no)
                    {
                        found = true;
                        break;
                    }
                }
            }
            // 判斷ALL
            else if (data_All.Count() > 0)
            {
                foreach (var item in data_All)
                {
                    int begin_no = int.Parse(item.begin_no);
                    int end_no = int.Parse(item.end_no);
                    if (inv_no >= begin_no && inv_no <= end_no)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }

        // 檢查統一編號
        public static bool CheckTaxNumber(string tax_number)
        {
            int CompanyNo;

            if (tax_number.Trim().Length != 8)
                return false;

            if (!int.TryParse(tax_number, out CompanyNo))
                return false;

            int[] Logic = new int[] { 1, 2, 1, 2, 1, 2, 4, 1 };
            int addition, sum = 0, j = 0, x;

            for (x = 0; x < Logic.Length; x++)
            {
                int no = Convert.ToInt32(tax_number.Substring(x, 1));
                j = no * Logic[x];
                addition = ((j / 10) + (j % 10));
                sum += addition;
            }

            if (sum % 10 == 0)
                return true;

            if (tax_number.Substring(6, 1) == "7")
            {
                if (sum % 10 == 9)
                    return true;
            }
            return false;
        }
    }
}
