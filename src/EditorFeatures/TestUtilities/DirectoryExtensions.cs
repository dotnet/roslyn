// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class DirectoryExtensions
    {
        /// <summary>
        /// The framework's Directory.Delete does not delete individual files within the directory structure. So it could throw directory not empty exception.
        /// This helper function recurses through the directory structure, deleting individual files before deleting the directory itself.
        /// </summary>
        /// <param name="path">The directory path to delete</param>
        public static void DeleteRecursively(string path)
        {
            var files = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            foreach (var file in files)
            {
                // If there were read-only attributes on the file, the delete would throw, so set normal permissions.
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteRecursively(dir);
            }

            Directory.Delete(path, recursive: false);
        }
    }
}
