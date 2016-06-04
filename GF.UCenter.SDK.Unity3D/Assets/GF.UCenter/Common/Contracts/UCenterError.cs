﻿using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GF.UCenter.Common.Portable.Contracts
{
    [DataContract]
    public class UCenterError
    {
        [DataMember]
        [JsonProperty("code")]
        public UCenterErrorCode ErrorCode { get; set; }

        [DataMember]
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}