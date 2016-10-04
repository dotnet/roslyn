// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
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
                var cancellationToken = context.CancellationToken;
                var syntaxRoot = context.Document.GetSyntaxRootSynchronously(cancellationToken);
                var options = context.Document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                ProvideBlockStructureWorker(context, syntaxRoot, options);
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
                var options = await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

                ProvideBlockStructureWorker(context, syntaxRoot, options);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void ProvideBlockStructureWorker(
            BlockStructureContext context, SyntaxNode syntaxRoot, DocumentOptionSet options)
        {
            var spans = ArrayBuilder<BlockSpan>.GetInstance();
            BlockSpanCollector.CollectBlockSpans(
                context.Document, syntaxRoot, _nodeProviderMap, _triviaProviderMap, spans, context.CancellationToken);

            var showIndentGuidesForCodeLevelConstructs = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs);
            var showIndentGuidesForDeclarationLevelConstructs = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs);
            var showIndentGuidesForCommentsAndPreprocessorRegions = options.GetOption(BlockStructureOptions.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions);
            var showOutliningForCodeLevelConstructs = options.GetOption(BlockStructureOptions.ShowOutliningForCodeLevelConstructs);
            var showOutliningForDeclarationLevelConstructs = options.GetOption(BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs);
            var showOutliningForCommentsAndPreprocessorRegions = options.GetOption(BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions);

            foreach (var span in spans)
            {
                if (span != null)
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

            spans.Free();
        }

        private BlockSpan UpdateBlockSpan(BlockSpan blockSpan,
            bool showIndentGuidesForCodeLevelConstructs,
            bool showIndentGuidesForDeclarationLevelConstructs,
            bool showIndentGuidesForCommentsAndPreprocessorRegions,
            bool showOutliningForCodeLevelConstructs,
            bool showOutliningForDeclarationLevelConstructs,
            bool showOutliningForCommentsAndPreprocessorRegions)
        {
            var type = blockSpan.Type;

            var isTopLevel = IsDeclarationLevelConstruct(type);
            var isMemberLevel = IsCodeLevelConstruct(type);
            var isComment = IsCommentOrPreprocessorRegion(type);

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

        private static bool IsCommentOrPreprocessorRegion(string type)
        {
            return type == BlockTypes.Comment || type == BlockTypes.PreprocessorRegion;
        }

        protected bool IsCodeLevelConstruct(string type)
        {
            switch (type)
            {
                case BlockTypes.Case:
                case BlockTypes.Conditional:
                case BlockTypes.LocalFunction:
                case BlockTypes.Lock:
                case BlockTypes.Loop:
                case BlockTypes.TryCatchFinally:
                case BlockTypes.Using:
                case BlockTypes.Standalone:
                case BlockTypes.Switch:
                case BlockTypes.AnonymousMethod:
                case BlockTypes.Xml:
                    return true;
            }

            return false;
        }

        protected bool IsDeclarationLevelConstruct(string type)
        {
            return !IsCodeLevelConstruct(type) && !IsCommentOrPreprocessorRegion(type);
        }
    }
}