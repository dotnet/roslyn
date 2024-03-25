// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractElseSnippetProvider : AbstractStatementSnippetProvider
{
    public override string Identifier => "else";

    public override string Description => FeaturesResources.else_statement;

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsElseClause;

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];
}
