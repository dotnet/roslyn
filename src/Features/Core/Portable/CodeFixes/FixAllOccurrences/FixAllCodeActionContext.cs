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
    internal partial class FixAllCodeActionContext : FixAllContext
    {
        private readonly FixAllProviderInfo _fixAllProviderInfo;
        private readonly IEnumerable<Diagnostic> _originalFixDiagnostics;
        private readonly FixAllDiagnosticProvider _diagnosticProvider;

        internal static FixAllCodeActionContext Create(
            Document document,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
        {
            var diagnosticIds = GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics).ToImmutableHashSet();
            var diagnosticProvider = new FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return new FixAllCodeActionContext(document, fixAllProviderInfo, originalFixProvider, originalFixDiagnostics, diagnosticIds, diagnosticProvider, cancellationToken);
        }

        internal static FixAllCodeActionContext Create(
            Project project,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync,
            CancellationToken cancellationToken)
        {
            var diagnosticIds = GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics).ToImmutableHashSet();
            var diagnosticProvider = new FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return new FixAllCodeActionContext(project, fixAllProviderInfo, originalFixProvider, originalFixDiagnostics, diagnosticIds, diagnosticProvider, cancellationToken);
        }

        private FixAllCodeActionContext(
            Document document,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            ImmutableHashSet<string> diagnosticIds,
            FixAllDiagnosticProvider diagnosticProvider,
            CancellationToken cancellationToken)
            : base(document, originalFixProvider, FixAllScope.Document, null, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            _fixAllProviderInfo = fixAllProviderInfo;
            _originalFixDiagnostics = originalFixDiagnostics;
            _diagnosticProvider = diagnosticProvider;
        }

        private FixAllCodeActionContext(
            Project project,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            ImmutableHashSet<string> diagnosticIds,
            FixAllDiagnosticProvider diagnosticProvider,
            CancellationToken cancellationToken)
            : base(project, originalFixProvider, FixAllScope.Project, null, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            _fixAllProviderInfo = fixAllProviderInfo;
            _originalFixDiagnostics = originalFixDiagnostics;
            _diagnosticProvider = diagnosticProvider;
        }

        private static IEnumerable<string> GetFixAllDiagnosticIds(FixAllProviderInfo fixAllProviderInfo, IEnumerable<Diagnostic> originalFixDiagnostics)
        {
            return originalFixDiagnostics
                .Where(fixAllProviderInfo.CanBeFixed)
                .Select(d => d.Id);
        }

        public IEnumerable<Diagnostic> OriginalDiagnostics
        {
            get { return _originalFixDiagnostics; }
        }

        public FixAllProvider FixAllProvider
        {
            get { return _fixAllProviderInfo.FixAllProvider; }
        }

        public IEnumerable<FixAllScope> SupportedScopes
        {
            get { return _fixAllProviderInfo.SupportedScopes; }
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
                    this.DiagnosticIds, _diagnosticProvider, this.CancellationToken);
            }

            return new FixAllContext(this.Project, this.CodeFixProvider, scope, codeActionEquivalenceKey,
                    this.DiagnosticIds, _diagnosticProvider, this.CancellationToken);
        }
    }
}
