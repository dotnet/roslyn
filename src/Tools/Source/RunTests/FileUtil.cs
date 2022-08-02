// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace RunTests
{
    internal static class FileUtil
    {
        /// <summary>
        /// Ensure a directory with the given name is present.  Will be created if necessary. True
        /// is returned when it is created.
        /// </summary>
        internal static bool EnsureDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Delete file if it exists and swallow any potential exceptions.  Returns true if the
        /// file is actually deleted.
        /// </summary>
        internal static bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        /// <summary>
        /// Delete directory if it exists and swallow any potential exceptions.  Returns true if the
        /// directory is actually deleted.
        /// </summary>
        internal static bool DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                    return true;
                }
            }
            catch
            {
                // Ignore
            }

            return false;
        }

        internal static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            var directory = new DirectoryInfo(sourceDirectory);
            Contract.Assert(directory.Exists);

            var subDirectories = directory.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDirectory);

            // Copy all files in the directory to the new directory.
            directory.GetFiles().ToList().ForEach(f => f.CopyTo(Path.Combine(destinationDirectory, f.Name), overwrite: true));
            
            // Copy all subdirectories recursively.
            subDirectories.ToList().ForEach(d => CopyDirectory(d.FullName, Path.Combine(destinationDirectory, d.Name)));
        }
    }
}
