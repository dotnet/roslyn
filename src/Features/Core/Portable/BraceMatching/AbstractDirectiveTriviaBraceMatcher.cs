// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.BraceMatching;

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
    protected abstract ImmutableArray<TDirectiveTriviaSyntax> GetMatchingConditionalDirectives(TDirectiveTriviaSyntax directive, CancellationToken cancellationToken);
    protected abstract TDirectiveTriviaSyntax? GetMatchingDirective(TDirectiveTriviaSyntax directive, CancellationToken cancellationToken);
    internal abstract TextSpan GetSpanForTagging(TDirectiveTriviaSyntax directive);

    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position, findInsideTrivia: true);

        if (token.Parent is not TDirectiveTriviaSyntax directive)
        {
            return null;
        }

        TDirectiveTriviaSyntax? matchingDirective = null;
        if (directive
                is TIfDirectiveTriviaSyntax
                or TElseIfDirectiveTriviaSyntax
                or TElseDirectiveTriviaSyntax
                or TEndIfDirectiveTriviaSyntax)
        {
            // #if/#elif/#else/#endif directive cases.
            var matchingDirectives = GetMatchingConditionalDirectives(directive, cancellationToken);
            if (matchingDirectives.Length > 0)
                matchingDirective = matchingDirectives[(matchingDirectives.IndexOf(directive) + 1) % matchingDirectives.Length];
        }
        else if (directive
                is TRegionDirectiveTriviaSyntax
                or TEndRegionDirectiveTriviaSyntax)
        {
            matchingDirective = GetMatchingDirective(directive, cancellationToken);
        }

        if (matchingDirective == null)
        {
            // one line directives, that do not have a matching begin/end directive pair.
            return null;
        }

        return new BraceMatchingResult(
            LeftSpan: GetSpanForTagging(directive),
            RightSpan: GetSpanForTagging(matchingDirective));
    }
}
