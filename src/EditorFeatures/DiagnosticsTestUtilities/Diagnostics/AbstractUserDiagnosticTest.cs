// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Testing;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract partial class AbstractUserDiagnosticTest : AbstractCodeActionOrUserDiagnosticTest
    {
        protected AbstractUserDiagnosticTest(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal abstract Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetDiagnosticAndFixesAsync(
            TestWorkspace workspace, TestParameters parameters);

        internal abstract Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            TestWorkspace workspace, TestParameters parameters);

        private protected async Task TestDiagnosticsAsync(
            string initialMarkup, TestParameters? parameters = null, params DiagnosticDescription[] expected)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);

            var diagnostics = await GetDiagnosticsAsync(workspace, ps).ConfigureAwait(false);

            // Special case for single diagnostic reported with annotated span.
            if (expected.Length == 1 && !expected[0].HasLocation)
            {
                var hostDocumentsWithAnnotations = workspace.Documents.Where(d => d.SelectedSpans.Any());
                if (hostDocumentsWithAnnotations.Count() == 1)
                {
                    var expectedSpan = hostDocumentsWithAnnotations.Single().SelectedSpans.Single();

                    Assert.Equal(1, diagnostics.Count());
                    var diagnostic = diagnostics.Single();

                    var actualSpan = diagnostic.Location.SourceSpan;
                    Assert.Equal(expectedSpan, actualSpan);

                    Assert.Equal(expected[0].Code, diagnostic.Id);
                    return;
                }
            }

            DiagnosticExtensions.Verify(diagnostics, expected);
        }

        protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var (_, actions, actionToInvoke) = await GetDiagnosticAndFixesAsync(workspace, parameters);
            return (actions, actionToInvoke);
        }

        protected override async Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var (dxs, _, _) = await GetDiagnosticAndFixesAsync(workspace, parameters);
            return dxs;
        }

        protected static void AddAnalyzerToWorkspace(Workspace workspace, DiagnosticAnalyzer analyzer, TestParameters parameters)
        {
            AnalyzerReference[] analyzeReferences;
            if (analyzer != null)
            {
                Contract.ThrowIfTrue(parameters.testHost == TestHost.OutOfProcess, $"Out-of-proc testing is not supported since {analyzer} can't be serialized.");

                analyzeReferences = new[] { new AnalyzerImageReference(ImmutableArray.Create(analyzer)) };
            }
            else
            {
                // create a serializable analyzer reference:
                analyzeReferences = new[]
                {
                    new AnalyzerFileReference(DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp).GetType().Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                    new AnalyzerFileReference(DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.VisualBasic).GetType().Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)
                };
            }

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(analyzeReferences));
        }

        protected static Document GetDocumentAndSelectSpan(TestWorkspace workspace, out TextSpan span)
        {
            var hostDocument = workspace.Documents.Single(d => d.SelectedSpans.Any());
            span = hostDocument.SelectedSpans.Single();
            return workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        protected static bool TryGetDocumentAndSelectSpan(TestWorkspace workspace, out Document document, out TextSpan span)
        {
            var hostDocument = workspace.Documents.FirstOrDefault(d => d.SelectedSpans.Any());
            if (hostDocument == null)
            {
                // If there wasn't a span, see if there was a $$ caret.  we'll create an empty span
                // there if so.
                hostDocument = workspace.Documents.FirstOrDefault(d => d.CursorPosition != null);
                if (hostDocument == null)
                {
                    document = null;
                    span = default;
                    return false;
                }

                span = new TextSpan(hostDocument.CursorPosition.Value, 0);
                document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                return true;
            }

            span = hostDocument.SelectedSpans.Single();
            document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            return true;
        }

        protected static Document GetDocumentAndAnnotatedSpan(TestWorkspace workspace, out string annotation, out TextSpan span)
        {
            var annotatedDocuments = workspace.Documents.Where(d => d.AnnotatedSpans.Any());
            Debug.Assert(!annotatedDocuments.IsEmpty(), "No annotated span found");
            var hostDocument = annotatedDocuments.Single();
            var annotatedSpan = hostDocument.AnnotatedSpans.Single();
            annotation = annotatedSpan.Key;
            span = annotatedSpan.Value.Single();
            return workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        protected static FixAllScope? GetFixAllScope(string annotation)
        {
            if (annotation == null)
            {
                return null;
            }

            return annotation switch
            {
                "FixAllInDocument" => FixAllScope.Document,
                "FixAllInProject" => FixAllScope.Project,
                "FixAllInSolution" => FixAllScope.Solution,
                "FixAllInSelection" => FixAllScope.Custom,
                _ => throw new InvalidProgramException("Incorrect FixAll annotation in test"),
            };
        }

        internal async Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetDiagnosticAndFixesAsync(
            IEnumerable<Diagnostic> diagnostics,
            CodeFixProvider fixer,
            TestDiagnosticAnalyzerDriver testDriver,
            Document document,
            TextSpan span,
            CodeActionOptions options,
            string annotation,
            int index)
        {
            if (diagnostics.IsEmpty())
            {
                return (ImmutableArray<Diagnostic>.Empty, ImmutableArray<CodeAction>.Empty, null);
            }

            var scope = GetFixAllScope(annotation);

            var intersectingDiagnostics = diagnostics.Where(d => d.Location.SourceSpan.IntersectsWith(span))
                                                     .ToImmutableArray();

            var fixes = new List<CodeFix>();

            foreach (var diagnostic in intersectingDiagnostics)
            {
                var context = new CodeFixContext(
                    document,
                    diagnostic.Location.SourceSpan,
                    ImmutableArray.Create(diagnostic),
                    (a, d) => fixes.Add(new CodeFix(document.Project, a, d)),
                    options,
                    CancellationToken.None);

                await fixer.RegisterCodeFixesAsync(context);
            }

            VerifyCodeActionsRegisteredByProvider(fixer, fixes);

            var actions = MassageActions(fixes.SelectAsArray(f => f.Action));

            if (scope == null)
            {
                // Simple code fix.
                return (intersectingDiagnostics, actions, actions.Length == 0 ? null : actions[index]);
            }

            var equivalenceKey = actions[index].EquivalenceKey;

            // Fix all fix.
            var fixAllProvider = fixer.GetFixAllProvider();
            Assert.NotNull(fixAllProvider);

            var fixAllState = GetFixAllState(
                fixAllProvider, diagnostics, fixer, testDriver, document,
                scope.Value, equivalenceKey, _ => options);
            var fixAllContext = new FixAllContext(fixAllState, new ProgressTracker(), CancellationToken.None);
            var fixAllFix = await fixAllProvider.GetFixAsync(fixAllContext);

            // We have collapsed the fixes down to the single fix-all fix, so we just let our
            // caller know they should pull that entry out of the result.
            return (intersectingDiagnostics, ImmutableArray.Create(fixAllFix), fixAllFix);
        }

        private static FixAllState GetFixAllState(
            FixAllProvider fixAllProvider,
            IEnumerable<Diagnostic> diagnostics,
            CodeFixProvider fixer,
            TestDiagnosticAnalyzerDriver testDriver,
            Document document,
            FixAllScope scope,
            string equivalenceKey,
            CodeActionOptionsProvider optionsProvider)
        {
            Assert.NotEmpty(diagnostics);

            if (scope == FixAllScope.Custom)
            {
                // Bulk fixing diagnostics in selected scope.                    
                var diagnosticsToFix = ImmutableDictionary.CreateRange(SpecializedCollections.SingletonEnumerable(KeyValuePairUtil.Create(document, diagnostics.ToImmutableArray())));
                return FixAllState.Create(fixAllProvider, diagnosticsToFix, fixer, equivalenceKey, optionsProvider);
            }

            var diagnostic = diagnostics.First();
            var diagnosticIds = ImmutableHashSet.Create(diagnostic.Id);
            var fixAllDiagnosticProvider = new FixAllDiagnosticProvider(testDriver, diagnosticIds);

            return diagnostic.Location.IsInSource
                ? new FixAllState(fixAllProvider, document, document.Project, fixer, scope, equivalenceKey, diagnosticIds, fixAllDiagnosticProvider, optionsProvider)
                : new FixAllState(fixAllProvider, document: null, document.Project, fixer, scope, equivalenceKey, diagnosticIds, fixAllDiagnosticProvider, optionsProvider);
        }

        private protected Task TestActionCountInAllFixesAsync(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            OptionsCollection options = null,
            CodeActionOptions? codeActionOptions = null,
            IdeAnalyzerOptions? ideAnalyzerOptions = null,
            object fixProviderData = null)
        {
            return TestActionCountInAllFixesAsync(
                initialMarkup,
                new TestParameters(parseOptions, compilationOptions, options, codeActionOptions, ideAnalyzerOptions, fixProviderData),
                count);
        }

        private async Task TestActionCountInAllFixesAsync(
            string initialMarkup,
            TestParameters parameters,
            int count)
        {
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters);

            var (_, actions, _) = await GetDiagnosticAndFixesAsync(workspace, parameters);
            Assert.Equal(count, actions.Length);
        }

        internal async Task TestSpansAsync(
            string initialMarkup,
            string diagnosticId = null,
            TestParameters? parameters = null)
        {
            MarkupTestFile.GetSpans(initialMarkup, out var unused, out ImmutableArray<TextSpan> spansList);

            var ps = parameters ?? TestParameters.Default;
            var expectedTextSpans = spansList.ToSet();
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);

            ISet<TextSpan> actualTextSpans;
            if (diagnosticId == null)
            {
                var (diagnostics, _, _) = await GetDiagnosticAndFixesAsync(workspace, ps);
                actualTextSpans = diagnostics.Select(d => d.Location.SourceSpan).ToSet();
            }
            else
            {
                var diagnostics = await GetDiagnosticsAsync(workspace, ps);
                actualTextSpans = diagnostics.Where(d => d.Id == diagnosticId).Select(d => d.Location.SourceSpan).ToSet();
            }

            Assert.True(expectedTextSpans.SetEquals(actualTextSpans));
        }
    }
}
