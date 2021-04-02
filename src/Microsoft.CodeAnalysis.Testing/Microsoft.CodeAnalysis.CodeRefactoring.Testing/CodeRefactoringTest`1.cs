// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeRefactoringTest<TVerifier> : CodeActionTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        public static DiagnosticDescriptor TriggerSpanDescriptor { get; } = new DiagnosticDescriptor(
            id: "Refactoring",
            title: "Refactoring",
            messageFormat: string.Empty,
            category: "Refactoring",
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

        /// <summary>
        /// Sets the expected output source file for code refactoring testing.
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

        public bool OffersEmptyRefactoring { get; set; }

        protected CodeRefactoringTest()
        {
            FixedState = new SolutionState(DefaultTestProjectName, Language, DefaultFilePathPrefix, DefaultFileExt);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new DiagnosticAnalyzer[] { new EmptyDiagnosticAnalyzer() };

        /// <summary>
        /// Returns the code refactorings being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeRefactoringProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders();

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty($"{nameof(TestState)}.{nameof(SolutionState.Sources)}", TestState.Sources);

            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = ImmutableArray<string>.Empty;

            var rawTestState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics);
            var rawFixedState = FixedState.WithInheritedValuesApplied(rawTestState, fixableDiagnostics);

            var testState = rawTestState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);
            var fixedState = rawFixedState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), FilterTriggerSpanResults(testState.ExpectedDiagnostics).ToArray(), Verify.PushContext("Diagnostics of test state"), cancellationToken).ConfigureAwait(false);

            if (CodeActionExpected())
            {
                await VerifyDiagnosticsAsync(new EvaluatedProjectState(fixedState, ReferenceAssemblies), fixedState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), FilterTriggerSpanResults(fixedState.ExpectedDiagnostics).ToArray(), Verify.PushContext("Diagnostics of fixed state"), cancellationToken).ConfigureAwait(false);
                await VerifyRefactoringAsync(testState, fixedState, GetTriggerSpanResult(testState.ExpectedDiagnostics), Verify, cancellationToken).ConfigureAwait(false);
            }

            static IEnumerable<DiagnosticResult> FilterTriggerSpanResults(IEnumerable<DiagnosticResult> expected)
            {
                return expected.Where(result => result.Id != TriggerSpanDescriptor.Id);
            }

            static DiagnosticResult GetTriggerSpanResult(IEnumerable<DiagnosticResult> expected)
            {
                DiagnosticResult? triggerSpan = null;
                foreach (var result in expected)
                {
                    if (result.Id == TriggerSpanDescriptor.Id)
                    {
                        Verify.Equal(null, triggerSpan, "Expected the test to only include a single trigger span for refactoring");
                        triggerSpan = result;
                    }
                }

                Verify.True(triggerSpan.HasValue, "Expected the test to include a single trigger span for refactoring");
                return triggerSpan!.Value;
            }
        }

        private bool CodeActionExpected()
        {
            return CodeActionExpected(FixedState);
        }

        protected override DiagnosticDescriptor? GetDefaultDiagnostic(DiagnosticAnalyzer[] analyzers)
        {
            if (base.GetDefaultDiagnostic(analyzers) is { } descriptor)
            {
                return descriptor;
            }

            return TriggerSpanDescriptor;
        }

        /// <summary>
        /// Called to test a C# code refactoring when applied on the input source as a string.
        /// </summary>
        /// <param name="testState">The effective input test state.</param>
        /// <param name="fixedState">The effective test state after the refactoring is applied.</param>
        /// <param name="triggerSpan">A <see cref="DiagnosticResult"/> indicating the location where the refactoring will be triggered.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyRefactoringAsync(SolutionState testState, SolutionState fixedState, DiagnosticResult triggerSpan, IVerifier verifier, CancellationToken cancellationToken)
        {
            var numberOfIncrementalIterations = OffersEmptyRefactoring || HasAnyChange(testState, fixedState) ? 1 : 0;
            await VerifyRefactoringAsync(Language, triggerSpan, GetCodeRefactoringProviders().ToImmutableArray(), testState, fixedState, numberOfIncrementalIterations, ApplyRefactoringAsync, verifier.PushContext("Code refactoring application"), cancellationToken);
        }

        private async Task VerifyRefactoringAsync(
            string language,
            DiagnosticResult triggerSpan,
            ImmutableArray<CodeRefactoringProvider> codeRefactoringProviders,
            SolutionState oldState,
            SolutionState newState,
            int numberOfIterations,
            Func<DiagnosticResult, ImmutableArray<CodeRefactoringProvider>, int?, string?, Action<CodeAction, IVerifier>?, Project, int, IVerifier, CancellationToken, Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)>> getFixedProject,
            IVerifier verifier,
            CancellationToken cancellationToken)
        {
            var project = await CreateProjectAsync(new EvaluatedProjectState(oldState, ReferenceAssemblies), oldState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), cancellationToken);
            _ = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            ExceptionDispatchInfo? iterationCountFailure;
            (project, iterationCountFailure) = await getFixedProject(triggerSpan, codeRefactoringProviders, CodeActionIndex, CodeActionEquivalenceKey, CodeActionVerifier, project, numberOfIterations, verifier, cancellationToken).ConfigureAwait(false);

            // After applying the refactoring, compare the resulting string to the inputted one
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

            // Validate the iteration counts after validating the content
            iterationCountFailure?.Throw();
        }

        private async Task<(Project project, ExceptionDispatchInfo? iterationCountFailure)> ApplyRefactoringAsync(DiagnosticResult triggerSpan, ImmutableArray<CodeRefactoringProvider> codeRefactoringProviders, int? codeActionIndex, string? codeActionEquivalenceKey, Action<CodeAction, IVerifier>? codeActionVerifier, Project project, int numberOfIterations, IVerifier verifier, CancellationToken cancellationToken)
        {
            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            bool done;
            do
            {
                try
                {
                    verifier.True(--numberOfIterations >= -1, "The upper limit for the number of code fix iterations was exceeded");
                }
                catch (Exception ex)
                {
                    return (project, ExceptionDispatchInfo.Capture(ex));
                }

                done = true;
                var anyActions = false;
                var actions = ImmutableArray.CreateBuilder<CodeAction>();

                var location = await GetTriggerLocationAsync();

                foreach (var codeRefactoringProvider in codeRefactoringProviders)
                {
                    var context = new CodeRefactoringContext(project.GetDocument(location.SourceTree), location.SourceSpan, actions.Add, cancellationToken);
                    await codeRefactoringProvider.ComputeRefactoringsAsync(context).ConfigureAwait(false);
                }

                var filteredActions = FilterCodeActions(actions.ToImmutable());
                var actionToApply = TryGetCodeActionToApply(filteredActions, codeActionIndex, codeActionEquivalenceKey, codeActionVerifier, verifier);
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

                if (!anyActions)
                {
                    verifier.True(done, "Expected to be done executing actions.");

                    // Avoid counting iterations that do not provide any code actions
                    numberOfIterations++;
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

            async Task<Location> GetTriggerLocationAsync()
            {
                var path = triggerSpan.Spans[0].Span.Path;
                var span = triggerSpan.Spans[0].Span.Span;

                var documentIds = project.Solution.GetDocumentIdsWithFilePath(triggerSpan.Spans[0].Span.Path);
                var document = project.Solution.GetDocument(documentIds.Single());
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                return Location.Create(tree, text.Lines.GetTextSpan(span));
            }
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
    }
}
