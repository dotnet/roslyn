// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal readonly struct BraceCharacterAndKind
    {
        public char Character { get; }
        public int Kind { get; }

        public BraceCharacterAndKind(char character, int kind)
        {
            this.Character = character;
            this.Kind = kind;
        }
    }
}
