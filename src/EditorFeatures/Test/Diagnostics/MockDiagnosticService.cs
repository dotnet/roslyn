// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared, PartNotDiscoverable]
    internal class MockDiagnosticService : IDiagnosticService
    {
        public const string DiagnosticId = "MockId";

        private DiagnosticData? _diagnosticData;

        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDiagnosticService(IGlobalOptionService globalOptions)
        {
        }

        public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            return new ValueTask<ImmutableArray<DiagnosticData>>(GetDiagnostics(workspace, projectId, documentId));
        }

        private ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId)
        {
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

            return _diagnosticData == null ? ImmutableArray<DiagnosticData>.Empty : ImmutableArray.Create(_diagnosticData);
        }

        public ImmutableArray<DiagnosticBucket> GetDiagnosticBuckets(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, CancellationToken cancellationToken)
        {
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

            return _diagnosticData == null
                ? ImmutableArray<DiagnosticBucket>.Empty
                : ImmutableArray.Create(new DiagnosticBucket(this, workspace, GetProjectId(workspace), GetDocumentId(workspace)));
        }

        internal void CreateDiagnosticAndFireEvents(Workspace workspace, MockDiagnosticAnalyzerService analyzerService, Location location, DiagnosticKind diagnosticKind, bool isSuppressed)
        {
            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            _diagnosticData = DiagnosticData.Create(Diagnostic.Create(DiagnosticId, "MockCategory", "MockMessage", DiagnosticSeverity.Error, DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0, isSuppressed: isSuppressed,
                location: location),
                document);

            analyzerService.AddDiagnostic(_diagnosticData, diagnosticKind);
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                this, workspace, workspace.CurrentSolution,
                GetProjectId(workspace), GetDocumentId(workspace),
                ImmutableArray.Create(_diagnosticData)));
        }

        private static DocumentId GetDocumentId(Workspace workspace)
            => workspace.CurrentSolution.Projects.Single().Documents.Single().Id;

        private static ProjectId GetProjectId(Workspace workspace)
            => workspace.CurrentSolution.Projects.Single().Id;
    }
}
