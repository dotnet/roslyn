// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Shared]
[Export(typeof(VSTypeScriptRequestDispatcherFactory))]
internal class VSTypeScriptRequestDispatcherFactory : AbstractRequestDispatcherFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptRequestDispatcherFactory(
        [ImportMany(ProtocolConstants.TypeScriptLanguageContract)] IEnumerable<Lazy<IRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders) : base(requestHandlerProviders)
    {
    }

    public override RequestDispatcher CreateRequestDispatcher(WellKnownLspServerKinds serverKind)
    {
        return base.CreateRequestDispatcher(serverKind);
    }
}
