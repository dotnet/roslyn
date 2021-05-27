// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Collections;
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

        public override void ProvideBlockStructure(BlockStructureContext context)
        {
            try
            {
                var syntaxRoot = context.SyntaxTree.GetRoot(context.CancellationToken);
                using var spans = TemporaryArray<BlockSpan>.Empty;
                BlockSpanCollector.CollectBlockSpans(
                    syntaxRoot, context.OptionProvider, _nodeProviderMap, _triviaProviderMap, ref spans.AsRef(), context.CancellationToken);

                foreach (var span in spans)
                    context.AddBlockSpan(span);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
