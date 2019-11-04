// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeFixTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        /// <summary>
        /// Gets or sets the index of the code fix to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeFixIndex"/> and <see cref="CodeFixEquivalenceKey"/> are both specified, the code fix
        /// test will further verify that the two properties refer to the same code fix.</para>
        /// </remarks>
        /// <seealso cref="CodeFixEquivalenceKey"/>
        public int? CodeFixIndex { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CodeAction.EquivalenceKey"/> of the code fix to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeFixIndex"/> and <see cref="CodeFixEquivalenceKey"/> are both specified, the code fix
        /// test will further verify that the two properties refer to the same code fix.</para>
        /// </remarks>
        /// <seealso cref="CodeFixIndex"/>
        public string? CodeFixEquivalenceKey { get; set; }

        /// <summary>
        /// Sets the expected output source file for code fix testing.
        /// </summary>
        /// <seealso cref="FixedState"/>
        public string FixedCode
        {
            set
            {
                if (value != null)
                {
                    FixedState.Sources.Add(value);
                }
            }
        }

        public SolutionState FixedState { get; }

        /// <summary>
        /// Sets the expected output source file after a Fix All operation is applied.
        /// </summary>
        /// <seealso cref="BatchFixedState"/>
        public string BatchFixedCode
        {
            set
            {
                if (value != null)
                {
                    BatchFixedState.Sources.Add(value);
                }
            }
        }

        public SolutionState BatchFixedState { get; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing.
        /// </summary>
        /// <remarks>
        /// <para>Code fixes are applied until one of the following conditions are met:</para>
        ///
        /// <list type="bullet">
        /// <item><description>No diagnostics are reported in the input.</description></item>
        /// <item><description>No code fixes are provided for the diagnostics reported in the input.</description></item>
        /// <item><description>The code fix applied for the diagnostics does not produce a change in the source file(s).</description></item>
        /// <item><description>The maximum number of allowed iterations is exceeded.</description></item>
        /// </list>
        ///
        /// <para>If the number of iterations is positive, it represents an exact number of iterations: code fix tests
        /// will fail if the code fix required more or fewer iterations to complete. If the number of iterations is
        /// negative, the negation of the number of iterations is treated as an upper bound on the number of allowed
        /// iterations: code fix tests will fail only if the code fix required more iterations to complete. If the
        /// number of iterations is zero, the code fix test will validate that no code fixes are offered for the set of
        /// diagnostics reported in the original input.</para>
        ///
        /// <para>When the number of iterations is not specified, the value is automatically selected according to the
        /// current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If the expected code fix output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as the negative of the number of fixable diagnostics appearing in the input source file(s).</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Iterative code fix operations are expected
        /// to complete in at most one operation for each fixable diagnostic in the input source has been applied.
        /// Completing in fewer iterations is acceptable."</para>
        /// </note>
        /// </remarks>
        public int? NumberOfIncrementalIterations { get; set; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing for Fix All scenarios.
        /// </summary>
        /// <remarks>
        /// <para>See the <see cref="NumberOfIncrementalIterations"/> property for an overview of the behavior of this
        /// property. If the number of Fix All iterations is not specified, the value is automatically selected
        /// according to the current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If the expected Fix All output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as <c>1</c>.</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Fix All operations are expected to complete
        /// in the minimum number of iterations possible unless otherwise specified."</para>
        /// </note>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        public int? NumberOfFixAllIterations { get; set; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing for Fix All in Document
        /// scenarios.
        /// </summary>
        /// <remarks>
        /// <para>See the <see cref="NumberOfIncrementalIterations"/> property for an overview of the behavior of this
        /// property. If the number of Fix All in Document iterations is not specified, the value from
        /// <see cref="NumberOfFixAllIterations"/> is used.</para>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        /// <seealso cref="NumberOfFixAllIterations"/>
        /// <seealso href="https://github.com/dotnet/roslyn-sdk/issues/147">#147: Figure out Fix All iteration counts by context</seealso>
        public int? NumberOfFixAllInDocumentIterations { get; set; }

        /// <summary>
        /// Gets or sets the validation mode for code fixes. The default is
        /// <see cref="CodeFixValidationMode.SemanticStructure"/>.
        /// </summary>
        public CodeFixValidationMode CodeFixValidationMode { get; set; } = CodeFixValidationMode.SemanticStructure;

        /// <summary>
        /// Gets the syntax kind enumeration type for the current code fix test.
        /// </summary>
        public abstract Type SyntaxKindType { get; }

        protected CodeFixTest()
        {
            FixedState = new SolutionState(DefaultFilePathPrefix, DefaultFileExt);
            BatchFixedState = new SolutionState(DefaultFilePathPrefix, DefaultFileExt);
        }

        /// <summary>
        /// Returns the code fixes being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeFixProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeFixProvider> GetCodeFixProviders();

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty($"{nameof(TestState)}.{nameof(SolutionState.Sources)}", TestState.Sources);

            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = GetCodeFixProviders().SelectMany(provider => provider.FixableDiagnosticIds).ToImmutableArray();

            var rawTestState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics);
            var rawFixedState = FixedState.WithInheritedValuesApplied(rawTestState, fixableDiagnostics);
            var rawBatchFixedState = BatchFixedState.WithInheritedValuesApplied(rawFixedState, fixableDiagnostics);

            var testState = rawTestState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);
            var fixedState = rawFixedState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);
            var batchFixedState = rawBatchFixedState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            await VerifyDiagnosticsAsync(testState.Sources.ToArray(), testState.AdditionalFiles.ToArray(), testState.AdditionalProjects.ToArray(), testState.AdditionalReferences.ToArray(), testState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of test state"), cancellationToken).ConfigureAwait(false);

            if (CodeFixExpected())
            {
                await VerifyDiagnosticsAsync(fixedState.Sources.ToArray(), fixedState.AdditionalFiles.ToArray(), fixedState.AdditionalProjects.ToArray(), fixedState.AdditionalReferences.ToArray(), fixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of fixed state"), cancellationToken).ConfigureAwait(false);
                if (CodeFixExpected(BatchFixedState))
                {
                    await VerifyDiagnosticsAsync(batchFixedState.Sources.ToArray(), batchFixedState.AdditionalFiles.ToArray(), batchFixedState.AdditionalProjects.ToArray(), batchFixedState.AdditionalReferences.ToArray(), batchFixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of batch fixed state"), cancellationToken).ConfigureAwait(false);
                }

                await VerifyFixAsync(testState, fixedState, batchFixedState, Verify, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool CodeFixExpected()
        {
            return CodeFixExpected(FixedState)
                || CodeFixExpected(BatchFixedState);
        }

        private static bool CodeFixExpected(SolutionState state)
        {
            return state.InheritanceMode != null
                || state.MarkupHandling != null
                || state.Sources.Any()
                || state.AdditionalFiles.Any()
                || state.AdditionalFilesFactories.Any();
        }

        /// <summary>
        /// Called to test a C# code fix when applied on the input source as a string.
        /// </summary>
        /// <param name="testState">The effective input test state.</param>
        /// <param name="fixedState">The effective test state after incremental code fixes are applied.</param>
        /// <param name="batchFixedState">The effective test state after batch code fixes are applied.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyFixAsync(SolutionState testState, SolutionState fixedState, SolutionState batchFixedState, IVerifier verifier, CancellationToken cancellationToken)
        {
            int numberOfIncrementalIterations;
            int numberOfFixAllIterations;
            int numberOfFixAllInDocumentIterations;
            if (NumberOfIncrementalIterations != null)
            {
                numberOfIncrementalIterations = NumberOfIncrementalIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, fixedState))
                {
                    numberOfIncrementalIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per fixable diagnostic
                    var fixers = GetCodeFixProviders().ToArray();
                    var fixableExpectedDiagnostics = testState.ExpectedDiagnostics.Count(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
                    numberOfIncrementalIterations = -fixableExpectedDiagnostics;
                }
            }

            if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, batchFixedState))
                {
                    numberOfFixAllIterations = 0;
                }
                else
                {
                    numberOfFixAllIterations = 1;
                }
            }

            if (NumberOfFixAllInDocumentIterations != null)
            {
                numberOfFixAllInDocumentIterations = NumberOfFixAllInDocumentIterations.Value;
            }
            else
            {
                numberOfFixAllInDocumentIterations = numberOfFixAllIterations;
            }

            var t1 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, fixedState, numberOfIncrementalIterations, FixEachAnalyzerDiagnosticAsync, verifier.PushContext("Iterative code fix application"), cancellationToken).ConfigureAwait(false);

            var fixAllProvider = GetCodeFixProviders().Select(codeFixProvider => codeFixProvider.GetFixAllProvider()).Where(codeFixProvider => codeFixProvider != null).ToImmutableArray();

            if (fixAllProvider.IsEmpty)
            {
                await t1;
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    await t1;
                }

                var t2 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllInDocumentIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, verifier.PushContext("Fix all in document"), cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t2;
                }

                var t3 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInProjectAsync, verifier.PushContext("Fix all in project"), cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t3;
                }

                var t4 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInSolutionAsync, verifier.PushContext("Fix all in solution"), cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t4;
                }

                if (!Debugger.IsAttached)
                {
                    // Allow the operations to run in parallel
                    await t1;
                    await t2;
                    await t3;
                    await t4;
                }
            }
        }

        private async Task VerifyFixAsync(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<CodeFixProvider> codeFixProviders,
            SolutionState oldState,
            SolutionState newState,
            int numberOfIterations,
            Func<ImmutableArray<DiagnosticAnalyzer>, ImmutableArray<CodeFixProvider>, int?, string?, Project, int, IVerifier, CancellationToken, Task<Project>> getFixedProject,
            IVerifier verifier,
            CancellationToken cancellationToken)
        {
            var project = await CreateProjectAsync(oldState.Sources.ToArray(), oldState.AdditionalFiles.ToArray(), oldState.AdditionalProjects.ToArray(), oldState.AdditionalReferences.ToArray(), language, cancellationToken);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            project = await getFixedProject(analyzers, codeFixProviders, CodeFixIndex, CodeFixEquivalenceKey, project, numberOfIterations, verifier, cancellationToken).ConfigureAwait(false);

            // After applying all of the code fixes, compare the resulting string to the inputted one
            var updatedDocuments = project.Documents.ToArray();

            verifier.Equal(newState.Sources.Count, updatedDocuments.Length, $"expected '{nameof(newState)}.{nameof(SolutionState.Sources)}' and '{nameof(updatedDocuments)}' to be equal but '{nameof(newState)}.{nameof(SolutionState.Sources)}' contains '{newState.Sources.Count}' documents and '{nameof(updatedDocuments)}' contains '{updatedDocuments.Length}' documents");

            for (var i = 0; i < updatedDocuments.Length; i++)
            {
                var actual = await GetSourceTextFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                verifier.EqualOrDiff(newState.Sources[i].content.ToString(), actual.ToString(), $"content of '{newState.Sources[i].filename}' did not match. Diff shown with expected as baseline:");
                verifier.Equal(newState.Sources[i].content.Encoding, actual.Encoding, $"encoding of '{newState.Sources[i].filename}' was expected to be '{newState.Sources[i].content.Encoding}' but was '{actual.Encoding}'");
                verifier.Equal(newState.Sources[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newState.Sources[i].filename}' was expected to be '{newState.Sources[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                verifier.Equal(newState.Sources[i].filename, updatedDocuments[i].Name, $"file name was expected to be '{newState.Sources[i].filename}' but was '{updatedDocuments[i].Name}'");
            }

            var updatedAdditionalDocuments = project.AdditionalDocuments.ToArray();

            verifier.Equal(newState.AdditionalFiles.Count, updatedAdditionalDocuments.Length, $"expected '{nameof(newState)}.{nameof(SolutionState.AdditionalFiles)}' and '{nameof(updatedAdditionalDocuments)}' to be equal but '{nameof(newState)}.{nameof(SolutionState.AdditionalFiles)}' contains '{newState.AdditionalFiles.Count}' documents and '{nameof(updatedAdditionalDocuments)}' contains '{updatedAdditionalDocuments.Length}' documents");

            for (var i = 0; i < updatedAdditionalDocuments.Length; i++)
            {
                var actual = await updatedAdditionalDocuments[i].GetTextAsync(cancellationToken).ConfigureAwait(false);
                verifier.EqualOrDiff(newState.AdditionalFiles[i].content.ToString(), actual.ToString(), $"content of '{newState.AdditionalFiles[i].filename}' did not match. Diff shown with expected as baseline:");
                verifier.Equal(newState.AdditionalFiles[i].content.Encoding, actual.Encoding, $"encoding of '{newState.AdditionalFiles[i].filename}' was expected to be '{newState.AdditionalFiles[i].content.Encoding}' but was '{actual.Encoding}'");
                verifier.Equal(newState.AdditionalFiles[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newState.AdditionalFiles[i].filename}' was expected to be '{newState.AdditionalFiles[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                verifier.Equal(newState.AdditionalFiles[i].filename, updatedAdditionalDocuments[i].Name, $"file name was expected to be '{newState.AdditionalFiles[i].filename}' but was '{updatedAdditionalDocuments[i].Name}'");
            }
        }

        private static bool HasAnyChange(SolutionState oldState, SolutionState newState)
        {
            return !oldState.Sources.SequenceEqual(newState.Sources, SourceFileEqualityComparer.Instance)
                || !oldState.AdditionalFiles.SequenceEqual(newState.AdditionalFiles, SourceFileEqualityComparer.Instance);
        }

        private async Task<Project> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, CompilerDiagnostics, cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                verifier.True(--numberOfIterations >= -1, "The upper limit for the number of code fix iterations was exceeded");

                previousDiagnostics = analyzerDiagnostics;

                done = true;
                var anyActions = false;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    var actions = new List<CodeAction>();

                    foreach (var codeFixProvider in codeFixProviders)
                    {
                        if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                        {
                            // do not pass unsupported diagnostics to a code fix provider
                            continue;
                        }

                        var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add(a), cancellationToken);
                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                    }

                    var actionToApply = TryGetCodeActionToApply(actions, codeFixIndex, codeFixEquivalenceKey, verifier);
                    if (actionToApply != null)
                    {
                        anyActions = true;

                        var fixedProject = await ApplyFixAsync(project, actionToApply, cancellationToken).ConfigureAwait(false);
                        if (fixedProject != project)
                        {
                            done = false;

                            project = await RecreateProjectDocumentsAsync(fixedProject, verifier, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                }

                if (!anyActions)
                {
                    verifier.True(done, "Expected to be done executing actions.");

                    // Avoid counting iterations that do not provide any code actions
                    numberOfIterations++;
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                verifier.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
            }
            else
            {
                verifier.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
            }

            return project;
        }

        private static CodeAction? TryGetCodeActionToApply(List<CodeAction> actions, int? codeFixIndex, string? codeFixEquivalenceKey, IVerifier verifier)
        {
            if (codeFixIndex.HasValue && codeFixEquivalenceKey != null)
            {
                if (actions.Count <= codeFixIndex)
                {
                    return null;
                }

                verifier.Equal(
                    codeFixEquivalenceKey,
                    actions[codeFixIndex.Value].EquivalenceKey,
                    "The code action equivalence key and index must be consistent when both are specified.");

                return actions[codeFixIndex.Value];
            }
            else if (codeFixEquivalenceKey != null)
            {
                return actions.Find(x => x.EquivalenceKey == codeFixEquivalenceKey);
            }
            else if (actions.Count > (codeFixIndex ?? 0))
            {
                return actions[codeFixIndex ?? 0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Implements a workaround for issue #936, force re-parsing to get the same sort of syntax tree as the original document.
        /// </summary>
        /// <param name="project">The project to update.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="Project"/>.</returns>
        private async Task<Project> RecreateProjectDocumentsAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                var initialTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                document = await RecreateDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                var recreatedTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (CodeFixValidationMode != CodeFixValidationMode.None)
                {
                    try
                    {
                        // We expect the tree produced by the code fix (initialTree) to match the form of the tree produced
                        // by the compiler for the same text (recreatedTree).
                        TreeEqualityVisitor.AssertNodesEqual(
                            verifier,
                            SyntaxKindType,
                            await recreatedTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                            await initialTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                            checkTrivia: CodeFixValidationMode == CodeFixValidationMode.Full);
                    }
                    catch
                    {
                        // Try to revalidate the tree with a better message
                        var renderedInitialTree = TreeToString(await initialTree.GetRootAsync(cancellationToken).ConfigureAwait(false), CodeFixValidationMode);
                        var renderedRecreatedTree = TreeToString(await recreatedTree.GetRootAsync(cancellationToken).ConfigureAwait(false), CodeFixValidationMode);
                        verifier.EqualOrDiff(renderedRecreatedTree, renderedInitialTree);

                        // This is not expected to be hit, but it will be hit if the validation failure occurred in a
                        // portion of the tree not captured by the rendered form from TreeToString.
                        throw;
                    }
                }

                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
        }

        /// <summary>
        /// Apply the inputted <see cref="CodeAction"/> to the inputted document.
        /// Meant to be used to apply code fixes.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to apply the fix on.</param>
        /// <param name="codeAction">A <see cref="CodeAction"/> that will be applied to the
        /// <paramref name="project"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> with the changes from the <see cref="CodeAction"/>.</returns>
        private static async Task<Project> ApplyFixAsync(Project project, CodeAction codeAction, CancellationToken cancellationToken)
        {
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetProject(project.Id);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInDocumentAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Document, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, verifier, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInProjectAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Project, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, verifier, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInSolutionAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Solution, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, verifier, cancellationToken);
        }

        private async Task<Project> FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope scope, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, CompilerDiagnostics, cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                verifier.False(--numberOfIterations < -1, "The upper limit for the number of fix all iterations was exceeded");

                Diagnostic? firstDiagnostic = null;
                CodeFixProvider? effectiveCodeFixProvider = null;
                string? equivalenceKey = null;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    var actions = new List<(CodeAction, CodeFixProvider)>();

                    foreach (var codeFixProvider in codeFixProviders)
                    {
                        if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                        {
                            // do not pass unsupported diagnostics to a code fix provider
                            continue;
                        }

                        var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add((a, codeFixProvider)), cancellationToken);
                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                    }

                    var actionToApply = TryGetCodeActionToApply(actions.Select(a => a.Item1).ToList(), codeFixIndex, codeFixEquivalenceKey, verifier);
                    if (actionToApply != null)
                    {
                        firstDiagnostic = diagnostic;
                        effectiveCodeFixProvider = actions.SingleOrDefault(a => a.Item1 == actionToApply).Item2;
                        equivalenceKey = actionToApply.EquivalenceKey;
                        break;
                    }
                }

                var fixAllProvider = effectiveCodeFixProvider?.GetFixAllProvider();
                if (firstDiagnostic == null || fixAllProvider == null)
                {
                    numberOfIterations++;
                    break;
                }

                previousDiagnostics = analyzerDiagnostics;

                done = true;

                FixAllContext.DiagnosticProvider fixAllDiagnosticProvider = TestDiagnosticProvider.Create(analyzerDiagnostics);

                var analyzerDiagnosticIds = analyzers.SelectMany(x => x.SupportedDiagnostics).Select(x => x.Id);
                var compilerDiagnosticIds = codeFixProviders.SelectMany(codeFixProvider => codeFixProvider.FixableDiagnosticIds).Where(x => x.StartsWith("CS", StringComparison.Ordinal) || x.StartsWith("BC", StringComparison.Ordinal));
                var disabledDiagnosticIds = project.CompilationOptions.SpecificDiagnosticOptions.Where(x => x.Value == ReportDiagnostic.Suppress).Select(x => x.Key);
                var relevantIds = analyzerDiagnosticIds.Concat(compilerDiagnosticIds).Except(disabledDiagnosticIds).Distinct();
                var fixAllContext = new FixAllContext(project.GetDocument(firstDiagnostic.Location.SourceTree), effectiveCodeFixProvider, scope, equivalenceKey, relevantIds, fixAllDiagnosticProvider, cancellationToken);

                var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                if (action == null)
                {
                    return project;
                }

                var fixedProject = await ApplyFixAsync(project, action, cancellationToken).ConfigureAwait(false);
                if (fixedProject != project)
                {
                    done = false;

                    project = await RecreateProjectDocumentsAsync(fixedProject, verifier, cancellationToken).ConfigureAwait(false);
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                verifier.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
            }
            else
            {
                verifier.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
            }

            return project;
        }

        /// <summary>
        /// Get the existing compiler diagnostics on the input document.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to run the compiler diagnostic analyzers on.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The compiler diagnostics that were found in the code.</returns>
        private static async Task<ImmutableArray<Diagnostic>> GetCompilerDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            var allDiagnostics = ImmutableArray.Create<Diagnostic>();

            foreach (var document in project.Documents)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                allDiagnostics = allDiagnostics.AddRange(semanticModel.GetDiagnostics(cancellationToken: cancellationToken));
            }

            return allDiagnostics;
        }

        /// <summary>
        /// Given a document, turn it into a string based on the syntax root.
        /// </summary>
        /// <param name="document">The <see cref="Document"/> to be converted to a string.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="SourceText"/> containing the syntax of the <see cref="Document"/> after formatting.</returns>
        private static async Task<SourceText> GetSourceTextFromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var formatted = await Formatter.FormatAsync(simplifiedDoc, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await formatted.GetTextAsync(cancellationToken).ConfigureAwait(false);
        }

        private static bool AreDiagnosticsDifferent(ImmutableArray<Diagnostic> analyzerDiagnostics, ImmutableArray<Diagnostic> previousDiagnostics)
        {
            if (analyzerDiagnostics.Length != previousDiagnostics.Length)
            {
                return true;
            }

            for (var i = 0; i < analyzerDiagnostics.Length; i++)
            {
                if ((analyzerDiagnostics[i].Id != previousDiagnostics[i].Id)
                    || (analyzerDiagnostics[i].Location.SourceSpan != previousDiagnostics[i].Location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        private string TreeToString(SyntaxNodeOrToken syntaxNodeOrToken, CodeFixValidationMode validationMode)
        {
            var result = new StringBuilder();
            TreeToString(syntaxNodeOrToken, string.Empty, validationMode, result);
            return result.ToString();
        }

        private void TreeToString(SyntaxNodeOrToken syntaxNodeOrToken, string indent, CodeFixValidationMode validationMode, StringBuilder result)
        {
            if (syntaxNodeOrToken.IsNode)
            {
                result.AppendLine($"{indent}Node({Kind(syntaxNodeOrToken.RawKind)}):");

                var childIndent = indent + "  ";
                foreach (var child in syntaxNodeOrToken.ChildNodesAndTokens())
                {
                    TreeToString(child, childIndent, validationMode, result);
                }
            }
            else
            {
                var syntaxToken = syntaxNodeOrToken.AsToken();
                result.AppendLine($"{indent}Token({Kind(syntaxToken.RawKind)}): {Escape(syntaxToken.Text)}");

                if (validationMode == CodeFixValidationMode.Full)
                {
                    var childIndent = indent + "  ";
                    foreach (var trivia in syntaxToken.LeadingTrivia)
                    {
                        if (trivia.HasStructure)
                        {
                            result.AppendLine($"{childIndent}Leading({Kind(trivia.RawKind)}):");
                            TreeToString(trivia.GetStructure(), childIndent + "  ", validationMode, result);
                        }
                        else
                        {
                            result.AppendLine($"{childIndent}Leading({Kind(trivia.RawKind)}): {Escape(trivia.ToString())}");
                        }
                    }

                    foreach (var trivia in syntaxToken.TrailingTrivia)
                    {
                        if (trivia.HasStructure)
                        {
                            result.AppendLine($"{childIndent}Trailing({Kind(trivia.RawKind)}):");
                            TreeToString(trivia.GetStructure(), childIndent + "  ", validationMode, result);
                        }
                        else
                        {
                            result.AppendLine($"{childIndent}Trailing({Kind(trivia.RawKind)}): {Escape(trivia.ToString())}");
                        }
                    }
                }
            }

            // Local functions
            string Escape(string text)
            {
                return text
                    .Replace("\\", "\\\\")
                    .Replace("\t", "\\t")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");
            }

            string Kind(int syntaxKind)
            {
                if (SyntaxKindType.GetTypeInfo()?.IsEnum ?? false)
                {
                    return Enum.Format(SyntaxKindType, (ushort)syntaxKind, "G");
                }
                else
                {
                    return syntaxKind.ToString();
                }
            }
        }

        private sealed class SourceFileEqualityComparer : IEqualityComparer<(string filename, SourceText content)>
        {
            private SourceFileEqualityComparer()
            {
            }

            public static SourceFileEqualityComparer Instance { get; } = new SourceFileEqualityComparer();

            public bool Equals((string filename, SourceText content) x, (string filename, SourceText content) y)
            {
                if (x.filename != y.filename)
                {
                    return false;
                }

                if (x.content is null || y.content is null)
                {
                    return ReferenceEquals(x, y);
                }

                return x.content.Encoding == y.content.Encoding
                    && x.content.ChecksumAlgorithm == y.content.ChecksumAlgorithm
                    && x.content.ContentEquals(y.content);
            }

            public int GetHashCode((string filename, SourceText content) obj)
            {
                return obj.filename.GetHashCode()
                    ^ (obj.content?.ToString().GetHashCode() ?? 0);
            }
        }

        private class TreeEqualityVisitor
        {
            private readonly IVerifier _verifier;
            private readonly Type _syntaxKindType;
            private readonly SyntaxNode _expected;
            private readonly bool _checkTrivia;

            private TreeEqualityVisitor(IVerifier verifier, Type syntaxKindType, SyntaxNode expected, bool checkTrivia)
            {
                _verifier = verifier;
                _syntaxKindType = syntaxKindType;
                _expected = expected ?? throw new ArgumentNullException(nameof(expected));
                _checkTrivia = checkTrivia;
            }

            public void Visit(SyntaxNode node)
            {
                AssertSyntaxKindEqual(_expected.RawKind, node.RawKind);
                AssertChildSyntaxListEqual(_expected.ChildNodesAndTokens(), node.ChildNodesAndTokens(), _checkTrivia);
            }

            internal static void AssertNodesEqual(IVerifier verifier, Type syntaxKindType, SyntaxNode expected, SyntaxNode actual, bool checkTrivia)
            {
                new TreeEqualityVisitor(verifier, syntaxKindType, expected, checkTrivia).Visit(actual);
            }

            private void AssertNodesEqual(SyntaxNode expected, SyntaxNode actual, bool checkTrivia)
            {
                AssertNodesEqual(_verifier, _syntaxKindType, expected, actual, checkTrivia);
            }

            private void AssertChildSyntaxListEqual(ChildSyntaxList expected, ChildSyntaxList actual, bool checkTrivia)
            {
                _verifier.Equal(expected.Count, actual.Count);
                foreach (var (expectedChild, actualChild) in expected.Zip(actual, (first, second) => (first, second)))
                {
                    if (expectedChild.IsToken)
                    {
                        _verifier.True(actualChild.IsToken);
                        AssertTokensEqual(expectedChild.AsToken(), actualChild.AsToken(), checkTrivia);
                    }
                    else
                    {
                        _verifier.True(actualChild.IsNode);
                        AssertNodesEqual(expectedChild.AsNode(), actualChild.AsNode(), checkTrivia);
                    }
                }
            }

            private void AssertTokensEqual(SyntaxToken expected, SyntaxToken actual, bool checkTrivia)
            {
                AssertTriviaListEqual(expected.LeadingTrivia, actual.LeadingTrivia, checkTrivia);
                AssertSyntaxKindEqual(expected.RawKind, actual.RawKind);
                _verifier.Equal(expected.Value, actual.Value);
                _verifier.Equal(expected.Text, actual.Text);
                _verifier.Equal(expected.ValueText, actual.ValueText);
                AssertTriviaListEqual(expected.TrailingTrivia, actual.TrailingTrivia, checkTrivia);
            }

            private void AssertTriviaListEqual(SyntaxTriviaList expected, SyntaxTriviaList actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
                {
                    AssertTriviaEqual(expected[i], actual[i], checkTrivia);
                }

                _verifier.Equal(expected.Count, actual.Count);
            }

            private void AssertTriviaEqual(SyntaxTrivia expected, SyntaxTrivia actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                AssertSyntaxKindEqual(expected.RawKind, actual.RawKind);
                _verifier.Equal(expected.HasStructure, actual.HasStructure);
                _verifier.Equal(expected.IsDirective, actual.IsDirective);
                _verifier.Equal(expected.GetAnnotations(), actual.GetAnnotations());
                if (expected.HasStructure)
                {
                    AssertNodesEqual(expected.GetStructure(), actual.GetStructure(), checkTrivia);
                }
            }

            private void AssertSyntaxKindEqual(int expected, int actual)
            {
                if (expected == actual)
                {
                    return;
                }

                if (_syntaxKindType.GetTypeInfo()?.IsEnum ?? false)
                {
                    _verifier.Equal(
                        Enum.Format(_syntaxKindType, (ushort)expected, "G"),
                        Enum.Format(_syntaxKindType, (ushort)actual, "G"));
                }
                else
                {
                    _verifier.Equal(expected, actual);
                }
            }
        }
    }
}
