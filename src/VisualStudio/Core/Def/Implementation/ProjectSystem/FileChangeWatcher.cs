// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// A service that wraps the Visual Studio file watching APIs to make them more convenient for use. With this, a consumer can create
    /// an <see cref="IContext"/> which lets you add/remove files being watched, and an event is raised when a file is modified.
    /// </summary>
    internal sealed class FileChangeWatcher
    {
        internal const uint FileChangeFlags = (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Size);

        /// <summary>
        /// Gate that is used to guard modifications to <see cref="_taskQueue"/>.
        /// </summary>
        private readonly object _taskQueueGate = new object();

        /// <summary>
        /// We create a queue of tasks against the IVsFileChangeEx service for two reasons. First, we are obtaining the service asynchronously, and don't want to
        /// block on it being available, so anybody who wants to do anything must wait for it. Secondly, the service itself is single-threaded; the entry points
        /// are asynchronous so we avoid starving the thread pool, but there's still no reason to create a lot more work blocked than needed. Finally, since this
        /// is all happening async, we generally need to ensure that an operation that happens for an earlier call to this is done before we do later calls. For example,
        /// if we started a subscription for a file, we need to make sure that's done before we try to unsubscribe from it.
        /// For performance and correctness reasons, NOTHING should ever do a block on this; figure out how to do your work without a block and add any work to
        /// the end of the queue.
        /// </summary>
        private Task<IVsAsyncFileChangeEx> _taskQueue;
        private static readonly Func<Task<IVsAsyncFileChangeEx>, object, Task<IVsAsyncFileChangeEx>> _executeActionDelegate =
            async (precedingTask, state) =>
            {
                var action = (Func<IVsAsyncFileChangeEx, Task>)state;
                await action(precedingTask.Result).ConfigureAwait(false);
                return precedingTask.Result;
            };

        public FileChangeWatcher(Task<IVsAsyncFileChangeEx> fileChangeService)
            => _taskQueue = fileChangeService;

        private void EnqueueWork(Func<IVsAsyncFileChangeEx, Task> action)
        {
            lock (_taskQueueGate)
            {
                _taskQueue = _taskQueue.ContinueWith(
                    _executeActionDelegate,
                    action,
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default).Unwrap();
            }
        }

        // TODO: remove this when there is a mechanism for a caller of EnqueueWatchingFile
        // to explicitly wait on that being complete.
        public void WaitForQueue_TestOnly()
        {
            Task queue;

            lock (_taskQueueGate)
            {
                queue = _taskQueue;
            }

            queue.Wait();
        }

        public IContext CreateContext(params WatchedDirectory[] watchedDirectories)
        {
            return new Context(this, watchedDirectories.ToImmutableArray());
        }

        /// <summary>
        /// Gives a hint to the <see cref="IContext"/> that we should watch a top-level directory for all changes in addition
        /// to any files called by <see cref="IContext.EnqueueWatchingFile(string)"/>.
        /// </summary>
        /// <remarks>
        /// This is largely intended as an optimization; consumers should still call <see cref="IContext.EnqueueWatchingFile(string)" />
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
        /// <remarks>This is only implemented today by <see cref="Context"/> but we don't want to leak implementation details out.</remarks>
        public interface IContext : IDisposable
        {
            /// <summary>
            /// Raised when a file has been changed. This may be a file watched explicitly by <see cref="EnqueueWatchingFile(string)"/> or it could be any
            /// file in the directory if the <see cref="IContext"/> was watching a directory.
            /// </summary>
            event EventHandler<string> FileChanged;

            /// <summary>
            /// Starts watching a file but doesn't wait for the file watcher to be registered with the operating system. Good if you know
            /// you'll need a file watched (eventually) but it's not worth blocking yet.
            /// </summary>
            IFileWatchingToken EnqueueWatchingFile(string filePath);

            void StopWatchingFile(IFileWatchingToken token);
        }

        /// <summary>
        /// A marker interface for tokens returned from <see cref="IContext.EnqueueWatchingFile(string)"/>. This is just to ensure type safety and avoid
        /// leaking the full surface area of the nested types.
        /// </summary>
        public interface IFileWatchingToken
        {
        }

        private sealed class Context : IVsFreeThreadedFileChangeEvents2, IContext
        {
            private readonly FileChangeWatcher _fileChangeWatcher;
            private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
            private readonly IFileWatchingToken _noOpFileWatchingToken;

            /// <summary>
            /// Gate to guard mutable fields in this class and any mutation of any <see cref="FileWatchingToken"/>s.
            /// </summary>
            private readonly object _gate = new object();
            private bool _disposed = false;
            private readonly HashSet<FileWatchingToken> _activeFileWatchingTokens = new HashSet<FileWatchingToken>();

            /// <summary>
            /// The list of cookies we used to make watchers for <see cref="_watchedDirectories"/>.
            /// </summary>
            /// <remarks>
            /// This does not need to be used under <see cref="_gate"/>, as it's only used inside the actual queue of file watcher
            /// actions.
            /// </remarks>
            private readonly List<uint> _directoryWatchCookies = new List<uint>();

            public Context(FileChangeWatcher fileChangeWatcher, ImmutableArray<WatchedDirectory> watchedDirectories)
            {
                _fileChangeWatcher = fileChangeWatcher;
                _watchedDirectories = watchedDirectories;
                _noOpFileWatchingToken = new FileWatchingToken();

                foreach (var watchedDirectory in watchedDirectories)
                {
                    _fileChangeWatcher.EnqueueWork(
                        async service =>
                        {
                            var cookie = await service.AdviseDirChangeAsync(watchedDirectory.Path, watchSubdirectories: true, this).ConfigureAwait(false);
                            _directoryWatchCookies.Add(cookie);

                            if (watchedDirectory.ExtensionFilter != null)
                            {
                                await service.FilterDirectoryChangesAsync(cookie, new string[] { watchedDirectory.ExtensionFilter }, CancellationToken.None).ConfigureAwait(false);
                            }
                        });
                }
            }

            public void Dispose()
            {
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                }

                _fileChangeWatcher.EnqueueWork(
                    async service =>
                    {
                        // This cleanup code all runs in the single queue that we push usages of the file change service into.
                        // Therefore, we know that any advise operations we had done have ran in that queue by now. Since this is also
                        // running after dispose, we don't need to take any locks at this point, since we're taking the general policy
                        // that any use of the type after it's been disposed is simply undefined behavior.

                        // We don't use IAsyncDisposable here simply because we don't ever want to block on the queue if we're
                        // able to avoid it, since that would potentially cause a stall or UI delay on shutting down.

                        foreach (var cookie in _directoryWatchCookies)
                        {
                            await service.UnadviseDirChangeAsync(cookie).ConfigureAwait(false);
                        }

                        // Since this runs after disposal, no lock is needed for _activeFileWatchingTokens
                        foreach (var token in _activeFileWatchingTokens)
                        {
                            await UnsubscribeFileChangeEventsAsync(service, token).ConfigureAwait(false);
                        }
                    });
            }

            public IFileWatchingToken EnqueueWatchingFile(string filePath)
            {
                // If we already have this file under our path, we may not have to do additional watching
                foreach (var watchedDirectory in _watchedDirectories)
                {
                    if (watchedDirectory != null && filePath.StartsWith(watchedDirectory.Path))
                    {
                        // If ExtensionFilter is null, then we're watching for all files in the directory so the prior check
                        // of the directory containment was sufficient. If it isn't null, then we have to check the extension
                        // matches.
                        if (watchedDirectory.ExtensionFilter == null || filePath.EndsWith(watchedDirectory.ExtensionFilter))
                        {
                            return _noOpFileWatchingToken;
                        }
                    }
                }

                var token = new FileWatchingToken();

                lock (_gate)
                {
                    _activeFileWatchingTokens.Add(token);
                }

                _fileChangeWatcher.EnqueueWork(async service =>
                {
                    token.Cookie = await service.AdviseFileChangeAsync(filePath, _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time, this).ConfigureAwait(false);
                });

                return token;
            }

            public void StopWatchingFile(IFileWatchingToken token)
            {
                var typedToken = token as FileWatchingToken;

                Contract.ThrowIfNull(typedToken, "The token passed did not originate from this service.");

                if (typedToken == _noOpFileWatchingToken)
                {
                    // This file never required a direct file watch, our main subscription covered it.
                    return;
                }

                lock (_gate)
                {
                    Contract.ThrowIfFalse(_activeFileWatchingTokens.Remove(typedToken), "This token was no longer being watched.");
                }

                _fileChangeWatcher.EnqueueWork(service => UnsubscribeFileChangeEventsAsync(service, typedToken));
            }

            private Task UnsubscribeFileChangeEventsAsync(IVsAsyncFileChangeEx service, FileWatchingToken typedToken)
                => service.UnadviseFileChangeAsync(typedToken.Cookie!.Value);

            public event EventHandler<string>? FileChanged;

            int IVsFreeThreadedFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (var i = 0; i < cChanges; i++)
                {
                    FileChanged?.Invoke(this, rgpszFile[i]);
                }

                return VSConstants.S_OK;
            }

            int IVsFreeThreadedFileChangeEvents.DirectoryChanged(string pszDirectory)
            {
                Debug.Fail("Since we're implementing IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2, this should not be called.");
                return VSConstants.E_NOTIMPL;
            }

            int IVsFreeThreadedFileChangeEvents2.DirectoryChanged(string pszDirectory)
            {
                Debug.Fail("Since we're implementing IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2, this should not be called.");
                return VSConstants.E_NOTIMPL;
            }

            int IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx(string pszDirectory, string pszFile)
            {
                Debug.Fail("Since we're implementing IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2, this should not be called.");
                return VSConstants.E_NOTIMPL;
            }

            int IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (var i = 0; i < cChanges; i++)
                {
                    FileChanged?.Invoke(this, rgpszFile[i]);
                }

                return VSConstants.S_OK;
            }

            int IVsFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                Debug.Fail("Since we're implementing IVsFreeThreadedFileChangeEvents2.FilesChanged, this should not be called.");
                return VSConstants.E_NOTIMPL;
            }

            int IVsFileChangeEvents.DirectoryChanged(string pszDirectory)
                => VSConstants.E_NOTIMPL;

            public class FileWatchingToken : IFileWatchingToken
            {
                /// <summary>
                /// The cookie we have for requesting a watch on this file. Any files that didn't need
                /// to be watched specifically are equal to <see cref="_noOpFileWatchingToken"/>, so
                /// any other instance is something that should be watched. Null means we either haven't
                /// done the subscription (and it's still in the queue) or we had some sort of error
                /// subscribing in the first place.
                /// </summary>
                public uint? Cookie;
            }

            int IVsFreeThreadedFileChangeEvents2.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (var i = 0; i < cChanges; i++)
                {
                    FileChanged?.Invoke(this, rgpszFile[i]);
                }

                return VSConstants.S_OK;
            }

            int IVsFreeThreadedFileChangeEvents.DirectoryChangedEx(string pszDirectory, string pszFile)
            {
                Debug.Fail("Since we're implementing IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2, this should not be called.");
                return VSConstants.E_NOTIMPL;
            }
        }
    }
}
