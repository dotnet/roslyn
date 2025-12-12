// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicLspServiceFactory(typeof(WorkDoneProgressManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkDoneProgressManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        return new WorkDoneProgressManager(clientLanguageServerManager);
    }
}