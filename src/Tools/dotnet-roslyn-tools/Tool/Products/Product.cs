// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.RoslynTools.Products;

internal static class Product
{
    public static readonly IProduct[] AllProducts = [
        new Roslyn(),
        new Razor(),
        new TypeScript(),
        new FSharp(),
    ];

    public static IProduct? GetProductByName(string name)
        => AllProducts.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static IProduct? GetProductByRepoUrl(string repoUrl)
        => AllProducts.FirstOrDefault(p =>
            {
                if (repoUrl.StartsWith("https://dev.azure.com/devdiv/"))
                    repoUrl = repoUrl.Replace("https://dev.azure.com/devdiv/", "https://devdiv.visualstudio.com/");
                else if (repoUrl.StartsWith("https://dev.azure.com/dnceng/"))
                    repoUrl = repoUrl.Replace("https://dev.azure.com/dnceng/", "https://dnceng.visualstudio.com/");

                return repoUrl.Equals(p.RepoHttpBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                   repoUrl.Equals(p.RepoSshBaseUrl, StringComparison.OrdinalIgnoreCase) ||
                   (p.InternalRepoBaseUrl is not null && repoUrl.Equals(p.InternalRepoBaseUrl, StringComparison.OrdinalIgnoreCase));
            });
}
