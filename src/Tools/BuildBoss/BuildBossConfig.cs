using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    public sealed class BuildBossConfig
    {
        /// <summary>
        /// The solutions targetted by this scan
        /// </summary>
        [JsonProperty(PropertyName = "targets")]
        public string[] Targets { get; set; }
    }
}
