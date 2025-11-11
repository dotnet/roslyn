// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis.CommandLine
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        internal Int32 cb;
        internal string? lpReserved;
        internal string? lpDesktop;
        internal string? lpTitle;
        internal Int32 dwX;
        internal Int32 dwY;
        internal Int32 dwXSize;
        internal Int32 dwYSize;
        internal Int32 dwXCountChars;
        internal Int32 dwYCountChars;
        internal Int32 dwFillAttribute;
        internal Int32 dwFlags;
        internal Int16 wShowWindow;
        internal Int16 cbReserved2;
        internal IntPtr lpReserved2;
        internal IntPtr hStdInput;
        internal IntPtr hStdOutput;
        internal IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    /// <summary>
    /// Interop methods.
    /// </summary>
#if NET
    [SupportedOSPlatform("windows")]
#endif
    internal static class NativeMethods
    {
        #region Constants

        internal static readonly IntPtr NullPtr = IntPtr.Zero;
        internal static readonly IntPtr InvalidIntPtr = new IntPtr(-1);

        internal const uint NORMAL_PRIORITY_CLASS = 0x0020;
        internal const uint CREATE_NO_WINDOW = 0x08000000;
        internal const Int32 STARTF_USESTDHANDLES = 0x00000100;
        internal const int ERROR_SUCCESS = 0;

        #endregion

        //------------------------------------------------------------------------------
        // CloseHandle
        //------------------------------------------------------------------------------
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        //------------------------------------------------------------------------------
        // CreateProcess
        //------------------------------------------------------------------------------
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess
        (
            string? lpApplicationName,
            [In, Out] StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [In, MarshalAs(UnmanagedType.Bool)]
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetCommandLine();

#if !NET
        //------------------------------------------------------------------------------
        // ResolveLinkTarget
        //------------------------------------------------------------------------------
        extension(File)
        {
            public static FileSystemInfo? ResolveLinkTarget(string path, bool returnFinalTarget)
            {
                return ResolveLinkTargetWin32(path, returnFinalTarget);
            }
        }
#endif

        /// <remarks>
        /// Unlike .NET Core's implementation of <c>File.ResolveLinkTarget</c>,
        /// this resolves virtual disk mappings (created via <c>subst</c>).
        /// </remarks>
        public static FileSystemInfo? ResolveLinkTargetWin32(string path, bool returnFinalTarget)
        {
            if (!returnFinalTarget) throw new NotSupportedException();

            using var handle = CreateFileW(
                lpFileName: path,
                dwDesiredAccess: FILE_READ_ATTRIBUTES,
                dwShareMode: FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                lpSecurityAttributes: IntPtr.Zero,
                dwCreationDisposition: OPEN_EXISTING,
                dwFlagsAndAttributes: FILE_FLAG_BACKUP_SEMANTICS, // needed for directories
                hTemplateFile: IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return null;
            }

            uint flags = FILE_NAME_NORMALIZED | VOLUME_NAME_DOS;
            uint needed = GetFinalPathNameByHandleW(hFile: handle, lpszFilePath: null, cchFilePath: 0, dwFlags: flags);
            if (needed == 0) return null;

            var sb = new StringBuilder((int)needed + 1);
            uint len = GetFinalPathNameByHandleW(hFile: handle, lpszFilePath: sb, cchFilePath: (uint)sb.Capacity, dwFlags: flags);
            if (len == 0) return null;

            return new FileInfo(TrimWin32ExtendedPrefix(sb.ToString()));
        }

        private static string TrimWin32ExtendedPrefix(string s)
        {
            if (s.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
                return @"\\" + s.Substring(8);
            if (s.StartsWith(@"\\?\", StringComparison.Ordinal))
                return s.Substring(4);
            return s;
        }

        // https://learn.microsoft.com/en-us/windows/win32/fileio/file-access-rights-constants
        private const uint FILE_READ_ATTRIBUTES = 0x0080;

        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_SHARE_DELETE = 0x00000004;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew
        private const uint VOLUME_NAME_DOS = 0x0;
        private const uint FILE_NAME_NORMALIZED = 0x0;

        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandleW(
            SafeFileHandle hFile,
            StringBuilder? lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

    }
}
