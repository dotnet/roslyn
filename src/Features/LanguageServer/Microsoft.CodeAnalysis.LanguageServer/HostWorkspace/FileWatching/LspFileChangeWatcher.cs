// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Collections.Immutable;
using Roslyn.Utilities;
using FileSystemWatcher = Microsoft.VisualStudio.LanguageServer.Protocol.FileSystemWatcher;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that delegates file watching through the LSP protocol to the client.
/// </summary>
internal sealed class LspFileChangeWatcher : IFileChangeWatcher
{
    private readonly LspDidChangeWatchedFilesHandler _didChangeWatchedFilesHandler;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly IAsynchronousOperationListener _asynchronousOperationListener;

    public LspFileChangeWatcher(LanguageServerHost languageServerHost, IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
    {
        _didChangeWatchedFilesHandler = languageServerHost.GetRequiredLspService<LspDidChangeWatchedFilesHandler>();
        _clientLanguageServerManager = languageServerHost.GetRequiredLspService<IClientLanguageServerManager>();
        _asynchronousOperationListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Workspace);

        Contract.ThrowIfFalse(SupportsLanguageServerHost(languageServerHost));
    }

    public static bool SupportsLanguageServerHost(LanguageServerHost languageServerHost)
    {
        // We can only use the LSP client for doing file watching if we support dynamic registration for it
        var clientCapabilitiesProvider = languageServerHost.GetRequiredLspService<IClientCapabilitiesProvider>();
        return clientCapabilitiesProvider.GetClientCapabilities().Workspace?.DidChangeWatchedFiles?.DynamicRegistration ?? false;
    }

    public IFileChangeContext CreateContext(params WatchedDirectory[] watchedDirectories)
    {
        return new FileChangeContext(watchedDirectories.ToImmutableArray(), this);
    }

    private class FileChangeContext : IFileChangeContext
    {
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
        private readonly LspFileChangeWatcher _lspFileChangeWatcher;

        /// <summary>
        /// The registration for the directory being watched in this context, if some were given.
        /// </summary>
        private readonly LspFileWatchRegistration? _directoryWatchRegistration;

        /// <summary>
        /// A lock to guard updates to <see cref="_watchedFiles" />. Using a reader/writer lock since file change notifications can be pretty chatty
        /// and so we want to be able to process changes as fast as possible.
        /// </summary>
        private readonly ReaderWriterLockSlim _watchedFilesLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The list of file paths we're watching manually that were outside the directories being watched. The count in this case counts
        /// the number of 
        /// </summary>
        private readonly Dictionary<string, int> _watchedFiles = new Dictionary<string, int>(StringComparer.Ordinal);

        public FileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories, LspFileChangeWatcher lspFileChangeWatcher)
        {
            _watchedDirectories = watchedDirectories;
            _lspFileChangeWatcher = lspFileChangeWatcher;

            // If we have any watched directories, then watch those directories directly
            if (watchedDirectories.Any())
            {
                var directoryWatches = watchedDirectories.Select(d => new FileSystemWatcher
                {
                    GlobPattern = new RelativePattern
                    {
                        BaseUri = ProtocolConversions.GetUriFromFilePath(d.Path),
                        Pattern = d.ExtensionFilter is not null ? "**/*" + d.ExtensionFilter : "**/*"
                    }
                }).ToArray();

                _directoryWatchRegistration = new LspFileWatchRegistration(lspFileChangeWatcher, directoryWatches);
            }

            _lspFileChangeWatcher._didChangeWatchedFilesHandler.NotificationRaised += WatchedFilesHandler_OnNotificationRaised;
        }

