// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    /// <summary>
    /// Represents the individual characters that raw string token represents (i.e. with escapes collapsed).  
    /// The difference between this and the result from token.ValueText is that for each collapsed character
    /// returned the original span of text in the original token can be found.  i.e. if you had the
    /// following in C#:
    ///
    /// "G\u006fo"
    ///
    /// Then you'd get back:
    ///
    /// 'G' -> [0, 1) 'o' -> [1, 7) 'o' -> [7, 1)
    ///
    /// This allows for embedded language processing that can refer back to the user's original code
    /// instead of the escaped value we're processing.
    /// </summary>
    internal partial struct VirtualCharSequence
    {
        public static readonly VirtualCharSequence Empty = Create(ImmutableSegmentedList<VirtualChar>.Empty);

        public static VirtualCharSequence Create(ImmutableSegmentedList<VirtualChar> virtualChars)
            => new(new ImmutableSegmentedListChunk(virtualChars));

        public static VirtualCharSequence Create(int firstVirtualCharPosition, string underlyingData)
            => new(new StringChunk(firstVirtualCharPosition, underlyingData));

        /// <summary>
        /// The actual characters that this <see cref="VirtualCharSequence"/> is a portion of.
        /// </summary>
        private readonly Chunk _leafCharacters;

        /// <summary>
        /// The portion of <see cref="_leafCharacters"/> that is being exposed.  This span 
        /// is `[inclusive, exclusive)`.
        /// </summary>
        private readonly TextSpan _span;

        private VirtualCharSequence(Chunk sequence) : this(sequence, new TextSpan(0, sequence.Length))
        {
        }

        private VirtualCharSequence(Chunk sequence, TextSpan span)
        {
            if (span.Start > sequence.Length)
            {
                throw new ArgumentException();
            }

            if (span.End > sequence.Length)
            {
                throw new ArgumentException();
            }

            _leafCharacters = sequence;
            _span = span;
        }

        public int Length => _span.Length;
        public VirtualChar this[int index] => _leafCharacters[_span.Start + index];

        public bool IsDefault => _leafCharacters == null;
        public bool IsEmpty => Length == 0;
        public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

        public VirtualCharSequence GetSubSequence(TextSpan span)
           => new(_leafCharacters, new TextSpan(_span.Start + span.Start, span.Length));

        public VirtualChar First() => this[0];
        public VirtualChar Last() => this[^1];

        public Enumerator GetEnumerator()
            => new(this);

        /// <summary>
        /// Finds the virtual char in this sequence that contains the position.  Will return null if this position is not
        /// in the span of this sequence.
        /// </summary>
        public VirtualChar? Find(int position)
            => _leafCharacters?.Find(position);

        public bool Contains(VirtualChar @char)
            => IndexOf(@char) >= 0;

        public int IndexOf(VirtualChar @char)
        {
            var index = 0;
            foreach (var ch in this)
            {
                if (ch == @char)
                    return index;

                index++;
            }

            return -1;
        }

        public bool Any(Func<VirtualChar, bool> predicate)
        {
            foreach (var ch in this)
            {
                if (predicate(ch))
                    return true;
            }

            return false;
        }

        public bool All(Func<VirtualChar, bool> predicate)
        {
            foreach (var ch in this)
            {
                if (!predicate(ch))
                    return false;
            }

            return true;
        }

        public string CreateString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            foreach (var ch in this)
                ch.AppendTo(builder);

            return builder.ToString();
        }

        [Conditional("DEBUG")]
        public void AssertAdjacentTo(VirtualCharSequence virtualChars)
        {
            Debug.Assert(_leafCharacters == virtualChars._leafCharacters);
            Debug.Assert(_span.End == virtualChars._span.Start);
        }

        /// <summary>
        /// Combines two <see cref="VirtualCharSequence"/>s, producing a final
        /// sequence that points at the same underlying data, but spans from the 
        /// start of <paramref name="chars1"/> to the end of <paramref name="chars2"/>.
        /// </summary>  
        public static VirtualCharSequence FromBounds(
            VirtualCharSequence chars1, VirtualCharSequence chars2)
        {
            Debug.Assert(chars1._leafCharacters == chars2._leafCharacters);
            return new VirtualCharSequence(
                chars1._leafCharacters,
                TextSpan.FromBounds(chars1._span.Start, chars2._span.End));
        }
    }
}
