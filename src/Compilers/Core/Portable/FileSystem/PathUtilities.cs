// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    // Contains path parsing utilities.
    // We need our own because System.IO.Path is insufficient for our purposes
    // For example we need to be able to work with invalid paths or paths containing wildcards
    internal static class PathUtilities
    {
        // We consider '/' a directory separator on Unix like systems. 
        // On Windows both / and \ are equally accepted.
        internal static readonly char DirectorySeparatorChar = IsUnixLikePlatform ? '/' : '\\';
        internal static readonly char AltDirectorySeparatorChar = '/';
        internal static readonly string DirectorySeparatorStr = new string(DirectorySeparatorChar, 1);
        internal const char VolumeSeparatorChar = ':';

        private static bool IsUnixLikePlatform
        {
            get
            {
                return PortableShim.Path.DirectorySeparatorChar == '/';
            }
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
        }

        internal static string TrimTrailingSeparators(string s)
        {
            int lastSeparator = s.Length;
            while (lastSeparator > 0 && IsDirectorySeparator(s[lastSeparator - 1]))
            {
                lastSeparator = lastSeparator - 1;
            }

            if (lastSeparator != s.Length)
            {
                s = s.Substring(0, lastSeparator);
            }

            return s;
        }

        internal static string GetExtension(string path)
        {
            return FileNameUtilities.GetExtension(path);
        }

        internal static string ChangeExtension(string path, string extension)
        {
            return FileNameUtilities.ChangeExtension(path, extension);
        }

        internal static string RemoveExtension(string path)
        {
            return FileNameUtilities.ChangeExtension(path, extension: null);
        }

        internal static string GetFileName(string path, bool includeExtension = true)
        {
            return FileNameUtilities.GetFileName(path, includeExtension);
        }

        /// <summary>
        /// Get directory name from path.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="System.IO.Path.GetDirectoryName"/> it
        ///     doesn't check for invalid path characters, 
        /// </remarks>
        /// <returns>Prefix of path that represents a directory. </returns>
        internal static string GetDirectoryName(string path)
        {
            return GetDirectoryName(path, IsUnixLikePlatform);
        }

        /// <summary>
        /// Exposed for testing purposes only.
        /// </summary>
        internal static string GetDirectoryName(string path, bool isUnixLike)
        {
            if (path != null)
            {
                var rootLength = GetRoot(path, isUnixLike).Length;
                if (path.Length > rootLength)
                {
                    var i = path.Length;
                    while (i > rootLength)
                    {
                        i--;
                        if (IsDirectorySeparator(path[i]))
                        {
                            if (i > 0 && IsDirectorySeparator(path[i - 1]))
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    return path.Substring(0, i);
                }
            }

            return null;
        }

        private static string GetRoot(string path, bool isUnixLike)
        {
            if (path == null)
            {
                return null;
            }

            if (isUnixLike)
            {
                return GetUnixRoot(path);
            }
            else
            {
                return GetWindowsRoot(path);
            }
        }

        private static string GetWindowsRoot(string path)
        {
            // Windows
            int length = path.Length;
            if (length >= 1 && IsDirectorySeparator(path[0]))
            {
                if (length < 2 || !IsDirectorySeparator(path[1]))
                {
                    //  It was of the form:
                    //          \     
                    //          \f
                    // in this case, just return \ as the root.
                    return path.Substring(0, 1);
                }

                // We've got \\ so far.  If we have a path of the form \\x\y\z
                // then we want to return "\\x\y" as the root portion.
                int i = 2;
                bool hitSeparator = false;
                while (true)
                {
                    if (i == length)
                    {
                        // We reached the end of the path. The entire path is
                        // considered the root.
                        return path;
                    }

                    if (path[i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar)
                    {
                        // We got a non separator character.  Just keep consuming.
                        i++;
                        continue;
                    }

                    if (!hitSeparator)
                    {
                        // This is the first separator we've hit.  Consume it and keep going.
                        hitSeparator = true;
                        i++;
                        continue;
                    }

                    // We hit the second separator.  The root is the path up to this point.
                    return path.Substring(0, i);
                }
            }
            else if (length >= 2 && path[1] == VolumeSeparatorChar)
            {
                // handles c: and c:\
                return length >= 3 && IsDirectorySeparator(path[2])
                    ? path.Substring(0, @"c:\".Length)
                    : path.Substring(0, @"c:".Length);
            }
            else
            {
                // No path root.
                return "";
            }
        }

        private static string GetUnixRoot(string path)
        {
            if (path.Length > 0)
            {
                if (IsDirectorySeparator(path[0]))
                {
                    //  "/..."
                    return path.Substring(0, 1);
                }
            }

            // No path root.
            return "";
        }

        internal static PathKind GetPathKind(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PathKind.Empty;
            }

            // "C:\"
            // "\\machine" (UNC)
            // "/etc"      (Unix)
            if (IsAbsolute(path))
            {
                return PathKind.Absolute;
            }

            // "."
            // ".."
            // ".\"
            // "..\"
            if (path.Length > 0 && path[0] == '.')
            {
                if (path.Length == 1 || IsDirectorySeparator(path[1]))
                {
                    return PathKind.RelativeToCurrentDirectory;
                }

                if (path[1] == '.')
                {
                    if (path.Length == 2 || IsDirectorySeparator(path[2]))
                    {
                        return PathKind.RelativeToCurrentParent;
                    }
                }
            }

            if (!IsUnixLikePlatform)
            {
                // "\"
                // "\foo"
                if (path.Length >= 1 && IsDirectorySeparator(path[0]))
                {
                    return PathKind.RelativeToCurrentRoot;
                }

                // "C:foo"

                if (path.Length >= 2 && path[1] == VolumeSeparatorChar && (path.Length <= 2 || !IsDirectorySeparator(path[2])))
                {
                    return PathKind.RelativeToDriveDirectory;
                }
            }

            // "foo.dll"
            return PathKind.Relative;
        }

        internal static bool IsAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (IsUnixLikePlatform)
            {
                return path[0] == DirectorySeparatorChar;
            }

            // "C:\"
            if (IsDriveRootedAbsolutePath(path))
            {
                // Including invalid paths (e.g. "*:\")
                return true;
            }

            // "\\machine\share"
            // Including invalid/incomplete UNC paths (e.g. "\\foo")
            return path.Length >= 2 &&
                IsDirectorySeparator(path[0]) &&
                IsDirectorySeparator(path[1]);
        }

        /// <summary>
        /// Returns true if given path is absolute and starts with a drive specification ("C:\").
        /// </summary>
        private static bool IsDriveRootedAbsolutePath(string path)
        {
            Debug.Assert(!IsUnixLikePlatform);
            return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
        }

        /// <summary>
        /// Get a prefix of given path which is the root of the path.
        /// </summary>
        /// <returns>
        /// Root of an absolute path or null if the path isn't absolute or has invalid format (e.g. "\\").
        /// It may or may not end with a directory separator (e.g. "C:\", "C:\foo", "\\machine\share", etc.) .
        /// </returns>
        internal static string GetPathRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            int length = GetPathRootLength(path);
            return (length != -1) ? path.Substring(0, length) : null;
        }

        private static int GetPathRootLength(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (IsUnixLikePlatform)
            {
                if (IsDirectorySeparator(path[0]))
                {
                    //  "/*"
                    return 1;
                }
            }
            else
            {
                // "C:\"
                if (IsDriveRootedAbsolutePath(path))
                {
                    return 3;
                }

                if (IsDirectorySeparator(path[0]))
                {
                    // "\\machine\share"
                    return GetUncPathRootLength(path);
                }
            }

            return -1;
        }

        /// <summary>
        /// Calculates the length of root of an UNC path.
        /// </summary>
        /// <remarks>
        /// "\\server\share" is root of UNC path "\\server\share\dir1\dir2\file".
        /// </remarks>
        private static int GetUncPathRootLength(string path)
        {
            Debug.Assert(IsDirectorySeparator(path[0]));

            // root:
            // [directory-separator]{2,}[^directory-separator]+[directory-separator]+[^directory-separator]+

            int serverIndex = IndexOfNonDirectorySeparator(path, 1);
            if (serverIndex < 2)
            {
                return -1;
            }

            int separator = IndexOfDirectorySeparator(path, serverIndex);
            if (separator == -1)
            {
                return -1;
            }

            int shareIndex = IndexOfNonDirectorySeparator(path, separator);
            if (shareIndex == -1)
            {
                return -1;
            }

            int rootEnd = IndexOfDirectorySeparator(path, shareIndex);
            return rootEnd == -1 ? path.Length : rootEnd;
        }

        private static int IndexOfDirectorySeparator(string path, int start)
        {
            for (int i = start; i < path.Length; i++)
            {
                if (IsDirectorySeparator(path[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfNonDirectorySeparator(string path, int start)
        {
            for (int i = start; i < path.Length; i++)
            {
                if (!IsDirectorySeparator(path[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Combines an absolute path with a relative.
        /// </summary>
        /// <param name="root">Absolute root path.</param>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>
        /// An absolute combined path, or null if <paramref name="relativePath"/> is 
        /// absolute (e.g. "C:\abc", "\\machine\share\abc"), 
        /// relative to the current root (e.g. "\abc"), 
        /// or relative to a drive directory (e.g. "C:abc\def").
        /// </returns>
        /// <seealso cref="CombinePossiblyRelativeAndRelativePaths"/>
        internal static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
        {
            Debug.Assert(IsAbsolute(root));

            return CombinePossiblyRelativeAndRelativePaths(root, relativePath);
        }

        /// <summary>
        /// Combine two paths, the first of which may be absolute.
        /// </summary>
        /// <param name="rootOpt">First path: absolute, relative, or null.</param>
        /// <param name="relativePath">Second path: relative and non-null.</param>
        /// <returns>null, if <paramref name="rootOpt"/> is null; a combined path, otherwise.</returns>
        /// <seealso cref="CombineAbsoluteAndRelativePaths"/>
        internal static string CombinePossiblyRelativeAndRelativePaths(string rootOpt, string relativePath)
        {
            if (string.IsNullOrEmpty(rootOpt))
            {
                return null;
            }

            switch (GetPathKind(relativePath))
            {
                case PathKind.Empty:
                    return rootOpt;

                case PathKind.Absolute:
                case PathKind.RelativeToCurrentRoot:
                case PathKind.RelativeToDriveDirectory:
                    return null;
            }

            return CombinePathsUnchecked(rootOpt, relativePath);
        }

        internal static string CombinePathsUnchecked(string root, string relativePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(root));

            char c = root[root.Length - 1];
            if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
            {
                return root + DirectorySeparatorStr + relativePath;
            }

            return root + relativePath;
        }

        internal static string RemoveTrailingDirectorySeparator(string path)
        {
            if (path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]))
            {
                return path.Substring(0, path.Length - 1);
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// Determines whether an assembly reference is considered an assembly file path or an assembly name.
        /// used, for example, on values of /r and #r.
        /// </summary>
        internal static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            Debug.Assert(assemblyDisplayNameOrPath != null);

            string extension = FileNameUtilities.GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }

        /// <summary>
        /// Determines if "path" contains 'component' within itself.
        /// i.e. asking if the path "c:\foo\bar\baz" has component "bar" would return 'true'.
        /// On the other hand, if you had "c:\foo\bar1\baz" then it would not have "bar" as a
        /// component.
        /// 
        /// A path contains a component if any file name or directory name in the path
        /// matches 'component'.  As such, if you had something like "\\foo" then that would
        /// not have "foo" as a component. That's because here "foo" is the server name portion
        /// of the UNC path, and not an actual directory or file name.
        /// </summary>
        internal static bool ContainsPathComponent(string path, string component)
        {
            if (path?.IndexOf(component, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var currentPath = path;
                while (currentPath != null)
                {
                    var currentName = GetFileName(currentPath);
                    if (StringComparer.OrdinalIgnoreCase.Equals(currentName, component))
                    {
                        return true;
                    }

                    currentPath = GetDirectoryName(currentPath);
                }
            }

            return false;
        }
    }
}
