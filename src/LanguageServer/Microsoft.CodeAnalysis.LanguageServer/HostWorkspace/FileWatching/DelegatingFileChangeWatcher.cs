// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
[Export(typeof(IFileChangeWatcher)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DelegatingFileChangeWatcher(
    ILoggerFactory loggerFactory,
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
    : IFileChangeWatcher
{
    private readonly Lazy<IFileChangeWatcher> _underlyingFileWatcher = new(() =>
        {
            // Do we already have an LSP client that we can confirm works for us?
            var instance = LanguageServerHost.Instance;

            if (instance != null && LspFileChangeWatcher.SupportsLanguageServerHost(instance))
                return new LspFileChangeWatcher(instance, asynchronousOperationListenerProvider);

            loggerFactory.CreateLogger<DelegatingFileChangeWatcher>().LogWarning("We are unable to use LSP file watching; falling back to our in-process watcher.");
            return new DefaultFileChangeWatcher();
        });

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => _underlyingFileWatcher.Value.CreateContext(watchedDirectories);
}
