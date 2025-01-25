// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http.Headers;
using Maestro.Common;
using Maestro.Common.AzureDevOpsTokens;
using Microsoft.RoslynTools.Authentication;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.RoslynTools.Utilities;

internal record RemoteConnections : IDisposable
{
    public AzDOConnection DevDivConnection { get; }
    public AzDOConnection DncEngConnection { get; }
    public HttpClient GitHubClient { get; }

    public RemoteConnections(RoslynToolsSettings settings)
    {
        DevDivConnection = CreateDevDivAzdoConnection(settings.GetDevDivAzDOTokenProvider());
        DncEngConnection = CreateDncEngAzdoConnection(settings.GetDncEngAzDOTokenProvider());

        var gitHubTokenProvider = settings.GetGitHubTokenProvider();
        GitHubClient = CreateGitHubClient(gitHubTokenProvider);
    }

    public void Dispose()
    {
        DevDivConnection.Dispose();
        DncEngConnection.Dispose();
        GitHubClient.Dispose();
    }

    private static AzDOConnection CreateDncEngAzdoConnection(IAzureDevOpsTokenProvider tokenProvider)
        => new(CreateVssConnection("dnceng", tokenProvider), "internal");

    private static AzDOConnection CreateDevDivAzdoConnection(IAzureDevOpsTokenProvider tokenProvider)
        => new(CreateVssConnection("devdiv", tokenProvider), "DevDiv");

    private static VssConnection CreateVssConnection(string accountName, IAzureDevOpsTokenProvider tokenProvider)
    {
        var accountUri = new Uri($"https://dev.azure.com/{accountName}");
        var creds = new VssCredentials(new VssBasicCredential("", tokenProvider.GetTokenForAccount(accountName)));
        return new VssConnection(accountUri, creds);
    }

    private static HttpClient CreateGitHubClient(IRemoteTokenProvider tokenProvider)
    {
        const string GitHubApiUri = "https://api.github.com";

        var client = new HttpClient(new HttpClientHandler { CheckCertificateRevocationList = true })
        {
            BaseAddress = new Uri(GitHubApiUri)
        };
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        client.DefaultRequestHeaders.Add("User-Agent", "dotnet-roslyn-tools");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenProvider.GetTokenForRepository(GitHubApiUri));

        return client;
    }
}
