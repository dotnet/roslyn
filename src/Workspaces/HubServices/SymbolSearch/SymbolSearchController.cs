using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.CodeAnalysis.Elfie.Model;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public class SymbolSearchController : JsonController
    {
        private static ConcurrentDictionary<string, AddReferenceDatabase> _sourceToDatabase = 
            new ConcurrentDictionary<string, AddReferenceDatabase>();

        [HttpPost]
        [Route("SymbolSearch/" + nameof(HubProtocolConstants.CancelOperationName))]
        public new void CancelOperation(HubDataModel value)
        {
            base.CancelOperation(value);
        }

        [HttpPost]
        [Route("SymbolSearch/" + nameof(OnPackageSourcesChanged))]
        public HttpResponseMessage OnPackageSourcesChanged(HubDataModel model)
        {
            return base.ProcessRequest(model, (arg, c) =>
            {
                return new JObject(new JProperty("received", arg.ToString()));
            });
        }

#if false
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
#endif
    }
}