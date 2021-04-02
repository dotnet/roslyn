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
    public abstract class SourceGeneratorTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => Enumerable.Empty<DiagnosticAnalyzer>();

        /// <summary>
        /// Returns the source generators being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="ISourceGenerator"/> to be used.</returns>
        protected abstract IEnumerable<ISourceGenerator> GetSourceGenerators();

        protected abstract GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators);

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = ImmutableArray<string>.Empty;
            var testState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics).WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            var diagnostics = await VerifySourceGeneratorAsync(testState, Verify, cancellationToken).ConfigureAwait(false);
            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies).WithAdditionalDiagnostics(diagnostics), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), testState.ExpectedDiagnostics.ToArray(), Verify.PushContext("Diagnostics of test state"), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<Compilation> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var (finalProject, diagnostics) = await ApplySourceGeneratorAsync(GetSourceGenerators().ToImmutableArray(), project, verifier, cancellationToken).ConfigureAwait(false);
            return (await finalProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
        }

        /// <summary>
        /// Called to test a C# source generator when applied on the input source as a string.
        /// </summary>
        /// <param name="testState">The effective input test state.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task<ImmutableArray<Diagnostic>> VerifySourceGeneratorAsync(SolutionState testState, IVerifier verifier, CancellationToken cancellationToken)
        {
            return await VerifySourceGeneratorAsync(Language, GetSourceGenerators().ToImmutableArray(), testState, ApplySourceGeneratorAsync, verifier.PushContext("Source generator application"), cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> VerifySourceGeneratorAsync(
            string language,
            ImmutableArray<ISourceGenerator> sourceGenerators,
            SolutionState testState,
            Func<ImmutableArray<ISourceGenerator>, Project, IVerifier, CancellationToken, Task<(Project project, ImmutableArray<Diagnostic> diagnostics)>> getFixedProject,
            IVerifier verifier,
            CancellationToken cancellationToken)
        {
            var project = await CreateProjectAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), cancellationToken);
            _ = await GetCompilerDiagnosticsAsync(project, verifier, cancellationToken).ConfigureAwait(false);

            ImmutableArray<Diagnostic> diagnostics;
            (project, diagnostics) = await getFixedProject(sourceGenerators, project, verifier, cancellationToken).ConfigureAwait(false);

            // After applying the source generator, compare the resulting string to the inputted one
            if (!TestBehaviors.HasFlag(TestBehaviors.SkipGeneratedSourcesCheck))
            {
                var updatedDocuments = project.Documents.ToArray();
                var expectedSources = testState.Sources.Concat(testState.GeneratedSources).ToArray();

                verifier.Equal(expectedSources.Length, updatedDocuments.Length, $"expected '{nameof(testState)}.{nameof(SolutionState.Sources)}' with '{nameof(testState)}.{nameof(SolutionState.GeneratedSources)}' to match '{nameof(updatedDocuments)}', but '{nameof(testState)}.{nameof(SolutionState.Sources)}' with '{nameof(testState)}.{nameof(SolutionState.GeneratedSources)}' contains '{expectedSources.Length}' documents and '{nameof(updatedDocuments)}' contains '{updatedDocuments.Length}' documents");

                for (var i = 0; i < updatedDocuments.Length; i++)
                {
                    var actual = await GetSourceTextFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                    verifier.EqualOrDiff(expectedSources[i].content.ToString(), actual.ToString(), $"content of '{expectedSources[i].filename}' did not match. Diff shown with expected as baseline:");
                    verifier.Equal(expectedSources[i].content.Encoding, actual.Encoding, $"encoding of '{expectedSources[i].filename}' was expected to be '{expectedSources[i].content.Encoding?.WebName}' but was '{actual.Encoding?.WebName}'");
                    verifier.Equal(expectedSources[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{expectedSources[i].filename}' was expected to be '{expectedSources[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                    verifier.Equal(expectedSources[i].filename, updatedDocuments[i].Name, $"file name was expected to be '{expectedSources[i].filename}' but was '{updatedDocuments[i].Name}'");
                }
            }

            return diagnostics;
        }

        private async Task<(Project project, ImmutableArray<Diagnostic> diagnostics)> ApplySourceGeneratorAsync(ImmutableArray<ISourceGenerator> sourceGenerators, Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            verifier.True(compilation is { });

            var driver = CreateGeneratorDriver(project, sourceGenerators).RunGenerators(compilation, cancellationToken);
            var result = driver.GetRunResult();

            var updatedProject = project;
            foreach (var tree in result.GeneratedTrees)
            {
                updatedProject = updatedProject.AddDocument(tree.FilePath, await tree.GetTextAsync(cancellationToken).ConfigureAwait(false), filePath: tree.FilePath).Project;
            }

            return (updatedProject, result.Diagnostics);
        }

        /// <summary>
        /// Get the existing compiler diagnostics on the input document.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to run the compiler diagnostic analyzers on.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The compiler diagnostics that were found in the code.</returns>
        private static async Task<ImmutableArray<Diagnostic>> GetCompilerDiagnosticsAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var allDiagnostics = ImmutableArray.Create<Diagnostic>();

            foreach (var document in project.Documents)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                verifier.True(semanticModel is { });

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