        private void WatchedFilesHandler_OnNotificationRaised(object? sender, DidChangeWatchedFilesParams e)
        {
            foreach (var changedFile in e.Changes)
            {
                var filePath = changedFile.Uri.LocalPath;

                // Unfortunately the LSP protocol doesn't give us any hint of which of the file watches we might have sent to the client
                // was the one that registered for this change, so we have to check paths to see if this one we should respond to.
                if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, StringComparison.Ordinal))
                {
                    FileChanged?.Invoke(this, filePath);
                }
                else
                {
                    bool isFileWatched;
                    using (_watchedFilesLock.DisposableRead())
                    {
                        isFileWatched = _watchedFiles.ContainsKey(filePath);
                    }

                    if (isFileWatched)
                        FileChanged?.Invoke(this, filePath);
                }
            }
        }

        public event EventHandler<string>? FileChanged;

        public void Dispose()
        {
            _lspFileChangeWatcher._didChangeWatchedFilesHandler.NotificationRaised -= WatchedFilesHandler_OnNotificationRaised;
            _directoryWatchRegistration?.Dispose();
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If we already have this file under our path, we may not have to do additional watching
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, StringComparison.OrdinalIgnoreCase))
                return NoOpWatchedFile.Instance;

            // Record that we're now watching this file
            using (_watchedFilesLock.DisposableWrite())
            {
                _watchedFiles.TryGetValue(filePath, out var existingWatches);
                _watchedFiles[filePath] = existingWatches + 1;
            }

            var fileSystemWatcher = new FileSystemWatcher()
            {
                // TODO: figure out how I just can do an absolute path watch
                GlobPattern = new RelativePattern
                {
                    BaseUri = ProtocolConversions.GetUriFromFilePath(Path.GetDirectoryName(filePath)!),
                    Pattern = Path.GetFileName(filePath)
                }
            };

            return new WatchedFile(filePath, new LspFileWatchRegistration(_lspFileChangeWatcher, fileSystemWatcher), this);
        }

        private void RemoveFileFromWatchList(string filePath)
        {
            // Record that we're no longer watching this file
            using (_watchedFilesLock.DisposableWrite())
            {
                var existingWatches = _watchedFiles[filePath];
                if (existingWatches == 1)
                    _watchedFiles.Remove(filePath);
                else
                    _watchedFiles[filePath] = existingWatches - 1;
            }
        }

        private class WatchedFile : IWatchedFile
        {
            private readonly string _filePath;
            private readonly LspFileWatchRegistration _fileWatchRegistration;
            private readonly FileChangeContext _fileChangeContext;

            public WatchedFile(string filePath, LspFileWatchRegistration fileWatchRegistration, FileChangeContext fileChangeContext)
            {
                _filePath = filePath;
                _fileWatchRegistration = fileWatchRegistration;
                _fileChangeContext = fileChangeContext;
            }

            public void Dispose()
            {
                _fileWatchRegistration.Dispose();
                _fileChangeContext.RemoveFileFromWatchList(_filePath);
            }
        }
    }

    /// <summary>
    /// A small class to represent a registration that is sent to the client that we can cancel later. Since we send
    /// registrations asynchronously, this tracks that so we don't send the unregister too early.
    /// </summary>
    private sealed class LspFileWatchRegistration : IDisposable
    {
        private readonly LspFileChangeWatcher _changeWatcher;
        private readonly string _id;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _registrationTask;

        public LspFileWatchRegistration(LspFileChangeWatcher changeWatcher, params FileSystemWatcher[] fileSystemWatchers)
        {
            _changeWatcher = changeWatcher;
            _id = Guid.NewGuid().ToString();
            _cancellationTokenSource = new CancellationTokenSource();

            var registrationParams = new RegistrationParams()
            {
                Registrations = new Registration[]
                {
                    new Registration
                    {
                        Id = _id,
                        Method = "workspace/didChangeWatchedFiles",
                        RegisterOptions = new DidChangeWatchedFilesRegistrationOptions
                        {
                            Watchers = fileSystemWatchers
                        }
                    }
                }
            };

            var asyncToken = _changeWatcher._asynchronousOperationListener.BeginAsyncOperation(nameof(LspFileWatchRegistration));
            _registrationTask = changeWatcher._clientLanguageServerManager.SendRequestAsync("client/registerCapability", registrationParams, _cancellationTokenSource.Token).AsTask();
            _registrationTask.ReportNonFatalErrorUnlessCancelledAsync(_cancellationTokenSource.Token).CompletesAsyncOperation(asyncToken);
        }

        public void Dispose()
        {
            // We need to remove our file watch. We'll run that once the previous work has completed. We'll run only if the registration completed successfully, since cancellation
            // means it never actually made it to the client, and fault would mean it never was actually created.
            _cancellationTokenSource.Cancel();

            var asyncToken = _changeWatcher._asynchronousOperationListener.BeginAsyncOperation(nameof(LspFileWatchRegistration) + "." + nameof(Dispose));

            _registrationTask.ContinueWith(async _ =>
            {
                var unregistrationParams = new UnregistrationParamsWithMisspelling()
                {
                    Unregistrations = new Unregistration[]
                    {
                        new Unregistration()
                        {
                            Id = _id,
                            Method = "workspace/didChangeWatchedFiles"
                        }
                    }
                };

                await _changeWatcher._clientLanguageServerManager.SendRequestAsync("client/unregisterCapability", unregistrationParams, CancellationToken.None);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default).Unwrap().ReportNonFatalErrorAsync().CompletesAsyncOperation(asyncToken);
        }
    }
}
