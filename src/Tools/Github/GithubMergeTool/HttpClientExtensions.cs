using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GithubMergeTool
{
    internal static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PostAsyncAsJson(this HttpClient client, string requestUri, string body)
            => client.PostAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));
    }
}
