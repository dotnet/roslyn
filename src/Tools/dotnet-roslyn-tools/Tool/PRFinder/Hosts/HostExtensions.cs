// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.PRFinder.Hosts;

internal static class HostExtensions
{
    public static bool TryGetHost(
        this IProduct product,
        RemoteConnections connections,
        ILogger logger,
        [NotNullWhen(returnValue: true)] out IRepositoryHost? host)
    {
        var isGitHub = product.IsGitHubRepo();
        if (!isGitHub && !product.IsAzdoRepo())
        {
            host = null;
            return false;
        }

        host = isGitHub
            ? new Hosts.GitHub(product.RepoHttpBaseUrl, connections, logger)
            : new Hosts.Azure(product.RepoHttpBaseUrl);
        return true;
    }
}
