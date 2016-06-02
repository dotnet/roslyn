// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.Test.Utilities.Input
{
    public enum VirtualKey : byte
    {
        Enter = 0x0D,
        Tab = 0x09,
        Escape = 0x1B,

        PageUp = 0x21,
        PageDown = 0x22,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,

        Shift = 0x10,
        Control = 0x11,
        Alt = 0x12,

        CapsLock = 0x14,
        NumLock = 0x90,
        ScrollLock = 0x91,
        PrintScreen = 0x2C,
        Break = 0x03,
        Help = 0x2F,

        Backspace = 0x08,
        Clear = 0x0C,
        Insert = 0x2D,
        Delete = 0x2E,

        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        F13 = 0x7C,
        F14 = 0x7D,
        F15 = 0x7E,
        F16 = 0x7F
    }
}
