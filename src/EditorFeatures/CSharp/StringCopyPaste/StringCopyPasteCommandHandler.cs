// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    [Export(typeof(ICommandHandler))]
    [VSUtilities.ContentType(ContentTypeNames.CSharpContentType)]
    [VSUtilities.Name(nameof(StringCopyPasteCommandHandler))]
    internal class StringCopyPasteCommandHandler : IChainedCommandHandler<CopyCommandArgs>, IChainedCommandHandler<PasteCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IGlobalOptionService _globalOptions;

        private NormalizedSnapshotSpanCollection? _lastSelectedSpans;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StringCopyPasteCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IGlobalOptionService globalOptions)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _globalOptions = globalOptions;
        }

        public string DisplayName => nameof(StringCopyPasteCommandHandler);

        #region Copy

        public CommandState GetCommandState(CopyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(CopyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Ensure that the copy always goes through all other handlers.
            nextCommandHandler();

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            _lastSelectedSpans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        #endregion

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (!_globalOptions.GetOption(FeatureOnOffOptions.AutomaticallyFixStringContentsOnPaste))
                return;

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;

            var selectionsBeforePaste = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // if we're not even sure where the user caret/selection is on this buffer, we can't proceed.
            if (selectionsBeforePaste.Count == 0)
                return;

            var snapshotBeforePaste = subjectBuffer.CurrentSnapshot;

            // Always let the real paste go through.  That way we always have a version of the document that doesn't
            // include our changes that we can undo back to.
            nextCommandHandler();

            var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;

            // If there were multiple changes that already happened, then don't make any changes.  Some other component
            // already did something advanced.
            if (snapshotAfterPaste.Version != snapshotBeforePaste.Version.Next)
                return;

            // If the user pasted something other than the last piece of text we're tracking, then that means some other
            // copy happened, and we can't do anything special here.
            //if (PastedTextEqualsLastCopiedText(subjectBuffer))
            //{
            //    // ProcessPasteFromKnownSource();
            //}
            //else
            //{
            ProcessPasteFromUnknownSource(
                textView,
                subjectBuffer,
                snapshotBeforePaste,
                selectionsBeforePaste,
                executionContext);
            //}
        }

        private void ProcessPasteFromUnknownSource(
            ITextView textView,
            ITextBuffer subjectBuffer,
            ITextSnapshot snapshotBeforePaste,
            NormalizedSnapshotSpanCollection selectionsBeforePaste,
            CommandExecutionContext executionContext)
        {
            // Have to even be in a C# doc to be able to do anything here.
            var documentBeforePaste = snapshotBeforePaste.GetOpenDocumentInCurrentContextWithChanges();
            if (documentBeforePaste == null)
                return;

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

            var rootBeforePaste = documentBeforePaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

            // When pasting, only do anything special if the user selections were entirely inside a single string
            // literal token.  Otherwise, we have a multi-selection across token kinds which will be extremely 
            // complex to try to reconcile.
            if (!AllChangesInSameStringToken(rootBeforePaste, snapshotBeforePaste.AsText(), selectionsBeforePaste, out var stringExpression))
                return;

            var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;
            var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
            if (documentAfterPaste == null)
                return;

            // If the pasting was successful, then no need to change anything.
            if (PasteWasSuccessful(snapshotBeforePaste, snapshotAfterPaste, stringExpression, cancellationToken))
                return;

            // Ok, the user pasted text that couldn't cleanly be added to this token without issue.
            // Repaste the contents, but this time properly escapes/manipulated so that it follows
            // the rule of the particular token kind.
            var escapedTextChanges = GetEscapedTextChanges(
                stringExpression, snapshotBeforePaste.Version.Changes);

            var newTextAfterChanges = snapshotBeforePaste.AsText().WithChanges(escapedTextChanges);
            var newDocument = documentAfterPaste.WithText(newTextAfterChanges);

            using var transaction = new CaretPreservingEditTransaction(
                CSharpEditorResources.Fixing_string_literal_after_paste,
                textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
            transaction.Complete();
        }

        private static bool PasteWasSuccessful(
            ITextSnapshot snapshotBeforePaste,
            ITextSnapshot snapshotAfterPaste,
            ExpressionSyntax stringExpressionBeforePaste,
            CancellationToken cancellationToken)
        {
            // Pasting a control character into a normal string literal is normally not desired.  So even if this
            // is legal, we still escape the contents to make the pasted code clear.
            if (stringExpressionBeforePaste is LiteralExpressionSyntax literal && literal.Token.IsRegularStringLiteral())
            {
                if (ContainsControlCharacter(snapshotBeforePaste))
                    return false;
            }
            else if (stringExpressionBeforePaste is InterpolatedStringExpressionSyntax interpolatedString &&
                interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedStringStartToken)
            {
                if (ContainsControlCharacter(snapshotBeforePaste))
                    return false;
            }

            // try to find the same token after the paste.  If it's got no errors, and still ends at the same expected
            // location, then it looks like what was pasted was entirely legal and should probably not be touched.

            var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
            Contract.ThrowIfNull(documentAfterPaste);
            var rootAfterPaste = documentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

            var stringExpressionAfterPaste = FindContainingStringExpression(rootAfterPaste, stringExpressionBeforePaste.SpanStart);
            if (stringExpressionAfterPaste == null)
                return false;

            if (ContainsError(stringExpressionAfterPaste))
                return false;

            var trackingSpan = snapshotBeforePaste.CreateTrackingSpan(stringExpressionBeforePaste.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
            var spanAfterPaste = trackingSpan.GetSpan(snapshotAfterPaste).Span.ToTextSpan();
            return spanAfterPaste == stringExpressionAfterPaste.Span;
        }

        private static bool ContainsError(ExpressionSyntax stringExpression)
        {
            if (stringExpression is LiteralExpressionSyntax)
                return NodeOrTokenContainsError(stringExpression);

            if (stringExpression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                using var _ = PooledHashSet<Diagnostic>.GetInstance(out var errors);
                foreach (var diagnostic in interpolatedString.GetDiagnostics())
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                        errors.Add(diagnostic);
                }

                // we don't care about errors in holes.  Only errors in the content portions of the string.
                for (int i = 0, n = interpolatedString.Contents.Count; i < n && errors.Count > 0; i++)
                {
                    if (interpolatedString.Contents[i] is InterpolatedStringTextSyntax text)
                    {
                        foreach (var diagnostic in text.GetDiagnostics())
                            errors.Remove(diagnostic);
                    }
                }

                return errors.Count > 0;
            }

            throw ExceptionUtilities.UnexpectedValue(stringExpression);
        }

        private static bool NodeOrTokenContainsError(SyntaxNodeOrToken nodeOrToken)
        {
            foreach (var diagnostic in nodeOrToken.GetDiagnostics())
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    return true;
            }

            return false;
        }

        private static bool ContainsControlCharacter(ITextSnapshot snapshotBeforePaste)
        {
            return snapshotBeforePaste.Version.Changes.Any(c => ContainsControlCharacter(c.NewText));
        }

        private static bool ContainsControlCharacter(string newText)
        {
            foreach (var c in newText)
            {
                if (char.IsControl(c))
                    return true;
            }

            return false;
        }

        private static ImmutableArray<TextChange> GetEscapedTextChanges(
            ExpressionSyntax stringExpression, INormalizedTextChangeCollection changes)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var textChanges);

            foreach (var change in changes)
                textChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), Escape(stringExpression, change.NewText)));

            return textChanges.ToImmutable();
        }

        private static string Escape(ExpressionSyntax stringExpression, string text)
        {
            if (stringExpression is LiteralExpressionSyntax literalExpression)
            {
                if (stringExpression.Kind() == SyntaxKind.StringLiteralExpression)
                    return EscapeForStringLiteral(literalExpression.Token.IsVerbatimStringLiteral(), text);

                throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind());
            }
            else if (stringExpression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedStringStartToken)
                    return EscapeForStringLiteral(isVerbatim: false, text);

                if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
                    return EscapeForStringLiteral(isVerbatim: true, text);
            }

            throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind());
        }

        private static string EscapeForStringLiteral(bool isVerbatim, string value)
        {
            if (isVerbatim)
                return value.Replace("\"", "\"\"");

            using var _ = PooledStringBuilder.GetInstance(out var builder);

            // taken from object-display
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Surrogate)
                {
                    var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                    if (category == UnicodeCategory.Surrogate)
                    {
                        // an unpaired surrogate
                        builder.Append("\\u" + ((int)c).ToString("x4"));
                    }
                    else if (NeedsEscaping(category))
                    {
                        // a surrogate pair that needs to be escaped
                        var unicode = char.ConvertToUtf32(value, i);
                        builder.Append("\\U" + unicode.ToString("x8"));
                        i++; // skip the already-encoded second surrogate of the pair
                    }
                    else
                    {
                        // copy a printable surrogate pair directly
                        builder.Append(c);
                        builder.Append(value[++i]);
                    }
                }
                else if (TryReplaceChar(c, out var replaceWith))
                {
                    builder.Append(replaceWith);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static bool TryReplaceChar(char c, [NotNullWhen(true)] out string? replaceWith)
        {
            replaceWith = null;
            switch (c)
            {
                case '\\':
                    replaceWith = "\\\\";
                    break;
                case '\0':
                    replaceWith = "\\0";
                    break;
                case '\a':
                    replaceWith = "\\a";
                    break;
                case '\b':
                    replaceWith = "\\b";
                    break;
                case '\f':
                    replaceWith = "\\f";
                    break;
                case '\n':
                    replaceWith = "\\n";
                    break;
                case '\r':
                    replaceWith = "\\r";
                    break;
                case '\t':
                    replaceWith = "\\t";
                    break;
                case '\v':
                    replaceWith = "\\v";
                    break;
                case '"':
                    replaceWith = "\\\"";
                    break;
            }

            if (replaceWith != null)
                return true;

            if (NeedsEscaping(CharUnicodeInfo.GetUnicodeCategory(c)))
            {
                replaceWith = "\\u" + ((int)c).ToString("x4");
                return true;
            }

            return false;
        }

        private static bool NeedsEscaping(UnicodeCategory category)
        {
            switch (category)
            {
                case UnicodeCategory.Control:
                case UnicodeCategory.OtherNotAssigned:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.Surrogate:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the <see cref="LiteralExpressionSyntax"/> or <see cref="InterpolatedStringExpressionSyntax"/> if the
        /// selections were all contained within a single literal in a compatible fashion.  For interpolated strings,
        /// all the selections must be in the same <see cref="SyntaxKind.InterpolatedStringTextToken"/> token.
        /// </summary>
        private static bool AllChangesInSameStringToken(
            SyntaxNode root,
            SourceText text,
            NormalizedSnapshotSpanCollection selectionsBeforePaste,
            [NotNullWhen(true)] out ExpressionSyntax? stringExpression)
        {
            // First, try to see if all the selections are at least contained within a single string literal expression.
            stringExpression = FindContainingStringExpression(root, selectionsBeforePaste);
            if (stringExpression == null)
                return false;

            // Now, given that string expression, find the inside 'text' spans of the expression.  These are the parts
            // of the literal between the quotes.  It does not include the interpolation holes in an interpolated
            // string.  These spans may be empty (for an empty string, or empty text gap between interpolations).
            var contentSpans = GetContentSpans(text, stringExpression);

            // Now ensure that all the selections are contained within a single content span.
            int? spanIndex = null;
            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var currentIndex = contentSpans.BinarySearch(
                    snapshotSpan.Span.Start,
                    static (ts, pos) =>
                    {
                        if (ts.IntersectsWith(pos))
                            return 0;

                        if (ts.End < pos)
                            return -1;

                        return 1;
                    });

                if (currentIndex < 0)
                    return false;

                spanIndex ??= currentIndex;
                if (spanIndex != currentIndex)
                    return false;
            }

            return true;
        }

        private static ImmutableArray<TextSpan> GetContentSpans(
            SourceText text, ExpressionSyntax stringExpression)
        {
            if (stringExpression is LiteralExpressionSyntax)
            {
                // simple string literal (normal, verbatim or raw).
                //
                // Skip past the leading and trailing delimiters and add the span in between.
                if (stringExpression.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
                {
                    var start = stringExpression.SpanStart;
                    while (start < text.Length && text[start] == '"')
                        start++;

                    var end = stringExpression.Span.End;
                    while (end > start && text[end - 1] == '"')
                        end--;

                    return ImmutableArray.Create(TextSpan.FromBounds(start, end));
                }
                else
                {
                    var start = stringExpression.SpanStart;
                    if (start < text.Length && text[start] == '@')
                        start++;

                    if (start < text.Length && text[start] == '"')
                        start++;

                    var end = stringExpression.Span.End;
                    if (end > start && text[end - 1] == '"')
                        end--;

                    return ImmutableArray.Create(TextSpan.FromBounds(start, end));
                }
            }
            else if (stringExpression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                // Interpolated string.  Normal, verbatim, or raw.
                //
                // Skip past the leading and trailing delimiters.
                var start = stringExpression.SpanStart;
                while (start < text.Length && text[start] is '@' or '$')
                    start++;

                while (start < interpolatedString.StringStartToken.Span.End && text[start] == '"')
                    start++;

                var end = stringExpression.Span.End;
                while (end > interpolatedString.StringEndToken.Span.Start && text[end - 1] == '"')
                    end--;

                // Then walk the body of the interpolated string adding (possibly empty) spans for each chunk between
                // interpolations.
                using var result = TemporaryArray<TextSpan>.Empty;

                var currentPosition = start;
                for (var i = 0; i < interpolatedString.Contents.Count; i++)
                {
                    var content = interpolatedString.Contents[i];
                    if (content is InterpolationSyntax)
                    {
                        result.Add(TextSpan.FromBounds(currentPosition, content.SpanStart));
                        currentPosition = content.Span.End;
                    }
                }

                // Then, once through the body, add a final span from the end of the last interpolation to the end delimiter.
                result.Add(TextSpan.FromBounds(currentPosition, end));
                return result.ToImmutableAndClear();
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(stringExpression);
            }
        }

        private static ExpressionSyntax? FindContainingStringExpression(
            SyntaxNode root, NormalizedSnapshotSpanCollection selectionsBeforePaste)
        {
            ExpressionSyntax? expression = null;
            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var container = FindContainingStringExpression(root, snapshotSpan.Start.Position);
                if (container == null)
                    return null;

                expression ??= container;
                if (expression != container)
                    return null;
            }

            return expression;
        }

        private static ExpressionSyntax? FindContainingStringExpression(SyntaxNode root, int position)
        {
            var node = root.FindToken(position).Parent;
            for (var current = node; current != null; current = current.Parent)
            {
                if (current is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literalExpression)
                    return literalExpression;

                if (current is InterpolatedStringExpressionSyntax interpolatedString)
                    return interpolatedString;
            }

            return null;
        }

        //private static bool IsContainedWithinSomeStringToken(
        //    SyntaxToken token, SourceText text, TextSpan selectedSpan)
        //{
        //    switch (token.Kind())
        //    {
        //        case SyntaxKind.StringLiteralToken:
        //            return IsContainedWithStringLiteralToken(token, text, selectedSpan);

        //        case SyntaxKind.SingleLineRawStringLiteralToken:
        //        case SyntaxKind.MultiLineRawStringLiteralToken:
        //            return IsContainedWithinRawStringLiteralToken(token, text, selectedSpan);

        //        case SyntaxKind.InterpolatedStringTextToken:
        //            return IsContainedWithinInterpolatedTextToken(token, selectedSpan);

        //        case SyntaxKind.OpenBraceToken:
        //        case SyntaxKind.InterpolatedStringEndToken:
        //        case SyntaxKind.InterpolatedRawStringEndToken:
        //            return IsContainedWithinInterpolatedString(token, selectedSpan);

        //        default:
        //            // We hit some non-string token.  Don't do anything special on paste here.
        //            return false;
        //    }
        //}

        //private static bool IsContainedWithinInterpolatedString(SyntaxToken delimiterToken, TextSpan selectedSpan)
        //{
        //    // if we have `$"goo$$"`, then we're inside the string token (as long as the selection ends at the start of the delimiter).
        //    var interpolationExpression = delimiterToken.Parent as InterpolatedStringExpressionSyntax;
        //    if (interpolationExpression is null)
        //        return false;


        //}

        //private static bool IsContainedWithStringLiteralToken(SyntaxToken token, SourceText text, TextSpan selectedSpan)
        //{
        //    var start = token.SpanStart;
        //    var end = token.Span.End;

        //    if (start < text.Length && text[start] == '@')
        //        start++;

        //    if (start < text.Length && text[start] == '"')
        //        start++;

        //    if (end > start && text[end - 1] == '"')
        //        end--;

        //    return TextSpan.FromBounds(start, end).Contains(selectedSpan);
        //}

        //private static bool IsContainedWithinRawStringLiteralToken(SyntaxToken token, SourceText text, TextSpan selectedSpan)
        //{
        //    var start = token.SpanStart;
        //    var end = token.Span.End;
        //    while (start < text.Length && text[start] == '"')
        //        start++;

        //    while (end > start && text[end - 1] == '"')
        //        end--;

        //    return TextSpan.FromBounds(start, end).Contains(selectedSpan);
        //}

        //private static bool IsContainedWithinInterpolatedTextToken(SyntaxToken token, TextSpan selectedSpan)
        //{
        //    // interpolated text is trivial.  Because it contains no delimeters, it's fine to just check the selected span
        //    // against the token span itself.
        //    return token.Span.Contains(selectedSpan);
        //}

        private bool PastedTextEqualsLastCopiedText(ITextBuffer subjectBuffer)
        {
            // If we have no history of any copied text, then there's nothing in the past we can compare to.
            if (_lastSelectedSpans == null)
                return false;

            var copiedSpans = _lastSelectedSpans;
            var pastedChanges = subjectBuffer.CurrentSnapshot.Version.Changes;

            // If we don't have any actual changes to compare, we can't consider these the same.
            if (copiedSpans.Count == 0 || pastedChanges.Count == 0)
                return false;

            // Both the copied and pasted data is normalized.  So we should be able to compare counts to see
            // if they look the same.
            if (copiedSpans.Count != pastedChanges.Count)
                return false;

            // Validate each copied span from the source matches what was pasted into the destination.
            for (int i = 0, n = copiedSpans.Count; i < n; i++)
            {
                var copiedSpan = copiedSpans[i];
                var pastedChange = pastedChanges[i];

                if (copiedSpan.Length != pastedChange.NewLength)
                    return false;

                if (copiedSpan.GetText() != pastedChange.NewText)
                    return false;
            }

            return true;
        }
    }
}
