// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
    /// This allows for embedded language processing that can refer back to the users' original code
    /// instead of the escaped value we're processing.
    /// </summary>
    internal abstract partial class VirtualCharSequence
    {
        public static readonly SubSequenceVirtualCharSequence Empty
            = Create(ImmutableArray<VirtualChar>.Empty).GetSubSequence(default);

        private string _string;

        protected VirtualCharSequence()
        {
        }

        public abstract int Length { get; }

        public abstract VirtualChar this[int index] { get; }

        protected abstract string CreateStringWorker();

        public string CreateString()
        {
            if (_string == null)
            {
                _string = CreateStringWorker();
            }

            return _string;
        }
        //public VirtualCharSequence Concat(VirtualCharSequence other)
        //    => new ConcatVirtualCharSequence(this, other);

        public static LeafVirtualCharSequence Create(ImmutableArray<VirtualChar> virtualChars)
            => new ImmutableArrayVirtualCharSequence(virtualChars);

        public static LeafVirtualCharSequence Create(int firstVirtualCharPosition, string underlyingData, TextSpan underlyingDataSpan)
            => new StringVirtualCharSequence(firstVirtualCharPosition, underlyingData, underlyingDataSpan);

        //public static VirtualCharSequence Create(VirtualChar ch)
        //    => new SingleVirtualCharSequence(ch);

        public Enumerator GetEnumerator()
            => new Enumerator(this);

        public bool IsEmpty => Length == 0;

        public VirtualChar First() => this[0];

        public VirtualChar? FirstOrNullable(Func<VirtualChar, bool> predicate)
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

        public VirtualChar Last()
            => this[this.Length - 1];

        public bool Contains(VirtualChar @char)
            => IndexOf(@char) >= 0;

        public int IndexOf(VirtualChar @char)
        {
            int index = 0;
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
    }
}
