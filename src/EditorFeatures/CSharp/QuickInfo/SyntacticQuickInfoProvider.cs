// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Syntactic, LanguageNames.CSharp)]
    internal class SyntacticQuickInfoProvider : AbstractQuickInfoProvider
    {
        [ImportingConstructor]
        public SyntacticQuickInfoProvider(
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IGlyphService glyphService,
            ClassificationTypeMap typeMap)
            : base(textBufferFactoryService, contentTypeRegistryService, projectionBufferFactoryService,
                   editorOptionsFactoryService, textEditorFactoryService, glyphService, typeMap)
        {
        }

        protected override async Task<IDeferredQuickInfoContent> BuildContentAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            if (token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return null;
            }

            // Don't show for interpolations
            if (token.Parent.IsKind(SyntaxKind.Interpolation) &&
                ((InterpolationSyntax)token.Parent).CloseBraceToken == token)
            {
                return null;
            }

            // Now check if we can find an open brace. 
            var parent = token.Parent;
            var openBrace = parent.ChildNodesAndTokens().FirstOrDefault(n => n.Kind() == SyntaxKind.OpenBraceToken).AsToken();
            if (openBrace.Kind() != SyntaxKind.OpenBraceToken)
            {
                return null;
            }

            var spanStart = parent.SpanStart;
            var spanEnd = openBrace.Span.End;

            // If the parent is a scope block, check and include nearby comments around the open brace
            // LeadingTrivia is preferred
            if (parent.IsKind(SyntaxKind.Block) && parent.Parent.IsKind(SyntaxKind.Block))
            {
                MarkInterestedSpanNearbyScopeBlock(parent, openBrace, ref spanStart, ref spanEnd);
            }
            // If the parent is a child of a property/method declaration, object/array creation, or control flow node..
            // then walk up one higher so we can show more useful context
            else if (parent.GetFirstToken() == openBrace)
            {
                spanStart = parent.Parent.SpanStart;
            }

            // Now that we know what we want to display, create a small elision buffer with that
            // span, jam it in a view and show that to the user.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                return null;
            }

            var span = new SnapshotSpan(textSnapshot, Span.FromBounds(spanStart, spanEnd));
            return this.CreateElisionBufferDeferredContent(span);
        }

        private static void MarkInterestedSpanNearbyScopeBlock(SyntaxNode block, SyntaxToken openBrace, ref int spanStart, ref int spanEnd)
        {
            SyntaxTrivia nearbyTrivia;

            if (openBrace.HasLeadingTrivia && FindFurthestNearbyComment(openBrace.LeadingTrivia.Reverse(), out nearbyTrivia))
            {
                spanStart = nearbyTrivia.SpanStart;
                return;
            }

            var nextToken = block.FindToken(openBrace.FullSpan.End);
            if (nextToken.HasLeadingTrivia && FindFurthestNearbyComment(nextToken.LeadingTrivia, out nearbyTrivia))
            {
                spanEnd = nearbyTrivia.Span.End;
                return;
            }
        }

        private static bool FindFurthestNearbyComment(IEnumerable<SyntaxTrivia> triviaSearchList, out SyntaxTrivia nearbyTrivia)
        {
            var searchList = triviaSearchList.SkipWhile(IsIndentation).ToArray();

            if (searchList.Length == 0)
            {
                nearbyTrivia = default(SyntaxTrivia);
                return false;
            }

            nearbyTrivia = searchList[0];
            if (nearbyTrivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                return true;
            }

            // In case of line comments that are potentially
            // stacked like this, crawl and find the furthest
            for ( var i = 0; i < searchList.Length && searchList[i].IsKind(SyntaxKind.SingleLineCommentTrivia); )
            {
                nearbyTrivia = searchList[i];
                i++;

                for ( int skipped = 0; i < searchList.Length && IsIndentation(searchList[i], skipped); )
                {
                    skipped++;
                    i++;
                }
            }

            return nearbyTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia);
        }

        private static bool IsIndentation(SyntaxTrivia trivia, int skipped)
        {
            // At most one empty line when there are no indentation
            // but in 99% times there should be indentation as we are just testing against scope blocks.
            return skipped < 2 && (trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        }
    }
}
