// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class QuickInfoServiceWithProviders : QuickInfoService
    {
        private readonly Workspace _workspace;
        private readonly string _language;
        private List<Lazy<QuickInfoElementProvider, QuickInfoProviderMetadata>> _importedProviders;

        protected QuickInfoServiceWithProviders(Workspace workspace, string language)
        {
            _workspace = workspace;
            _language = language;
        }

        private IEnumerable<Lazy<QuickInfoElementProvider, QuickInfoProviderMetadata>> GetImportedProviders()
        {
            if (_importedProviders == null)
            {
                var language = _language;
                var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<QuickInfoElementProvider, QuickInfoProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == language)
                        ).ToList();

                Interlocked.CompareExchange(ref _importedProviders, providers, null);
            }

            return _importedProviders;
        }

        public override async Task<QuickInfoData> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var providers = GetImportedProviders();

            foreach (var lazyProvider in providers)
            {
                // this really just returns the first one that is not null?
                var info = await lazyProvider.Value.GetQuickInfoElementAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (info != null && info != QuickInfoData.Empty)
                {
                    return info;
                }
            }

            return QuickInfoData.Empty;
        }
    }
}