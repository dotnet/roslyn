// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[ExportCSharpVisualBasicLspService(typeof(CodeLensRefreshQueue)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class CodeLensRefreshQueue : AbstractRefreshQueue
{
    private readonly IGlobalOptionService _globalOptionService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        FeatureProviderRefresher providerRefresher,
        IGlobalOptionService globalOptionService,
        LspService<IClientLanguageServerManager> notificationManager,
        LspService<LspWorkspaceRegistrationService> lspWorkspaceRegistrationService,
        LspService<LspWorkspaceManager> lspWorkspaceManager)
        : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService.Value, lspWorkspaceManager.Value, notificationManager.Value, providerRefresher)
    {
        _globalOptionService = globalOptionService;
        _globalOptionService.AddOptionChangedHandler(this, OnOptionChanged);
    }

    protected override string GetFeatureAttribute()
        => FeatureAttribute.CodeLens;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
    {
        return clientCapabilities.Workspace?.CodeLens?.RefreshSupport;
    }

    protected override string GetWorkspaceRefreshName()
    {
        return Methods.WorkspaceCodeLensRefreshName;
    }

    private void OnOptionChanged(object sender, object target, OptionChangedEventArgs e)
    {
        if (e.HasOption(static option =>
            option.Equals(LspOptionsStorage.LspEnableReferencesCodeLens) ||
            option.Equals(LspOptionsStorage.LspEnableTestsCodeLens) ||
            option.Equals(LspOptionsStorage.LspUseSemanticTestDiscovery)))
        {
            EnqueueRefreshNotification(documentUri: null);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _globalOptionService.RemoveOptionChangedHandler(this, OnOptionChanged);
    }
}
