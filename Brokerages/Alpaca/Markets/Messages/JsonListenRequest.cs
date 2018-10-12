﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonListenRequest
    {
        internal sealed class JsonData
        {
            [JsonProperty(PropertyName = "streams", Required = Required.Always)]
            public List<String> Streams { get; set; }
        }

        [JsonProperty(PropertyName = "action", Required = Required.Always)]
        public JsonAction Action { get; set; }

        [JsonProperty(PropertyName = "data", Required = Required.Always)]
        public JsonData Data { get; set; }
    }
}