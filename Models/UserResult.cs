using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhatShouldWeWatch.Models
{
    public class UserResult
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime createdAt { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string userId { get; set; }

        [JsonProperty(PropertyName = "resultId")]
        public int resultId { get; set; }
    }
}