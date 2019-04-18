// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;
using Xunit;

namespace Test.Utilities
{
    internal class CodeFixRunner
    {
        private readonly DiagnosticAnalyzer _analyzerOpt;
        private readonly CodeFixProvider _codeFixProvider;
        private readonly TestValidationMode _validationMode;
        private readonly ISet<string> _fixableDiagnosticIds;
        private readonly Func<IEnumerable<Diagnostic>, ImmutableArray<Diagnostic>> _getFixableDiagnostics;

        public CodeFixRunner(
            DiagnosticAnalyzer analyzerOpt,
            CodeFixProvider codeFixProvider,
            TestValidationMode validationMode)
        {
            _analyzerOpt = analyzerOpt;
            _codeFixProvider = codeFixProvider;
            _validationMode = validationMode;
            _fixableDiagnosticIds = new HashSet<string>(_codeFixProvider.FixableDiagnosticIds);
            _getFixableDiagnostics = diags =>
                diags.Where(d => _fixableDiagnosticIds.Contains(d.Id)).ToImmutableArray();
        }

        public Solution ApplySingleFix(Project project, IEnumerable<TestAdditionalDocument> additionalFiles, int codeFixIndex)
        {
            var compilation = project.GetCompilationAsync().Result;
            var analyzerDiagnostics = GetSortedDiagnostics(compilation, additionalFiles: additionalFiles);
            var compilerDiagnostics = compilation.GetDiagnostics();
            var fixableDiagnostics = _getFixableDiagnostics(analyzerDiagnostics.Concat(compilerDiagnostics));
            var diagnostic = fixableDiagnostics[0];
            var document = FindDocument(diagnostic, project);

            List<CodeAction> actions = RegisterCodeFixes(document, diagnostic);
            if (!actions.Any())
            {
                return project.Solution;
            }

            if (codeFixIndex >= actions.Count)
            {
                throw new Exception($"Unable to invoke code fix at index '{codeFixIndex}', only '{actions.Count}' code fixes were registered.");
            }

            return DiagnosticFixerTestsExtensions.Apply(actions.ElementAt(codeFixIndex));
        }

        private List<CodeAction> RegisterCodeFixes(Document document, Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);
            _codeFixProvider.RegisterCodeFixesAsync(context).Wait();
            return actions;
        }

        public Solution ApplyFixesOneByOne(
            Solution solution,
            IEnumerable<TestAdditionalDocument> additionalFiles,
            bool allowNewCompilerDiagnostics,
            int codeFixIndex)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var compilation = project.GetCompilationAsync().Result;
                var analyzerDiagnostics = GetSortedDiagnostics(compilation, additionalFiles);
                var compilerDiagnostics = compilation.GetDiagnostics();
                var fixableDiagnostics = _getFixableDiagnostics(analyzerDiagnostics.Concat(compilerDiagnostics));

                var diagnosticIndexToFix = 0;
                while (diagnosticIndexToFix < fixableDiagnostics.Length)
                {
                    var actions = new List<CodeAction>();
                    var diagnostic = fixableDiagnostics[diagnosticIndexToFix];
                    var document = FindDocument(diagnostic, project);
                    var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);
                    _codeFixProvider.RegisterCodeFixesAsync(context).Wait();

                    if (!actions.Any() || codeFixIndex >= actions.Count)
                    {
                        break;
                    }

                    solution = DiagnosticFixerTestsExtensions.Apply(actions.ElementAt(codeFixIndex));

                    project = solution.GetProject(projectId);
                    additionalFiles = project.AdditionalDocuments.Select(a => new TestAdditionalDocument(a));

                    compilation = project.GetCompilationAsync().Result;
                    analyzerDiagnostics = GetSortedDiagnostics(compilation, additionalFiles);

                    var updatedCompilerDiagnostics = project.GetCompilationAsync().Result.GetDiagnostics();
                    if (!allowNewCompilerDiagnostics)
                    {
                        CheckNewCompilerDiagnostics(project, compilerDiagnostics, updatedCompilerDiagnostics);
                    }

