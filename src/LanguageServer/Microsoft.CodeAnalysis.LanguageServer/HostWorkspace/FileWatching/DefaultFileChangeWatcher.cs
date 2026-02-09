// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation creates one <see cref="FileSystemWatcher"/> per root drive and uses filtering to route file
/// change events to the appropriate watchers. The watchers are shared between all <see cref="FileChangeContext"/>
/// instances and are disposed when all contexts using them have been disposed.
/// </remarks>
internal sealed partial class DefaultFileChangeWatcher : IFileChangeWatcher
{
    private static readonly StringComparer s_pathStringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static readonly StringComparison s_pathStringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private readonly ReferenceCountedDisposableCache<string, FileSystemWatcher> _sharedRootWatchers = new(s_pathStringComparer);

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(this, watchedDirectories);

    private IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> GetOrCreateSharedWatcher(string rootPath)
    {
        var rootWatcher = _sharedRootWatchers.GetOrCreate<object?>(rootPath, static (key, _) => new FileSystemWatcher(key), arg: null);
        rootWatcher.Target.Value.IncludeSubdirectories = true;
        rootWatcher.Target.Value.EnableRaisingEvents = true;
        return rootWatcher;
    }

    private static void AttachWatcher(IEventRaiser eventRaiser, IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> watcher)
    {
        watcher.Target.Value.Changed += eventRaiser.RaiseEvent;
        watcher.Target.Value.Created += eventRaiser.RaiseEvent;
        watcher.Target.Value.Deleted += eventRaiser.RaiseEvent;
        watcher.Target.Value.Renamed += eventRaiser.RaiseEvent;
    }

    private static void DetachAndDisposeWatcher(IEventRaiser eventRaiser, IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> watcher)
    {
        watcher.Target.Value.Changed -= eventRaiser.RaiseEvent;
        watcher.Target.Value.Created -= eventRaiser.RaiseEvent;
        watcher.Target.Value.Deleted -= eventRaiser.RaiseEvent;
        watcher.Target.Value.Renamed -= eventRaiser.RaiseEvent;
        watcher.Dispose();
    }

    internal interface IEventRaiser
    {
        void RaiseEvent(object? sender, FileSystemEventArgs e);
    }

    internal static class TestAccessor
    {
        public static IEnumerable<string> GetWatchedRootPaths(DefaultFileChangeWatcher watcher)
            => ReferenceCountedDisposableCache<string, FileSystemWatcher>.TestAccessor.GetCacheKeys(watcher._sharedRootWatchers);
    }
}
