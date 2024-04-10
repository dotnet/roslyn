// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractElseSnippetProvider<TElseClauseSyntax> : AbstractStatementSnippetProvider<TElseClauseSyntax>
    where TElseClauseSyntax : SyntaxNode
{
    protected sealed override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TElseClauseSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];
}
