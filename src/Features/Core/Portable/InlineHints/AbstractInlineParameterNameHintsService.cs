// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        public async Task<ImmutableArray<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var forParameters = options.GetOption(InlineHintsOptions.EnabledForParameters);
            if (!forParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var literalParameters = options.GetOption(InlineHintsOptions.ForLiteralParameters);
            var objectCreationParameters = options.GetOption(InlineHintsOptions.ForObjectCreationParameters);
            var otherParameters = options.GetOption(InlineHintsOptions.ForOtherParameters);
            if (!literalParameters && !objectCreationParameters && !otherParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes(textSpan);

            using var _1 = ArrayBuilder<InlineParameterHint>.GetInstance(out var buffer);
            AddAllParameterNameHintLocations(semanticModel, nodes, buffer, cancellationToken);

            using var _2 = ArrayBuilder<InlineParameterHint>.GetInstance(out var result);

            foreach (var hint in buffer)
            {
                if (HintMatches(hint, literalParameters, objectCreationParameters, otherParameters))
                    result.Add(hint);
            }

            return result.ToImmutable();
        }

        private static bool HintMatches(InlineParameterHint hint, bool literalParameters, bool objectCreationParameters, bool otherParameters)
            => hint.Kind switch
            {
                InlineParameterHintKind.Literal => literalParameters,
                InlineParameterHintKind.ObjectCreation => objectCreationParameters,
                InlineParameterHintKind.Other => otherParameters,
                _ => throw ExceptionUtilities.UnexpectedValue(hint.Kind),
            };

        protected abstract void AddAllParameterNameHintLocations(
            SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes, ArrayBuilder<InlineParameterHint> result, CancellationToken cancellationToken);
    }
}
