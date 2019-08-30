// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implements a few file name utilities that are needed by the compiler.
    /// In general the compiler is not supposed to understand the format of the paths.
    /// In rare cases it needs to check if a string is a valid file name or change the extension 
    /// (embedded resources, netmodules, output name).
    /// The APIs are intentionally limited to cover just these rare cases. Do not add more APIs.
    /// </summary>
    internal static class FileNameUtilities
    {
        private const string DirectorySeparatorStr = "\\";
        internal const char DirectorySeparatorChar = '\\';
        internal const char AltDirectorySeparatorChar = '/';
        internal const char VolumeSeparatorChar = ':';

        /// <summary>
        /// Returns true if the string represents an unqualified file name. 
        /// The name may contain any characters but directory and volume separators.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>
        /// True if <paramref name="path"/> is a simple file name, false if it is null or includes a directory specification.
        /// </returns>
        internal static bool IsFileName(string path)
        {
            return IndexOfFileName(path) == 0;
        }

        /// <summary>
        /// Returns the offset in <paramref name="path"/> where the dot that starts an extension is, or -1 if the path doesn't have an extension.
        /// </summary>
        /// <remarks>
        /// Returns 0 for path ".goo".
        /// Returns -1 for path "goo.".
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
        /// The same functionality as <see cref="System.IO.Path.GetExtension(string)"/> but doesn't throw an exception
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
        /// Returns "goo" for path "goo.".
        /// Returns "goo.." for path "goo...".
        /// </remarks>
        private static string RemoveExtension(string path)
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
        /// Returns path with the extension changed to <paramref name="extension"/>.
        /// </summary>
        /// <returns>
        /// Equivalent of <see cref="System.IO.Path.ChangeExtension(string, string)"/>
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
        internal static int IndexOfFileName(string path)
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
        /// Get file name from path.
        /// </summary>
        /// <remarks>Unlike <see cref="System.IO.Path.GetFileName(string)"/> doesn't check for invalid path characters.</remarks>
        internal static string GetFileName(string path, bool includeExtension = true)
        {
            int fileNameStart = IndexOfFileName(path);
            var fileName = (fileNameStart <= 0) ? path : path.Substring(fileNameStart);
            return includeExtension ? fileName : RemoveExtension(fileName);
        }
    }
}
