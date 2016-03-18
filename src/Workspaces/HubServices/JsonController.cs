using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.HubServices
{
    public class JsonController : ApiController
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> s_idToTokenSouce =
            new ConcurrentDictionary<string, CancellationTokenSource>();

        internal HttpResponseMessage ProcessRequest(HubDataModel model, Func<JToken, CancellationToken, JToken> operation)
        {
            // Create a cancellation token to go along with this operation.  We'll cancel it
            // if we get a request to do so from the client we're connected to.
            var id = model.Id;
            var cancellationTokenSource = new CancellationTokenSource();
            s_idToTokenSouce[id] = cancellationTokenSource;

            try
            {
                var arg = JToken.Parse(model.Data);
                var json = ProcessRequestWorker(operation, arg, cancellationTokenSource.Token);
                var httpContent = new StringContent(json.ToString(), Encoding.UTF8);

                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json")
                {
                    CharSet = Encoding.UTF8.WebName
                };

                return new HttpResponseMessage
                {
                    Content = httpContent
                };
            }
            finally
            {
                // Release the token source once we're done processing the request.
                s_idToTokenSouce.TryRemove(id, out cancellationTokenSource);
            }
        }

        private JToken ProcessRequestWorker(
            Func<JToken, CancellationToken, JToken> operation, JToken arg, CancellationToken cancellationToken)
        {
            try
            {
                var data = operation(arg, cancellationToken);
                return new JObject(
                    new JProperty(HubProtocolConstants.TypePropertyName, HubProtocolConstants.RanToCompletionTypePropertyValue),
                    new JProperty(HubProtocolConstants.DataPropertyName, data));
            }
            catch(OperationCanceledException)
            {
                return new JObject(
                    new JProperty(HubProtocolConstants.TypePropertyName, HubProtocolConstants.CanceledTypePropertyValue));
            }
            catch (Exception e)
            {
                return new JObject(
                    new JProperty(HubProtocolConstants.TypePropertyName, HubProtocolConstants.FaultedTypePropertyValue),
                    new JProperty(HubProtocolConstants.DataPropertyName, e));
            }
        }


        internal void CancelOperation(HubDataModel model)
        {
            // Got a request to cancel an operation.  See if we still have the cancellation
            // token source for that request.  And, if so, cancel it.
            var id = model.Id;
            CancellationTokenSource cancellationTokenSource;
            if (s_idToTokenSouce.TryGetValue(id, out cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }
        }
    }

    public class HubDataModel
    {
        public string Id { get; set; }
        public string Data { get; set; }
    }
}
