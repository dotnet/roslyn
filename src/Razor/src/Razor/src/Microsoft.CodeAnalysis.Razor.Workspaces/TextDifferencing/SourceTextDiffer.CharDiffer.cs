// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal partial class SourceTextDiffer
{
    private class CharDiffer : SourceTextDiffer
    {
        private readonly struct Buffer
        {
            public readonly char[] Array;
            public readonly int Start;
            public readonly int Length;

            public Buffer(char[] array, int start, int length)
                => (Array, Start, Length) = (array, start, length);

            public void Deconstruct(out char[] array, out int start, out int length)
                => (array, start, length) = (Array, Start, Length);

            public char this[int index]
                => Array[index - Start];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(int index)
                => index >= Start && index < Start + Length;
        }

        private const int BufferSize = 1024 * 16;

        protected override int OldSourceLength { get; }
        protected override int NewSourceLength { get; }

        private char[] _appendBuffer;
        private Buffer _oldBuffer;
        private Buffer _newBuffer;

        public CharDiffer(SourceText oldText, SourceText newText)
            : base(oldText, newText)
        {
            _appendBuffer = RentArray(BufferSize);

            _oldBuffer = new(RentArray(BufferSize), 0, BufferSize);
            OldText.CopyTo(0, _oldBuffer.Array, 0, Math.Min(OldText.Length, BufferSize));

            _newBuffer = new(RentArray(BufferSize), 0, BufferSize);
            NewText.CopyTo(0, _newBuffer.Array, 0, Math.Min(NewText.Length, BufferSize));

            OldSourceLength = oldText.Length;
            NewSourceLength = newText.Length;
        }

        public override void Dispose()
        {
            ReturnArray(_appendBuffer);
            ReturnArray(_oldBuffer.Array);
            ReturnArray(_newBuffer.Array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FillBuffer(ref Buffer buffer, SourceText text, int index)
        {
            // We slide our buffer so that index is in the middle. However, we have
            // have to be careful not extend past either the start or end of the SourceText.
            // Note that we always assume that we're filling the buffer with
            // BufferSize # of characters. If the SourceText is smaller than BufferSize,
            // this method shouldn't be called.

            Debug.Assert(text.Length >= BufferSize);

            var start = Math.Max(index - (BufferSize / 2), 0);

            if (start + BufferSize > text.Length)
            {
                start = text.Length - BufferSize;
            }

            text.CopyTo(start, buffer.Array, 0, BufferSize);
            buffer = new(buffer.Array, start, BufferSize);
        }

        protected override bool SourceEqual(int oldSourceIndex, int newSourceIndex)
        {
            ref var oldBuffer = ref _oldBuffer;
            ref var newBuffer = ref _newBuffer;

            if (!oldBuffer.Contains(oldSourceIndex))
            {
                FillBuffer(ref oldBuffer, OldText, oldSourceIndex);
            }

            if (!newBuffer.Contains(newSourceIndex))
            {
                FillBuffer(ref newBuffer, NewText, newSourceIndex);
            }

            return oldBuffer[oldSourceIndex] == newBuffer[newSourceIndex];
        }

        protected override int GetEditPosition(DiffEdit edit)
            => edit.Position;

        protected override int AppendEdit(DiffEdit edit, StringBuilder builder)
        {
            if (edit.Kind == DiffEditKind.Insert)
            {
                Assumes.NotNull(edit.NewTextPosition);
                var newTextPosition = edit.NewTextPosition.GetValueOrDefault();

                if (edit.Length > 1)
                {
                    var buffer = EnsureBuffer(ref _appendBuffer, edit.Length);
                    NewText.CopyTo(newTextPosition, buffer, 0, edit.Length);

                    builder.Append(buffer, 0, edit.Length);
                }
                else if (edit.Length == 1)
                {
                    builder.Append(NewText[newTextPosition]);
                }

                return edit.Position;
            }

            return edit.Position + edit.Length;
        }
    }
}
