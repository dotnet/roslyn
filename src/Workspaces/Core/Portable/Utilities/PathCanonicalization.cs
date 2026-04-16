// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis.Utilities;

/// <summary>
/// Provides path canonicalization to resolve the actual filesystem casing of a path.
/// This is used in the IDE layer to ensure that editorconfig file paths and source file paths
/// have consistent casing, even when they originate from different project system components
/// (e.g., MSBuild's GetPathsOfAllDirectoriesAbove vs. Compile items).
/// </summary>
internal static class PathCanonicalization
{
    /// <summary>
    /// Attempts to resolve the canonical filesystem path for the given file or directory path.
    /// On Windows, this uses GetFinalPathNameByHandle to obtain the actual casing from the filesystem.
    /// On non-Windows platforms, returns the path unchanged (filesystems are typically case-sensitive).
    /// If the file does not exist or the operation fails, returns the original path.
    /// </summary>
    [return: NotNullIfNotNull(nameof(path))]
    internal static string? GetCanonicalPath(string? path)
    {
        if (path is null || path.IndexOf('\0') >= 0 || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return path;
        }

        return GetCanonicalPathWindows(path);
    }

    private static string GetCanonicalPathWindows(string path)
    {
        using var handle = CreateFileW(
            lpFileName: path,
            dwDesiredAccess: FILE_READ_ATTRIBUTES,
            dwShareMode: FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            lpSecurityAttributes: IntPtr.Zero,
            dwCreationDisposition: OPEN_EXISTING,
            dwFlagsAndAttributes: FILE_FLAG_BACKUP_SEMANTICS,
            hTemplateFile: IntPtr.Zero);

        if (handle.IsInvalid)
        {
            return path;
        }

        const uint flags = FILE_NAME_NORMALIZED | VOLUME_NAME_DOS;
        var needed = GetFinalPathNameByHandleW(hFile: handle, lpszFilePath: null, cchFilePath: 0, dwFlags: flags);
        if (needed == 0)
        {
            return path;
        }

        var sb = new StringBuilder((int)needed + 1);
        var len = GetFinalPathNameByHandleW(hFile: handle, lpszFilePath: sb, cchFilePath: (uint)sb.Capacity, dwFlags: flags);
        if (len == 0)
        {
            return path;
        }

        return TrimExtendedPrefix(sb.ToString());
    }

    private static string TrimExtendedPrefix(string s)
    {
        if (s.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
            return @"\\" + s.Substring(8);
        if (s.StartsWith(@"\\?\", StringComparison.Ordinal))
            return s.Substring(4);
        return s;
    }

    private const uint FILE_READ_ATTRIBUTES = 0x0080;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint VOLUME_NAME_DOS = 0x0;
    private const uint FILE_NAME_NORMALIZED = 0x0;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle hFile,
        StringBuilder? lpszFilePath,
        uint cchFilePath,
        uint dwFlags);
}
