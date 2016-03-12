using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.VsHub.Server.ServiceModulesCommon;
using Owin;

namespace Microsoft.CodeAnalysis.HubServices
{
    // Our registry guid is:
    // F4F0CA6A-5A28-4985-BD41-E71BF2090BDD
    public class Startup : ServiceStartupBase
    {
        protected override void BuildApplication(IAppBuilder app)
        {
            // Code provided by VsHub team.
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);
        }
    }
}