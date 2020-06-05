// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class MockDiagnosticService : IDiagnosticService
    {
        public const string DiagnosticId = "MockId";

        private readonly Workspace _workspace;
        private DiagnosticData? _diagnostic;

        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;

        public MockDiagnosticService(Workspace workspace)
            => _workspace = workspace;

        public IEnumerable<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            Assert.Equal(workspace, _workspace);
            Assert.Equal(projectId, GetProjectId());
            Assert.Equal(documentId, GetDocumentId());

            if (_diagnostic == null)
            {
                yield break;
            }
            else
            {
                yield return _diagnostic;
            }
        }

        public IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            Assert.Equal(workspace, _workspace);
            Assert.Equal(projectId, GetProjectId());
            Assert.Equal(documentId, GetDocumentId());

            if (_diagnostic == null)
            {
                yield break;
            }
            else
            {
                yield return new UpdatedEventArgs(this, workspace, GetProjectId(), GetDocumentId());
            }
        }

        internal void CreateDiagnosticAndFireEvents(Location location)
        {
            var document = _workspace.CurrentSolution.Projects.Single().Documents.Single();
            _diagnostic = DiagnosticData.Create(Diagnostic.Create(DiagnosticId, "MockCategory", "MockMessage", DiagnosticSeverity.Error, DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0,
                location: location),
                document);

            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                this, _workspace, _workspace.CurrentSolution,
                GetProjectId(), GetDocumentId(),
                ImmutableArray.Create(_diagnostic)));
        }

        private DocumentId GetDocumentId()
            => _workspace.CurrentSolution.Projects.Single().Documents.Single().Id;

        private ProjectId GetProjectId()
            => _workspace.CurrentSolution.Projects.Single().Id;
    }
}
