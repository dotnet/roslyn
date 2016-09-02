// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private List<Lazy<QuickInfoProvider, QuickInfoProviderMetadata>> _importedProviders;

        protected QuickInfoServiceWithProviders(Workspace workspace, string language)
        {
            _workspace = workspace;
            _language = language;
        }

        private IEnumerable<Lazy<QuickInfoProvider, QuickInfoProviderMetadata>> GetImportedProviders()
        {
            if (_importedProviders == null)
            {
                var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<QuickInfoProvider, QuickInfoProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == _language)
                        ).ToList();

                Interlocked.CompareExchange(ref _importedProviders, providers, null);
            }

            return _importedProviders;
        }

        public override async Task<QuickInfoItem> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // returns the first quick info provided by the providers (based on provider order)
            foreach (var lazyProvider in GetImportedProviders())
            {
                var info = await lazyProvider.Value.GetQuickInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (info != null && !info.IsEmpty)
                {
                    return info;
                }
            }

            return QuickInfoItem.Empty;
        }
    }
}