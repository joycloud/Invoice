//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace CEINV_DB.EFModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class EI_TNK_SUMMARY
    {
        public int seq_no { get; set; }
        public string from_party_id { get; set; }
        public string to_party_id { get; set; }
        public string from_routing_id { get; set; }
        public string to_routing_id { get; set; }
        public string uuid { get; set; }
        public string msg_type { get; set; }
        public string status { get; set; }
        public string ref_no { get; set; }
        public string inv_date { get; set; }
        public string add_dt { get; set; }
    }
}
