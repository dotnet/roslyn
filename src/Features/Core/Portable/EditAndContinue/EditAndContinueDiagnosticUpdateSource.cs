// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Export(typeof(EditAndContinueDiagnosticUpdateSource))]
    [Shared]
    internal sealed class EditAndContinueDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private int _diagnosticsVersion;
        private bool _previouslyHadDiagnostics;

        /// <summary>
        /// Represents an increasing integer version of diagnostics from Edit and Continue, which increments
        /// when diagnostics might have changed even if there is no associated document changes (eg a restart
        /// of an app during Hot Reload)
        /// </summary>
        public int Version => _diagnosticsVersion;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueDiagnosticUpdateSource(IDiagnosticUpdateSourceRegistrationService registrationService)
            => registrationService.Register(this);

        // for testing
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal EditAndContinueDiagnosticUpdateSource()
        {
        }

        public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>? DiagnosticsUpdated;
        public event EventHandler? DiagnosticsCleared;

        /// <summary>
        /// This implementation reports diagnostics via <see cref="DiagnosticsUpdated"/> event.
        /// </summary>
        public bool SupportGetDiagnostics => false;

        public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            => new(ImmutableArray<DiagnosticData>.Empty);

        /// <summary>
        /// Clears all diagnostics reported thru this source.
        /// We do not track the particular reported diagnostics here since we can just clear all of them at once.
        /// </summary>
        public void ClearDiagnostics(bool isSessionEnding = false)
        {
            // If ClearDiagnostics is called and there weren't any diagnostics previously, then there is no point incrementing
            // our version number and potentially invalidating caches unnecessarily.
            // If the debug session is ending, however, we want to always increment otherwise we can get stuck. eg if the user
            // makes a rude edit during a debug session, but doesn't apply the changes, the rude edit will be raised without
            // this class knowing about it, and then if the debug session is stopped, we have no knowledge of any diagnostics here
            // so don't bump our version number, but the document checksum also doesn't change, so we get stuck with the rude edit.
            if (isSessionEnding || _previouslyHadDiagnostics)
            {
                _previouslyHadDiagnostics = false;
                _diagnosticsVersion++;
            }

            DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Reports given set of project or solution level diagnostics. 
        /// </summary>
        public void ReportDiagnostics(Workspace workspace, Solution solution, ImmutableArray<DiagnosticData> diagnostics, ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits)
        {
            RoslynDebug.Assert(solution != null);

            // Even though we only report diagnostics, and not rude edits, we still need to
            // ensure that the presence of rude edits are considered when we decide to update
            // our version number.
            // The array inside rudeEdits won't ever be empty for a given document so we can just
            // check the outer array.
            if (diagnostics.Any() || rudeEdits.Any())
            {
                _previouslyHadDiagnostics = true;
                _diagnosticsVersion++;
            }

            var updateEvent = DiagnosticsUpdated;
            if (updateEvent == null)
            {
                return;
            }

            var documentDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId != null);
            var projectDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId == null && d.ProjectId != null);
            var solutionDiagnostics = diagnostics.WhereAsArray(d => d.DocumentId == null && d.ProjectId == null);

            using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;

            if (documentDiagnostics.Length > 0)
            {
                foreach (var (documentId, diagnosticData) in documentDiagnostics.GroupBy(static data => data.DocumentId!))
                {
                    var diagnosticGroupId = (this, documentId);

                    argsBuilder.Add(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        diagnosticGroupId,
                        workspace,
                        solution,
                        documentId.ProjectId,
                        documentId: documentId,
                        diagnostics: diagnosticData.ToImmutableArray()));
                }
            }

            if (projectDiagnostics.Length > 0)
            {
                foreach (var (projectId, diagnosticData) in projectDiagnostics.GroupBy(static data => data.ProjectId!))
                {
                    var diagnosticGroupId = (this, projectId);

                    argsBuilder.Add(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        diagnosticGroupId,
                        workspace,
                        solution,
                        projectId,
                        documentId: null,
                        diagnostics: diagnosticData.ToImmutableArray()));
                }
            }

            if (solutionDiagnostics.Length > 0)
            {
                var diagnosticGroupId = this;

                argsBuilder.Add(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    diagnosticGroupId,
                    workspace,
                    solution,
                    projectId: null,
                    documentId: null,
                    diagnostics: solutionDiagnostics));
            }

            if (argsBuilder.Count > 0)
            {
                updateEvent(this, argsBuilder.ToImmutableAndClear());
            }
        }
    }
}
