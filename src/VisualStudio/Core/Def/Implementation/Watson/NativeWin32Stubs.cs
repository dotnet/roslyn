// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//////////////////////////////////////////////////////////////////////////////////////////////////////
// Note: This implementation is copied from vsproject\cps\components\implementations\NativeMethods.cs
//////////////////////////////////////////////////////////////////////////////////////////////////////

//-----------------------------------------------------------------------
// <copyright file="Watson.cs" company="Microsoft">
// </copyright>
// <summary>Native method calls.</summary>
//-----------------------------------------------------------------------

#if !SILVERLIGHT
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32.SafeHandles;
using DWORD = System.UInt32;
using WORD = System.UInt16;
using LPTSTR = System.String;
using LPBYTE = System.IntPtr;
using HANDLE = System.IntPtr;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Watson
{
    internal static unsafe class NativeWin32Stubs
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(HANDLE hObject);

        [DllImport("kernel32.dll")]
        internal static extern HANDLE CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        internal const DWORD WAIT_OBJECT_0 = 0;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern DWORD WaitForMultipleObjects(DWORD nCount, IntPtr[] lpHandles, bool bWaitAll, DWORD dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern HANDLE GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(HANDLE hSourceProcessHandle, HANDLE hSourceHandle, HANDLE hTargetProcessHandle, out HANDLE lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            internal int nLength;
            internal HANDLE lpSecurityDescriptor;
            internal bool bInheritHandle;
        }

        internal enum DESIRED_ACCESS
        {
            DUPLICATE_CLOSE_SOURCE = 0x01,
            DUPLICATE_SAME_ACCESS = 0x02
        }
    }
}

#endif
