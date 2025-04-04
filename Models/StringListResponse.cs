using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Models
{
    public class StringListResponse
    {

        [JsonProperty("models")]
        public List<string>? Models { get; set; }

    }
}
