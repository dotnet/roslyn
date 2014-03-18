// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using IO = System.IO;

namespace Roslyn.Utilities
{
    internal static class Kernel32File
    {
        internal static IO.FileStream Open(string path, IO.FileAccess access, IO.FileMode mode, IO.FileShare share = IO.FileShare.None, bool throwException = true)
        {
            var fileHandle = CreateFile(path, GetFileAccess(access), GetFileShare(share), default(IntPtr), GetFileMode(mode));

            if (fileHandle.IsInvalid)
            {
                if (throwException)
                {
                    HandleCOMError(Marshal.GetLastWin32Error());
                }
                else
                {
                    return null;
                }
            }

            return new IO.FileStream(fileHandle, access);
        }

        [Flags]
        private enum FileShare
        {
            FILE_SHARE_NONE = 0x00,
            FILE_SHARE_READ = 0x01,
            FILE_SHARE_WRITE = 0x02,
            FILE_SHARE_DELETE = 0x04
        }

        private enum FileMode
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        private enum FileAccess
        {
            GENERIC_READ = unchecked((int)0x80000000),
            GENERIC_WRITE = 0x40000000
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string filename,
                                       FileAccess desiredAccess,
                                       FileShare shareMode,
                                       IntPtr attributes,
                                       FileMode creationDisposition,
                                       uint flagsAndAttributes = 0,
                                       IntPtr templateFile = default(IntPtr));

        private static void HandleCOMError(int error)
        {
            throw new System.ComponentModel.Win32Exception(error);
        }

        private static FileMode GetFileMode(IO.FileMode mode)
        {
            if (mode != IO.FileMode.Append)
            {
                return (FileMode)(int)mode;
            }
            else
            {
                return (FileMode)(int)IO.FileMode.OpenOrCreate;
            }
        }

        private static FileAccess GetFileAccess(IO.FileAccess access)
        {
            return access == IO.FileAccess.Read ?
                FileAccess.GENERIC_READ :
                FileAccess.GENERIC_WRITE;
        }

        private static FileShare GetFileShare(IO.FileShare share)
        {
            return (FileShare)(int)share;
        }
    }
}