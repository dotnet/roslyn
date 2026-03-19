// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

/// <summary>
/// Base class for <see cref="QuickInfoService"/>'s that delegate to <see cref="QuickInfoProvider"/>'s.
/// </summary>
internal abstract class QuickInfoServiceWithProviders(LanguageServices services) : QuickInfoService
{
    private readonly LanguageServices _services = services;
    private ImmutableArray<QuickInfoProvider> _providers;

    private ImmutableArray<QuickInfoProvider> GetProviders()
    {
        if (_providers.IsDefault)
        {
            var mefExporter = _services.SolutionServices.ExportProvider;

            var providers = ExtensionOrderer
                .Order(mefExporter
                    .GetExports<QuickInfoProvider, QuickInfoProviderMetadata>()
                    .Where(lz => lz.Metadata.Language == _services.Language))
                .SelectAsArray(lz => lz.Value);

            ImmutableInterlocked.InterlockedCompareExchange(ref _providers, providers, default);
        }

        return _providers;
    }

    internal override async Task<QuickInfoItem?> GetQuickInfoAsync(Document document, int position, SymbolDescriptionOptions options, CancellationToken cancellationToken)
    {
        var extensionManager = _services.SolutionServices.GetRequiredService<IExtensionManager>();

        // returns the first non-empty quick info found (based on provider order)
        foreach (var provider in GetProviders())
        {
            var info = await extensionManager.PerformFunctionAsync(
                provider,
                cancellationToken => provider.GetQuickInfoAsync(new QuickInfoContext(document, position, options, cancellationToken)),
                defaultValue: null,
                cancellationToken).ConfigureAwait(false);
            if (info != null)
                return info;
        }

        return null;
    }
}
