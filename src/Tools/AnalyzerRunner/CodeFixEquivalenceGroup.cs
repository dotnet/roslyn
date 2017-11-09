// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AnalyzerRunner
{
    class CodeFixEquivalenceGroup
    {
        private CodeFixEquivalenceGroup(
        string equivalenceKey,
        Solution solution,
        FixAllProvider fixAllProvider,
        CodeFixProvider codeFixProvider,
        ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> documentDiagnosticsToFix,
        ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnosticsToFix)
        {
            this.CodeFixEquivalenceKey = equivalenceKey;
            this.Solution = solution;
            this.FixAllProvider = fixAllProvider;
            this.CodeFixProvider = codeFixProvider;
            this.DocumentDiagnosticsToFix = documentDiagnosticsToFix;
            this.ProjectDiagnosticsToFix = projectDiagnosticsToFix;
            this.NumberOfDiagnostics = documentDiagnosticsToFix.SelectMany(x => x.Value.Select(y => y.Value).SelectMany(y => y)).Count()
                + projectDiagnosticsToFix.SelectMany(x => x.Value).Count();
        }

        internal string CodeFixEquivalenceKey { get; }

        internal Solution Solution { get; }

        internal FixAllProvider FixAllProvider { get; }

        internal CodeFixProvider CodeFixProvider { get; }

        internal ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> DocumentDiagnosticsToFix { get; }

        internal ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> ProjectDiagnosticsToFix { get; }

        internal int NumberOfDiagnostics { get; }

        internal static async Task<ImmutableArray<CodeFixEquivalenceGroup>> CreateAsync(CodeFixProvider codeFixProvider, ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> allDiagnostics, Solution solution, CancellationToken cancellationToken)
        {
            var fixAllProvider = codeFixProvider.GetFixAllProvider();
            if (fixAllProvider == null)
            {
                return ImmutableArray.Create<CodeFixEquivalenceGroup>();
            }

            Dictionary<ProjectId, Dictionary<string, List<Diagnostic>>> relevantDocumentDiagnostics =
                new Dictionary<ProjectId, Dictionary<string, List<Diagnostic>>>();
            Dictionary<ProjectId, List<Diagnostic>> relevantProjectDiagnostics =
                new Dictionary<ProjectId, List<Diagnostic>>();

            foreach (var projectDiagnostics in allDiagnostics)
            {
                foreach (var diagnostic in projectDiagnostics.Value)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        continue;
                    }

                    if (diagnostic.Location.IsInSource)
                    {
                        string sourcePath = diagnostic.Location.GetLineSpan().Path;

                        Dictionary<string, List<Diagnostic>> projectDocumentDiagnostics;
                        if (!relevantDocumentDiagnostics.TryGetValue(projectDiagnostics.Key, out projectDocumentDiagnostics))
                        {
                            projectDocumentDiagnostics = new Dictionary<string, List<Diagnostic>>();
                            relevantDocumentDiagnostics.Add(projectDiagnostics.Key, projectDocumentDiagnostics);
                        }

                        List<Diagnostic> diagnosticsInFile;
                        if (!projectDocumentDiagnostics.TryGetValue(sourcePath, out diagnosticsInFile))
                        {
                            diagnosticsInFile = new List<Diagnostic>();
                            projectDocumentDiagnostics.Add(sourcePath, diagnosticsInFile);
                        }

                        diagnosticsInFile.Add(diagnostic);
                    }
                    else
                    {
                        List<Diagnostic> diagnosticsInProject;
                        if (!relevantProjectDiagnostics.TryGetValue(projectDiagnostics.Key, out diagnosticsInProject))
                        {
                            diagnosticsInProject = new List<Diagnostic>();
                            relevantProjectDiagnostics.Add(projectDiagnostics.Key, diagnosticsInProject);
                        }

                        diagnosticsInProject.Add(diagnostic);
                    }
                }
            }

            ImmutableDictionary<ProjectId, ImmutableDictionary<string, ImmutableArray<Diagnostic>>> documentDiagnosticsToFix =
                relevantDocumentDiagnostics.ToImmutableDictionary(i => i.Key, i => i.Value.ToImmutableDictionary(j => j.Key, j => j.Value.ToImmutableArray(), StringComparer.OrdinalIgnoreCase));
            ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> projectDiagnosticsToFix =
                relevantProjectDiagnostics.ToImmutableDictionary(i => i.Key, i => i.Value.ToImmutableArray());

            HashSet<string> equivalenceKeys = new HashSet<string>();
            foreach (var diagnostic in relevantDocumentDiagnostics.Values.Where(i => i.Values != null && i.Values.Any()).SelectMany(i => i.Values).SelectMany(i => i).Concat(relevantProjectDiagnostics.Values.SelectMany(i => i)))
            {
                foreach (var codeAction in await GetFixesAsync(solution, codeFixProvider, diagnostic, cancellationToken).ConfigureAwait(false))
                {
                    equivalenceKeys.Add(codeAction.EquivalenceKey);
                }
            }

            List<CodeFixEquivalenceGroup> groups = new List<CodeFixEquivalenceGroup>();
            foreach (var equivalenceKey in equivalenceKeys)
            {
                groups.Add(new CodeFixEquivalenceGroup(equivalenceKey, solution, fixAllProvider, codeFixProvider, documentDiagnosticsToFix, projectDiagnosticsToFix));
            }

            return groups.ToImmutableArray();
        }

        internal async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken)
        {
            Diagnostic diagnostic = this.DocumentDiagnosticsToFix.Values.SelectMany(i => i.Values).Concat(this.ProjectDiagnosticsToFix.Values).First().First();
            Document document = this.Solution.GetDocument(diagnostic.Location.SourceTree);
            HashSet<string> diagnosticIds = new HashSet<string>(this.DocumentDiagnosticsToFix.Values.SelectMany(i => i.Values).Concat(this.ProjectDiagnosticsToFix.Values).SelectMany(i => i.Select(j => j.Id)));

            var diagnosticsProvider = new TesterDiagnosticProvider(this.DocumentDiagnosticsToFix, this.ProjectDiagnosticsToFix);

            var context = new FixAllContext(document, this.CodeFixProvider, FixAllScope.Solution, this.CodeFixEquivalenceKey, diagnosticIds, diagnosticsProvider, cancellationToken);

            CodeAction action = await this.FixAllProvider.GetFixAsync(context).ConfigureAwait(false);

            return await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<CodeAction>> GetFixesAsync(Solution solution, CodeFixProvider codeFixProvider, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            List<CodeAction> codeActions = new List<CodeAction>();
            try
            {
                await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(solution.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => codeActions.Add(a), cancellationToken)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Utilities.WriteLine($"Failed to GetFixesAsync for {diagnostic.GetMessage()} due to {e.ToString()}", ConsoleColor.Red);
            }

            return codeActions;
        }
    }
}