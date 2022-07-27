// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(RequestDispatcher<RequestContext>)), Shared]
    internal class RequestDispatcherFactory : ILspServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RequestDispatcherFactory()
        {
        }

        public virtual ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new RoslynRequestDispatcher(lspServices);
        }
    }
}
