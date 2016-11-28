using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal sealed class FileJson
    {
        [JsonProperty(PropertyName = "sign")]
        public string[] SignList { get; set; }

        [JsonProperty(PropertyName = "exclude")]
        public string[] ExcludeList { get; set; }

        public FileJson()
        {

        }
    }
}
