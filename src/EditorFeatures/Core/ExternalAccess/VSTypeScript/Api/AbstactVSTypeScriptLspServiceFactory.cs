// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal abstract class AbstractVSTypeScriptRequestHandlerFactory : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return CreateRequestHandler();
    }

    protected abstract IVSTypeScriptRequestHandler CreateRequestHandler();
}
