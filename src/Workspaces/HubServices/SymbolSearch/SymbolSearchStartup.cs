using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.VsHub.Server.ServiceModulesCommon;
using Owin;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    internal class SymbolSearchStartup : ServiceStartupBase
    {
        protected override void BuildApplication(IAppBuilder app)
        {
            // Code provided by VsHub team.
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);

            // TODO(cyrusn): Spin up thread to keep databases up to date.
        }
    }
}
