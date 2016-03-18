using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.VsHub;
using VSShellInterop = Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis.HubServices;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServices.HubServices
{
    [Export(typeof(IHubClient)), Shared]
    internal class HubClient : IHubClient
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1);
        private readonly Dictionary<string, IVsHubServiceHttpClient> _serviceNameToClient =
            new Dictionary<string, IVsHubServiceHttpClient>();

        private readonly SVsServiceProvider _serviceProvider;

        private IAsyncServiceProvider _asyncServiceProvider;
        private IVsHubService _hubService;

        private static int requestId = 0;

        [ImportingConstructor]
        public HubClient(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _asyncServiceProvider = (IAsyncServiceProvider)_serviceProvider.GetService(typeof(VSShellInterop.SAsyncServiceProvider));
        }

        public async Task<JToken> SendRequestAsync(
            string serviceName, string operationName, JToken data, CancellationToken cancellationToken)
        {
            // Create a new id to track this request.
            var id = Interlocked.Increment(ref requestId);

            // Find the appropriate client for this service.
            var client = await GetClientAsync(serviceName, cancellationToken).ConfigureAwait(false);

            return await SendRequestAsync(client, id, serviceName, operationName, data, 
                registerCancellationCallback: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<JToken> SendRequestAsync(
            IVsHubServiceHttpClient client,
            int id, string serviceName, string operationName, JToken data, 
            bool registerCancellationCallback, CancellationToken cancellationToken)
        {
            // If we hear about this cancellation token being canceled, then report to the 
            // VSHub server to stop processing this request.
            using (var registration = GetCancellationRegistration(client, id, serviceName, registerCancellationCallback, cancellationToken))
            {
                return await SendRequestAsync(client, id, operationName, data, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<JToken> SendRequestAsync(
            IVsHubServiceHttpClient client, int id, string operationName, JToken data, CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(operationName, CreateHttpContent(id, data), cancellationToken).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseObject = JObject.Parse(responseString);
            return ProcessResponse(responseObject, cancellationToken);
        }

        private static JToken ProcessResponse(JObject responseObject, CancellationToken cancellationToken)
        {
            var typeValue = responseObject.Value<string>(HubProtocolConstants.TypePropertyName);
            switch (typeValue)
            {
                case HubProtocolConstants.CanceledTypePropertyValue:
                    // Operation was canceled on the server.  That means we canceled locally.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw ExceptionUtilities.Unreachable;

                case HubProtocolConstants.RanToCompletionTypePropertyValue:
                    return responseObject[HubProtocolConstants.DataPropertyName];

                case HubProtocolConstants.FaultedTypePropertyValue:
                default:
                    return null;
            }
        }

        private IDisposable GetCancellationRegistration(
            IVsHubServiceHttpClient client,
            int id, string serviceName,
            bool registerCancellationCallback, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled && registerCancellationCallback)
            {
                var callback = (Action)(() =>
                {
                    // Send the 'CancelOperation' request.  Note that we pass 'false' for 
                    // registerCancellationCallback so we don't keep attacking cancellation
                    // callbacks.  Note that we also do this in a fire and forget fashion.
                    // Remote cancellation is performed in a 'best effort' fashion.
                    var unused = SendRequestAsync(client, id, serviceName, HubProtocolConstants.CancelOperationName, new JObject(),
                            registerCancellationCallback: false, cancellationToken: cancellationToken);
                });

                return cancellationToken.Register(callback);
            }

            return null;
        }

        private HttpContent CreateHttpContent(int id, JToken data)
        {
            var json = new JObject(
                new JProperty(HubProtocolConstants.IdPropertyName, id.ToString()),
                new JProperty(HubProtocolConstants.DataPropertyName, data.ToString()));

            var httpContent = new StringContent(json.ToString(), Encoding.UTF8);

            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = Encoding.UTF8.WebName
            };

            return httpContent;
        }

        private async Task<IVsHubServiceHttpClient> GetClientAsync(string serviceName, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                IVsHubServiceHttpClient client;
                if (!_serviceNameToClient.TryGetValue(serviceName, out client))
                {
                    var hubService = await this.GetHubServiceAsync(cancellationToken).ConfigureAwait(false);
                    var clientFactory = hubService.GetService<IVsHubServiceHttpClientFactory>();
                    client = clientFactory.CreateHttpClient("Microsoft.CodeAnalysis.HubServices", serviceName, new Version(1, 0), useDefaultCredentials: true);
                    await client.StartHeartbeatAsync().ConfigureAwait(false);

                    _serviceNameToClient.Add(serviceName, client);
                }

                return client;
            }
        }

        private async Task<IVsHubService> GetHubServiceAsync(CancellationToken cancellationToken)
        {
            if (_hubService == null)
            {
                var serviceObject = await _asyncServiceProvider.GetServiceAsync(typeof(VSShellInterop.SVsHubService)).ConfigureAwait(false);
                _hubService = (IVsHubService)serviceObject;
            }

            return _hubService;
        }
    }
}