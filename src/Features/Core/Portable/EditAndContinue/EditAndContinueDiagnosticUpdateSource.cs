// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => ImmutableArray<DiagnosticData>.Empty;

        /// <summary>
        /// Clears all diagnostics reported thru this source.
        /// We do not track the particular reported diagnostics here since we can just clear all of them at once.
        /// </summary>
        public void ClearDiagnostics()
            => DiagnosticsCleared?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Reports given set of diagnostics. 
        /// Categorizes diagnostic into two groups - diagnostics associated with a document and diagnostics associated with a project or solution.
        /// </summary>
        public void ReportDiagnostics(Workspace workspace, Solution solution, ProjectId? projectId, IEnumerable<Diagnostic> diagnostics)
        {
            RoslynDebug.Assert(solution != null);

            var updateEvent = DiagnosticsUpdated;
            if (updateEvent == null)
            {
                return;
            }

            using var _1 = ArrayBuilder<DiagnosticData>.GetInstance(out var documentDiagnosticData);
            using var _2 = ArrayBuilder<DiagnosticData>.GetInstance(out var nonDocumentDiagnosticData);
            var options = solution.Options;
            var project = (projectId != null) ? solution.GetProject(projectId) : null;

            foreach (var diagnostic in diagnostics)
            {
                var document = solution.GetDocument(diagnostic.Location.SourceTree);

                if (document != null)
                {
                    documentDiagnosticData.Add(DiagnosticData.Create(diagnostic, document));
                }
                else if (project != null)
                {
                    nonDocumentDiagnosticData.Add(DiagnosticData.Create(diagnostic, project));
                }
                else
                {
                    nonDocumentDiagnosticData.Add(DiagnosticData.Create(diagnostic, options));
                }
            }

            if (documentDiagnosticData.Count > 0)
            {
                foreach (var (documentId, diagnosticData) in documentDiagnosticData.ToDictionary(data => data.DocumentId))
                {
                    var diagnosticGroupId = (this, documentId, projectId);

                    updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        diagnosticGroupId,
                        workspace,
                        solution,
                        projectId,
                        documentId: documentId,
                        diagnostics: diagnosticData));
                }
            }

            if (nonDocumentDiagnosticData.Count > 0)
            {
                var diagnosticGroupId = (this, projectId);

                updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    diagnosticGroupId,
                    workspace,
                    solution,
                    projectId,
                    documentId: null,
                    diagnostics: nonDocumentDiagnosticData.ToImmutable()));
            }
        }
    }
}
