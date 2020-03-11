// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConflictMarkerResolution
{
    internal abstract partial class AbstractResolveConflictMarkerCodeFixProvider : CodeFixProvider
    {
        internal const string TakeTopEquivalenceKey = nameof(TakeTopEquivalenceKey);
        internal const string TakeBottomEquivalenceKey = nameof(TakeBottomEquivalenceKey);
        internal const string TakeBothEquivalenceKey = nameof(TakeBothEquivalenceKey);

        private static readonly int s_mergeConflictLength = "<<<<<<<".Length;

        private readonly ISyntaxKinds _syntaxKinds;

        protected AbstractResolveConflictMarkerCodeFixProvider(
            ISyntaxKinds syntaxKinds, string diagnosticId)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
            _syntaxKinds = syntaxKinds;
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }

        public override FixAllProvider GetFixAllProvider()
            => new ConflictMarkerFixAllProvider(this);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var startTrivia = root.FindTrivia(context.Span.Start);
            if (!IsConflictMarker(text, startTrivia, '<'))
                return;

            var conflictTrivia = TryGetConflictTrivia(text, startTrivia);
            if (conflictTrivia == null)
                return;

            var (equalsTrivia, endTrivia) = conflictTrivia.Value;
            RegisterCodeFixes(context, startTrivia, equalsTrivia, endTrivia);
        }

        private (SyntaxTrivia equalsTrivia, SyntaxTrivia endTrivia)? TryGetConflictTrivia(SourceText text, SyntaxTrivia startTrivia)
        {
            var token = startTrivia.Token;

            while (true)
            {
                var index = GetEqualsConflictMarkerIndex(text, token, afterPosition: startTrivia.SpanStart);
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

                        if (_syntaxKinds.EndOfLineTrivia == endOfLineTrivia.RawKind &&
                            _syntaxKinds.DisabledTextTrivia == disabledTrivia.RawKind &&
                            IsConflictMarker(text, endTrivia, '>'))
                        {
                            return (equalsTrivia, endTrivia);
                        }
                    }

                    if (index + 2 < token.LeadingTrivia.Count)
                    {
                        // case where there is ===== followed by >>>>>>  on the next line.

                        var equalsTrivia = leadingTrivia[index];
                        var endOfLineTrivia = leadingTrivia[index + 1];
                        var endTrivia = leadingTrivia[index + 2];

                        if (_syntaxKinds.EndOfLineTrivia == endOfLineTrivia.RawKind &&
                            IsConflictMarker(text, endTrivia, '>'))
                        {
                            return (equalsTrivia, endTrivia);
                        }
                    }
                }

                token = token.GetNextToken(includeZeroWidth: true);
                if (token.RawKind == 0)
                {
                    return null;
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
                    c => TakeTopAsync(document, startPos, equalsPos, endPos, c),
                    TakeTopEquivalenceKey),
                context.Diagnostics);
            context.RegisterCodeFix(
                new MyCodeAction(takeBottomText,
                    c => TakeBottomAsync(document, startPos, equalsPos, endPos, c),
                    TakeBottomEquivalenceKey),
                context.Diagnostics);
            context.RegisterCodeFix(
                new MyCodeAction(FeaturesResources.Take_both,
                    c => TakeBothAsync(document, startPos, equalsPos, endPos, c),
                    TakeBothEquivalenceKey),
                context.Diagnostics);
        }

        private async Task<Document> AddEditsAsync(
            Document document, int startPos, int equalsPos, int endPos,
            Action<SourceText, ArrayBuilder<TextChange>, int, int, int> addEdits,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);
            addEdits(text, edits, startPos, equalsPos, endPos);

            var finalText = text.WithChanges(edits);
            return document.WithText(finalText);
        }

        private static void AddTopEdits(
            SourceText text, ArrayBuilder<TextChange> edits,
            int startPos, int equalsPos, int endPos)
        {
            // Delete the line containing <<<<<<<
            var startEnd = GetEndIncludingLineBreak(text, startPos);
            edits.Add(new TextChange(TextSpan.FromBounds(startPos, startEnd), ""));

            // Remove the chunk of text (inclusive) from ======= through >>>>>>>
            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            edits.Add(new TextChange(TextSpan.FromBounds(equalsPos, bottomEnd), ""));
        }

        private static void AddBottomEdits(
            SourceText text, ArrayBuilder<TextChange> edits,
            int startPos, int equalsPos, int endPos)
        {
            // Remove the chunk of text (inclusive) from <<<<<<< through =======
            var equalsEnd = GetEndIncludingLineBreak(text, equalsPos);
            edits.Add(new TextChange(TextSpan.FromBounds(startPos, equalsEnd), ""));

            // Delete the line containing >>>>>>> 
            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            edits.Add(new TextChange(TextSpan.FromBounds(endPos, bottomEnd), ""));
        }

        private static void AddBothEdits(
            SourceText text, ArrayBuilder<TextChange> edits,
            int startPos, int equalsPos, int endPos)
        {
            // Delete the line containing <<<<<<<
            var startEnd = GetEndIncludingLineBreak(text, startPos);
            edits.Add(new TextChange(TextSpan.FromBounds(startPos, startEnd), ""));

            // Delete the line containing =======
            var equalsEnd = GetEndIncludingLineBreak(text, equalsPos);
            edits.Add(new TextChange(TextSpan.FromBounds(equalsPos, equalsEnd), ""));

            // Delete the line containing >>>>>>>
            var bottomEnd = GetEndIncludingLineBreak(text, endPos);
            edits.Add(new TextChange(TextSpan.FromBounds(endPos, bottomEnd), ""));
        }

        private Task<Document> TakeTopAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddTopEdits, cancellationToken);

        private Task<Document> TakeBottomAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddBottomEdits, cancellationToken);

        private Task<Document> TakeBothAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddBothEdits, cancellationToken);

        private static int GetEndIncludingLineBreak(SourceText text, int position)
            => text.Lines.GetLineFromPosition(position).SpanIncludingLineBreak.End;

        private int GetEqualsConflictMarkerIndex(SourceText text, SyntaxToken token, int afterPosition)
        {
            if (token.HasLeadingTrivia)
            {
                var i = 0;
                foreach (var trivia in token.LeadingTrivia)
                {
                    if (trivia.SpanStart >= afterPosition &&
                        IsConflictMarker(text, trivia, '='))
                    {
                        return i;
                    }

                    i++;
                }
            }

            return -1;
        }

        private bool IsConflictMarker(SourceText text, SyntaxTrivia trivia, char ch)
        {
            return
                _syntaxKinds.ConflictMarkerTrivia == trivia.RawKind &&
                trivia.Span.Length > 0 &&
                text[trivia.SpanStart] == ch;
        }

        private async Task<SyntaxNode> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            string equivalenceKey, CancellationToken cancellationToken)
        {
            Debug.Assert(
                equivalenceKey == TakeTopEquivalenceKey ||
                equivalenceKey == TakeBottomEquivalenceKey ||
                equivalenceKey == TakeBothEquivalenceKey);

            // Process diagnostics in order so we produce edits in the right order.
            var orderedDiagnostics = diagnostics.OrderBy(
                (d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start).ToImmutableArray();

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Create a single array of edits to apply.  Then walk over all the
            // conflict-marker-regions we want to fix and add the edits for each
            // region into that array.  Then apply the array just once to get the
            // final document.
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

            foreach (var diagnostic in diagnostics)
            {
                var startTrivia = root.FindTrivia(diagnostic.Location.SourceSpan.Start);

                // We'll be called on all the conflict marker diagnostics (i.e. for <<<<<<< =======
                // and >>>>>>>). We only care about the <<<<<<< ones as that controls which chunks
                // we'll be processing.
                if (!IsConflictMarker(text, startTrivia, '<'))
                    continue;

                var conflictTrivia = TryGetConflictTrivia(text, startTrivia);
                if (conflictTrivia == null)
                    continue;

                var startPos = startTrivia.SpanStart;
                var equalsPos = conflictTrivia.Value.equalsTrivia.SpanStart;
                var endPos = conflictTrivia.Value.endTrivia.SpanStart;

                switch (equivalenceKey)
                {
                    case TakeTopEquivalenceKey:
                        AddTopEdits(text, edits, startPos, equalsPos, endPos);
                        continue;

                    case TakeBottomEquivalenceKey:
                        AddBottomEdits(text, edits, startPos, equalsPos, endPos);
                        continue;

                    case TakeBothEquivalenceKey:
                        AddBothEdits(text, edits, startPos, equalsPos, endPos);
                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(equivalenceKey);
                }
            }

            var finalText = text.WithChanges(edits);
            var finalDoc = document.WithText(finalText);

            return await finalDoc.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
