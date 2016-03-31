﻿using System;
using System.Runtime.Serialization;

namespace GF.UCenter.Common.Portable
{
    [DataContract]
    public class AccountResetPasswordResponse : AccountRequestResponse
    {
        [DataMember]
        public string Token { get; private set; }
        [DataMember]
        public DateTime LastLoginDateTime { get; private set; }

        public override void ApplyEntity(AccountResponse account)
        {
            this.Token = account.Token;
            this.LastLoginDateTime = account.LastLoginDateTime;
            base.ApplyEntity(account);
        }
    }
}