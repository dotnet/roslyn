// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

[ExportCSharpVisualBasicLspServiceFactory(typeof(SourceGeneratorRefreshQueue)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratorRefreshQueueFactory(
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        return new SourceGeneratorRefreshQueue(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager);
    }
}