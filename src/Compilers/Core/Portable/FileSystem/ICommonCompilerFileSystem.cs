// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Abstraction over the file system that is useful for test hooks
    /// </summary>
    internal interface ICommonCompilerFileSystem
    {
        bool FileExists(string filePath);

        Stream OpenFile(string filePath, FileMode mode, FileAccess access, FileShare share);

        Stream OpenFileEx(string filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, out string normalizedFilePath);
    }

    internal static class CommonCompilerFileSystemExtensions
    {
        /// <summary>
        /// Open a file and ensure common exception types are wrapped to <see cref="IOException"/>.
        /// </summary>
        internal static Stream OpenFileWithNormalizedException(this ICommonCompilerFileSystem fileSystem, string filePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            try
            {
                return fileSystem.OpenFile(filePath, fileMode, fileAccess, fileShare);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                throw new FileNotFoundException(e.Message, filePath, e);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }
    }

    internal sealed class StandardFileSystem : ICommonCompilerFileSystem
    {
        public static StandardFileSystem Instance { get; } = new StandardFileSystem();

        private StandardFileSystem()
        {
        }

        public bool FileExists(string filePath) => File.Exists(filePath);

        public Stream OpenFile(string filePath, FileMode mode, FileAccess access, FileShare share)
            => new FileStream(filePath, mode, access, share);

        public Stream OpenFileEx(string filePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, out string normalizedFilePath)
        {
            var fileStream = new FileStream(filePath, mode, access, share, bufferSize, options);
            normalizedFilePath = fileStream.Name;
            return fileStream;
        }
    }
}
