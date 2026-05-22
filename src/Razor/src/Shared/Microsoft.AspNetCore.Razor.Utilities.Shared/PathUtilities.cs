// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Razor.Utilities;

#if !NET
using Microsoft.AspNetCore.Razor.PooledObjects;
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.AspNetCore.Razor;

internal static partial class PathUtilities
{
    public static readonly StringComparer OSSpecificPathComparer = PlatformInformation.IsWindows
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static readonly StringComparison OSSpecificPathComparison = PlatformInformation.IsWindows
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

#if !NET
    // \\?\, \\.\, \??\
    private const int DevicePrefixLength = 4;
    // \\
    private const int UncPrefixLength = 2;
    // \\?\UNC\, \\.\UNC\
    private const int UncExtendedPrefixLength = 8;
    private const int MaxShortPath = 260;
#endif

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L149-L172

    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetDirectoryName(string? path)
    {
#if NET
        return Path.GetDirectoryName(path);
#else
        if (path == null || IsEffectivelyEmpty(path.AsSpan()))
        {
            return null;
        }

        var end = GetDirectoryNameOffset(path.AsSpan());
        return end >= 0 ? NormalizeDirectorySeparators(path[..end]) : null;
#endif
    }

    public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
    {
#if NET
        return Path.GetDirectoryName(path);
#else
        if (IsEffectivelyEmpty(path))
            return [];

        var end = GetDirectoryNameOffset(path);
        return end >= 0 ? path[..end] : [];
#endif
    }

#if !NET

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Unix.cs#L94
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L381

    private static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        if (PlatformInformation.IsWindows)
        {
            if (path.IsEmpty)
                return true;

            foreach (var c in path)
            {
                if (c != ' ')
                    return false;
            }

            return true;
        }

