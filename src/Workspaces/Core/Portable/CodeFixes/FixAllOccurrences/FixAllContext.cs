// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by a <see cref="FixAllProvider"/>.
    /// </summary>
    public partial class FixAllContext : IFixAllContext
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
        /// Document within which fix all occurrences was triggered, null if the <see cref="FixAllContext"/> is scoped to a project.
        /// </summary>
        public Document? Document => State.Document;

        /// <summary>
        /// Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.
        /// </summary>
        public CodeFixProvider CodeFixProvider => State.Provider;

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
        /// Minimum diagnostic severity to fix. Diagnostics with a severity lower than this value will be excluded from
        /// <see cref="GetDocumentDiagnosticsAsync(Document)"/>, <see cref="GetProjectDiagnosticsAsync(Project)"/>, and
        /// <see cref="GetAllDiagnosticsAsync(Project)"/>.
        /// </summary>
        internal DiagnosticSeverity MinimumSeverity => State.MinimumSeverity;

        /// <summary>
        /// The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.
        /// </summary>
        public string? CodeActionEquivalenceKey => State.CodeActionEquivalenceKey;

        /// <summary>
        /// CancellationToken for fix all session.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Progress sink for reporting the progress of a fix-all operation.
        /// </summary>
        public IProgress<CodeAnalysisProgress> Progress { get; }

        #region IFixAllContext implementation
        IFixAllState IFixAllContext.State => this.State;

        IFixAllProvider IFixAllContext.FixAllProvider => this.FixAllProvider;

        object IFixAllContext.Provider => this.CodeFixProvider;

        string IFixAllContext.GetDefaultFixAllTitle()
            => this.GetDefaultFixAllTitle();

        IFixAllContext IFixAllContext.With(
            Optional<(Document? document, Project project)> documentAndProject,
            Optional<FixAllScope> scope,
            Optional<string?> codeActionEquivalenceKey)
            => this.With(documentAndProject, scope, codeActionEquivalenceKey);
        #endregion

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/>.
        /// Use this overload when applying fix all to a diagnostic with a source location.
        /// <para>
        /// This overload cannot be used with <see cref="FixAllScope.ContainingMember"/> or
        /// <see cref="FixAllScope.ContainingType"/> value for the <paramref name="scope"/>.
        /// For those fix all scopes, use the <see cref="FixAllContext"/> constructor that
        /// takes a 'diagnosticSpan' parameter to identify the containing member or type based
        /// on this span.
        /// </para>
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
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
#pragma warning disable RS0030 // Do not used banned APIs - It is fine to invoke the public FixAllContext constructor here.
            : this(document, diagnosticSpan: null, codeFixProvider, scope,
                  codeActionEquivalenceKey, diagnosticIds, minimumSeverity: DiagnosticSeverity.Hidden, fixAllDiagnosticProvider, cancellationToken)
#pragma warning restore RS0030 // Do not used banned APIs
        {
            if (scope is FixAllScope.ContainingMember or FixAllScope.ContainingType)
            {
                throw new ArgumentException(WorkspacesResources.FixAllScope_ContainingType_and_FixAllScope_ContainingMember_are_not_supported_with_this_constructor,
                    nameof(scope));
            }
        }

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/> with an associated <paramref name="diagnosticSpan"/>.
        /// Use this overload when applying fix all to a diagnostic with a source location and
        /// using <see cref="FixAllScope.ContainingMember"/> or <see cref="FixAllScope.ContainingType"/>
        /// for the <paramref name="scope"/>.  When using other fix all scopes, <paramref name="diagnosticSpan"/>
        /// is not required and other constructor which does not take a diagnostic span can be used instead.
        /// </summary>
        /// <param name="document">Document within which fix all occurrences was triggered.</param>
        /// <param name="diagnosticSpan">Span for the diagnostic for which fix all occurrences was triggered.</param>
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
            TextSpan? diagnosticSpan,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
#pragma warning disable RS0030 // Do not used banned APIs - It is fine to invoke the public FixAllContext constructor here.
            : this(document, diagnosticSpan, codeFixProvider, scope,
                  codeActionEquivalenceKey, diagnosticIds, minimumSeverity: DiagnosticSeverity.Hidden, fixAllDiagnosticProvider, cancellationToken)
