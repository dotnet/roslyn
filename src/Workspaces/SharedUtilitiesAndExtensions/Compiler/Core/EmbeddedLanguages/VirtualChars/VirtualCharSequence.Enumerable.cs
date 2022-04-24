// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal partial struct VirtualCharSequence
    {
        private class Enumerable : IEnumerable<VirtualChar>
        {
            private readonly VirtualCharSequence _sequence;

            public Enumerable(VirtualCharSequence sequence)
                => _sequence = sequence;

            public IEnumerator<VirtualChar> GetEnumerator()
                => _sequence.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => _sequence.GetEnumerator();
        }
    }
}
