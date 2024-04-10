// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(ResolveDataCache)), Shared]
    internal sealed class ResolveDataCacheFactory : ILspServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ResolveDataCacheFactory()
        {
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
            => new ResolveDataCache();
    }
}
