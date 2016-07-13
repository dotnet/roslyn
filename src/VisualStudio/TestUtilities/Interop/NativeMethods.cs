// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.Interop
{
    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";
        private const string Ole32 = "ole32.dll";
        private const string User32 = "User32.dll";

        [DllImport(Kernel32)]
        public static extern uint GetCurrentThreadId();

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

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
        public delegate bool WNDENUMPROC(IntPtr hWnd, IntPtr lParam);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput([MarshalAs(UnmanagedType.Bool)] bool fBlockIt);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows([MarshalAs(UnmanagedType.FunctionPtr)] WNDENUMPROC lpEnumFunc, IntPtr lParam);

        [DllImport(User32)]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport(User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport(User32)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, [Optional] IntPtr lpdwProcessId);

        [DllImport(User32)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, [Optional] out uint lpdwProcessId);

        [DllImport(User32, SetLastError = true)]
        public static extern uint SendInput(uint nInputs, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] INPUT[] pInputs, int cbSize);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint uMsg, IntPtr wParam, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport(User32, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, [Optional] IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern short VkKeyScan(char ch);

        public const uint MAPVK_VK_TO_VSC = 0;
        public const uint MAPVK_VSC_TO_VK = 1;
        public const uint MAPVK_VK_TO_CHAR = 2;
        public const uint MAPVK_VSC_TO_KV_EX = 3;

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
