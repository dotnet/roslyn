// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

internal partial struct VirtualCharGreenSequence
{
    public struct Enumerator(VirtualCharGreenSequence virtualCharSequence) : IEnumerator<VirtualCharGreen>
    {
        private int _position = -1;

        public bool MoveNext() => ++_position < virtualCharSequence.Length;
        public readonly VirtualCharGreen Current => virtualCharSequence[_position];

        public void Reset()
            => _position = -1;

        readonly object? IEnumerator.Current => this.Current;
        public readonly void Dispose() { }
    }
}
