﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class einvoiceEntities : DbContext
    {
        public einvoiceEntities()
            : base("name=einvoiceEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<EI_BAS_COMPANY> EI_BAS_COMPANY { get; set; }
        public virtual DbSet<EI_BRC_DETAILDATA> EI_BRC_DETAILDATA { get; set; }
        public virtual DbSet<EI_BRC_MASTERDATA> EI_BRC_MASTERDATA { get; set; }
        public virtual DbSet<EI_DIS_DETAILDATA> EI_DIS_DETAILDATA { get; set; }
        public virtual DbSet<EI_DIS_MASTERDATA> EI_DIS_MASTERDATA { get; set; }
        public virtual DbSet<EI_DIS_MESSAGEQUEUE> EI_DIS_MESSAGEQUEUE { get; set; }
        public virtual DbSet<EI_DIS_PROCESSLOG> EI_DIS_PROCESSLOG { get; set; }
        public virtual DbSet<EI_ERR_MESSAGE> EI_ERR_MESSAGE { get; set; }
        public virtual DbSet<EI_INV_DETAILDATA> EI_INV_DETAILDATA { get; set; }
        public virtual DbSet<EI_INV_MASTERDATA> EI_INV_MASTERDATA { get; set; }
        public virtual DbSet<EI_INV_MESSAGEQUEUE> EI_INV_MESSAGEQUEUE { get; set; }
        public virtual DbSet<EI_INV_PRIZE> EI_INV_PRIZE { get; set; }
        public virtual DbSet<EI_INV_PROCESSLOG> EI_INV_PROCESSLOG { get; set; }
        public virtual DbSet<EI_INV_USABLEDATA> EI_INV_USABLEDATA { get; set; }
        public virtual DbSet<EI_INV_YEARDATA> EI_INV_YEARDATA { get; set; }
        public virtual DbSet<EI_MBR_CARRIER> EI_MBR_CARRIER { get; set; }
        public virtual DbSet<EI_SYS_ADMINDATA> EI_SYS_ADMINDATA { get; set; }
        public virtual DbSet<EI_SYS_USERDATA> EI_SYS_USERDATA { get; set; }
        public virtual DbSet<EI_TNK_SUMMARY> EI_TNK_SUMMARY { get; set; }
        public virtual DbSet<FROM_CONFIG> FROM_CONFIG { get; set; }
        public virtual DbSet<SCHEDULE_CONFIG> SCHEDULE_CONFIG { get; set; }
        public virtual DbSet<SIGN_CONFIG> SIGN_CONFIG { get; set; }
        public virtual DbSet<TASK_CONFIG> TASK_CONFIG { get; set; }
        public virtual DbSet<TO_CONFIG> TO_CONFIG { get; set; }
        public virtual DbSet<TURNKEY_MESSAGE_LOG> TURNKEY_MESSAGE_LOG { get; set; }
        public virtual DbSet<TURNKEY_MESSAGE_LOG_DETAIL> TURNKEY_MESSAGE_LOG_DETAIL { get; set; }
        public virtual DbSet<TURNKEY_SEQUENCE> TURNKEY_SEQUENCE { get; set; }
        public virtual DbSet<TURNKEY_SYSEVENT_LOG> TURNKEY_SYSEVENT_LOG { get; set; }
        public virtual DbSet<TURNKEY_TRANSPORT_CONFIG> TURNKEY_TRANSPORT_CONFIG { get; set; }
        public virtual DbSet<TURNKEY_USER_PROFILE> TURNKEY_USER_PROFILE { get; set; }
        public virtual DbSet<EI_SYS_LOG> EI_SYS_LOG { get; set; }
        public virtual DbSet<EI_SYS_STATUS> EI_SYS_STATUS { get; set; }
    }
}
