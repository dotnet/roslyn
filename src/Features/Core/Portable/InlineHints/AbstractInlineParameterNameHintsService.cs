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

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        public async Task<ImmutableArray<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes(textSpan);

            using var _ = ArrayBuilder<InlineParameterHint>.GetInstance(out var result);
            AddAllParameterNameHintLocations(semanticModel, nodes, result, cancellationToken);
            return result.ToImmutable();
        }

        protected abstract void AddAllParameterNameHintLocations(
            SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes, ArrayBuilder<InlineParameterHint> result, CancellationToken cancellationToken);
    }
}
