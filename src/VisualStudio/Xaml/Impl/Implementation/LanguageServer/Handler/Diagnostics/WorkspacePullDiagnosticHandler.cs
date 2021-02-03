// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Diagnostics
{
    [ExportLspMethod(MSLSPMethods.WorkspacePullDiagnosticName, mutatesSolutionState: false, StringConstants.XamlLanguageName), Shared]
    internal class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspacePullDiagnosticHandler(
            IXamlPullDiagnosticService xamlPullDiagnosticService)
            : base(xamlPullDiagnosticService)
        { }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceDocumentDiagnosticsParams request) => null;

        protected override WorkspaceDiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId)
            => new WorkspaceDiagnosticReport { TextDocument = identifier, Diagnostics = diagnostics, ResultId = resultId };

        /// <summary>
        /// Collect all the opened documents from solution. 
        /// In XamlLanguageService, we are only able to retrieve diagnostic information for opened documents. 
        /// So this is the same error experience we have now in full VS scenario.
        /// </summary>
        protected override ImmutableArray<Document> GetDocuments(RequestContext context)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);
            var projects = context.Solution.GetXamlProjects();
            foreach (var project in projects)
            {
                result.AddRange(project.Documents);
            }

            return result.Distinct().ToImmutableArray();
        }

        protected override DiagnosticParams[]? GetPreviousResults(WorkspaceDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults;

        protected override IProgress<WorkspaceDiagnosticReport[]>? GetProgress(WorkspaceDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PartialResultToken;
    }
}
