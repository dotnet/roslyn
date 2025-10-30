// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[ExportLspServiceFactory(typeof(IRazorClientLanguageServerManager), ProtocolConstants.RoslynLspLanguagesContract)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorClientLanguageServerManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorClientLanguageServerManager(notificationManager);
    }
}
