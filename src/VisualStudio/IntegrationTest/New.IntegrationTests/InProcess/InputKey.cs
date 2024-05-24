// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using WindowsInput;
using WindowsInput.Native;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

internal readonly struct InputKey
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
            if (c == '\n')
                simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            else if (c == '\t')
                simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            else
                simulator.Keyboard.TextEntry(c);

            return;
        }
        else if (Text is not null)
        {
            var offset = 0;
            while (offset < Text.Length)
            {
                if (Text[offset] == '\r' && offset < Text.Length - 1 && Text[offset + 1] == '\n')
                {
                    // Treat \r\n as a single RETURN character
                    offset++;
                    continue;
                }
                else if (Text[offset] == '\n')
                {
                    simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    offset++;
                    continue;
                }
                else if (Text[offset] == '\t')
                {
                    simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    offset++;
                    continue;
                }
                else
                {
                    var nextSpecial = Text.IndexOfAny(['\r', '\n', '\t'], offset);
                    var endOfCurrentSegment = nextSpecial < 0 ? Text.Length : nextSpecial;
                    simulator.Keyboard.TextEntry(Text[offset..endOfCurrentSegment]);
                    offset = endOfCurrentSegment;
                }
            }

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