#pragma warning restore RS0030 // Do not used banned APIs
        {
        }

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/> with an associated <paramref name="diagnosticSpan"/>.
        /// Use this overload when applying fix all to a diagnostic with a source location and
        /// a minimum severity.  When using <see cref="DiagnosticSeverity.Hidden"/> as the severity and/or not filtering
        /// by severity, other overloads may be used.
        /// </summary>
        /// <param name="document">Document within which fix all occurrences was triggered.</param>
        /// <param name="diagnosticSpan">Span for the diagnostic for which fix all occurrences was triggered.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="scope"><see cref="FixAllScope"/> to fix all occurrences.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="diagnosticIds">Diagnostic Ids to fix.</param>
        /// <param name="minimumSeverity">The minimum severity of diagnostics to fix.</param>
        /// <param name="fixAllDiagnosticProvider">
        /// <see cref="DiagnosticProvider"/> to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        internal FixAllContext(
            Document document,
            TextSpan? diagnosticSpan,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticSeverity minimumSeverity,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
            : this(new FixAllState(
                    fixAllProvider: NoOpFixAllProvider.Instance,
                    diagnosticSpan,
                    document ?? throw new ArgumentNullException(nameof(document)),
                    document.Project,
                    codeFixProvider ?? throw new ArgumentNullException(nameof(codeFixProvider)),
                    scope,
                    codeActionEquivalenceKey,
                    PublicContract.RequireNonNullItems(diagnosticIds, nameof(diagnosticIds)),
                    minimumSeverity,
                    fixAllDiagnosticProvider ?? throw new ArgumentNullException(nameof(fixAllDiagnosticProvider)),
                    CodeActionOptions.DefaultProvider),
                  CodeAnalysisProgress.None, cancellationToken)
        {
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
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
#pragma warning disable RS0030 // Do not use banned APIs - It is fine to invoke the public FixAllContext constructor here.
            : this(project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, minimumSeverity: DiagnosticSeverity.Hidden, fixAllDiagnosticProvider, cancellationToken)
#pragma warning restore RS0030 // Do not use banned APIs
        {
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
        /// <param name="minimumSeverity">The minimum severity of diagnostics to fix.</param>
        /// <param name="fixAllDiagnosticProvider">
        /// <see cref="DiagnosticProvider"/> to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        internal FixAllContext(
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            DiagnosticSeverity minimumSeverity,
            DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
            : this(new FixAllState(
                    fixAllProvider: NoOpFixAllProvider.Instance,
                    diagnosticSpan: null,
                    document: null,
                    project ?? throw new ArgumentNullException(nameof(project)),
                    codeFixProvider ?? throw new ArgumentNullException(nameof(codeFixProvider)),
                    scope,
                    codeActionEquivalenceKey,
                    PublicContract.RequireNonNullItems(diagnosticIds, nameof(diagnosticIds)),
                    minimumSeverity,
                    fixAllDiagnosticProvider ?? throw new ArgumentNullException(nameof(fixAllDiagnosticProvider)),
                    CodeActionOptions.DefaultProvider),
                  CodeAnalysisProgress.None, cancellationToken)
        {
            if (scope is FixAllScope.ContainingMember or FixAllScope.ContainingType)
            {
                throw new ArgumentException(WorkspacesResources.FixAllScope_ContainingType_and_FixAllScope_ContainingMember_are_not_supported_with_this_constructor,
                    nameof(scope));
            }
        }

        internal FixAllContext(
            FixAllState state,
            IProgress<CodeAnalysisProgress> progressTracker,
            CancellationToken cancellationToken)
        {
            State = state;
            this.Progress = progressTracker;
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
            return await GetFilteredDiagnosticsAsync(getDiagnosticsTask, this.DiagnosticIds, MinimumSeverity, filterSpan: null).ConfigureAwait(false);
        }

        private static async Task<ImmutableArray<Diagnostic>> GetFilteredDiagnosticsAsync(
            Task<IEnumerable<Diagnostic>> getDiagnosticsTask,
            ImmutableHashSet<string> diagnosticIds,
            DiagnosticSeverity minimumSeverity,
            TextSpan? filterSpan)
        {
            if (getDiagnosticsTask != null)
            {
                var diagnostics = await getDiagnosticsTask.ConfigureAwait(false);
                if (diagnostics != null)
                {
                    return diagnostics.WhereAsArray(
                        static (d, arg) => IsIncluded(d, arg.diagnosticIds, arg.minimumSeverity, arg.filterSpan),
                        (diagnosticIds, minimumSeverity, filterSpan));
                }
            }

            return ImmutableArray<Diagnostic>.Empty;

            static bool IsIncluded(Diagnostic d, ImmutableHashSet<string> diagnosticIds, DiagnosticSeverity minimumSeverity, TextSpan? filterSpan)
            {
                if (d is null)
                {
                    // This isn't allowed by contract, but the data is being provided by a public API so we check.
                    return false;
                }

                if (!diagnosticIds.Contains(d.Id))
                    return false;

                if (d.Severity < minimumSeverity)
                    return false;

                if (filterSpan is not null
                    && !filterSpan.Value.Contains(d.Location.SourceSpan))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets all the diagnostics in the given <paramref name="filterSpan"/> for the given <paramref name="document"/> filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        internal async Task<ImmutableArray<Diagnostic>> GetDocumentSpanDiagnosticsAsync(Document document, TextSpan filterSpan)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (this.Project.Language != document.Project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var getDiagnosticsTask = State.DiagnosticProvider is FixAllContext.SpanBasedDiagnosticProvider spanBasedDiagnosticProvider
                ? spanBasedDiagnosticProvider.GetDocumentSpanDiagnosticsAsync(document, filterSpan, this.CancellationToken)
                : State.DiagnosticProvider.GetDocumentDiagnosticsAsync(document, this.CancellationToken);
            return await GetFilteredDiagnosticsAsync(getDiagnosticsTask, this.DiagnosticIds, MinimumSeverity, filterSpan).ConfigureAwait(false);
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
            return await GetFilteredDiagnosticsAsync(getDiagnosticsTask, this.DiagnosticIds, MinimumSeverity, filterSpan: null).ConfigureAwait(false);
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

            return new FixAllContext(State, this.Progress, cancellationToken);
        }

        internal FixAllContext With(
            Optional<(Document? document, Project project)> documentAndProject = default,
            Optional<FixAllScope> scope = default,
            Optional<string?> codeActionEquivalenceKey = default)
        {
            var newState = State.With(documentAndProject, scope, codeActionEquivalenceKey);
            return State == newState ? this : new FixAllContext(newState, this.Progress, CancellationToken);
        }

        internal Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync()
            => DiagnosticProvider.GetDocumentDiagnosticsToFixAsync(this);

        internal Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync()
            => DiagnosticProvider.GetProjectDiagnosticsToFixAsync(this);
    }
}
