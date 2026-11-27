// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.RoslynTools.Extensions;

using System.Net.Http.Headers;
using System.Text;

internal static class HttpClientExtensions
{
    public static async Task<string> GetGitHubFileContentAsync(this HttpClient client, string repository, string path, string gitRef)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{repository}/contents/{path}?ref={Uri.EscapeDataString(gitRef)}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static Task<HttpResponseMessage> PostAsyncAsJson(this HttpClient client, string requestUri, string body)
        => client.PostAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));

    public static Task<HttpResponseMessage> PutAsyncAsJson(this HttpClient client, string requestUri, string body)
        => client.PutAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));
}
