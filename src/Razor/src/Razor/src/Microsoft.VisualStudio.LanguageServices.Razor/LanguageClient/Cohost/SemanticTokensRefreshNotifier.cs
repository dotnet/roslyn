// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

[Export(typeof(IRazorCohostStartupService))]
[method: ImportingConstructor]
internal sealed class SemanticTokensRefreshNotifier(IClientSettingsManager clientSettingsManager) : IRazorCohostStartupService, IDisposable
{
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;

    private IRazorClientLanguageServerManager? _razorClientLanguageServerManager;
    private bool _lastColorBackground;

    public int Order => WellKnownStartupOrder.Default;

    public Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        _razorClientLanguageServerManager = requestContext.GetRequiredService<IRazorClientLanguageServerManager>();

        if (clientCapabilities.Workspace?.SemanticTokens?.RefreshSupport ?? false)
        {
            _lastColorBackground = _clientSettingsManager.GetClientSettings().AdvancedSettings.ColorBackground;
            _clientSettingsManager.ClientSettingsChanged += ClientSettingsManager_ClientSettingsChanged;
        }

        return Task.CompletedTask;
    }

    private void ClientSettingsManager_ClientSettingsChanged(object sender, EventArgs e)
    {
        var colorBackground = _clientSettingsManager.GetClientSettings().AdvancedSettings.ColorBackground;
        if (colorBackground == _lastColorBackground)
        {
            return;
        }

        _lastColorBackground = colorBackground;
        _razorClientLanguageServerManager.AssumeNotNull().SendNotificationAsync(Methods.WorkspaceSemanticTokensRefreshName, CancellationToken.None).Forget();
    }

    public void Dispose()
    {
        _clientSettingsManager.ClientSettingsChanged -= ClientSettingsManager_ClientSettingsChanged;
    }
}
