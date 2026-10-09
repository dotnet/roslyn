// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that delegates file watching through the LSP protocol to the client.
/// </summary>
internal sealed class LspFileChangeWatcher : AbstractConsolidatingFileChangeWatcher
{
    private readonly LspDidChangeWatchedFilesHandler _didChangeWatchedFilesHandler;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly IAsynchronousOperationListener _asynchronousOperationListener;
    private readonly RoslynTelemetry _telemetry;

    private LspFileChangeWatcher(ILspServices lspServices, IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
    {
        _didChangeWatchedFilesHandler = lspServices.GetRequiredService<LspDidChangeWatchedFilesHandler>();
        _clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        _asynchronousOperationListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Workspace);
        _telemetry = RoslynTelemetry.Current;
    }

    public static bool TryCreate(ILspServices lspServices, IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider, [NotNullWhen(true)] out LspFileChangeWatcher? fileChangeWatcher)
    {
        // We can only use the LSP client for doing file watching if we support dynamic registration for it
        var clientCapabilitiesProvider = lspServices.GetRequiredService<IInitializeManager>();
        var supportsLspFileWatching = clientCapabilitiesProvider.GetClientCapabilities().Workspace?.DidChangeWatchedFiles?.DynamicRegistration ?? false;

        if (supportsLspFileWatching)
        {
            fileChangeWatcher = new LspFileChangeWatcher(lspServices, asynchronousOperationListenerProvider);
            return true;
        }

        fileChangeWatcher = null;
        return false;
    }

    protected override IDirectoryWatcher CreateDirectoryWatcher(string path, ImmutableArray<string> filters, bool includeSubdirectories)
        => new DirectoryWatcher(this, path, filters, includeSubdirectories);

    private sealed class DirectoryWatcher : IDirectoryWatcher
    {
        private readonly LspFileChangeWatcher _owner;

        private readonly DocumentUri _baseUri;
        private readonly string _directoryPath;

        /// <summary>
        /// The registration task to register this watch with the LSP client. The task returns the ID of the registration, or null if we have no
        /// current registration.
        /// </summary>
        private Task<string?> _registrationTask = Task.FromResult<string?>(null);

        /// <summary>
        /// The current watch configuration for this directory watcher. There is no synchronization here: it's expected any calls to <see cref="Update"/>
        /// or <see cref="Dispose"/> are synchronized by the caller. File change notifications are still raised on other threads but it'll read the current
        /// state just once.
        /// </summary>
        private volatile WatchConfiguration? _configuration;

        public IReadOnlyList<string> Filters => GetRequiredConfiguration().Filters;
        public bool IncludeSubdirectories => GetRequiredConfiguration().IncludeSubdirectories;
        public event EventHandler<FileChangedEventArgs>? FileChanged;

        public DirectoryWatcher(LspFileChangeWatcher owner, string path, ImmutableArray<string> filters, bool includeSubdirectories)
        {
            _owner = owner;

            // We send the URI to the client that doesn't need a trailing separator, but need a trailing separator on the path when filtering notifications
            // so a watch for 'foo' doesn't also match files in 'foobar'. Just hold onto both.
            _baseUri = ProtocolConversions.CreateAbsoluteDocumentUri(path);
            _directoryPath = PathUtilities.EnsureTrailingSeparator(path);
            _owner._didChangeWatchedFilesHandler.NotificationRaised += DidChangeWatchedFilesHandler_OnNotificationRaised;

            QueueRegistration(new WatchConfiguration(filters, includeSubdirectories));
        }

        public void Update(ImmutableArray<string> filters, bool includeSubdirectories)
        {
            var existingConfiguration = GetRequiredConfiguration();
            if (existingConfiguration.IncludeSubdirectories == includeSubdirectories && existingConfiguration.Filters.SequenceEqual(filters))
                return;

            QueueRegistration(new WatchConfiguration(filters, includeSubdirectories));
        }

        private WatchConfiguration GetRequiredConfiguration()
        {
            var configuration = _configuration;
            ObjectDisposedException.ThrowIf(configuration is null, this);
            return configuration;
        }

