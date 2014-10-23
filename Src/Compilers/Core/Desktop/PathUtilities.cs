// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Utilities
{
    internal static class PathUtilities
    {
        private const string DirectorySeparatorStr = "\\";
        internal const char DirectorySeparatorChar = '\\';
        internal const char AltDirectorySeparatorChar = '/';
        internal const char VolumeSeparatorChar = ':';

        private static readonly char[] DirectorySeparators = new[]
        {
            DirectorySeparatorChar, AltDirectorySeparatorChar, VolumeSeparatorChar
        };

        internal static bool HasDirectorySeparators(string path)
        {
            return path.IndexOfAny(DirectorySeparators) >= 0;
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

        internal static string GetFileName(string path)
        {
            return FileNameUtilities.GetFileName(path);
        }

        /// <summary>
        /// Get directory name from path.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="System.IO.Path.GetDirectoryName"/> it
        ///     doesn't check for invalid path characters, 
        ///     doesn't strip any trailing directory separators (TODO: tomat),
        ///     doesn't recognize UNC structure \\computer-name\share\directory-name\file-name (TODO: tomat).
        /// </remarks>
        /// <returns>Prefix of path that represents a directory. </returns>
        internal static string GetDirectoryName(string path)
        {
            int fileNameStart = FileNameUtilities.IndexOfFileName(path);
            if (fileNameStart < 0)
            {
                return null;
            }

            return path.Substring(0, fileNameStart);
        }
        
        internal static PathKind GetPathKind(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PathKind.Empty;
            }

            // "C:\"
            // "\\machine" (UNC)
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

            // "foo.dll"
            return PathKind.Relative;
        }

        internal static bool IsAbsolute(string path)
        {
            if (path == null)
            {
                return false;
            }

            // "C:\"
            if (IsDriveRootedAbsolutePath(path))
            {
                // Including invalid paths (e.g. "*:\")
                return true;
            }

            // "\\machine\share"
            if (path.Length >= 2 && IsDirectorySeparator(path[0]) && IsDirectorySeparator(path[1]))
            {
                // Including invalid/incomplete UNC paths (e.g. "\\foo")
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if given path is absolute and starts with a drive specification ("C:\").
        /// </summary>
        private static bool IsDriveRootedAbsolutePath(string path)
        {
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
            if (path == null)
            {
                return null;
            }

            int length = GetPathRootLength(path);
            return (length != -1) ? path.Substring(0, length) : null;
        }

        private static int GetPathRootLength(string path)
        {
            Debug.Assert(path != null);

            // "C:\"
            if (IsDriveRootedAbsolutePath(path))
            {
                return 3;
            }

            // "\\machine\share"
            return GetUncPathRootLength(path);
        }

        /// <summary>
        /// Calculates the length of root of an UNC path.
        /// </summary>
        /// <remarks>
        /// "\\server\share" is root of UNC path "\\server\share\dir1\dir2\file".
        /// </remarks>
        private static int GetUncPathRootLength(string path)
        {
            Debug.Assert(path != null);

            // root:
            // [directory-separator]{2,}[^directory-separator]+[directory-separator]+[^directory-separator]+

            int serverIndex = IndexOfNonDirectorySeparator(path, 0);
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

        internal static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
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
    }
}
