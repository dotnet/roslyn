// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal class CSharpStructuredTriviaFormatEngine : AbstractFormatEngine
{
    public static IFormattingResult Format(
        SyntaxTrivia trivia,
        int initialColumn,
        SyntaxFormattingOptions options,
        ChainedFormattingRules formattingRules,
        CancellationToken cancellationToken)
    {
        var root = trivia.GetStructure() ?? throw new ArgumentException();
        var formatter = new CSharpStructuredTriviaFormatEngine(trivia, initialColumn, options, formattingRules, root.GetFirstToken(includeZeroWidth: true), root.GetLastToken(includeZeroWidth: true));
        return formatter.Format(cancellationToken);
    }

    private CSharpStructuredTriviaFormatEngine(
        SyntaxTrivia trivia,
        int initialColumn,
        SyntaxFormattingOptions options,
        ChainedFormattingRules formattingRules,
        SyntaxToken startToken,
        SyntaxToken endToken)
        : base(TreeData.Create(trivia, initialColumn),
               options,
               formattingRules,
               startToken,
               endToken)
    {
    }

    internal override IHeaderFacts HeaderFacts => CSharpHeaderFacts.Instance;

    protected override AbstractTriviaDataFactory CreateTriviaFactory()
        => new TriviaDataFactory(this.TreeData, this.Options);

    protected override FormattingContext CreateFormattingContext(TokenStream tokenStream, CancellationToken cancellationToken)
        => new(this, tokenStream);

    protected override NodeOperations CreateNodeOperations(CancellationToken cancellationToken)
    {
        // ignore all node operations for structured trivia since it is not possible for this to have any impact currently.
        return NodeOperations.Empty;
    }

    protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        => new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat);
}
