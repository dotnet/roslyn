// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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

            foreach (var span in spans)
            {
                context.AddBlockSpan(span);
            }

            spans.Free();
        }
    }
}
