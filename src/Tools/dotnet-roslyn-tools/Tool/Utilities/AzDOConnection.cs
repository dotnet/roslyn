// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net;
using Azure.Security.KeyVault.Secrets;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.RoslynTools.Utilities;

internal sealed class AzDOConnection : IDisposable
{
    private bool _disposed = false;

    public string BuildProjectName { get; }
    private VssConnection Connection { get; }
    public GitHttpClient GitClient { get; }
    public BuildHttpClient BuildClient { get; }
    public HttpClient NuGetClient { get; }
    public FileContainerHttpClient ContainerClient { get; }
    public ProjectHttpClient ProjectClient { get; }

    public AzDOConnection(string azdoUrl, string projectName, string azdoToken)
    {
        BuildProjectName = projectName;

        Connection = new VssConnection(new Uri(azdoUrl), new VssBasicCredential(string.Empty, azdoToken));
        NuGetClient = new HttpClient();

        GitClient = Connection.GetClient<GitHttpClient>();
        BuildClient = Connection.GetClient<BuildHttpClient>();

        ContainerClient = Connection.GetClient<FileContainerHttpClient>();

        ProjectClient = Connection.GetClient<ProjectHttpClient>();
    }

    public async Task<List<Build>?> TryGetBuildsAsync(string pipelineName, string buildNumber)
    {
        try
        {
            var buildDefinition = (await BuildClient.GetDefinitionsAsync(BuildProjectName, name: pipelineName)).Single();
            var builds = await BuildClient.GetBuildsAsync(buildDefinition.Project.Id, definitions: new[] { buildDefinition.Id }, buildNumber: buildNumber);
            return builds;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            Connection.Dispose();
            NuGetClient.Dispose();
            GitClient.Dispose();
            BuildClient.Dispose();
            ContainerClient.Dispose();
            ProjectClient.Dispose();
        }
    }
}
