// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.Validation;

internal static class PRValidation
{
    public static async Task<int> RunPRValidationPipeline(
        string productName,
        RemoteConnections remoteConnections,
        ILogger logger,
        int prNumber,
        string? sha,
        string? branch)
    {
        try
        {
            var cancellationToken = CancellationToken.None;
            var product = VSBranchInfo.AllProducts.Single(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            // If the user doesn't pass the SHA, retrieve the most recent from the PR.
            if (sha is null)
            {
                sha = await Utilities.GetLatestShaFromPullRequestAsync(product, remoteConnections.GitHubClient, prNumber, logger, cancellationToken).ConfigureAwait(false);
                if (sha is null)
                {
                    logger.LogError("Could not find a SHA for the given PR number.");
                    return -1;
                }
            }

            var repositoryParams = new Dictionary<string, RepositoryResourceParameters>
            {
                {
                    "self", new RepositoryResourceParameters
                    {
                        RefName = $"refs/heads/{branch}",
                        Version = sha
                    }
                }
            };

            var runPipelineParameters = new RunPipelineParameters
            {
                Resources = new RunResourcesParameters
                {

                },
                TemplateParameters = new Dictionary<string, string> { { "PRNumber", prNumber.ToString() }, { "CommitSHA", sha } }
            };

            await remoteConnections.DevDivConnection.TryRunPipelineAsync(product.PRValidationPipelineName, repositoryParams, runPipelineParameters, logger).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "{Message}", e.Message);
            return -1;
        }

        return 0;
    }
}
