// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractForEachLoopSnippetProvider<TStatementSyntax> : AbstractInlineStatementSnippetProvider<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
{
    protected override bool IsValidAccessingType(ITypeSymbol type, SnippetContext context, CancellationToken cancellationToken)
        => context.SemanticModel.GetIterationType(context.Position, type, cancellationToken) is not null;
}
