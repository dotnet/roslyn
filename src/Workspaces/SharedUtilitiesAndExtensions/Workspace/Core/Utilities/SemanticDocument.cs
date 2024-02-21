// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed class SemanticDocument(Document document, SourceText text, SyntaxNode root, SemanticModel semanticModel)
    : SyntacticDocument(document, text, root)
{
    public readonly SemanticModel SemanticModel = semanticModel;

    public static new async Task<SemanticDocument> CreateAsync(Document document, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return new SemanticDocument(document, text, root, model);
    }

    public new async ValueTask<SemanticDocument> WithSyntaxRootAsync(SyntaxNode root, CancellationToken cancellationToken)
    {
        var newDocument = this.Document.WithSyntaxRoot(root);
        return await CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
    }
}
