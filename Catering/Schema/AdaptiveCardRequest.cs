using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catering.Schema
{
    public class AdaptiveCardRequest
    {
        [JsonProperty("action")]
        public AdaptiveCardAction Action { get; set; }
    }
}
