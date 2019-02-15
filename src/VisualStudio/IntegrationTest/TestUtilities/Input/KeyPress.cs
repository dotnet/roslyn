// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using WindowsInput.Native;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public struct KeyPress
    {
        public readonly char Character;
        public readonly VirtualKeyCode VirtualKey;
        public readonly ImmutableArray<VirtualKeyCode> Modifiers;

        public KeyPress(char ch)
            : this()
        {
            Character = ch;
        }

        public KeyPress(VirtualKeyCode virtualKey)
            : this(virtualKey, ImmutableArray<VirtualKeyCode>.Empty)
        {
        }

        public KeyPress(VirtualKeyCode virtualKey, VirtualKeyCode modifier)
            : this(virtualKey, ImmutableArray.Create(modifier))
        {
        }

        public KeyPress(VirtualKeyCode virtualKey, ImmutableArray<VirtualKeyCode> modifiers)
        {
            Character = '\0';
            VirtualKey = virtualKey;
            Modifiers = modifiers;
        }

        public bool IsTextEntry => Character != 0;
    }
}
