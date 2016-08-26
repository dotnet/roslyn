// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractBlockStructureProvider : BlockStructureProvider
    {
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeOutlinerMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaOutlinerMap;

        protected AbstractBlockStructureProvider(
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> defaultNodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> defaultTriviaOutlinerMap)
        {
            _nodeOutlinerMap = defaultNodeOutlinerMap;
            _triviaOutlinerMap = defaultTriviaOutlinerMap;
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

        private void ProvideBlockStructureWorker(
            BlockStructureContext context, SyntaxNode syntaxRoot)
        {
            var spans = ImmutableArray.CreateBuilder<BlockSpan>();
            BlockSpanCollector.CollectBlockSpans(
                context.Document, syntaxRoot, _nodeOutlinerMap, _triviaOutlinerMap, spans, context.CancellationToken);

            foreach (var region in spans)
            {
                context.AddBlockSpan(region);
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
    }
}