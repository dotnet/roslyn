// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion
{
    internal class XmlDocCommentCompletionItem : CompletionItem
    {
        private readonly string _beforeCaretText;
        private readonly string _afterCaretText;

        public XmlDocCommentCompletionItem(CompletionListProvider provider,
            TextSpan filterSpan,
            string displayText,
            CompletionItemRules rules)
            : this(provider, filterSpan, displayText, displayText, string.Empty, rules)
        {
        }

        public XmlDocCommentCompletionItem(CompletionListProvider provider,
            TextSpan filterSpan,
            string displayText,
            string beforeCaretText,
            string afterCaretText,
            CompletionItemRules rules)
            : base(provider, displayText, filterSpan, glyph: CodeAnalysis.Glyph.Keyword, rules: rules)
        {
            _beforeCaretText = beforeCaretText;
            _afterCaretText = afterCaretText;
        }

        internal void Commit(ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot snapshot, char? commitChar)
        {
            var replacementSpan = ComputeReplacementSpan(subjectBuffer, snapshot);

            var insertedText = InsertFirstHalf(textView, subjectBuffer, commitChar, replacementSpan);

            InsertSecondHalf(textView, subjectBuffer, insertedText, replacementSpan);

            var targetCaretPosition = textView.GetPositionInView(ComputeCaretPoint(subjectBuffer, replacementSpan, insertedText));
            textView.Caret.MoveTo(targetCaretPosition.Value);
        }

        private SnapshotPoint ComputeCaretPoint(ITextBuffer subjectBuffer, Span replacementSpan, string insertedText)
        {
            return new SnapshotPoint(subjectBuffer.CurrentSnapshot, replacementSpan.Start + insertedText.Length);
        }

        private void InsertSecondHalf(ITextView textView, ITextBuffer subjectBuffer, string insertedText, Span replacementSpan)
        {
            subjectBuffer.Insert(replacementSpan.Start + insertedText.Length, _afterCaretText);
        }

        private string InsertFirstHalf(ITextView textView, ITextBuffer subjectBuffer, char? commitChar, Span replacementSpan)
        {
            var insertedText = _beforeCaretText;

            if (commitChar.HasValue && !char.IsWhiteSpace(commitChar.Value) && commitChar.Value != insertedText[insertedText.Length - 1])
            {
                // The caret goes after whatever commit character we spit.
                insertedText += commitChar.Value;
            }

            subjectBuffer.Replace(replacementSpan, insertedText);
            return insertedText;
        }

        private Span ComputeReplacementSpan(ITextBuffer subjectBuffer, ITextSnapshot snapshot)
        {
            var trackingSpan = snapshot.CreateTrackingSpan(FilterSpan.ToSpan(), SpanTrackingMode.EdgeInclusive);
            var currentSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot);

            return Span.FromBounds(subjectBuffer.CurrentSnapshot[currentSpan.Start - 1] == '<' && _beforeCaretText[0] == '<'
                            ? currentSpan.Start - 1
                            : currentSpan.Start,
                            currentSpan.End);
        }

        private int FindTextExtent(int p, ITextSnapshot textSnapshot)
        {
            for (; p < textSnapshot.Length; p++)
            {
                if (!(char.IsLetterOrDigit(textSnapshot[p]) || textSnapshot[p] == '>'))
                {
                    break;
                }
            }

            return p;
        }
    }
}
