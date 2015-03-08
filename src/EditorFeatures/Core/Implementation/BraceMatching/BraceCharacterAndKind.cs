// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal struct BraceCharacterAndKind
    {
        public char Character { get; }
        public int Kind { get; }

        public BraceCharacterAndKind(char character, int kind)
            : this()
        {
            this.Character = character;
            this.Kind = kind;
        }
    }
}
