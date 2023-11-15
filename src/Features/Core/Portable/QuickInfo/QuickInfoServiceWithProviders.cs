// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Base class for <see cref="QuickInfoService"/>'s that delegate to <see cref="QuickInfoProvider"/>'s.
    /// </summary>
    internal abstract class QuickInfoServiceWithProviders : QuickInfoService
    {
        private readonly LanguageServices _services;
        private ImmutableArray<QuickInfoProvider> _providers;

        protected QuickInfoServiceWithProviders(LanguageServices services)
        {
            _services = services;
        }

        private ImmutableArray<QuickInfoProvider> GetProviders()
        {
            if (_providers.IsDefault)
            {
                var mefExporter = _services.SolutionServices.ExportProvider;

                var providers = ExtensionOrderer
                    .Order(mefExporter.GetExports<QuickInfoProvider, QuickInfoProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == _services.Language))
                    .Select(lz => lz.Value)
                    .ToImmutableArray();

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
                    () =>
                    {
                        var context = new QuickInfoContext(document, position, options, cancellationToken);

                        return provider.GetQuickInfoAsync(context);
                    },
                    defaultValue: null).ConfigureAwait(false);
                if (info != null)
                    return info;
            }

            return null;
        }

        internal async Task<QuickInfoItem?> GetQuickInfoAsync(SemanticModel semanticModel, int position, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var extensionManager = _services.SolutionServices.GetRequiredService<IExtensionManager>();

            // returns the first non-empty quick info found (based on provider order)
            foreach (var provider in GetProviders().OfType<CommonQuickInfoProvider>())
            {
                var info = await extensionManager.PerformFunctionAsync(
                    provider,
                    () =>
                    {
                        var context = new CommonQuickInfoContext(_services.SolutionServices, semanticModel, position, options, cancellationToken);

                        return provider.GetQuickInfoAsync(context);
                    },
                    defaultValue: null).ConfigureAwait(false);
                if (info != null)
                    return info;
            }

            return null;
        }
    }
}
