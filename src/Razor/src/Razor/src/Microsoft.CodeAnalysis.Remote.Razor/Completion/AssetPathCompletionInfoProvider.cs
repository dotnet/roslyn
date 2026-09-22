// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.StaticWebAssets;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

/// <summary>
/// Assembles the project-scoped data asset path completion needs, caching each half against the
/// immutable object it derives from so that repeated requests against an unchanged project are
/// dictionary lookups.
/// </summary>
[Export(typeof(AssetPathCompletionInfoProvider)), Shared]
[method: ImportingConstructor]
internal sealed class AssetPathCompletionInfoProvider(StaticWebAssetsProvider staticWebAssetsProvider)
{
    private readonly StaticWebAssetsProvider _staticWebAssetsProvider = staticWebAssetsProvider;
    private readonly ConditionalWeakTable<TagHelperCollection, AssetPathCompletionInfo> _allowListCache = new();

    public async ValueTask<AssetPathCompletionInfo> GetInfoAsync(RemoteProjectSnapshot projectSnapshot, CancellationToken cancellationToken)
    {
        var tagHelpers = await projectSnapshot.GetTagHelpersAsync(cancellationToken).ConfigureAwait(false);

        if (!_allowListCache.TryGetValue(tagHelpers, out var info))
        {
            info = _allowListCache.GetValue(tagHelpers, AssetPathCompletionInfo.Create);
        }

        // Nothing opted in, so the asset list would have nowhere to be offered.
        if (ReferenceEquals(info, AssetPathCompletionInfo.Empty))
        {
            return AssetPathCompletionInfo.Empty;
        }

        var assets = await _staticWebAssetsProvider.GetAssetsAsync(projectSnapshot, cancellationToken).ConfigureAwait(false);

        return assets.IsEmpty ? AssetPathCompletionInfo.Empty : info.WithAssets(assets);
    }
}
