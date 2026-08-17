// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;

namespace Microsoft.RoslynTools.Validation;

internal class Utilities
{
    public static async Task<string?> GetLatestShaFromPullRequestAsync(IProduct product, HttpClient gitHubClient, int prNumber, ILogger logger, CancellationToken cancellationToken)
    {
        var requestUri = $"/repos/dotnet/{product.Name.ToLower()}/pulls/{prNumber}";
        var response = await gitHubClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var prData = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return prData?["head"]?["sha"]?.ToString();
        }
        else
        {
            logger.LogError("Failed to retrieve PR data from GitHub. Status code: {StatusCode}", response.StatusCode);
        }

        return null;
    }
}
