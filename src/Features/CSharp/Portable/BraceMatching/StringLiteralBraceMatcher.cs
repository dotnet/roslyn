// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceMatching;

[ExportBraceMatcher(LanguageNames.CSharp), Shared]
internal sealed class StringLiteralBraceMatcher : IBraceMatcher
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public StringLiteralBraceMatcher()
    {
    }

    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        if (!token.ContainsDiagnostics)
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                return GetSimpleStringBraceMatchingResult(token, endTokenLength: 1);
            }
            else if (token.IsKind(SyntaxKind.Utf8StringLiteralToken))
            {
                return GetSimpleStringBraceMatchingResult(token, endTokenLength: 3);
            }
            else if (token.Kind() is SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedVerbatimStringStartToken)
            {
                if (token.Parent is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    return new BraceMatchingResult(token.Span, interpolatedString.StringEndToken.Span);
                }
            }
            else if (token.IsKind(SyntaxKind.InterpolatedStringEndToken))
            {
                if (token.Parent is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    return new BraceMatchingResult(interpolatedString.StringStartToken.Span, token.Span);
                }
            }
        }

        return null;
    }

    private static BraceMatchingResult GetSimpleStringBraceMatchingResult(SyntaxToken token, int endTokenLength)
    {
        if (token.IsVerbatimStringLiteral())
        {
            return new BraceMatchingResult(
                new TextSpan(token.SpanStart, 2),
                new TextSpan(token.Span.End - endTokenLength, endTokenLength));
        }
        else
        {
            return new BraceMatchingResult(
                new TextSpan(token.SpanStart, 1),
                new TextSpan(token.Span.End - endTokenLength, endTokenLength));
        }
    }
}
