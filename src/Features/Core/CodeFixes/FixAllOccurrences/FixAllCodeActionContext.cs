// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// FixAll context with some additional information specifically for <see cref="FixAllCodeAction"/>.
    /// </summary>
    internal class FixAllCodeActionContext : FixAllContext
    {
        private readonly FixAllProviderInfo fixAllProviderInfo;
        private readonly IEnumerable<Diagnostic> originalFixDiagnostics;
        private readonly Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync;
        private readonly Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync;

        internal FixAllCodeActionContext(
            Document document,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
            : base(document, originalFixProvider, FixAllScope.Document,
                  null, GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics),
                  getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync, cancellationToken)
        {
            this.fixAllProviderInfo = fixAllProviderInfo;
            this.originalFixDiagnostics = originalFixDiagnostics;
            this.getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
            this.getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
        }

        internal FixAllCodeActionContext(
            Project project,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
            : base(project, originalFixProvider, FixAllScope.Project,
                  null, GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics),
                  getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync, cancellationToken)
        {
            this.fixAllProviderInfo = fixAllProviderInfo;
            this.originalFixDiagnostics = originalFixDiagnostics;
            this.getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
            this.getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
        }

        private static IEnumerable<string> GetFixAllDiagnosticIds(FixAllProviderInfo fixAllProviderInfo, IEnumerable<Diagnostic> originalFixDiagnostics)
        {
            return originalFixDiagnostics
                .Where(d => fixAllProviderInfo.SupportedDiagnosticIds.Contains(d.Id))
                .Select(d => d.Id);
        }

        public IEnumerable<Diagnostic> OriginalDiagnostics
        {
            get { return this.originalFixDiagnostics; }
        }

        public FixAllProvider FixAllProvider
        {
            get { return this.fixAllProviderInfo.FixAllProvider; }
        }

        public IEnumerable<FixAllScope> SupportedScopes
        {
            get { return this.fixAllProviderInfo.SupportedScopes; }
        }

        /// <summary>
        /// Transforms this context into the public <see cref="FixAllContext"/> to be used for <see cref="FixAllProvider.GetFixAsync(FixAllContext)"/> invocation.
        /// </summary>
        internal FixAllContext GetContextForScopeAndActionId(FixAllScope scope, string codeActionEquivalenceKey)
        {
            if (this.Scope == scope && this.CodeActionEquivalenceKey == codeActionEquivalenceKey)
            {
                return this;
            }

            if (this.Document != null)
            {
                return new FixAllContext(this.Document, this.CodeFixProvider, scope, codeActionEquivalenceKey,
                    this.DiagnosticIds, this.getDocumentDiagnosticsAsync, this.getProjectDiagnosticsAsync, this.CancellationToken);
            }

            return new FixAllContext(this.Project, this.CodeFixProvider, scope, codeActionEquivalenceKey,
                    this.DiagnosticIds, this.getDocumentDiagnosticsAsync, this.getProjectDiagnosticsAsync, this.CancellationToken);
        }
    }
}
