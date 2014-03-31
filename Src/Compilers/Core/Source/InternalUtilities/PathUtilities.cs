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

        private static readonly char[] InvalidFileNameChars =
        {
            '\"', '<', '>', '|', '\0', (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7,
            (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15, (char)16,
            (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25,
            (char)26, (char)27, (char)28, (char)29, (char)30, (char)31, ':', '*', '?', '\\', '/'
        };

        internal static bool IsValidFileName(string fileName)
        {
            return fileName.IndexOfAny(InvalidFileNameChars) < 0;
        }

        internal static bool HasDirectorySeparators(string path)
        {
            return path.IndexOfAny(DirectorySeparators) >= 0;
        }

        /// <summary>
        /// Trims all '.' and whitespaces from the end of the path
        /// </summary>
        internal static string RemoveTrailingSpacesAndDots(string path)
        {
            if (path == null)
            {
                return path;
            }

            int length = path.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                char c = path[i];
                if (!char.IsWhiteSpace(c) && c != '.')
                {
                    return i == (length - 1) ? path : path.Substring(0, i + 1);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the offset in <paramref name="path"/> where the dot that starts an extension is, or -1 if the path doesn't have an extension.
        /// </summary>
        /// <remarks>
        /// Returns 0 for path ".foo".
        /// Returns -1 for path "foo.".
        /// </remarks>
        private static int IndexOfExtension(string path)
        {
            if (path == null)
            {
                return -1;
            }

            int length = path.Length;
            int i = length;

            while (--i >= 0)
            {
                char c = path[i];
                if (c == '.')
                {
                    if (i != length - 1)
                    {
                        return i;
                    }

                    return -1;
                }

                if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar)
                {
                    break;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns an extension of the specified path string.
        /// </summary>
        /// <remarks>
        /// The same functionality as <see cref="M:System.IO.Path.GetExtension(string)"/> but doesn't throw an exception
        /// if there are invalid characters in the path.
        /// </remarks>
        internal static string GetExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            return (index >= 0) ? path.Substring(index) : string.Empty;
        }

        /// <summary>
        /// Removes extension from path.
        /// </summary>
        /// <remarks>
        /// Returns "foo" for path "foo.".
        /// Returns "foo.." for path "foo...".
        /// </remarks>
        internal static string RemoveExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            if (index >= 0)
            {
                return path.Substring(0, index);
            }

            // trim last ".", if present
            if (path.Length > 0 && path[path.Length - 1] == '.')
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        /// <summary>
        /// Returns path with the extenion changed to <paramref name="extension"/>.
        /// </summary>
        /// <returns>
        /// Equivalent of <see cref="M:System.IO.Path.ChangeExtension"/>
        /// 
        /// If <paramref name="path"/> is null, returns null. 
        /// If path does not end with an extension, the new extension is appended to the path.
        /// If extension is null, equivalent to <see cref="RemoveExtension"/>.
        /// </returns>
        internal static string ChangeExtension(string path, string extension)
        {
            if (path == null)
            {
                return null;
            }

            var pathWithoutExtension = RemoveExtension(path);
            if (extension == null || path.Length == 0)
            {
                return pathWithoutExtension;
            }

            if (extension.Length == 0 || extension[0] != '.')
            {
                return pathWithoutExtension + "." + extension;
            }

            return pathWithoutExtension + extension;
        }

        /// <summary>
        /// Returns the position in given path where the file name starts.
        /// </summary>
        /// <returns>-1 if path is null.</returns>
        private static int IndexOfFileName(string path)
        {
            if (path == null)
            {
                return -1;
            }

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns true if the string represents an unqualified file name.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>True if <paramref name="path"/> is a simple file name, false if it is null or includes a directory specification.</returns>
        internal static bool IsFileName(string path)
        {
            return IndexOfFileName(path) == 0;
        }

        /// <summary>
        /// Get file name from path.
        /// </summary>
        /// <remarks>Unlike <see cref="M:System.IO.Path.GetFileName"/> doesn't check for invalid path characters.</remarks>
        internal static string GetFileName(string path)
        {
            int fileNameStart = IndexOfFileName(path);
            return (fileNameStart <= 0) ? path : path.Substring(fileNameStart);
        }

        /// <summary>
        /// Get directory name from path.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="M:System.IO.Path.GetDirectoryName"/> it
        ///     doesn't check for invalid path characters, 
        ///     doesn't strip any trailing directory separators (TODO: tomat),
        ///     doesn't recognize UNC structure \\computer-name\share\directory-name\file-name (TODO: tomat).
        /// </remarks>
        /// <returns>Prefix of path that represents a directory. </returns>
        internal static string GetDirectoryName(string path)
        {
            int fileNameStart = IndexOfFileName(path);
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

            string extension = GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }
    }
}
