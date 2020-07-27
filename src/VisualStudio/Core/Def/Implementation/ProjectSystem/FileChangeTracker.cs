// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class FileChangeTracker : IVsFreeThreadedFileChangeEvents2, IDisposable
    {
        private const _VSFILECHANGEFLAGS DefaultFileChangeFlags = _VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Size;

        private static readonly AsyncLazy<uint?> s_none = new AsyncLazy<uint?>(value: null);

        private readonly IVsFileChangeEx _fileChangeService;
        private readonly string _filePath;
        private readonly _VSFILECHANGEFLAGS _fileChangeFlags;
        private bool _disposed;

        /// <summary>
        /// The cookie received from the IVsFileChangeEx interface that is watching for changes to
        /// this file. This field may never be null, but might be a Lazy that has a value of null if
        /// we either failed to subscribe over never have tried to subscribe.
        /// </summary>
        private AsyncLazy<uint?> _fileChangeCookie;

        public event EventHandler UpdatedOnDisk;

        /// <summary>
        /// Operations on <see cref="IVsFileChangeEx"/> synchronize on a single lock within that service, so there's no point
        /// in us trying to have multiple threads all trying to use it at the same time. When we queue a new background thread operation
        /// we'll just do a continuation after the previous one. Any callers of <see cref="EnsureSubscription"/> will bypass that queue
        /// and ensure it happens quickly.
        /// </summary>
        private static Task s_lastBackgroundTask = Task.CompletedTask;

        /// <summary>
        /// The object to use as a monitor guarding <see cref="s_lastBackgroundTask"/>. This lock is not strictly necessary, since we don't need
        /// to ensure the background tasks happen entirely sequentially -- if we just removed the lock, and two subscriptions happened, we end up with
        /// a 'branching' set of continuations, but that's fine since we're generally not running things in parallel. But it's easy to write,
        /// and easy to delete if this lock has contention itself. Given we tend to call <see cref="StartFileChangeListeningAsync"/> on the UI
        /// thread, I don't expect to see contention.
        /// </summary>
        private static readonly object s_lastBackgroundTaskGate = new object();

        public FileChangeTracker(IVsFileChangeEx fileChangeService, string filePath, _VSFILECHANGEFLAGS fileChangeFlags = DefaultFileChangeFlags)
        {
            _fileChangeService = fileChangeService;
            _filePath = filePath;
            _fileChangeFlags = fileChangeFlags;
            _fileChangeCookie = s_none;
        }

        ~FileChangeTracker()
        {
            if (!Environment.HasShutdownStarted)
            {
                this.AssertUnsubscription();
            }
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        /// <summary>
        /// Returns true if a previous call to <see cref="StartFileChangeListeningAsync"/> has completed.
        /// </summary>
        public bool PreviousCallToStartFileChangeHasAsynchronouslyCompleted
        {
            get
            {
                var cookie = _fileChangeCookie;
                return cookie != s_none && cookie.TryGetValue(out _);
            }
        }

        public void AssertUnsubscription()
        {
            // We must have been disposed properly.
            Contract.ThrowIfTrue(_fileChangeCookie != s_none);
        }

        public void EnsureSubscription()
        {
            // make sure we have file notification subscribed
            _ = _fileChangeCookie.GetValue(CancellationToken.None);
        }

        public Task StartFileChangeListeningAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileChangeTracker));
            }

            Contract.ThrowIfTrue(_fileChangeCookie != s_none);

            _fileChangeCookie = new AsyncLazy<uint?>(async cancellationToken =>
            {
                try
                {
                    return await ((IVsAsyncFileChangeEx)_fileChangeService).AdviseFileChangeAsync(_filePath, _fileChangeFlags, this).ConfigureAwait(false);
                }
                catch (Exception e) when (ReportException(e))
                {
                    return null;
                }
            }, cancellationToken =>
            {
                try
                {
                    Marshal.ThrowExceptionForHR(
                        _fileChangeService.AdviseFileChange(_filePath, (uint)_fileChangeFlags, this, out var newCookie));
                    return newCookie;
                }
                catch (Exception e) when (ReportException(e))
                {
                    return null;
                }
            }, cacheResult: true);

            lock (s_lastBackgroundTaskGate)
            {
                s_lastBackgroundTask = s_lastBackgroundTask.ContinueWith(_ => _fileChangeCookie.GetValueAsync(CancellationToken.None), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
                return s_lastBackgroundTask;
            }
        }

        private static bool ReportException(Exception e)
        {
            // If we got a PathTooLongException there's really nothing we can do about it; we will fail to read the file later which is fine
            if (!(e is PathTooLongException))
            {
                return FatalError.ReportWithoutCrash(e);
            }

            // We'll always capture all exceptions regardless. If we don't, then the exception is captured by our lazy and will be potentially rethrown from
            // StopFileChangeListening or Dispose which causes all sorts of downstream problems.
            return true;
        }

        private void StopFileChangeListening()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileChangeTracker));
            }

            // there is a slight chance that we haven't subscribed to the service yet so we subscribe and unsubscribe
            // both here unnecessarily. but I believe that probably is a theoretical problem and never happen in real life.
            // and even if that happens, it will be just a perf hit
            if (_fileChangeCookie == s_none)
            {
                return;
            }

            var fileChangeCookie = _fileChangeCookie.GetValue(CancellationToken.None);
            _fileChangeCookie = s_none;

            // We may have tried to subscribe but failed, so have to check a second time
            if (fileChangeCookie.HasValue)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(
                        _fileChangeService.UnadviseFileChange(fileChangeCookie.Value));
                }
                catch (Exception e) when (ReportException(e))
                {
                }
            }
        }

        public void Dispose()
        {
            this.StopFileChangeListening();

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        int IVsFileChangeEvents.DirectoryChanged(string directory)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");

        int IVsFileChangeEvents.FilesChanged(uint changeCount, string[] files, uint[] changes)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        int IVsFreeThreadedFileChangeEvents2.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        int IVsFreeThreadedFileChangeEvents2.DirectoryChanged(string pszDirectory)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");

        int IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx(string pszDirectory, string pszFile)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");

        int IVsFreeThreadedFileChangeEvents2.DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");

        int IVsFreeThreadedFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        int IVsFreeThreadedFileChangeEvents.DirectoryChanged(string pszDirectory)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");

        int IVsFreeThreadedFileChangeEvents.DirectoryChangedEx(string pszDirectory, string pszFile)
            => throw new Exception("We only watch files; we should never be seeing directory changes!");
    }
}