        private void QueueRegistration(WatchConfiguration? configuration)
        {
            _configuration = configuration;
            var asyncToken = _owner._asynchronousOperationListener.BeginAsyncOperation(nameof(DirectoryWatcher));

            // Queue an update that will update our registration after any previous registrations;
            // this task chain is marked as OnlyOnRanToCompletion -- any fault in the middle just means we don't
            // know the state of the client anymore and thus we'll have to leak the watcher.
            _registrationTask = _registrationTask.ContinueWith(async previousTask =>
            {
                try
                {
                    using var telemetryScope = RoslynTelemetry.SetCurrent(_owner._telemetry);

                    string? newId = null;
                    if (configuration is not null)
                    {
                        newId = await RegisterAsync(configuration);
                    }

                    // Now that we've registered a new configuration, we can get rid of our old one;
                    // this way there's not a small gap where we might not be listening at all
                    var previousId = previousTask.Result;
                    if (previousId is not null)
                    {
                        try
                        {
                            await UnregisterAsync(previousId);
                        }
                        catch (Exception e) when (e is ConnectionLostException or ObjectDisposedException)
                        {
                            // The pipe can close during shutdown while we're replacing or disposing a registration.
                            // There is no need to spam non fatal faults when this happens.
                        }
                    }

                    return newId;
                }
                catch (Exception ex) when (FatalError.ReportAndPropagate(ex))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default).Unwrap();

            _registrationTask.CompletesAsyncOperation(asyncToken);
        }

        private async Task<string> RegisterAsync(WatchConfiguration configuration)
        {
            var filters = configuration.Filters.IsEmpty ? ["*"] : configuration.Filters;
            var prefix = configuration.IncludeSubdirectories ? "**/" : string.Empty;

            // If we have more than one filter, combine all the individual filters with braces. This is a specifically
            // recognized pattern in VS Code where if multiple filters are combined that way, it can be optimized into a simple
            // set of checks: https://github.com/microsoft/vscode/blob/d070672faea9561775af68832bc9fd536fb077c3/src/vs/base/common/glob.ts#L374-L375
            var pattern = filters.Length == 1
                ? prefix + filters[0]
                : "{" + string.Join(',', filters.SelectAsArray(filter => prefix + filter)) + "}";

            var id = Guid.NewGuid().ToString();
            var registrationParams = new RegistrationParams
            {
                Registrations =
                [
                    new Registration
                    {
                        Id = id,
                        Method = Methods.WorkspaceDidChangeWatchedFilesName,
                        RegisterOptions = new DidChangeWatchedFilesRegistrationOptions
                        {
                            Watchers =
                            [
                                new Roslyn.LanguageServer.Protocol.FileSystemWatcher
                                {
                                    GlobPattern = new RelativePattern
                                    {
                                        BaseUri = _baseUri,
                                        Pattern = pattern,
                                    },
                                },
                            ],
                        },
                    },
                ],
            };

            await _owner._clientLanguageServerManager.SendRequestAsync("client/registerCapability", registrationParams, CancellationToken.None);
            return id;
        }

        private async Task UnregisterAsync(string id)
        {
            var unregistrationParams = new UnregistrationParams
            {
                Unregistrations =
                [
                    new Unregistration
                    {
                        Id = id,
                        Method = Methods.WorkspaceDidChangeWatchedFilesName,
                    },
                ],
            };

            await _owner._clientLanguageServerManager.SendRequestAsync("client/unregisterCapability", unregistrationParams, CancellationToken.None);
        }

        private void DidChangeWatchedFilesHandler_OnNotificationRaised(object? sender, DidChangeWatchedFilesParams e)
        {
            // The LSP protocol gives us no way to determine if this notification applies to this directory watch or another directory watch,
            // so we'll just filter them out. We'll filter changes here that only apply to our directory, using the current configuration.
            // This may differ from the configuration that's still being sent over but that's fine -- if we get a file change we're no longer
            // interested in, we can just drop it.
            var configuration = _configuration;
            if (configuration is null)
                return;

            foreach (var change in e.Changes)
            {
                var filePath = change.Uri.GetRequiredParsedUri().FsPath;
                if (!filePath.StartsWith(_directoryPath, s_pathStringComparison))
                    continue;

                var relativePath = filePath.AsSpan(_directoryPath.Length);
                if (!configuration.IncludeSubdirectories && relativePath.ContainsAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                    continue;

                if (!configuration.Filters.IsEmpty &&
                    !configuration.Filters.Any(filter => FileSystemName.MatchesSimpleExpression(filter, Path.GetFileName(filePath.AsSpan()), ignoreCase: s_pathStringComparison == StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var changeKind = change.FileChangeType switch
                {
                    FileChangeType.Created => FileChangeKind.Created,
                    FileChangeType.Deleted => FileChangeKind.Deleted,
                    FileChangeType.Changed => FileChangeKind.Changed,
                    _ => throw ExceptionUtilities.UnexpectedValue(change.FileChangeType),
                };

                FileChanged?.Invoke(this, new(filePath, changeKind));
            }
        }

        public void Dispose()
        {
            if (_configuration is null)
                return;

            QueueRegistration(configuration: null);
            _owner._didChangeWatchedFilesHandler.NotificationRaised -= DidChangeWatchedFilesHandler_OnNotificationRaised;
        }

        private sealed record WatchConfiguration(ImmutableArray<string> Filters, bool IncludeSubdirectories);
    }
}
