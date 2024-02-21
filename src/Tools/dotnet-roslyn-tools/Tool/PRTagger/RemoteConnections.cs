// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http.Headers;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.PRTagger;

internal record RemoteConnections : IDisposable
{
    public RemoteConnections(RoslynToolsSettings settings)
    {
        this.DevDivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);;
        this.DncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);;

        var gitHubClient = new HttpClient
        {
            BaseAddress = new("https://api.github.com/")
        };
        gitHubClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        gitHubClient.DefaultRequestHeaders.Add("User-Agent", "roslyn-tool-tagger");
        gitHubClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            settings.GitHubToken);
        this.GitHubClient = gitHubClient;
    }

    public void Dispose()
    {
        DevDivConnection.Dispose();
        DncengConnection.Dispose();
        GitHubClient.Dispose();
    }

    public AzDOConnection DevDivConnection { get; }
    public AzDOConnection DncengConnection { get; }
    public HttpClient GitHubClient { get; }
}
