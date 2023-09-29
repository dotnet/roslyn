// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class WorkspaceDocumentDiagnosticSource : AbstractDocumentDiagnosticSource<TextDocument>
{
    private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer;
    private readonly bool _cachedDiagnosticsOnly;

    public WorkspaceDocumentDiagnosticSource(TextDocument document, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool cachedDiagnosticsOnly)
        : base(document)
    {
        _shouldIncludeAnalyzer = shouldIncludeAnalyzer;
        _cachedDiagnosticsOnly = cachedDiagnosticsOnly;
    }

    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (_cachedDiagnosticsOnly)
        {
            Debug.Assert(diagnosticAnalyzerService.WasForceAnalyzed(Document.Project.Id));
            var diagnostics = await diagnosticAnalyzerService.GetCachedDiagnosticsAsync(Document.Project.Solution.Workspace,
                Document.Project.Id, Document.Id, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
            return diagnostics;
        }

        if (Document is SourceGeneratedDocument sourceGeneratedDocument)
        {
            // Unfortunately GetDiagnosticsForIdsAsync returns nothing for source generated documents.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(sourceGeneratedDocument, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return documentDiagnostics;
        }
        else
        {
            // We call GetDiagnosticsForIdsAsync as we want to ensure we get the full set of diagnostics for this document
            // including those reported as a compilation end diagnostic.  These are not included in document pull (uses GetDiagnosticsForSpan) due to cost.
            // However we can include them as a part of workspace pull when FSA is on.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(
                Document.Project.Solution, Document.Project.Id, Document.Id,
                diagnosticIds: null, _shouldIncludeAnalyzer, includeSuppressedDiagnostics: false,
                includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
            return documentDiagnostics;
        }
    }
}
