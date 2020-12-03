// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal partial struct VirtualCharSequence
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
            /// The underlying string that we're returning virtual chars from.  Note:
            /// this will commonly include things like quote characters.  Clients who
            /// do not want that should then ask for an appropriate <see cref="VirtualCharSequence.GetSubSequence"/>
            /// back that does not include those characters.
            /// </summary>
            private readonly string _underlyingData;

            public StringChunk(int firstVirtualCharPosition, string data)
            {
                _firstVirtualCharPosition = firstVirtualCharPosition;
                _underlyingData = data;
            }

            public override int Length => _underlyingData.Length;

            public override VirtualChar this[int index]
            {
                get
                {
#if DEBUG
                    // We should never have a property paired high/low surrogate in a StringChunk. We are only created
                    // when the string has the same number of chars as there are VirtualChars.
                    if (char.IsHighSurrogate(_underlyingData[index]))
                    {
                        Debug.Assert(index + 1 >= _underlyingData.Length ||
                                     !char.IsLowSurrogate(_underlyingData[index + 1]));
                    }
#endif

                    var span = new TextSpan(_firstVirtualCharPosition + index, length: 1);
                    var ch = _underlyingData[index];
                    return char.IsSurrogate(ch)
                        ? VirtualChar.Create(ch, span)
                        : VirtualChar.Create(new Rune(ch), span);
                }
            }
        }
    }
}
