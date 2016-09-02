// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class BlockStructureServiceWithProviders : BlockStructureService
    {
        private readonly Workspace _workspace;
        private readonly ImmutableArray<BlockStructureProvider> _providers;

        protected BlockStructureServiceWithProviders(Workspace workspace)
        {
            _workspace = workspace;
            _providers = GetBuiltInProviders().Concat(GetImportedProviders());
        }

        /// <summary>
        /// Returns the providers always available to the service.
        /// This does not included providers imported via MEF composition.
        /// </summary>
        protected virtual ImmutableArray<BlockStructureProvider> GetBuiltInProviders()
        {
            return ImmutableArray<BlockStructureProvider>.Empty;
        }

        private ImmutableArray<BlockStructureProvider> GetImportedProviders()
        {
            var language = this.Language;
            var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

            var providers = mefExporter.GetExports<BlockStructureProvider, LanguageMetadata>()
                                       .Where(lz => lz.Metadata.Language == language)
                                       .Select(lz => lz.Value);

            return providers.ToImmutableArray();
        }

        public override async Task<BlockStructure> GetBlockStructureAsync(
            Document document, CancellationToken cancellationToken)
        {
            var context = new BlockStructureContext(document, cancellationToken);
            foreach (var provider in _providers)
            {
                await provider.ProvideBlockStructureAsync(context).ConfigureAwait(false);
            }

            return new BlockStructure(context.Spans);
        }

        public override BlockStructure GetBlockStructure(
            Document document, CancellationToken cancellationToken)
        {
            var context = new BlockStructureContext(document, cancellationToken);
            foreach (var provider in _providers)
            {
                provider.ProvideBlockStructure(context);
            }

            return new BlockStructure(context.Spans);
        }
    }
}