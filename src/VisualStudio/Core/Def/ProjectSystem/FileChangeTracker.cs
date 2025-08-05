// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using IVsAsyncFileChangeEx2 = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx2;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal sealed class FileChangeTracker : IVsFreeThreadedFileChangeEvents2, IDisposable
{
    internal const _VSFILECHANGEFLAGS DefaultFileChangeFlags = _VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Size;

    private static readonly AsyncLazy<uint?> s_none = AsyncLazy.Create(value: (uint?)null);

    private readonly IVsFileChangeEx _fileChangeService;
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
    private static readonly object s_lastBackgroundTaskGate = new();

    public FileChangeTracker(IVsFileChangeEx fileChangeService, string filePath, _VSFILECHANGEFLAGS fileChangeFlags = DefaultFileChangeFlags)
    {
        _fileChangeService = fileChangeService;
        FilePath = filePath;
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

    public string FilePath { get; }

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

        _fileChangeCookie = AsyncLazy.Create(
            static async (self, cancellationToken) =>
            {
                try
                {
                    // TODO: Should we pass in cancellationToken here instead of CancellationToken.None?
                    uint? result = await ((IVsAsyncFileChangeEx2)self._fileChangeService).AdviseFileChangeAsync(self.FilePath, self._fileChangeFlags, self, CancellationToken.None).ConfigureAwait(false);
                    return result;
                }
                catch (Exception e) when (ReportException(e))
                {
                    return null;
                }
            },
            static (self, cancellationToken) =>
            {
                try
                {
                    Marshal.ThrowExceptionForHR(
                        self._fileChangeService.AdviseFileChange(self.FilePath, (uint)self._fileChangeFlags, self, out var newCookie));
                    return newCookie;
                }
                catch (Exception e) when (ReportException(e))
                {
                    return null;
                }
            },
            arg: this);

        lock (s_lastBackgroundTaskGate)
        {
            s_lastBackgroundTask = s_lastBackgroundTask.ContinueWith(_ => _fileChangeCookie.GetValueAsync(CancellationToken.None), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            return s_lastBackgroundTask;
        }
    }

    private static bool ReportException(Exception e)
    {
        // If we got a PathTooLongException there's really nothing we can do about it; we will fail to read the file later which is fine
        if (e is not PathTooLongException)
        {
            return FatalError.ReportAndCatch(e);
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
