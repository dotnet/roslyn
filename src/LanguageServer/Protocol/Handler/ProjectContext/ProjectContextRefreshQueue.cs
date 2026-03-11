// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ProjectContext;

internal sealed class ProjectContextRefreshQueue(
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
    LspWorkspaceManager lspWorkspaceManager,
    IClientLanguageServerManager notificationManager,
    FeatureProviderRefresher providerRefresher) : AbstractRefreshQueue(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, providerRefresher)
{
    protected override string GetFeatureAttribute()
        => FeatureAttribute.LanguageServer;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        => (clientCapabilities.Workspace as VSInternalWorkspaceClientCapabilities)?.ProjectContext?.RefreshSupport;

    protected override string GetWorkspaceRefreshName()
        => VSInternalMethods.WorkspaceProjectContextRefreshName;
}
