// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Editor.ExternalAccess.VSTypeScript.Api;

internal abstract class AbstractVSTypeScriptRequestHandlerProvider : AbstractRequestHandlerProvider
{
    public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
    {
        return CreateRequestHandlers().SelectAsArray(tsHandler => (IRequestHandler)tsHandler);
    }

    protected abstract ImmutableArray<IVSTypeScriptRequestHandler> CreateRequestHandlers();
}
