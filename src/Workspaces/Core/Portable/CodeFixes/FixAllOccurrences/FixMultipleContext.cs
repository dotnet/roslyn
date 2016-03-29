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
    /// Context for "Fix multiple occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    internal partial class FixMultipleContext : FixAllContext
    {
        private readonly FixMultipleDiagnosticProvider _diagnosticProvider;

        /// <summary>
        /// Creates a new <see cref="FixMultipleContext"/>.
        /// Use this overload when applying fix multiple diagnostics with a source location.
        /// </summary>
        /// <param name="diagnosticsToFix">Specific set of diagnostics to fix. Must be a non-empty set.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        public static FixMultipleContext Create(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey,
            CancellationToken cancellationToken)
        {
            var triggerDocument = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixMultipleContext(triggerDocument, codeFixProvider, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider, cancellationToken);
        }

        /// <summary>
        /// Creates a new <see cref="FixMultipleContext"/>.
        /// Use this overload when applying fix multiple diagnostics with no source location.
        /// </summary>
        /// <param name="diagnosticsToFix">Specific set of diagnostics to fix. Must be a non-empty set.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        public static FixMultipleContext Create(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey,
            CancellationToken cancellationToken)
        {
            var triggerProject = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixMultipleContext(triggerProject, codeFixProvider, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider, cancellationToken);
        }

        private FixMultipleContext(
            Document triggerDocument,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey,
            ImmutableHashSet<string> diagnosticIds,
            FixMultipleDiagnosticProvider diagnosticProvider,
            CancellationToken cancellationToken)
            : base(triggerDocument, codeFixProvider, FixAllScope.Custom, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            _diagnosticProvider = diagnosticProvider;
        }

        private FixMultipleContext(
            Project triggerProject,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey,
            ImmutableHashSet<string> diagnosticIds,
            FixMultipleDiagnosticProvider diagnosticProvider,
            CancellationToken cancellationToken)
            : base(triggerProject, codeFixProvider, FixAllScope.Custom, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider, cancellationToken)
        {
            _diagnosticProvider = diagnosticProvider;
        }

        private static ImmutableHashSet<string> GetDiagnosticsIds(IEnumerable<ImmutableArray<Diagnostic>> diagnosticsCollection)
        {
            var uniqueIds = ImmutableHashSet.CreateBuilder<string>();
            foreach (var diagnostics in diagnosticsCollection)
            {
                foreach (var diagnostic in diagnostics)
                {
                    uniqueIds.Add(diagnostic.Id);
                }
            }

            return uniqueIds.ToImmutable();
        }

        public ImmutableDictionary<Document, ImmutableArray<Diagnostic>> DocumentDiagnosticsToFix => _diagnosticProvider.DocumentDiagnosticsToFix;
        public ImmutableDictionary<Project, ImmutableArray<Diagnostic>> ProjectDiagnosticsToFix => _diagnosticProvider.ProjectDiagnosticsToFix;

        public Diagnostic GetTriggerDiagnostic()
        {
            if (Document != null)
            {
                return DocumentDiagnosticsToFix[Document].First();
            }

            return ProjectDiagnosticsToFix[Project].First();
        }

        public new FixAllContext WithCancellationToken(CancellationToken cancellationToken)
        {
            if (this.CancellationToken == cancellationToken)
            {
                return this;
            }

            return Document != null ?
                new FixMultipleContext(Document, CodeFixProvider, CodeActionEquivalenceKey, DiagnosticIds, _diagnosticProvider, cancellationToken) :
                new FixMultipleContext(Project, CodeFixProvider, CodeActionEquivalenceKey, DiagnosticIds, _diagnosticProvider, cancellationToken);
        }
    }
}
