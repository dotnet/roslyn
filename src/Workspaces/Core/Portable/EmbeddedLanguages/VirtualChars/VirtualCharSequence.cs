// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        public static readonly VirtualCharSequence Empty
            = Create(ImmutableArray<VirtualChar>.Empty);

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
        public VirtualCharSequence Concat(VirtualCharSequence other)
            => new ConcatVirtualCharSequence(this, other);

        public VirtualCharSequence GetSubSequence(TextSpan span)
            => new SubSequenceVirtualCharSequence(this, span);

        public static VirtualCharSequence Create(ImmutableArray<VirtualChar> virtualChars)
            => new ImmutableArrayVirtualCharSequence(virtualChars);

        public static VirtualCharSequence Create(int firstVirtualCharPosition, string underlyingData, TextSpan underlyingDataSpan)
            => new StringVirtualCharSequence(firstVirtualCharPosition, underlyingData, underlyingDataSpan);

        public static VirtualCharSequence Create(VirtualChar ch)
            => new SingleVirtualCharSequence(ch);

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
