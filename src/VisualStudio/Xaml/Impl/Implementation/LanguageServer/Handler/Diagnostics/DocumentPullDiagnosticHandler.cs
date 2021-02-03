// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Diagnostics
{
    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName, mutatesSolutionState: false, StringConstants.XamlLanguageName), Shared]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticsParams, DiagnosticReport>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandler(
            IXamlPullDiagnosticService xamlPullDiagnosticService)
            : base(xamlPullDiagnosticService)
        { }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticsParams request)
            => request.TextDocument;

        protected override DiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId)
            => new DiagnosticReport { Diagnostics = diagnostics, ResultId = resultId };

        protected override ImmutableArray<Document> GetDocuments(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            return context.Document == null ? ImmutableArray<Document>.Empty : ImmutableArray.Create(context.Document);
        }

        protected override DiagnosticParams[]? GetPreviousResults(DocumentDiagnosticsParams diagnosticsParams)
           => new[] { diagnosticsParams };

        protected override IProgress<DiagnosticReport[]>? GetProgress(DocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PartialResultToken;
    }
}
