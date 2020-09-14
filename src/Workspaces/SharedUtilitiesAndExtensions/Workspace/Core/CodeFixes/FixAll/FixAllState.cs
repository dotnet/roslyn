// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class FixAllState
    {
        internal readonly int CorrelationId = LogAggregator.GetNextId();

        internal FixAllContext.DiagnosticProvider DiagnosticProvider { get; }

        public FixAllProvider? FixAllProvider { get; }
        public string? CodeActionEquivalenceKey { get; }
        public CodeFixProvider CodeFixProvider { get; }
        public ImmutableHashSet<string> DiagnosticIds { get; }
        public Document? Document { get; }
        public Project Project { get; }
        public FixAllScope Scope { get; }
        public Solution Solution => this.Project.Solution;

        internal FixAllState(
            FixAllProvider? fixAllProvider,
            Document document,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
            : this(fixAllProvider, document, document.Project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
        }

        internal FixAllState(
            FixAllProvider? fixAllProvider,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
            : this(fixAllProvider, null, project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
        }

        private FixAllState(
            FixAllProvider? fixAllProvider,
            Document? document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
        {
            Contract.ThrowIfNull(project);
            if (diagnosticIds == null)
            {
                throw new ArgumentNullException(nameof(diagnosticIds));
            }

            if (diagnosticIds.Any(d => d == null))
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Supplied_diagnostic_cannot_be_null, nameof(diagnosticIds));
            }

            this.FixAllProvider = fixAllProvider;
            this.Document = document;
            this.Project = project;
            this.CodeFixProvider = codeFixProvider ?? throw new ArgumentNullException(nameof(codeFixProvider));
            this.Scope = scope;
            this.CodeActionEquivalenceKey = codeActionEquivalenceKey;
            this.DiagnosticIds = ImmutableHashSet.CreateRange(diagnosticIds);
            this.DiagnosticProvider = fixAllDiagnosticProvider ?? throw new ArgumentNullException(nameof(fixAllDiagnosticProvider));
        }

        internal bool IsFixMultiple => this.DiagnosticProvider is FixMultipleDiagnosticProvider;

        public FixAllState WithScopeAndEquivalenceKey(FixAllScope scope, string? codeActionEquivalenceKey)
        {
            if (this.Scope == scope && this.CodeActionEquivalenceKey == codeActionEquivalenceKey)
            {
                return this;
            }

            return new FixAllState(
                this.FixAllProvider,
                this.Document, this.Project, this.CodeFixProvider,
                scope, codeActionEquivalenceKey,
                this.DiagnosticIds, this.DiagnosticProvider);
        }

        #region FixMultiple

        internal static FixAllState Create(
            FixAllProvider fixAllProvider,
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey)
        {
            var triggerDocument = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixAllState(
                fixAllProvider,
                triggerDocument, codeFixProvider,
                FixAllScope.Custom, codeActionEquivalenceKey,
                diagnosticIds, diagnosticProvider);
        }

        internal static FixAllState Create(
            FixAllProvider fixAllProvider,
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey)
        {
            var triggerProject = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixAllState(
                fixAllProvider,
                triggerProject, codeFixProvider,
                FixAllScope.Custom, codeActionEquivalenceKey,
                diagnosticIds, diagnosticProvider);
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

        #endregion
    }
}
