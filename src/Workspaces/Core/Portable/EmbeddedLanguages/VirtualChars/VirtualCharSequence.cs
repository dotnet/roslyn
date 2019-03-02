// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        public static readonly VirtualCharSequence Empty = Create("");

        public abstract int Length { get; }

        public abstract VirtualChar this[int index] { get; }

        public abstract string CreateString();

        public VirtualCharSequence Concat(VirtualCharSequence other)
            => new ConcatVirtualCharSequence(this, other);

        public VirtualCharSequence GetSubSequence(TextSpan span)
            => new SubSequenceVirtualCharSequence(this, span);

        public static VirtualCharSequence Create(ImmutableArray<VirtualChar> virtualChars)
            => new ImmutableArrayVirtualCharSequence(virtualChars);

        public static VirtualCharSequence Create(string text, int position)
            => new StringVirtualCharSequence(text, position);

        public static VirtualCharSequence Create(VirtualChar ch)
            => new SingleVirtualCharSequence(ch);

        public Enumerator GetEnumerator()
            => new Enumerator(this);
    }
}
