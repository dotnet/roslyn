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
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
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

        public IEnumerable<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

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
            Assert.Equal(projectId, GetProjectId(workspace));
            Assert.Equal(documentId, GetDocumentId(workspace));

            if (_diagnostic == null)
            {
                yield break;
            }
            else
            {
                yield return new UpdatedEventArgs(this, workspace, GetProjectId(workspace), GetDocumentId(workspace));
            }
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
