// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
        private readonly Task<IVsAsyncFileChangeEx> _fileChangeService;

        /// <summary>
        /// We create a batching queue of operations against the IVsFileChangeEx service for two reasons. First, we are obtaining the service asynchronously, and don't want to
        /// block on it being available, so anybody who wants to do anything must wait for it. Secondly, the service itself is single-threaded; the entry points
        /// are asynchronous so we avoid starving the thread pool, but there's still no reason to create a lot more work blocked than needed. Finally, since this
        /// is all happening async, we generally need to ensure that an operation that happens for an earlier call to this is done before we do later calls. For example,
        /// if we started a subscription for a file, we need to make sure that's done before we try to unsubscribe from it.
        /// For performance and correctness reasons, NOTHING should ever do a block on this; figure out how to do your work without a block and add any work to
        /// the end of the queue.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<WatcherOperation> _taskQueue;

        public FileChangeWatcher(
            IAsynchronousOperationListenerProvider listenerProvider,
            Task<IVsAsyncFileChangeEx> fileChangeService)
        {
            _fileChangeService = fileChangeService;

            // 📝 Empirical testing during high activity (e.g. solution close) showed strong batching performance even
            // though the batching delay is 0.
            _taskQueue = new AsyncBatchingWorkQueue<WatcherOperation>(
                TimeSpan.Zero,
                ProcessBatchAsync,
                listenerProvider.GetListener(FeatureAttribute.Workspace),
                CancellationToken.None);
        }

        private async ValueTask ProcessBatchAsync(ImmutableSegmentedList<WatcherOperation> workItems, CancellationToken cancellationToken)
        {
            var service = await _fileChangeService.ConfigureAwait(false);

            var prior = WatcherOperation.Empty;
            for (var i = 0; i < workItems.Count; i++)
            {
                if (prior.TryCombineWith(workItems[i], out var combined))
                {
                    prior = combined;
                    continue;
                }

                // The current item can't be combined with the prior item. Process the prior item before marking the
                // current item as the new prior item.
                await prior.ApplyAsync(service, cancellationToken).ConfigureAwait(false);
                prior = workItems[i];
            }

            // The last item is always stored in prior rather than processing it directly. Make sure to process it
            // before returning from the batch.
            await prior.ApplyAsync(service, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Represents an operation to subscribe or unsubscribe from <see cref="IVsAsyncFileChangeEx"/> events. The
        /// values of the fields depends on the <see cref="_kind"/> of the particular instance.
        /// </summary>
        private readonly struct WatcherOperation
        {
            /// <summary>
            /// The kind of the watcher operation. The values of individual fields depends on the kind.
            /// </summary>
            private readonly Kind _kind;

            /// <summary>
            /// The path to subscribe to for <see cref="Kind.WatchDirectory"/> or <see cref="Kind.WatchFile"/>.
            /// </summary>
            private readonly string _directory;

            /// <summary>
            /// The extension filter to apply for <see cref="Kind.WatchDirectory"/>. This value may be
            /// <see langword="null"/> to disable the extension filter.
            /// </summary>
            private readonly string? _filter;

            /// <summary>
            /// The file change flags to apply for <see cref="Kind.WatchFile"/>.
            /// </summary>
            private readonly _VSFILECHANGEFLAGS _fileChangeFlags;

            /// <summary>
            /// The instance to receive callback events for <see cref="Kind.WatchDirectory"/> or
            /// <see cref="Kind.WatchFile"/>.
            /// </summary>
            private readonly IVsFreeThreadedFileChangeEvents2 _sink;

            /// <summary>
            /// The collection holding cookies. For <see cref="Kind.WatchDirectory"/>, the operation will add the
            /// resulting cookie to this collection. For <see cref="Kind.UnwatchDirectories"/>, the operation will
            /// unsubscribe to all cookies in the collection.
            /// </summary>
            /// <remarks>
            /// ⚠ Do not change this to another collection like <c>ImmutableList&lt;uint&gt;</c>. This collection
            /// references an instance held by the <see cref="Context"/> class, and the values are lazily read when
            /// <see cref="TryCombineWith"/> is called.
            /// </remarks>
            private readonly List<uint> _cookies;

            /// <summary>
            /// A file watcher token. The <see cref="Context.FileWatchingToken.Cookie"/> field is assigned by the
            /// operation for <see cref="Kind.WatchFile"/>, or read by the operation for <see cref="Kind.UnwatchFile"/>.
            /// </summary>
            private readonly Context.FileWatchingToken _token;

            /// <summary>
            /// A collection of file watcher tokens to remove for <see cref="Kind.UnwatchFiles"/>.
            /// </summary>
            private readonly IEnumerable<Context.FileWatchingToken> _tokens;

            private WatcherOperation(Kind kind)
            {
                Contract.ThrowIfFalse(kind is Kind.None);
                _kind = kind;

                // Other watching fields are not used for this kind
                _directory = null!;
                _filter = null;
                _fileChangeFlags = 0;
                _sink = null!;
                _cookies = null!;
                _token = null!;
                _tokens = null!;
            }

            private WatcherOperation(Kind kind, string directory, string? filter, IVsFreeThreadedFileChangeEvents2 sink, List<uint> cookies)
            {
                Contract.ThrowIfFalse(kind is Kind.WatchDirectory);
                _kind = kind;

                _directory = directory;
                _filter = filter;
                _sink = sink;
                _cookies = cookies;

                // Other watching fields are not used for this kind
                _fileChangeFlags = 0;
                _token = null!;
                _tokens = null!;
            }

            private WatcherOperation(Kind kind, string path, _VSFILECHANGEFLAGS fileChangeFlags, IVsFreeThreadedFileChangeEvents2 sink, Context.FileWatchingToken token)
            {
                Contract.ThrowIfFalse(kind is Kind.WatchFile);
                _kind = kind;

                _directory = path;
                _fileChangeFlags = fileChangeFlags;
                _sink = sink;
                _token = token;

                // Other watching fields are not used for this kind
                _filter = null;
                _cookies = null!;
                _tokens = null!;
            }

            private WatcherOperation(Kind kind, List<uint> cookies)
            {
                Contract.ThrowIfFalse(kind is Kind.UnwatchDirectories);
                _kind = kind;

                _cookies = cookies;

                // Other watching fields are not used for this kind
                _directory = null!;
                _filter = null;
                _fileChangeFlags = 0;
                _sink = null!;
                _token = null!;
                _tokens = null!;
            }

            private WatcherOperation(Kind kind, IEnumerable<Context.FileWatchingToken> tokens)
            {
                Contract.ThrowIfFalse(kind is Kind.UnwatchFiles);
                _kind = kind;

                _tokens = tokens;

                // Other watching fields are not used for this kind
                _directory = null!;
                _filter = null;
                _fileChangeFlags = 0;
                _sink = null!;
                _cookies = null!;
                _token = null!;
            }

            private WatcherOperation(Kind kind, Context.FileWatchingToken token)
            {
                Contract.ThrowIfFalse(kind is Kind.UnwatchFile);
                _kind = kind;

                _token = token;

                // Other watching fields are not used for this kind
                _directory = null!;
                _filter = null;
                _fileChangeFlags = 0;
                _sink = null!;
                _cookies = null!;
                _tokens = null!;
            }

            private enum Kind
            {
                None,
                WatchDirectory,
                WatchFile,
                UnwatchFile,
                UnwatchDirectories,
                UnwatchFiles,
            }

            /// <summary>
            /// Represents a watcher operation that takes no action when applied. This value intentionally has the same
            /// representation as <c>default(WatcherOperation)</c>.
            /// </summary>
            public static WatcherOperation Empty => new(Kind.None);

            public static WatcherOperation WatchDirectory(string directory, string? filter, IVsFreeThreadedFileChangeEvents2 sink, List<uint> cookies)
                => new(Kind.WatchDirectory, directory, filter, sink, cookies);

            public static WatcherOperation WatchFile(string path, _VSFILECHANGEFLAGS fileChangeFlags, IVsFreeThreadedFileChangeEvents2 sink, Context.FileWatchingToken token)
                => new(Kind.WatchFile, path, fileChangeFlags, sink, token);

            public static WatcherOperation UnwatchDirectories(List<uint> cookies)
                => new(Kind.UnwatchDirectories, cookies);

            public static WatcherOperation UnwatchFiles(IEnumerable<Context.FileWatchingToken> tokens)
                => new(Kind.UnwatchFiles, tokens);

            public static WatcherOperation UnwatchFile(Context.FileWatchingToken token)
                => new(Kind.UnwatchFile, token);

            /// <summary>
            /// Attempts to combine the current <see cref="WatcherOperation"/> with the next operation in sequence. When
            /// successful, <paramref name="combined"/> is assigned a value which, when applied, performs an operation
            /// equivalent to performing the current instance immediately followed by <paramref name="other"/>.
            /// </summary>
            /// <param name="other">The next operation to apply.</param>
            /// <param name="combined">An operation representing the combined application of the current instance and
            /// <paramref name="other"/>, in that order; otherwise, <see cref="Empty"/> if the current operation cannot
            /// be combined with <paramref name="other"/>.</param>
            /// <returns><see langword="true"/> if the current operation can be combined with <paramref name="other"/>;
            /// otherwise, <see langword="false"/>.</returns>
            public bool TryCombineWith(in WatcherOperation other, out WatcherOperation combined)
            {
                if (other._kind == Kind.None)
                {
                    combined = this;
                    return true;
                }
                else if (_kind == Kind.None)
                {
                    combined = other;
                    return true;
                }

                switch (_kind)
                {
                    case Kind.WatchDirectory:
                    case Kind.WatchFile:
                        // Watching operations cannot be combined
                        break;

                    case Kind.UnwatchFile when other._kind == Kind.UnwatchFile:
                        combined = UnwatchFiles(ImmutableList.Create(_token, other._token));
                        return true;

                    case Kind.UnwatchFile when other._kind == Kind.UnwatchFiles:
                        combined = UnwatchFiles(other._tokens.ToImmutableList().Insert(0, _token));
                        return true;

                    case Kind.UnwatchDirectories when other._kind == Kind.UnwatchDirectories:
                        var cookies = new List<uint>(_cookies);
                        cookies.AddRange(other._cookies);
                        combined = UnwatchDirectories(cookies);
                        return true;

                    case Kind.UnwatchFiles when other._kind == Kind.UnwatchFile:
                        combined = UnwatchFiles(_tokens.ToImmutableList().Add(other._token));
                        return true;

                    case Kind.UnwatchFiles when other._kind == Kind.UnwatchFiles:
                        combined = UnwatchFiles(_tokens.ToImmutableList().AddRange(other._tokens));
                        return true;

                    default:
                        break;
                }

                combined = default;
                return false;
            }

            public async ValueTask ApplyAsync(IVsAsyncFileChangeEx service, CancellationToken cancellationToken)
            {
                switch (_kind)
                {
                    case Kind.None:
                        return;

                    case Kind.WatchDirectory:
                        var cookie = await service.AdviseDirChangeAsync(_directory, watchSubdirectories: true, _sink, cancellationToken).ConfigureAwait(false);
                        _cookies.Add(cookie);

                        if (_filter != null)
                            await service.FilterDirectoryChangesAsync(cookie, new[] { _filter }, cancellationToken).ConfigureAwait(false);

                        return;

                    case Kind.WatchFile:
                        _token.Cookie = await service.AdviseFileChangeAsync(_directory, _fileChangeFlags, _sink, cancellationToken).ConfigureAwait(false);
                        return;

                    case Kind.UnwatchFile:
                        await service.UnadviseFileChangeAsync(_token.Cookie!.Value, cancellationToken).ConfigureAwait(false);
                        return;

                    case Kind.UnwatchDirectories:
                        Contract.ThrowIfFalse(_cookies is not null);
                        await service.UnadviseDirChangesAsync(_cookies, cancellationToken).ConfigureAwait(false);
                        return;

                    case Kind.UnwatchFiles:
                        Contract.ThrowIfFalse(_tokens is not null);
                        await service.UnadviseFileChangesAsync(_tokens.Select(token => token.Cookie!.Value).ToArray(), cancellationToken).ConfigureAwait(false);
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private sealed class Context : IVsFreeThreadedFileChangeEvents2, IContext
        {
            private readonly FileChangeWatcher _fileChangeWatcher;
            private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
            private readonly IFileWatchingToken _noOpFileWatchingToken;

            /// <summary>
            /// Gate to guard mutable fields in this class and any mutation of any <see cref="FileWatchingToken"/>s.
            /// </summary>
            private readonly object _gate = new();
            private bool _disposed = false;
            private readonly HashSet<FileWatchingToken> _activeFileWatchingTokens = new();

            /// <summary>
            /// The list of cookies we used to make watchers for <see cref="_watchedDirectories"/>.
            /// </summary>
            /// <remarks>
            /// This does not need to be used under <see cref="_gate"/>, as it's only used inside the actual queue of file watcher
            /// actions.
            /// </remarks>
            private readonly List<uint> _directoryWatchCookies = new();

            public Context(FileChangeWatcher fileChangeWatcher, ImmutableArray<WatchedDirectory> watchedDirectories)
            {
                _fileChangeWatcher = fileChangeWatcher;
                _watchedDirectories = watchedDirectories;
                _noOpFileWatchingToken = new FileWatchingToken();

                foreach (var watchedDirectory in watchedDirectories)
                {
                    _fileChangeWatcher._taskQueue.AddWork(watchedDirectories.Select(
                        watchedDirectory => WatcherOperation.WatchDirectory(watchedDirectory.Path, watchedDirectory.ExtensionFilter, this, _directoryWatchCookies)));
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

                _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.UnwatchDirectories(_directoryWatchCookies));
                _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.UnwatchFiles(_activeFileWatchingTokens));
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

                _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.WatchFile(filePath, _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time, this, token));

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

                _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.UnwatchFile(typedToken));
            }

            public event EventHandler<string>? FileChanged;

            int IVsFreeThreadedFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                for (var i = 0; i < cChanges; i++)
                {
                    var fileChangeFlags = (_VSFILECHANGEFLAGS)rggrfChange[i];
                    if ((fileChangeFlags & FileChangeTracker.DefaultFileChangeFlags) == 0)
                        continue;

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
                    var fileChangeFlags = (_VSFILECHANGEFLAGS)rggrfChange[i];
                    if ((fileChangeFlags & FileChangeTracker.DefaultFileChangeFlags) == 0)
                        continue;

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
                    var fileChangeFlags = (_VSFILECHANGEFLAGS)rggrfChange[i];
                    if ((fileChangeFlags & FileChangeTracker.DefaultFileChangeFlags) == 0)
                        continue;

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
