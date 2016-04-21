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
    internal static partial class FixAllCodeActionContext 
    {
        internal static FixAllContext Create(
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
            return new FixAllContext(
                document: document,
                codeFixProvider: originalFixProvider,
                scope: FixAllScope.Document,
                codeActionEquivalenceKey: null,
                diagnosticIds: diagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider,
                cancellationToken: cancellationToken);
        }

        internal static FixAllContext Create(
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
            return new FixAllContext(
                project: project, 
                codeFixProvider: originalFixProvider, 
                scope: FixAllScope.Project, 
                codeActionEquivalenceKey: null, diagnosticIds: diagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider,
                cancellationToken: cancellationToken);
        }

        private static IEnumerable<string> GetFixAllDiagnosticIds(FixAllProviderInfo fixAllProviderInfo, IEnumerable<Diagnostic> originalFixDiagnostics)
        {
            return originalFixDiagnostics
                .Where(fixAllProviderInfo.CanBeFixed)
                .Select(d => d.Id);
        }

#if false

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
#endif
    }
}
