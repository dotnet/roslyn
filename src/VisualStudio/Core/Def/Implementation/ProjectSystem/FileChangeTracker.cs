// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class FileChangeTracker : IVsFileChangeEvents, IDisposable
    {
        private const uint FileChangeFlags = (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Size);

        private static readonly Lazy<uint?> s_none = new Lazy<uint?>(() => null, LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly IVsFileChangeEx _fileChangeService;
        private readonly string _filePath;
        private bool _disposed;

        /// <summary>
        /// The cookie received from the IVsFileChangeEx interface that is watching for changes to
        /// this file. This field may never be null, but might be a Lazy that has a value of null if
        /// we either failed to subscribe over never have tried to subscribe.
        /// </summary>
        private Lazy<uint?> _fileChangeCookie;

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

        public FileChangeTracker(IVsFileChangeEx fileChangeService, string filePath)
        {
            _fileChangeService = fileChangeService;
            _filePath = filePath;
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

        public void AssertUnsubscription()
        {
            // We must have been disposed properly.
            Contract.ThrowIfTrue(_fileChangeCookie != s_none);
        }

        public void EnsureSubscription()
        {
            // make sure we have file notification subscribed
            var unused = _fileChangeCookie.Value;
        }

        public void StartFileChangeListeningAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileChangeTracker));
            }

            Contract.ThrowIfTrue(_fileChangeCookie != s_none);

            _fileChangeCookie = new Lazy<uint?>(() =>
            {
                try
                {
                    Marshal.ThrowExceptionForHR(
                        _fileChangeService.AdviseFileChange(_filePath, FileChangeFlags, this, out var newCookie));
                    return newCookie;
                }
                catch (Exception e) when (ReportException(e))
                {
                    return null;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            lock (s_lastBackgroundTaskGate)
            {
                s_lastBackgroundTask = s_lastBackgroundTask.ContinueWith(_ => _fileChangeCookie.Value, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
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

        public void StopFileChangeListening()
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

            var fileChangeCookie = _fileChangeCookie.Value;
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
        {
            throw new Exception("We only watch files; we should never be seeing directory changes!");
        }

        int IVsFileChangeEvents.FilesChanged(uint changeCount, string[] files, uint[] changes)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }
    }
}