                    var newFixableDiagnostics = _getFixableDiagnostics(analyzerDiagnostics.Concat(updatedCompilerDiagnostics));
                    if (!fixableDiagnostics.Except(newFixableDiagnostics, DiagnosticComparer.Instance).Any()
                        && !newFixableDiagnostics.Except(fixableDiagnostics, DiagnosticComparer.Instance).Any())
                    {
                        diagnosticIndexToFix++;
                    }
                    else
                    {
                        fixableDiagnostics = newFixableDiagnostics;
                        diagnosticIndexToFix = 0;
                    }
                }
            }

            return solution;
        }

        private static Document FindDocument(Diagnostic diagnostic, Project project)
        {
            foreach (var document in project.Documents)
            {
                Assert.NotNull(diagnostic.Location.SourceTree);
                if (diagnostic.Location.SourceTree == document.GetSyntaxTreeAsync().Result)
                {
                    return document;
                }
            }

            throw new ArgumentException($"Could not find diagnostic {diagnostic} in documents provided");
        }

        private static void CheckNewCompilerDiagnostics(Project project, ImmutableArray<Diagnostic> compilerDiagnostics, ImmutableArray<Diagnostic> updatedCompilerDiagnostics)
        {
            var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, updatedCompilerDiagnostics);
            if (newCompilerDiagnostics.Any())
            {
                // Format and get the compiler diagnostics again so that the locations make sense in the output
                var projectId = project.Id;
                var solution = project.Solution;
                foreach (var documentId in project.DocumentIds)
                {
                    solution = solution.WithDocumentSyntaxRoot(documentId, Formatter.Format(solution.GetDocument(documentId).GetSyntaxRootAsync().Result, Formatter.Annotation, solution.Workspace));
                }

                project = solution.GetProject(projectId);

                newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, project.GetCompilationAsync().Result.GetDiagnostics());

                Assert.True(false,
                    string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew documents:\r\n{1}\r\n",
                        string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                        string.Join("\r\n", project.Documents.Select(doc => doc.GetSyntaxRootAsync().Result.ToFullString()))));
            }
        }

        private static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
        {
            Diagnostic[] oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            Diagnostic[] newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

            int oldIndex = 0;
            int newIndex = 0;

            while (newIndex < newArray.Length)
            {
                if (oldIndex < oldArray.Length && oldArray[oldIndex].Id == newArray[newIndex].Id)
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newArray[newIndex++];
                }
            }
        }

        protected Diagnostic[] GetSortedDiagnostics(Compilation compilation, IEnumerable<TestAdditionalDocument> additionalFiles = null)
        {
            if (_analyzerOpt == null)
            {
                return Array.Empty<Diagnostic>();
            }

            var analyzerOptions = additionalFiles != null ? new AnalyzerOptions(additionalFiles.ToImmutableArray<AdditionalText>()) : null;
            compilation = EnableAnalyzer(_analyzerOpt, compilation);

            return compilation.GetAnalyzerDiagnostics(new[] { _analyzerOpt }, _validationMode, analyzerOptions).OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        private static Compilation EnableAnalyzer(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            return compilation.WithOptions(
                compilation
                    .Options
                    .WithSpecificDiagnosticOptions(
                        analyzer
                            .SupportedDiagnostics
                            .Select(x => new KeyValuePair<string, ReportDiagnostic>(x.Id, ReportDiagnostic.Default))
                            .ToImmutableDictionary()));
        }

        private sealed class DiagnosticComparer : IEqualityComparer<Diagnostic>
        {
            internal static readonly DiagnosticComparer Instance = new DiagnosticComparer();

            public bool Equals(Diagnostic x, Diagnostic y)
            {
                return x.Id == y.Id &&
                    x.GetMessage() == y.GetMessage() &&
                    x.Location.IsInSource == y.Location.IsInSource &&
                    x.Location.SourceSpan == y.Location.SourceSpan &&
                    (x.Location.SourceTree?.IsEquivalentTo(y.Location.SourceTree)).GetValueOrDefault();
            }

            public int GetHashCode(Diagnostic obj)
            {
                var hash = 5;

                unchecked
                {
                    hash = (7 * hash) ^ obj.Id.GetHashCode();
                    hash = (7 * hash) ^ obj.GetMessage().GetHashCode();
                    hash = (7 * hash) ^ (obj.Location.IsInSource ? 1 : 0);
                    hash = (7 * hash) ^ obj.Location.SourceSpan.GetHashCode();
                    hash = (7 * hash) ^ (obj.Location.SourceTree?.ToString().GetHashCode() ?? 0);
                }

                return hash;
            }
        }
    }
}
