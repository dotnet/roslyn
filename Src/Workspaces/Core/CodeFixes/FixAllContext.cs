// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    public class FixAllContext
    {
        private readonly Func<Project, Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDiagnosticsAsync;
        
        /// <summary>
        /// Solution to fix all occurences.
        /// </summary>
        public Solution Solution { get { return this.Project.Solution; } }

        /// <summary>
        /// Project within which fix all occurences was triggered.
        /// </summary>
        public Project Project { get; private set; }

        /// <summary>
        /// Document within which fix all occurences was triggered.
        /// </summary>
        public Document Document { get; private set; }

        /// <summary>
        /// Underlying <see cref="ICodeFixProvider"/> which triggered this fix all.
        /// </summary>
        public ICodeFixProvider CodeFixProvider { get; private set; }

        /// <summary>
        /// FixAllScope to fix all occurrences.
        /// </summary>
        public FixAllScope Scope { get; private set; }

        /// <summary>
        /// Diagnostic Ids to fix.
        /// Note that <see cref="GetDiagnosticsAsync(Document)"/> and <see cref="GetDiagnosticsAsync(Project)"/> methods
        /// return only diagnostics whose IDs are contained in this set of Ids.
        /// </summary>
        public ImmutableHashSet<string> DiagnosticIds { get; private set; }

        /// <summary>
        /// CodeAction Id to generate a fix all occurrences code fix.
        /// </summary>
        public string CodeActionId { get; private set; }

        /// <summary>
        /// CancellationToken for fix all session.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        internal FixAllContext(
            Document document,
            ICodeFixProvider codeFixProvider, 
            FixAllScope scope,
            string codeActionId,
            IEnumerable<string> diagnosticIds,
            Func<Project, Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDiagnosticsAsync,
            CancellationToken cancellationToken)
            : this(document, document.Project, codeFixProvider, scope, codeActionId, diagnosticIds, getDiagnosticsAsync, cancellationToken)
        {
        }

        internal FixAllContext(
            Project project,
            ICodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionId,
            IEnumerable<string> diagnosticIds,
            Func<Project, Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDiagnosticsAsync,
            CancellationToken cancellationToken)
            : this(null, project, codeFixProvider, scope, codeActionId, diagnosticIds, getDiagnosticsAsync, cancellationToken)
        {
        }

        private FixAllContext(
            Document document,
            Project project,
            ICodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionId,
            IEnumerable<string> diagnosticIds,
            Func<Project, Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDiagnosticsAsync,
            CancellationToken cancellationToken)
        {
            this.Document = document;
            this.Project = project;
            this.CodeFixProvider = codeFixProvider;
            this.Scope = scope;
            this.CodeActionId = codeActionId;
            this.DiagnosticIds = ImmutableHashSet.CreateRange(diagnosticIds);
            this.getDiagnosticsAsync = getDiagnosticsAsync;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets all the diagnostics in the given document filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document)
        {
            Contract.ThrowIfNull(document);

            if (this.Project.Language != document.Project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var diagnostics = await this.getDiagnosticsAsync(document.Project, document, this.DiagnosticIds, this.CancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(diagnostics.All(d => this.DiagnosticIds.Contains(d.Id)));
            return diagnostics.ToImmutableArray();
        }

        /// <summary>
        /// Gets all the diagnostics in the given project filtered by <see cref="DiagnosticIds"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Project project)
        {
            Contract.ThrowIfNull(project);

            if (this.Project.Language != project.Language)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var diagnostics = await this.getDiagnosticsAsync(project, null, this.DiagnosticIds, this.CancellationToken).ConfigureAwait(false);
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
                this.CodeActionId, 
                this.DiagnosticIds, 
                this.getDiagnosticsAsync, 
                cancellationToken);
        }
    }
}
