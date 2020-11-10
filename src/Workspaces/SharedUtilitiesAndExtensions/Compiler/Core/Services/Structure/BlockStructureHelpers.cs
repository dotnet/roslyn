// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockStructureHelpers
    {
        public static async Task<BlockStructure> GetBlockStructureAsync(
            BlockStructureContext context,
            ImmutableArray<BlockStructureProvider> providers)
        {
            foreach (var provider in providers)
            {
                await provider.ProvideBlockStructureAsync(context).ConfigureAwait(false);
            }

            return CreateBlockStructure(context);
        }

        public static BlockStructure GetBlockStructure(
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
