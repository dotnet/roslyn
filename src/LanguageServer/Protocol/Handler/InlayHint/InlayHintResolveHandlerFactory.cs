// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(InlayHintResolveHandler)), Shared]
    internal sealed class InlayHintResolveHandlerFactory : ILspServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintResolveHandlerFactory()
        {
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var inlayHintCache = lspServices.GetRequiredService<InlayHintCache>();
            return new InlayHintResolveHandler(inlayHintCache);
        }
    }
}
