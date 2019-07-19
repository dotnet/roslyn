using System;
using System.Collections.Generic;
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
        {
            _taskQueue = fileChangeService;
        }

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

        public IContext CreateContext()
        {
            return new Context(this, null);
        }

        /// <summary>
        /// Creates an <see cref="IContext"/> that watches all files in a directory, in addition to any files explicitly requested by <see cref="IContext.EnqueueWatchingFile(string)"/>.
        /// </summary>
        public IContext CreateContextForDirectory(string directoryFilePath)
        {
            if (directoryFilePath == null)
            {
                throw new ArgumentNullException(nameof(directoryFilePath));
            }

            return new Context(this, directoryFilePath);
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
            private readonly string _directoryFilePathOpt;
            private readonly IFileWatchingToken _noOpFileWatchingToken;

            /// <summary>
            /// Gate to guard mutable fields in this class and any mutation of any <see cref="FileWatchingToken"/>s.
            /// </summary>
            private readonly object _gate = new object();
            private bool _disposed = false;
            private readonly HashSet<FileWatchingToken> _activeFileWatchingTokens = new HashSet<FileWatchingToken>();
            private uint _directoryWatchCookie;

            public Context(FileChangeWatcher fileChangeWatcher, string directoryFilePath)
            {
                _fileChangeWatcher = fileChangeWatcher;
                _noOpFileWatchingToken = new FileWatchingToken();

                if (directoryFilePath != null)
                {
                    if (!directoryFilePath.EndsWith("\\"))
                    {
                        directoryFilePath += "\\";
                    }

                    _directoryFilePathOpt = directoryFilePath;

                    _fileChangeWatcher.EnqueueWork(
                        async service =>
                        {
                            _directoryWatchCookie = await service.AdviseDirChangeAsync(_directoryFilePathOpt, watchSubdirectories: true, this).ConfigureAwait(false);
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
                        // Since we put all of our work in a queue, we know that if we had tried to advise file or directory changes,
                        // it must have happened before now
                        if (_directoryFilePathOpt != null)
                        {
                            await service.UnadviseDirChangeAsync(_directoryWatchCookie).ConfigureAwait(false);
                        }

                        // it runs after disposed. so no lock is needed for _activeFileWatchingTokens
                        foreach (var token in _activeFileWatchingTokens)
                        {
                            await UnsubscribeFileChangeEventsAsync(service, token).ConfigureAwait(false);
                        }
                    });
            }

            public IFileWatchingToken EnqueueWatchingFile(string filePath)
            {
                // If we already have this file under our path, we don't have to do additional watching
                if (_directoryFilePathOpt != null && filePath.StartsWith(_directoryFilePathOpt))
                {
                    return _noOpFileWatchingToken;
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
            {
                return service.UnadviseFileChangeAsync(typedToken.Cookie.Value);
            }

            public event EventHandler<string> FileChanged;

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
            {
                return VSConstants.E_NOTIMPL;
            }

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
