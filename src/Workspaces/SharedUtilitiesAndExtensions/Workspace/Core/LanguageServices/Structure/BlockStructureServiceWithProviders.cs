// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

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
            => ImmutableArray<BlockStructureProvider>.Empty;

        private ImmutableArray<BlockStructureProvider> GetImportedProviders()
        {
            var language = Language;
            var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

            var providers = mefExporter.GetExports<BlockStructureProvider, LanguageMetadata>()
                                       .Where(lz => lz.Metadata.Language == language)
                                       .Select(lz => lz.Value);

            return providers.ToImmutableArray();
        }

        public override async Task<BlockStructure> GetBlockStructureAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var context = await CreateContextAsync(document, cancellationToken).ConfigureAwait(false);
            return await BlockStructureHelpers.GetBlockStructureAsync(context, _providers).ConfigureAwait(false);
        }

        public override BlockStructure GetBlockStructure(
            Document document,
            CancellationToken cancellationToken)
        {
            var context = CreateContextAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
            return BlockStructureHelpers.GetBlockStructure(context, _providers);
        }

        private static async Task<BlockStructureContext> CreateContextAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

#if CODE_STYLE
            var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
#else
            var options = document.Project.Solution.Options;
#endif

            var isMetadataAsSource = document.Project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource;
            var optionProvider = new BlockStructureOptionProvider(options, isMetadataAsSource);
            return new BlockStructureContext(syntaxTree, optionProvider, cancellationToken);
        }
    }
}
