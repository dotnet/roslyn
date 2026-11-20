// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ProjectContext;

[ExportCSharpVisualBasicLspService(typeof(ProjectContextRefreshQueue)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ProjectContextRefreshQueue(
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    FeatureProviderRefresher providerRefresher,
    LspService<IClientLanguageServerManager> notificationManager,
    LspService<LspWorkspaceRegistrationService> lspWorkspaceRegistrationService,
    LspService<LspWorkspaceManager> lspWorkspaceManager) : AbstractRefreshQueue(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService.Value, lspWorkspaceManager.Value, notificationManager.Value, providerRefresher)
{
    protected override string GetFeatureAttribute()
        => FeatureAttribute.LanguageServer;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        => (clientCapabilities.Workspace as VSInternalWorkspaceClientCapabilities)?.ProjectContext?.RefreshSupport;

    protected override string GetWorkspaceRefreshName()
        => VSInternalMethods.WorkspaceProjectContextRefreshName;
}
