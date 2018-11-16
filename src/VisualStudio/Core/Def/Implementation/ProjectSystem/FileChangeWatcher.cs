using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// A service that wraps the Visual Studio file watching APIs to make them more convenient for use. With this, a consumer can create
    /// an <see cref="IContext"/> which lets you add/remove files being watched, and an event is raised when a file is modified.
    /// </summary>
    internal sealed class FileChangeWatcher
    {
        /// <summary>
        /// Gate that is used to guard modifications to <see cref="_taskQueue"/>.
        /// </summary>
        private readonly object _taskQueueGate = new object();

        /// <summary>
        /// We create a queue of tasks against the IVsFileChangeEx service for two reasons. First, we are obtaining the service asynchronously, and don't want to
        /// block on it being available, so anybody who wants to do anything must wait for it. Secondly, the service itself is single-threaded; in the past
        /// we've blocked up a bunch of threads all trying to use it at once. If the latter ever changes, we probably want to reconsider the implementation of this.
        /// For performance and correctness reasons, NOTHING should ever do a block on this; figure out how to do your work without a block and add any work to
        /// the end of the queue.
        /// </summary>
        private Task<IVsFileChangeEx> _taskQueue;
        private static readonly Func<Task<IVsFileChangeEx>, object, IVsFileChangeEx> _executeActionDelegate =
            (precedingTask, state) => { ((Action<IVsFileChangeEx>)state)(precedingTask.Result); return precedingTask.Result; };

        public FileChangeWatcher(Task<IVsFileChangeEx> fileChangeService)
        {
            _taskQueue = fileChangeService;
        }

        private void EnqueueWork(Action<IVsFileChangeEx> action)
        {
            lock (_taskQueueGate)
            {
                _taskQueue = _taskQueue.ContinueWith(
                    _executeActionDelegate,
                    action,
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);
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

        private sealed class Context : IVsFreeThreadedFileChangeEvents, IContext
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
                        directoryFilePath = directoryFilePath + "\\";
                    }

                    _directoryFilePathOpt = directoryFilePath;

                    _fileChangeWatcher.EnqueueWork(
                        service => { ErrorHandler.ThrowOnFailure(service.AdviseDirChange(_directoryFilePathOpt, fWatchSubDir: 1, this, out _directoryWatchCookie)); });
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
                    service =>
                    {
                        // Since we put all of our work in a queue, we know that if we had tried to advise file or directory changes,
                        // it must have happened before now
                        if (_directoryFilePathOpt != null)
                        {
                            ErrorHandler.ThrowOnFailure(service.UnadviseDirChange(_directoryWatchCookie));
                        }

                        // it runs after disposed. so no lock is needed for _activeFileWatchingTokens
                        foreach (var token in _activeFileWatchingTokens)
                        {
                            UnsubscribeFileChangeEvents(service, token);
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

                _fileChangeWatcher.EnqueueWork(service =>
                {
                    uint cookie;
                    ErrorHandler.ThrowOnFailure(service.AdviseFileChange(filePath, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time), this, out cookie));

                    token.Cookie = cookie;
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

                _fileChangeWatcher.EnqueueWork(service => UnsubscribeFileChangeEvents(service, typedToken));
            }

            private void UnsubscribeFileChangeEvents(IVsFileChangeEx service, FileWatchingToken typedToken)
            {
                ErrorHandler.ThrowOnFailure(service.UnadviseFileChange(typedToken.Cookie.Value));
            }

            public event EventHandler<string> FileChanged;

            int IVsFreeThreadedFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (int i = 0; i < cChanges; i++)
                {
                    FileChanged?.Invoke(this, rgpszFile[i]);
                }

                return VSConstants.S_OK;
            }

            int IVsFreeThreadedFileChangeEvents.DirectoryChanged(string pszDirectory)
            {
                return VSConstants.E_NOTIMPL;
            }

            int IVsFreeThreadedFileChangeEvents.DirectoryChangedEx(string pszDirectory, string pszFile)
            {
                FileChanged?.Invoke(this, pszFile);

                return VSConstants.S_OK;
            }

            int IVsFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (int i = 0; i < cChanges; i++)
                {
                    FileChanged?.Invoke(this, rgpszFile[i]);
                }

                return VSConstants.S_OK;
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
        }
    }
}
