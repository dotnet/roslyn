// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

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
    }
}
