// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not currently supported for this code fix
            // https://github.com/dotnet/roslyn/issues/34461
            return null;
        }

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
                var index = GetEqualsConflictMarkerIndex(text, token);
                if (index >= 0)
                {
                    var leadingTrivia = token.LeadingTrivia;

                    if (index + 3 < token.LeadingTrivia.Count)
                    {
                        // normal case where there us =====, then dead code, then >>>>>>

                        var equalsTrivia = leadingTrivia[index];
                        var endOfLineTrivia = leadingTrivia[index + 1];
                        var disabledTrivia = leadingTrivia[index + 2];
                        var endTrivia = leadingTrivia[index + 3];

                        if (IsEndOfLine(endOfLineTrivia) &&
                            IsDisabledText(disabledTrivia) &&
                            IsConflictMarker(text, endTrivia, '>'))
                        {
                            RegisterCodeFixes(context, startTrivia, equalsTrivia, endTrivia);
                            return;
                        }
                    }

                    if (index + 2 < token.LeadingTrivia.Count)
                    {
                        // case where there is ===== followed by >>>>>>  on the next line.

                        var equalsTrivia = leadingTrivia[index];
                        var endOfLineTrivia = leadingTrivia[index + 1];
                        var endTrivia = leadingTrivia[index + 2];

                        if (IsEndOfLine(endOfLineTrivia) &&
                            IsConflictMarker(text, endTrivia, '>'))
                        {
                            RegisterCodeFixes(context, startTrivia, equalsTrivia, endTrivia);
                            return;
                        }
                    }
                }

                token = token.GetNextToken(includeZeroWidth: true);
                if (token.RawKind == 0)
                {
                    return;
                }
            }
        }

        private void RegisterCodeFixes(
            CodeFixContext context, SyntaxTrivia startTrivia, SyntaxTrivia equalsTrivia, SyntaxTrivia endTrivia)
        {
            var document = context.Document;

            var topText = startTrivia.ToString().Substring(s_mergeConflictLength).Trim();
            var takeTopText = string.IsNullOrWhiteSpace(topText)
                ? FeaturesResources.Take_top
                : string.Format(FeaturesResources.Take_0, topText);

            var bottomText = endTrivia.ToString().Substring(s_mergeConflictLength).Trim();
            var takeBottomText = string.IsNullOrWhiteSpace(bottomText)
                ? FeaturesResources.Take_bottom
                : string.Format(FeaturesResources.Take_0, bottomText);

            var startPos = startTrivia.SpanStart;
            var equalsPos = equalsTrivia.SpanStart;
            var endPos = endTrivia.SpanStart;

            context.RegisterCodeFix(
                new MyCodeAction(takeTopText,
                    c => TakeTopAsync(document, startPos, equalsPos, endPos, c)),
                context.Diagnostics);
            context.RegisterCodeFix(
                new MyCodeAction(takeBottomText,
                    c => TakeBottomAsync(document, startPos, equalsPos, endPos, c)),
                context.Diagnostics);
            context.RegisterCodeFix(
                new MyCodeAction(FeaturesResources.Take_both,
                    c => TakeBothAsync(document, startPos, equalsPos, endPos, c)),
                context.Diagnostics);
        }

        private async Task<Document> TakeTopAsync(
            Document document, int startPos, int equalsPos, int endPos,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            var newText = text.Replace(TextSpan.FromBounds(equalsPos, bottomEnd), "");

            var startEnd = GetEndIncludingLineBreak(text, startPos);
            var finaltext = newText.Replace(TextSpan.FromBounds(startPos, startEnd), "");

            return document.WithText(finaltext);
        }

        private async Task<Document> TakeBottomAsync(
            Document document, int startPos, int equalsPos, int endPos,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            var newText = text.Replace(TextSpan.FromBounds(endPos, bottomEnd), "");

            var equalsEnd = GetEndIncludingLineBreak(text, equalsPos);
            var finaltext = newText.Replace(TextSpan.FromBounds(startPos, equalsEnd), "");

            return document.WithText(finaltext);
        }

        private async Task<Document> TakeBothAsync(
            Document document, int startPos, int equalsPos, int endPos,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            var newText = text.Replace(TextSpan.FromBounds(endPos, bottomEnd), "");

            var equalsEnd = GetEndIncludingLineBreak(text, equalsPos);
            newText = newText.Replace(TextSpan.FromBounds(equalsPos, equalsEnd), "");

            var startEnd = GetEndIncludingLineBreak(text, startPos);
            var finaltext = newText.Replace(TextSpan.FromBounds(startPos, startEnd), "");

            return document.WithText(finaltext);
        }

        private int GetEndIncludingLineBreak(SourceText text, int position)
            => text.Lines.GetLineFromPosition(position).SpanIncludingLineBreak.End;

        private int GetEqualsConflictMarkerIndex(SourceText text, SyntaxToken token)
        {
            if (token.HasLeadingTrivia)
            {
                var i = 0;
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
