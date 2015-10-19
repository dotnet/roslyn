// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class FilePathUtilities
    {
        public static bool IsNestedPath(string basePath, string fullPath)
        {
            return basePath.Length > 0
                && fullPath.Length > basePath.Length
                && fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                && (PathUtilities.IsDirectorySeparator(basePath[basePath.Length - 1]) || PathUtilities.IsDirectorySeparator(fullPath[basePath.Length]));
        }

        public static string GetNestedPath(string baseDirectory, string fullPath)
        {
            if (IsNestedPath(baseDirectory, fullPath))
            {
                var relativePath = fullPath.Substring(baseDirectory.Length);
                while (relativePath.Length > 0 && PathUtilities.IsDirectorySeparator(relativePath[0]))
                {
                    relativePath = relativePath.Substring(1);
                }

                return relativePath;
            }

            return fullPath;
        }

        private static readonly char[] s_pathChars = new char[] { Path.VolumeSeparatorChar, Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static string GetRelativePath(string baseDirectory, string fullPath)
        {
            string relativePath = string.Empty;

            if (IsNestedPath(baseDirectory, fullPath))
            {
                return GetNestedPath(baseDirectory, fullPath);
            }

            var basePathParts = baseDirectory.Split(s_pathChars);
            var fullPathParts = fullPath.Split(s_pathChars);

            if (basePathParts.Length == 0 || fullPathParts.Length == 0)
            {
                return fullPath;
            }

            int index = 0;

            // find index where full path diverges from base path
            for (; index < basePathParts.Length; index++)
            {
                if (!PathsEqual(basePathParts[index], fullPathParts[index]))
                {
                    break;
                }
            }

            // if the first part doesn't match, they don't even have the same volume
            // so there can be no relative path.
            if (index == 0)
            {
                return fullPath;
            }

            // add backup notation for remaining base path levels beyond the index
            var remainingParts = basePathParts.Length - index;
            if (remainingParts > 0)
            {
                string directorySeparator = Path.DirectorySeparatorChar.ToString();
                for (int i = 0; i < remainingParts; i++)
                {
                    relativePath = relativePath + ".." + directorySeparator;
                }
            }

            // add the rest of the full path parts
            for (int i = index; i < fullPathParts.Length; i++)
            {
                relativePath = Path.Combine(relativePath, fullPathParts[i]);
            }

            return relativePath;
        }

        internal static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException(WorkspacesResources.AbsolutePathExpected, argumentName);
            }
        }

        public static bool PathsEqual(string path1, string path2)
        {
            return string.Compare(path1, path2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool TryCombine(string path1, string path2, out string result)
        {
            try
            {
                // don't throw exception when either path1 or path2 contains illegal path char
                result = Path.Combine(path1, path2);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
