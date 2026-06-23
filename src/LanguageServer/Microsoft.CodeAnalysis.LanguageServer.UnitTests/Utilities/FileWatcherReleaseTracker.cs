// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Captures the file watches active in the process-wide <see cref="DefaultFileChangeWatcher"/> (shared by every
/// server via <see cref="DelegatingFileChangeWatcher"/>) so a test can later assert that any watches created after the
/// snapshot were released. This is used to catch tests that leak file watches by failing to dispose a server (and
/// hence its projects) on shutdown.
/// </summary>
/// <remarks>
/// Tests in this assembly run sequentially (see <c>eng/config/xunit.runner.json</c>), so the shared watcher is only
/// mutated by one test at a time. Capturing a baseline at test construction and comparing against it on teardown means
/// only the test that actually leaked a watch fails, rather than some arbitrary later test that happens to inspect the
/// shared watcher. Watches are tracked by context identity rather than by directory path so the comparison is unaffected
/// by watcher consolidation or by an earlier leaked watch happening to cover the same directory.
/// </remarks>
internal readonly struct FileWatcherReleaseTracker
{
    private readonly ImmutableHashSet<IFileChangeContext> _baselineContexts;

    private FileWatcherReleaseTracker(ImmutableHashSet<IFileChangeContext> baselineContexts)
    {
        _baselineContexts = baselineContexts;
    }

    /// <summary>
    /// Snapshots the file watches currently active. Capture this before creating a server so that
    /// <see cref="AssertWatchesReleased"/> only considers watches created (and expected to be released) afterwards.
    /// </summary>
    public static FileWatcherReleaseTracker Capture()
        => new(GetActiveContexts());

    /// <summary>
    /// Asserts that every file watch created since <see cref="Capture"/> has been released. Call this after all servers
    /// created by the test have shut down (for example, from the test class's dispose method).
    /// </summary>
    public void AssertWatchesReleased()
    {
        var leakedContexts = GetActiveContexts().Except(_baselineContexts);
        if (leakedContexts.IsEmpty)
            return;

        var watcher = DelegatingFileChangeWatcher.TestAccessor.SharedDefaultFileChangeWatcher;
        var watchedDirectories = DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher).Select(static d => d.path);

        Assert.Fail($"""
            The language server did not release all of its file watches on shutdown ({leakedContexts.Count} file watch context(s) leaked).
            This usually indicates the test left a server (or one of its projects) undisposed. Currently watched directories:
            {string.Join(Environment.NewLine, watchedDirectories)}
            """);
    }

    private static ImmutableHashSet<IFileChangeContext> GetActiveContexts()
    {
        var watcher = DelegatingFileChangeWatcher.TestAccessor.SharedDefaultFileChangeWatcher;
        return [.. DefaultFileChangeWatcher.TestAccessor.GetActiveContexts(watcher)];
    }
}
