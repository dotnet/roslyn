// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class FilePathUtilities
    {
        public static bool IsRelativePath(string basePath, string path)
        {
            return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRelativePath(string basePath, string path)
        {
            if (IsRelativePath(basePath, path))
            {
                var relativePath = path.Substring(basePath.Length);
                while (relativePath.Length > 0 && PathUtilities.IsDirectorySeparator(relativePath[0]))
                {
                    relativePath = relativePath.Substring(1);
                }

                return relativePath;
            }

            return path;
        }

        internal static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException(WorkspacesResources.Absolute_path_expected, argumentName);
            }
        }
    }
}
