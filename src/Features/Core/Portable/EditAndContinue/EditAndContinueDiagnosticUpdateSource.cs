// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Export(typeof(EditAndContinueDiagnosticUpdateSource))]
    [Shared]
    internal sealed class EditAndContinueDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueDiagnosticUpdateSource(IDiagnosticUpdateSourceRegistrationService registrationService)
            => registrationService.Register(this);

        // for testing
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal EditAndContinueDiagnosticUpdateSource()
        {
        }

        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;
        public event EventHandler? DiagnosticsCleared;

        /// <summary>
        /// This implementation reports diagnostics via <see cref="DiagnosticsUpdated"/> event.
        /// </summary>
        public bool SupportGetDiagnostics => false;

        public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => new(ImmutableArray<DiagnosticData>.Empty);

        /// <summary>
        /// Clears all diagnostics reported thru this source.
        /// We do not track the particular reported diagnostics here since we can just clear all of them at once.
        /// </summary>
        public void ClearDiagnostics()
            => DiagnosticsCleared?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Reports given set of project or solution level diagnostics. 
        /// </summary>
        public void ReportDiagnostics(Workspace workspace, Solution solution, ImmutableArray<DiagnosticData> diagnostics)
        {
            RoslynDebug.Assert(solution != null);

            var updateEvent = DiagnosticsUpdated;
            if (updateEvent == null)
            {
                return;
            }

            var documentDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId != null);
            var projectDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId == null && d.ProjectId != null);
            var solutionDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId == null && d.ProjectId == null);

            if (documentDiagnostics.Length > 0)
            {
                foreach (var (documentId, diagnosticData) in documentDiagnostics.ToDictionary(data => data.DocumentId!))
                {
                    var diagnosticGroupId = (this, documentId);

                    updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        diagnosticGroupId,
                        workspace,
                        solution,
                        documentId.ProjectId,
                        documentId: documentId,
                        diagnostics: diagnosticData));
                }
            }

            if (projectDiagnostics.Length > 0)
            {
                foreach (var (projectId, diagnosticData) in projectDiagnostics.ToDictionary(data => data.ProjectId!))
                {
                    var diagnosticGroupId = (this, projectId);

                    updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        diagnosticGroupId,
                        workspace,
                        solution,
                        projectId,
                        documentId: null,
                        diagnostics: diagnosticData));
                }
            }

            if (solutionDiagnostics.Length > 0)
            {
                var diagnosticGroupId = this;

                updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    diagnosticGroupId,
                    workspace,
                    solution,
                    projectId: null,
                    documentId: null,
                    diagnostics: solutionDiagnostics));
            }
        }
    }
}
