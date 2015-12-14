// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class AbstractDirectiveTriviaBraceMatcher<TDirectiveTriviaSyntax,
        TIfDirectiveTriviaSyntax, TElseIfDirectiveTriviaSyntax,
        TElseDirectiveTriviaSyntax, TEndIfDirectiveTriviaSyntax,
        TRegionDirectiveTriviaSyntax, TEndRegionDirectiveTriviaSyntax> : IBraceMatcher
            where TDirectiveTriviaSyntax : SyntaxNode
            where TIfDirectiveTriviaSyntax : TDirectiveTriviaSyntax
            where TElseIfDirectiveTriviaSyntax : TDirectiveTriviaSyntax
            where TElseDirectiveTriviaSyntax : TDirectiveTriviaSyntax
            where TEndIfDirectiveTriviaSyntax : TDirectiveTriviaSyntax
            where TRegionDirectiveTriviaSyntax : TDirectiveTriviaSyntax
            where TEndRegionDirectiveTriviaSyntax : TDirectiveTriviaSyntax
    {
        internal abstract List<TDirectiveTriviaSyntax> GetMatchingConditionalDirectives(TDirectiveTriviaSyntax directive, CancellationToken cancellationToken);
        internal abstract TDirectiveTriviaSyntax GetMatchingDirective(TDirectiveTriviaSyntax directive, CancellationToken cancellationToken);
        internal abstract TextSpan GetSpanForTagging(TDirectiveTriviaSyntax directive);

        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position, findInsideTrivia: true);

            var directive = token.Parent as TDirectiveTriviaSyntax;
            if (directive == null)
            {
                return null;
            }

            TDirectiveTriviaSyntax matchingDirective;
            if (IsConditionalDirective(directive))
            {
                // #if/#elif/#else/#endif directive cases.
                var matchingDirectives = GetMatchingConditionalDirectives(directive, cancellationToken);
                matchingDirective = matchingDirectives[(matchingDirectives.IndexOf(directive) + 1) % matchingDirectives.Count];
            }
            else
            {
                // #region/#endregion or other directive cases.
                matchingDirective = GetMatchingDirective(directive, cancellationToken);
            }

            if (matchingDirective == null)
            {
                // one line directives, that do not have a matching begin/end directive pair.
                return null;
            }

            return new BraceMatchingResult(
                leftSpan: GetSpanForTagging(directive),
                rightSpan: GetSpanForTagging(matchingDirective));
        }


        private bool IsConditionalDirective(TDirectiveTriviaSyntax directive)
        {
            return directive is TIfDirectiveTriviaSyntax ||
                   directive is TElseIfDirectiveTriviaSyntax ||
                   directive is TElseDirectiveTriviaSyntax ||
                   directive is TEndIfDirectiveTriviaSyntax;
        }
    }
}