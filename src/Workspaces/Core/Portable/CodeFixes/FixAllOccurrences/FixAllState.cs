// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class FixAllState
    {
        internal readonly int CorrelationId = LogAggregator.GetNextId();

        internal FixAllContext.DiagnosticProvider DiagnosticProvider { get; }

        public FixAllProvider FixAllProvider { get; }
        public string CodeActionEquivalenceKey { get; }
        public CodeFixProvider CodeFixProvider { get; }
        public ImmutableHashSet<string> DiagnosticIds { get; }
        public Document Document { get; }
        public Project Project { get; }
        public FixAllScope Scope { get; }
        public Solution Solution => this.Project.Solution;

        internal FixAllState(
            FixAllProvider fixAllProvider,
            Document document,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
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
            FixAllProvider fixAllProvider,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
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
            FixAllProvider fixAllProvider,
            Document document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
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
                throw new ArgumentException(WorkspacesResources.Supplied_diagnostic_cannot_be_null, nameof(diagnosticIds));
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

        internal bool IsFixMultiple => this.DiagnosticProvider.IsFixMultiple;

        public FixAllState WithScopeAndEquivalenceKey(FixAllScope scope, string codeActionEquivalenceKey)
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

        public FixAllContext CreateFixAllContext(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return new FixAllContext(this, progressTracker, cancellationToken);
        }

        internal string GetDefaultFixAllTitle()
        {
            var diagnosticIds = this.DiagnosticIds;
            string diagnosticId;
            if (diagnosticIds.Count == 1)
            {
                diagnosticId = diagnosticIds.Single();
            }
            else
            {
                diagnosticId = string.Join(",", diagnosticIds.ToArray());
            }

            switch (this.Scope)
            {
                case FixAllScope.Custom:
                    return string.Format(WorkspacesResources.Fix_all_0, diagnosticId);

                case FixAllScope.Document:
                    var document = this.Document;
                    return string.Format(WorkspacesResources.Fix_all_0_in_1, diagnosticId, document.Name);

                case FixAllScope.Project:
                    var project = this.Project;
                    return string.Format(WorkspacesResources.Fix_all_0_in_1, diagnosticId, project.Name);

                case FixAllScope.Solution:
                    return string.Format(WorkspacesResources.Fix_all_0_in_Solution, diagnosticId);

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Scope);
            }
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
