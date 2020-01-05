// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by a <see cref="FixAllProvider"/>.
    /// </summary>
    public partial class FixAllContext
    {
        internal FixAllState State { get; }

        internal FixAllProvider FixAllProvider => State.FixAllProvider;

        /// <summary>
        /// Solution to fix all occurrences.
        /// </summary>
        public Solution Solution => State.Solution;

        /// <summary>
        /// Project within which fix all occurrences was triggered.
        /// </summary>
        public Project Project => State.Project;

        /// <summary>
        /// Document within which fix all occurrences was triggered.
        /// Can be null if the context was created using <see cref="FixAllContext.FixAllContext(Project, CodeFixProvider, FixAllScope, string, IEnumerable{string}, DiagnosticProvider, CancellationToken)"/>.
        /// </summary>
        public Document Document => State.Document;

        /// <summary>
        /// Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.
        /// </summary>
        public CodeFixProvider CodeFixProvider => State.CodeFixProvider;

        /// <summary>
        /// <see cref="FixAllScope"/> to fix all occurrences.
        /// </summary>
        public FixAllScope Scope => State.Scope;

        /// <summary>
        /// Diagnostic Ids to fix.
        /// Note that <see cref="GetDocumentDiagnosticsAsync(Document)"/>, <see cref="GetProjectDiagnosticsAsync(Project)"/> and <see cref="GetAllDiagnosticsAsync(Project)"/> methods
        /// return only diagnostics whose IDs are contained in this set of Ids.
        /// </summary>
        public ImmutableHashSet<string> DiagnosticIds => State.DiagnosticIds;

        /// <summary>
        /// The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.
        /// </summary>
        public string CodeActionEquivalenceKey => State.CodeActionEquivalenceKey;

        /// <summary>
        /// CancellationToken for fix all session.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal IProgressTracker ProgressTracker { get; }

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/>.
        /// Use this overload when applying fix all to a diagnostic with a source location.
        /// </summary>
        /// <param name="document">Document within which fix all occurrences was triggered.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="scope"><see cref="FixAllScope"/> to fix all occurrences.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="diagnosticIds">Diagnostic Ids to fix.</param>
        /// <param name="fixAllDiagnosticProvider">
        /// <see cref="DiagnosticProvider"/> to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        public FixAllContext(
            Document document,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
            : this(new FixAllState(null, document, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider),
                  new ProgressTracker(), cancellationToken)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
        }

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/>.
        /// Use this overload when applying fix all to a diagnostic with no source location, i.e. <see cref="Location.None"/>.
        /// </summary>
        /// <param name="project">Project within which fix all occurrences was triggered.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="scope"><see cref="FixAllScope"/> to fix all occurrences.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="diagnosticIds">Diagnostic Ids to fix.</param>
        /// <param name="fixAllDiagnosticProvider">
        /// <see cref="DiagnosticProvider"/> to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        public FixAllContext(
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
            : this(new FixAllState(null, project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider),
                  new ProgressTracker(), cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
        }

        internal FixAllContext(
            FixAllState state,
            IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            State = state;
            this.ProgressTracker = progressTracker;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets all the diagnostics in the given document filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (this.Project.Language != document.Project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var getDiagnosticsTask = State.DiagnosticProvider.GetDocumentDiagnosticsAsync(document, this.CancellationToken);
            return await GetFilteredDiagnosticsAsync(getDiagnosticsTask, this.DiagnosticIds).ConfigureAwait(false);
        }

        private static async Task<ImmutableArray<Diagnostic>> GetFilteredDiagnosticsAsync(Task<IEnumerable<Diagnostic>> getDiagnosticsTask, ImmutableHashSet<string> diagnosticIds)
        {
            if (getDiagnosticsTask != null)
            {
                var diagnostics = await getDiagnosticsTask.ConfigureAwait(false);
                if (diagnostics != null)
                {
                    return diagnostics.Where(d => d != null && diagnosticIds.Contains(d.Id)).ToImmutableArray();
                }
            }

            return ImmutableArray<Diagnostic>.Empty;
        }

        /// <summary>
        /// Gets all the project-level diagnostics, i.e. diagnostics with no source location, in the given project filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return GetProjectDiagnosticsAsync(project, includeAllDocumentDiagnostics: false);
        }

        /// <summary>
        /// Gets all the diagnostics in the given project filtered by <see cref="DiagnosticIds"/>.
        /// This includes both document-level diagnostics for all documents in the given project and project-level diagnostics, i.e. diagnostics with no source location, in the given project. 
        /// </summary>
        public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return GetProjectDiagnosticsAsync(project, includeAllDocumentDiagnostics: true);
        }

        /// <summary>
        /// Gets all the project diagnostics in the given project filtered by <see cref="DiagnosticIds"/>.
        /// If <paramref name="includeAllDocumentDiagnostics"/> is false, then returns only project-level diagnostics which have no source location.
        /// Otherwise, returns all diagnostics in the project, including the document diagnostics for all documents in the given project.
        /// </summary>
        private async Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(Project project, bool includeAllDocumentDiagnostics)
        {
            Contract.ThrowIfNull(project);

            if (this.Project.Language != project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var getDiagnosticsTask = includeAllDocumentDiagnostics
                ? State.DiagnosticProvider.GetAllDiagnosticsAsync(project, CancellationToken)
                : State.DiagnosticProvider.GetProjectDiagnosticsAsync(project, CancellationToken);
            return await GetFilteredDiagnosticsAsync(getDiagnosticsTask, this.DiagnosticIds).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a new <see cref="FixAllContext"/> with the given cancellationToken.
        /// </summary>
        public FixAllContext WithCancellationToken(CancellationToken cancellationToken)
        {
            // TODO: We should change this API to be a virtual method, as the class is not sealed.
            if (this.CancellationToken == cancellationToken)
            {
                return this;
            }

            return new FixAllContext(State, this.ProgressTracker, cancellationToken);
        }

        internal Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync()
        {
            return State.DiagnosticProvider.GetDocumentDiagnosticsToFixAsync(this);
        }

        internal Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync()
        {
            return State.DiagnosticProvider.GetProjectDiagnosticsToFixAsync(this);
        }
    }
}
