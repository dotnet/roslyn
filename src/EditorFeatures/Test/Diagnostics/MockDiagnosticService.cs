// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [Export(typeof(IDiagnosticService))]
    [Shared]
    [PartNotDiscoverable]
    internal class MockDiagnosticService : IDiagnosticService
    {
        public const string DiagnosticId = "MockId";

        private DiagnosticData? _diagnostic;

        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDiagnosticService()
        {
        }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => GetPushDiagnostics(workspace, projectId, documentId, id, includeSuppressedDiagnostics, InternalDiagnosticsOptions.NormalDiagnosticMode, cancellationToken);

        public ImmutableArray<DiagnosticData> GetPullDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            return GetDiagnostics(workspace, projectId, documentId);
        }

        public ImmutableArray<DiagnosticData> GetPushDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            return GetDiagnostics(workspace, projectId, documentId);
        }

        private ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId)
        {
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

            return _diagnostic == null ? ImmutableArray<DiagnosticData>.Empty : ImmutableArray.Create(_diagnostic);
        }

        public ImmutableArray<DiagnosticBucket> GetPullDiagnosticBuckets(Workspace workspace, ProjectId? projectId, DocumentId? documentId, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            return GetDiagnosticBuckets(workspace, projectId, documentId);
        }

        public ImmutableArray<DiagnosticBucket> GetPushDiagnosticBuckets(Workspace workspace, ProjectId? projectId, DocumentId? documentId, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            return GetDiagnosticBuckets(workspace, projectId, documentId);
        }

        private ImmutableArray<DiagnosticBucket> GetDiagnosticBuckets(Workspace workspace, ProjectId? projectId, DocumentId? documentId)
        {
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

            return _diagnostic == null
                ? ImmutableArray<DiagnosticBucket>.Empty
                : ImmutableArray.Create(new DiagnosticBucket(this, workspace, GetProjectId(workspace), GetDocumentId(workspace)));
        }

        internal void CreateDiagnosticAndFireEvents(Workspace workspace, Location location)
        {
            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            _diagnostic = DiagnosticData.Create(Diagnostic.Create(DiagnosticId, "MockCategory", "MockMessage", DiagnosticSeverity.Error, DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0,
                location: location),
                document);

            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                this, workspace, workspace.CurrentSolution,
                GetProjectId(workspace), GetDocumentId(workspace),
                ImmutableArray.Create(_diagnostic)));
        }

        private static DocumentId GetDocumentId(Workspace workspace)
            => workspace.CurrentSolution.Projects.Single().Documents.Single().Id;

        private static ProjectId GetProjectId(Workspace workspace)
            => workspace.CurrentSolution.Projects.Single().Id;
    }
}
