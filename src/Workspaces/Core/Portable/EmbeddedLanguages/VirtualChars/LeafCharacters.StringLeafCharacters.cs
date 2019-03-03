// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class LeafCharacters
    {
        /// <summary>
        /// Represents a <see cref="LeafCharacters"/> on top of a normal
        /// string.  This is the common case of the type of the sequence we would
        /// create for a normal string token without any escapes in it.
        /// </summary>
        private class StringLeafCharacters : LeafCharacters
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

            public StringLeafCharacters(int firstVirtualCharPosition, string data, TextSpan dataSpan)
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
