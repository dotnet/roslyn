// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Base class for <see cref="QuickInfoService"/>'s that delegate to <see cref="QuickInfoProvider"/>'s.
    /// </summary>
    internal abstract class QuickInfoServiceWithProviders : QuickInfoService
    {
        private readonly Workspace _workspace;
        private readonly string _language;
        private ImmutableArray<QuickInfoProvider> _externalProviders;
        private ImmutableArray<InternalQuickInfoProvider> _internalProviders;

        protected QuickInfoServiceWithProviders(Workspace workspace, string language)
        {
            _workspace = workspace;
            _language = language;
        }

        private ImmutableArray<QuickInfoProvider> GetExternalProviders()
        {
            if (_externalProviders.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _externalProviders, CreateProviders<QuickInfoProvider>(), default);
            }

            return _externalProviders;
        }

        private ImmutableArray<InternalQuickInfoProvider> GetInternalProviders()
        {
            if (_internalProviders.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _internalProviders, CreateProviders<InternalQuickInfoProvider>(), default);
            }

            return _internalProviders;
        }

        private ImmutableArray<TQuickInfoProvider> CreateProviders<TQuickInfoProvider>()
        {
            var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

            return ExtensionOrderer
                .Order(mefExporter.GetExports<TQuickInfoProvider, QuickInfoProviderMetadata>()
                    .Where(lz => lz.Metadata.Language == _language))
                .Select(lz => lz.Value)
                .ToImmutableArray();
        }

        public override async Task<QuickInfoItem?> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            if (document.SupportsSemanticModel)
            {
                var internalContext = await InternalQuickInfoContext.CreateAsync(document, position, cancellationToken).ConfigureAwait(false);
                var info = await GetQuickInfoAsync(internalContext).ConfigureAwait(false);
                if (info != null)
                {
                    return info;
                }
            }

            var context = new QuickInfoContext(document, position, cancellationToken);
            return await GetQuickInfoAsync(context).ConfigureAwait(false);
        }

        public async Task<QuickInfoItem?> GetQuickInfoAsync(SemanticModel semanticModel, int position, HostLanguageServices languageServices, CancellationToken cancellationToken)
        {
            var context = await InternalQuickInfoContext.CreateAsync(semanticModel, position, languageServices, cancellationToken).ConfigureAwait(false);
            return await GetQuickInfoAsync(context).ConfigureAwait(false);
        }

        private Task<QuickInfoItem?> GetQuickInfoAsync(InternalQuickInfoContext context)
            => GetQuickInfoAsync(GetInternalProviders(), context,
                getQuickInfoAsync: (provider, context) => provider.GetQuickInfoAsync(context));

        private Task<QuickInfoItem?> GetQuickInfoAsync(QuickInfoContext context)
            => GetQuickInfoAsync(GetExternalProviders(), context,
                getQuickInfoAsync: (provider, context) => provider.GetQuickInfoAsync(context));

        private async Task<QuickInfoItem?> GetQuickInfoAsync<TQuickInfoProvider, TQuickInfoContext>(
            ImmutableArray<TQuickInfoProvider> providers,
            TQuickInfoContext context,
            Func<TQuickInfoProvider, TQuickInfoContext, Task<QuickInfoItem?>> getQuickInfoAsync)
            where TQuickInfoProvider : class
            where TQuickInfoContext : AbstractQuickInfoContext
        {
            var extensionManager = _workspace.Services.GetRequiredService<IExtensionManager>();

            // returns the first non-empty quick info found (based on provider order)
            foreach (var provider in providers)
            {
                try
                {
                    if (!extensionManager.IsDisabled(provider))
                    {
                        var info = await getQuickInfoAsync(provider, context).ConfigureAwait(false);
                        if (info != null)
                        {
                            return info;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e) when (extensionManager.CanHandleException(provider, e))
                {
                    extensionManager.HandleException(provider, e);
                }
            }

            return null;
        }
    }
}
