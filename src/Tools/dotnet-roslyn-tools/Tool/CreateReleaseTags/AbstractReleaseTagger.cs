// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.CreateReleaseTags;

internal abstract class AbstractReleaseTagger<TRelease, TBuild>
    where TRelease : ReleaseInformation
    where TBuild : BuildInformation
{
    public abstract string Name { get; }
    public abstract Task<ImmutableArray<TRelease>> GetReleasesAsync(RemoteConnections connections);
    public abstract string GetTagName(TRelease release);
    public abstract Task<TBuild?> TryGetBuildAsync(RemoteConnections connections, IProduct product, TRelease release);
    public abstract Task<TBuild?> TryGetBuildAsync(RemoteConnections connections, IProduct product, TBuild vmrBuild);
    public abstract string CreateTagMessage(IProduct product, TRelease release, TBuild build);

    public async Task CreateReleaseTagsAsync(
        RemoteConnections connections,
        IProduct product,
        Repository repository,
        ImmutableHashSet<string> existingTags,
        ILogger logger)
    {
        logger.LogInformation("Loading {Name} releases...", Name);

        var releases = await GetReleasesAsync(connections);

        logger.LogWarning("Tagging {ReleasesLength} releases...", releases.Length);

        var tagsAdded = 0;

        foreach (var release in releases)
        {
            var tagName = GetTagName(release);
            if (existingTags.Contains(tagName))
            {
                logger.LogWarning("Tag '{TagName}' already exists.", tagName);
                continue;
            }

            logger.LogTrace("Tag '{TagName}' is missing.", tagName);

            var build = await TryGetBuildAsync(connections, product, release);
            if (build is null)
            {
                logger.LogWarning("Unable to find the build for '{TagName}'.", tagName);
                continue;
            }

            if (TryCreateTag(release, build, tagName))
            {
                tagsAdded++;
            }
            else
            {
                await TryTagFromVmrBuildAsync(release, build, tagName);
            }
        }

        logger.LogInformation("Added {TagsAdded} tags. Tagging complete.", tagsAdded);

        bool TryCreateTag(TRelease release, TBuild build, string tagName)
        {
            try
            {
                logger.LogInformation("Tagging {CommitSha} as '{TagName}'.", build.CommitSha, tagName);

                var message = CreateTagMessage(product, release, build);

                repository.ApplyTag(tagName, build.CommitSha, new Signature(product.GitUserName, product.GitEmail, when: release.CreationTime), message);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unable to tag the commit '{CommitSha}' with '{TagName}': {Message}", build.CommitSha, tagName, ex.Message);

                return false;
            }
        }

        async Task TryTagFromVmrBuildAsync(TRelease release, TBuild build, string tagName)
        {
            logger.LogInformation("Attempting to tag build from VMR commit.");

            var vmrBuild = await TryGetBuildAsync(connections, product, build);
            if (vmrBuild is null)
            {
                logger.LogWarning("Unable to find repo information for the VMR build.");
                return;
            }

            if (TryCreateTag(release, vmrBuild, tagName))
            {
                tagsAdded++;
            }
        }
    }
}
