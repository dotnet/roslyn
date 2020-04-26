// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;

namespace AnalyzerRunner
{
    internal class CodeFixEquivalenceGroup
    {
        private CodeFixEquivalenceGroup(
            string equivalenceKey,
            Solution solution,
            FixAllProvider fixAllProvider,
            CodeFixProvider codeFixProvider,
            ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> documentDiagnosticsToFix,
            ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnosticsToFix)
        {
            CodeFixEquivalenceKey = equivalenceKey;
            Solution = solution;
            FixAllProvider = fixAllProvider;
            CodeFixProvider = codeFixProvider;
            DocumentDiagnosticsToFix = documentDiagnosticsToFix;
            ProjectDiagnosticsToFix = projectDiagnosticsToFix;
            NumberOfDiagnostics = documentDiagnosticsToFix.SelectMany(x => x.Value.Select(y => y.Value).SelectMany(y => y)).Count()
                + projectDiagnosticsToFix.SelectMany(x => x.Value).Count();
        }

        internal string CodeFixEquivalenceKey { get; }

        internal Solution Solution { get; }

        internal FixAllProvider FixAllProvider { get; }

        internal CodeFixProvider CodeFixProvider { get; }

        internal ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> DocumentDiagnosticsToFix { get; }

        internal ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> ProjectDiagnosticsToFix { get; }

        internal int NumberOfDiagnostics { get; }

        internal static async Task<ImmutableArray<CodeFixEquivalenceGroup>> CreateAsync(string language, CodeFixProvider codeFixProvider, ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> allDiagnostics, Solution solution, CancellationToken cancellationToken)
        {
            var fixAllProvider = codeFixProvider.GetFixAllProvider();
            if (fixAllProvider == null)
            {
                return ImmutableArray.Create<CodeFixEquivalenceGroup>();
            }

            var relevantDocumentDiagnostics = new Dictionary<ProjectId, Dictionary<string, List<Diagnostic>>>();
            var relevantProjectDiagnostics = new Dictionary<ProjectId, List<Diagnostic>>();

            foreach (var (projectId, diagnostics) in allDiagnostics)
            {
                if (solution.GetProject(projectId).Language != language)
                {
                    continue;
                }

                foreach (var diagnostic in diagnostics)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        continue;
                    }

                    if (diagnostic.Location.IsInSource)
                    {
                        var sourcePath = diagnostic.Location.GetLineSpan().Path;

                        var projectDocumentDiagnostics = relevantDocumentDiagnostics.GetOrAdd(projectId, _ => new Dictionary<string, List<Diagnostic>>());
                        var diagnosticsInFile = projectDocumentDiagnostics.GetOrAdd(sourcePath, _ => new List<Diagnostic>());
                        diagnosticsInFile.Add(diagnostic);
                    }
                    else
                    {
                        var diagnosticsInProject = relevantProjectDiagnostics.GetOrAdd(projectId, _ => new List<Diagnostic>());
                        diagnosticsInProject.Add(diagnostic);
                    }
                }
            }

            var documentDiagnosticsToFix = relevantDocumentDiagnostics.ToImmutableDictionary(
                i => i.Key,
                i => i.Value.ToImmutableDictionary(j => j.Key, j => j.Value.ToImmutableArray(), StringComparer.OrdinalIgnoreCase));
            var projectDiagnosticsToFix = relevantProjectDiagnostics.ToImmutableDictionary(
                i => i.Key,
                i => i.Value.ToImmutableArray());

            var equivalenceKeys = new HashSet<string>();
            foreach (var diagnostic in relevantDocumentDiagnostics.Values.SelectMany(i => i.Values).SelectMany(i => i).Concat(relevantProjectDiagnostics.Values.SelectMany(i => i)))
            {
                foreach (var codeAction in await GetFixesAsync(solution, codeFixProvider, diagnostic, cancellationToken).ConfigureAwait(false))
                {
                    equivalenceKeys.Add(codeAction.EquivalenceKey);
                }
            }

            var groups = new List<CodeFixEquivalenceGroup>();
            foreach (var equivalenceKey in equivalenceKeys)
            {
                groups.Add(new CodeFixEquivalenceGroup(equivalenceKey, solution, fixAllProvider, codeFixProvider, documentDiagnosticsToFix, projectDiagnosticsToFix));
            }

            return groups.ToImmutableArray();
        }

        internal async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            var diagnostic = DocumentDiagnosticsToFix.Values.SelectMany(i => i.Values).Concat(ProjectDiagnosticsToFix.Values).First().First();
            var document = Solution.GetDocument(diagnostic.Location.SourceTree);
            var diagnosticIds = new HashSet<string>(DocumentDiagnosticsToFix.Values.SelectMany(i => i.Values).Concat(ProjectDiagnosticsToFix.Values).SelectMany(i => i.Select(j => j.Id)));

            var diagnosticsProvider = new TesterDiagnosticProvider(DocumentDiagnosticsToFix, ProjectDiagnosticsToFix);

            var context = new FixAllContext(document, CodeFixProvider, FixAllScope.Solution, CodeFixEquivalenceKey, diagnosticIds, diagnosticsProvider, cancellationToken);

            var action = await FixAllProvider.GetFixAsync(context).ConfigureAwait(false);

            return await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<CodeAction>> GetFixesAsync(Solution solution, CodeFixProvider codeFixProvider, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var codeActions = new List<CodeAction>();

            await codeFixProvider
                .RegisterCodeFixesAsync(
                    new CodeFixContext(
                        solution.GetDocument(diagnostic.Location.SourceTree),
                        diagnostic,
                        (a, d) => codeActions.Add(a),
                        cancellationToken))
                .ConfigureAwait(false);

            return codeActions;
        }
    }
}
