using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CEINV_API.Models
{
    public class Types
    {
        // 開立發票
        public class jsonstring_Create
        {
            public string action { get; set; }
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public object csv { get; set; }
        }

        public class A0101A0401
        {
            public string InvoiceNumber { get; set; }// 發票號碼
            public string InvoiceDate { get; set; }//發票開立日期
            public string InvoiceTime { get; set; }//發票開立時間
            public string BuyerIdentifier { get; set; }//買方統編
            public string BuyerName { get; set; }//買方名稱
            public string BuyerAddress { get; set; }//買方地址
            public string BuyerTelephoneNumber { get; set; }//買方電話
            public string BuyerEmailAddress { get; set; }//買方Email
            public string SalesAmount { get; set; }//應稅銷售額
            public string FreeTaxSalesAmount { get; set; }//免稅銷售額
            public string ZeroTaxSalesAmount { get; set; }//零稅率銷售額
            public int TaxType { get; set; }//稅別
            public decimal TaxRate { get; set; }//稅率
            public int TaxAmount { get; set; }//稅額
            public decimal TotalAmount { get; set; }//總計
            public string CustomsClearanceMark { get; set; }//通關方式
            public string MainRemark { get; set; }//主備註
            public string NPOBAN { get; set; }//捐贈碼          
            public List<Details> Details { get; set; }//明細
        }


        public class C0401
        {
            public string InvoiceNumber { get; set; }// 發票號碼
            public string InvoiceDate { get; set; }//發票開立日期
            public string InvoiceTime { get; set; }//發票開立時間
            public string BuyerIdentifier { get; set; }//買方統編
            public string BuyerName { get; set; }//買方名稱
            public string BuyerAddress { get; set; }//買方地址
            public string BuyerTelephoneNumber { get; set; }//買方電話
            public string BuyerEmailAddress { get; set; }//買方Email
            public string SalesAmount { get; set; }//應稅銷售額
            public string FreeTaxSalesAmount { get; set; }//免稅銷售額
            public string ZeroTaxSalesAmount { get; set; }//零稅率銷售額
            public int TaxType { get; set; }//稅別
            public decimal TaxRate { get; set; }//稅率
            public string TaxAmount { get; set; }//稅額
            public string TotalAmount { get; set; }//總計
            public string PrintMark { get; set; }//列印註記
            public string RandomNumber { get; set; }//隨機碼
            public string CustomsClearanceMark { get; set; }//通關方式
            public string MainRemark { get; set; }//主備註
            public string CarrierType { get; set; }//載具類別
            public string CarrierId1 { get; set; }//載具明碼
            public string CarrierId2 { get; set; }//載具隱碼
            public string NPOBAN { get; set; }//捐贈碼          
            public List<Details> Details { get; set; }//明細
        }

        public class Details
        {
            public string Description { get; set; }//品名
            public int Quantity { get; set; }//數量
            public decimal UnitPrice { get; set; }//單價
            public int Amount { get; set; }//小計金額
            public string Remark { get; set; }//明細備註
            public int TaxType { get; set; }//稅別
        }

        // 欄位錯誤
        public class ErrorColumns
        {
            public string Columns { get; set; }
            public string Remark { get; set; }
        }

        // 發票字軌取號
        public class jsonstring_GetTrackList
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public int year { get; set; }
            public int period { get; set; }
            public int size { get; set; }
        }

        // 發票狀態查詢
        public class jsonstring_GetInvoiceStatus
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public string start { get; set; }
            public string end { get; set; }
        }

        // 發票狀態查詢(逐筆)
        public class jsonstring_GetInvoiceStatusIncsv
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public string invoice { get; set; }
        }

        // 發票狀態暫存用
        public class MASTER
        {
            public string inv_no { get; set; }
            public string inv_type { get; set; }
            public string inv_status { get; set; }
            public string statusname { get; set; }
        }

        // 發票字軌狀態查詢
        public class jsonstring_GetTrackStatus
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public int year { get; set; }
            public int period { get; set; }
            public int status { get; set; }
        }

        // 發票作廢
        public class jsonstring_Invalid
        {
            public string action { get; set; }
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public List<C0501> csv { get; set; }
        }

        public class C0501
        {
            public string InvoiceNumber { get; set; }
            public string InvoiceDate { get; set; }
        }

        // 發票註銷
        public class jsonstring_Cancellation
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public List<C0501> csv { get; set; }
        }

        // 發票註銷
        public class jsonstring_InvoicePDF
        {
            public string id { get; set; }
            public string user { get; set; }
            public string passwd { get; set; }
            public string invoice { get; set; }
            public int type { get; set; }
        }
    }
}