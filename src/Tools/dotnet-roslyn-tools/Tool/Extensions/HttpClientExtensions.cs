// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.Extensions;

using System.Text;

internal static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PostAsyncAsJson(this HttpClient client, string requestUri, string body)
        => client.PostAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));

    public static Task<HttpResponseMessage> PutAsyncAsJson(this HttpClient client, string requestUri, string body)
        => client.PutAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));
}
