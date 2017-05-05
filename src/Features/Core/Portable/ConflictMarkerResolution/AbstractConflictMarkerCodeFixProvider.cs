// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConflictMarkerResolution
{
    internal abstract class AbstractResolveConflictMarkerCodeFixProvider : CodeFixProvider
    {
        protected AbstractResolveConflictMarkerCodeFixProvider(string diagnosticId)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
        }

        protected abstract bool IsNewLine(char ch);
        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);
        protected abstract bool IsDisabledText(SyntaxTrivia trivia);
        protected abstract bool IsConflictMarker(SyntaxTrivia trivia);

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        private static readonly int s_mergeConflictLength = "<<<<<<<".Length;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var startTrivia = root.FindTrivia(context.Span.Start);

            if (!IsConflictMarker(text, startTrivia, '<'))
            {
                return;
            }

            var token = startTrivia.Token;

            while (true)
            {
                token = token.GetNextToken(includeZeroWidth: true);
                if (token.RawKind == 0)
                {
                    return;
                }

                var index = GetEqualsConflictMarkerIndex(text, token);
                if (index >= 0)
                {
                    var leadingTrivia = token.LeadingTrivia;

                    if (index + 3 < token.LeadingTrivia.Count)
                    {
                        var equalsTrivia = leadingTrivia[index];
                        var endOfLineTrivia = leadingTrivia[index + 1];
                        var disabledTrivia = leadingTrivia[index + 2];
                        var endTrivia = leadingTrivia[index + 3];

                        if (IsEndOfLine(endOfLineTrivia) &&
                            IsDisabledText(disabledTrivia) &&
                            IsConflictMarker(text, endTrivia, '>'))
                        {
                            var topText = startTrivia.ToString().Substring(s_mergeConflictLength).Trim();
                            var takeTopText = string.IsNullOrWhiteSpace(topText)
                                ? FeaturesResources.Take_top
                                : string.Format(FeaturesResources.Take_0, topText);

                            var bottomText = endTrivia.ToString().Substring(s_mergeConflictLength).Trim();
                            var takeBottomText = string.IsNullOrWhiteSpace(bottomText)
                                ? FeaturesResources.Take_bottom
                                : string.Format(FeaturesResources.Take_0, bottomText);

                            var startSpan = startTrivia.Span;
                            var equalsSpan = equalsTrivia.Span;
                            var endSpan = endTrivia.Span;
                            
                            context.RegisterCodeFix(
                                new MyCodeAction(takeTopText, 
                                    c => TakeTopAsync(document, startSpan, equalsSpan, endSpan, c)),
                                context.Diagnostics);
                            context.RegisterCodeFix(
                                new MyCodeAction(takeBottomText, 
                                c => TakeBottomAsync(document, startSpan, equalsSpan, endSpan, c)),
                                context.Diagnostics);
                            context.RegisterCodeFix(
                                new MyCodeAction(FeaturesResources.Take_both, 
                                c => TakeBothAsync(document, startSpan, equalsSpan, endSpan, c)),
                                context.Diagnostics);
                        }
                    }
                }
            }
        }

        private async Task<Document> TakeTopAsync(
            Document document, TextSpan startSpan, TextSpan equalsSpan, TextSpan endSpan,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingNewLines(text, endSpan.End);
            var newText = text.Replace(TextSpan.FromBounds(equalsSpan.Start, bottomEnd), "");

            var startEnd = GetEndIncludingNewLines(text, startSpan.End);
            var finaltext = newText.Replace(TextSpan.FromBounds(startSpan.Start, startEnd), "");

            return document.WithText(finaltext);
        }

        private async Task<Document> TakeBottomAsync(
            Document document, TextSpan startSpan, TextSpan equalsSpan, TextSpan endSpan,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingNewLines(text, endSpan.End);
            var newText = text.Replace(TextSpan.FromBounds(endSpan.Start, bottomEnd), "");

            var equalsEnd = GetEndIncludingNewLines(text, equalsSpan.End);
            var finaltext = newText.Replace(TextSpan.FromBounds(startSpan.Start, equalsEnd), "");

            return document.WithText(finaltext);
        }

        private async Task<Document> TakeBothAsync(
            Document document,
            TextSpan startSpan, TextSpan equalsSpan, TextSpan endSpan,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingNewLines(text, endSpan.End);
            var newText = text.Replace(TextSpan.FromBounds(endSpan.Start, bottomEnd), "");

            var equalsEnd = GetEndIncludingNewLines(text, equalsSpan.End);
            newText = newText.Replace(TextSpan.FromBounds(equalsSpan.Start, equalsEnd), "");

            var startEnd = GetEndIncludingNewLines(text, startSpan.End);
            var finaltext = newText.Replace(TextSpan.FromBounds(startSpan.Start, startEnd), "");

            return document.WithText(finaltext);
        }

        private int GetEndIncludingNewLines(SourceText text, int position)
        {
            var endPosition = position;

            // Skip the text until we get to the newlines.
            while (endPosition < text.Length && !IsNewLine(text[endPosition]))
            {
                endPosition++;
            }

            // Skip the newlines.
            while (endPosition < text.Length && IsNewLine(text[endPosition]))
            {
                endPosition++;
            }

            return endPosition;
        }

        private int GetEqualsConflictMarkerIndex(SourceText text, SyntaxToken token)
        {
            if (token.HasLeadingTrivia)
            {
                int i = 0;
                foreach (var trivia in token.LeadingTrivia)
                {
                    if (IsConflictMarker(text, trivia, '='))
                    {
                        return i;
                    }

                    i++;
                }
            }

            return -1;
        }

        private SyntaxTrivia GetEqualsConflictMarker(SyntaxToken token)
        {
            throw new NotImplementedException();
        }

        private bool IsConflictMarker(SourceText text, SyntaxTrivia trivia, char ch)
        {
            return 
                IsConflictMarker(trivia) &&
                trivia.Span.Length > 0 &&
                text[trivia.SpanStart] == ch;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
