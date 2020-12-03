using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CEINV_DB.Helper
{
    public class ConverTaxexcluded
    {
        public static DataTable Taxexcluded(DataTable Details)
        {
            int i = 0;
            decimal ProductPrice_total = 0;
            decimal tax_total = 0;
            decimal rate = Convert.ToDecimal(0.05);

            // 2020.08.06 轉成未稅 (目前只有應稅算法)==============================================================================
            foreach (DataRow item in Details.Rows)
            {
                if (item["TaxType"].ToString() == "1")
                {
                    decimal totalprice = decimal.Parse(item["Amount"].ToString()); //應稅單價
                    decimal duty_totalprice = Math.Round(decimal.Parse((totalprice / (1 + rate)).ToString("0.0")), 0, MidpointRounding.AwayFromZero); //未稅單價                                        
                    decimal tax = Math.Round(decimal.Parse((duty_totalprice * rate).ToString("0.0")), 0, MidpointRounding.AwayFromZero); //總稅額                                        
                    item["tax"] = tax;

                    // 第一階段先平未稅額，單價=未稅額+稅額
                    if (duty_totalprice + tax != decimal.Parse(item["Amount"].ToString()))
                        duty_totalprice = decimal.Parse(item["Amount"].ToString()) - tax;

                    item["dutyPrice"] = Math.Round(duty_totalprice / Convert.ToInt32(item["Quantity"]), 2, MidpointRounding.AwayFromZero);
                    decimal amount = Convert.ToDecimal(item["dutyPrice"].ToString()) * Convert.ToInt32(item["Quantity"]); //未稅總額
                    item["amount"] = amount;
                }
                else
                    item["tax"] = 0;
                
                // 應稅
                if (item["TaxType"].ToString() == "1")
                    ProductPrice_total += decimal.Parse(item["Amount"].ToString());

                tax_total += decimal.Parse(item["tax"].ToString());
                i++;
            }

            decimal ProductPrice_total_duty = Math.Round(decimal.Parse((ProductPrice_total / (1 + rate)).ToString("0.0")), 0, MidpointRounding.AwayFromZero); // 應稅總額換算未稅
            decimal ProductPrice_total_dutytax = Math.Round(decimal.Parse((ProductPrice_total_duty * rate).ToString("0.0")), 0, MidpointRounding.AwayFromZero); // 換算稅額

            if (ProductPrice_total_dutytax != tax_total)
            {
                decimal diff_tax = Math.Abs(ProductPrice_total_dutytax - tax_total);
                bool isSumHigher = tax_total.CompareTo(ProductPrice_total_dutytax) > 0;

                // 稅額高到低排序
                Details.DefaultView.Sort = "tax desc";
                Details = Details.DefaultView.ToTable();

                for (int y = 0; y < Details.Select("TaxType = 1").Count(); y++)
                {
                    if (diff_tax > 0)
                    {
                        if (isSumHigher)
                        {
                            Details.Rows[y]["tax"] = Convert.ToDecimal(Details.Rows[y]["tax"]) - 1;
                            Details.Rows[y]["amount"] = Convert.ToDecimal(Details.Rows[y]["amount"]) + 1;
                        }
                        else
                        {
                            Details.Rows[y]["tax"] = Convert.ToDecimal(Details.Rows[y]["tax"]) + 1;
                            Details.Rows[y]["amount"] = Convert.ToDecimal(Details.Rows[y]["amount"]) - 1;
                        }

                        Details.Rows[y]["dutyPrice"] = Math.Round(Convert.ToDecimal(Details.Rows[y]["amount"]) /
                                                           Convert.ToDecimal(Details.Rows[y]["Quantity"]), 2, MidpointRounding.AwayFromZero);
                        diff_tax--;
                    }
                    else
                        break;
                }
            }

            return Details;
        }
    }
}
