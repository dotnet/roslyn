// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class CSharpFormatEngine : AbstractFormatEngine
{
    public CSharpFormatEngine(
        SyntaxNode node,
        SyntaxFormattingOptions options,
        ImmutableArray<AbstractFormattingRule> formattingRules,
        SyntaxToken startToken,
        SyntaxToken endToken)
        : base(TreeData.Create(node),
             options,
             formattingRules,
             startToken,
             endToken)
    {
    }

    internal override IHeaderFacts HeaderFacts => CSharpHeaderFacts.Instance;

    protected override AbstractTriviaDataFactory CreateTriviaFactory()
        => new TriviaDataFactory(this.TreeData, this.Options.LineFormatting);

    protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        => new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat);
}
