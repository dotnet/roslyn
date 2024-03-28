// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

// This will be removed in future when the cohost server is removed, and we move to dynamic registration.
// It's already marked as Obsolete though, because Roslyn MEF rules :)

[Shared]
[ExportRazorLspServiceFactory(typeof(IRazorCohostClientLanguageServerManager))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorCohostServerClientLanguageServerManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorCohostClientLanguageServerManager(notificationManager);
    }
}
