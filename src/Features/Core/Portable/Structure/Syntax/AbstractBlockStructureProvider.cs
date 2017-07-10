// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Structure
{
    /// <summary>
    /// Note: this type is for subclassing by the VB and C# provider only.
    /// It presumes that the language supports Syntax Trees.
    /// </summary>
    internal abstract class AbstractBlockStructureProvider : BlockStructureProvider
    {
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;

        protected AbstractBlockStructureProvider(
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> defaultNodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> defaultTriviaOutlinerMap)
        {
            _nodeProviderMap = defaultNodeOutlinerMap;
            _triviaProviderMap = defaultTriviaOutlinerMap;
        }

        /// <summary>
        /// Keep in sync with <see cref="ProvideBlockStructureAsync"/>
        /// </summary>
        public override void ProvideBlockStructure(BlockStructureContext context)
        {
            try
            {
                var syntaxRoot = context.Document.GetSyntaxRootSynchronously(context.CancellationToken);

                ProvideBlockStructureWorker(context, syntaxRoot);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Keep in sync with <see cref="ProvideBlockStructure"/>
        /// </summary>
        public override async Task ProvideBlockStructureAsync(BlockStructureContext context)
        {
            try
            {
                var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

                ProvideBlockStructureWorker(context, syntaxRoot);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void ProvideBlockStructureWorker(
            BlockStructureContext context, SyntaxNode syntaxRoot)
        {
            var spans = ArrayBuilder<BlockSpan>.GetInstance();
            BlockSpanCollector.CollectBlockSpans(
                context.Document, syntaxRoot, _nodeProviderMap, _triviaProviderMap, spans, context.CancellationToken);

            UpdateAndAddSpans(context, spans);

            spans.Free();
        }

        internal static void UpdateAndAddSpans(BlockStructureContext context, ArrayBuilder<BlockSpan> spans)
        {
            var options = context.Document.Project.Solution.Workspace.Options;
            var language = context.Document.Project.Language;

            var showIndentGuidesForCodeLevelConstructs = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, language);
            var showIndentGuidesForDeclarationLevelConstructs = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, language);
            var showIndentGuidesForCommentsAndPreprocessorRegions = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, language);
            var showOutliningForCodeLevelConstructs = options.GetOption(BlockStructureOptions.ShowOutliningForCodeLevelConstructs, language);
            var showOutliningForDeclarationLevelConstructs = options.GetOption(BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, language);
            var showOutliningForCommentsAndPreprocessorRegions = options.GetOption(BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, language);

            foreach (var span in spans)
            {
                var updatedSpan = UpdateBlockSpan(span,
                        showIndentGuidesForCodeLevelConstructs,
                        showIndentGuidesForDeclarationLevelConstructs,
                        showIndentGuidesForCommentsAndPreprocessorRegions,
                        showOutliningForCodeLevelConstructs,
                        showOutliningForDeclarationLevelConstructs,
                        showOutliningForCommentsAndPreprocessorRegions);
                context.AddBlockSpan(updatedSpan);
            }
        }

        internal static BlockSpan UpdateBlockSpan(BlockSpan blockSpan,
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

            if (!showIndentGuidesForDeclarationLevelConstructs && isTopLevel)
            {
                type = BlockTypes.Nonstructural;
            }
            else if (!showIndentGuidesForCodeLevelConstructs && isMemberLevel)
            {
                type = BlockTypes.Nonstructural;
            }
            else if (!showIndentGuidesForCommentsAndPreprocessorRegions && isComment)
            {
                type = BlockTypes.Nonstructural;
            }

            var isCollapsible = blockSpan.IsCollapsible;
            if (isCollapsible)
            {
                if (!showOutliningForDeclarationLevelConstructs && isTopLevel)
                {
                    isCollapsible = false;
                }
                else if (!showOutliningForCodeLevelConstructs && isMemberLevel)
                {
                    isCollapsible = false;
                }
                else if (!showOutliningForCommentsAndPreprocessorRegions && isComment)
                {
                    isCollapsible = false;
                }
            }

            return blockSpan.With(type: type, isCollapsible: isCollapsible);
        }
    }
}
