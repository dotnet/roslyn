// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.RoslynTools.VS;

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal static partial class CreateReleaseTags
{
    public static async Task<int> CreateReleaseTagsAsync(string productName, RemoteConnections connections, ILogger logger)
    {
        var product = Product.GetProductByName(productName)!;

        logger.LogInformation("Opening {ProductName} repo and gathering tags...", product.Name);

        if (!TryOpenProductRespository(product, logger, out var repository))
        {
            return 1;
        }

        var existingTags = repository.Tags.Select(t => t.FriendlyName).ToImmutableHashSet();

        await new SdkReleaseTagger().CreateReleaseTagsAsync(connections, product, repository, existingTags, logger);
        await new VsReleaseTagger().CreateReleaseTagsAsync(connections, product, repository, existingTags, logger);

        logger.LogInformation("Tagging complete.");

        return 0;

        static bool TryOpenProductRespository(
            IProduct product,
            ILogger logger,
            [NotNullWhen(returnValue: true)] out Repository? repository)
        {
            try
            {
                repository = new Repository(Environment.CurrentDirectory);

                if (!repository.Network.Remotes.Any(r =>
                    r.Url.Equals(product.RepoHttpBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                    r.Url.Equals(product.RepoSshBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                    r.Url.Equals(product.RepoHttpBaseUrl + ".git", StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogError("Repo does not appear to be the {ProductName} repo. Please fetch tags if tags are not already fetched and try again.", product.Name);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to open repo. Please run from your repo directory for the product in question.");
                repository = null;
                return false;
            }
        }
    }
}
