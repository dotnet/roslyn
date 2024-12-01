// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractPropertySnippetProvider<TPropertyDeclarationSyntax> : AbstractSingleChangeSnippetProvider<TPropertyDeclarationSyntax>
    where TPropertyDeclarationSyntax : SyntaxNode
{
    /// <summary>
    /// Generates the property syntax.
    /// Requires language specificity for the TypeSyntax as well as the
    /// type of the PropertySyntax.
    /// </summary>
    protected abstract Task<TPropertyDeclarationSyntax> GenerateSnippetSyntaxAsync(Document document, int position, CancellationToken cancellationToken);

    protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var propertyDeclaration = await GenerateSnippetSyntaxAsync(document, position, cancellationToken).ConfigureAwait(false);
        return new TextChange(TextSpan.FromBounds(position, position), propertyDeclaration.NormalizeWhitespace().ToFullString());
    }
}
