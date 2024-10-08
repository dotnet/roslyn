// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractForEachLoopSnippetProvider<TStatementSyntax> : AbstractInlineStatementSnippetProvider<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
{
    protected sealed override bool IsValidAccessingType(ITypeSymbol type, Compilation compilation)
        => type.CanBeEnumerated() || type.CanBeAsynchronouslyEnumerated(compilation);
}
