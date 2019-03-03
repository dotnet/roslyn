// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal class SubSequenceVirtualCharSequence : VirtualCharSequence
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

        public override int Length => Span.Length;

        public override VirtualChar this[int index] => UnderlyingSequence[Span.Start + index];

        protected override string CreateStringWorker()
            => UnderlyingSequence.CreateString().Substring(Span.Start, Span.Length);
    }
}
