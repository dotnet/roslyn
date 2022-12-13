// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ProjectSystem
{
    internal interface IFileChangeWatcher
    {
        IFileChangeContext CreateContext(params WatchedDirectory[] watchedDirectories);
    }

    /// <summary>
    /// Gives a hint to the <see cref="IFileChangeContext"/> that we should watch a top-level directory for all changes in addition
    /// to any files called by <see cref="IFileChangeContext.EnqueueWatchingFile(string)"/>.
    /// </summary>
    /// <remarks>
    /// This is largely intended as an optimization; consumers should still call <see cref="IFileChangeContext.EnqueueWatchingFile(string)" />
    /// for files they want to watch. This allows the caller to give a hint that it is expected that most of the files being
    /// watched is under this directory, and so it's more efficient just to watch _all_ of the changes in that directory
    /// rather than creating and tracking a bunch of file watcher state for each file separately. A good example would be
    /// just creating a single directory watch on the root of a project for source file changes: rather than creating a file watcher
    /// for each individual file, we can just watch the entire directory and that's it.
    /// </remarks>
    public sealed class WatchedDirectory
    {
        public WatchedDirectory(string path, string? extensionFilter)
        {
            // We are doing string comparisons with this path, so ensure it has a trailing \ so we don't get confused with sibling
            // paths that won't actually be covered.
            if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                path += System.IO.Path.DirectorySeparatorChar;
            }

            if (extensionFilter != null && !extensionFilter.StartsWith("."))
            {
                throw new ArgumentException($"{nameof(extensionFilter)} should start with a period.", nameof(extensionFilter));
            }

            Path = path;
            ExtensionFilter = extensionFilter;
        }

        public string Path { get; }

        /// <summary>
        /// If non-null, only watch the directory for changes to a specific extension. String always starts with a period.
        /// </summary>
        public string? ExtensionFilter { get; }
    }

    /// <summary>
    /// A context that is watching one or more files.
    /// </summary>
    public interface IFileChangeContext : IDisposable
    {
        /// <summary>
        /// Raised when a file has been changed. This may be a file watched explicitly by <see cref="EnqueueWatchingFile(string)"/> or it could be any
        /// file in the directory if the <see cref="IFileChangeContext"/> was watching a directory.
        /// </summary>
        event EventHandler<string> FileChanged;

        /// <summary>
        /// Starts watching a file but doesn't wait for the file watcher to be registered with the operating system. Good if you know
        /// you'll need a file watched (eventually) but it's not worth blocking yet.
        /// </summary>
        IWatchedFile EnqueueWatchingFile(string filePath);
    }

    public interface IWatchedFile : IDisposable
    {
    }
}
