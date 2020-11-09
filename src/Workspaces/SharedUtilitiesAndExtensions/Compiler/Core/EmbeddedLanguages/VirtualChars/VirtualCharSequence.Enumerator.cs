// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
