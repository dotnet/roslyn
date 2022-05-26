// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// 'Fix merge conflict markers' gets special privileges.  A core user scenario around them is that a user does
        /// a source control merge, gets conflicts, and then wants to open and edit them in the IDE very quickly.
        /// Forcing their fixes to be gated behind the set of normal fixes (which also involves semantic analysis) just
        /// slows the user down.  As we can compute this syntactically, and the user is almost certainly trying to fix
        /// them if they bring up the lightbulb on a <c>&lt;&lt;&lt;&lt;&lt;&lt;&lt;</c> line, it should run ahead of
        /// normal fix providers else so the user can quickly fix the conflict and move onto the next conflict.
        /// </summary>
        private protected override CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.High;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var position = context.Span.Start;
            if (!ShouldFix(root, text, position, out var startLine, out var middleLine, out var endLine))
                return;

            RegisterCodeFixes(context, startLine, middleLine, endLine);
        }

        private bool ShouldFix(
            SyntaxNode root, SourceText text, int position,
            out TextLine startLine, out TextLine middleLine, out TextLine endLine)
        {
            startLine = default;
            middleLine = default;
            endLine = default;

            var lines = text.Lines;
            var conflictLine = lines.GetLineFromPosition(position);
            if (position != conflictLine.Start)
            {
                Debug.Assert(false, "All conflict markers should start at the beginning of a line.");
                return false;
            }

            if (!TryGetConflictLines(text, position, out startLine, out middleLine, out endLine))
                return false;

            var startTrivia = root.FindTrivia(startLine.Start);
            var middleTrivia = root.FindTrivia(middleLine.Start);

            if (position == middleLine.Start)
            {
                // we were on the ======= lines.  We only want to report here if there was no
                // conflict trivia on the <<<<<<< line (since we would have already reported the
                // issue there.
                if (startTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                    return false;
            }
            else if (position == endLine.Start)
            {
                // we were on the >>>>>>> lines.  We only want to report here if there was no
                // conflict trivia on the ======= or <<<<<<< line (since we would have already reported the
                // issue there.
                if (startTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia ||
                    middleTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetConflictLines(
            SourceText text, int position,
            out TextLine startLine, out TextLine middleLine, out TextLine endLine)
        {
            startLine = default;
            middleLine = default;
            endLine = default;

            var lines = text.Lines;
            switch (text[position])
            {
                case '<':
                    startLine = lines.GetLineFromPosition(position);
                    return TryFindLineForwards(startLine, '=', out middleLine) &&
                           TryFindLineForwards(middleLine, '>', out endLine);
                case '=':
                    middleLine = lines.GetLineFromPosition(position);
                    return TryFindLineBackwards(middleLine, '<', out startLine) &&
                           TryFindLineForwards(middleLine, '>', out endLine);
                case '>':
                    endLine = lines.GetLineFromPosition(position);
                    return TryFindLineBackwards(endLine, '=', out middleLine) &&
                           TryFindLineBackwards(middleLine, '<', out startLine);
                default:
                    throw ExceptionUtilities.UnexpectedValue(text[position]);
            }
        }

        private static bool TryFindLineForwards(TextLine startLine, char ch, out TextLine foundLine)
        {
            var text = startLine.Text!;
            var lines = text.Lines;
            for (var i = startLine.LineNumber + 1; i < lines.Count; i++)
            {
                var currentLine = lines[i];
                if (IsConflictMarker(currentLine, ch))
                {
                    foundLine = currentLine;
                    return true;
                }
            }

            foundLine = default;
            return false;
        }

        private static bool TryFindLineBackwards(TextLine startLine, char ch, out TextLine foundLine)
        {
            var text = startLine.Text!;
            var lines = text.Lines;
            for (var i = startLine.LineNumber - 1; i >= 0; i--)
            {
                var currentLine = lines[i];
                if (IsConflictMarker(currentLine, ch))
                {
                    foundLine = currentLine;
                    return true;
                }
            }

            foundLine = default;
            return false;
        }

        private static bool IsConflictMarker(TextLine currentLine, char ch)
        {
            var text = currentLine.Text!;
            var currentLineStart = currentLine.Start;
            var currentLineLength = currentLine.End - currentLine.Start;
            if (currentLineLength < s_mergeConflictLength)
            {
                return false;
            }

            for (var j = 0; j < s_mergeConflictLength; j++)
            {
                if (text[currentLineStart + j] != ch)
                {
                    return false;
                }
            }

            return true;
        }

        private static void RegisterCodeFixes(
            CodeFixContext context, TextLine startLine, TextLine middleLine, TextLine endLine)
        {
            var document = context.Document;

            var topText = startLine.ToString()[s_mergeConflictLength..].Trim();
            var takeTopText = string.IsNullOrWhiteSpace(topText)
                ? FeaturesResources.Take_top
                : string.Format(FeaturesResources.Take_0, topText);

            var bottomText = endLine.ToString()[s_mergeConflictLength..].Trim();
            var takeBottomText = string.IsNullOrWhiteSpace(bottomText)
                ? FeaturesResources.Take_bottom
                : string.Format(FeaturesResources.Take_0, bottomText);

            var startPos = startLine.Start;
            var equalsPos = middleLine.Start;
            var endPos = endLine.Start;

            context.RegisterCodeFix(
                CodeAction.Create(takeTopText,
                    c => TakeTopAsync(document, startPos, equalsPos, endPos, c),
                    TakeTopEquivalenceKey),
                context.Diagnostics);
            context.RegisterCodeFix(
                CodeAction.Create(takeBottomText,
                    c => TakeBottomAsync(document, startPos, equalsPos, endPos, c),
                    TakeBottomEquivalenceKey),
                context.Diagnostics);
            context.RegisterCodeFix(
                CodeAction.Create(FeaturesResources.Take_both,
                    c => TakeBothAsync(document, startPos, equalsPos, endPos, c),
                    TakeBothEquivalenceKey),
                context.Diagnostics);
        }

        private static async Task<Document> AddEditsAsync(
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

        private static Task<Document> TakeTopAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddTopEdits, cancellationToken);

        private static Task<Document> TakeBottomAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddBottomEdits, cancellationToken);

        private static Task<Document> TakeBothAsync(Document document, int startPos, int equalsPos, int endPos, CancellationToken cancellationToken)
            => AddEditsAsync(document, startPos, equalsPos, endPos, AddBothEdits, cancellationToken);

        private static int GetEndIncludingLineBreak(SourceText text, int position)
            => text.Lines.GetLineFromPosition(position).SpanIncludingLineBreak.End;

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            string? equivalenceKey, CancellationToken cancellationToken)
        {
            Debug.Assert(
                equivalenceKey is TakeTopEquivalenceKey or
                TakeBottomEquivalenceKey or
                TakeBothEquivalenceKey);

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
                var position = diagnostic.Location.SourceSpan.Start;
                if (!ShouldFix(root, text, position, out var startLine, out var middleLine, out var endLine))
                    continue;

                var startPos = startLine.Start;
                var equalsPos = middleLine.Start;
                var endPos = endLine.Start;

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

            return finalDoc;
        }

        public override FixAllProvider GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) =>
                await this.FixAllAsync(document, diagnostics, context.CodeActionEquivalenceKey, context.CancellationToken).ConfigureAwait(false));
    }
}
