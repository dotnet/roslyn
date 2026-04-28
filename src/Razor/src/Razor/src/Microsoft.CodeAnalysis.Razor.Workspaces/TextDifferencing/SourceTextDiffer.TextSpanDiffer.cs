// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal partial class SourceTextDiffer
{
    private abstract class TextSpanDiffer : SourceTextDiffer
    {
        private readonly ImmutableArray<TextSpan> _oldSpans = [];
        private readonly ImmutableArray<TextSpan> _newSpans = [];

        private char[] _oldBuffer;
        private char[] _newBuffer;
        private char[] _appendBuffer;

        protected override int OldSourceLength { get; }
        protected override int NewSourceLength { get; }

        public TextSpanDiffer(SourceText oldText, SourceText newText)
            : base(oldText, newText)
        {
            _oldBuffer = RentArray(1024);
            _newBuffer = RentArray(1024);
            _appendBuffer = RentArray(1024);

            if (oldText.Length > 0)
            {
                _oldSpans = Tokenize(oldText);
            }

            if (newText.Length > 0)
            {
                _newSpans = Tokenize(newText);
            }

            OldSourceLength = _oldSpans.Length;
            NewSourceLength = _newSpans.Length;
        }

        protected abstract ImmutableArray<TextSpan> Tokenize(SourceText text);

        public override void Dispose()
        {
            ReturnArray(_oldBuffer);
            ReturnArray(_newBuffer);
            ReturnArray(_appendBuffer);
        }

        protected override bool SourceEqual(int oldSourceIndex, int newSourceIndex)
        {
            var oldSpan = _oldSpans[oldSourceIndex];
            var newSpan = _newSpans[newSourceIndex];

            if (oldSpan.Length != newSpan.Length)
            {
                return false;
            }

            var length = oldSpan.Length;

            // Simple case: Both lines are empty.
            if (length == 0)
            {
                return true;
            }

            // Copy the text into char arrays for comparison. Note: To avoid allocation,
            // we try to reuse the same char buffers and only grow them when a longer
            // line is encountered.
            var oldChars = EnsureBuffer(ref _oldBuffer, length);
            var newChars = EnsureBuffer(ref _newBuffer, length);

            OldText.CopyTo(oldSpan.Start, oldChars, 0, length);
            NewText.CopyTo(newSpan.Start, newChars, 0, length);

            for (var i = 0; i < length; i++)
            {
                if (oldChars[i] != newChars[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected override int GetEditPosition(DiffEdit edit)
            => _oldSpans[edit.Position].Start;

        protected override int AppendEdit(DiffEdit edit, StringBuilder builder)
        {
            if (edit.Kind == DiffEditKind.Insert)
            {
                Assumes.NotNull(edit.NewTextPosition);
                var newTextPosition = edit.NewTextPosition.GetValueOrDefault();

                for (var i = 0; i < edit.Length; i++)
                {
                    var newSpan = _newSpans[newTextPosition + i];

                    if (newSpan.Length > 0)
                    {
                        var buffer = EnsureBuffer(ref _appendBuffer, newSpan.Length);
                        NewText.CopyTo(newSpan.Start, buffer, 0, newSpan.Length);

                        builder.Append(buffer, 0, newSpan.Length);
                    }
                }

                return _oldSpans[edit.Position].Start;
            }

            return _oldSpans[edit.Position + edit.Length - 1].End;
        }
    }
}
