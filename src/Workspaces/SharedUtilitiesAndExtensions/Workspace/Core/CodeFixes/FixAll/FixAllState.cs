// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            Document? document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
        {
            Debug.Assert(document == null || document.Project == project);

            FixAllProvider = fixAllProvider;
            Document = document;
            Project = project;
            CodeFixProvider = codeFixProvider;
            Scope = scope;
            CodeActionEquivalenceKey = codeActionEquivalenceKey;
            DiagnosticIds = ImmutableHashSet.CreateRange(diagnosticIds);
            DiagnosticProvider = fixAllDiagnosticProvider;
        }

        internal bool IsFixMultiple => this.DiagnosticProvider is FixMultipleDiagnosticProvider;

        public FixAllState WithScope(FixAllScope scope)
            => this.With(scope: scope);

        public FixAllState WithCodeActionEquivalenceKey(string codeActionEquivalenceKey)
            => this.With(codeActionEquivalenceKey: codeActionEquivalenceKey);

        public FixAllState WithProject(Project project)
            => this.With(project: project);

        public FixAllState WithDocument(Document? document)
            => this.With(document: document);

        public FixAllState With(
            Optional<Document?> document = default,
            Optional<Project> project = default,
            Optional<FixAllScope> scope = default,
            Optional<string?> codeActionEquivalenceKey = default)
        {
            var newDocument = document.HasValue ? document.Value : this.Document;
            var newProject = project.HasValue ? project.Value : this.Project;
            var newScope = scope.HasValue ? scope.Value : this.Scope;
            var newCodeActionEquivalenceKey = codeActionEquivalenceKey.HasValue ? codeActionEquivalenceKey.Value : this.CodeActionEquivalenceKey;

            if (newDocument == this.Document &&
                newProject == this.Project &&
                newScope == this.Scope &&
                newCodeActionEquivalenceKey == this.CodeActionEquivalenceKey)
            {
                return this;
            }

            return new FixAllState(
                this.FixAllProvider,
                newDocument,
                newProject,
                this.CodeFixProvider,
                newScope,
                newCodeActionEquivalenceKey,
                this.DiagnosticIds,
                this.DiagnosticProvider);
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
                triggerDocument,
                triggerDocument.Project,
                codeFixProvider,
                FixAllScope.Custom,
                codeActionEquivalenceKey,
                diagnosticIds,
                diagnosticProvider);
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
                document: null,
                triggerProject,
                codeFixProvider,
                FixAllScope.Custom,
                codeActionEquivalenceKey,
                diagnosticIds,
                diagnosticProvider);
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
