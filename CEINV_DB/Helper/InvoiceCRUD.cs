using CEINV_DB.EFModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace CEINV_DB.Helper
{
    public class InvoiceCRUD
    {
        public static void InvoiceSave(DataTable Master, DataTable Details, string id, string user, string action)
        {
            using (einvoiceEntities db1 = new einvoiceEntities())
            {
                //Master.Rows[0]["InvoiceNumber"] = "YY00000127";
                string InvoiceNumber = Master.Rows[0]["InvoiceNumber"].ToString();
                string year = Master.Rows[0]["InvoiceDate"].ToString();

                // 判斷主檔
                var data = db1.EI_INV_MASTERDATA.Where(o => o.inv_no == InvoiceNumber).ToList();

                if (data.Count == 0)
                {
                    // 新增主檔===================================================================
                    foreach (DataRow oRow in Master.Rows)
                    {
                        db1.EI_INV_MASTERDATA.Add(new EI_INV_MASTERDATA()
                        {
                            tax_number = id,
                            inv_no = oRow["InvoiceNumber"].ToString(),
                            inv_date = oRow["InvoiceDate"].ToString().Substring(0, 4) + "-" +
                                       oRow["InvoiceDate"].ToString().Substring(4, 2) + "-" +
                                       oRow["InvoiceDate"].ToString().Substring(6, 2),
                            inv_time = oRow["InvoiceTime"].ToString(),
                            inv_type = oRow["TaxType"].ToString(),
                            inv_status = "1",
                            is_mailed = "N",
                            random_no = oRow["RandomNumber"].ToString(),
                            tax_type = oRow["TaxType"].ToString(),
                            tax_rate = decimal.Parse(oRow["TaxRate"].ToString()),
                            tax_amt = decimal.Parse(oRow["TaxAmount"].ToString()),
                            taxable_amt = decimal.Parse(oRow["SalesAmount"].ToString()),
                            freetax_amt = decimal.Parse(oRow["FreeTaxSalesAmount"].ToString()),
                            zerotax_amt = decimal.Parse(oRow["ZeroTaxSalesAmount"].ToString()),
                            total_amt = decimal.Parse(oRow["TotalAmount"].ToString()),
                            print_flag = oRow["PrintMark"].ToString(),
                            pass_way = oRow["CustomsClearanceMark"].ToString(),
                            buy_id = oRow["BuyerIdentifier"].ToString(),
                            buy_name = oRow["BuyerName"].ToString(),
                            buy_address = oRow["BuyerAddress"].ToString(),
                            buy_phone = oRow["BuyerTelephoneNumber"].ToString(),
                            buy_mail = oRow["BuyerEmailAddress"].ToString(),
                            car_type = oRow["CarrierType"].ToString(),
                            car_id1 = oRow["CarrierId1"].ToString(),
                            car_id2 = oRow["CarrierId2"].ToString(),
                            npoban = oRow["NPOBAN"].ToString(),
                            main_remark = oRow["MainRemark"].ToString(),
                            add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                            add_user = user
                        });
                    }
                }
                else
                {
                    // 註銷且完成的發票，update=======================================================================
                    if (data.FirstOrDefault().inv_type.ToString() == "3" && data[0].inv_status.ToString() == "99")
                    {
                        foreach (DataRow oRow in Master.Rows)
                        {
                            data.FirstOrDefault().tax_number = id;
                            data.FirstOrDefault().inv_no = InvoiceNumber;
                            data.FirstOrDefault().inv_date = oRow["InvoiceTime"].ToString().Substring(0, 4) + "/" +
                                                             oRow["InvoiceTime"].ToString().Substring(4, 2) + "/" +
                                                             oRow["InvoiceTime"].ToString().Substring(6, 2);
                            data.FirstOrDefault().inv_time = oRow["InvoiceTime"].ToString();
                            data.FirstOrDefault().inv_type = oRow["TaxType"].ToString();
                            data.FirstOrDefault().inv_status = "1";
                            data.FirstOrDefault().is_mailed = "N";
                            data.FirstOrDefault().random_no = oRow["RandomNumber"].ToString();
                            data.FirstOrDefault().tax_type = oRow["RandomNumber"].ToString();
                            data.FirstOrDefault().tax_rate = decimal.Parse(oRow["TaxRate"].ToString());
                            data.FirstOrDefault().tax_amt = decimal.Parse(oRow["TaxAmount"].ToString());
                            data.FirstOrDefault().taxable_amt = decimal.Parse(oRow["SalesAmount"].ToString());
                            data.FirstOrDefault().freetax_amt = decimal.Parse(oRow["FreeTaxSalesAmount"].ToString());
                            data.FirstOrDefault().zerotax_amt = decimal.Parse(oRow["ZeroTaxSalesAmount"].ToString());
                            data.FirstOrDefault().total_amt = decimal.Parse(oRow["TotalAmount"].ToString());
                            data.FirstOrDefault().print_flag = oRow["PrintMark"].ToString();
                            data.FirstOrDefault().pass_way = oRow["CustomsClearanceMark"].ToString();
                            data.FirstOrDefault().buy_id = oRow["BuyerIdentifier"].ToString();
                            data.FirstOrDefault().buy_name = oRow["BuyerName"].ToString();
                            data.FirstOrDefault().buy_address = oRow["BuyerAddress"].ToString();
                            data.FirstOrDefault().buy_phone = oRow["BuyerTelephoneNumber"].ToString();
                            data.FirstOrDefault().buy_mail = oRow["BuyerEmailAddress"].ToString();
                            data.FirstOrDefault().car_type = oRow["CarrierType"].ToString();
                            data.FirstOrDefault().car_id1 = oRow["CarrierId1"].ToString();
                            data.FirstOrDefault().car_id2 = oRow["CarrierId2"].ToString();
                            data.FirstOrDefault().npoban = oRow["NPOBAN"].ToString();
                            data.FirstOrDefault().main_remark = oRow["MainRemark"].ToString();
                            data.FirstOrDefault().add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                            data.FirstOrDefault().add_user = user;
                        }

                        // 刪除原本該筆發票的所有明細
                        var Details_Data = db1.EI_INV_DETAILDATA.Where(o => o.tax_number == id && o.inv_no == InvoiceNumber);

                        if (Details_Data != null)
                            db1.EI_INV_DETAILDATA.RemoveRange(Details_Data);
                    }
                }

                // 根據上面規則判斷紀錄==============================================================================
                if (data.Count == 0 || (data.FirstOrDefault().inv_type.ToString() == "3" && data[0].inv_status.ToString() == "99"))
                {
                    // 抓明細最大值============================================================================
                    short num = 1;

                    foreach (DataRow oRowo in Details.Rows)
                    {
                        db1.EI_INV_DETAILDATA.Add(new EI_INV_DETAILDATA()
                        {
                            tax_number = id,
                            inv_no = InvoiceNumber,
                            seq_no = num,
                            description = oRowo["Description"].ToString(),
                            quantity = int.Parse(oRowo["Quantity"].ToString()),
                            unit_price = decimal.Parse(oRowo["UnitPrice"].ToString()),
                            amount = decimal.Parse(oRowo["Amount"].ToString()),
                        });
                        num++;
                    }

                    // 新增要給Turnkey的訊息佇列
                    db1.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    {
                        msg_kind = action.ToUpper(),
                        send_id = id,
                        recv_id = "",
                        inv_no = InvoiceNumber,
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    });

                    #region
                    //if (action.ToUpper() == "C0401")
                    //{
                    //    db1.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    //    {
                    //        msg_kind = "C0401",
                    //        send_id = id,
                    //        recv_id = "",
                    //        inv_no = InvoiceNumber,
                    //        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    //    });
                    //}
                    //else if (action.ToUpper() == "A0401")
                    //{
                    //    // B2B存證
                    //    db1.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    //    {
                    //        msg_kind = "A0401",
                    //        send_id = id,
                    //        recv_id = "",
                    //        inv_no = InvoiceNumber,
                    //        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    //    });
                    //}
                    //else if (action.ToUpper() == "A0101")
                    //{
                    //    // B2B交換
                    //    db1.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    //    {
                    //        msg_kind = "A0101",
                    //        send_id = id,
                    //        recv_id = "",
                    //        inv_no = InvoiceNumber,
                    //        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    //    });
                    //}
                    //else
                    //{
                    //    return;
                    //}
                    #endregion

                    // 新增處理記錄
                    db1.EI_INV_PROCESSLOG.Add(new EI_INV_PROCESSLOG()
                    {
                        tax_number = id,
                        inv_no = InvoiceNumber,
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        proc_remark = "開立發票[待處理]",
                    });

                    // 更新已配號號碼================================================================
                    // 抓目前最大的發票
                    string max_inv_no = db1.EI_INV_MASTERDATA.Where(o => o.tax_number == id &&
                                                     o.inv_date.Substring(0, 4) == year.Substring(0, 4) &&
                                                     o.inv_no.Substring(0, 2) == InvoiceNumber.Substring(0, 2)).
                                          Select(o => o.inv_no).Max();

                    // 判斷當期第一筆字軌
                    string now_inv;
                    if (string.IsNullOrEmpty(max_inv_no))
                        now_inv = InvoiceNumber.Substring(2, 8);
                    if (int.Parse(max_inv_no.Substring(2, 8)) < int.Parse(InvoiceNumber.Substring(2, 8)))
                        now_inv = InvoiceNumber.Substring(2, 8);
                    else
                        now_inv = max_inv_no.Substring(2, 8);

                    var usa_data = db1.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                               o.year == year.Substring(0, 4) &&
                                                               o.header == InvoiceNumber.Substring(0, 2) &&
                                                               (string.Compare(now_inv, o.begin_no) >= 0) &&
                                                               (string.Compare(now_inv, o.end_no) <= 0)
                                                               //now_inv >= "Argentina" &&
                                                               //now_inv <= decimal.Parse(o.end_no)
                                                               ).FirstOrDefault();

                    usa_data.now_no = now_inv.ToString();
                    usa_data.modify_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    usa_data.modify_user = user;
                }
                db1.SaveChanges();
            }
        }
        public class trackinfo
        {
            public string track { get; set; }
            public string start { get; set; }
            public string end { get; set; }
        }

        public static string Invoice_GetTrackList(string id, int year, string user, int period, int size)
        {
            int i = 0;
            using (einvoiceEntities db = new einvoiceEntities())
            {
                var data = db.EI_INV_USABLEDATA.Where(o => o.tax_number == id &&
                                                       o.year == year.ToString() &&
                                                       o.period == period.ToString() &&
                                                       o.status == "Y" &&
                                                       (o.user_account == "ALL" || o.user_account == user)).
                                                       OrderBy(o => o.begin_no).ToList();

                int nowsize = size;
                string re = "";
                foreach (var item in data)
                {
                    re += item.header + ",";

                    //已有開立過的資料
                    if (!string.IsNullOrEmpty(data[i].now_no.ToString()))
                    {
                        // 此筆張數
                        int totalecount = int.Parse(item.end_no) - int.Parse(item.now_no);
                        re += (int.Parse(item.now_no) + 1).ToString().PadLeft(8, '0') + ",";

                        // 所取張數大於此筆張數
                        if (nowsize > totalecount)
                        {
                            item.now_no = item.end_no;
                            nowsize = nowsize - totalecount;
                        }
                        else
                        {
                            item.now_no = (int.Parse(item.now_no) + nowsize).ToString().PadLeft(8, '0');
                            nowsize = 0;
                        }
                    }
                    else //未開立過的資料
                    {
                        // 此筆張數
                        int totalecount = int.Parse(item.end_no) - int.Parse(item.begin_no) + 1;
                        re += item.begin_no + ",";

                        // 所取張數大於此筆張數
                        if (nowsize > totalecount)
                        {
                            item.now_no = item.end_no;
                            nowsize = nowsize - totalecount;
                        }
                        else
                        {
                            item.now_no = (int.Parse(item.begin_no) + nowsize - 1).ToString().PadLeft(8, '0');
                            nowsize = 0;
                        }
                    }

                    re += item.now_no + ",";
                    item.modify_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    item.modify_user = user;

                    // 分配完跳出
                    if (nowsize == 0)
                        break;
                    i++;
                }
                db.SaveChanges();

                re = re.TrimEnd(',');
                return ErrMeg.StatusString2("0000", "取票成功", re, size);
            }
        }

        // 作廢發票
        public static void InvoiceInvalid(DataTable InvoiceDetails, string id, string user, string action)
        {
            using (einvoiceEntities db = new einvoiceEntities())
            {
                foreach (DataRow oRow in InvoiceDetails.Rows)
                {
                    //針對舊資料
                    string date = oRow["InvoiceDate"].ToString().Substring(0, 4) + "/" +
                                  oRow["InvoiceDate"].ToString().Substring(4, 2) + "/" +
                                  oRow["InvoiceDate"].ToString().Substring(6, 2);

                    string date2 = oRow["InvoiceDate"].ToString().Substring(0, 4) + "-" +
                                   oRow["InvoiceDate"].ToString().Substring(4, 2) + "-" +
                                   oRow["InvoiceDate"].ToString().Substring(6, 2);

                    // 不知道為什麼直接用oRow["InvoiceNumber"]他不給過??
                    string InvoiceNumber = oRow["InvoiceNumber"].ToString();
                    var data = db.EI_INV_MASTERDATA.Where(o => o.tax_number == id &&
                                                               o.inv_no == InvoiceNumber &&
                                                               (o.inv_date == date || o.inv_date == date2)).ToList();

                    if (data.Count > 0)
                    {
                        data.FirstOrDefault().inv_type = "2";
                        data.FirstOrDefault().inv_status = "1";
                        data.FirstOrDefault().cancel_date = DateTime.Now.ToString("yyyy/MM/dd");
                        data.FirstOrDefault().cancel_time = DateTime.Now.ToString("HH:mm:ss");
                        data.FirstOrDefault().cancel_reason = "user cancel";
                    }
                    //新增要給Turnkey的訊息佇列
                    //B2C存證:C0501、B2B存證:A0501、B2B交換:A0201
                    db.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    {
                        msg_kind = action,
                        send_id = id,
                        recv_id = "",
                        inv_no = oRow["InvoiceNumber"].ToString(),
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    });

                    //新增處理記錄
                    db.EI_INV_PROCESSLOG.Add(new EI_INV_PROCESSLOG()
                    {
                        tax_number = id,
                        inv_no = oRow["InvoiceNumber"].ToString(),
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        proc_remark = "作廢發票[待處理]"
                    });
                }
                db.SaveChanges();
            }
        }

        // 註銷發票
        public static void InvoiceCancellation(DataTable InvoiceDetails, string id, string user)
        {
            using (einvoiceEntities db = new einvoiceEntities())
            {
                foreach (DataRow oRow in InvoiceDetails.Rows)
                {
                    //針對舊資料
                    string date = oRow["InvoiceDate"].ToString().Substring(0, 4) + "/" +
                                  oRow["InvoiceDate"].ToString().Substring(4, 2) + "/" +
                                  oRow["InvoiceDate"].ToString().Substring(6, 2);

                    string date2 = oRow["InvoiceDate"].ToString().Substring(0, 4) + "-" +
                                   oRow["InvoiceDate"].ToString().Substring(4, 2) + "-" +
                                   oRow["InvoiceDate"].ToString().Substring(6, 2);

                    // 不知道為什麼直接用oRow["InvoiceNumber"]他不給過??
                    string InvoiceNumber = oRow["InvoiceNumber"].ToString();
                    var data = db.EI_INV_MASTERDATA.Where(o => o.tax_number == id &&
                                                               o.inv_no == InvoiceNumber &&
                                                               (o.inv_date == date || o.inv_date == date2)).ToList();

                    if (data.Count > 0)
                    {
                        data.FirstOrDefault().inv_type = "3";
                        data.FirstOrDefault().inv_status = "1";
                        data.FirstOrDefault().void_date = DateTime.Now.ToString("yyyy/MM/dd");
                        data.FirstOrDefault().void_time = DateTime.Now.ToString("HH:mm:ss");
                        data.FirstOrDefault().void_reason = "user void";
                    }
                    //新增要給Turnkey的訊息佇列
                    //B2C存證:C0501、B2B存證:A0501、B2B交換:A0201
                    db.EI_INV_MESSAGEQUEUE.Add(new EI_INV_MESSAGEQUEUE()
                    {
                        msg_kind = "C0701",
                        send_id = id,
                        recv_id = "",
                        inv_no = oRow["InvoiceNumber"].ToString(),
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    });

                    //新增處理記錄
                    db.EI_INV_PROCESSLOG.Add(new EI_INV_PROCESSLOG()
                    {
                        tax_number = id,
                        inv_no = oRow["InvoiceNumber"].ToString(),
                        add_dt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        proc_remark = "註銷發票[待處理]"
                    });
                }
                db.SaveChanges();
            }
        }
    }
}
