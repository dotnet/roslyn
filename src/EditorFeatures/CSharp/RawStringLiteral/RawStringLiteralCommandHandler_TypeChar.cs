// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral
{
    internal partial class RawStringLiteralCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext context)
        {
            if (!ExecuteCommandWorker(args, nextCommandHandler))
                nextCommandHandler();
        }

        private bool ExecuteCommandWorker(TypeCharCommandArgs args, Action nextCommandHandler)
        {
            if (args.TypedChar != '"')
                return false;

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            if (spans.Count != 1)
                return false;

            var span = spans.First();
            if (span.Length != 0)
                return false;

            var caret = textView.GetCaretPoint(subjectBuffer);
            if (caret == null)
                return false;

            var cancellationToken = CancellationToken.None;
            var textChangeOpt =
                TryGenerateInitialEmptyRawString(caret.Value, cancellationToken) ??
                TryGrowInitialEmptyRawString(caret.Value, cancellationToken) ??
                TryGrowRawStringDelimeters(caret.Value, cancellationToken);

            if (textChangeOpt is not TextChange textChange)
                return false;

            // Looks good.  First, let the quote get added by the normal type char handlers.  Then make our text change.
            // We do this in two steps so that undo can work properly.
            nextCommandHandler();

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Grow_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

            var edit = subjectBuffer.CreateEdit();
            edit.Insert(textChange.Span.Start, textChange.NewText);
            edit.Apply();

            // ensure the caret is placed after where the original quote got added.
            textView.Caret.MoveTo(new SnapshotPoint(subjectBuffer.CurrentSnapshot, caret.Value.Position + 1));

            transaction?.Complete();
            return true;
        }

        /// <summary>
        /// When typing <c>"</c> given a normal string like <c>""$$</c>, then update the text to be <c>"""$$"""</c>.
        /// Note that this puts the user in the position where TryGrowInitialEmptyRawString can now take effect.
        /// </summary>
        private static TextChange? TryGenerateInitialEmptyRawString(
            SnapshotPoint caret,
            CancellationToken cancellationToken)
        {
            var snapshot = caret.Snapshot;
            var position = caret.Position;

            // if we have ""$$"   then typing `"` here should not be handled by this path but by TryGrowInitialEmptyRawString
            if (position + 1 < snapshot.Length && snapshot[position + 1] == '"')
                return null;

            var start = position;
            while (start - 1 >= 0 && snapshot[start - 1] == '"')
                start--;

            // must have exactly `""`
            if (position - start != 2)
                return null;

            while (start - 1 >= 0 && snapshot[start - 1] == '$')
                start--;

            // hitting `"` after `@""` shouldn't do anything
            if (start - 1 >= 0 && snapshot[start - 1] == '@')
                return null;

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindToken(start);
            if (token.SpanStart != start)
                return null;

            if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedSingleLineRawStringStartToken))
                return null;

            return new TextChange(new TextSpan(position + 1, 0), "\"\"\"");
        }

        /// <summary>
        /// When typing <c>"</c> given a raw string like <c>"""$$"""</c> (or a similar multiline form), then update the
        /// text to be: <c>""""$$""""</c>.  i.e. grow both the start and end delimiters to keep the string properly
        /// balanced.  This differs from TryGrowRawStringDelimeters in that the language will consider that initial
        /// <c>""""""</c> text to be a single delimeter, while we want to treat it as two.
        /// </summary>
        private static TextChange? TryGrowInitialEmptyRawString(
            SnapshotPoint caret,
            CancellationToken cancellationToken)
        {
            var snapshot = caret.Snapshot;
            var position = caret.Position;

            var start = position;
            while (start - 1 >= 0 && snapshot[start - 1] == '"')
                start--;

            var end = position;
            while (end < snapshot.Length && snapshot[end] == '"')
                end++;

            // Have to have an even number of quotes.
            var quoteLength = end - start;
            if (quoteLength % 2 == 1)
                return null;

            // User position must be halfway through the quotes.
            if (position != (start + quoteLength / 2))
                return null;

            // have to at least have `"""$$"""`
            if (quoteLength < 6)
                return null;

            while (start - 1 >= 0 && snapshot[start - 1] == '$')
                start--;

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindToken(start);
            if (token.SpanStart != start)
                return null;

            if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                     SyntaxKind.MultiLineRawStringLiteralToken or
                                     SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                     SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                return null;
            }

            return new TextChange(new TextSpan(position + 1, 0), "\"");
        }

        /// <summary>
        /// When typing <c>"</c> given a raw string like <c>"""$$ goo bar """</c> (or a similar multiline form), then
        /// update the text to be: <c>"""" goo bar """"</c>.  i.e. grow both the start and end delimiters to keep the
        /// string properly balanced.
        /// </summary>
        private static TextChange? TryGrowRawStringDelimeters(
            SnapshotPoint caret,
            CancellationToken cancellationToken)
        {
            var snapshot = caret.Snapshot;
            var position = caret.Position;

            // if we have """$$"   then typing `"` here should not grow the start/end quotes.  we only want to grow them
            // if the user is at the end of the start delimeter.
            if (position < snapshot.Length && snapshot[position] == '"')
                return null;

            var start = position;
            while (start - 1 >= 0 && snapshot[start - 1] == '"')
                start--;

            // must have at least three quotes for this to be a raw string
            var quoteCount = position - start;
            if (quoteCount < 3)
                return null;

            while (start - 1 >= 0 && snapshot[start - 1] == '$')
                start--;

            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindToken(start);
            if (token.SpanStart != start)
                return null;

            if (token.Span.Length < (2 * quoteCount))
                return null;

            if (token.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken)
            {
                var interpolatedString = (InterpolatedStringExpressionSyntax)token.GetRequiredParent();
                var endToken = interpolatedString.StringEndToken;
                if (!endToken.Text.EndsWith(new string('"', quoteCount)))
                    return null;
            }
            else if (token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
            {
                if (!token.Text.EndsWith(new string('"', quoteCount)))
                    return null;
            }

            return new TextChange(new TextSpan(token.GetRequiredParent().Span.End, 0), "\"");
        }
    }
}
