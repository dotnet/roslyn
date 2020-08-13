// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var pragmaWarningDiagnosticId = token.Parent switch
            {
                PragmaWarningDirectiveTriviaSyntax pragmaWarning => pragmaWarning.ErrorCodes.FirstOrDefault() as IdentifierNameSyntax,
                IdentifierNameSyntax identifier when identifier.Parent is PragmaWarningDirectiveTriviaSyntax => identifier,
                _ => null,
            };
            if (pragmaWarningDiagnosticId != null)
            {
                var pragma = (pragmaWarningDiagnosticId.Parent as PragmaWarningDirectiveTriviaSyntax)!;
                var removedId = new SeparatedSyntaxList<ExpressionSyntax>().AddRange(pragma.ErrorCodes.Where(e => e != pragmaWarningDiagnosticId));
                var newPragma = removedId.Count == 0
                    ? null
                    : pragma.WithErrorCodes(removedId);
                var root = await pragma.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = newPragma == null
                    ? root.RemoveNode(pragma, SyntaxRemoveOptions.KeepUnbalancedDirectives)
                    : root.ReplaceNode(pragma, newPragma);
                if (newRoot != null)
                {
                    var newDocument = document.WithSyntaxRoot(newRoot);
                    var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    if (semanticModel != null)
                    {
                        var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
                        var findDiagnostic = diagnostics.FirstOrDefault(d => d.Id == pragmaWarningDiagnosticId.Identifier.ValueText);
                        if (findDiagnostic != null)
                        {
                            return QuickInfoItem.Create(pragma.Span, sections: new[]
                                {
                                    QuickInfoSection.Create(QuickInfoSectionKinds.Description, new[]
                                    {
                                        new TaggedText(TextTags.Text, findDiagnostic.ToString())
                                    }.ToImmutableArray())
                                }.ToImmutableArray(), relatedSpans: new[] { findDiagnostic.Location.SourceSpan }.ToImmutableArray());
                        }
                    }
                }
            }
            if (token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return null;
            }

            // Don't show for interpolations
            if (token.Parent.IsKind(SyntaxKind.Interpolation, out InterpolationSyntax? interpolation) &&
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
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var spans = ImmutableArray.Create(TextSpan.FromBounds(spanStart, spanEnd));
            return QuickInfoItem.Create(token.Span, relatedSpans: spans);
        }

        private static bool IsScopeBlock(SyntaxNode node)
        {
            var parent = node.Parent;
            return node.IsKind(SyntaxKind.Block)
                && (parent.IsKind(SyntaxKind.Block)
                    || parent.IsKind(SyntaxKind.SwitchSection)
                    || parent.IsKind(SyntaxKind.GlobalStatement));
        }

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
                else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    break;
                }
            }

            return nearbyTrivia.IsSingleOrMultiLineComment();
        }
    }
}
