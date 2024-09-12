// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

/// <summary>
/// Base class for "if" and "while" snippet providers
/// </summary>
internal abstract class AbstractConditionalBlockSnippetProvider : AbstractInlineStatementSnippetProvider
{
    protected abstract SyntaxNode GetCondition(SyntaxNode node);

    protected override bool IsValidAccessingType(ITypeSymbol type, Compilation compilation)
        => type.SpecialType == SpecialType.System_Boolean;

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        if (ConstructedFromInlineExpression)
            return [];

        var condition = GetCondition(node);
        var placeholder = new SnippetPlaceholder(condition.ToString(), condition.SpanStart);

        return [placeholder];
    }
}
