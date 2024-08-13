// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// A MEF export for <see cref="IFileChangeWatcher" />. This checks if we're able to create an <see cref="LspFileChangeWatcher" /> if the client supports
/// file watching. If we do, we create that and delegate to it. Otherwise we use a <see cref="SimpleFileChangeWatcher" />.
/// </summary>
/// <remarks>
/// LSP clients don't always support file watching; this allows us to be flexible and use it when we can, but fall back to something else if we can't.
/// </remarks>
[Export(typeof(IFileChangeWatcher)), Shared]
internal sealed class DelegatingFileChangeWatcher : IFileChangeWatcher
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IAsynchronousOperationListenerProvider _asynchronousOperationListenerProvider;
    private readonly Lazy<IFileChangeWatcher> _underlyingFileWatcher;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DelegatingFileChangeWatcher(ILoggerFactory loggerFactory, IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
    {
        _loggerFactory = loggerFactory;
        _asynchronousOperationListenerProvider = asynchronousOperationListenerProvider;
        _underlyingFileWatcher = new Lazy<IFileChangeWatcher>(CreateFileWatcher);
    }

    private IFileChangeWatcher CreateFileWatcher()
    {
        // Do we already have an LSP client that we can confirm works for us?
        var instance = LanguageServerHost.Instance;

        if (instance != null && LspFileChangeWatcher.SupportsLanguageServerHost(instance))
        {
            return new LspFileChangeWatcher(instance, _asynchronousOperationListenerProvider);
        }
        else
        {
            _loggerFactory.CreateLogger<DelegatingFileChangeWatcher>().LogWarning("We are unable to use LSP file watching; falling back to our in-process watcher.");
            return new SimpleFileChangeWatcher();
        }
    }

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => _underlyingFileWatcher.Value.CreateContext(watchedDirectories);
}
