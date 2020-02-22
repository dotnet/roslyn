// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Editor.Wpf.Utilities
{
    internal static class NativeMethods
    {
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        public const int WM_SYSCOLORCHANGE = 0x0015;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
    }
}
