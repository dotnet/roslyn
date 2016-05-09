// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Roslyn.VisualStudio.Test.Utilities.Interop
{
    internal static class User32
    {
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

        public const uint WM_GETTEXT = 0x000D;
        public const uint WM_GETTEXTLENGTH = 0x000E;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool WNDENUMPROC(
            [In] IntPtr hWnd,
            [In] IntPtr lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "EnumWindows", PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(
            [In] WNDENUMPROC lpEnumFunc,
            [In] IntPtr lParam
        );

        [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "GetAncestor", PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetAncestor(
            [In] IntPtr hWnd,
            [In] uint gaFlags
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
    }
}
