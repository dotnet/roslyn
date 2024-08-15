// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractStatementSnippetProvider<TStatementSyntax> : AbstractSingleChangeSnippetProvider<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
{
    protected override bool IsValidSnippetLocation(SnippetContext context, CancellationToken cancellationToken)
        => context.SyntaxContext.IsStatementContext || context.SyntaxContext.IsGlobalStatementContext;
}
