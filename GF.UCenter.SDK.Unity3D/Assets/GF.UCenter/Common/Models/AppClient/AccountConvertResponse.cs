﻿using System.Runtime.Serialization;

namespace GF.UCenter.Common.Portable.Models.AppClient
{
    [DataContract]
    public class AccountConvertResponse : AccountRequestResponse
    {
        public override void ApplyEntity(AccountResponse account)
        {
            base.ApplyEntity(account);
        }
    }
}