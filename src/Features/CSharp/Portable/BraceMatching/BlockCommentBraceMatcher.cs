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

namespace Microsoft.CodeAnalysis.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp), Shared]
    internal class BlockCommentBraceMatcher : IBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BlockCommentBraceMatcher()
        {
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var trivia = root.FindTrivia(position);

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

            return null;
        }
    }
}
