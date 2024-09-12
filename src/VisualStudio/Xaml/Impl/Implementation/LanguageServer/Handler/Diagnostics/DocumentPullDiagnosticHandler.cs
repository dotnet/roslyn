// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Diagnostics
{
    [ExportStatelessXamlLspService(typeof(DocumentPullDiagnosticHandler)), Shared]
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport>, ITextDocumentIdentifierHandler<VSInternalDocumentDiagnosticsParams, TextDocumentIdentifier?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandler(
            IXamlPullDiagnosticService xamlPullDiagnosticService)
            : base(xamlPullDiagnosticService)
        { }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalDocumentDiagnosticsParams request)
            => request.TextDocument;

        protected override VSInternalDiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId)
            => new VSInternalDiagnosticReport { Diagnostics = diagnostics, ResultId = resultId };

        protected override ImmutableArray<Document> GetDocuments(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            return context.Document == null ? ImmutableArray<Document>.Empty : [context.Document];
        }

        protected override VSInternalDiagnosticParams[]? GetPreviousResults(VSInternalDocumentDiagnosticsParams diagnosticsParams)
           => new[] { diagnosticsParams };

        protected override IProgress<VSInternalDiagnosticReport[]>? GetProgress(VSInternalDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PartialResultToken;
    }
}
