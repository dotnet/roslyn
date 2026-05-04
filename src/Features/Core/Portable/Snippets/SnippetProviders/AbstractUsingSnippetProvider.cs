// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractUsingSnippetProvider<TUsingStatementSyntax> : AbstractStatementSnippetProvider<TUsingStatementSyntax>
    where TUsingStatementSyntax : SyntaxNode
{
    protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var identifierName = NameGenerator.GenerateUniqueName("resource",
            n => semanticModel.LookupSymbols(position, name: n).IsEmpty);
        var statement = generator.UsingStatement(generator.IdentifierName(identifierName), statements: []);
        return new TextChange(TextSpan.FromBounds(position, position), statement.NormalizeWhitespace().ToFullString());
    }
}
