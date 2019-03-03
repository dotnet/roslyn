// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal partial struct SubSequenceVirtualCharSequence
    {
        public struct Enumerator
        {
            private readonly SubSequenceVirtualCharSequence _virtualCharSequence;
            private int _position;

            public Enumerator(SubSequenceVirtualCharSequence virtualCharSequence)
            {
                _virtualCharSequence = virtualCharSequence;
                _position = -1;
            }

            public bool MoveNext()
            {
                _position++;
                return _position < _virtualCharSequence.Length;
            }

            public VirtualChar Current
                => _virtualCharSequence[_position];
        }
    }
}
