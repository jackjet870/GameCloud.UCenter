﻿namespace GF.UCenter.Common.Portable
{
    using System.Runtime.Serialization;

    [DataContract]
    public class AccountResetPasswordResponse : AccountRequestResponse
    {
        public override void ApplyEntity(AccountResponse account)
        {
            base.ApplyEntity(account);
        }
    }
}