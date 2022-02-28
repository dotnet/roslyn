// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal sealed partial class FixAllState
    {
        public readonly int CorrelationId = LogAggregator.GetNextId();

        public FixAllContext.DiagnosticProvider DiagnosticProvider { get; }

        public FixAllProvider? FixAllProvider { get; }
        public string? CodeActionEquivalenceKey { get; }
        public CodeFixProvider CodeFixProvider { get; }
        public ImmutableHashSet<string> DiagnosticIds { get; }
        public Document? Document { get; }
        public Project Project { get; }
        public FixAllScope Scope { get; }
        public CodeActionOptionsProvider CodeActionOptionsProvider { get; }

        internal FixAllState(
            FixAllProvider? fixAllProvider,
            Document? document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
            CodeActionOptionsProvider codeActionOptionsProvider)
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
            CodeActionOptionsProvider = codeActionOptionsProvider;
        }

        public Solution Solution => Project.Solution;
        internal bool IsFixMultiple => DiagnosticProvider is FixMultipleDiagnosticProvider;

        public FixAllState WithScope(FixAllScope scope)
            => With(scope: scope);

        public FixAllState WithCodeActionEquivalenceKey(string? codeActionEquivalenceKey)
            => With(codeActionEquivalenceKey: codeActionEquivalenceKey);

        public FixAllState WithDocumentAndProject(Document? document, Project project)
            => With(documentAndProject: (document, project));

        public FixAllState With(
            Optional<(Document? document, Project project)> documentAndProject = default,
            Optional<FixAllScope> scope = default,
            Optional<string?> codeActionEquivalenceKey = default)
        {
            var (newDocument, newProject) = documentAndProject.HasValue ? documentAndProject.Value : (Document, Project);
            var newScope = scope.HasValue ? scope.Value : Scope;
            var newCodeActionEquivalenceKey = codeActionEquivalenceKey.HasValue ? codeActionEquivalenceKey.Value : CodeActionEquivalenceKey;

            if (newDocument == Document &&
                newProject == Project &&
                newScope == Scope &&
                newCodeActionEquivalenceKey == CodeActionEquivalenceKey)
            {
                return this;
            }

            return new FixAllState(
                FixAllProvider,
                newDocument,
                newProject,
                CodeFixProvider,
                newScope,
                newCodeActionEquivalenceKey,
                DiagnosticIds,
                DiagnosticProvider,
                CodeActionOptionsProvider);
        }

        #region FixMultiple

        internal static FixAllState Create(
            FixAllProvider fixAllProvider,
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string? codeActionEquivalenceKey,
            CodeActionOptionsProvider codeActionOptionsProvider)
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
                diagnosticProvider,
                codeActionOptionsProvider);
        }

        internal static FixAllState Create(
            FixAllProvider fixAllProvider,
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string? codeActionEquivalenceKey,
            CodeActionOptionsProvider codeActionOptionsProvider)
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
                diagnosticProvider,
                codeActionOptionsProvider);
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
