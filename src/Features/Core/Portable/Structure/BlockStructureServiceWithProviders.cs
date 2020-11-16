// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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
            return await GetBlockStructureAsync(context).ConfigureAwait(false);
        }

        public override BlockStructure GetBlockStructure(
            Document document,
            CancellationToken cancellationToken)
        {
            var context = CreateContextAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
            return GetBlockStructure(context);
        }

        public async Task<BlockStructure> GetBlockStructureAsync(BlockStructureContext context)
            => await GetBlockStructureAsync(context, _providers).ConfigureAwait(false);

        public BlockStructure GetBlockStructure(BlockStructureContext context)
            => GetBlockStructure(context, _providers);

        private static async Task<BlockStructureContext> CreateContextAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var options = document.Project.Solution.Options;
            var isMetadataAsSource = document.Project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource;
            var optionProvider = new BlockStructureOptionProvider(options, isMetadataAsSource);
            return new BlockStructureContext(syntaxTree, optionProvider, cancellationToken);
        }

        private static async Task<BlockStructure> GetBlockStructureAsync(
            BlockStructureContext context,
            ImmutableArray<BlockStructureProvider> providers)
        {
            foreach (var provider in providers)
            {
                await provider.ProvideBlockStructureAsync(context).ConfigureAwait(false);
            }

            return CreateBlockStructure(context);
        }

        private static BlockStructure GetBlockStructure(
            BlockStructureContext context,
            ImmutableArray<BlockStructureProvider> providers)
        {
            foreach (var provider in providers)
            {
                provider.ProvideBlockStructure(context);
            }

            return CreateBlockStructure(context);
        }

        private static BlockStructure CreateBlockStructure(BlockStructureContext context)
        {
            var language = context.SyntaxTree.Options.Language;

            var showIndentGuidesForCodeLevelConstructs = context.OptionProvider.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, language);
            var showIndentGuidesForDeclarationLevelConstructs = context.OptionProvider.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, language);
            var showIndentGuidesForCommentsAndPreprocessorRegions = context.OptionProvider.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, language);
            var showOutliningForCodeLevelConstructs = context.OptionProvider.GetOption(BlockStructureOptions.ShowOutliningForCodeLevelConstructs, language);
            var showOutliningForDeclarationLevelConstructs = context.OptionProvider.GetOption(BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, language);
            var showOutliningForCommentsAndPreprocessorRegions = context.OptionProvider.GetOption(BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, language);

            using var _ = ArrayBuilder<BlockSpan>.GetInstance(out var updatedSpans);
            foreach (var span in context.Spans)
            {
                var updatedSpan = UpdateBlockSpan(span,
                    showIndentGuidesForCodeLevelConstructs,
                    showIndentGuidesForDeclarationLevelConstructs,
                    showIndentGuidesForCommentsAndPreprocessorRegions,
                    showOutliningForCodeLevelConstructs,
                    showOutliningForDeclarationLevelConstructs,
                    showOutliningForCommentsAndPreprocessorRegions);
                updatedSpans.Add(updatedSpan);
            }

            return new BlockStructure(updatedSpans.ToImmutable());
        }

        private static BlockSpan UpdateBlockSpan(BlockSpan blockSpan,
            bool showIndentGuidesForCodeLevelConstructs,
            bool showIndentGuidesForDeclarationLevelConstructs,
            bool showIndentGuidesForCommentsAndPreprocessorRegions,
            bool showOutliningForCodeLevelConstructs,
            bool showOutliningForDeclarationLevelConstructs,
            bool showOutliningForCommentsAndPreprocessorRegions)
        {
            var type = blockSpan.Type;

            var isTopLevel = BlockTypes.IsDeclarationLevelConstruct(type);
            var isMemberLevel = BlockTypes.IsCodeLevelConstruct(type);
            var isComment = BlockTypes.IsCommentOrPreprocessorRegion(type);

            if ((!showIndentGuidesForDeclarationLevelConstructs && isTopLevel) ||
                (!showIndentGuidesForCodeLevelConstructs && isMemberLevel) ||
                (!showIndentGuidesForCommentsAndPreprocessorRegions && isComment))
            {
                type = BlockTypes.Nonstructural;
            }

            var isCollapsible = blockSpan.IsCollapsible;
            if (isCollapsible)
            {
                if ((!showOutliningForDeclarationLevelConstructs && isTopLevel) ||
                    (!showOutliningForCodeLevelConstructs && isMemberLevel) ||
                    (!showOutliningForCommentsAndPreprocessorRegions && isComment))
                {
                    isCollapsible = false;
                }
            }

            return blockSpan.With(type: type, isCollapsible: isCollapsible);
        }
    }
}
