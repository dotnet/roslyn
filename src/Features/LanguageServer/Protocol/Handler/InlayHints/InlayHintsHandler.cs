// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHints
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(InlayHintsHandler)), Shared]
    [Method(LSP.Methods.TextDocumentInlayHint)]
    internal sealed class InlayHintsHandler : ILspServiceDocumentRequestHandler<LSP.InlayHintParams>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlayHintsHandler()
        {
        }

        public bool MutatesSolutionState => throw new NotImplementedException();

        public bool RequiresLSPSolution => throw new NotImplementedException();

        public TextDocumentIdentifier GetTextDocumentIdentifier(object request)
        {
            throw new NotImplementedException();
        }

        public TextDocumentIdentifier GetTextDocumentIdentifier(object request)
        {
            throw new NotImplementedException();
        }
    }
}
