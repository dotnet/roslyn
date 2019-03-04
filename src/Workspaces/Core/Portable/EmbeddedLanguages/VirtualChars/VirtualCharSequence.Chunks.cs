// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal partial struct VirtualCharSequence
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
        private abstract partial class Chunk
        {
            protected Chunk()
            {
            }

            public abstract int Length { get; }

            public abstract VirtualChar this[int index] { get; }

            public VirtualCharSequence GetFullSequence()
                => new VirtualCharSequence(this, new TextSpan(0, this.Length));
        }

        /// <summary>
        /// Thin wrapper over an actual <see cref="ImmutableArray{VirtualChar}"/>.
        /// This will be the common construct we generate when getting the
        /// <see cref="Chunk"/> for a string token that has escapes in it.
        /// </summary>
        private class ImmutableArrayChunk : Chunk
        {
            private readonly ImmutableArray<VirtualChar> _array;

            public ImmutableArrayChunk(ImmutableArray<VirtualChar> array)
                => _array = array;

            public override int Length => _array.Length;
            public override VirtualChar this[int index] => _array[index];
        }

        /// <summary>
        /// Represents a <see cref="Chunk"/> on top of a normal
        /// string.  This is the common case of the type of the sequence we would
        /// create for a normal string token without any escapes in it.
        /// </summary>
        private class StringChunk : Chunk
        {
            private readonly int _firstVirtualCharPosition;

            /// <summary>
            /// The underlying string that we're returning virtual chars from.
            /// Note the chars we return will normally be from a subsection of this string.
            /// i.e. the _underlyingData will be something like:  "abc" (including the quotes).
            /// The <see cref="_underlyingDataSpan"/> will snip out the quotes, leaving just
            /// "abc"
            /// </summary>
            private readonly string _underlyingData;

            /// <summary>
            /// The subsection of <see cref="_underlyingData"/> that we're producing virtual chars from.
            /// </summary>
            private readonly TextSpan _underlyingDataSpan;

            public StringChunk(int firstVirtualCharPosition, string data, TextSpan dataSpan)
            {
                _firstVirtualCharPosition = firstVirtualCharPosition;
                _underlyingData = data;
                _underlyingDataSpan = dataSpan;
            }

            public override int Length => _underlyingDataSpan.Length;

            public override VirtualChar this[int index]
                => new VirtualChar(
                    _underlyingData[_underlyingDataSpan.Start + index],
                    new TextSpan(_firstVirtualCharPosition + index, length: 1));
        }
    }
}
