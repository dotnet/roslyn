// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    public class FixAllContext
    {
        private readonly Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getDocumentDiagnosticsAsync;
        private readonly Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getProjectDiagnosticsAsync;

        /// <summary>
        /// Solution to fix all occurrences.
        /// </summary>
        public Solution Solution { get { return this.Project.Solution; } }

        /// <summary>
        /// Project within which fix all occurrences was triggered.
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// Document within which fix all occurrences was triggered.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.
        /// </summary>
        public CodeFixProvider CodeFixProvider { get; }

        /// <summary>
        /// FixAllScope to fix all occurrences.
        /// </summary>
        public FixAllScope Scope { get; }

        /// <summary>
        /// Diagnostic Ids to fix.
        /// Note that <see cref="GetDocumentDiagnosticsAsync(Document)"/>, <see cref="GetProjectDiagnosticsAsync(Project)"/> and <see cref="GetAllDiagnosticsAsync(Project)"/> methods
        /// return only diagnostics whose IDs are contained in this set of Ids.
        /// </summary>
        public ImmutableHashSet<string> DiagnosticIds { get; }

        /// <summary>
        /// The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.
        /// </summary>
        public string CodeActionEquivalenceKey { get; }

        /// <summary>
        /// CancellationToken for fix all session.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal FixAllContext(
            Document document,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionId,
            IEnumerable<string> diagnosticIds,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
            : this(document, document.Project, codeFixProvider, scope, codeActionId, diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync, cancellationToken)
        {
        }

        internal FixAllContext(
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionId,
            IEnumerable<string> diagnosticIds,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
            : this(null, project, codeFixProvider, scope, codeActionId, diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync, cancellationToken)
        {
        }

        private FixAllContext(
            Document document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
        {
            this.Document = document;
            this.Project = project;
            this.CodeFixProvider = codeFixProvider;
            this.Scope = scope;
            this.CodeActionEquivalenceKey = codeActionEquivalenceKey;
            this.DiagnosticIds = ImmutableHashSet.CreateRange(diagnosticIds);
            _getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
            _getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets all the diagnostics in the given document filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document)
        {
            Contract.ThrowIfNull(document);

            if (this.Project.Language != document.Project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var diagnostics = await _getDocumentDiagnosticsAsync(document, this.DiagnosticIds, this.CancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(diagnostics.All(d => this.DiagnosticIds.Contains(d.Id)));
            return diagnostics.ToImmutableArray();
        }

        /// <summary>
        /// Gets all the project-level diagnostics, i.e. diagnostics with no source location, in the given project filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(Project project)
        {
            return await GetProjectDiagnosticsAsync(project, includeAllDocumentDiagnostics: false).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all the diagnostics in the given project filtered by <see cref="DiagnosticIds"/>.
        /// This includes both document-level diagnostics for all documents in the given project and project-level diagnostics, i.e. diagnostics with no source location, in the given project. 
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(Project project)
        {
            return await GetProjectDiagnosticsAsync(project, includeAllDocumentDiagnostics: true).ConfigureAwait(false);
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

            var diagnostics = await _getProjectDiagnosticsAsync(project, includeAllDocumentDiagnostics, this.DiagnosticIds, this.CancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(diagnostics.All(d => this.DiagnosticIds.Contains(d.Id)));
            return diagnostics.ToImmutableArray();
        }

        /// <summary>
        /// Gets a new <see cref="FixAllContext"/> with the given cancellationToken.
        /// </summary>
        public FixAllContext WithCancellationToken(CancellationToken cancellationToken)
        {
            if (this.CancellationToken == cancellationToken)
            {
                return this;
            }

            return new FixAllContext(
                this.Document,
                this.Project,
                this.CodeFixProvider,
                this.Scope,
                this.CodeActionEquivalenceKey,
                this.DiagnosticIds,
                _getDocumentDiagnosticsAsync,
                _getProjectDiagnosticsAsync,
                cancellationToken);
        }
    }
}
