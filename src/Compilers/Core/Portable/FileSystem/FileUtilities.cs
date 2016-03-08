// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Utilities
{
    internal static class FileUtilities
    {
        /// <summary>
        /// Resolves relative path and returns absolute path.
        /// The method depends only on values of its parameters and their implementation (for fileExists).
        /// It doesn't itself depend on the state of the current process (namely on the current drive directories) or 
        /// the state of file system.
        /// </summary>
        /// <param name="path">
        /// Path to resolve.
        /// </param>
        /// <param name="basePath">
        /// Base file path to resolve CWD-relative paths against. Null if not available.
        /// </param>
        /// <param name="baseDirectory">
        /// Base directory to resolve CWD-relative paths against if <paramref name="basePath"/> isn't specified. 
        /// Must be absolute path.
        /// Null if not available.
        /// </param>
        /// <param name="searchPaths">
        /// Sequence of paths used to search for unqualified relative paths.
        /// </param>
        /// <param name="fileExists">
        /// Method that tests existence of a file.
        /// </param>
        /// <returns>
        /// The resolved path or null if the path can't be resolved or does not exist.
        /// </returns>
        internal static string ResolveRelativePath(
            string path,
            string basePath,
            string baseDirectory,
            IEnumerable<string> searchPaths,
            Func<string, bool> fileExists)
        {
            Debug.Assert(baseDirectory == null || searchPaths != null || PathUtilities.IsAbsolute(baseDirectory));
            Debug.Assert(searchPaths != null);
            Debug.Assert(fileExists != null);

            string combinedPath;
            var kind = PathUtilities.GetPathKind(path);
            if (kind == PathKind.Relative)
            {
                // first, look in the base directory:
                baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                if (baseDirectory != null)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                // try search paths:
                foreach (var searchPath in searchPaths)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(searchPath, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                return null;
            }

            combinedPath = ResolveRelativePath(kind, path, basePath, baseDirectory);
            if (combinedPath != null)
            {
                Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                if (fileExists(combinedPath))
                {
                    return combinedPath;
                }
            }

            return null;
        }

        internal static string ResolveRelativePath(string path, string baseDirectory)
        {
            return ResolveRelativePath(path, null, baseDirectory);
        }

        internal static string ResolveRelativePath(string path, string basePath, string baseDirectory)
        {
            Debug.Assert(baseDirectory == null || PathUtilities.IsAbsolute(baseDirectory));
            return ResolveRelativePath(PathUtilities.GetPathKind(path), path, basePath, baseDirectory);
        }

        private static string ResolveRelativePath(PathKind kind, string path, string basePath, string baseDirectory)
        {
            switch (kind)
            {
                case PathKind.Empty:
                    return null;

                case PathKind.Relative:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    // with no search paths relative paths are relative to the base directory:
                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);

                case PathKind.RelativeToCurrentDirectory:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    if (path.Length == 1)
                    {
                        // "."
                        return baseDirectory;
                    }
                    else
                    {
                        // ".\path"
                        return PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    }

                case PathKind.RelativeToCurrentParent:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    // ".."
                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);

                case PathKind.RelativeToCurrentRoot:
                    string baseRoot;
                    if (basePath != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(basePath);
                    }
                    else if (baseDirectory != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(baseDirectory);
                    }
                    else
                    {
                        return null;
                    }

                    if (string.IsNullOrEmpty(baseRoot))
                    {
                        return null;
                    }

                    Debug.Assert(PathUtilities.IsDirectorySeparator(path[0]));
                    Debug.Assert(path.Length == 1 || !PathUtilities.IsDirectorySeparator(path[1]));
                    return PathUtilities.CombinePathsUnchecked(baseRoot, path.Substring(1));

                case PathKind.RelativeToDriveDirectory:
                    // drive relative paths not supported, can't resolve:
                    return null;

                case PathKind.Absolute:
                    return path;

                default:
                    // EDMAURER this is not using ExceptionUtilities.UnexpectedValue() because this file
                    // is shared via linking with other code that doesn't have the ExceptionUtilities.
                    throw new InvalidOperationException(string.Format("Unexpected PathKind {0}.", kind));
            }
        }

        private static string GetBaseDirectory(string basePath, string baseDirectory)
        {
            // relative base paths are relative to the base directory:
            string resolvedBasePath = ResolveRelativePath(basePath, baseDirectory);
            if (resolvedBasePath == null)
            {
                return baseDirectory;
            }

            // Note: Path.GetDirectoryName doesn't normalize the path and so it doesn't depend on the process state.
            Debug.Assert(PathUtilities.IsAbsolute(resolvedBasePath));
            try
            {
                return Path.GetDirectoryName(resolvedBasePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();

        internal static string NormalizeRelativePath(string path, string basePath, string baseDirectory)
        {
            // Does this look like a URI at all or does it have any invalid path characters? If so, just use it as is.
            if (path.IndexOf("://", StringComparison.Ordinal) >= 0 || path.IndexOfAny(s_invalidPathChars) >= 0)
            {
                return null;
            }

            string resolvedPath = ResolveRelativePath(path, basePath, baseDirectory);
            if (resolvedPath == null)
            {
                return null;
            }

            string normalizedPath = TryNormalizeAbsolutePath(resolvedPath);
            if (normalizedPath == null)
            {
                return null;
            }

            return normalizedPath;
        }

        /// <summary>
        /// Normalizes an absolute path.
        /// </summary>
        /// <param name="path">Path to normalize.</param>
        /// <exception cref="IOException"/>
        /// <returns>Normalized path.</returns>
        internal static string NormalizeAbsolutePath(string path)
        {
            // we can only call GetFullPath on an absolute path to avoid dependency on process state (current directory):
            Debug.Assert(PathUtilities.IsAbsolute(path));

            try
            {
                return PortableShim.Path.GetFullPath(path);
            }
            catch (ArgumentException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (System.Security.SecurityException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (NotSupportedException e)
            {
                throw new IOException(e.Message, e);
            }
        }

        internal static string NormalizeDirectoryPath(string path)
        {
            return NormalizeAbsolutePath(path).TrimEnd(PortableShim.Path.DirectorySeparatorChar, PortableShim.Path.AltDirectorySeparatorChar);
        }

        internal static string TryNormalizeAbsolutePath(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));

            try
            {
                return PortableShim.Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        internal static Stream OpenRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            try
            {
                return PortableShim.FileStream.Create(fullPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.Read);
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

        internal static Stream OpenAsyncRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            return RethrowExceptionsAsIOException(() => PortableShim.FileStream.Create(fullPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.Read, 4096, PortableShim.FileOptions.Asynchronous));
        }

        internal static T RethrowExceptionsAsIOException<T>(Func<T> operation)
        {
            try
            {
                return operation();
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

        /// <summary>
        /// Used to create a file given a path specified by the user.
        /// paramName - Provided by the Public surface APIs to have a clearer message. Internal API just rethrow the exception
        /// </summary>
        internal static Stream CreateFileStreamChecked(Func<string, Stream> factory, string path, string paramName = null)
        {
            try
            {
                return factory(path);
            }
            catch (ArgumentNullException)
            {
                if (paramName == null)
                {
                    throw;
                }
                else
                {
                    throw new ArgumentNullException(paramName);
                }
            }
            catch (ArgumentException e)
            {
                if (paramName == null)
                {
                    throw;
                }
                else
                {
                    throw new ArgumentException(e.Message, paramName);
                }
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

        /// <exception cref="IOException"/>
        internal static DateTime GetFileTimeStamp(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            try
            {
                return PortableShim.File.GetLastWriteTimeUtc(fullPath);
            }
            catch (Exception e)
            {
                throw new IOException(e.Message);
            }
        }

        internal static Stream OpenFileStream(string path)
        {
            try
            {
                return PortableShim.File.OpenRead(path);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (IOException e)
            {
                if (e.GetType().Name == "DirectoryNotFoundException")
                {
                    throw new FileNotFoundException(e.Message, path, e);
                }

                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }
    }
}
