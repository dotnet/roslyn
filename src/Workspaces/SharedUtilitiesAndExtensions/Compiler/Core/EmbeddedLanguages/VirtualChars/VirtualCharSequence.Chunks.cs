// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

internal readonly partial struct VirtualCharSequence
{
    /// <summary>
    /// Abstraction over a contiguous chunk of <see cref="VirtualChar"/>s.  This
    /// is used so we can expose <see cref="VirtualChar"/>s over an <see cref="ImmutableArray{VirtualChar}"/>
    /// or over a <see cref="string"/>.  The latter is especially useful for reducing
    /// memory usage in common cases of string tokens without escapes.
    /// </summary>
    private abstract partial class Chunk
    {
        protected Chunk()
        {
        }

        public abstract int Length { get; }
        public abstract VirtualChar this[int index] { get; }
        public abstract VirtualChar? Find(int position);
    }

    /// <summary>
    /// Thin wrapper over an actual <see cref="ImmutableSegmentedList{T}"/>.
    /// This will be the common construct we generate when getting the
    /// <see cref="Chunk"/> for a string token that has escapes in it.
    /// </summary>
    private class ImmutableSegmentedListChunk(ImmutableSegmentedList<VirtualChar> array) : Chunk
    {
        public override int Length => array.Count;
        public override VirtualChar this[int index] => array[index];

        public override VirtualChar? Find(int position)
        {
            if (array.IsEmpty)
                return null;

            if (position < array[0].Span.Start || position >= array[^1].Span.End)
                return null;

            var index = array.BinarySearch(position, static (ch, position) =>
            {
                if (position < ch.Span.Start)
                    return 1;

                if (position >= ch.Span.End)
                    return -1;

                return 0;
            });

            // Characters can be discontiguous (for example, in multi-line-raw-string literals).  So if the
            // position is in one of the gaps, it won't be able to find a corresponding virtual char.
            if (index < 0)
                return null;

            return array[index];
        }
    }

    /// <summary>
    /// Represents a <see cref="Chunk"/> on top of a normal
    /// string.  This is the common case of the type of the sequence we would
    /// create for a normal string token without any escapes in it.
    /// </summary>
    /// <param name="data">
    /// The underlying string that we're returning virtual chars from.  Note:
    /// this will commonly include things like quote characters.  Clients who
    /// do not want that should then ask for an appropriate <see cref="VirtualCharSequence.GetSubSequence"/>
    /// back that does not include those characters.
    /// </param>
    private class StringChunk(int firstVirtualCharPosition, string data) : Chunk
    {
        public override int Length => data.Length;

        public override VirtualChar? Find(int position)
        {
            var stringIndex = position - firstVirtualCharPosition;
            if (stringIndex < 0 || stringIndex >= data.Length)
                return null;

            return this[stringIndex];
        }

        public override VirtualChar this[int index]
        {
            get
            {
#if DEBUG
                // We should never have a properly paired high/low surrogate in a StringChunk. We are only created
                // when the string has the same number of chars as there are VirtualChars.
                if (char.IsHighSurrogate(data[index]))
                {
                    Debug.Assert(index + 1 >= data.Length ||
                                 !char.IsLowSurrogate(data[index + 1]));
                }
#endif

                var span = new TextSpan(firstVirtualCharPosition + index, length: 1);
                var ch = data[index];
                return char.IsSurrogate(ch)
                    ? VirtualChar.Create(ch, span)
                    : VirtualChar.Create(new Rune(ch), span);
            }
        }
    }
}
