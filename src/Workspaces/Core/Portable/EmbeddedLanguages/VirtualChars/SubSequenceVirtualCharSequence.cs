// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    /// <summary>
    /// Represents a subsequence of some other sequence.  Useful for cases
    /// like the regex lexer which might consume snip out part of the full
    /// seqeunce of characters in the string token.  This allows for a single
    /// alloc to represent that, instead of needing to copy all the chars
    /// over.
    /// </summary>
    internal partial struct SubSequenceVirtualCharSequence
    {
        public readonly LeafVirtualCharSequence UnderlyingSequence;
        public readonly TextSpan Span;

        public SubSequenceVirtualCharSequence(LeafVirtualCharSequence sequence, TextSpan span)
        {
            if (span.Start > sequence.Length)
            {
                throw new ArgumentException();
            }

            if (span.End > sequence.Length)
            {
                throw new ArgumentException();
            }

            UnderlyingSequence = sequence;
            Span = span;
        }

        public int Length => Span.Length;

        public VirtualChar this[int index] => UnderlyingSequence[Span.Start + index];

        //protected override string CreateStringWorker()
        //    => UnderlyingSequence.CreateString().Substring(Span.Start, Span.Length);

        public bool IsEmpty => Length == 0;

        public VirtualChar First() => this[0];

        public Enumerator GetEnumerator()
            => new Enumerator(this);

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

        public string CreateString()
        {
            var builder = PooledStringBuilder.GetInstance();
            foreach (var ch in this)
            {
                builder.Builder.Append(ch.Char);
            }

            return builder.ToStringAndFree();
        }
    }
}