        return path.IsEmpty;
    }

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L158

    private static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
    {
        var rootLength = GetRootLength(path);
        var end = path.Length;
        if (end <= rootLength)
            return -1;

        while (end > rootLength && !IsDirectorySeparator(path[--end]))
            ;

        // Trim off any remaining separators (to deal with C:\foo\\bar)
        while (end > rootLength && IsDirectorySeparator(path[end - 1]))
            end--;

        return end;
    }

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Unix.cs#L22
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L181

    private static int GetRootLength(ReadOnlySpan<char> path)
    {
        if (PlatformInformation.IsWindows)
        {
            var pathLength = path.Length;
            var i = 0;

            var deviceSyntax = IsDevice(path);
            var deviceUnc = deviceSyntax && IsDeviceUNC(path);

            if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")
                if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1])))
                {
                    // UNC (\\?\UNC\ or \\), scan past server\share

                    // Start past the prefix ("\\" or "\\?\UNC\")
                    i = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;

                    // Skip two separators at most
                    var n = 2;
                    while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0))
                        i++;
                }
                else
                {
                    // Current drive rooted (e.g. "\foo")
                    i = 1;
                }
            }
            else if (deviceSyntax)
            {
                // Device path (e.g. "\\?\.", "\\.\")
                // Skip any characters following the prefix that aren't a separator
                i = DevicePrefixLength;
                while (i < pathLength && !IsDirectorySeparator(path[i]))
                    i++;

                // If there is another separator take it, as long as we have had at least one
                // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
                if (i < pathLength && i > DevicePrefixLength && IsDirectorySeparator(path[i]))
                    i++;
            }
            else if (pathLength >= 2
                && path[1] == Path.VolumeSeparatorChar
                && IsValidDriveChar(path[0]))
            {
                // Valid drive specified path ("C:", "D:", etc.)
                i = 2;

                // If the colon is followed by a directory separator, move past it (e.g "C:\")
                if (pathLength > 2 && IsDirectorySeparator(path[2]))
                    i++;
            }

            return i;
        }

        return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
    }

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L134

    private static bool IsDevice(ReadOnlySpan<char> path)
    {
        // If the path begins with any two separators is will be recognized and normalized and prepped with
        // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
        return IsExtended(path)
            ||
            (
                path.Length >= DevicePrefixLength
                && IsDirectorySeparator(path[0])
                && IsDirectorySeparator(path[1])
                && (path[2] == '.' || path[2] == '?')
                && IsDirectorySeparator(path[3])
            );
    }

    private static bool IsDeviceUNC(ReadOnlySpan<char> path)
    {
        return path.Length >= UncExtendedPrefixLength
            && IsDevice(path)
            && IsDirectorySeparator(path[7])
            && path[4] == 'U'
            && path[5] == 'N'
            && path[6] == 'C';
    }

    private static bool IsExtended(ReadOnlySpan<char> path)
    {
        // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
        // Skipping of normalization will *only* occur if back slashes ('\') are used.
        return path.Length >= DevicePrefixLength
            && path[0] == '\\'
            && (path[1] == '\\' || path[1] == '?')
            && path[2] == '?'
            && path[3] == '\\';
    }

    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Unix.cs#L39
    // - https://github.com/dotnet/runtime/blob/50be35a211536209b3e1d68619f7d2f2bf3caa0a/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L318

    [return: NotNullIfNotNull(nameof(path))]
    private static string? NormalizeDirectorySeparators(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (PlatformInformation.IsWindows)
        {
            char current;

            // Make a pass to see if we need to normalize so we can potentially skip allocating
            var normalized = true;

            for (var i = 0; i < path.Length; i++)
            {
                current = path[i];
                if (IsDirectorySeparator(current)
                    && (current != Path.DirectorySeparatorChar
                        // Check for sequential separators past the first position (we need to keep initial two for UNC/extended)
                        || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))))
                {
                    normalized = false;
                    break;
                }
            }

            if (normalized)
                return path;

            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            builder.SetCapacityIfLarger(MaxShortPath);

            var start = 0;
            if (IsDirectorySeparator(path[start]))
            {
                start++;
                builder.Append(Path.DirectorySeparatorChar);
            }

            for (var i = start; i < path.Length; i++)
            {
                current = path[i];

                // If we have a separator
                if (IsDirectorySeparator(current))
                {
                    // If the next is a separator, skip adding this
                    if (i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Ensure it is the primary separator
                    current = Path.DirectorySeparatorChar;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }
        else
        {
            // Make a pass to see if we need to normalize so we can potentially skip allocating
            var normalized = true;

            for (var i = 0; i < path.Length; i++)
            {
                if (IsDirectorySeparator(path[i]) &&
                    i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                {
                    normalized = false;
                    break;
                }
            }

            if (normalized)
                return path;

            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            builder.SetCapacityIfLarger(path.Length);

            for (var i = 0; i < path.Length; i++)
            {
                var current = path[i];

                // Skip if we have another separator following
                if (IsDirectorySeparator(current) &&
                    i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                    continue;

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
#endif

    [return: NotNullIfNotNull(nameof(path))]
    public static string? GetExtension(string? path)
        => Path.GetExtension(path);

    public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
    {
#if NET
        return Path.GetExtension(path);
#else
        // Derived from the .NET Runtime:
        // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L189-L213

        var length = path.Length;

        for (var i = length - 1; i >= 0; i--)
        {
            var ch = path[i];

            if (ch == '.')
            {
                return i != length - 1
                    ? path[i..length]
                    : [];
            }

            if (IsDirectorySeparator(ch))
            {
                break;
            }
        }

        return [];
#endif
    }

    public static bool HasExtension([NotNullWhen(true)] string? path)
        => Path.HasExtension(path);

    public static bool HasExtension(ReadOnlySpan<char> path)
    {
#if NET
        return Path.HasExtension(path);
#else
        return !GetExtension(path).IsEmpty;
#endif
    }

#if !NET
    // Derived from the .NET Runtime:
    // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/Common/src/System/IO/PathInternal.Unix.cs#L27-L32
    // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L280-L283

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectorySeparator(char ch)
        => ch == Path.DirectorySeparatorChar ||
          (PlatformInformation.IsWindows && ch == Path.AltDirectorySeparatorChar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidDriveChar(char value)
        => (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');
#endif

    public static bool IsPathFullyQualified(string path)
    {
        ArgHelper.ThrowIfNull(path);

        return IsPathFullyQualified(path.AsSpan());
    }

    public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
    {
#if NET
        return Path.IsPathFullyQualified(path);
#else
        if (PlatformInformation.IsWindows)
        {
            // Derived from .NET Runtime:
            // - https://github.com/dotnet/runtime/blob/c7c961a330395152e5ec4000032cd3204ceb4a10/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L250-L274

            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return false;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes or
                // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
                return path[1] == '?' || IsDirectorySeparator(path[1]);
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return (path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2])
                // To match old behavior we'll check the drive character for validity as the path is technically
                // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
                && IsValidDriveChar(path[0]);
        }
        else
        {
            // Derived from .NET Runtime:
            // - https://github.com/dotnet/runtime/blob/c7c961a330395152e5ec4000032cd3204ceb4a10/src/libraries/Common/src/System/IO/PathInternal.Unix.cs#L77-L82

            // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
            // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
            return IsPathRooted(path);
        }
#endif
    }

    public static bool IsPathRooted(string path)
    {
#if NET
        return Path.IsPathRooted(path);
#else
        return IsPathRooted(path.AsSpan());
#endif
    }

    public static bool IsPathRooted(ReadOnlySpan<char> path)
    {
#if NET
        return Path.IsPathRooted(path);

#else
        if (PlatformInformation.IsWindows)
        {
            // Derived from .NET Runtime
            // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/System.Private.CoreLib/src/System/IO/Path.Windows.cs#L271-L276

            var length = path.Length;
            return (length >= 1 && IsDirectorySeparator(path[0]))
                || (length >= 2 && IsValidDriveChar(path[0]) && path[1] == Path.VolumeSeparatorChar);
        }
        else
        {
            // Derived from .NET Runtime
            // - https://github.com/dotnet/runtime/blob/850c0ab4519b904a28f2d67abdaba1ac78c955ff/src/libraries/System.Private.CoreLib/src/System/IO/Path.Unix.cs#L132-L135

            return path.StartsWith(Path.DirectorySeparatorChar);
        }
#endif
    }
}
