// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

internal abstract class AbstractCopilotLspServiceFactory : ILspServiceFactory
{
    public abstract AbstractCopilotLspService CreateILspService(CopilotLspServices lspServices);

    ILspService ILspServiceFactory.CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return CreateILspService(new CopilotLspServices(lspServices));
    }
}
