// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService)), Shared, PartNotDiscoverable]
    internal class MockDiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        private readonly ArrayBuilder<(DiagnosticData Diagnostic, DiagnosticKind KindFilter)> _diagnosticsWithKindFilter;
        public readonly List<DocumentId> DocumentsToReanalyze = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDiagnosticAnalyzerService(IGlobalOptionService globalOptions)
        {
            GlobalOptions = globalOptions;
            _diagnosticsWithKindFilter = ArrayBuilder<(DiagnosticData Diagnostic, DiagnosticKind KindFilter)>.GetInstance();
        }

        public void AddDiagnostic(DiagnosticData diagnostic, DiagnosticKind diagnosticKind)
            => _diagnosticsWithKindFilter.Add((diagnostic, diagnosticKind));

        public void AddDiagnostics(ImmutableArray<DiagnosticData> diagnostics, DiagnosticKind diagnosticKind)
        {
            foreach (var diagnostic in diagnostics)
                AddDiagnostic(diagnostic, diagnosticKind);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds, bool highPriority)
            => DocumentsToReanalyze.AddRange(documentIds);

        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache
            => throw new NotImplementedException();

        public IGlobalOptionService GlobalOptions { get; }

        public bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
            => throw new NotImplementedException();

        public Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(TextDocument document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic, bool includeCompilerDiagnostics, bool includeSuppressedDiagnostics, ICodeActionRequestPriorityProvider priorityProvider, Func<string, IDisposable?>? addOperationScope, DiagnosticKind diagnosticKind, bool isExplicit, CancellationToken cancellationToken)
            => Task.FromResult(_diagnosticsWithKindFilter.Where(d => diagnosticKind == d.KindFilter).Select(d => d.Diagnostic).ToImmutableArray());

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<(ImmutableArray<DiagnosticData> diagnostics, bool upToDate)> TryGetDiagnosticsForSpanAsync(TextDocument document, TextSpan range, Func<string, bool>? shouldIncludeDiagnostic, bool includeSuppressedDiagnostics, ICodeActionRequestPriorityProvider priorityProvider, DiagnosticKind diagnosticKind, bool isExplicit, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
