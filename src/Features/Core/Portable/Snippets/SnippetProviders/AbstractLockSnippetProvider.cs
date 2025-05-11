// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractLockSnippetProvider<TLockStatementSyntax> : AbstractStatementSnippetProvider<TLockStatementSyntax>
    where TLockStatementSyntax : SyntaxNode
{
    protected sealed override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var statement = generator.LockStatement(generator.ThisExpression(), statements: []);
        return Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), statement.NormalizeWhitespace().ToFullString()));
    }
}
