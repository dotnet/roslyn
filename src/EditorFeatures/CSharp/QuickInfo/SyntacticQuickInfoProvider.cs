﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Syntactic, LanguageNames.CSharp)]
    internal class SyntacticQuickInfoProvider : AbstractQuickInfoProvider
    {
        [ImportingConstructor]
        public SyntacticQuickInfoProvider(
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IGlyphService glyphService,
            ClassificationTypeMap typeMap)
            : base(projectionBufferFactoryService, editorOptionsFactoryService,
                   textEditorFactoryService, glyphService, typeMap)
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
            if (IsScopeBlock(parent))
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
            return this.CreateProjectionBufferDeferredContent(span);
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
            nearbyTrivia = default(SyntaxTrivia);

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
