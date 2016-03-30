using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.HubServices
{
    internal interface IHubClient
    {
        Task<JToken> SendRequestAsync(string serviceName, string operationName, JToken data, CancellationToken cancellationToken);
    }
}