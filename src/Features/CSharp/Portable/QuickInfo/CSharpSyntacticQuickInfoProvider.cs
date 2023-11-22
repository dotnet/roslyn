// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.Syntactic, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = QuickInfoProviderNames.Semantic)]
    internal class CSharpSyntacticQuickInfoProvider : CommonQuickInfoProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpSyntacticQuickInfoProvider()
        {
        }

        protected override Task<QuickInfoItem?> BuildQuickInfoAsync(
            QuickInfoContext context,
            SyntaxToken token)
            => Task.FromResult(BuildQuickInfo(token, context.CancellationToken));

        protected override Task<QuickInfoItem?> BuildQuickInfoAsync(
            CommonQuickInfoContext context,
            SyntaxToken token)
            => Task.FromResult(BuildQuickInfo(token, context.CancellationToken));

        private static QuickInfoItem? BuildQuickInfo(SyntaxToken token, CancellationToken cancellationToken)
        {
            switch (token.Kind())
            {
                case SyntaxKind.CloseBraceToken:
                    return BuildQuickInfoCloseBrace(token);
                case SyntaxKind.HashToken:
                case SyntaxKind.EndRegionKeyword:
                case SyntaxKind.EndIfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.ElifKeyword:
                case SyntaxKind.EndOfDirectiveToken:
                    return BuildQuickInfoDirectives(token, cancellationToken);
                default:
                    return null;
            }
        }

        private static QuickInfoItem? BuildQuickInfoCloseBrace(SyntaxToken token)
        {
            // Don't show for interpolations
            if (token.Parent is InterpolationSyntax interpolation &&
                interpolation.CloseBraceToken == token)
            {
                return null;
            }

            // Now check if we can find an open brace.
            var parent = token.Parent!;
            var openBrace = parent.ChildNodesAndTokens().FirstOrDefault(n => n.Kind() == SyntaxKind.OpenBraceToken).AsToken();
            if (openBrace.Kind() != SyntaxKind.OpenBraceToken)
            {
                return null;
            }

            var spanStart = parent.SpanStart;
            var spanEnd = openBrace.Span.End;

            // If the parent is a scope block, check and include nearby comments around the open brace
            // LeadingTrivia is preferred
            if (IsScopeBlock(parent))
            {
                MarkInterestedSpanNearbyScopeBlock(parent, openBrace, ref spanStart, ref spanEnd);
            }
            // If the parent is a child of a property/method declaration, object/array creation, or control flow node,
            // then walk up one higher so we can show more useful context
            else if (parent.GetFirstToken() == openBrace)
            {
                // parent.Parent must be non-null, because for GetFirstToken() to have returned something it would have had to walk up to its parent
                spanStart = parent.Parent!.SpanStart;
            }

            // encode document spans that correspond to the text to show
            var spans = ImmutableArray.Create(TextSpan.FromBounds(spanStart, spanEnd));
            return QuickInfoItem.Create(token.Span, relatedSpans: spans);
        }

        private static bool IsScopeBlock(SyntaxNode node)
            => node.IsKind(SyntaxKind.Block)
                && node.Parent?.Kind() is SyntaxKind.Block or SyntaxKind.SwitchSection or SyntaxKind.GlobalStatement;

        private static void MarkInterestedSpanNearbyScopeBlock(SyntaxNode block, SyntaxToken openBrace, ref int spanStart, ref int spanEnd)
        {
            var searchListAbove = openBrace.LeadingTrivia.Reverse();
            if (TryFindFurthestNearbyComment(ref searchListAbove, out var nearbyComment))
            {
                spanStart = nearbyComment.SpanStart;
                return;
            }

            var nextToken = block.FindToken(openBrace.FullSpan.End);
            var searchListBelow = nextToken.LeadingTrivia;
            if (TryFindFurthestNearbyComment(ref searchListBelow, out nearbyComment))
            {
                spanEnd = nearbyComment.Span.End;
                return;
            }
        }

        private static bool TryFindFurthestNearbyComment<T>(ref T triviaSearchList, out SyntaxTrivia nearbyTrivia)
            where T : IEnumerable<SyntaxTrivia>
        {
            nearbyTrivia = default;

            foreach (var trivia in triviaSearchList)
            {
                if (trivia.IsSingleOrMultiLineComment())
                {
                    nearbyTrivia = trivia;
                }
                else if (trivia.Kind() is not SyntaxKind.WhitespaceTrivia and not SyntaxKind.EndOfLineTrivia)
                {
                    break;
                }
            }

            return nearbyTrivia.IsSingleOrMultiLineComment();
        }

        private static QuickInfoItem? BuildQuickInfoDirectives(SyntaxToken token, CancellationToken cancellationToken)
        {
            if (token.Parent is DirectiveTriviaSyntax directiveTrivia)
            {
                if (directiveTrivia is EndRegionDirectiveTriviaSyntax)
                {
                    var regionStart = directiveTrivia.GetMatchingDirective(cancellationToken);
                    if (regionStart is not null)
                        return QuickInfoItem.Create(token.Span, relatedSpans: ImmutableArray.Create(regionStart.Span));
                }
                else if (directiveTrivia is ElifDirectiveTriviaSyntax or ElseDirectiveTriviaSyntax or EndIfDirectiveTriviaSyntax)
                {
                    var matchingDirectives = directiveTrivia.GetMatchingConditionalDirectives(cancellationToken);
                    var matchesBefore = matchingDirectives
                        .TakeWhile(d => d.SpanStart < directiveTrivia.SpanStart)
                        .Select(d => d.Span)
                        .ToImmutableArray();
                    if (matchesBefore.Length > 0)
                        return QuickInfoItem.Create(token.Span, relatedSpans: matchesBefore);
                }
            }

            return null;
        }
    }
}
