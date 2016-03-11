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
    internal class SymbolSearchController : ApiController
    {
        private static ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = 
            new ConcurrentDictionary<string, AddReferenceDatabase>();

        [HttpPost]
        [Route("SymbolSearch/OnPackageSourcesChanged/{packageSourcesJson}")]
        public string OnPackageSourcesChanged(string packageSourcesJson)
        {
            var packageSources = JArray.Parse(packageSourcesJson);
            return "received: " + packageSourcesJson;
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