﻿using System.Runtime.Serialization;

namespace GameCloud.UCenter.Common.Portable.Models.AppClient
{
    [DataContract]
    public class AccountResetPasswordInfo
    {
        [DataMember]
        public string AccountName { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string SuperPassword { get; set; }
    }
}