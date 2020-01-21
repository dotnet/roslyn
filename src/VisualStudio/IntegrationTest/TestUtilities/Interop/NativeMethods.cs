// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop
{
    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";
        private const string Ole32 = "ole32.dll";
        private const string User32 = "User32.dll";

        #region kernel32.dll

        [DllImport(Kernel32)]
        public static extern uint GetCurrentThreadId();

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport(Kernel32, SetLastError = false)]
        public static extern IntPtr GetConsoleWindow();

        #endregion

        #region ole32.dll

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

        #endregion

        #region user32.dll

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

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const uint INPUT_HARDWARE = 2;

        public const uint KEYEVENTF_NONE = 0x0000;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_SCANCODE = 0x0008;

        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint Type;
            public InputUnion Input;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
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
        public static extern bool EnumWindows([MarshalAs(UnmanagedType.FunctionPtr)] WNDENUMPROC lpEnumFunc, IntPtr lParam);

        [DllImport(User32)]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport(User32)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport(User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport(User32)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

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

        [DllImport(User32, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        public const uint SWP_NOZORDER = 4;

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern void SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool fUnknown);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern short VkKeyScan(char ch);

        public const uint MAPVK_VK_TO_VSC = 0;
        public const uint MAPVK_VSC_TO_VK = 1;
        public const uint MAPVK_VK_TO_CHAR = 2;
        public const uint MAPVK_VSC_TO_KV_EX = 3;

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport(User32)]
        public static extern IntPtr GetMessageExtraInfo();

        #endregion
    }
}
