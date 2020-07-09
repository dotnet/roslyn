// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineParameterNameHints
{
    internal abstract class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        public async Task<IEnumerable<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes(textSpan);
            var spans = AddAllParameterNameHintLocations(semanticModel, nodes, cancellationToken);
            return spans;
        }

        protected abstract IEnumerable<InlineParameterHint> AddAllParameterNameHintLocations(
            SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes, CancellationToken cancellationToken);
    }
}
