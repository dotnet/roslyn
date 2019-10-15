// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        public static readonly VirtualCharSequence Empty = Create(ImmutableArray<VirtualChar>.Empty);

        public static VirtualCharSequence Create(ImmutableArray<VirtualChar> virtualChars)
            => new VirtualCharSequence(new ImmutableArrayChunk(virtualChars));

        public static VirtualCharSequence Create(int firstVirtualCharPosition, string underlyingData)
            => new VirtualCharSequence(new StringChunk(firstVirtualCharPosition, underlyingData));

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
           => new VirtualCharSequence(
               _leafCharacters, new TextSpan(_span.Start + span.Start, span.Length));

        public VirtualChar First() => this[0];
        public VirtualChar Last() => this[this.Length - 1];

        public Enumerator GetEnumerator()
            => new Enumerator(this);

        public VirtualChar? FirstOrNull(Func<VirtualChar, bool> predicate)
        {
            foreach (var ch in this)
            {
                if (predicate(ch))
                {
                    return ch;
                }
            }

            return null;
        }

        public bool Contains(VirtualChar @char)
            => IndexOf(@char) >= 0;

        public int IndexOf(VirtualChar @char)
        {
            var index = 0;
            foreach (var ch in this)
            {
                if (ch == @char)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public string CreateString()
        {
            var builder = PooledStringBuilder.GetInstance();
            foreach (var ch in this)
            {
                builder.Builder.Append(ch.Char);
            }

            return builder.ToStringAndFree();
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
