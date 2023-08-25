// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal class SemanticDocument : SyntacticDocument
    {
        public readonly SemanticModel SemanticModel;

        private SemanticDocument(Document document, SourceText text, SyntaxNode root, SemanticModel semanticModel)
            : base(document, text, root)
        {
            this.SemanticModel = semanticModel;
        }

        public static new async Task<SemanticDocument> CreateAsync(Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return new SemanticDocument(document, text, root, model);
        }
    }
}
