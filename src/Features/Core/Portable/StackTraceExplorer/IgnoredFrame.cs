// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal sealed class IgnoredFrame : ParsedFrame
    {
        private readonly VirtualCharSequence _sequence;

        public IgnoredFrame(VirtualCharSequence sequence)
        {
            _sequence = sequence;
        }

        public override string ToString()
            => _sequence.CreateString();
    }
}
