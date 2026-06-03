// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// A MEF export for <see cref="IFileChangeWatcher" />. This checks if we're able to create an <see
/// cref="LspFileChangeWatcher" /> if the client supports file watching. If we do, we create that and delegate to it.
/// Otherwise we use a <see cref="DefaultFileChangeWatcher" />.
/// </summary>
/// <remarks>
/// LSP clients don't always support file watching; this allows us to be flexible and use it when we can, but fall back
/// to something else if we can't.
/// </remarks>
internal sealed class DelegatingFileChangeWatcher(
    ILspServices lspServices,
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
    : IFileChangeWatcher, ILspService
{
    private readonly Lazy<IFileChangeWatcher> _underlyingFileWatcher = new(() =>
        {
            if (LspFileChangeWatcher.TryCreate(lspServices, asynchronousOperationListenerProvider, out var lspFileChangeWatcher))
                return lspFileChangeWatcher;

            loggerFactory.CreateLogger<DelegatingFileChangeWatcher>().LogWarning("We are unable to use LSP file watching; falling back to our in-process watcher.");

            // On non-Windows platforms, the number of inotify handles is limited, so we'll want to be more aggressive with reducing it.
            // TODO: we could read the inotify limit and set this dynamically, since some newer kernels have a higher default.
            return new DefaultFileChangeWatcher(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 10_000 : 50);
        });

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => _underlyingFileWatcher.Value.CreateContext(watchedDirectories);

    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor(DelegatingFileChangeWatcher instance)
    {
        internal IFileChangeWatcher UnderlyingFileWatcher => instance._underlyingFileWatcher.Value;
    }
}
