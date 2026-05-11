// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[Export(typeof(IRazorCohostStartupService))]
[method: ImportingConstructor]
internal sealed class VSCodeRemoteServicesInitializer(
    LanguageServerFeatureOptions featureOptions,
    ISemanticTokensLegendService semanticTokensLegendService,
    IWorkspaceProvider workspaceProvider,
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory) : IRazorCohostStartupService, IDisposable
{
    private readonly LanguageServerFeatureOptions _featureOptions = featureOptions;
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;
    private readonly IWorkspaceProvider _workspaceProvider = workspaceProvider;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    private IRemoteClientSettingsService? _clientSettingsService;

    public int Order => WellKnownStartupOrder.RemoteServices;

    public async Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        // Normal remote service invoker logic requires a solution, but we don't have one here. Fortunately we don't need one, and since
        // we know this is VS Code specific, its all just smoke and mirrors anyway. We can avoid the smoke :)
        var serviceInterceptor = new VSCodeBrokeredServiceInterceptor();

        // First things first, set the cache directory for the MEF composition.
        RemoteMefComposition.CacheDirectory = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location)!, "cache");

        var logger = _loggerFactory.GetOrCreateLogger<VSCodeRemoteServicesInitializer>();
        logger.LogDebug("Initializing remote services.");
        var service = await InProcServiceFactory.CreateServiceAsync<IRemoteClientInitializationService>(serviceInterceptor, _workspaceProvider, _loggerFactory).ConfigureAwait(false);
        logger.LogDebug("Initialized remote services.");

        await service.InitializeAsync(new RemoteClientInitializationOptions
        {
            ReturnCodeActionAndRenamePathsWithPrefixedSlash = _featureOptions.ReturnCodeActionAndRenamePathsWithPrefixedSlash,
            SupportsFileManipulation = _featureOptions.SupportsFileManipulation,
            ShowAllCSharpCodeActions = _featureOptions.ShowAllCSharpCodeActions,
        }, cancellationToken).ConfigureAwait(false);

        await service.InitializeLspAsync(new RemoteClientLSPInitializationOptions
        {
            ClientCapabilities = clientCapabilities,
            TokenTypes = _semanticTokensLegendService.TokenTypes.All,
            TokenModifiers = _semanticTokensLegendService.TokenModifiers.All,
        }, cancellationToken).ConfigureAwait(false);

        _clientSettingsService = await InProcServiceFactory.CreateServiceAsync<IRemoteClientSettingsService>(serviceInterceptor, _workspaceProvider, _loggerFactory).ConfigureAwait(false);
        // Client settings are initialized after this service, so there is no point updating settings at startup.
        _clientSettingsManager.ClientSettingsChanged += ClientSettingsManager_ClientSettingsChanged;
    }

    public void Dispose()
    {
        _clientSettingsManager?.ClientSettingsChanged -= ClientSettingsManager_ClientSettingsChanged;
    }

    private void ClientSettingsManager_ClientSettingsChanged(object? sender, EventArgs e)
    {
        UpdateClientSettingsAsync(CancellationToken.None).Forget();
    }

    private Task UpdateClientSettingsAsync(CancellationToken cancellationToken)
    {
        if (_clientSettingsService is not { } clientSettingsService)
        {
            throw new InvalidOperationException($"{nameof(VSCodeRemoteServicesInitializer)} has not been started.");
        }

        return clientSettingsService.UpdateAsync(_clientSettingsManager.GetClientSettings(), cancellationToken).AsTask();
    }
}
