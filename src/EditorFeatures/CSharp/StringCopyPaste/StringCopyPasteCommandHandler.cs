// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
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
                subjectBuffer,
                snapshotBeforePaste,
                selectionsBeforePaste,
                executionContext);
            //}
        }

        private void ProcessPasteFromUnknownSource(
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

            // When pasting, only do anything special if the user selections were entirely inside a single stirng
            // literal token.  Otherwise, we have a multi-selection across token kinds which will be extremely 
            // complex to try to reconcile.
            if (!AllChangesInSameStringToken(rootBeforePaste, snapshotBeforePaste.AsText(), selectionsBeforePaste, out var tokenBeforePaste))
                return;

            // try to find the same token after the paste.  If it's got no errors, and still ends at the same expected
            // location, then it looks like what was pasted was entirely legal and should not be touched.
            var snapshotAfterPaste = subjectBuffer.CurrentSnapshot;
            var documentAfterPaste = snapshotAfterPaste.GetOpenDocumentInCurrentContextWithChanges();
            if (documentAfterPaste == null)
                return;

            var rootAfterPaste = documentAfterPaste.GetRequiredSyntaxRootSynchronously(cancellationToken);

            var tokenAfterPaste = rootAfterPaste.FindToken(tokenBeforePaste.SpanStart);
            if (tokenBeforePaste.Kind() == tokenAfterPaste.Kind() &&
                tokenBeforePaste.SpanStart == tokenAfterPaste.SpanStart &&
                !tokenAfterPaste.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                var trackingSpan = snapshotBeforePaste.CreateTrackingSpan(tokenBeforePaste.Span.ToSpan(), SpanTrackingMode.EdgeInclusive);
                var spanAfterPaste = trackingSpan.GetSpan(snapshotAfterPaste).Span.ToTextSpan();
                if (spanAfterPaste == tokenAfterPaste.Span)
                    return;
            }

            // Ok, the user pasted text that couldn't cleanly be added to this token without issue.
            // Repaste the contents, but this time properly escapes/manipulated so that it follows
            // the rule of the particular token kind.
            var textEscapedTextChanges = GetEscapedTextChanges(
                tokenBeforePaste, snapshotAfterPaste.Version.Changes);

            // Now, create a transaction, roll us back to the prior version, then apply these new changes
            using var transaction = new CaretPreservingEditTransaction(
                CSharpEditorFeaturesResources.Fixing_string_literal_after_paste,
                _undoHistoryRegistry,
                _editorOperationsFactoryService);
        }

        private static ImmutableArray<TextChange> GetEscapedTextChanges(SyntaxToken token, INormalizedTextChangeCollection changes)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var textChanges);

            foreach (var change in changes)
                textChanges.Add(new TextChange(change.OldSpan.ToTextSpan(), Escape(token, change.NewText)));

            return textChanges.ToImmutable();
        }

        private static string Escape(SyntaxToken token, string text)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken:
                    return EscapeForStringLiteral(token.IsVerbatimStringLiteral(), text);

                default:
                    throw ExceptionUtilities.UnexpectedValue(token.Kind());
            }
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

        private static bool AllChangesInSameStringToken(
            SyntaxNode root, SourceText text, NormalizedSnapshotSpanCollection selectionsBeforePaste, out SyntaxToken token)
        {
            token = default;
            foreach (var snapshotSpan in selectionsBeforePaste)
            {
                var position = snapshotSpan.Start.Position;
                var currentToken = root.FindToken(position);
                if (currentToken.Kind() == SyntaxKind.None)
                    return false;

                // keep track of the first token we see.
                if (token.Kind() == SyntaxKind.None)
                    token = currentToken;

                // if we hit a different token, immediately bail.
                if (token != currentToken)
                    return false;

                // Quick check that we're inside the token (and not its trivia)
                var selectedSpan = snapshotSpan.Span.ToTextSpan();
                if (!token.Span.Contains(selectedSpan))
                    return false;

                // Now do a stronger check that we're actually inside the bounds of a some string literal token.
                if (!IsContainedWithinSomeStringToken(token, text, selectedSpan))
                    return false;
            }

            return true;
        }

        private static bool IsContainedWithinSomeStringToken(
            SyntaxToken token, SourceText text, TextSpan selectedSpan)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken:
                    return IsContainedWithStringLiteralToken(token, text, selectedSpan);

                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                    return IsContainedWithinRawStringLiteralToken(token, text, selectedSpan);

                case SyntaxKind.InterpolatedStringTextToken:
                    return IsContainedWithinInterpolatedTextToken(token, selectedSpan);

                default:
                    // We hit some non-string token.  Don't do anything special on paste here.
                    return false;
            }
        }

        private static bool IsContainedWithStringLiteralToken(SyntaxToken token, SourceText text, TextSpan selectedSpan)
        {
            var start = token.SpanStart;
            var end = token.Span.End;

            if (start < text.Length && text[start] == '@')
                start++;

            if (start < text.Length && text[start] == '"')
                start++;

            if (end > start && text[end - 1] == '"')
                end--;

            return TextSpan.FromBounds(start, end).Contains(selectedSpan);
        }

        private static bool IsContainedWithinRawStringLiteralToken(SyntaxToken token, SourceText text, TextSpan selectedSpan)
        {
            var start = token.SpanStart;
            var end = token.Span.End;
            while (start < text.Length && text[start] == '"')
                start++;

            while (end > start && text[end - 1] == '"')
                end--;

            return TextSpan.FromBounds(start, end).Contains(selectedSpan);
        }

        private static bool IsContainedWithinInterpolatedTextToken(SyntaxToken token, TextSpan selectedSpan)
        {
            // interpolated text is trivial.  Because it contains no delimeters, it's fine to just check the selected span
            // against the token span itself.
            return token.Span.Contains(selectedSpan);
        }

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
