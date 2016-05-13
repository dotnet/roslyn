// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Roslyn.VisualStudio.Test.Utilities.Interop
{
    internal static class User32
    {
        public static readonly int SizeOf_INPUT = Marshal.SizeOf<INPUT>();

        public const uint GA_PARENT = 1;
        public const uint GA_ROOT = 2;
        public const uint GA_ROOTOWNER = 3;

        public const uint GW_HWNDFIRST = 0;
        public const uint GW_HWNDLAST = 1;
        public const uint GW_HWNDNEXT = 2;
        public const uint GW_HWNDPREV = 3;
        public const uint GW_OWNER = 4;
        public const uint GW_CHILD = 5;
        public const uint GW_ENABLEDPOPUP = 6;

        public const int HWND_NOTOPMOST = -2;
        public const int HWND_TOPMOST = -1;
        public const int HWND_TOP = 0;
        public const int HWND_BOTTOM = 1;

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const uint INPUT_HARDWARE = 2;

        public const uint KEYEVENTF_NONE = 0x0000;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_SCANCODE = 0x0008;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_DRAWFRAME = 0x0020;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_NOCOPYBITS = 0x0100;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const uint SWP_NOREPOSITION = 0x0200;
        public const uint SWP_NOSENDCHANGING = 0x0400;
        public const uint SWP_DEFERERASE = 0x2000;
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;

        public const ushort VK_SHIFT = 0x0010;
        public const ushort VK_CONTROL = 0x0011;
        public const ushort VK_MENU = 0x0012;

        public const ushort VK_PRIOR = 0x0021;
        public const ushort VK_NEXT = 0x0022;
        public const ushort VK_END = 0x0023;
        public const ushort VK_HOME = 0x0024;
        public const ushort VK_LEFT = 0x0025;
        public const ushort VK_UP = 0x0026;
        public const ushort VK_RIGHT = 0x0027;
        public const ushort VK_DOWN = 0x0028;

        public const ushort VK_INSERT = 0x002D;
        public const ushort VK_DELETE = 0x002E;

        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 8)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public uint Type;

            [FieldOffset(4)]
            public MOUSEINPUT mi;

            [FieldOffset(4)]
            public KEYBDINPUT ki;

            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool WNDENUMPROC(
            [In] IntPtr hWnd,
            [In] IntPtr lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "AttachThreadInput", PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(
            [In] uint idAttach,
            [In] uint idAttachTo,
            [In, MarshalAs(UnmanagedType.Bool)] bool fAttach
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "BlockInput", PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput(
            [In, MarshalAs(UnmanagedType.Bool)] bool fBlockIt
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "EnumWindows", PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(
            [In, MarshalAs(UnmanagedType.FunctionPtr)] WNDENUMPROC lpEnumFunc,
            [In] IntPtr lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetAncestor", PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetAncestor(
            [In] IntPtr hWnd,
            [In] uint gaFlags
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetForegroundWindow", PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetForegroundWindow(
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetParent", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr GetParent(
            [In] IntPtr hWnd
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetWindow", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr GetWindow(
            [In] IntPtr hWnd,
            [In] uint uCmd
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetWindowThreadProcessId", PreserveSig = true, SetLastError = false)]
        public static extern uint GetWindowThreadProcessId(
            [In] IntPtr hWnd,
            [Out, Optional] IntPtr lpdwProcessId
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetWindowThreadProcessId", PreserveSig = true, SetLastError = false)]
        public static extern uint GetWindowThreadProcessId(
            [In] IntPtr hWnd,
            [Out, Optional] out uint lpdwProcessId
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SendInput", PreserveSig = true, SetLastError = true)]
        public static extern uint SendInput(
            [In] uint nInputs,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] INPUT[] pInputs,
            [In] int cbSize
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SendMessageW", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SendMessage(
            [In] IntPtr hWnd,
            [In] uint uMsg,
            [In] IntPtr wParam,
            [In] IntPtr lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SendMessageW", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SendMessage(
            [In] IntPtr hWnd,
            [In] uint uMsg,
            [In] IntPtr wParam,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SetActiveWindow", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SetActiveWindow(
            [In] IntPtr hWnd
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SetFocus", PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SetFocus(
            [In] IntPtr hWnd
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SetForegroundWindow", PreserveSig = true, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(
            [In] IntPtr hWnd
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SetWindowPos", PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            [In] IntPtr hWnd,
            [In, Optional] IntPtr hWndInsertAfter,
            [In] int X,
            [In] int Y,
            [In] int cx,
            [In] int cy,
            [In] uint uFlags
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "VkKeyScanW", PreserveSig = true, SetLastError = false)]
        public static extern short VkKeyScan(
            [In] char ch
        );
    }
}
