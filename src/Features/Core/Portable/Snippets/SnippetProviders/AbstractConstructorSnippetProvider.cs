// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractConstructorSnippetProvider : AbstractSingleChangeSnippetProvider
{
    public override string Identifier => "ctor";

    public override string Description => FeaturesResources.constructor;

    public override ImmutableArray<string> AdditionalFilterTexts { get; } = ["constructor"];

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsConstructorDeclaration;

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];
}
