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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using IVsAsyncFileChangeEx2 = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx2;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

/// <summary>
/// A service that wraps the Visual Studio file watching APIs to make them more convenient for use. With this, a consumer can create
/// an <see cref="IFileChangeContext"/> which lets you add/remove files being watched, and an event is raised when a file is modified.
/// </summary>
internal sealed class FileChangeWatcher : IFileChangeWatcher
{
    private readonly Task<IVsAsyncFileChangeEx2> _fileChangeService;

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
        Task<IVsAsyncFileChangeEx2> fileChangeService)
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

    private async ValueTask ProcessBatchAsync(ImmutableSegmentedList<WatcherOperation> operations, CancellationToken cancellationToken)
    {
        var service = await _fileChangeService.ConfigureAwait(false);

        for (var startIndex = 0; startIndex < operations.Count; startIndex++)
        {
            var combinableEndIndex = FindCombinableRange(operations, startIndex);

            var combinedOp = WatcherOperation.CombineRange(operations, startIndex, combinableEndIndex);

            await combinedOp.ApplyAsync(service, cancellationToken).ConfigureAwait(false);

            startIndex = combinableEndIndex;
        }

        return;

        static int FindCombinableRange(ImmutableSegmentedList<WatcherOperation> operations, int startIndex)
        {
            var firstOp = operations[startIndex];
            for (var endIndex = startIndex + 1; endIndex < operations.Count; endIndex++)
            {
                if (!firstOp.CanCombineWith(operations[endIndex]))
                    return endIndex - 1;
            }

            return operations.Count - 1;
        }
    }

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new Context(this, watchedDirectories);

    /// <summary>
    /// Represents an operation to subscribe or unsubscribe from <see cref="IVsAsyncFileChangeEx2"/> events. The
    /// values of the fields depends on the <see cref="_kind"/> of the particular instance.
    /// </summary>
    private readonly struct WatcherOperation
    {
        /// <summary>
        /// The kind of the watcher operation. The values of individual fields depends on the kind.
        /// </summary>
        private readonly Kind _kind;

        /// <summary>
        /// The extension filter to apply for <see cref="Kind.WatchDirectory"/>. This value may be
        /// <see langword="null"/> to disable the extension filter.
        /// </summary>
        private readonly string? _filter;

        /// <summary>
        /// The file change flags to apply for <see cref="Kind.WatchFiles"/>.
        /// </summary>
        private readonly _VSFILECHANGEFLAGS _fileChangeFlags;

        /// <summary>
        /// The instance to receive callback events for <see cref="Kind.WatchDirectory"/> or
        /// <see cref="Kind.WatchFiles"/>.
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
        /// <see cref="CombineRange"/> is called.
        /// </remarks>
        private readonly List<uint> _cookies;

        /// <summary>
        /// A collection of file watcher tokens. The <see cref="Context.RegularWatchedFile.Cookie"/> field is
        /// assigned by the operation for <see cref="Kind.WatchFiles"/>, or read by the operation for
        /// <see cref="Kind.UnwatchFiles"/>.
        /// </summary>
        private readonly OneOrMany<Context.RegularWatchedFile> _tokens;

        /// <summary>
        /// A collection of file paths to subscribe to for <see cref="Kind.WatchFiles"/> or
        /// <see cref="Kind.WatchDirectory"/>.
        /// </summary>
        private readonly OneOrMany<string> _paths;

        private WatcherOperation(Kind kind, string directory, string? filter, IVsFreeThreadedFileChangeEvents2 sink, List<uint> cookies)
        {
            Contract.ThrowIfFalse(kind is Kind.WatchDirectory);
            _kind = kind;

            _paths = new OneOrMany<string>(directory);
            _filter = filter;
            _sink = sink;
            _cookies = cookies;

            // Other watching fields are not used for this kind
            _fileChangeFlags = 0;
            _tokens = OneOrMany<Context.RegularWatchedFile>.Empty;
        }

        private WatcherOperation(Kind kind, OneOrMany<string> files, _VSFILECHANGEFLAGS fileChangeFlags, IVsFreeThreadedFileChangeEvents2 sink, OneOrMany<Context.RegularWatchedFile> tokens)
        {
            Contract.ThrowIfFalse(kind is Kind.WatchFiles);
            _kind = kind;

            _paths = files;
            _fileChangeFlags = fileChangeFlags;
            _sink = sink;
            _tokens = tokens;

            // Other watching fields are not used for this kind
            _filter = null;
            _cookies = null!;
        }

        private WatcherOperation(Kind kind, List<uint> cookies)
        {
            Contract.ThrowIfFalse(kind is Kind.UnwatchDirectories);
            _kind = kind;

            _cookies = cookies;

            // Other watching fields are not used for this kind
            _filter = null;
            _fileChangeFlags = 0;
            _sink = null!;
            _tokens = OneOrMany<Context.RegularWatchedFile>.Empty;
            _paths = OneOrMany<string>.Empty;
        }

        private WatcherOperation(Kind kind, OneOrMany<Context.RegularWatchedFile> tokens)
        {
            Contract.ThrowIfFalse(kind is Kind.UnwatchFiles);
            _kind = kind;

            _tokens = tokens;

            // Other watching fields are not used for this kind
            _filter = null;
            _fileChangeFlags = 0;
            _sink = null!;
            _cookies = null!;
            _paths = OneOrMany<string>.Empty;
        }

        private enum Kind
        {
            WatchDirectory,
            WatchFiles,
            UnwatchDirectories,
            UnwatchFiles,
        }

        public static WatcherOperation WatchDirectory(string directory, string? filter, IVsFreeThreadedFileChangeEvents2 sink, List<uint> cookies)
            => new(Kind.WatchDirectory, directory, filter, sink, cookies);

        public static WatcherOperation WatchFile(string path, _VSFILECHANGEFLAGS fileChangeFlags, IVsFreeThreadedFileChangeEvents2 sink, Context.RegularWatchedFile token)
            => new(Kind.WatchFiles, OneOrMany.Create(path), fileChangeFlags, sink, OneOrMany.Create(token));

        public static WatcherOperation WatchFiles(ImmutableArray<string> files, _VSFILECHANGEFLAGS fileChangeFlags, IVsFreeThreadedFileChangeEvents2 sink, ImmutableArray<Context.RegularWatchedFile> tokens)
            => new(Kind.WatchFiles, new OneOrMany<string>(files), fileChangeFlags, sink, new OneOrMany<Context.RegularWatchedFile>(tokens));

        public static WatcherOperation UnwatchDirectories(List<uint> cookies)
            => new(Kind.UnwatchDirectories, cookies);

        public static WatcherOperation UnwatchFiles(ImmutableArray<Context.RegularWatchedFile> tokens)
            => new(Kind.UnwatchFiles, new OneOrMany<Context.RegularWatchedFile>(tokens));

        public static WatcherOperation UnwatchFile(Context.RegularWatchedFile token)
            => new(Kind.UnwatchFiles, OneOrMany.Create(token));

        /// <summary>
        /// Combines <see cref="WatcherOperation"/> instances between <paramref name="start"/> and <paramref name="end"/>
        /// in <paramref name="operations"/>. This input is assumed to have been pre-verified that all operations
        /// within this range are combinable. The resultant value, when applied, performs an operation
        /// equivalent to performing the specified range of operations consecutively.
        /// </summary>
        /// <param name="operations">The collection containing the operations to combine.</param>
        /// <param name="start">Start index (inclusive) of operations to combine.</param>
        /// <param name="end">End index (inclusive) of operations to combine.</param>
        public static WatcherOperation CombineRange(ImmutableSegmentedList<WatcherOperation> operations, int start, int end)
        {
            var firstOp = operations[start];
            if (start == end)
                return firstOp;

            using var _1 = ArrayBuilder<Context.RegularWatchedFile>.GetInstance(out var tokensBuilder);
            using var _2 = ArrayBuilder<string>.GetInstance(out var fileNamesBuilder);
            using var _3 = ArrayBuilder<uint>.GetInstance(out var cookiesBuilder);

            for (; start <= end; start++)
            {
                var op = operations[start];

                switch (op._kind)
                {
                    case Kind.WatchFiles:
                        for (var i = 0; i < op._paths.Count; i++)
                        {
                            fileNamesBuilder.Add(op._paths[i]);
                        }

                        for (var i = 0; i < op._tokens.Count; i++)
                        {
                            tokensBuilder.Add(op._tokens[i]);
                        }
                        break;

                    case Kind.UnwatchFiles:
                        for (var i = 0; i < op._tokens.Count; i++)
                        {
                            tokensBuilder.Add(op._tokens[i]);
                        }
                        break;

                    case Kind.UnwatchDirectories:
                        cookiesBuilder.AddRange(op._cookies);
                        break;

                    default:
                        break;
                }
            }

            return firstOp._kind switch
            {
                Kind.WatchFiles =>
                    WatchFiles(fileNamesBuilder.ToImmutable(), firstOp._fileChangeFlags, firstOp._sink, tokensBuilder.ToImmutable()),
                Kind.UnwatchFiles =>
                    UnwatchFiles(tokensBuilder.ToImmutable()),
                Kind.UnwatchDirectories =>
                    UnwatchDirectories(cookiesBuilder.ToList()),
                _ =>
                    throw ExceptionUtilities.Unreachable()
            };
        }

        public bool CanCombineWith(in WatcherOperation other)
        {
            // Watching directory operation cannot be combined
            if (_kind == Kind.WatchDirectory)
                return false;

            return (_kind == other._kind);
        }

        public async ValueTask ApplyAsync(IVsAsyncFileChangeEx2 service, CancellationToken cancellationToken)
        {
            switch (_kind)
            {
                case Kind.WatchDirectory:
                    Contract.ThrowIfTrue(_paths.Count != 1);

                    var cookie = await service.AdviseDirChangeAsync(_paths[0], watchSubdirectories: true, _sink, cancellationToken).ConfigureAwait(false);
                    _cookies.Add(cookie);

                    if (_filter != null)
                        await service.FilterDirectoryChangesAsync(cookie, [_filter], cancellationToken).ConfigureAwait(false);

                    return;

                case Kind.WatchFiles:
                    var cookies = await service.AdviseFileChangesAsync(_paths.ToImmutable(), _fileChangeFlags, _sink, cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfTrue(cookies.Length != _tokens.Count);
                    for (var i = 0; i < cookies.Length; i++)
                        _tokens[i].Cookie = cookies[i];

                    return;

                case Kind.UnwatchDirectories:
                    Contract.ThrowIfFalse(_cookies is not null);
                    await service.UnadviseDirChangesAsync(_cookies, cancellationToken).ConfigureAwait(false);
                    return;

                case Kind.UnwatchFiles:
                    await service.UnadviseFileChangesAsync(_tokens.Select(token => token.Cookie!.Value).ToArray(), cancellationToken).ConfigureAwait(false);
                    return;

                default:
                    throw new NotImplementedException();
            }
        }
    }

    private sealed class Context : IVsFreeThreadedFileChangeEvents2, IFileChangeContext
    {
        private readonly FileChangeWatcher _fileChangeWatcher;
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// Gate to guard mutable fields in this class and any mutation of any <see cref="RegularWatchedFile"/>s.
        /// </summary>
        private readonly object _gate = new();
        private bool _disposed = false;
        private readonly HashSet<RegularWatchedFile> _activeFileWatchingTokens = [];

        /// <summary>
        /// The list of cookies we used to make watchers for <see cref="_watchedDirectories"/>.
        /// </summary>
        /// <remarks>
        /// This does not need to be used under <see cref="_gate"/>, as it's only used inside the actual queue of file watcher
        /// actions.
        /// </remarks>
        private readonly List<uint> _directoryWatchCookies = [];

        public Context(FileChangeWatcher fileChangeWatcher, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _fileChangeWatcher = fileChangeWatcher;
            _watchedDirectories = watchedDirectories;

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
            _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.UnwatchFiles(_activeFileWatchingTokens.ToImmutableArray()));
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If we already have this file under our path, we may not have to do additional watching
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, StringComparison.OrdinalIgnoreCase))
                return NoOpWatchedFile.Instance;

            var token = new RegularWatchedFile(this);

            lock (_gate)
            {
                _activeFileWatchingTokens.Add(token);
            }

            _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.WatchFile(filePath, _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time, this, token));

            return token;
        }

        private void StopWatchingFile(RegularWatchedFile watchedFile)
        {
            lock (_gate)
            {
                Contract.ThrowIfFalse(_activeFileWatchingTokens.Remove(watchedFile), "This token was no longer being watched.");
            }

            _fileChangeWatcher._taskQueue.AddWork(WatcherOperation.UnwatchFile(watchedFile));
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

        public sealed class RegularWatchedFile : IWatchedFile
        {
            public RegularWatchedFile(Context context)
            {
                _context = context;
            }

            private readonly Context _context;

            /// <summary>
            /// The cookie we have for requesting a watch on this file. Null means we either haven't
            /// done the subscription (and it's still in the queue) or we had some sort of error
            /// subscribing in the first place.
            /// </summary>
            public uint? Cookie;

            public void Dispose()
            {
                _context.StopWatchingFile(this);
            }
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
