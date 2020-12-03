using Newtonsoft.Json;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static CEINV_API.Models.Types;
using CEINV_DB;
using CEINV_DB.Helper;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using CEINV_DB.EFModel;
using System.Data;
using System.IO;
using System.Text;
using System.Configuration;
using System.Web.Hosting;

namespace CEINV_API.Controllers
{
    public class InvoiceController : ApiController
    {
        public string action = "";

        //private int Period(dynamic dynamic)
        //{
        //    throw new NotImplementedException();
        //}

        // dynamic浮動的Object，告訴系統跳過錯誤繼續執行，例如底下沒這個欄位，Object則會出現錯誤
        private string ColumnsCheck(dynamic DataObject, string id, string user)
        {
            string state = "";
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            try
            {
                // 發票內容檢查
                Type t1 = DataObject.GetType();
                PropertyInfo[] ps1 = t1.GetProperties();
                //Hashtable ht = new Hashtable();
                List<ErrorColumns> ErrColumns = new List<ErrorColumns>();
                int count = 0;

                decimal SalesAmount = 0;
                decimal FreeTaxSalesAmount = 0;
                decimal ZeroTaxSalesAmount = 0;
                decimal TotalAmount = 0;

                state = "檢查欄位";
                foreach (PropertyInfo pi in ps1)
                {
                    // 判斷發票字碼=========================================================================================
                    if (pi.Name.ToUpper() == "InvoiceNumber".ToUpper())
                    {
                        // 空發票
                        if (pi.GetValue(DataObject) == null || string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                            return ErrMeg.ErrString("1301", "發票號碼空白");

                        // 判斷發票
                        else
                        {
                            // 判斷發票長度
                            if (pi.GetValue(DataObject).ToString().Trim().Length != 10)
                                return ErrMeg.ErrString("1206", "發票號碼長度錯誤");

                            // 判斷字軌
                            string inv_no_h = pi.GetValue(DataObject).ToString().Substring(0, 2);
                            string inv_no_t = pi.GetValue(DataObject).ToString().Substring(2, 8);
                            System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[A-Za-z]+$");
                            System.Text.RegularExpressions.Regex reg2 = new System.Text.RegularExpressions.Regex(@"^[0-9]+$");
                            if ((!reg1.IsMatch(inv_no_h)) || (!reg2.IsMatch(inv_no_t)))
                                return ErrMeg.ErrString("1301", "發票字軌有誤");


                            // 檢查該客戶有無字軌資料
                            int period = GetPeriod.get_period("");
                            int year = Convert.ToInt32(DateTime.Now.ToString("yyyy"));
                            string header = pi.GetValue(DataObject).ToString().Substring(0, 2);

                            using (einvoiceEntities db = new einvoiceEntities())
                            {
                                var data = db.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                                       o.period == period.ToString() &&
                                                                       o.year == year.ToString() &&
                                                                       o.header == header).Select(o => o).ToList();
                                if (data.Count == 0)
                                    return ErrMeg.ErrString("1302", "");
                            }

                            //非電子發票專用字軌
                            string inv_date_year = DataObject.InvoiceDate.ToString().Substring(0, 4);
                            int inv_date_MM = GetPeriod.get_period(DataObject.InvoiceDate.ToString().Substring(4, 2));
                            bool check = CEINV_DB.Helper.CheckInvoice.CurrentCurrentWordTrack(inv_date_year, "", header);
                            if (!check)
                                return ErrMeg.ErrString("1106", "");

                            //非當期字軌
                            check = CEINV_DB.Helper.CheckInvoice.CurrentCurrentWordTrack(inv_date_year, inv_date_MM.ToString(), header);
                            if (!check)
                                return ErrMeg.ErrString("1107", "");

                            //發票號碼重複
                            check = CEINV_DB.Helper.CheckInvoice.checkinvoice(id, pi.GetValue(DataObject).ToString());
                            if (check)
                                return ErrMeg.ErrString("1401", "");

                            //字軌期別與發票開立日期不符
                            check = CEINV_DB.Helper.CheckInvoice.CheckWordTrackDate(id, inv_date_year, inv_date_MM.ToString(),
                                                                                    pi.GetValue(DataObject).ToString().Substring(0, 2));
                            if (!check)
                                return ErrMeg.ErrString("1303", "");

                            //發票號碼不在可開立字軌區間內
                            check = CEINV_DB.Helper.CheckInvoice.CheckRange(id, user, inv_date_year, inv_date_MM.ToString(),
                                                                            pi.GetValue(DataObject).ToString().Substring(0, 2), inv_no_t);
                            if (!check)
                                return ErrMeg.ErrString("1304", "");
                        }
                    }
                    // 判斷發票日期格式=========================================================================================
                    else if (pi.Name.ToUpper() == "InvoiceDate".ToUpper())
                    {
                        if (pi.GetValue(DataObject) == null || string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                            return ErrMeg.ErrString("1207", "");
                        else
                        {
                            if (pi.GetValue(DataObject).ToString().Length == 8)
                            {
                                DateTime _date;
                                string inv_date = pi.GetValue(DataObject).ToString();
                                inv_date = inv_date.Substring(0, 4) + "/" + inv_date.Substring(4, 2) + "/" + inv_date.Substring(6, 2);

                                // 判斷是否日期格式
                                if (!DateTime.TryParse(inv_date, out _date))
                                    return ErrMeg.ErrString("1207", "");
                                // 判斷日期沒有大於今天
                                else if (inv_date.CompareTo(DateTime.Now.ToString("yyyy/MM/dd")) > 0)
                                    return ErrMeg.ErrString("1208", "");

                                // 判斷發票時間
                                string inv_time = DataObject.InvoiceTime.ToString();
                                // 發票時間空白
                                if (string.IsNullOrEmpty(inv_time))
                                    return ErrMeg.ErrString("1209", "發票時間空白");
                                else
                                {
                                    // 發票時間錯誤
                                    if (!DateTime.TryParse(inv_date + " " + inv_time, out _date))
                                        return ErrMeg.ErrString("1209", "");
                                }

                                //超出單月15日上傳期限
                                //Ex: 1,2月份的發票超過3/15就不能上傳
                                string inv_ym = pi.GetValue(DataObject).ToString().Substring(0, 6);
                                string now_ym = DateTime.Now.ToString("yyyyMMdd");

                                // 先判斷年份
                                if (now_ym.CompareTo(inv_ym) > 0)
                                {
                                    // 間隔兩年
                                    if (int.Parse(now_ym.Substring(0, 4)) - int.Parse(inv_ym.Substring(0, 4)) > 1)
                                        return ErrMeg.ErrString("1210", "發票日期過舊");

                                    int inv_Period = GetPeriod.get_period(inv_ym.Substring(4, 2));
                                    int now_Period = GetPeriod.get_period(now_ym.Substring(4, 2));
                                    int inv_mm = int.Parse(now_ym.Substring(6, 2));

                                    // 跨期且超過15號
                                    if (now_Period > inv_Period && inv_mm >= 15)
                                        return ErrMeg.ErrString("1210", "發票跨期且超過15號");
                                }
                                else
                                    return ErrMeg.ErrString("1210", "");
                            }
                            else
                                return ErrMeg.ErrString("1207", "");
                        }
                    }
                    // 判斷買方統編=========================================================================================
                    else if (pi.Name.ToUpper() == "BuyerIdentifier".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            bool check = CEINV_DB.Helper.CheckInvoice.CheckTaxNumber(pi.GetValue(DataObject).ToString());
                            if (!check)
                                return ErrMeg.ErrString("1309", "");
                        }
                    }
                    //應稅銷售額錯誤(需填整數)================================================================================
                    else if (pi.Name.ToUpper() == "SalesAmount".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            int taxable_amt;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out taxable_amt))
                                return ErrMeg.ErrString("1314", "");
                            else
                            {
                                //判斷應稅銷售額、免稅銷售額、零稅率銷售額至少其一不能為0
                                if (int.Parse(pi.GetValue(DataObject)) == 0)
                                    count++;
                            }
                        }
                        else
                            return ErrMeg.ErrString("1314", "");

                        SalesAmount = decimal.Parse(pi.GetValue(DataObject));
                    }
                    //免稅銷售額錯誤(需填整數)================================================================================
                    else if (pi.Name.ToUpper() == "FreeTaxSalesAmount".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            int freetax_amt;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out freetax_amt))
                                return ErrMeg.ErrString("1315", "");
                            else
                            {
                                //判斷應稅銷售額、免稅銷售額、零稅率銷售額至少其一不能為0
                                if (int.Parse(pi.GetValue(DataObject)) == 0)
                                    count++;
                            }
                        }
                        else
                            return ErrMeg.ErrString("1315", "");

                        FreeTaxSalesAmount = decimal.Parse(pi.GetValue(DataObject));
                    }
                    //零稅率銷售額錯誤(需填整數)================================================================================
                    else if (pi.Name.ToUpper() == "ZeroTaxSalesAmount".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            int zerotax_amt;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out zerotax_amt))
                                return ErrMeg.ErrString("1316", "");
                            else
                            {
                                //判斷應稅銷售額、免稅銷售額、零稅率銷售額至少其一不能為0
                                if (int.Parse(pi.GetValue(DataObject)) == 0)
                                    count++;
                                if (count == 3)
                                    return ErrMeg.ErrString("1316", "應稅銷售額、免稅銷售額、零稅率銷售額至少其一不能為0");
                            }
                        }
                        else
                            return ErrMeg.ErrString("1316", "");

                        ZeroTaxSalesAmount = decimal.Parse(pi.GetValue(DataObject));
                    }
                    //稅別===================================================================================================
                    else if (pi.Name.ToUpper() == "TaxType".ToUpper())
                    {
                        //課稅別錯誤
                        if (pi.GetValue(DataObject).ToString() != "1" && pi.GetValue(DataObject).ToString() != "2" &&
                            pi.GetValue(DataObject).ToString() != "3" && pi.GetValue(DataObject).ToString() != "4" &&
                            pi.GetValue(DataObject).ToString() != "9")
                            return ErrMeg.ErrString("1317", "");
                    }
                    //稅率===================================================================================================
                    else if (pi.Name.ToUpper() == "TaxRate".ToUpper())
                    {
                        //若無則給0.05
                        if (string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                            DataObject.TaxRate = Convert.ToDecimal(0.05);
                        else
                        {
                            double tax_rate;
                            if (!double.TryParse(pi.GetValue(DataObject).ToString(), out tax_rate))
                                return ErrMeg.ErrString("1322", "");
                        }
                    }
                    //營業稅額錯誤(需填整數)====================================================================================
                    else if (pi.Name.ToUpper() == "TaxAmount".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            int tax_amt;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out tax_amt))
                                return ErrMeg.ErrString("1318", "");
                        }
                        else
                            return ErrMeg.ErrString("1318", "");
                    }
                    //總計錯誤(需填整數)========================================================================================
                    else if (pi.Name.ToUpper() == "TotalAmount".ToUpper())
                    {
                        if (!string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            int total_amt;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out total_amt))
                                return ErrMeg.ErrString("1319", "");
                        }
                        else
                            return ErrMeg.ErrString("1319", "");

                        TotalAmount = pi.GetValue(DataObject);
                    }
                    //列印註記錯誤==============================================================================================
                    else if (pi.Name.ToUpper() == "PrintMark".ToUpper() && typeof(C0401) == t1)
                    {
                        if (string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                            DataObject.PrintMark = "N";
                        else if (pi.GetValue(DataObject).ToString() != "Y" && pi.GetValue(DataObject).ToString() != "N")
                            return ErrMeg.ErrString("1320", "");
                    }
                    //隨機碼錯誤================================================================================================
                    else if (pi.Name.ToUpper() == "RandomNumber".ToUpper() && typeof(C0401) == t1)
                    {
                        string random_no = string.Empty;
                        if (string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                        {
                            Random ran = new Random();
                            // 產生隨機4碼
                            DataObject.RandomNumber = ran.Next(0, 9999).ToString().PadLeft(4, '0');
                        }
                        else
                        {
                            if (pi.GetValue(DataObject).ToString().Length != 4)
                                return ErrMeg.ErrString("1321", "長度需要4位數");

                            int random;
                            if (!int.TryParse(pi.GetValue(DataObject).ToString(), out random))
                                return ErrMeg.ErrString("1321", "必須為4位數字");
                        }
                    }
                    //發票備註==================================================================================================
                    else if (pi.Name.ToUpper() == "MainRemark".ToUpper())
                    {
                        if (pi.GetValue(DataObject).ToString().Length > 200)
                            return ErrMeg.ErrString("1334", "");
                    }
                    //載具類別錯誤===============================================================================================
                    else if (pi.Name.ToUpper() == "CustomsClearanceMark".ToUpper())
                    {
                        if (string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()))
                            DataObject.CustomsClearanceMark = "1";

                        //零稅率通關方式錯誤
                        if (DataObject.TaxType == 2)
                        {
                            if (string.IsNullOrEmpty(pi.GetValue(DataObject).ToString()) ||
                                (pi.GetValue(DataObject).ToString() != "1" && pi.GetValue(DataObject).ToString() != "2"))
                                return ErrMeg.ErrString("1338", "");
                        }
                    }
                    else
                    {
                        if (pi.GetValue(DataObject) == null)
                        {
                            ErrColumns.Add(new ErrorColumns
                            {
                                Columns = pi.Name,
                                Remark = "欄位&內容有誤"
                            });
                        }
                    }
                }

                state = "檢查Details";
                // 商品明細檢查===================================================================================================
                int k = 1;
                foreach (var item in DataObject.Details)
                {
                    // 品名
                    if (string.IsNullOrEmpty(item.Description))
                    {
                        ErrColumns.Add(new ErrorColumns
                        {
                            Columns = "Description",
                            Remark = "第" + k + "商品品名欄位or內容有誤"
                        });
                    }
                    // 數量
                    if (string.IsNullOrEmpty(item.Quantity.ToString()) || item.Quantity == 0)
                    {
                        ErrColumns.Add(new ErrorColumns
                        {
                            Columns = "Quantity",
                            Remark = "第" + k + "商品數量欄位or內容有誤"
                        });
                    }
                    // 單價
                    if (string.IsNullOrEmpty(item.UnitPrice.ToString()) || item.UnitPrice == 0)
                    {
                        ErrColumns.Add(new ErrorColumns
                        {
                            Columns = "UnitPrice",
                            Remark = "第" + k + "商品單價欄位or內容有誤"
                        });
                    }
                    // 小計金額
                    if (string.IsNullOrEmpty(item.Amount.ToString()) || item.Amount == 0)
                    {
                        ErrColumns.Add(new ErrorColumns
                        {
                            Columns = "Amount",
                            Remark = "第" + k + "商品小計金額欄位or內容有誤"
                        });
                    }
                    // 明細備註
                    if (item.Remark == null)
                    {
                        ErrColumns.Add(new ErrorColumns
                        {
                            Columns = "Remark",
                            Remark = "第" + k + "商品明細備註欄位or內容有誤"
                        });
                    }
                    k++;
                }

                state = "判斷金額計算";
                //判斷金額計算==================================================================================================
                if (SalesAmount + FreeTaxSalesAmount + ZeroTaxSalesAmount != TotalAmount)
                    return ErrMeg.ErrString("1319", "應稅銷售額、免稅銷售額、零稅率銷售額加總金額有誤");
                else
                {
                    //判斷 數量*單價 = Amount
                    foreach (var item in DataObject.Details)
                    {
                        if (item.Quantity * item.UnitPrice != item.Amount)
                            return ErrMeg.ErrString("1331", "");
                    }
                }

                // 彙總錯誤回報
                if (ErrColumns.Count > 0)
                {
                    string mmegg = "  ";
                    foreach (var item in ErrColumns)
                        mmegg += "\r\n" + item.Columns + ":" + item.Remark + ",";
                    mmegg = mmegg.TrimEnd(',');

                    return ErrMeg.ErrString("1101", "請再次確認以下欄位&內容" + mmegg);
                }
                // 資料都無誤，轉成未稅
                else
                {
                    // 發票主檔
                    DataTable Masters = new DataTable();
                    Masters.Columns.Add("InvoiceNumber", typeof(string));
                    Masters.Columns.Add("InvoiceDate", typeof(string));
                    Masters.Columns.Add("InvoiceTime", typeof(string));
                    Masters.Columns.Add("BuyerIdentifier", typeof(string));
                    Masters.Columns.Add("BuyerName", typeof(string));
                    Masters.Columns.Add("BuyerAddress", typeof(string));
                    Masters.Columns.Add("BuyerTelephoneNumber", typeof(string));
                    Masters.Columns.Add("BuyerEmailAddress", typeof(string));
                    Masters.Columns.Add("SalesAmount", typeof(string));
                    Masters.Columns.Add("FreeTaxSalesAmount", typeof(string));
                    Masters.Columns.Add("ZeroTaxSalesAmount", typeof(string));
                    Masters.Columns.Add("TaxType", typeof(string));
                    Masters.Columns.Add("TaxRate", typeof(string));
                    Masters.Columns.Add("TaxAmount", typeof(string));
                    Masters.Columns.Add("TotalAmount", typeof(string));
                    Masters.Columns.Add("PrintMark", typeof(string));
                    Masters.Columns.Add("RandomNumber", typeof(string));
                    Masters.Columns.Add("CustomsClearanceMark", typeof(string));
                    Masters.Columns.Add("MainRemark", typeof(string));
                    Masters.Columns.Add("CarrierType", typeof(string));
                    Masters.Columns.Add("CarrierId1", typeof(string));
                    Masters.Columns.Add("CarrierId2", typeof(string));
                    Masters.Columns.Add("NPOBAN", typeof(string));

                    // 商品明細
                    DataTable Details = new DataTable();
                    Details.Columns.Add("Num", typeof(int));            // 商品原排序
                    Details.Columns.Add("TaxType", typeof(int));        // 稅別
                    Details.Columns.Add("Description", typeof(string)); // 品名
                    Details.Columns.Add("Quantity", typeof(int));       // 數量
                    Details.Columns.Add("UnitPrice", typeof(decimal));  // 單價
                    Details.Columns.Add("Amount", typeof(decimal));     // 應稅總和
                    Details.Columns.Add("Remark", typeof(string));      // 明細備註
                    Details.Columns.Add("dutyPrice", typeof(decimal));  // 未稅單價
                    Details.Columns.Add("tax", typeof(int));            // 稅額
                    Details.Columns.Add("amount", typeof(decimal));     // 未稅總和

                    // 丟入Table
                    Masters.Rows.Add();
                    Masters.Rows[0]["InvoiceNumber"] = DataObject.InvoiceNumber;
                    Masters.Rows[0]["InvoiceDate"] = DataObject.InvoiceDate;
                    Masters.Rows[0]["InvoiceTime"] = DataObject.InvoiceTime;
                    Masters.Rows[0]["BuyerIdentifier"] = DataObject.BuyerIdentifier;
                    Masters.Rows[0]["BuyerName"] = DataObject.BuyerName;
                    Masters.Rows[0]["BuyerAddress"] = DataObject.BuyerAddress;
                    Masters.Rows[0]["BuyerTelephoneNumber"] = DataObject.BuyerTelephoneNumber;
                    Masters.Rows[0]["BuyerEmailAddress"] = DataObject.BuyerEmailAddress;
                    Masters.Rows[0]["SalesAmount"] = DataObject.SalesAmount;
                    Masters.Rows[0]["FreeTaxSalesAmount"] = DataObject.FreeTaxSalesAmount;
                    Masters.Rows[0]["ZeroTaxSalesAmount"] = DataObject.ZeroTaxSalesAmount;
                    Masters.Rows[0]["TaxType"] = DataObject.TaxType;
                    Masters.Rows[0]["TaxRate"] = DataObject.TaxRate;
                    Masters.Rows[0]["TaxAmount"] = DataObject.TaxAmount;
                    Masters.Rows[0]["TotalAmount"] = DataObject.TotalAmount;
                    Masters.Rows[0]["CustomsClearanceMark"] = DataObject.CustomsClearanceMark;
                    Masters.Rows[0]["MainRemark"] = DataObject.MainRemark;
                    Masters.Rows[0]["NPOBAN"] = DataObject.NPOBAN;

                    // 只有C0401才有這些欄位
                    if (typeof(C0401) == t1)
                    {
                        Masters.Rows[0]["PrintMark"] = DataObject.PrintMark;
                        Masters.Rows[0]["RandomNumber"] = DataObject.RandomNumber;
                        Masters.Rows[0]["CarrierType"] = DataObject.CarrierType;
                        Masters.Rows[0]["CarrierId1"] = DataObject.CarrierId1;
                        Masters.Rows[0]["CarrierId2"] = DataObject.CarrierId2;
                    }

                    k = 0;
                    foreach (var item in DataObject.Details)
                    {
                        Details.Rows.Add();
                        Details.Rows[k]["Num"] = k + 1;
                        Details.Rows[k]["Description"] = item.Description;
                        Details.Rows[k]["Quantity"] = item.Quantity;
                        Details.Rows[k]["TaxType"] = item.TaxType;
                        Details.Rows[k]["UnitPrice"] = item.UnitPrice;
                        Details.Rows[k]["Amount"] = item.Amount;
                        Details.Rows[k]["Remark"] = item.Remark;
                        k++;
                    }

                    //判斷應稅銷售額、免稅銷售額、零稅率銷售額，明細加總是否正確
                    for (int y = 1; y <= 3; y++)
                    {
                        if (y == 1 &&
                           SalesAmount != Details.AsEnumerable().Where(o => o.Field<int>("TaxType") == y).Sum(row => row.Field<decimal>("Amount")))
                            return ErrMeg.ErrString("1314", "應稅銷售額與明細計算有誤");
                        else if (y == 2 &&
                           ZeroTaxSalesAmount != Details.AsEnumerable().Where(o => o.Field<int>("TaxType") == y).Sum(row => row.Field<decimal>("Amount")))
                            return ErrMeg.ErrString("1316", "零稅銷售額與明細計算有誤");
                        else if (y == 3 &&
                           FreeTaxSalesAmount != Details.AsEnumerable().Where(o => o.Field<int>("TaxType") == y).Sum(row => row.Field<decimal>("Amount")))
                            return ErrMeg.ErrString("1315", "免稅售額與明細計算有誤");
                    }

                    state = "換算未稅";
                    // 應稅換算未稅==============================================================
                    Details = ConverTaxexcluded.Taxexcluded(Details);
                    // 排序
                    Details.DefaultView.Sort = "Num";
                    Details = Details.DefaultView.ToTable();

                    // Save into data==========================================================
                    state = "資料存檔";
                    InvoiceCRUD.InvoiceSave(Masters, Details, id, user, action);
                }
                return "0000";
            }
            catch (Exception ex)
            {
                string errormess = "[Message]:" + ex.Message +
                                   "[StackTrace]:" + ex.StackTrace +
                                   "[data]:" + JsonConvert.SerializeObject(DataObject);

                SaveLog.Invoice("InvoiceController", functionname, DataObject, "error", state, errormess);
                return ErrMeg.ErrString("9999", "");
            }
        }

        [HttpPost]
        public string Create([FromBody] jsonstring_Create responce)
        {
            if (responce != null)
            {
                // 判斷使用者資料
                if (string.IsNullOrEmpty(responce.id))
                    return ErrMeg.ErrString("1101", "統編不可空白");
                if (string.IsNullOrEmpty(responce.user))
                    return ErrMeg.ErrString("1101", "帳號不可空白");
                if (string.IsNullOrEmpty(responce.passwd))
                    return ErrMeg.ErrString("1101", "密碼不可空白");
                if (string.IsNullOrEmpty(responce.csv.ToString().Replace("{", "").Replace("}", "")))
                    return ErrMeg.ErrString("1101", "檔案內文不可空白");
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");

            string messege = "";
            action = responce.action;
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;

            // B2B 交換，開立發票                         B2B 存證，平台存證開立發票
            if (responce.action.ToUpper() == "A0101" || responce.action.ToUpper() == "A0401")
            {
                // 丟入object
                A0101A0401 DataObject = new A0101A0401();
                try
                {
                    DataObject = JsonConvert.DeserializeObject<A0101A0401>(JsonConvert.SerializeObject(responce.csv));
                }
                catch (JsonSerializationException e)
                {
                    return ErrMeg.ErrString("1101", "json格式錯誤");
                }

                //存Log
                SaveLog.Invoice("InvoiceController", functionname, DataObject, "", "", "");

                // 判斷帳密資料
                string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                if (!string.IsNullOrEmpty(mess))
                    return mess;

                // 判斷欄位、內容
                messege = ColumnsCheck(DataObject, responce.id, responce.user);

                // 抓金鑰
            }
            // B2C 平台存證開立發票
            else if (responce.action.ToUpper() == "C0401")
            {
                // 丟入object
                C0401 DataObject = new C0401();
                try
                {
                    DataObject = JsonConvert.DeserializeObject<C0401>(JsonConvert.SerializeObject(responce.csv));
                }
                catch (JsonSerializationException e)
                {
                    return ErrMeg.ErrString("1101", "json格式錯誤");
                }

                //存Log                
                SaveLog.Invoice("InvoiceController", functionname, DataObject, "", "", "");

                // 判斷帳密資料
                string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                if (!string.IsNullOrEmpty(mess))
                    return mess;

                // 判斷欄位、內容
                messege = ColumnsCheck(DataObject, responce.id, responce.user);
            }
            else
                return ErrMeg.ErrString("1101", "開立發票代碼錯誤");

            if (messege == "0000")
                messege = ErrMeg.ErrString(messege, "開立發票成功");

            return messege;
        }

        [HttpPost]
        public string GetTrackList([FromBody] jsonstring_GetTrackList responce)
        {
            if (responce != null)
            {
                string state = "";
                string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
                try
                {
                    int num1;
                    // 判斷使用者資料
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.year.ToString()))
                        return ErrMeg.ErrString("1101", "年份不可空白");
                    else if (int.TryParse(responce.year.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "年份必須是數字");
                    if (string.IsNullOrEmpty(responce.period.ToString()))
                        return ErrMeg.ErrString("1101", "發票字軌期別不可空白");
                    else if (int.TryParse(responce.period.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "發票字軌期別必須是數字");
                    if (string.IsNullOrEmpty(responce.size.ToString()))
                        return ErrMeg.ErrString("1101", "索取張數不可空白");
                    else if (int.TryParse(responce.size.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "索取張數必須是數字");

                    //存Log                    
                    SaveLog.Invoice("InvoiceController", functionname, responce, "", "", "");

                    // 判斷帳密資料
                    string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                    if (!string.IsNullOrEmpty(mess))
                        return mess;

                    int now_month = DateTime.Now.Month;
                    int now_day = DateTime.Now.Day;
                    int now_period = GetPeriod.get_period("");

                    state = "取票規則判斷";
                    string limit_month = "";
                    // 大於現在期別
                    if (responce.period > now_period)
                    {
                        // 現在月份
                        limit_month = now_month.ToString().PadLeft(2, '0');

                        // 奇數月
                        if (now_month % 2 == 1)
                            limit_month = (now_month + 1).ToString().PadLeft(2, '0');

                        limit_month = limit_month + "25";

                        // 雙月25號後才能取用下期字軌
                        if (DateTime.Now.ToString("MMdd").CompareTo(limit_month) < 0)
                            return ErrMeg.ErrString("1104", "");
                    }
                    // 小於現在期別
                    else if (responce.period < now_period)
                    {
                        limit_month = DateTime.Now.Month.ToString().PadLeft(2, '0');

                        if (now_month % 2 == 0)
                            limit_month = (now_month - 1).ToString().PadLeft(2, '0');

                        limit_month = limit_month + "14";

                        //單月14號後不得取用前期字軌
                        if (DateTime.Now.ToString("MMdd").CompareTo(limit_month) > 0)
                            return ErrMeg.ErrString("1105", "");
                    }

                    state = "當年度期別判斷";
                    //是否已有當年度期別之電子發票專用字軌
                    using (einvoiceEntities db = new einvoiceEntities())
                    {
                        var data1 = db.EI_INV_YEARDATA.Where(o => o.year == responce.year.ToString() &&
                                                                 o.period == responce.period.ToString()).ToList();

                        if (data1.Count == 0)
                            return ErrMeg.ErrString("1106", "");

                        var data2 = db.EI_INV_USABLEDATA.Where(o => o.tax_number == responce.id &&
                                                                o.year == responce.year.ToString() &&
                                                                o.period == responce.period.ToString() &&
                                                                o.status == "Y" &&
                                                                (o.user_account == "ALL" || o.user_account == responce.user)).
                                                                OrderBy(o => o.begin_no).ToList();
                        if (data2.Count == 0)
                            return ErrMeg.ErrString("1102", "");

                        if (responce.size > 0)
                        {
                            // 計算目前所有張數
                            int totalsize = 0;
                            foreach (var item in data2)
                            {
                                if (!string.IsNullOrEmpty(item.now_no))
                                    totalsize += int.Parse(item.end_no) - int.Parse(item.now_no);
                                else
                                    totalsize += int.Parse(item.end_no) - int.Parse(item.begin_no) + 1;
                            }

                            // 超過擁有張數
                            if (responce.size > totalsize)
                                return ErrMeg.ErrString("1103", "");

                            state = "取票更新資料";
                            //update EI_INV_USABLEDATA 目前票號
                            return InvoiceCRUD.Invoice_GetTrackList(responce.id, responce.year, responce.user, responce.period, responce.size);
                        }
                        else
                            return ErrMeg.ErrString("1101", "索取張數不得為0");
                    }
                }
                catch (Exception ex)
                {
                    string errormess = "[Message]:" + ex.Message +
                                       "[StackTrace]:" + ex.StackTrace +
                                       "[data]:" + JsonConvert.SerializeObject(responce);

                    SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                    return ErrMeg.ErrString("9999", "");
                }
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");
        }

        // 發票狀態查詢(起訖號碼)
        [HttpPost]
        public string GetInvoiceStatus([FromBody] jsonstring_GetInvoiceStatus responce)
        {
            string state = "";
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (responce != null)
            {
                try
                {
                    state = "資料判斷";
                    // 判斷使用者資料
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.start))
                        return ErrMeg.ErrString("1101", "發票號碼起號不可空白");
                    if (string.IsNullOrEmpty(responce.end))
                        return ErrMeg.ErrString("1101", "發票號碼迄號不可空白");

                    //存Log
                    SaveLog.Invoice("InvoiceController", functionname, responce, "", "", "");

                    // 判斷帳密資料
                    string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                    if (!string.IsNullOrEmpty(mess))
                        return mess;

                    // 判斷發票長度、存在
                    if (responce.start.Length != 10)
                        return ErrMeg.ErrString("1905", "");
                    else if (responce.end.Length != 10)
                        return ErrMeg.ErrString("1906", "");

                    // 判斷前面字軌
                    if (responce.start.Substring(0, 2) != responce.end.Substring(0, 2))
                        return ErrMeg.ErrString("1904", "");

                    //驗證起訖號碼是否顛倒
                    if (responce.start.CompareTo(responce.end) > 0)
                        return ErrMeg.ErrString("1907", "");

                    // 判斷發票存在
                    if (!CheckInvoice.FindInvoice(responce.id, responce.start))
                        return ErrMeg.ErrString("1905", "");
                    if (!CheckInvoice.FindInvoice(responce.id, responce.end))
                        return ErrMeg.ErrString("1906", "");

                    state = "抓取範圍資料";
                    // 取資料
                    string result = "";
                    using (einvoiceEntities db = new einvoiceEntities())
                    {
                        var data = (from a in db.EI_INV_MASTERDATA
                                    join b in db.EI_SYS_STATUS on a.inv_status equals b.code.ToString()
                                    where a.tax_number == responce.id &&
                                    (string.Compare(a.inv_no, responce.start) >= 0) &&
                                    (string.Compare(a.inv_no, responce.end) <= 0)
                                    select new MASTER
                                    {
                                        inv_no = a.inv_no,
                                        inv_type = a.inv_type,
                                        inv_status = a.inv_status,
                                        statusname = b.msg
                                    }).OrderBy(o => o.inv_no).ToList();

                        //串結果
                        if (data.Count > 0)
                        {
                            string inv_status = "";
                            foreach (var item in data)
                            {
                                string inv_type = "";
                                if (item.inv_type == "1")
                                    inv_type = "C0401";
                                else if (item.inv_type == "2")
                                    inv_type = "C0501";
                                else if (item.inv_type == "3")
                                    inv_type = "C0701";

                                inv_status = item.inv_status + "\",\"" + item.statusname;
                                result += "\"" + item.inv_no + "\",\"" + inv_type + "\",\"" + inv_status + "\"\r\n";
                            }
                        }
                    }
                    return ErrMeg.StatusString("0000", "", result);
                }
                catch (Exception ex)
                {
                    string errormess = "[Message]:" + ex.Message +
                                       "[StackTrace]:" + ex.StackTrace +
                                       "[data]:" + JsonConvert.SerializeObject(responce);

                    SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                    return ErrMeg.ErrString("9999", "");
                }
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");
        }

        // 發票狀態查詢(串發票)
        [HttpPost]
        public string GetInvoiceStatusIncsv([FromBody] jsonstring_GetInvoiceStatusIncsv responce)
        {
            string state = "";
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (responce != null)
            {
                try
                {
                    state = "資料判斷";
                    // 判斷使用者資料
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.invoice))
                        return ErrMeg.ErrString("1101", "發票號碼不可空白");

                    //存Log
                    SaveLog.Invoice("InvoiceController", functionname, responce, "", "", "");

                    // 判斷帳密資料
                    string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                    if (!string.IsNullOrEmpty(mess))
                        return mess;

                    //拆解invoice，檢查發票
                    string[] inv_list = responce.invoice.Split(',');
                    //distinct order by 
                    List<string> inv_list2 = inv_list.Distinct().OrderBy(o => o).ToList();

                    string result = "";
                    using (einvoiceEntities db = new einvoiceEntities())
                    {
                        foreach (var item in inv_list2)
                        {
                            state = "判斷長度";
                            if (item.Length != 10)
                                return ErrMeg.ErrString("1301", "發票" + item + "長度錯誤");

                            state = "取發票資料";
                            var data = (from a in db.EI_INV_MASTERDATA
                                        join b in db.EI_SYS_STATUS on a.inv_status equals b.code.ToString()
                                        where a.tax_number == responce.id &&
                                              a.inv_no == item
                                        select new MASTER
                                        {
                                            inv_no = a.inv_no,
                                            inv_type = a.inv_type,
                                            inv_status = a.inv_status,
                                            statusname = b.msg
                                        }).OrderBy(o => o.inv_no).ToList();

                            //串結果
                            string inv_status = "";
                            if (data.Count > 0)
                            {
                                string inv_type = "";
                                if (data[0].inv_type == "1")
                                    inv_type = "C0401";
                                else if (data[0].inv_type == "2")
                                    inv_type = "C0501";
                                else if (data[0].inv_type == "3")
                                    inv_type = "C0701";

                                inv_status = data[0].inv_status + "\",\"" + data[0].statusname;
                                result += "\"" + data[0].inv_no + "\",\"" + inv_type + "\",\"" + inv_status + "\"\r\n";
                            }
                            else
                            {
                                inv_status = 0 + "\",\"" + "無此發票";
                                result += "\"" + item + "\",\"" + "" + "\",\"" + inv_status + "\"\r\n";
                            }
                        }
                    }
                    int u = int.Parse("e123");

                    return ErrMeg.StatusString("0000", "", result);
                }
                catch (Exception ex)
                {
                    string errormess = "[Message]:" + ex.Message +
                                       "[StackTrace]:" + ex.StackTrace +
                                       "[data]:" + JsonConvert.SerializeObject(responce);

                    SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                    return ErrMeg.ErrString("9999", "");
                }
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");
        }

        // 發票字軌狀態查詢
        [HttpPost]
        public string GetTrackStatus([FromBody] jsonstring_GetTrackStatus responce)
        {
            string state = "";
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (responce != null)
            {
                state = "資料判斷";
                try
                {
                    int num1;
                    // 判斷使用者資料
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.year.ToString()))
                        return ErrMeg.ErrString("1101", "年份不可空白");
                    else if (int.TryParse(responce.year.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "年份必須是數字");
                    else if (int.Parse(responce.year.ToString()) == 0)
                        return ErrMeg.ErrString("1101", "年份不得為0");
                    if (string.IsNullOrEmpty(responce.period.ToString()))
                        return ErrMeg.ErrString("1101", "期別不可空白");
                    else if (int.TryParse(responce.period.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "期別必須是數字");
                    if (string.IsNullOrEmpty(responce.status.ToString()))
                        return ErrMeg.ErrString("1101", "字軌狀態選擇不可空白");
                    else if (int.TryParse(responce.status.ToString(), out num1) == false)
                        return ErrMeg.ErrString("1101", "字軌狀態選擇必須是數字");
                    else if (int.Parse(responce.status.ToString()) == 0)
                        return ErrMeg.ErrString("1101", "字軌狀態選擇不得為0");

                    //存Log
                    SaveLog.Invoice("InvoiceController", functionname, responce, "", "", "");

                    // 判斷帳密資料
                    string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                    if (!string.IsNullOrEmpty(mess))
                        return mess;

                    state = "字軌資料判斷";
                    string trackinfo = "";
                    int size = 0;
                    // 查詢是否有該年份期別的字軌資料
                    using (einvoiceEntities db = new einvoiceEntities())
                    {
                        var data = db.EI_INV_USABLEDATA.Where(o => o.tax_number == responce.id &&
                                                                   o.year == responce.year.ToString() &&
                                                                   o.period == responce.period.ToString() &&
                                                                   (o.user_account == responce.user || o.user_account == "ALL")).
                                                                   OrderBy(o => o.header).ThenBy(o => o.begin_no).ToList();

                        foreach (var item in data)
                        {
                            //所有字軌
                            if (responce.status == 1)
                            {
                                trackinfo += item.header + "," + item.begin_no + "," + item.end_no + ",";
                                size += int.Parse(item.end_no) - int.Parse(item.begin_no) + 1;
                            }
                            //已用字軌
                            else if (responce.status == 2)
                            {
                                //已用過票本
                                if (!string.IsNullOrEmpty(item.now_no))
                                {
                                    trackinfo += item.header + "," + item.begin_no + "," + item.now_no + ",";
                                    size += int.Parse(item.now_no) - int.Parse(item.begin_no) + 1;
                                }
                            }
                            //未用字軌
                            else if (responce.status == 3)
                            {
                                //已用過票本
                                if (!string.IsNullOrEmpty(item.now_no))
                                {
                                    // 目前發票下一張不等於end_no
                                    if (int.Parse(item.now_no) + 1 != int.Parse(item.end_no))
                                    {
                                        trackinfo += item.header + "," + (int.Parse(item.now_no) + 1).ToString().PadLeft(8, '0') + "," + item.end_no + ",";
                                        size += int.Parse(item.end_no) - int.Parse(item.now_no);
                                    }
                                    else
                                    {
                                        trackinfo += item.header + "," + item.end_no + ",";
                                        size += 1;
                                    }
                                }
                                //未用過票本
                                else
                                {
                                    trackinfo += item.header + "," + item.begin_no + "," + item.end_no + ",";
                                    size += int.Parse(item.end_no) - int.Parse(item.begin_no) + 1;
                                }
                            }
                            else
                                return ErrMeg.ErrString("1101", "字軌狀態選擇錯誤");
                        }
                        trackinfo = trackinfo.TrimEnd(',');
                        return ErrMeg.StatusString2("0000", "", trackinfo, size);
                    }
                }
                catch (Exception ex)
                {
                    string errormess = "[Message]:" + ex.Message +
                                       "[StackTrace]:" + ex.StackTrace +
                                       "[data]:" + JsonConvert.SerializeObject(responce);

                    SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                    return ErrMeg.ErrString("9999", "");
                }
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");
        }

        // 作廢發票
        [HttpPost]
        public string Invalid([FromBody] jsonstring_Invalid responce)
        {
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string state = "";
            try
            {
                if (responce != null)
                {
                    state = "基本判斷";
                    // 判斷使用者資料 
                    if (string.IsNullOrEmpty(responce.action))
                        return ErrMeg.ErrString("1101", "代碼不可空白");
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.csv.ToString().Replace("{", "").Replace("}", "")))
                        return ErrMeg.ErrString("1101", "檔案內文不可空白");
                }
                else
                    return ErrMeg.ErrString("1101", "json格式錯誤");

                // B2C存證、B2B存證、B2B交換
                if (responce.action.ToUpper() != "C0501" &&
                    responce.action.ToUpper() != "A0501" &&
                    responce.action.ToUpper() != "A0201")
                    return ErrMeg.ErrString("1101", "代碼錯誤");

                //存Log
                SaveLog.Invoice("InvoiceController", functionname, responce.csv, "", "", "");

                // 判斷帳密資料
                string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                if (!string.IsNullOrEmpty(mess))
                    return mess;

                state = "內容判斷";
                int k = 1;
                foreach (var item in responce.csv)
                {
                    // 發票號碼
                    if (string.IsNullOrEmpty(item.InvoiceNumber))
                        return ErrMeg.ErrString("1301", "第" + k + "筆發票號碼空白");

                    // 判斷發票長度
                    if (item.InvoiceNumber.Trim().Length != 10)
                        return ErrMeg.ErrString("1206", "第" + k + "筆發票號碼長度錯誤");

                    // 判斷字軌
                    string inv_no_h = item.InvoiceNumber.Substring(0, 2);
                    string inv_no_t = item.InvoiceNumber.Substring(2, 8);
                    System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[A-Za-z]+$");
                    System.Text.RegularExpressions.Regex reg2 = new System.Text.RegularExpressions.Regex(@"^[0-9]+$");
                    if ((!reg1.IsMatch(inv_no_h)) || (!reg2.IsMatch(inv_no_t)))
                        return ErrMeg.ErrString("1301", "第" + k + "筆發票字軌有誤");

                    // 發票時間
                    if (string.IsNullOrEmpty(item.InvoiceDate))
                        return ErrMeg.ErrString("1209", "第" + k + "筆發票時間空白");
                    k++;
                }

                k = 1;
                //超過每批發票上限500筆
                List<string> ch = new List<string>();
                using (einvoiceEntities db = new einvoiceEntities())
                {
                    foreach (var item in responce.csv)
                    {
                        //日期長度
                        if (item.InvoiceDate.Length != 8)
                            return ErrMeg.ErrString("1207", "發票" + item.InvoiceNumber);

                        //日期格式
                        DateTime _date;
                        string date = item.InvoiceDate.Substring(0, 4) + "/" +
                                      item.InvoiceDate.Substring(4, 2) + "/" +
                                      item.InvoiceDate.Substring(6, 2);

                        string date2 = item.InvoiceDate.Substring(0, 4) + "-" +
                                       item.InvoiceDate.Substring(4, 2) + "-" +
                                       item.InvoiceDate.Substring(6, 2);

                        //發票日期格式錯誤
                        if (!DateTime.TryParse(date, out _date))
                            return ErrMeg.ErrString("1207", "發票" + item.InvoiceNumber);

                        //發票日期大於今日日期
                        if (date.CompareTo(DateTime.Now.ToString("yyyy/MM/dd")) > 0)
                            return ErrMeg.ErrString("1208", "發票" + item.InvoiceNumber);

                        //超出單月15日上傳期限
                        //Ex: 1,2月份的發票超過3/15就不能上傳
                        string inv_ym = item.InvoiceDate.Substring(0, 6);

                        if (DateTime.Now.ToString("yyyyMM").CompareTo(inv_ym) > 0)
                        {
                            if ((DateTime.Now.Month % 2 == 1) && (DateTime.Now.Day > 15))
                                return ErrMeg.ErrString("1210", "發票" + item.InvoiceNumber);
                        }

                        //查詢發票
                        var data = db.EI_INV_MASTERDATA.Where(o => o.tax_number == responce.id &&
                                                                   o.inv_no == item.InvoiceNumber &&
                                                                   (o.inv_date == date || o.inv_date == date2)).ToList();

                        //無此發票
                        if (data.Count == 0)
                            return ErrMeg.ErrString("1206", "發票" + item.InvoiceNumber);
                        else
                        {
                            //發票開立中
                            if (data[0].inv_type == "1" && data[0].inv_status != "99")
                                return ErrMeg.ErrString("1404", "發票" + item.InvoiceNumber);
                            //發票作廢
                            else if (data[0].inv_type == "2")
                            {
                                //發票已作廢
                                if (data[0].inv_status == "99")
                                    return ErrMeg.ErrString("1405", "發票" + item.InvoiceNumber);
                                //發票作廢中
                                else
                                    return ErrMeg.ErrString("1406", "發票" + item.InvoiceNumber);
                            }
                            //發票註銷
                            else if (data[0].inv_type == "3")
                            {
                                //發票已註銷
                                if (data[0].inv_status == "99")
                                    return ErrMeg.ErrString("1407", "發票" + item.InvoiceNumber);
                                //發票註銷中
                                else
                                    return ErrMeg.ErrString("1408", "發票" + item.InvoiceNumber);
                            }
                        }
                        //紀錄發票張數
                        ch.Add(item.InvoiceNumber);
                    }
                }

                // 超過每批發票上限500筆
                if (ch.Distinct().ToList().Count() > 500)
                    return ErrMeg.ErrString("1205", "");

                state = "丟入table";
                // 商品明細
                DataTable InvoiceDetails = new DataTable();
                InvoiceDetails.Columns.Add("InvoiceNumber", typeof(string)); //發票號碼
                InvoiceDetails.Columns.Add("InvoiceDate", typeof(string));   //發票日期

                k = 0;
                foreach (var item in responce.csv)
                {
                    InvoiceDetails.Rows.Add();
                    InvoiceDetails.Rows[k]["InvoiceNumber"] = item.InvoiceNumber;
                    InvoiceDetails.Rows[k]["InvoiceDate"] = item.InvoiceDate;
                    k++;
                }
                InvoiceDetails.DefaultView.Sort = "InvoiceNumber";
                InvoiceDetails = InvoiceDetails.DefaultView.ToTable();


                state = "更新、紀錄資料";
                InvoiceCRUD.InvoiceInvalid(InvoiceDetails, responce.id, responce.user, responce.action);
            }
            catch (Exception ex)
            {
                string errormess = "[Message]:" + ex.Message +
                                   "[StackTrace]:" + ex.StackTrace +
                                   "[data]:" + JsonConvert.SerializeObject(responce);

                SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                return ErrMeg.ErrString("9999", "");
            }
            return ErrMeg.ErrString("0000", "發票作廢成功");
        }

        // 發票註銷
        [HttpPost]
        public string Cancellation([FromBody] jsonstring_Cancellation responce)
        {
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string state = "";
            try
            {
                if (responce != null)
                {
                    state = "基本判斷";
                    // 判斷使用者資料
                    if (string.IsNullOrEmpty(responce.id))
                        return ErrMeg.ErrString("1101", "統編不可空白");
                    if (string.IsNullOrEmpty(responce.user))
                        return ErrMeg.ErrString("1101", "帳號不可空白");
                    if (string.IsNullOrEmpty(responce.passwd))
                        return ErrMeg.ErrString("1101", "密碼不可空白");
                    if (string.IsNullOrEmpty(responce.csv.ToString().Replace("{", "").Replace("}", "")))
                        return ErrMeg.ErrString("1101", "檔案內文不可空白");
                }
                else
                    return ErrMeg.ErrString("1101", "json格式錯誤");

                //存Log
                SaveLog.Invoice("InvoiceController", functionname, responce.csv, "", "", "");

                //int i = int.Parse("D");

                // 判斷帳密資料
                string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
                if (!string.IsNullOrEmpty(mess))
                    return mess;

                state = "內容判斷";
                int k = 1;
                foreach (var item in responce.csv)
                {
                    // 發票號碼
                    if (string.IsNullOrEmpty(item.InvoiceNumber))
                        return ErrMeg.ErrString("1301", "第" + k + "筆發票號碼空白");

                    // 判斷發票長度
                    if (item.InvoiceNumber.Trim().Length != 10)
                        return ErrMeg.ErrString("1206", "第" + k + "筆發票號碼長度錯誤");

                    // 判斷字軌
                    string inv_no_h = item.InvoiceNumber.Substring(0, 2);
                    string inv_no_t = item.InvoiceNumber.Substring(2, 8);
                    System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[A-Za-z]+$");
                    System.Text.RegularExpressions.Regex reg2 = new System.Text.RegularExpressions.Regex(@"^[0-9]+$");
                    if ((!reg1.IsMatch(inv_no_h)) || (!reg2.IsMatch(inv_no_t)))
                        return ErrMeg.ErrString("1301", "第" + k + "筆發票字軌有誤");

                    // 發票時間
                    if (string.IsNullOrEmpty(item.InvoiceDate))
                        return ErrMeg.ErrString("1209", "第" + k + "筆發票時間空白");
                    k++;
                }

                //超過每批發票上限500筆
                List<string> ch = new List<string>();
                using (einvoiceEntities db = new einvoiceEntities())
                {
                    foreach (var item in responce.csv)
                    {
                        //日期長度
                        if (item.InvoiceDate.Length != 8)
                            return ErrMeg.ErrString("1207", "發票" + item.InvoiceNumber);

                        //日期格式
                        DateTime _date;
                        string date = item.InvoiceDate.Substring(0, 4) + "/" +
                                      item.InvoiceDate.Substring(4, 2) + "/" +
                                      item.InvoiceDate.Substring(6, 2);

                        string date2 = item.InvoiceDate.Substring(0, 4) + "-" +
                                       item.InvoiceDate.Substring(4, 2) + "-" +
                                       item.InvoiceDate.Substring(6, 2);

                        //發票日期格式錯誤
                        if (!DateTime.TryParse(date, out _date))
                            return ErrMeg.ErrString("1207", "發票" + item.InvoiceNumber);

                        //發票日期大於今日日期
                        if (date.CompareTo(DateTime.Now.ToString("yyyy/MM/dd")) > 0)
                            return ErrMeg.ErrString("1208", "發票" + item.InvoiceNumber);

                        //超出單月15日上傳期限
                        //Ex: 1,2月份的發票超過3/15就不能上傳
                        string inv_ym = item.InvoiceDate.Substring(0, 6);

                        if (DateTime.Now.ToString("yyyyMM").CompareTo(inv_ym) > 0)
                        {
                            if ((DateTime.Now.Month % 2 == 1) && (DateTime.Now.Day > 15))
                                return ErrMeg.ErrString("1210", "發票" + item.InvoiceNumber);
                        }

                        //查詢發票
                        var data = db.EI_INV_MASTERDATA.Where(o => o.tax_number == responce.id &&
                                                                   o.inv_no == item.InvoiceNumber &&
                                                                   (o.inv_date == date || o.inv_date == date2)).ToList();

                        //無此發票
                        if (data.Count == 0)
                            return ErrMeg.ErrString("1206", "發票" + item.InvoiceNumber);
                        else
                        {
                            //發票開立中
                            if (data[0].inv_type == "1" && data[0].inv_status != "99")
                                return ErrMeg.ErrString("1404", "發票" + item.InvoiceNumber);
                            //發票作廢
                            else if (data[0].inv_type == "2")
                            {
                                //發票已作廢
                                if (data[0].inv_status == "99")
                                    return ErrMeg.ErrString("1405", "發票" + item.InvoiceNumber);
                                //發票作廢中
                                else
                                    return ErrMeg.ErrString("1406", "發票" + item.InvoiceNumber);
                            }
                            //發票註銷
                            else if (data[0].inv_type == "3")
                            {
                                //發票已註銷
                                if (data[0].inv_status == "99")
                                    return ErrMeg.ErrString("1407", "發票" + item.InvoiceNumber);
                                //發票註銷中
                                else
                                    return ErrMeg.ErrString("1408", "發票" + item.InvoiceNumber);
                            }
                        }
                        //紀錄發票張數
                        ch.Add(item.InvoiceNumber);
                    }
                }

                // 超過每批發票上限500筆
                if (ch.Distinct().ToList().Count() > 500)
                    return ErrMeg.ErrString("1205", "");

                state = "丟入table";
                // 商品明細
                DataTable InvoiceDetails = new DataTable();
                InvoiceDetails.Columns.Add("InvoiceNumber", typeof(string)); //發票號碼
                InvoiceDetails.Columns.Add("InvoiceDate", typeof(string));   //發票日期

                k = 0;
                foreach (var item in responce.csv)
                {
                    InvoiceDetails.Rows.Add();
                    InvoiceDetails.Rows[k]["InvoiceNumber"] = item.InvoiceNumber;
                    InvoiceDetails.Rows[k]["InvoiceDate"] = item.InvoiceDate;
                    k++;
                }
                InvoiceDetails.DefaultView.Sort = "InvoiceNumber";
                InvoiceDetails = InvoiceDetails.DefaultView.ToTable();


                state = "更新、紀錄資料";
                InvoiceCRUD.InvoiceCancellation(InvoiceDetails, responce.id, responce.user);
            }
            catch (Exception ex)
            {
                string errormess = "[Message]:" + ex.Message +
                                   "[StackTrace]:" + ex.StackTrace +
                                   "[data]:" + JsonConvert.SerializeObject(responce);

                SaveLog.Invoice("InvoiceController", functionname, responce, "error", state, errormess);
                return ErrMeg.ErrString("9999", "");
            }
            return ErrMeg.ErrString("0000", "發票註銷成功");
        }

        // 下載電子發票證明聯(PDF)
        [HttpPost]
        public string InvoicePDF([FromBody] jsonstring_InvoicePDF responce)
        {
            string functionname = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string state = "";

            if (responce != null)
            {
                state = "基本判斷";
                // 判斷使用者資料
                if (string.IsNullOrEmpty(responce.id))
                    return ErrMeg.ErrString("1101", "統編不可空白");
                if (string.IsNullOrEmpty(responce.user))
                    return ErrMeg.ErrString("1101", "帳號不可空白");
                if (string.IsNullOrEmpty(responce.passwd))
                    return ErrMeg.ErrString("1101", "密碼不可空白");
                if (string.IsNullOrEmpty(responce.invoice))
                    return ErrMeg.ErrString("1101", "發票號碼不可空白");
                if (string.IsNullOrEmpty(responce.type.ToString()))
                    return ErrMeg.ErrString("1101", "印表類型不可空白");
            }
            else
                return ErrMeg.ErrString("1101", "json格式錯誤");

            //存Log
            SaveLog.Invoice("InvoiceController", functionname, responce, "", "", "");

            // 判斷帳密資料
            string mess = CheckUserData.CheckUser(responce.id, responce.user, responce.passwd);
            if (!string.IsNullOrEmpty(mess))
                return mess;

            // 判斷發票存在
            if (!CheckInvoice.FindInvoice(responce.id, responce.invoice))
                return ErrMeg.ErrString("1402", "");

            state = "印表類型判斷";
            if(responce.type != 1 && responce.type != 2 && responce.type != 3 )
                return ErrMeg.ErrString("1922", "");

            // 建立資料夾
            string sPath_CurrntFolder = System.Web.HttpContext.Current.Server.MapPath("/Datas/image/");
            if (!Directory.Exists(sPath_CurrntFolder))
                Directory.CreateDirectory(sPath_CurrntFolder);

            sPath_CurrntFolder = System.Web.HttpContext.Current.Server.MapPath("/Datas/pdf/");
            if (!Directory.Exists(sPath_CurrntFolder))
                Directory.CreateDirectory(sPath_CurrntFolder);


            //下載發票圖檔
            using (einvoiceEntities db = new einvoiceEntities())
            {
                var data = db.EI_BAS_COMPANY.Where(o => o.tax_number == responce.id && 
                                                        o.invoice_image != null &&
                                                        o.invoice_image.ToString().Trim() != "").ToList();
            }


            return "";
        }
    }
}
