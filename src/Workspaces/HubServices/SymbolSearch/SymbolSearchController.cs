using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.CodeAnalysis.Elfie.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public class DataModel
    {
        public string Value { get; set; }
    }

    public class SymbolSearchController : ApiController
    {
        private static ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = 
            new ConcurrentDictionary<string, AddReferenceDatabase>();

        [HttpPost]
        [Route("SymbolSearch/" + nameof(OnPackageSourcesChanged))]
        public string OnPackageSourcesChanged(DataModel value)
        {
            // var packageSources = JArray.Parse(packageSourcesJson);
            return "received: " + JArray.Parse(value.Value);
        }

        [HttpGet]
        [Route("SymbolSearch/FindPackagesWithType/{queryJson}")]
        public string FindPackagesWithType(string queryJson)
        {
            var query = JObject.Parse(queryJson);
            return new JObject().ToString();
        }

        [HttpGet]
        [Route("SymbolSearch/FindReferenceAssembliesWithType/{queryJson}")]
        public string FindReferenceAssembliesWithType(string queryJson)
        {
            var query = JObject.Parse(queryJson);
            return new JObject().ToString();
        }
    }
}