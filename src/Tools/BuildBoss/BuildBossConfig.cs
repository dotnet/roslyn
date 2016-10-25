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
        /// The set of projects and paths to exclude.
        /// </summary>
        [JsonProperty(PropertyName = "exclude")]
        public string[] Exclude { get; set; }
    }
}
