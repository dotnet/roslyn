using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineParameterNameHints
{
    internal class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        public async Task<IEnumerable<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var spans = new List<InlineParameterHint>();

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.DescendantNodes(textSpan);
            spans = AddAllParameterNameHintLocations(semanticModel, nodes, spans, cancellationToken);
            return spans;
        }

        protected virtual List<InlineParameterHint> AddAllParameterNameHintLocations(
            SemanticModel semanticModel, IEnumerable<SyntaxNode> nodes, List<InlineParameterHint> spans, CancellationToken cancellationToken)
        {
            return new List<InlineParameterHint>();
        }
    }
}
