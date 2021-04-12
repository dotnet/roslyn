﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class StringLiteralBraceMatcher : IBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StringLiteralBraceMatcher()
        {
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (!token.ContainsDiagnostics)
            {
                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    if (token.IsVerbatimStringLiteral())
                    {
                        return new BraceMatchingResult(
                            new TextSpan(token.SpanStart, 2),
                            new TextSpan(token.Span.End - 1, 1));
                    }
                    else
                    {
                        return new BraceMatchingResult(
                            new TextSpan(token.SpanStart, 1),
                            new TextSpan(token.Span.End - 1, 1));
                    }
                }
                else if (token.IsKind(SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedVerbatimStringStartToken))
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
    }
}
