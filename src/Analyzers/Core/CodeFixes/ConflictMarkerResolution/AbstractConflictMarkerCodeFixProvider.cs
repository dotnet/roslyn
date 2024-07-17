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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConflictMarkerResolution;

/// <summary>
/// This code fixer helps remove version conflict markers in code by offering the choice
/// of which version to keep and which version to discard.
///
/// Conflict markers come in two flavors, diff3 and diff formats.
///
/// diff3 has a start marker, followed by a first middle markers and a second middle marker, and terminate with an end marker.
///   The disabled text between the first and second middle markers is the baseline for the three-way diff.
///   The fixer always discards this baseline text.
///
/// diff has a start marker, followed by a middle marker, and terminates with an end marker.
///   We treat the middle marker as both the first and second middle markers (degenerate case with no baseline).
/// </summary>
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
        FixableDiagnosticIds = [diagnosticId];
        _syntaxKinds = syntaxKinds;

#if !CODE_STYLE
        // Backdoor that allows this provider to use the high-priority bucket.
        this.CustomTags = this.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
#endif
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
    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.High;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var position = context.Span.Start;
        if (!ShouldFix(root, text, position, out var startLine, out var firstMiddleLine, out var secondMiddleLine, out var endLine))
            return;

        RegisterCodeFixes(context, startLine, firstMiddleLine, secondMiddleLine, endLine);
    }

    private bool ShouldFix(
        SyntaxNode root, SourceText text, int position,
        out TextLine startLine, out TextLine firstMiddleLine, out TextLine secondMiddleLine, out TextLine endLine)
    {
        startLine = default;
        firstMiddleLine = default;
        secondMiddleLine = default;
        endLine = default;

        var lines = text.Lines;
        var conflictLine = lines.GetLineFromPosition(position);
        if (position != conflictLine.Start)
        {
            Debug.Assert(false, "All conflict markers should start at the beginning of a line.");
            return false;
        }

        if (!TryGetConflictLines(text, position, out startLine, out firstMiddleLine, out secondMiddleLine, out endLine))
            return false;

        var startTrivia = root.FindTrivia(startLine.Start);
        var firstMiddleTrivia = root.FindTrivia(firstMiddleLine.Start);
        var secondMiddleTrivia = root.FindTrivia(secondMiddleLine.Start);

        if (position == firstMiddleLine.Start)
        {
            // We were on the ||||||| line.
            // We don't want to report here if there was conflict trivia on the <<<<<<< line  (since we would have already reported the issue there).
            if (startTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;
        }
        else if (position == secondMiddleLine.Start)
        {
            // We were on the ======= line.
            // We don't want to report here if there was conflict trivia on the <<<<<<< line  (since we would have already reported the issue there).
            if (startTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;

            // We don't want to report here if there was conflict trivia on the ||||||| line  (since we would have already reported the issue there).
            if (firstMiddleLine != secondMiddleLine && firstMiddleTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;
        }
        else if (position == endLine.Start)
        {
            // We were on the >>>>>>> line.
            // We don't want to report here if there was conflict trivia on the <<<<<<< line  (since we would have already reported the issue there).
            if (startTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;

            // We don't want to report here if there was conflict trivia on the ||||||| line  (since we would have already reported the issue there).
            if (firstMiddleLine != secondMiddleLine && firstMiddleTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;

            // We don't want to report here if there was conflict trivia on the ======= line  (since we would have already reported the issue there).
            if (secondMiddleTrivia.RawKind == _syntaxKinds.ConflictMarkerTrivia)
                return false;
        }

        return true;
    }

    private static bool TryGetConflictLines(
        SourceText text, int position,
        out TextLine startLine, out TextLine firstMiddleLine, out TextLine secondMiddleLine, out TextLine endLine)
    {
        startLine = default;
        firstMiddleLine = default;
        secondMiddleLine = default;
        endLine = default;

        var lines = text.Lines;
        bool foundBarLine;
        switch (text[position])
        {
            case '<':
                startLine = lines.GetLineFromPosition(position);
                foundBarLine = TryFindLineForwards(startLine, '|', out firstMiddleLine);

                if (!TryFindLineForwards(foundBarLine ? firstMiddleLine : startLine, '=', out secondMiddleLine) ||
                    !TryFindLineForwards(secondMiddleLine, '>', out endLine))
                {
                    return false;
                }

                break;
            case '|':
                firstMiddleLine = lines.GetLineFromPosition(position);
                return TryFindLineBackwards(firstMiddleLine, '<', out startLine) &&
                       TryFindLineForwards(firstMiddleLine, '=', out secondMiddleLine) &&
                       TryFindLineForwards(secondMiddleLine, '>', out endLine);
            case '=':
                secondMiddleLine = lines.GetLineFromPosition(position);
                foundBarLine = TryFindLineBackwards(secondMiddleLine, '|', out firstMiddleLine);

                if (!TryFindLineBackwards(foundBarLine ? firstMiddleLine : secondMiddleLine, '<', out startLine) ||
                    !TryFindLineForwards(secondMiddleLine, '>', out endLine))
                {
                    return false;
                }

                break;
            case '>':
                endLine = lines.GetLineFromPosition(position);
                if (!TryFindLineBackwards(endLine, '=', out secondMiddleLine))
                {
                    return false;
                }

                foundBarLine = TryFindLineBackwards(secondMiddleLine, '|', out firstMiddleLine);

                if (!TryFindLineBackwards(foundBarLine ? firstMiddleLine : secondMiddleLine, '<', out startLine))
                    return false;

                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(text[position]);
        }

        if (!foundBarLine)
            firstMiddleLine = secondMiddleLine;

        return true;
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
        CodeFixContext context, TextLine startLine, TextLine firstMiddleLine, TextLine secondMiddleLine, TextLine endLine)
    {
        var document = context.Document;

        var topText = startLine.ToString()[s_mergeConflictLength..].Trim();
        var takeTopText = string.IsNullOrWhiteSpace(topText)
            ? CodeFixesResources.Take_top
            : string.Format(CodeFixesResources.Take_0, topText);

        var bottomText = endLine.ToString()[s_mergeConflictLength..].Trim();
        var takeBottomText = string.IsNullOrWhiteSpace(bottomText)
            ? CodeFixesResources.Take_bottom
            : string.Format(CodeFixesResources.Take_0, bottomText);

        var startPos = startLine.Start;
        var firstMiddlePos = firstMiddleLine.Start;
        var secondMiddlePos = secondMiddleLine.Start;
        var endPos = endLine.Start;

        context.RegisterCodeFix(
            CreateCodeAction(takeTopText,
                c => TakeTopAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, c),
                TakeTopEquivalenceKey),
            context.Diagnostics);
        context.RegisterCodeFix(
            CreateCodeAction(takeBottomText,
                c => TakeBottomAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, c),
                TakeBottomEquivalenceKey),
            context.Diagnostics);
        context.RegisterCodeFix(
            CreateCodeAction(CodeFixesResources.Take_both,
                c => TakeBothAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, c),
                TakeBothEquivalenceKey),
            context.Diagnostics);

        static CodeAction CreateCodeAction(string title, Func<CancellationToken, Task<Document>> action, string equivalenceKey)
        {
            var codeAction = CodeAction.Create(title, action, equivalenceKey, CodeActionPriority.High);

#if !CODE_STYLE
            // Backdoor that allows this provider to use the high-priority bucket.
            codeAction.CustomTags = codeAction.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
#endif

            return codeAction;
        }
    }

    private static async Task<Document> AddEditsAsync(
        Document document, int startPos, int firstMiddlePos, int secondMiddlePos, int endPos,
        Action<SourceText, ArrayBuilder<TextChange>, int, int, int, int> addEdits,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);
        addEdits(text, edits, startPos, firstMiddlePos, secondMiddlePos, endPos);

        var finalText = text.WithChanges(edits);
        return document.WithText(finalText);
    }

    private static void AddTopEdits(
        SourceText text, ArrayBuilder<TextChange> edits,
        int startPos, int firstMiddlePos, int secondMiddlePos, int endPos)
    {
        // Delete the line containing <<<<<<<
        var startEnd = GetEndIncludingLineBreak(text, startPos);
        edits.Add(new TextChange(TextSpan.FromBounds(startPos, startEnd), ""));

        // Remove the chunk of text (inclusive) from ||||||| or ======= through >>>>>>>
        var bottomEnd = GetEndIncludingLineBreak(text, endPos);
        edits.Add(new TextChange(TextSpan.FromBounds(firstMiddlePos, bottomEnd), ""));
    }

    private static void AddBottomEdits(
        SourceText text, ArrayBuilder<TextChange> edits,
        int startPos, int firstMiddlePos, int secondMiddlePos, int endPos)
    {
        // Remove the chunk of text (inclusive) from <<<<<<< through =======
        var equalsEnd = GetEndIncludingLineBreak(text, secondMiddlePos);
        edits.Add(new TextChange(TextSpan.FromBounds(startPos, equalsEnd), ""));

        // Delete the line containing >>>>>>>
        var bottomEnd = GetEndIncludingLineBreak(text, endPos);
        edits.Add(new TextChange(TextSpan.FromBounds(endPos, bottomEnd), ""));
    }

    private static void AddBothEdits(
        SourceText text, ArrayBuilder<TextChange> edits,
        int startPos, int firstMiddlePos, int secondMiddlePos, int endPos)
    {
        // Delete the line containing <<<<<<<
        var startEnd = GetEndIncludingLineBreak(text, startPos);
        edits.Add(new TextChange(TextSpan.FromBounds(startPos, startEnd), ""));

        if (firstMiddlePos == secondMiddlePos)
        {
            // Delete the line containing =======
            var equalsEnd = GetEndIncludingLineBreak(text, secondMiddlePos);
            edits.Add(new TextChange(TextSpan.FromBounds(secondMiddlePos, equalsEnd), ""));
        }
        else
        {
            // Remove the chunk of text (inclusive) from ||||||| through =======
            var equalsEnd = GetEndIncludingLineBreak(text, secondMiddlePos);
            edits.Add(new TextChange(TextSpan.FromBounds(firstMiddlePos, equalsEnd), ""));
        }

        // Delete the line containing >>>>>>>
        var bottomEnd = GetEndIncludingLineBreak(text, endPos);
        edits.Add(new TextChange(TextSpan.FromBounds(endPos, bottomEnd), ""));
    }

    private static Task<Document> TakeTopAsync(Document document, int startPos, int firstMiddlePos, int secondMiddlePos, int endPos, CancellationToken cancellationToken)
        => AddEditsAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, AddTopEdits, cancellationToken);

    private static Task<Document> TakeBottomAsync(Document document, int startPos, int firstMiddlePos, int secondMiddlePos, int endPos, CancellationToken cancellationToken)
        => AddEditsAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, AddBottomEdits, cancellationToken);

    private static Task<Document> TakeBothAsync(Document document, int startPos, int firstMiddlePos, int secondMiddlePos, int endPos, CancellationToken cancellationToken)
        => AddEditsAsync(document, startPos, firstMiddlePos, secondMiddlePos, endPos, AddBothEdits, cancellationToken);

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

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Create a single array of edits to apply.  Then walk over all the
        // conflict-marker-regions we want to fix and add the edits for each
        // region into that array.  Then apply the array just once to get the
        // final document.
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

        foreach (var diagnostic in diagnostics)
        {
            var position = diagnostic.Location.SourceSpan.Start;
            if (!ShouldFix(root, text, position, out var startLine, out var firstMiddleLine, out var secondMiddleLine, out var endLine))
                continue;

            var startPos = startLine.Start;
            var firstMiddlePos = firstMiddleLine.Start;
            var secondMiddlePos = secondMiddleLine.Start;
            var endPos = endLine.Start;

            switch (equivalenceKey)
            {
                case TakeTopEquivalenceKey:
                    AddTopEdits(text, edits, startPos, firstMiddlePos, secondMiddlePos, endPos);
                    continue;

                case TakeBottomEquivalenceKey:
                    AddBottomEdits(text, edits, startPos, firstMiddlePos, secondMiddlePos, endPos);
                    continue;

                case TakeBothEquivalenceKey:
                    AddBothEdits(text, edits, startPos, firstMiddlePos, secondMiddlePos, endPos);
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
