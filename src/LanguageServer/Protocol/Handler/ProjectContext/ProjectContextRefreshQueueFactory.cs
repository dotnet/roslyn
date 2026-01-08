// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectContext;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ProjectContext;

[ExportCSharpVisualBasicLspServiceFactory(typeof(ProjectContextRefreshQueue)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ProjectContextRefreshQueueFactory(
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
    IProjectContextRefresher refresher) : ILspServiceFactory
{
    private readonly IAsynchronousOperationListenerProvider _asynchronousOperationListenerProvider = asynchronousOperationListenerProvider;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
    private readonly IProjectContextRefresher _refresher = refresher;

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();

        return new ProjectContextRefreshQueue(_asynchronousOperationListenerProvider, _lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, _refresher);
    }
}
