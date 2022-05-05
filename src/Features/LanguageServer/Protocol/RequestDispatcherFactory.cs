// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportRoslynLspServiceFactory(typeof(RequestDispatcher))]
    internal class RequestDispatcherFactory : ILspServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RequestDispatcherFactory()
        {
        }

        public virtual ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new RequestDispatcher(lspServices);
        }
    }
}
