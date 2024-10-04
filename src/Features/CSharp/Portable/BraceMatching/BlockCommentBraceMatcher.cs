// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceMatching;

[ExportBraceMatcher(LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BlockCommentBraceMatcher() : IBraceMatcher
{
    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position, findInsideTrivia: false);
        if (token == default)
            return null;

        var span = token.Span;
        if (span.Contains(position))
            return null;

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        if (position < token.SpanStart)
            return FindBraces(token.LeadingTrivia);
        else if (position >= token.Span.End)
            return FindBraces(token.TrailingTrivia);

        return null;

        BraceMatchingResult? FindBraces(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.FullSpan.Contains(position))
                {
                    if (trivia.Kind() is SyntaxKind.MultiLineCommentTrivia &&
                        trivia.ToString() is ['/', '*', .., '*', '/'])
                    {
                        return new BraceMatchingResult(new TextSpan(trivia.SpanStart, "/*".Length), TextSpan.FromBounds(trivia.Span.End - "*/".Length, trivia.Span.End));
                    }
                    else if (trivia.Kind() is SyntaxKind.MultiLineDocumentationCommentTrivia)
                    {
                        var startBrace = new TextSpan(trivia.FullSpan.Start, "/**".Length);
                        var endBrace = TextSpan.FromBounds(trivia.FullSpan.End - "*/".Length, trivia.FullSpan.End);
                        if (text.ToString(startBrace) == "/**" && text.ToString(endBrace) == "*/")
                            return new BraceMatchingResult(startBrace, endBrace);
                    }
                }
            }

            return null;
        }
    }
}
