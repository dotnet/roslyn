// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    /// <summary>
    /// Abstraction over a contiguous chunk of <see cref="VirtualChar"/>s.  This
    /// is used so we can expose <see cref="VirtualChar"/>s over an <see cref="ImmutableArray{VirtualChar}"/>
    /// or over a <see cref="string"/>.  The latter is especially useful for reducing
    /// memory usage in common cases of string tokens without escapes.
    /// 
    /// Note: this type represents tha raw contiguous data for the entire string
    /// token contents.  Consumers should use <see cref="VirtualCharSequence"/> which 
    /// allows them to consume portions of this raw data without incurring heap
    /// allocations.
    /// </summary>
    internal abstract partial class LeafCharacters
    {
        protected LeafCharacters()
        {
        }

        public abstract int Length { get; }

        public abstract VirtualChar this[int index] { get; }

        public static LeafCharacters Create(ImmutableArray<VirtualChar> virtualChars)
            => new ImmutableArrayLeafCharacters(virtualChars);

        public static LeafCharacters Create(int firstVirtualCharPosition, string underlyingData, TextSpan underlyingDataSpan)
            => new StringLeafCharacters(firstVirtualCharPosition, underlyingData, underlyingDataSpan);

        public VirtualCharSequence GetFullSequence()
            => new VirtualCharSequence(this, new TextSpan(0, this.Length));
    }
}
