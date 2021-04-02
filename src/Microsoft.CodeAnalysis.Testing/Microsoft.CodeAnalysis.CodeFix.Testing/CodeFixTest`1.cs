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

            var allowFixAll = (CodeFixTestBehaviors & CodeFixTestBehaviors.SkipFixAllCheck) != CodeFixTestBehaviors.SkipFixAllCheck;

            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), testState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of test state"), cancellationToken).ConfigureAwait(false);

            if (CodeFixExpected())
            {
                await VerifyDiagnosticsAsync(new EvaluatedProjectState(fixedState, ReferenceAssemblies), fixedState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), fixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of fixed state"), cancellationToken).ConfigureAwait(false);
                if (allowFixAll && CodeActionExpected(BatchFixedState))
                {
                    await VerifyDiagnosticsAsync(new EvaluatedProjectState(batchFixedState, ReferenceAssemblies), batchFixedState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), batchFixedState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of batch fixed state"), cancellationToken).ConfigureAwait(false);
                }

                await VerifyFixAsync(testState, fixedState, batchFixedState, Verify, cancellationToken).ConfigureAwait(false);
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
            else if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllInDocumentIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(testState, batchFixedState))
                {
                    numberOfFixAllInDocumentIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per fixable document
                    var fixers = GetCodeFixProviders().ToArray();
                    var fixableDiagnostics = testState.ExpectedDiagnostics.Where(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
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
                    : VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), testState, batchFixedState, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInProjectAsync, verifier.PushContext("Fix all in project"), cancellationToken).ConfigureAwait(false);
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

            // Validate the iteration counts after validating the content
            iterationCountFailure?.Throw();
        }

        private async Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string? codeFixEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
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
                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, additionalDiagnostics: ImmutableArray<Diagnostic>.Empty, CompilerDiagnostics, verifier, cancellationToken).ConfigureAwait(false);
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
                    return (project, ExceptionDispatchInfo.Capture(ex));
                }

                previousDiagnostics = analyzerDiagnostics;

                var fixableDiagnostics = analyzerDiagnostics
                    .Where(diagnostic => codeFixProviders.Any(provider => provider.FixableDiagnosticIds.Contains(diagnostic.Id)))
                    .Where(diagnostic => project.GetDocument(diagnostic.Location.SourceTree) is object)
                    .ToImmutableArray();

                if (CodeFixTestBehaviors.HasFlag(CodeFixTestBehaviors.FixOne))
                {
                    var diagnosticToFix = TrySelectDiagnosticToFix(fixableDiagnostics);
                    fixableDiagnostics = diagnosticToFix is object ? ImmutableArray.Create(diagnosticToFix) : ImmutableArray<Diagnostic>.Empty;
                }

                done = true;
                var anyActions = false;
                foreach (var diagnostic in fixableDiagnostics)
                {
                    var actions = ImmutableArray.CreateBuilder<CodeAction>();

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

                    var filteredActions = FilterCodeActions(actions.ToImmutable());
                    var actionToApply = TryGetCodeActionToApply(filteredActions, codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, verifier);
                    if (actionToApply != null)
                    {
                        anyActions = true;

                        var fixedProject = await ApplyCodeActionAsync(project, actionToApply, verifier, cancellationToken).ConfigureAwait(false);
                        if (fixedProject != project)
                        {
                            done = false;
                            project = fixedProject;
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
                return (project, ExceptionDispatchInfo.Capture(ex));
            }

            return (project, null);
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
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsAsync(project.Solution, analyzers, additionalDiagnostics: ImmutableArray<Diagnostic>.Empty, CompilerDiagnostics, verifier, cancellationToken).ConfigureAwait(false);
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
                    return (project, ExceptionDispatchInfo.Capture(ex));
                }

                Diagnostic? firstDiagnostic = null;
                CodeFixProvider? effectiveCodeFixProvider = null;
                string? equivalenceKey = null;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    var actions = new List<(CodeAction, CodeFixProvider)>();

                    foreach (var codeFixProvider in codeFixProviders)
                    {
                        if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id)
                            || !(project.GetDocument(diagnostic.Location.SourceTree) is { } document))
                        {
                            // do not pass unsupported diagnostics to a code fix provider
                            continue;
                        }

                        var actionsBuilder = ImmutableArray.CreateBuilder<CodeAction>();
                        var context = new CodeFixContext(document, diagnostic, (a, d) => actionsBuilder.Add(a), cancellationToken);
                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                        actions.AddRange(FilterCodeActions(actionsBuilder.ToImmutable()).Select(action => (action, codeFixProvider)));
                    }

                    var actionToApply = TryGetCodeActionToApply(actions.Select(a => a.Item1).ToImmutableArray(), codeFixIndex, codeFixEquivalenceKey, codeActionVerifier, verifier);
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
                    return (project, null);
                }

                var fixedProject = await ApplyCodeActionAsync(project, action, verifier, cancellationToken).ConfigureAwait(false);
                if (fixedProject != project)
                {
                    done = false;
                    project = fixedProject;
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
                return (project, ExceptionDispatchInfo.Capture(ex));
            }

            return (project, null);
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
    }
}
