// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class DirectiveTriviaMatcher : IBraceMatcher
    {
        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position, findInsideTrivia: true);

            var directive = token.Parent as DirectiveTriviaSyntax;
            if (directive == null)
            {
                return null;
            }

            DirectiveTriviaSyntax matchingDirective = null;
            if (IsConditionalDirective(directive))
            {
                // #If/#elif/#else/#endIf directive cases.
                var matchingDirectives = directive.GetMatchingConditionalDirectives(cancellationToken).ToList();
                matchingDirective = matchingDirectives[(matchingDirectives.IndexOf(directive) + 1) % matchingDirectives.Count];
            }
            else
            {
                // #region/#endregion or other directive cases.
                matchingDirective = directive.GetMatchingDirective(cancellationToken);
            }

            if (matchingDirective == null)
            {
                // one line directives, that do not have a matching begin/end directive pair.
                return null;
            }

            return new BraceMatchingResult(
                TextSpan.FromBounds(
                    directive.HashToken.SpanStart,
                    directive.DirectiveNameToken.Span.End),
                TextSpan.FromBounds(
                    matchingDirective.HashToken.SpanStart,
                    matchingDirective.DirectiveNameToken.Span.End));
        }

        private bool IsConditionalDirective(DirectiveTriviaSyntax directive)
        {
            return directive.IsKind(SyntaxKind.IfDirectiveTrivia) ||
                directive.IsKind(SyntaxKind.ElifDirectiveTrivia) ||
                directive.IsKind(SyntaxKind.ElseDirectiveTrivia) ||
                directive.IsKind(SyntaxKind.EndIfDirectiveTrivia);
        }
    }
}
