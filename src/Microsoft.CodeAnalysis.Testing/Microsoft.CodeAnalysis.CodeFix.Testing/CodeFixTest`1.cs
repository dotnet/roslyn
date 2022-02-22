// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeFixTest<TVerifier> : CodeActionTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        /// <inheritdoc cref="CodeActionTest{TVerifier}.CodeActionIndex"/>
        [Obsolete("Use " + nameof(CodeActionIndex) + " instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int? CodeFixIndex
        {
            get => CodeActionIndex;
            set => CodeActionIndex = value;
        }

        /// <inheritdoc cref="CodeActionTest{TVerifier}.CodeActionEquivalenceKey"/>
        [Obsolete("Use " + nameof(CodeActionEquivalenceKey) + " instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string? CodeFixEquivalenceKey
        {
            get => CodeActionEquivalenceKey;
            set => CodeActionEquivalenceKey = value;
        }

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
        /// to complete after at most one operation for each fixable diagnostic in the input source has been applied.
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
        /// <item><description>If all projects in the solution have the same <see cref="ProjectState.Language"/>, the default value is treated as <c>1</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as new negative of the number of languages represented by projects in the solution.</description></item>
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
        /// property. If the number of Fix All in Document iterations is not specified, the value is automatically
        /// selected according to the current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If a value has been explicitly provided for <see cref="NumberOfFixAllIterations"/>, the value is used as-is.</description></item>
        /// <item><description>If the expected Fix All output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as the negative of the number of distinct documents containing fixable diagnostics (typically <c>-1</c>).</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Fix All in Document operations are expected
        /// to complete after at most one operation for each fixable document in the input source has been applied.
        /// Completing in fewer iterations is acceptable."</para>
        /// </note>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        /// <seealso cref="NumberOfFixAllIterations"/>
        public int? NumberOfFixAllInDocumentIterations { get; set; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing for Fix All in Project
        /// scenarios.
        /// </summary>
        /// <remarks>
        /// <para>See the <see cref="NumberOfIncrementalIterations"/> property for an overview of the behavior of this
        /// property. If the number of Fix All in Project iterations is not specified, the value is automatically
        /// selected according to the current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If a value has been explicitly provided for <see cref="NumberOfFixAllIterations"/>, the value is used as-is.</description></item>
        /// <item><description>If the expected Fix All output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as the negative of the number of distinct projects containing fixable diagnostics (typically <c>-1</c>).</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Fix All in Project operations are expected
        /// to complete after at most one operation for each fixable project in the input source has been applied.
        /// Completing in fewer iterations is acceptable."</para>
        /// </note>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        /// <seealso cref="NumberOfFixAllIterations"/>
        public int? NumberOfFixAllInProjectIterations { get; set; }

        /// <summary>
        /// Gets or sets the code fix test behaviors applying to this test. The default value is
        /// <see cref="CodeFixTestBehaviors.None"/>.
        /// </summary>
        public CodeFixTestBehaviors CodeFixTestBehaviors { get; set; }

        /// <inheritdoc cref="CodeActionTest{TVerifier}.CodeActionValidationMode"/>
        [Obsolete("Use " + nameof(CodeActionValidationMode) + " instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CodeFixValidationMode CodeFixValidationMode
        {
            get => CodeActionValidationMode switch
            {
                CodeActionValidationMode.None => CodeFixValidationMode.None,
                CodeActionValidationMode.SemanticStructure => CodeFixValidationMode.SemanticStructure,
                CodeActionValidationMode.Full => CodeFixValidationMode.Full,
                _ => throw new InvalidOperationException(),
            };

            set => CodeActionValidationMode = value switch
            {
                CodeFixValidationMode.None => CodeActionValidationMode.None,
                CodeFixValidationMode.SemanticStructure => CodeActionValidationMode.SemanticStructure,
                CodeFixValidationMode.Full => CodeActionValidationMode.Full,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }

        protected CodeFixTest()
        {
            FixedState = new SolutionState(DefaultTestProjectName, Language, DefaultFilePathPrefix, DefaultFileExt);
            BatchFixedState = new SolutionState(DefaultTestProjectName, Language, DefaultFilePathPrefix, DefaultFileExt);
        }

        /// <summary>
        /// Returns the code fixes being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeFixProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeFixProvider> GetCodeFixProviders();

        /// <summary>
        /// Creates a code fix context to be used for testing.
        /// </summary>
        /// <param name="document">Document to fix.</param>
        /// <param name="span">Text span within the <paramref name="document"/> to fix.</param>
        /// <param name="diagnostics">Diagnostic to fix.</param>
        /// <param name="registerCodeFix">Delegate to register a <see cref="CodeAction"/> fixing a subset of diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New <see cref="CodeFixContext"/>.</returns>
        protected virtual CodeFixContext CreateCodeFixContext(Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix, CancellationToken cancellationToken)
            => new CodeFixContext(document, span, diagnostics, registerCodeFix, cancellationToken);

        /// <summary>
        /// Creates a new <see cref="FixAllContext"/>.
        /// </summary>
        /// <param name="document">Document within which fix all occurrences was triggered, or null when applying fix all to a diagnostic with no source location.</param>
        /// <param name="project">Project within which fix all occurrences was triggered.</param>
        /// <param name="codeFixProvider">Underlying <see cref="CodeFixes.CodeFixProvider"/> which triggered this fix all.</param>
        /// <param name="scope"><see cref="FixAllScope"/> to fix all occurrences.</param>
        /// <param name="codeActionEquivalenceKey">The <see cref="CodeAction.EquivalenceKey"/> value expected of a <see cref="CodeAction"/> participating in this fix all.</param>
        /// <param name="diagnosticIds">Diagnostic Ids to fix.</param>
        /// <param name="fixAllDiagnosticProvider">
        /// <see cref="FixAllContext.DiagnosticProvider"/> to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token for fix all computation.</param>
        /// <returns>New <see cref="FixAllContext"/></returns>
        protected virtual FixAllContext CreateFixAllContext(
            Document? document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string? codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
            CancellationToken cancellationToken)
            => document != null ?
                new FixAllContext(document, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken) :
                new FixAllContext(project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider, cancellationToken);

        /// <inheritdoc />
        protected override bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics)
        {
            if (base.IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics))
            {
                return true;
            }

            return CodeFixProvidersHandleDiagnostic(diagnostic);

            bool CodeFixProvidersHandleDiagnostic(Diagnostic localDiagnostic)
            {
                var codeFixProviders = GetCodeFixProviders();
                return codeFixProviders
                    .Any(provider => provider.FixableDiagnosticIds.Any(fixerDiagnosticId => string.Equals(fixerDiagnosticId, localDiagnostic.Id, StringComparison.OrdinalIgnoreCase)));
            }
        }

        protected override async Task RunImplAsync(CancellationToken cancellationToken)
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

            var allowFixAll = (CodeFixTestBehaviors & CodeFixTestBehaviors.SkipFixAllCheck) != CodeFixTestBehaviors.SkipFixAllCheck;

            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), testState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of test state"), cancellationToken).ConfigureAwait(false);

            if (CodeFixExpected())
            {
                // Verify code fix output before verifying the diagnostics of the fixed state
                await VerifyFixAsync(testState, fixedState, batchFixedState, Verify, cancellationToken).ConfigureAwait(false);

                await VerifyDiagnosticsAsync(new EvaluatedProjectState(fixedState, ReferenceAssemblies), fixedState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), fixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of fixed state"), cancellationToken).ConfigureAwait(false);
                if (allowFixAll && CodeActionExpected(BatchFixedState))
                {
                    await VerifyDiagnosticsAsync(new EvaluatedProjectState(batchFixedState, ReferenceAssemblies), batchFixedState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), batchFixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of batch fixed state"), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private bool CodeFixExpected()
        {
            return CodeActionExpected(FixedState)
                || CodeActionExpected(BatchFixedState);
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
            var fixers = GetCodeFixProviders().ToImmutableArray();
            var fixableDiagnostics = testState.ExpectedDiagnostics.Where(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id))).ToImmutableArray();

            int numberOfIncrementalIterations;
            int numberOfFixAllIterations;
            int numberOfFixAllInProjectIterations;
            int numberOfFixAllInDocumentIterations;
            if (NumberOfIncrementalIterations != null)
            {
                numberOfIncrementalIterations = NumberOfIncrementalIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, fixedState, recursive: true))
                {
                    numberOfIncrementalIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per fixable diagnostic
                    numberOfIncrementalIterations = -fixableDiagnostics.Count();
                }
            }

            if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, batchFixedState, recursive: true))
                {
                    numberOfFixAllIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per language with fixable diagnostics. Since we can't tell the
                    // language from ExpectedDiagnostic, use a conservative value from the number of project languages
                    // present.
                    numberOfFixAllIterations = -Enumerable.Repeat(testState.Language, 1).Concat(testState.AdditionalProjects.Select(p => p.Value.Language)).Distinct().Count();
                }
            }

            if (NumberOfFixAllInProjectIterations != null)
            {
                numberOfFixAllInProjectIterations = NumberOfFixAllInProjectIterations.Value;
            }
            else if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllInProjectIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                numberOfFixAllInProjectIterations = 0;
                if (HasAnyChange(testState, batchFixedState, recursive: false))
                {
                    // Expect at most one iteration for a fixable primary project
                    numberOfFixAllInProjectIterations--;
                }

                foreach (var (name, state) in testState.AdditionalProjects)
                {
                    if (!batchFixedState.AdditionalProjects.TryGetValue(name, out var expected)
                        || HasAnyChange(state, expected, recursive: true))
                    {
                        // Expect at most one iteration for each fixable additional project
                        numberOfFixAllInProjectIterations--;
                    }
                }
            }

            if (NumberOfFixAllInDocumentIterations != null)
            {
                numberOfFixAllInDocumentIterations = NumberOfFixAllInDocumentIterations.Value;
            }
            else if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllInDocumentIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, batchFixedState, recursive: false))
                {
                    numberOfFixAllInDocumentIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per fixable document
                    numberOfFixAllInDocumentIterations = -fixableDiagnostics.GroupBy(diagnostic => diagnostic.Spans.FirstOrDefault().Span.Path).Count();
                }
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

                var t2 = CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.SkipFixAllInDocumentCheck)
                    ? ((Task)Task.FromResult(true)).ConfigureAwait(false)
                    : VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllInDocumentIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, verifier.PushContext("Fix all in document"), cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t2;
                }

                var t3 = CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.SkipFixAllInProjectCheck)
                    ? ((Task)Task.FromResult(true)).ConfigureAwait(false)
                    : VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllInProjectIterations, FixAllAnalyzerDiagnosticsInProjectAsync, verifier.PushContext("Fix all in project"), cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t3;
                }

                var t4 = CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.SkipFixAllInSolutionCheck)
                    ? ((Task)Task.FromResult(true)).ConfigureAwait(false)
                    : VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInSolutionAsync, verifier.PushContext("Fix all in solution"), cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Selects the diagnostic to fix when <see cref="CodeFixTestBehaviors.FixOne"/> is used.
        /// </summary>
        /// <param name="fixableDiagnostics">The diagnostics available for fixing.</param>
        /// <returns>The diagnostic to fix; otherwise, <see langword="null"/> if no diagnostics should be fixed.</returns>
        protected virtual Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
        {
            return fixableDiagnostics.FirstOrDefault();
        }

        private async Task VerifyFixAsync(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<CodeFixProvider> codeFixProviders,
            SolutionState oldState,
            SolutionState newState,
            int numberOfIterations,
            Func<ImmutableArray<DiagnosticAnalyzer>, ImmutableArray<CodeFixProvider>, int?, string?, Action<CodeAction, IVerifier>?, Project, int, IVerifier, CancellationToken, Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)>> getFixedProject,
            IVerifier verifier,
            CancellationToken cancellationToken)
        {
            var project = await CreateProjectAsync(new EvaluatedProjectState(oldState, ReferenceAssemblies), oldState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), cancellationToken);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            ExceptionDispatchInfo? iterationCountFailure;
            (project, iterationCountFailure) = await getFixedProject(analyzers, codeFixProviders, CodeActionIndex, CodeActionEquivalenceKey, CodeActionVerifier, project, numberOfIterations, verifier, cancellationToken).ConfigureAwait(false);

            // After applying all of the code fixes, compare the resulting string to the inputted one
            await VerifyProjectAsync(newState, project, verifier, cancellationToken).ConfigureAwait(false);

            foreach (var additionalProject in newState.AdditionalProjects)
            {
                var actualProject = project.Solution.Projects.Single(p => p.Name == additionalProject.Key);
                await VerifyProjectAsync(additionalProject.Value, actualProject, verifier, cancellationToken);
            }

            // Validate the iteration counts after validating the content
            iterationCountFailure?.Throw();
        }

        private async Task VerifyProjectAsync(ProjectState newState, Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            // After applying all of the code fixes, compare the resulting string to the inputted one
            var updatedDocuments = project.Documents.ToArray();

            verifier.Equal(newState.Sources.Count, updatedDocuments.Length, $"expected '{nameof(newState)}.{nameof(SolutionState.Sources)}' and '{nameof(updatedDocuments)}' to be equal but '{nameof(newState)}.{nameof(SolutionState.Sources)}' contains '{newState.Sources.Count}' documents and '{nameof(updatedDocuments)}' contains '{updatedDocuments.Length}' documents");

            for (var i = 0; i < updatedDocuments.Length; i++)
            {
                var actual = await GetSourceTextFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                verifier.EqualOrDiff(newState.Sources[i].content.ToString(), actual.ToString(), $"content of '{newState.Sources[i].filename}' did not match. Diff shown with expected as baseline:");
                verifier.Equal(newState.Sources[i].content.Encoding, actual.Encoding, $"encoding of '{newState.Sources[i].filename}' was expected to be '{newState.Sources[i].content.Encoding?.WebName}' but was '{actual.Encoding?.WebName}'");
                verifier.Equal(newState.Sources[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newState.Sources[i].filename}' was expected to be '{newState.Sources[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                verifier.Equal(newState.Sources[i].filename, updatedDocuments[i].Name, $"file name was expected to be '{newState.Sources[i].filename}' but was '{updatedDocuments[i].Name}'");
            }

            var updatedAdditionalDocuments = project.AdditionalDocuments.ToArray();

            verifier.Equal(newState.AdditionalFiles.Count, updatedAdditionalDocuments.Length, $"expected '{nameof(newState)}.{nameof(SolutionState.AdditionalFiles)}' and '{nameof(updatedAdditionalDocuments)}' to be equal but '{nameof(newState)}.{nameof(SolutionState.AdditionalFiles)}' contains '{newState.AdditionalFiles.Count}' documents and '{nameof(updatedAdditionalDocuments)}' contains '{updatedAdditionalDocuments.Length}' documents");

            for (var i = 0; i < updatedAdditionalDocuments.Length; i++)
            {
                var actual = await updatedAdditionalDocuments[i].GetTextAsync(cancellationToken).ConfigureAwait(false);
                verifier.EqualOrDiff(newState.AdditionalFiles[i].content.ToString(), actual.ToString(), $"content of '{newState.AdditionalFiles[i].filename}' did not match. Diff shown with expected as baseline:");
                verifier.Equal(newState.AdditionalFiles[i].content.Encoding, actual.Encoding, $"encoding of '{newState.AdditionalFiles[i].filename}' was expected to be '{newState.AdditionalFiles[i].content.Encoding?.WebName}' but was '{actual.Encoding?.WebName}'");
                verifier.Equal(newState.AdditionalFiles[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newState.AdditionalFiles[i].filename}' was expected to be '{newState.AdditionalFiles[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                verifier.Equal(newState.AdditionalFiles[i].filename, updatedAdditionalDocuments[i].Name, $"file name was expected to be '{newState.AdditionalFiles[i].filename}' but was '{updatedAdditionalDocuments[i].Name}'");
            }

            var updatedAnalyzerConfigDocuments = project.AnalyzerConfigDocuments().ToArray();

            verifier.Equal(newState.AnalyzerConfigFiles.Count, updatedAnalyzerConfigDocuments.Length, $"expected '{nameof(newState)}.{nameof(SolutionState.AnalyzerConfigFiles)}' and '{nameof(updatedAnalyzerConfigDocuments)}' to be equal but '{nameof(newState)}.{nameof(SolutionState.AnalyzerConfigFiles)}' contains '{newState.AnalyzerConfigFiles.Count}' documents and '{nameof(updatedAnalyzerConfigDocuments)}' contains '{updatedAnalyzerConfigDocuments.Length}' documents");

            for (var i = 0; i < updatedAnalyzerConfigDocuments.Length; i++)
            {
                var actual = await updatedAnalyzerConfigDocuments[i].GetTextAsync(cancellationToken).ConfigureAwait(false);
                verifier.EqualOrDiff(newState.AnalyzerConfigFiles[i].content.ToString(), actual.ToString(), $"content of '{newState.AnalyzerConfigFiles[i].filename}' did not match. Diff shown with expected as baseline:");
                verifier.Equal(newState.AnalyzerConfigFiles[i].content.Encoding, actual.Encoding, $"encoding of '{newState.AnalyzerConfigFiles[i].filename}' was expected to be '{newState.AnalyzerConfigFiles[i].content.Encoding?.WebName}' but was '{actual.Encoding?.WebName}'");
                verifier.Equal(newState.AnalyzerConfigFiles[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newState.AnalyzerConfigFiles[i].filename}' was expected to be '{newState.AnalyzerConfigFiles[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                verifier.Equal(newState.AnalyzerConfigFiles[i].filename, updatedAnalyzerConfigDocuments[i].Name, $"file name was expected to be '{newState.AnalyzerConfigFiles[i].filename}' but was '{updatedAnalyzerConfigDocuments[i].Name}'");
            }
        }

        private async Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (numberOfIterations == -1)
            {
                // For better error messages, use '==' instead of '<=' for iteration comparison when the right hand
                // side is 1.
                numberOfIterations = 1;
            }

            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<(Project project, Diagnostic diagnostic)>();

            ExceptionDispatchInfo? firstValidationError = null;
            var currentIteration = -1;
            bool done;
            do
            {
                currentIteration++;

                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, additionalDiagnostics: ImmutableArray<(Project project, Diagnostic diagnostic)>.Empty, CompilerDiagnostics, verifier, cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                try
                {
                    verifier.True(--numberOfIterations >= -1, "The upper limit for the number of code fix iterations was exceeded");
                }
                catch (Exception ex)
                {
                    return (project, firstValidationError ?? ExceptionDispatchInfo.Capture(ex));
                }

                previousDiagnostics = analyzerDiagnostics;

                var fixableDiagnostics = analyzerDiagnostics
                    .Where(diagnostic => codeFixProviders.Any(provider => provider.FixableDiagnosticIds.Contains(diagnostic.diagnostic.Id)))
                    .Where(diagnostic => project.Solution.GetDocument(diagnostic.diagnostic.Location.SourceTree) is object)
                    .ToImmutableArray();

                if (CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
                {
                    var diagnosticToFix = TrySelectDiagnosticToFix(fixableDiagnostics.Select(x => x.diagnostic).ToImmutableArray());
                    fixableDiagnostics = diagnosticToFix is object ? ImmutableArray.Create(fixableDiagnostics.Single(x => x.diagnostic == diagnosticToFix)) : ImmutableArray<(Project project, Diagnostic diagnostic)>.Empty;
                }

                done = true;
                var anyActions = false;
                foreach (var (_, diagnostic) in fixableDiagnostics)
                {
                    var actions = ImmutableArray.CreateBuilder<CodeAction>();

                    var fixableDocument = project.Solution.GetDocument(diagnostic.Location.SourceTree);
                    foreach (var codeFixProvider in codeFixProviders)
                    {
                        if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                        {
                            // do not pass unsupported diagnostics to a code fix provider
                            continue;
                        }

                        var context = CreateCodeFixContext(fixableDocument, diagnostic.Location.SourceSpan, ImmutableArray.Create(diagnostic), (a, d) => actions.Add(a), cancellationToken);
                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                    }

                    var filteredActions = FilterCodeActions(actions.ToImmutable());
                    var actionToApply = TryGetCodeActionToApply(currentIteration, filteredActions, codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, verifier);
                    if (actionToApply != null)
                    {
                        anyActions = true;

                        var originalProjectId = project.Id;
                        var (fixedProject, currentError) = await ApplyCodeActionAsync(fixableDocument.Project, actionToApply, verifier, cancellationToken).ConfigureAwait(false);
                        firstValidationError ??= currentError;
                        if (fixedProject != fixableDocument.Project)
                        {
                            done = false;
                            project = fixedProject.Solution.GetProject(originalProjectId);
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

                if (CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
                {
                    break;
                }
            }
            while (!done);

            try
            {
                if (expectedNumberOfIterations >= 0)
                {
                    verifier.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
                }
                else
                {
                    verifier.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
                }
            }
            catch (Exception ex)
            {
                return (project, firstValidationError ?? ExceptionDispatchInfo.Capture(ex));
            }

            return (project, firstValidationError ?? null);
        }

        private Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixAllAnalyzerDiagnosticsInDocumentAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Document, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, project, numberOfIterations, verifier, cancellationToken);
        }

        private Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixAllAnalyzerDiagnosticsInProjectAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Project, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, project, numberOfIterations, verifier, cancellationToken);
        }

        private Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixAllAnalyzerDiagnosticsInSolutionAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Solution, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, project, numberOfIterations, verifier, cancellationToken);
        }

        private async Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope scope, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (numberOfIterations == -1)
            {
                // For better error messages, use '==' instead of '<=' for iteration comparison when the right hand
                // side is 1.
                numberOfIterations = 1;
            }

            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<(Project project, Diagnostic diagnostic)>();

            ExceptionDispatchInfo? firstValidationError = null;
            var currentIteration = -1;
            bool done;
            do
            {
                currentIteration++;

                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, additionalDiagnostics: ImmutableArray<(Project project, Diagnostic diagnostic)>.Empty, CompilerDiagnostics, verifier, cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                try
                {
                    verifier.False(--numberOfIterations < -1, "The upper limit for the number of fix all iterations was exceeded");
                }
                catch (Exception ex)
                {
                    return (project, firstValidationError ?? ExceptionDispatchInfo.Capture(ex));
                }

                var fixableDiagnostics = analyzerDiagnostics
                    .Where(diagnostic => codeFixProviders.Any(provider => provider.FixableDiagnosticIds.Contains(diagnostic.diagnostic.Id)))
                    .Where(diagnostic => project.Solution.GetDocument(diagnostic.diagnostic.Location.SourceTree) is object)
                    .ToImmutableArray();

                if (CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
                {
                    var diagnosticToFix = TrySelectDiagnosticToFix(fixableDiagnostics.Select(x => x.diagnostic).ToImmutableArray());
                    fixableDiagnostics = diagnosticToFix is object ? ImmutableArray.Create(fixableDiagnostics.Single(x => x.diagnostic == diagnosticToFix)) : ImmutableArray<(Project project, Diagnostic diagnostic)>.Empty;
                }

                Diagnostic? firstDiagnostic = null;
                CodeFixProvider? effectiveCodeFixProvider = null;
                string? equivalenceKey = null;
                foreach (var (_, diagnostic) in fixableDiagnostics)
                {
                    var actions = new List<(CodeAction, CodeFixProvider)>();

                    var diagnosticDocument = project.Solution.GetDocument(diagnostic.Location.SourceTree);
                    foreach (var codeFixProvider in codeFixProviders)
                    {
                        if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                        {
                            // do not pass unsupported diagnostics to a code fix provider
                            continue;
                        }

                        var actionsBuilder = ImmutableArray.CreateBuilder<CodeAction>();
                        var context = CreateCodeFixContext(diagnosticDocument, diagnostic.Location.SourceSpan, ImmutableArray.Create(diagnostic), (a, d) => actionsBuilder.Add(a), cancellationToken);
                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                        actions.AddRange(FilterCodeActions(actionsBuilder.ToImmutable()).Select(action => (action, codeFixProvider)));
                    }

                    var actionToApply = TryGetCodeActionToApply(currentIteration, actions.Select(a => a.Item1).ToImmutableArray(), codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, verifier);
                    if (actionToApply != null)
                    {
                        firstDiagnostic = diagnostic;
                        effectiveCodeFixProvider = actions.SingleOrDefault(a => a.Item1 == actionToApply).Item2;
                        equivalenceKey = actionToApply.EquivalenceKey;
                        break;
                    }
                }

                var fixAllProvider = effectiveCodeFixProvider?.GetFixAllProvider();
                if (firstDiagnostic == null || fixAllProvider == null || !fixAllProvider.GetSupportedFixAllScopes().Contains(scope))
                {
                    numberOfIterations++;
                    break;
                }

                previousDiagnostics = analyzerDiagnostics;

                done = true;

                FixAllContext.DiagnosticProvider fixAllDiagnosticProvider = TestDiagnosticProvider.Create(analyzerDiagnostics);

                var fixableDocument = project.Solution.GetDocument(firstDiagnostic.Location.SourceTree);
                var analyzerDiagnosticIds = analyzers.SelectMany(x => x.SupportedDiagnostics).Select(x => x.Id);
                var compilerDiagnosticIds = codeFixProviders.SelectMany(codeFixProvider => codeFixProvider.FixableDiagnosticIds).Where(x => x.StartsWith("CS", StringComparison.Ordinal) || x.StartsWith("BC", StringComparison.Ordinal));
                var disabledDiagnosticIds = project.CompilationOptions.SpecificDiagnosticOptions.Where(x => x.Value == ReportDiagnostic.Suppress).Select(x => x.Key);
                var relevantIds = analyzerDiagnosticIds.Concat(compilerDiagnosticIds).Except(disabledDiagnosticIds).Distinct();
                var fixAllContext = CreateFixAllContext(fixableDocument, fixableDocument.Project, effectiveCodeFixProvider!, scope, equivalenceKey, relevantIds, fixAllDiagnosticProvider, cancellationToken);

                var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                if (action == null)
                {
                    return (project, null);
                }

                var originalProjectId = project.Id;
                var (fixedProject, currentError) = await ApplyCodeActionAsync(fixableDocument.Project, action, verifier, cancellationToken).ConfigureAwait(false);
                firstValidationError ??= currentError;
                if (fixedProject != fixableDocument.Project)
                {
                    done = false;
                    project = fixedProject.Solution.GetProject(originalProjectId);
                }

                if (CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
                {
                    break;
                }
            }
            while (!done);

            try
            {
                if (expectedNumberOfIterations >= 0)
                {
                    verifier.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
                }
                else
                {
                    verifier.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
                }
            }
            catch (Exception ex)
            {
                return (project, firstValidationError ?? ExceptionDispatchInfo.Capture(ex));
            }

            return (project, firstValidationError);
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

        private static bool AreDiagnosticsDifferent(ImmutableArray<(Project project, Diagnostic diagnostic)> analyzerDiagnostics, ImmutableArray<(Project project, Diagnostic diagnostic)> previousDiagnostics)
        {
            if (analyzerDiagnostics.Length != previousDiagnostics.Length)
            {
                return true;
            }

            for (var i = 0; i < analyzerDiagnostics.Length; i++)
            {
                if ((analyzerDiagnostics[i].project.Id != previousDiagnostics[i].project.Id)
                    || (analyzerDiagnostics[i].diagnostic.Id != previousDiagnostics[i].diagnostic.Id)
                    || (analyzerDiagnostics[i].diagnostic.Location.SourceSpan != previousDiagnostics[i].diagnostic.Location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
