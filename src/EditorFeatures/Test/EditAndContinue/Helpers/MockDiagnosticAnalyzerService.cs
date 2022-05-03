// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockDiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        public readonly List<DocumentId> DocumentsToReanalyze = new();

        public MockDiagnosticAnalyzerService(IGlobalOptionService globalOptions)
        {
            GlobalOptions = globalOptions;
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false)
            => DocumentsToReanalyze.AddRange(documentIds);

        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache
            => throw new NotImplementedException();

        public IGlobalOptionService GlobalOptions { get; }

        public bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
            => throw new NotImplementedException();

        public Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId = null, DocumentId? documentId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan? range, Func<string, bool>? shouldIncludeDiagnostic, bool includeSuppressedDiagnostics = false, CodeActionRequestPriority priority = CodeActionRequestPriority.None, Func<string, IDisposable?>? addOperationScope = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId = null, ImmutableHashSet<string>? diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, ArrayBuilder<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
