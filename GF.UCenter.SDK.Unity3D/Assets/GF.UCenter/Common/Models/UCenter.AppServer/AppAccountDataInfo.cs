﻿using System;
using System.Runtime.Serialization;

namespace GF.UCenter.Common.Portable
{
    [DataContract]
    public class AppAccountDataInfo
    {
        [DataMember]
        public string AccountId;
        [DataMember]
        public string AppId;
        [DataMember]
        public string AppSecret;
        [DataMember]
        public string Data;
    }
}