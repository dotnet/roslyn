// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal partial struct VirtualCharSequence
    {
        public struct Enumerator
        {
            private readonly VirtualCharSequence _virtualCharSequence;
            private int _position;

            public Enumerator(VirtualCharSequence virtualCharSequence)
            {
                _virtualCharSequence = virtualCharSequence;
                _position = -1;
            }

            public bool MoveNext() => ++_position < _virtualCharSequence.Length;
            public VirtualChar Current => _virtualCharSequence[_position];
        }
    }
}
