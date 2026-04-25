// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace Microsoft.VisualStudio.Razor.Remote;

[Export(typeof(IRemoteServiceInvoker))]
[method: ImportingConstructor]
internal sealed class RemoteServiceInvoker(
    IWorkspaceProvider workspaceProvider,
    LanguageServerFeatureOptions languageServerFeatureOptions,
    IClientSettingsManager clientSettingsManager,
    IClientCapabilitiesService clientCapabilitiesService,
    ISemanticTokensLegendService semanticTokensLegendService,
    SVsServiceProvider serviceProvider,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory) : IRemoteServiceInvoker, IDisposable
{
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RemoteServiceInvoker>();

    private readonly CancellationTokenSource _disposeTokenSource = new();

    private readonly AsyncLazy<RazorRemoteHostClient> _lazyMessagePackClient = AsyncLazy.Create(GetMessagePackClientAsync, workspaceProvider);
    private readonly AsyncLazy<RazorRemoteHostClient> _lazyJsonClient = AsyncLazy.Create(GetJsonClientAsync, workspaceProvider);

    private readonly object _gate = new();
    private Task? _initializeOOPTask;
    private Task? _initializeLspTask;

    public void Dispose()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _clientSettingsManager.ClientSettingsChanged -= ClientSettingsManager_ClientSettingsChanged;

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();
    }

    public async ValueTask<TResult?> TryInvokeAsync<TService, TResult>(
        Solution solution,
        Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken,
        [CallerFilePath] string? callerFilePath = null,
        [CallerMemberName] string? callerMemberName = null)
        where TService : class
    {
        await InitializeAsync().ConfigureAwait(false);

        var client = await GetClientAsync<TService>(cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return default;
        }

        try
        {
            var result = await client.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);

            return result.Value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var approximateCallingClassName = Path.GetFileNameWithoutExtension(callerFilePath);
            _logger.LogError(ex, $"Error calling remote method for {typeof(TService).Name} service, invocation: {approximateCallingClassName}.{callerMemberName}");
            _telemetryReporter.ReportFault(ex, "Exception calling remote method for {service}, invocation: {class}.{method}", typeof(TService).FullName, approximateCallingClassName, callerMemberName);
            return default;
        }
    }

    private Task<RazorRemoteHostClient> GetClientAsync<TService>(CancellationToken cancellationToken)
        where TService : class
        => typeof(IRemoteJsonService).IsAssignableFrom(typeof(TService))
            ? _lazyJsonClient.GetValueAsync(cancellationToken)
            : _lazyMessagePackClient.GetValueAsync(cancellationToken);

    private static async Task<RazorRemoteHostClient> GetMessagePackClientAsync(IWorkspaceProvider workspaceProvider, CancellationToken cancellationToken)
    {
        var workspace = workspaceProvider.GetWorkspace();

        var remoteClient = await RazorRemoteHostClient
            .TryGetClientAsync(
                workspace.Services,
                RazorServices.Descriptors,
                RazorRemoteServiceCallbackDispatcherRegistry.Empty,
                cancellationToken)
            .ConfigureAwait(false);

        return remoteClient
            ?? throw new InvalidOperationException($"Couldn't retrieve {nameof(RazorRemoteHostClient)} for MessagePack serialization.");
    }

    private static async Task<RazorRemoteHostClient> GetJsonClientAsync(IWorkspaceProvider workspaceProvider, CancellationToken cancellationToken)
    {
        var workspace = workspaceProvider.GetWorkspace();

        var remoteClient = await RazorRemoteHostClient
            .TryGetClientAsync(
                workspace.Services,
                RazorServices.JsonDescriptors,
                RazorRemoteServiceCallbackDispatcherRegistry.Empty,
                cancellationToken)
            .ConfigureAwait(false);

        return remoteClient
            ?? throw new InvalidOperationException($"Couldn't retrieve {nameof(RazorRemoteHostClient)} for JSON serialization.");
    }

    private ValueTask InitializeAsync()
    {
        var oopInitialized = _initializeOOPTask is { Status: TaskStatus.RanToCompletion };
        var lspInitialized = _initializeLspTask is { Status: TaskStatus.RanToCompletion };

        // Note: Since InitializeAsync will be called for each remote service call, we provide a synchronous path
        // to exit quickly when initialized and avoid creating an unnecessary async state machine.
        return oopInitialized && lspInitialized
            ? default
            : new(InitializeCoreAsync(oopInitialized, lspInitialized));

        async Task InitializeCoreAsync(bool oopInitialized, bool lspInitialized)
        {
            // Note: IRemoteClientInitializationService is an IRemoteJsonService, so we always need the JSON client.
            var remoteClient = await _lazyJsonClient
                .GetValueAsync(_disposeTokenSource.Token)
                .ConfigureAwait(false);

            if (!oopInitialized)
            {
                lock (_gate)
                {
                    _initializeOOPTask ??= InitializeOOPAsync(remoteClient);
                }

                await _initializeOOPTask.ConfigureAwait(false);
            }

            if (!lspInitialized && _clientCapabilitiesService.CanGetClientCapabilities)
            {
                lock (_gate)
                {
                    _initializeLspTask ??= InitializeLspAsync(remoteClient);
                }

                await _initializeLspTask.ConfigureAwait(false);
            }

            async Task InitializeOOPAsync(RazorRemoteHostClient remoteClient)
            {
                // The first call to OOP must be to initialize the MEF services, because everything after that relies on MEF.
                var localSettingsDirectory = new ShellSettingsManager(_serviceProvider).GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);
                var cacheDirectory = Path.Combine(localSettingsDirectory, "Razor", "RemoteMEFCache");
                await remoteClient.TryInvokeAsync<IRemoteMEFInitializationService>(
                    (s, ct) => s.InitializeAsync(cacheDirectory, ct),
                    _disposeTokenSource.Token).ConfigureAwait(false);

                var initParams = new RemoteClientInitializationOptions
                {
                    ReturnCodeActionAndRenamePathsWithPrefixedSlash = _languageServerFeatureOptions.ReturnCodeActionAndRenamePathsWithPrefixedSlash,
                    SupportsFileManipulation = _languageServerFeatureOptions.SupportsFileManipulation,
                    ShowAllCSharpCodeActions = _languageServerFeatureOptions.ShowAllCSharpCodeActions,
                };

                _logger.LogDebug($"First OOP call, so initializing OOP service.");

                await remoteClient
                    .TryInvokeAsync<IRemoteClientInitializationService>(
                        (s, ct) => s.InitializeAsync(initParams, ct),
                        _disposeTokenSource.Token).ConfigureAwait(false);

                // Now that we're initialized, send over the current client settings, and subscribe to changes
                await UpdateClientSettingsAsync(remoteClient, _disposeTokenSource.Token).ConfigureAwait(false);
                _clientSettingsManager.ClientSettingsChanged += ClientSettingsManager_ClientSettingsChanged;
            }

            Task InitializeLspAsync(RazorRemoteHostClient remoteClient)
            {
                var initParams = new RemoteClientLSPInitializationOptions
                {
                    ClientCapabilities = _clientCapabilitiesService.ClientCapabilities,
                    TokenTypes = _semanticTokensLegendService.TokenTypes.All,
                    TokenModifiers = _semanticTokensLegendService.TokenModifiers.All,
                };

                _logger.LogDebug($"LSP server has started since last OOP call, so initializing OOP service with LSP info.");

                return remoteClient
                    .TryInvokeAsync<IRemoteClientInitializationService>(
                        (s, ct) => s.InitializeLspAsync(initParams, ct),
                        _disposeTokenSource.Token)
                    .AsTask();
            }
        }
    }

    private void ClientSettingsManager_ClientSettingsChanged(object? sender, EventArgs e)
    {
        if (_initializeOOPTask is null || _disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _ = UpdateClientSettingsAsync(_disposeTokenSource.Token);
    }

    private async Task UpdateClientSettingsAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync().ConfigureAwait(false);

        var remoteClient = await _lazyJsonClient.GetValueAsync(cancellationToken).ConfigureAwait(false);
        await UpdateClientSettingsAsync(remoteClient, cancellationToken).ConfigureAwait(false);
    }

    private Task UpdateClientSettingsAsync(RazorRemoteHostClient remoteClient, CancellationToken cancellationToken)
    {
        var clientSettings = _clientSettingsManager.GetClientSettings();

        _logger.LogDebug("Syncing client settings to OOP.");

        return remoteClient
            .TryInvokeAsync<IRemoteClientSettingsService>(
                (s, ct) => s.UpdateAsync(clientSettings, ct),
                cancellationToken)
            .AsTask();
    }
}
