// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using WindowsInput;
using WindowsInput.Native;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal struct InputKey
    {
        public readonly ImmutableArray<VirtualKeyCode> Modifiers;
        public readonly VirtualKeyCode VirtualKeyCode;
        public readonly char? Character;
        public readonly string? Text;

        public InputKey(VirtualKeyCode virtualKeyCode, ImmutableArray<VirtualKeyCode> modifiers)
        {
            Modifiers = modifiers;
            VirtualKeyCode = virtualKeyCode;
            Character = null;
            Text = null;
        }

        public InputKey(char character)
        {
            Modifiers = ImmutableArray<VirtualKeyCode>.Empty;
            VirtualKeyCode = 0;
            Character = character;
            Text = null;
        }

        public InputKey(string text)
        {
            Modifiers = ImmutableArray<VirtualKeyCode>.Empty;
            VirtualKeyCode = 0;
            Character = null;
            Text = text;
        }

        public static implicit operator InputKey(char character)
            => new(character);

        public static implicit operator InputKey(string text)
            => new(text);

        public static implicit operator InputKey(VirtualKeyCode virtualKeyCode)
            => new(virtualKeyCode, ImmutableArray<VirtualKeyCode>.Empty);

        public static implicit operator InputKey((VirtualKeyCode virtualKeyCode, VirtualKeyCode modifier) modifiedKey)
            => new(modifiedKey.virtualKeyCode, ImmutableArray.Create(modifiedKey.modifier));

        public void Apply(IInputSimulator simulator)
        {
            if (Character is { } c)
            {
                simulator.Keyboard.TextEntry(c);
                return;
            }
            else if (Text is not null)
            {
                simulator.Keyboard.TextEntry(Text);
                return;
            }

            if (Modifiers.IsEmpty)
            {
                simulator.Keyboard.KeyPress(VirtualKeyCode);
            }
            else
            {
                simulator.Keyboard.ModifiedKeyStroke(Modifiers, VirtualKeyCode);
            }
        }
    }
}
