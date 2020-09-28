// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;

    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName, mutatesSolutionState: false), Shared]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticsParams, DiagnosticReport>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandler(
            ILspSolutionProvider solutionProvider,
            IDiagnosticService diagnosticService)
            : base(solutionProvider, diagnosticService)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticsParams request)
            => request.TextDocument;

        protected override DiagnosticReport CreateReport(TextDocumentIdentifier? identifier, ArrayBuilder<LspDiagnostic>? diagnostics, string? resultId)
            => new DiagnosticReport { Diagnostics = diagnostics?.ToArray(), ResultId = resultId };

        protected override DiagnosticParams? GetPreviousDiagnosticParams(DocumentDiagnosticsParams? diagnosticParams, Document? document)
            => diagnosticParams;

        protected override TextDocumentIdentifier? GetTextDocument(DocumentDiagnosticsParams? diagnosticParams, Document? document, RequestContext context)
            => diagnosticParams?.TextDocument;
    }
}
