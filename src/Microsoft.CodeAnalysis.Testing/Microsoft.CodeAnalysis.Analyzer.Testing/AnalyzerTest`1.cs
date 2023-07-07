// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Testing.Extensions;
using Microsoft.CodeAnalysis.Testing.Lightup;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using IComparer = System.Collections.IComparer;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        private static readonly Lazy<IExportProviderFactory> ExportProviderFactory;
        private static readonly ConditionalWeakTable<Diagnostic, object> NonLocalDiagnostics = new ConditionalWeakTable<Diagnostic, object>();

        static AnalyzerTest()
        {
            ExportProviderFactory = new Lazy<IExportProviderFactory>(
                () =>
                {
                    var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
                    var parts = Task.Run(() => discovery.CreatePartsAsync(MefHostServices.DefaultAssemblies)).GetAwaiter().GetResult();
                    var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts).WithDocumentTextDifferencingService();

                    var configuration = CompositionConfiguration.Create(catalog);
                    var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
                    return runtimeComposition.CreateExportProviderFactory();
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the default verifier for the test.
        /// </summary>
        protected static TVerifier Verify { get; } = new TVerifier();

        /// <summary>
        /// Gets the prefix to apply to source files added without an explicit name.
        /// </summary>
        protected virtual string DefaultFilePathPrefix { get; } = "/0/Test";

        /// <summary>
        /// Gets the name of the default project created for testing.
        /// </summary>
        protected virtual string DefaultTestProjectName { get; } = "TestProject";

        /// <summary>
        /// Gets the default full name of the first source file added for a test.
        /// </summary>
        protected virtual string DefaultFilePath => DefaultFilePathPrefix + 0 + "." + DefaultFileExt;

        /// <summary>
        /// Gets the default file extension to use for files added to the test without an explicit name.
        /// </summary>
        protected abstract string DefaultFileExt { get; }

        protected AnalyzerTest()
        {
            TestState = new SolutionState(DefaultTestProjectName, Language, DefaultFilePathPrefix, DefaultFileExt);
        }

        /// <summary>
        /// Gets the language name used for the test.
        /// </summary>
        /// <value>
        /// The language name used for the test.
        /// </value>
        public abstract string Language { get; }

        /// <summary>
        /// Sets the input source file for analyzer or code fix testing.
        /// </summary>
        /// <seealso cref="TestState"/>
        public string TestCode
        {
            set
            {
                if (value != null)
                {
                    TestState.Sources.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the list of diagnostics expected in the source(s) and/or additonal files.
        /// </summary>
        public List<DiagnosticResult> ExpectedDiagnostics => TestState.ExpectedDiagnostics;

        /// <summary>
        /// Gets or sets the behavior of compiler diagnostics in validation scenarios. The default value is
        /// <see cref="CompilerDiagnostics.Errors"/>.
        /// </summary>
        public CompilerDiagnostics CompilerDiagnostics { get; set; } = CompilerDiagnostics.Errors;

        /// <summary>
        /// Gets or sets options for the markup processor when markup is used for diagnostics. The default value is
        /// <see cref="MarkupOptions.None"/>.
        /// </summary>
        public MarkupOptions MarkupOptions { get; set; }

        public SolutionState TestState { get; }

        /// <summary>
        /// Gets the collection of inputs to provide to the XML documentation resolver.
        /// </summary>
        /// <remarks>
        /// <para>Files in this collection may be referenced via <c>&lt;include&gt;</c> elements in documentation
        /// comments.</para>
        /// </remarks>
        public Dictionary<string, string> XmlReferences { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the test behaviors applying to this analyzer. The default value is
        /// <see cref="TestBehaviors.None"/>.
        /// </summary>
        public TestBehaviors TestBehaviors { get; set; }

        /// <summary>
        /// Gets a collection of diagnostics to explicitly disable in the <see cref="CompilationOptions"/> for projects.
        /// </summary>
        public List<string> DisabledDiagnostics { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the default reference assemblies to use.
        /// </summary>
        /// <see cref="ProjectState.ReferenceAssemblies"/>
        public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Default;

        /// <summary>
        /// Gets or sets an additional verifier for a diagnostic.
        /// The action compares actual <see cref="Diagnostic"/> and the expected
        /// <see cref="DiagnosticResult"/> based on custom test requirements not yet supported by the test framework.
        /// </summary>
        public Action<Diagnostic, DiagnosticResult, IVerifier>? DiagnosticVerifier { get; set; }

        /// <summary>
        /// Gets a collection of transformation functions to apply to <see cref="Workspace.Options"/> during diagnostic
        /// or code fix test setup.
        /// </summary>
        public List<Func<OptionSet, OptionSet>> OptionsTransforms { get; } = new List<Func<OptionSet, OptionSet>>();

        /// <summary>
        /// Gets a collection of transformation functions to apply to a <see cref="Solution"/> during diagnostic or code
        /// fix test setup.
        /// </summary>
        public List<Func<Solution, ProjectId, Solution>> SolutionTransforms { get; } = new List<Func<Solution, ProjectId, Solution>>();

        /// <summary>
        /// Gets or sets the timeout to use when matching expected and actual diagnostics. The default value is 2
        /// seconds.
        /// </summary>
        protected TimeSpan MatchDiagnosticsTimeout { get; set; } = TimeSpan.FromSeconds(2);

        private readonly ConcurrentBag<Workspace> _workspaces = new ConcurrentBag<Workspace>();

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the operation will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await RunImplAsync(cancellationToken);
            }
            finally
            {
                while (_workspaces.TryTake(out var workspace))
                {
                    workspace.Dispose();
                }
            }
        }

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the operation will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task RunImplAsync(CancellationToken cancellationToken)
        {
            if (!TestState.GeneratedSources.Any())
            {
                // Verify the test state has at least one source, which may or may not be generated
                Verify.NotEmpty($"{nameof(TestState)}.{nameof(SolutionState.Sources)}", TestState.Sources);
            }

            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = ImmutableArray<string>.Empty;
            var testState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics).WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            var diagnostics = await VerifySourceGeneratorAsync(testState, Verify, cancellationToken).ConfigureAwait(false);
            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), testState.ExpectedDiagnostics.ToArray(), Verify, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the default diagnostic to use during markup processing. By default, the <em>single</em> diagnostic of
        /// the first analyzer is used, and no default diagonostic is available if multiple diagnostics are provided by
        /// the analyzer. If <see cref="MarkupOptions.UseFirstDescriptor"/> is used, the first available diagnostic
        /// is used.
        /// </summary>
        /// <param name="analyzers">The analyzers to consider.</param>
        /// <returns>The default diagnostic to use during markup processing.</returns>
        protected internal virtual DiagnosticDescriptor? GetDefaultDiagnostic(DiagnosticAnalyzer[] analyzers)
        {
            if (analyzers.Length == 0)
            {
                return null;
            }

            if (MarkupOptions.HasFlag(MarkupOptions.UseFirstDescriptor))
            {
                foreach (var analyzer in analyzers)
                {
                    if (analyzer.SupportedDiagnostics.Any())
                    {
                        return analyzer.SupportedDiagnostics[0];
                    }
                }

                return null;
            }
            else if (analyzers[0].SupportedDiagnostics.Length == 1)
            {
                return analyzers[0].SupportedDiagnostics[0];
            }
            else
            {
                return null;
            }
        }

        protected string FormatVerifierMessage(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic actual, DiagnosticResult expected, string message)
        {
            return $"{message}{Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"Expected diagnostic:{Environment.NewLine}" +
                $"    {FormatDiagnostics(analyzers, DefaultFilePath, expected)}{Environment.NewLine}" +
                $"Actual diagnostic:{Environment.NewLine}" +
                $"    {FormatDiagnostics(analyzers, DefaultFilePath, actual)}{Environment.NewLine}";
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
            var sourceGenerators = GetSourceGenerators().ToImmutableArray();
            if (sourceGenerators.IsEmpty)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return await VerifySourceGeneratorAsync(Language, sourceGenerators, testState, verifier.PushContext("Source generator application"), cancellationToken);
        }

        private protected async Task<ImmutableArray<Diagnostic>> VerifySourceGeneratorAsync(
            string language,
            ImmutableArray<Type> sourceGenerators,
            SolutionState testState,
            IVerifier verifier,
            CancellationToken cancellationToken)
        {
            var project = await CreateProjectAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), cancellationToken);

            ImmutableArray<Diagnostic> diagnostics;
            (project, diagnostics) = await ApplySourceGeneratorsAsync(sourceGenerators, project, verifier, cancellationToken).ConfigureAwait(false);

            // After applying the source generator, compare the resulting string to the inputted one
            if (!TestBehaviors.HasFlag(TestBehaviors.SkipGeneratedSourcesCheck))
            {
                var numOriginalSources = testState.Sources.Count;
                var updatedOriginalDocuments = project.Documents.Take(numOriginalSources).ToArray();
                var generatedDocuments = project.Documents.Skip(numOriginalSources).ToArray();

                // Verify no changes occurred to the original documents
                var updatedOriginalDocumentsWithTextBuilder = ImmutableArray.CreateBuilder<(Document document, SourceText content)>();
                foreach (var updatedOriginalDocument in updatedOriginalDocuments)
                {
                    updatedOriginalDocumentsWithTextBuilder.Add((updatedOriginalDocument, await updatedOriginalDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)));
                }

                VerifyDocuments(
                    verifier.PushContext("Original files after running source generators"),
                    updatedOriginalDocumentsWithTextBuilder.ToImmutable(),
                    testState.Sources.ToImmutableArray(),
                    allowReordering: false,
                    DefaultFilePathPrefix,
                    GetNameAndFoldersFromPath,
                    MatchDiagnosticsTimeout);

                // Verify the source generated documents match expectations
                var generatedDocumentsWithTextBuilder = ImmutableArray.CreateBuilder<(Document document, SourceText content)>();
                foreach (var generatedDocument in generatedDocuments)
                {
                    generatedDocumentsWithTextBuilder.Add((generatedDocument, await generatedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)));
                }

                VerifyDocuments(
                    verifier.PushContext("Verifying source generated files"),
                    generatedDocumentsWithTextBuilder.ToImmutable(),
                    testState.GeneratedSources.ToImmutableArray(),
                    allowReordering: true,
                    DefaultFilePathPrefix,
                    static (_, path) => GetNameAndFoldersFromSourceGeneratedFilePath(path),
                    MatchDiagnosticsTimeout);
            }

            return diagnostics;

            static void VerifyDocuments(
                IVerifier verifier,
                ImmutableArray<(Document document, SourceText content)> actualDocuments,
                ImmutableArray<(string filename, SourceText content)> expectedDocuments,
                bool allowReordering,
                string defaultFilePathPrefix,
                Func<string, string, (string fileName, IEnumerable<string> folders)> getNameAndFolders,
                TimeSpan matchTimeout)
            {
                ImmutableArray<WeightedMatch.Result<(string filename, SourceText content), (Document document, SourceText content)>> matches;
                if (allowReordering)
                {
                    matches = WeightedMatch.Match(
                        expectedDocuments,
                        actualDocuments,
                        ImmutableArray.Create<Func<(string filename, SourceText content), (Document document, SourceText content), bool, double>>(
                            static (expected, actual, exactOnly) =>
                            {
                                if (actual.content.ToString() == expected.content.ToString())
                                {
                                    return 0.0;
                                }

                                if (exactOnly)
                                {
                                    // Avoid expensive diff calculation when exact match was requested.
                                    return 1.0;
                                }

                                var diffBuilder = new InlineDiffBuilder(new Differ());
                                var diff = diffBuilder.BuildDiffModel(expected.content.ToString(), actual.content.ToString(), ignoreWhitespace: true, ignoreCase: false, new LineChunker());
                                var changeCount = diff.Lines.Count(static line => line.Type is ChangeType.Inserted or ChangeType.Deleted);
                                if (changeCount == 0)
                                {
                                    // We have a failure caused only by line ending or whitespace differences. Make sure
                                    // to use a non-zero value so it can be distinguished from exact matches.
                                    changeCount = 1;
                                }

                                // Apply a multiplier to the content distance to account for its increased importance
                                // over encoding and checksum algorithm changes.
                                var priority = 3;

                                return priority * changeCount / (double)diff.Lines.Count;
                            },
                            static (expected, actual, exactOnly) =>
                            {
                                return actual.content.Encoding == expected.content.Encoding ? 0.0 : 1.0;
                            },
                            static (expected, actual, exactOnly) =>
                            {
                                return actual.content.ChecksumAlgorithm == expected.content.ChecksumAlgorithm ? 0.0 : 1.0;
                            },
                            (expected, actual, exactOnly) =>
                            {
                                var distance = 0.0;
                                var (fileName, folders) = getNameAndFolders(defaultFilePathPrefix, expected.filename);
                                if (fileName != actual.document.Name)
                                {
                                    distance += 1.0;
                                }

                                if (!folders.SequenceEqual(actual.document.Folders))
                                {
                                    distance += 1.0;
                                }

                                return distance;
                            }),
                        matchTimeout);
                }
                else
                {
                    // Matching with an empty set of matching functions always takes the 1:1 alignment without reordering
                    matches = WeightedMatch.Match(
                        expectedDocuments,
                        actualDocuments,
                        ImmutableArray<Func<(string filename, SourceText content), (Document document, SourceText content), bool, double>>.Empty,
                        matchTimeout);
                }

                // Use EqualOrDiff to verify the actual and expected filenames (and total collection length) in a convenient manner
                verifier.EqualOrDiff(
                    string.Join(Environment.NewLine, matches.Select(match => match.TryGetExpected(out var expected) ? expected.filename : string.Empty)),
                    string.Join(Environment.NewLine, matches.Select(match => match.TryGetActual(out var actual) ? actual.document.FilePath : string.Empty)),
                    $"Expected source file list to match");

                // Follow by verifying each property of interest
                foreach (var result in matches)
                {
                    if (!result.TryGetExpected(out var expected)
                        || !result.TryGetActual(out var actual))
                    {
                        throw new InvalidOperationException("Unexpected state: should have failed during the previous assertion.");
                    }

                    verifier.EqualOrDiff(expected.content.ToString(), actual.content.ToString(), $"content of '{expected.filename}' did not match. Diff shown with expected as baseline:");
                    verifier.Equal(expected.content.Encoding, actual.content.Encoding, $"encoding of '{expected.filename}' was expected to be '{expected.content.Encoding?.WebName}' but was '{actual.content.Encoding?.WebName}'");
                    verifier.Equal(expected.content.ChecksumAlgorithm, actual.content.ChecksumAlgorithm, $"checksum algorithm of '{expected.filename}' was expected to be '{expected.content.ChecksumAlgorithm}' but was '{actual.content.ChecksumAlgorithm}'");

                    // Source-generated sources are implicitly in a subtree, so they have a different folders calculation.
                    var (fileName, folders) = getNameAndFolders(defaultFilePathPrefix, expected.filename);
                    verifier.Equal(fileName, actual.document.Name, $"file name was expected to be '{fileName}' but was '{actual.document.Name}'");
                    verifier.SequenceEqual(folders, actual.document.Folders, message: $"folders was expected to be '{string.Join("/", folders)}' but was '{string.Join("/", actual.document.Folders)}'");
                }
            }
        }

        /// <summary>
        /// General method that gets a collection of actual <see cref="Diagnostic"/>s found in the source after the
        /// analyzer is run, then verifies each of them.
        /// </summary>
        /// <param name="primaryProject">The primary project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="expected">A collection of <see cref="DiagnosticResult"/>s that should appear after the analyzer
        /// is run on the sources.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyDiagnosticsAsync(EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            (string filename, SourceText content)[] sources = primaryProject.Sources.ToArray();

            var analyzers = GetDiagnosticAnalyzers().ToImmutableArray();
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(primaryProject, additionalProjects, analyzers, verifier, cancellationToken).ConfigureAwait(false), analyzers, expected, verifier);
            await VerifyGeneratedCodeDiagnosticsAsync(analyzers, sources, primaryProject, additionalProjects, expected, verifier, cancellationToken).ConfigureAwait(false);
            await VerifySuppressionDiagnosticsAsync(analyzers, sources, primaryProject, additionalProjects, expected, verifier, cancellationToken).ConfigureAwait(false);
        }

        private async Task VerifyGeneratedCodeDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, (string filename, SourceText content)[] sources, EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (TestBehaviors.HasFlag(TestBehaviors.SkipGeneratedCodeCheck)
                || analyzers.All(analyzer => AnalyzerInfo.HasConfiguredGeneratedCodeAnalysis(analyzer)))
            {
                return;
            }

            if (!expected.Any(x => IsSubjectToExclusion(x, analyzers, sources)))
            {
                return;
            }

            // Diagnostics reported by the compiler and analyzer diagnostics which don't have a location will
            // still be reported. We also insert a new line at the beginning so we have to move all diagnostic
            // locations which have a specific position down by one line.
            var expectedResults = expected
                .Where(x => !IsSubjectToExclusion(x, analyzers, sources))
                .Select(x => IsInSourceFile(x, sources) ? x.WithLineOffset(1) : x)
                .ToArray();

            var generatedCodeVerifier = verifier.PushContext("Verifying exclusions in <auto-generated> code");
            var commentPrefix = Language == LanguageNames.CSharp ? "//" : "'";
            var transformedProject = primaryProject.WithSources(primaryProject.Sources.Select(x => (x.filename, x.content.Replace(new TextSpan(0, 0), $" {commentPrefix} <auto-generated>\r\n"))).ToImmutableArray());
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(transformedProject, additionalProjects, analyzers, generatedCodeVerifier, cancellationToken).ConfigureAwait(false), analyzers, expectedResults, generatedCodeVerifier);
        }

        /// <summary>
        /// Checks that diagnostics will not be reported if a <c>#pragma warning disable</c> appears at the beginning of the file.
        /// </summary>
        private async Task VerifySuppressionDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, (string filename, SourceText content)[] sources, EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (TestBehaviors.HasFlag(TestBehaviors.SkipSuppressionCheck))
            {
                return;
            }

            if (!expected.Any(x => IsSubjectToExclusion(x, analyzers, sources)))
            {
                return;
            }

            // Diagnostics reported by the compiler and analyzer diagnostics which don't have a location will
            // still be reported. We also insert a new line at the beginning so we have to move all diagnostic
            // locations which have a specific position down by one line.
            var expectedResults = expected
                .Where(x => !IsSuppressible(analyzers, x, sources))
                .Select(x => IsInSourceFile(x, sources) ? x.WithLineOffset(1) : x)
                .ToArray();

            var prefix = Language == LanguageNames.CSharp ? "#pragma warning disable" : "#Disable Warning";
            var suppressionVerifier = verifier.PushContext($"Verifying exclusions in '{prefix}' code");
            var suppressedDiagnostics = expected.Where(x => IsSubjectToExclusion(x, analyzers, sources)).Select(x => x.Id).Distinct();
            var suppression = prefix + " " + string.Join(", ", suppressedDiagnostics);
            var transformedProject = primaryProject.WithSources(primaryProject.Sources.Select(x => (x.filename, x.content.Replace(new TextSpan(0, 0), $"{suppression}\r\n"))).ToImmutableArray());
            var actualDiagnostics = await GetSortedDiagnosticsAsync(transformedProject, additionalProjects, analyzers, suppressionVerifier, cancellationToken).ConfigureAwait(false);

            // For #pragma verification, we only care about unsuppressed diagnostics. Filter out suppressed diagnostics
            // from both the expected and actual lists.
            actualDiagnostics = actualDiagnostics.Where((projectAndDiagnostic) => !projectAndDiagnostic.diagnostic.IsSuppressed()).ToImmutableArray();
            expectedResults = expectedResults.Where(diagnosticResult => diagnosticResult.IsSuppressed != true).ToArray();

            VerifyDiagnosticResults(actualDiagnostics, analyzers, expectedResults, suppressionVerifier);
        }

        /// <summary>
        /// Checks each of the actual <see cref="Diagnostic"/>s found and compares them with the corresponding
        /// <see cref="DiagnosticResult"/> in the array of expected results. <see cref="Diagnostic"/>s are considered
        /// equal only if the <see cref="DiagnosticResult.Spans"/>, <see cref="DiagnosticResult.Id"/>,
        /// <see cref="DiagnosticResult.Severity"/>, and <see cref="DiagnosticResult.Message"/> of the
        /// <see cref="DiagnosticResult"/> match the actual <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="actualResults">The <see cref="Diagnostic"/>s found by the compiler after running the analyzer
        /// on the source code.</param>
        /// <param name="analyzers">The analyzers that have been run on the sources.</param>
        /// <param name="expectedResults">A collection of <see cref="DiagnosticResult"/>s describing the expected
        /// diagnostics for the sources.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        private void VerifyDiagnosticResults(IEnumerable<(Project project, Diagnostic diagnostic)> actualResults, ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult[] expectedResults, IVerifier verifier)
        {
            var matchedDiagnostics = MatchDiagnostics(actualResults.ToArray(), expectedResults);
            verifier.Equal(actualResults.Count(), matchedDiagnostics.Count(x => x.actual is object), $"{nameof(MatchDiagnostics)} failed to include all actual diagnostics in the result");
            verifier.Equal(expectedResults.Length, matchedDiagnostics.Count(x => x.expected is object), $"{nameof(MatchDiagnostics)} failed to include all expected diagnostics in the result");

            actualResults = matchedDiagnostics.Select(x => x.actual).Where(x => x is { }).Select(x => x!.Value);
            expectedResults = matchedDiagnostics.Where(x => x.expected is object).Select(x => x.expected.GetValueOrDefault()).ToArray();

            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count();

            var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, DefaultFilePath, actualResults.Select(result => result.diagnostic).ToArray()) : "    NONE.";
            var message = $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n";
            verifier.Equal(expectedCount, actualCount, message);

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (!expected.HasLocation)
                {
                    message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, "Expected a project diagnostic with no location:");
                    verifier.Equal(Location.None, actual.diagnostic.Location, message);
                }
                else
                {
                    VerifyDiagnosticLocation(analyzers, actual.diagnostic, expected, actual.diagnostic.Location, expected.Spans[0], verifier);
                    if (!expected.Options.HasFlag(DiagnosticOptions.IgnoreAdditionalLocations))
                    {
                        var additionalLocations = actual.diagnostic.AdditionalLocations.ToArray();

                        message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected {expected.Spans.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:");
                        verifier.Equal(expected.Spans.Length - 1, additionalLocations.Length, message);

                        for (var j = 0; j < additionalLocations.Length; ++j)
                        {
                            VerifyDiagnosticLocation(analyzers, actual.diagnostic, expected, additionalLocations[j], expected.Spans[j + 1], verifier);
                        }
                    }
                }

                message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.diagnostic.Id}\"");
                verifier.Equal(expected.Id, actual.diagnostic.Id, message);

                if (!expected.Options.HasFlag(DiagnosticOptions.IgnoreSeverity))
                {
                    message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.diagnostic.Severity}\"");
                    verifier.Equal(expected.Severity, actual.diagnostic.Severity, message);
                }

                if (expected.Message != null)
                {
                    message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.diagnostic.GetMessage()}\"");
                    verifier.Equal(expected.Message, actual.diagnostic.GetMessage(), message);
                }
                else if (expected.MessageArguments?.Length > 0)
                {
                    message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected diagnostic message arguments to match");
                    verifier.SequenceEqual(
                        expected.MessageArguments.Select(argument => argument?.ToString() ?? string.Empty),
                        actual.diagnostic.Arguments().Select(argument => argument?.ToString() ?? string.Empty),
                        StringComparer.Ordinal,
                        message);
                }

                if (expected.IsSuppressed.HasValue)
                {
                    message = FormatVerifierMessage(analyzers, actual.diagnostic, expected, $"Expected diagnostic suppression state to match");
                    verifier.Equal(expected.IsSuppressed.Value, actual.diagnostic.IsSuppressed(), message);
                }

                DiagnosticVerifier?.Invoke(actual.diagnostic, expected, verifier);
            }
        }

        /// <summary>
        /// Match actual diagnostics with expected diagnostics.
        /// </summary>
        /// <remarks>
        /// <para>While each actual diagnostic contains complete information about the diagnostic (location, severity,
        /// message, etc.), the expected diagnostics sometimes contain partial information. It is therefore possible for
        /// an expected diagnostic to match more than one actual diagnostic, while another expected diagnostic with more
        /// complete information only matches a single specific actual diagnostic.</para>
        ///
        /// <para>This method attempts to find a best matching of actual and expected diagnostics.</para>
        /// </remarks>
        /// <param name="actualResults">The actual diagnostics reported by analysis.</param>
        /// <param name="expectedResults">The expected diagnostics.</param>
        /// <returns>
        /// <para>A collection of matched diagnostics, with the following characteristics:</para>
        ///
        /// <list type="bullet">
        /// <item><description>Every element of <paramref name="actualResults"/> will appear exactly once as the first element of an item in the result.</description></item>
        /// <item><description>Every element of <paramref name="expectedResults"/> will appear exactly once as the second element of an item in the result.</description></item>
        /// <item><description>An item in the result which specifies both a <see cref="Diagnostic"/> and a <see cref="DiagnosticResult"/> indicates a matched pair, i.e. the actual and expected results are believed to refer to the same diagnostic.</description></item>
        /// <item><description>An item in the result which specifies only a <see cref="Diagnostic"/> indicates an actual diagnostic for which no matching expected diagnostic was found.</description></item>
        /// <item><description>An item in the result which specifies only a <see cref="DiagnosticResult"/> indicates an expected diagnostic for which no matching actual diagnostic was found.</description></item>
        /// </list>
        ///
        /// <para>If no exact match is found (all actual diagnostics are matched to an expected diagnostic without
        /// errors), this method is <em>allowed</em> to attempt fall-back matching using a strategy intended to minimize
        /// the total number of mismatched pairs.</para>
        /// </returns>
        private ImmutableArray<((Project project, Diagnostic diagnostic)? actual, DiagnosticResult? expected)> MatchDiagnostics((Project project, Diagnostic diagnostic)[] actualResults, DiagnosticResult[] expectedResults)
        {
            var result = WeightedMatch.Match(
                expectedResults.ToImmutableArray(),
                actualResults.ToImmutableArray(),
                ImmutableArray.Create<Func<DiagnosticResult, (Project project, Diagnostic diagnostic), bool, double>>(
                    static (expected, actual, exactOnly) =>
                    {
                        if (IsLocationMatch(actual.diagnostic, expected, out var matchSpanStart, out var matchSpanEnd))
                        {
                            return 0.0;
                        }

                        return (matchSpanStart, matchSpanEnd) switch
                        {
                            (true, true) => 1.0,
                            (true, false) => 2.0,
                            (false, true) => 2.0,
                            (false, false) => 3.0,
                        };
                    },
                    static (expected, actual, exactOnly) => expected.Id == actual.diagnostic.Id ? 0.0 : 1.0,
                    static (expected, actual, exactOnly) => IsSeverityMatch(actual.diagnostic, expected) ? 0.0 : 1.0,
                    static (expected, actual, exactOnly) => IsMessageMatch(actual.diagnostic, expected) ? 0.0 : 1.0),
                MatchDiagnosticsTimeout);

            return result
                .Select(result =>
                {
                    (Project project, Diagnostic diagnostic)? actual = result.TryGetActual(out var maybeActual) ? maybeActual : null;
                    DiagnosticResult? expected = result.TryGetExpected(out var maybeExpected) ? maybeExpected : null;
                    return (actual, expected);
                })
                .ToImmutableArray();

            static bool IsLocationMatch(Diagnostic diagnostic, DiagnosticResult diagnosticResult, out bool matchSpanStart, out bool matchSpanEnd)
            {
                var lineSpan = diagnostic.Location.GetLineSpan();
                var additionalLineSpans = diagnostic.AdditionalLocations.Select(location => location.GetLineSpan()).ToImmutableArray();
                if (!diagnosticResult.HasLocation)
                {
                    matchSpanStart = false;
                    matchSpanEnd = false;
                    return Equals(Location.None, diagnostic.Location);
                }
                else
                {
                    if (!IsLocationMatch2(diagnostic.Location, lineSpan, diagnosticResult.Spans[0], out matchSpanStart, out matchSpanEnd))
                    {
                        return false;
                    }

                    if (diagnosticResult.Options.HasFlag(DiagnosticOptions.IgnoreAdditionalLocations))
                    {
                        return true;
                    }

                    var additionalLocations = diagnostic.AdditionalLocations.ToArray();
                    if (additionalLocations.Length != diagnosticResult.Spans.Length - 1)
                    {
                        // Number of additional locations does not match expected result
                        return false;
                    }

                    for (var i = 0; i < additionalLocations.Length; i++)
                    {
                        if (!IsLocationMatch2(additionalLocations[i], additionalLineSpans[i], diagnosticResult.Spans[i + 1], out _, out _))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            static bool IsLocationMatch2(Location actual, FileLinePositionSpan actualSpan, DiagnosticLocation expected, out bool matchSpanStart, out bool matchSpanEnd)
            {
                matchSpanStart = actualSpan.StartLinePosition == expected.Span.StartLinePosition;
                matchSpanEnd = expected.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength)
                    || actualSpan.EndLinePosition == expected.Span.EndLinePosition;

                var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains("Test0.") == true && expected.Span.Path.Contains("Test."));
                if (!assert)
                {
                    // Expected diagnostic to be in file "{expected.Span.Path}" was actually in file "{actualSpan.Path}"
                    return false;
                }

                if (!matchSpanStart || !matchSpanEnd)
                {
                    return false;
                }

                return true;
            }

            static bool IsSeverityMatch(Diagnostic actual, DiagnosticResult expected)
            {
                if (expected.Options.HasFlag(DiagnosticOptions.IgnoreSeverity))
                {
                    return true;
                }

                return actual.Severity == expected.Severity;
            }

            static bool IsMessageMatch(Diagnostic actual, DiagnosticResult expected)
            {
                if (expected.Message is null)
                {
                    if (expected.MessageArguments?.Length > 0)
                    {
                        var actualArguments = actual.Arguments().Select(ToStringOrEmpty);
                        var expectedArguments = expected.MessageArguments.Select(ToStringOrEmpty);
                        return actualArguments.SequenceEqual(expectedArguments);
                    }

                    return true;
                }

                return string.Equals(expected.Message, actual.GetMessage());
            }

            static string ToStringOrEmpty(object? argument)
                => argument?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Helper method to <see cref="VerifyDiagnosticResults"/> that checks the location of a
        /// <see cref="Diagnostic"/> and compares it with the location described by a
        /// <see cref="FileLinePositionSpan"/>.
        /// </summary>
        /// <param name="analyzers">The analyzer that have been run on the sources.</param>
        /// <param name="diagnostic">The diagnostic that was found in the code.</param>
        /// <param name="expectedDiagnostic">The expected diagnostic.</param>
        /// <param name="actual">The location of the diagnostic found in the code.</param>
        /// <param name="expected">The <see cref="FileLinePositionSpan"/> describing the expected location of the
        /// diagnostic.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        private void VerifyDiagnosticLocation(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, DiagnosticResult expectedDiagnostic, Location actual, DiagnosticLocation expected, IVerifier verifier)
        {
            var actualSpan = actual.GetLineSpan();

            var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains("Test0.") == true && expected.Span.Path.Contains("Test."));

            var message = FormatVerifierMessage(analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to be in file \"{expected.Span.Path}\" was actually in file \"{actualSpan.Path}\"");
            verifier.True(assert, message);

            VerifyLinePosition(analyzers, diagnostic, expectedDiagnostic, actualSpan.StartLinePosition, expected.Span.StartLinePosition, "start", verifier);
            if (!expected.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
            {
                VerifyLinePosition(analyzers, diagnostic, expectedDiagnostic, actualSpan.EndLinePosition, expected.Span.EndLinePosition, "end", verifier);
            }
        }

        private void VerifyLinePosition(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, DiagnosticResult expectedDiagnostic, LinePosition actualLinePosition, LinePosition expectedLinePosition, string positionText, IVerifier verifier)
        {
            var message = FormatVerifierMessage(analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to {positionText} on line \"{expectedLinePosition.Line + 1}\" was actually on line \"{actualLinePosition.Line + 1}\"");
            verifier.Equal(
                expectedLinePosition.Line,
                actualLinePosition.Line,
                message);

            message = FormatVerifierMessage(analyzers, diagnostic, expectedDiagnostic, $"Expected diagnostic to {positionText} at column \"{expectedLinePosition.Character + 1}\" was actually at column \"{actualLinePosition.Character + 1}\"");
            verifier.Equal(
                expectedLinePosition.Character,
                actualLinePosition.Character,
                message);
        }

        /// <summary>
        /// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
        /// </summary>
        /// <param name="analyzers">The analyzers that this verifier tests.</param>
        /// <param name="defaultFilePath">The default file path for diagnostics.</param>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be formatted.</param>
        /// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
        private static string FormatDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, string defaultFilePath, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i)
            {
                var diagnosticsId = diagnostics[i].Id;
                var location = diagnostics[i].Location;

                builder.Append("// ").AppendLine(diagnostics[i].ToString());

                var applicableAnalyzer = analyzers.FirstOrDefault(a => a.SupportedDiagnostics.Any(dd => dd.Id == diagnosticsId));
                if (applicableAnalyzer != null)
                {
                    var analyzerType = applicableAnalyzer.GetType();
                    var rule = location != Location.None && location.IsInSource && applicableAnalyzer.SupportedDiagnostics.Length == 1 ? string.Empty : $"{analyzerType.Name}.{diagnosticsId}";

                    if (location == Location.None || !location.IsInSource)
                    {
                        builder.Append($"new DiagnosticResult({rule})");
                    }
                    else
                    {
                        var resultMethodName = location.SourceTree.FilePath.EndsWith(".cs") ? "VerifyCS.Diagnostic" : "VerifyVB.Diagnostic";
                        builder.Append($"{resultMethodName}({rule})");
                    }
                }
                else
                {
                    builder.Append(
                        diagnostics[i].Severity switch
                        {
                            DiagnosticSeverity.Error => $"{nameof(DiagnosticResult)}.{nameof(DiagnosticResult.CompilerError)}(\"{diagnostics[i].Id}\")",
                            DiagnosticSeverity.Warning => $"{nameof(DiagnosticResult)}.{nameof(DiagnosticResult.CompilerWarning)}(\"{diagnostics[i].Id}\")",
                            var severity => $"new {nameof(DiagnosticResult)}(\"{diagnostics[i].Id}\", {nameof(DiagnosticSeverity)}.{severity})",
                        });
                }

                if (location == Location.None)
                {
                    // No additional location data needed
                }
                else
                {
                    AppendLocation(diagnostics[i].Location);
                    foreach (var additionalLocation in diagnostics[i].AdditionalLocations)
                    {
                        AppendLocation(additionalLocation);
                    }
                }

                var arguments = diagnostics[i].Arguments();
                if (arguments.Count > 0)
                {
                    builder.Append($".{nameof(DiagnosticResult.WithArguments)}(");
                    builder.Append(string.Join(", ", arguments.Select(a => "\"" + a?.ToString() + "\"")));
                    builder.Append(")");
                }

                if (diagnostics[i].IsSuppressed())
                {
                    builder.Append($".{nameof(DiagnosticResult.WithIsSuppressed)}(true)");
                }

                builder.AppendLine(",");
            }

            return builder.ToString();

            // Local functions
            void AppendLocation(Location location)
            {
                var lineSpan = location.GetLineSpan();
                var pathString = location.IsInSource && lineSpan.Path == defaultFilePath ? string.Empty : $"\"{lineSpan.Path}\", ";
                var linePosition = lineSpan.StartLinePosition;
                var endLinePosition = lineSpan.EndLinePosition;
                builder.Append($".WithSpan({pathString}{linePosition.Line + 1}, {linePosition.Character + 1}, {endLinePosition.Line + 1}, {endLinePosition.Character + 1})");
            }
        }

        /// <summary>
        /// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
        /// </summary>
        /// <param name="analyzers">The analyzers that this verifier tests.</param>
        /// <param name="defaultFilePath">The default file path for diagnostics.</param>
        /// <param name="diagnostics">A collection of <see cref="DiagnosticResult"/>s to be formatted.</param>
        /// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
        private static string FormatDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, string defaultFilePath, params DiagnosticResult[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i)
            {
                var diagnosticsId = diagnostics[i].Id;

                builder.Append("// ").AppendLine(diagnostics[i].ToString());

                var applicableAnalyzer = analyzers.FirstOrDefault(a => a.SupportedDiagnostics.Any(dd => dd.Id == diagnosticsId));
                if (applicableAnalyzer != null)
                {
                    var analyzerType = applicableAnalyzer.GetType();
                    var rule = diagnostics[i].HasLocation && applicableAnalyzer.SupportedDiagnostics.Length == 1 ? string.Empty : $"{analyzerType.Name}.{diagnosticsId}";

                    if (!diagnostics[i].HasLocation)
                    {
                        builder.Append($"new DiagnosticResult({rule})");
                    }
                    else
                    {
                        var resultMethodName = diagnostics[i].Spans[0].Span.Path.EndsWith(".cs") ? "VerifyCS.Diagnostic" : "VerifyVB.Diagnostic";
                        builder.Append($"{resultMethodName}({rule})");
                    }
                }
                else
                {
                    builder.Append(
                        diagnostics[i].Severity switch
                        {
                            DiagnosticSeverity.Error => $"{nameof(DiagnosticResult)}.{nameof(DiagnosticResult.CompilerError)}(\"{diagnostics[i].Id}\")",
                            DiagnosticSeverity.Warning => $"{nameof(DiagnosticResult)}.{nameof(DiagnosticResult.CompilerWarning)}(\"{diagnostics[i].Id}\")",
                            var severity => $"new {nameof(DiagnosticResult)}(\"{diagnostics[i].Id}\", {nameof(DiagnosticSeverity)}.{severity})",
                        });
                }

                if (!diagnostics[i].HasLocation)
                {
                    // No additional location data needed
                }
                else
                {
                    foreach (var span in diagnostics[i].Spans)
                    {
                        AppendLocation(span);
                        if (diagnostics[i].Options.HasFlag(DiagnosticOptions.IgnoreAdditionalLocations))
                        {
                            break;
                        }
                    }
                }

                var arguments = diagnostics[i].MessageArguments;
                if (arguments?.Length > 0)
                {
                    builder.Append($".{nameof(DiagnosticResult.WithArguments)}(");
                    builder.Append(string.Join(", ", arguments.Select(a => "\"" + a?.ToString() + "\"")));
                    builder.Append(")");
                }

                if (diagnostics[i].IsSuppressed.HasValue)
                {
                    builder.Append($".{nameof(DiagnosticResult.WithIsSuppressed)}(");
                    builder.Append(diagnostics[i].IsSuppressed.GetValueOrDefault() ? "true" : "false");
                    builder.Append(")");
                }

                builder.AppendLine(",");
            }

            return builder.ToString();

            // Local functions
            void AppendLocation(DiagnosticLocation location)
            {
                var pathString = location.Span.Path == defaultFilePath ? string.Empty : $"\"{location.Span.Path}\", ";
                var linePosition = location.Span.StartLinePosition;

                if (location.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
                {
                    builder.Append($".WithLocation({pathString}{linePosition.Line + 1}, {linePosition.Character + 1})");
                }
                else
                {
                    var endLinePosition = location.Span.EndLinePosition;
                    builder.Append($".WithSpan({pathString}{linePosition.Line + 1}, {linePosition.Character + 1}, {endLinePosition.Line + 1}, {endLinePosition.Character + 1})");
                }
            }
        }

        private static bool IsCompilerDiagnosticId(string id)
        {
            return id.StartsWith("CS", StringComparison.Ordinal)
                || id.StartsWith("BC", StringComparison.Ordinal);
        }

        private static bool IsSubjectToExclusion(DiagnosticResult result, ImmutableArray<DiagnosticAnalyzer> analyzers, (string filename, SourceText content)[] sources)
        {
            if (IsCompilerDiagnosticId(result.Id))
            {
                // This is a compiler diagnostic
                return false;
            }

            if (result.Id.StartsWith("AD", StringComparison.Ordinal))
            {
                // This diagnostic is reported by the analyzer infrastructure
                return false;
            }

            if (result.Spans.IsEmpty)
            {
                return false;
            }

            if (!IsInSourceFile(result, sources))
            {
                // This diagnostic is not reported in a source file
                return false;
            }

            if (!analyzers.Any(analyzer => analyzer.SupportedDiagnostics.Any(supported => supported.Id == result.Id)))
            {
                // This diagnostic is not reported by an active analyzer
                return false;
            }

            return true;
        }

        private static bool IsSuppressible(ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult result, (string filename, SourceText content)[] sources)
        {
            if (!IsSubjectToExclusion(result, analyzers, sources))
            {
                return false;
            }

            foreach (var analyzer in analyzers)
            {
                foreach (var diagnostic in analyzer.SupportedDiagnostics)
                {
                    if (diagnostic.Id != result.Id)
                    {
                        continue;
                    }

                    if (diagnostic.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsInSourceFile(DiagnosticResult result, (string filename, SourceText content)[] sources)
        {
            if (!result.HasLocation)
            {
                return false;
            }

            return sources.Any(source => source.filename.Equals(result.Spans[0].Span.Path));
        }

        /// <summary>
        /// Given classes in the form of strings, their language, and an <see cref="DiagnosticAnalyzer"/> to apply to
        /// it, return the <see cref="Diagnostic"/>s found in the string after converting it to a
        /// <see cref="Document"/>.
        /// </summary>
        /// <param name="primaryProject">The primary project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="analyzers">The analyzers to be run on the sources.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        private async Task<ImmutableArray<(Project project, Diagnostic diagnostic)>> GetSortedDiagnosticsAsync(EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, ImmutableArray<DiagnosticAnalyzer> analyzers, IVerifier verifier, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(primaryProject, additionalProjects, verifier, cancellationToken);
            var primaryProjectInSolution = solution.Projects.Single(project => project.Name == DefaultTestProjectName);
            var additionalDiagnostics = primaryProject.AdditionalDiagnostics.Select(diagnostic => (primaryProjectInSolution, diagnostic)).ToImmutableArray();
            foreach (var additionalProject in additionalProjects)
            {
                var additionalProjectInSolution = solution.Projects.Single(project => project.Name == additionalProject.Name);
                additionalDiagnostics = additionalDiagnostics.AddRange(additionalProject.AdditionalDiagnostics.Select(diagnostic => (additionalProjectInSolution, diagnostic)));
            }

            return await GetSortedDiagnosticsAsync(solution, analyzers, additionalDiagnostics, CompilerDiagnostics, verifier, cancellationToken);
        }

        /// <summary>
        /// Given an analyzer and a collection of documents to apply it to, run the analyzer and gather an array of
        /// diagnostics found. The returned diagnostics are then ordered by location in the source documents.
        /// </summary>
        /// <param name="solution">The <see cref="Solution"/> that the analyzer(s) will be run on.</param>
        /// <param name="analyzers">The analyzer to run on the documents.</param>
        /// <param name="additionalDiagnostics">Additional diagnostics reported for the solution, which need to be verified.</param>
        /// <param name="compilerDiagnostics">The behavior of compiler diagnostics in validation scenarios.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        protected async Task<ImmutableArray<(Project project, Diagnostic diagnostic)>> GetSortedDiagnosticsAsync(Solution solution, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<(Project project, Diagnostic diagnostic)> additionalDiagnostics, CompilerDiagnostics compilerDiagnostics, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (analyzers.IsEmpty)
            {
                analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyDiagnosticAnalyzer());
            }

            var diagnostics = ImmutableArray.CreateBuilder<(Project project, Diagnostic diagnostic)>();
            foreach (var project in solution.Projects)
            {
                var (compilation, generatorDiagnostics) = await GetProjectCompilationAsync(project, verifier, cancellationToken).ConfigureAwait(false);
                var analyzerOptions = GetAnalyzerOptions(project);
                var compilationWithAnalyzers = CreateCompilationWithAnalyzers(compilation, analyzers, analyzerOptions, cancellationToken);

                ImmutableArray<Diagnostic> allDiagnostics;
                if (AnalysisResultWrapper.WrappedType is not null)
                {
                    var compilerReportedDiagnostics = await GetCompilerDiagnosticsAsync(this, compilation, analyzers, analyzerOptions, cancellationToken).ConfigureAwait(false);
                    var analysisResult = await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var (analyzer, analyzerNonLocalDiagnostics) in analysisResult.CompilationDiagnostics)
                    {
                        foreach (var diagnostic in analyzerNonLocalDiagnostics)
                        {
                            NonLocalDiagnostics.Add(diagnostic, new object());
                        }
                    }

                    allDiagnostics = compilerReportedDiagnostics.AddRange(analysisResult.GetAllDiagnostics());
                }
                else
                {
                    allDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
                }

                diagnostics.AddRange(generatorDiagnostics.Select(diagnostic => (project, diagnostic)));
                diagnostics.AddRange(allDiagnostics.Where(diagnostic => !IsCompilerDiagnostic(diagnostic) || IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics)).Select(diagnostic => (project, diagnostic)));
            }

            diagnostics.AddRange(additionalDiagnostics);
            var filteredDiagnostics = FilterDiagnostics(diagnostics.ToImmutable());
            var results = SortDistinctDiagnostics(filteredDiagnostics);
            return results;

            static async Task<ImmutableArray<Diagnostic>> GetCompilerDiagnosticsAsync(AnalyzerTest<TVerifier> self, Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
            {
                if (!analyzers.Any(static analyzer => IsCompilerDiagnosticSuppressor(analyzer)))
                {
                    return compilation.GetDiagnostics(cancellationToken);
                }

                // Need to get the compiler diagnostics through a new CompilationWithAnalyzers instance to ensure
                // suppressions are applied.
                var compilerSuppressors = analyzers.Where(static analyzer => IsCompilerDiagnosticSuppressor(analyzer)).ToImmutableArray();
                var compilationWithAnalyzers = self.CreateCompilationWithAnalyzers(compilation, compilerSuppressors, analyzerOptions, cancellationToken);
                return await compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
            }

            static bool IsCompilerDiagnosticSuppressor(DiagnosticAnalyzer analyzer)
            {
                if (!DiagnosticSuppressorWrapper.IsInstance(analyzer))
                {
                    return false;
                }

                var wrapper = DiagnosticSuppressorWrapper.FromInstance(analyzer);
                foreach (var descriptor in wrapper.SupportedSuppressions)
                {
                    if (IsCompilerDiagnosticId(descriptor.SuppressedDiagnosticId))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private protected static bool IsNonLocalDiagnostic(Diagnostic diagnostic)
        {
            return NonLocalDiagnostics.TryGetValue(diagnostic, out _);
        }

        protected virtual async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var (finalProject, generatorDiagnostics) = await ApplySourceGeneratorsAsync(GetSourceGenerators().ToImmutableArray(), project, verifier, cancellationToken).ConfigureAwait(false);
            return ((await finalProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!, generatorDiagnostics);
        }

        private protected async Task<(Project project, ImmutableArray<Diagnostic> diagnostics)> ApplySourceGeneratorsAsync(ImmutableArray<Type> sourceGeneratorTypes, Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            verifier.True(compilation is { });

            if (sourceGeneratorTypes.IsEmpty)
            {
                return (project, ImmutableArray<Diagnostic>.Empty);
            }

            var sourceGenerators = ImmutableArray.CreateRange(sourceGeneratorTypes, static type => Activator.CreateInstance(type)!);
            var driver = CreateGeneratorDriver(project, sourceGenerators, verifier).RunGenerators(compilation, cancellationToken);
            var result = driver.GetRunResult();

            var updatedProject = project;
            foreach (var tree in result.GeneratedTrees)
            {
                var (fileName, folders) = GetNameAndFoldersFromSourceGeneratedFilePath(tree.FilePath);
                updatedProject = updatedProject.AddDocument(fileName, await tree.GetTextAsync(cancellationToken).ConfigureAwait(false), folders: folders, filePath: tree.FilePath).Project;
            }

            return (updatedProject, result.Diagnostics);
        }

        private protected static (string fileName, IEnumerable<string> folders) GetNameAndFoldersFromSourceGeneratedFilePath(string filePath)
        {
            // Source-generated files are always implicitly subpaths under the project root path.
            var folders = Path.GetDirectoryName(filePath)!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fileName = Path.GetFileName(filePath);
            return (fileName, folders);
        }

        private LightupGeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<object> sourceGenerators, IVerifier verifier)
        {
            var generatorDriverTypeName = project.Language switch
            {
                LanguageNames.CSharp => "Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver",
                LanguageNames.VisualBasic => "Microsoft.CodeAnalysis.VisualBasic.VisualBasicGeneratorDriver",
                _ => throw new NotSupportedException(),
            };

            var assembly = project.CompilationOptions.GetType().GetTypeInfo().Assembly;
            var generatorDriverType = assembly.GetType(generatorDriverTypeName);
            verifier.True(generatorDriverType is not null, "Failed to locate language-specific source generator driver");

            var isourceGeneratorType = typeof(CompilationOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.ISourceGenerator");
            verifier.True(isourceGeneratorType is not null, "Failed to locate ISourceGenerator interface");
            var ienumerableOfISourceGeneratorType = typeof(IEnumerable<>).MakeGenericType(isourceGeneratorType);
            var immutableArrayOfISourceGeneratorType = typeof(ImmutableArray<>).MakeGenericType(isourceGeneratorType);

            var analyzerConfigOptionsProviderType = typeof(CompilationOptions).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider");
            verifier.True(analyzerConfigOptionsProviderType is not null, "Failed to locate AnalyzerConfigOptionsProvider class");

            var createMethod = (from method in generatorDriverType.GetTypeInfo().GetMethods()
                                where method is { Name: "Create", IsPublic: true, IsStatic: true }
                                let parameterTypes = method.GetParameters().Select(static parameter => parameter.ParameterType)
                                where parameterTypes.SequenceEqual(new[] { ienumerableOfISourceGeneratorType, typeof(IEnumerable<AdditionalText>), project.ParseOptions.GetType(), analyzerConfigOptionsProviderType })
                                    || parameterTypes.SequenceEqual(new[] { immutableArrayOfISourceGeneratorType, typeof(ImmutableArray<AdditionalText>), project.ParseOptions.GetType(), analyzerConfigOptionsProviderType })
                                select method).SingleOrDefault();
            verifier.True(createMethod is not null, "Failed to locate factory method for diagnostic driver");

            var convertedSourceGeneratorsArray = Array.CreateInstance(isourceGeneratorType, sourceGenerators.Length);
            for (var i = 0; i < sourceGenerators.Length; i++)
            {
                if (isourceGeneratorType.IsAssignableFrom(sourceGenerators[i].GetType()))
                {
                    convertedSourceGeneratorsArray.SetValue(sourceGenerators[i], i);
                }
                else
                {
                    var iincrementalGeneratorType = isourceGeneratorType.GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.IIncrementalGenerator");
                    verifier.True(iincrementalGeneratorType?.IsAssignableFrom(sourceGenerators[i].GetType()) ?? false, $"'{sourceGenerators[i].GetType().FullName}' must implement '{iincrementalGeneratorType.FullName}' or '{isourceGeneratorType.FullName}'");
                    var asGeneratorMethod = (from method in isourceGeneratorType.GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.GeneratorExtensions")!.GetMethods()
                                             where method is { Name: "AsSourceGenerator", IsStatic: true, IsPublic: true }
                                             let parameterTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray()
                                             where parameterTypes.SequenceEqual(new[] { iincrementalGeneratorType })
                                             select method).SingleOrDefault();
                    convertedSourceGeneratorsArray.SetValue(asGeneratorMethod.Invoke(null, new[] { sourceGenerators[i] }), i);
                }
            }

            var createRangeMethod = (from method in typeof(ImmutableArray).GetTypeInfo().GetMethods()
                                     where method is { Name: nameof(ImmutableArray.CreateRange), IsStatic: true, IsPublic: true }
                                     let parameterTypes = method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray()
                                     where parameterTypes.Length == 1 && parameterTypes[0].GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                     select method).SingleOrDefault();
            var convertedSourceGenerators = createRangeMethod.MakeGenericMethod(isourceGeneratorType).Invoke(null, new object[] { convertedSourceGeneratorsArray });

            var analyzerOptions = project.AnalyzerOptions;
            var additionalFiles = analyzerOptions.AdditionalFiles;
            var analyzerConfigOptionsProvider = analyzerOptions.AnalyzerConfigOptionsProvider();
            verifier.True(analyzerConfigOptionsProvider is not null, "Failed to locate AnalyzerConfigOptionsProvider for project");

            var driver = createMethod.Invoke(null, new[] { convertedSourceGenerators, additionalFiles, project.ParseOptions, analyzerConfigOptionsProvider });
            verifier.True(driver is not null, "Failed to invoke factory method for diagnostic driver");

            return new LightupGeneratorDriver(driver);
        }

        private static bool IsCompilerDiagnostic(Diagnostic diagnostic)
        {
            return diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Compiler);
        }

        /// <summary>
        /// Determines if a compiler diagnostic should be included for diagnostic validation. The default implementation includes all diagnostics at a severity level indicated by <paramref name="compilerDiagnostics"/>.
        /// </summary>
        /// <param name="diagnostic">The compiler diagnostic.</param>
        /// <param name="compilerDiagnostics">The compiler diagnostic level in effect for the test.</param>
        /// <returns><see langword="true"/> to include the diagnostic for validation; otherwise, <see langword="false"/> to exclude a diagnostic.</returns>
        protected virtual bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics)
        {
            switch (compilerDiagnostics)
            {
                case CompilerDiagnostics.None:
                default:
                    return false;

                case CompilerDiagnostics.Errors:
                    return diagnostic.Severity >= DiagnosticSeverity.Error;

                case CompilerDiagnostics.Warnings:
                    return diagnostic.Severity >= DiagnosticSeverity.Warning;

                case CompilerDiagnostics.Suggestions:
                    return diagnostic.Severity >= DiagnosticSeverity.Info;

                case CompilerDiagnostics.All:
                    return true;
            }
        }

        /// <summary>
        /// Gets the effective analyzer options for a project. The default implementation returns
        /// <see cref="Project.AnalyzerOptions"/>.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The effective <see cref="AnalyzerOptions"/> for the project.</returns>
        protected virtual AnalyzerOptions GetAnalyzerOptions(Project project)
            => project.AnalyzerOptions;

        /// <summary>
        /// Combine a compilation with analyzers and options.
        /// </summary>
        /// <param name="compilation">The compilation the analyzers will be run on.</param>
        /// <param name="analyzers">The analyzer to run on the documents.</param>
        /// <param name="options">The <see cref="AnalyzerOptions"/> for the project.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="CompilationWithAnalyzers"/> object representing the provided compilation, analyzers, and options.</returns>
        protected virtual CompilationWithAnalyzers CreateCompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
            => CompilationWithAnalyzersExtensions.Create(compilation, analyzers, options, cancellationToken);

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a <see cref="Project"/> and return the
        /// solution.
        /// </summary>
        /// <param name="primaryProject">The primary project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A solution containing a project with the specified sources and additional files.</returns>
        private async Task<Solution> GetSolutionAsync(EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, IVerifier verifier, CancellationToken cancellationToken)
        {
            verifier.LanguageIsSupported(Language);

            var project = await CreateProjectAsync(primaryProject, additionalProjects, cancellationToken);
            var documents = project.Documents.ToArray();

            verifier.Equal(primaryProject.Sources.Length, documents.Length, "Amount of sources did not match amount of Documents created");

            return project.Solution;
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <remarks>
        /// <para>This method first creates a <see cref="Project"/> by calling <see cref="CreateProjectImplAsync"/>, and then
        /// applies compilation options to the project by calling <see cref="ApplyCompilationOptions"/>.</para>
        /// </remarks>
        /// <param name="primaryProject">The primary project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected async Task<Project> CreateProjectAsync(EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, CancellationToken cancellationToken)
        {
            var project = await CreateProjectImplAsync(primaryProject, additionalProjects, cancellationToken);
            return ApplyCompilationOptions(project);
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <param name="primaryProject">The primary project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected virtual async Task<Project> CreateProjectImplAsync(EvaluatedProjectState primaryProject, ImmutableArray<EvaluatedProjectState> additionalProjects, CancellationToken cancellationToken)
        {
            var fileNamePrefix = DefaultFilePathPrefix;
            var fileExt = DefaultFileExt;

            var projectIdMap = new Dictionary<string, ProjectId>();

            var projectId = ProjectId.CreateNewId(debugName: primaryProject.Name);
            projectIdMap.Add(primaryProject.Name, projectId);
            var solution = await CreateSolutionAsync(projectId, primaryProject, cancellationToken);

            foreach (var projectState in additionalProjects)
            {
                var additionalProjectId = ProjectId.CreateNewId(debugName: projectState.Name);
                projectIdMap.Add(projectState.Name, additionalProjectId);

                solution = solution.AddProject(additionalProjectId, projectState.Name, projectState.AssemblyName, projectState.Language);

                var referenceAssemblies = projectState.ReferenceAssemblies ?? ReferenceAssemblies;

                var xmlReferenceResolver = new TestXmlReferenceResolver();
                foreach (var xmlReference in XmlReferences)
                {
                    xmlReferenceResolver.XmlReferences.Add(xmlReference.Key, xmlReference.Value);
                }

                solution = solution.WithProjectCompilationOptions(
                    additionalProjectId,
                    solution.GetProject(additionalProjectId).CompilationOptions
                        .WithOutputKind(projectState.OutputKind)
                        .WithXmlReferenceResolver(xmlReferenceResolver)
                        .WithAssemblyIdentityComparer(referenceAssemblies.AssemblyIdentityComparer));

                solution = solution.WithProjectParseOptions(
                    additionalProjectId,
                    solution.GetProject(additionalProjectId).ParseOptions
                        .WithDocumentationMode(projectState.DocumentationMode));

                var metadataReferences = await referenceAssemblies.ResolveAsync(projectState.Language, cancellationToken);
                solution = solution.AddMetadataReferences(additionalProjectId, metadataReferences)
                    .AddMetadataReferences(additionalProjectId, projectState.AdditionalReferences);

                foreach (var (newFileName, source) in projectState.Sources)
                {
                    var documentId = DocumentId.CreateNewId(additionalProjectId, debugName: newFileName);
                    var (fileName, folders) = GetNameAndFoldersFromPath(projectState.DefaultPrefix, newFileName);
                    solution = solution.AddDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
                }

                foreach (var (newFileName, source) in projectState.AdditionalFiles)
                {
                    var documentId = DocumentId.CreateNewId(additionalProjectId, debugName: newFileName);
                    var (fileName, folders) = GetNameAndFoldersFromPath(projectState.DefaultPrefix, newFileName);
                    solution = solution.AddAdditionalDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
                }

                foreach (var (newFileName, source) in projectState.AnalyzerConfigFiles)
                {
                    var documentId = DocumentId.CreateNewId(additionalProjectId, debugName: newFileName);
                    var (fileName, folders) = GetNameAndFoldersFromPath(projectState.DefaultPrefix, newFileName);
                    solution = solution.AddAnalyzerConfigDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
                }
            }

            solution = solution.AddMetadataReferences(projectId, primaryProject.AdditionalReferences);

            foreach (var (newFileName, source) in primaryProject.Sources)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                var (fileName, folders) = GetNameAndFoldersFromPath(primaryProject.DefaultPrefix, newFileName);
                solution = solution.AddDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
            }

            foreach (var (newFileName, source) in primaryProject.AdditionalFiles)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                var (fileName, folders) = GetNameAndFoldersFromPath(primaryProject.DefaultPrefix, newFileName);
                solution = solution.AddAdditionalDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
            }

            foreach (var (newFileName, source) in primaryProject.AnalyzerConfigFiles)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                var (fileName, folders) = GetNameAndFoldersFromPath(primaryProject.DefaultPrefix, newFileName);
                solution = solution.AddAnalyzerConfigDocument(documentId, fileName, source, folders: folders, filePath: newFileName);
            }

            solution = AddProjectReferences(solution, projectId, primaryProject.AdditionalProjectReferences.Select(name => projectIdMap[name]));
            foreach (var projectState in additionalProjects)
            {
                solution = AddProjectReferences(solution, projectIdMap[projectState.Name], projectState.AdditionalProjectReferences.Select(name => projectIdMap[name]));
            }

            foreach (var transform in SolutionTransforms)
            {
                solution = transform(solution, projectId);
            }

            return solution.GetProject(projectId);

            // Local functions
            static Solution AddProjectReferences(Solution solution, ProjectId sourceProject, IEnumerable<ProjectId> targetProjects)
            {
                return solution.AddProjectReferences(sourceProject, targetProjects.Select(id => new ProjectReference(id)));
            }
        }

        protected (string fileName, IEnumerable<string> folders) GetNameAndFoldersFromPath(string projectPathPrefix, string path)
        {
            // Normalize to platform path separators for simplicity later on
            var normalizedPath = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var normalizedDefaultPathPrefix = projectPathPrefix.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (!Path.IsPathRooted(normalizedDefaultPathPrefix))
            {
                // If our default path isn't rooted, then we assume that we don't have any rooted paths
                // and just use the file name
                return (Path.GetFileName(normalizedPath), folders: new string[0]);
            }

            // | Default path | Project root path |
            // |--------------|-------------------|
            // |   /0/Temp    |  /0/              |
            // |   /0/        |  /0/              |
            var projectRootPath = Path.GetFileName(normalizedDefaultPathPrefix) == string.Empty
                ? normalizedDefaultPathPrefix
                : (Path.GetDirectoryName(normalizedDefaultPathPrefix) + Path.DirectorySeparatorChar);

            // If the default path prefix is a directory name (ending with a directory separator)
            // then treat it as the project root.
            if (!normalizedPath.StartsWith(projectRootPath))
            {
                // If our path doesn't start with the default path prefix, then the file is out of tree.
                if (Path.IsPathRooted(normalizedPath))
                {
                    // If the user provides a rooted path as the file name, just use that as-is.
                    return (path, folders: new string[0]);
                }

                // Otherwise, to match VS behavior we will report no folders and only the file name.
                return (Path.GetFileName(normalizedPath), folders: new string[0]);
            }

            var subpath = normalizedPath.Substring(projectRootPath.Length);

            var fileName = Path.GetFileName(subpath);
            if (Path.GetDirectoryName(subpath) == string.Empty)
            {
                return (fileName, folders: new string[0]);
            }

            var folders = Path.GetDirectoryName(subpath)!.Split(Path.DirectorySeparatorChar);
            return (fileName, folders);
        }

        /// <summary>
        /// Creates a solution that will be used as parent for the sources that need to be checked.
        /// </summary>
        /// <param name="projectId">The project identifier to use.</param>
        /// <param name="projectState">The primary project.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The created solution.</returns>
        protected virtual async Task<Solution> CreateSolutionAsync(ProjectId projectId, EvaluatedProjectState projectState, CancellationToken cancellationToken)
        {
            var referenceAssemblies = projectState.ReferenceAssemblies ?? ReferenceAssemblies;

            var compilationOptions = CreateCompilationOptions()
                .WithOutputKind(projectState.OutputKind);

            var xmlReferenceResolver = new TestXmlReferenceResolver();
            foreach (var xmlReference in XmlReferences)
            {
                xmlReferenceResolver.XmlReferences.Add(xmlReference.Key, xmlReference.Value);
            }

            compilationOptions = compilationOptions
                .WithXmlReferenceResolver(xmlReferenceResolver)
                .WithAssemblyIdentityComparer(referenceAssemblies.AssemblyIdentityComparer);

            var parseOptions = CreateParseOptions()
                .WithDocumentationMode(projectState.DocumentationMode);

            var workspace = CreateWorkspace();
            foreach (var transform in OptionsTransforms)
            {
                workspace.Options = transform(workspace.Options);
            }

            var solution = workspace
                .CurrentSolution
                .AddProject(projectId, projectState.Name, projectState.Name, projectState.Language)
                .WithProjectCompilationOptions(projectId, compilationOptions)
                .WithProjectParseOptions(projectId, parseOptions);

            var metadataReferences = await referenceAssemblies.ResolveAsync(projectState.Language, cancellationToken);
            solution = solution.AddMetadataReferences(projectId, metadataReferences);

            return solution;
        }

        /// <summary>
        /// Applies compilation options to a project.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation configures the project by enabling all supported diagnostics of analyzers
        /// included in <see cref="GetDiagnosticAnalyzers"/> as well as <c>AD0001</c>. After configuring these
        /// diagnostics, any diagnostic IDs indicated in <see cref="DisabledDiagnostics"/> are explicitly suppressed
        /// using <see cref="ReportDiagnostic.Suppress"/>.</para>
        /// </remarks>
        /// <param name="project">The project.</param>
        /// <returns>The modified project.</returns>
        protected virtual Project ApplyCompilationOptions(Project project)
        {
            var analyzers = GetDiagnosticAnalyzers();

            var supportedDiagnosticsSpecificOptions = new Dictionary<string, ReportDiagnostic>();
            foreach (var analyzer in analyzers)
            {
                foreach (var diagnostic in analyzer.SupportedDiagnostics)
                {
                    // make sure the analyzers we are testing are enabled
                    if (diagnostic.IsEnabledByDefault)
                    {
                        supportedDiagnosticsSpecificOptions[diagnostic.Id] = ReportDiagnostic.Default;
                    }
                    else
                    {
                        supportedDiagnosticsSpecificOptions[diagnostic.Id] = diagnostic.DefaultSeverity switch
                        {
                            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
                            DiagnosticSeverity.Info => ReportDiagnostic.Info,
                            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
                            DiagnosticSeverity.Error => ReportDiagnostic.Error,
                            _ => throw new InvalidOperationException(),
                        };
                    }
                }
            }

            // Report exceptions during the analysis process as errors
            supportedDiagnosticsSpecificOptions.Add("AD0001", ReportDiagnostic.Error);

            foreach (var id in DisabledDiagnostics)
            {
                supportedDiagnosticsSpecificOptions[id] = ReportDiagnostic.Suppress;
            }

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = supportedDiagnosticsSpecificOptions.ToImmutableDictionary().SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

            var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
            return solution.GetProject(project.Id);
        }

        public Workspace CreateWorkspace()
        {
            var workspace = CreateWorkspaceImpl();
            _workspaces.Add(workspace);
            return workspace;
        }

        protected virtual Workspace CreateWorkspaceImpl()
        {
            var exportProvider = ExportProviderFactory.Value.CreateExportProvider();
            var host = MefHostServices.Create(exportProvider.AsCompositionContext());
            return new AdhocWorkspace(host);
        }

        protected abstract CompilationOptions CreateCompilationOptions();

        protected abstract ParseOptions CreateParseOptions();

        /// <summary>
        /// Filter <see cref="Diagnostic"/>s to only include items of interest to testing. By default, this includes all
        /// unsuppressed diagnostics, and all diagnostics suppressed by a
        /// <see cref="T:Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor"/>.
        /// </summary>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be filtered.</param>
        /// <returns>A collection containing the input <paramref name="diagnostics"/>, filtered to only include
        /// diagnostics relevant for testing.</returns>
        protected virtual ImmutableArray<(Project project, Diagnostic diagnostic)> FilterDiagnostics(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
        {
            return diagnostics
                .Where(d => !d.diagnostic.IsSuppressed() || d.diagnostic.ProgrammaticSuppressionInfo() != null)
                .ToImmutableArray();
        }

        /// <summary>
        /// Sort <see cref="Diagnostic"/>s by location in source document.
        /// </summary>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be sorted.</param>
        /// <returns>A collection containing the input <paramref name="diagnostics"/>, sorted by
        /// <see cref="Diagnostic.Location"/> and <see cref="Diagnostic.Id"/>.</returns>
        protected virtual ImmutableArray<(Project project, Diagnostic diagnostic)> SortDistinctDiagnostics(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
        {
            return diagnostics
                .OrderBy(d => d.diagnostic.Location.GetLineSpan().Path, StringComparer.Ordinal)
                .ThenBy(d => d.diagnostic.Location.SourceSpan.Start)
                .ThenBy(d => d.diagnostic.Location.SourceSpan.End)
                .ThenBy(d => d.diagnostic.Id)
                .ThenBy(d => d.diagnostic.Arguments(), LexicographicComparer.Instance).ToImmutableArray();
        }

        /// <summary>
        /// Gets the analyzers being tested.
        /// </summary>
        /// <returns>
        /// New instances of all the analyzers being tested.
        /// </returns>
        protected abstract IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers();

        /// <summary>
        /// Gets the source generators to apply to the projects under test.
        /// </summary>
        /// <returns>
        /// The types of all source generators to apply to projects in the test. These types will be instantiated by the
        /// test framework to obtain the source generator instances.
        /// </returns>
        protected virtual IEnumerable<Type> GetSourceGenerators()
            => Enumerable.Empty<Type>();

        private sealed class LexicographicComparer : IComparer<IEnumerable<object?>?>
        {
            public static LexicographicComparer Instance { get; } = new LexicographicComparer();

            public int Compare(IEnumerable<object?>? x, IEnumerable<object?>? y)
            {
                if (x is null)
                {
                    return y is null ? 0 : -1;
                }
                else if (y is null)
                {
                    return 1;
                }

                using var xe = x.GetEnumerator();
                using var ye = y.GetEnumerator();

                while (xe.MoveNext())
                {
                    if (!ye.MoveNext())
                    {
                        // y has fewer elements
                        return 1;
                    }

                    IComparer elementComparer = Comparer<object>.Default;
                    if (xe.Current is string && ye.Current is string)
                    {
                        // Avoid culture-sensitive string comparisons
                        elementComparer = StringComparer.Ordinal;
                    }

                    try
                    {
                        var elementComparison = elementComparer.Compare(xe.Current, ye.Current);
                        if (elementComparison == 0)
                        {
                            continue;
                        }

                        return elementComparison;
                    }
                    catch (ArgumentException)
                    {
                        // The arguments are not directly comparable, so convert the values to strings and try again
                        var elementComparison = string.CompareOrdinal(xe.Current?.ToString(), ye.Current?.ToString());
                        if (elementComparison == 0)
                        {
                            continue;
                        }

                        return elementComparison;
                    }
                }

                if (ye.MoveNext())
                {
                    // x has fewer elements
                    return -1;
                }

                return 0;
            }
        }

        private sealed class LightupGeneratorDriver
        {
            private readonly object _instance;

            public LightupGeneratorDriver(object instance)
            {
                _instance = instance;
            }

            internal LightupGeneratorDriver RunGenerators(Compilation compilation, CancellationToken cancellationToken)
            {
                var runGeneratorsMethod = (from method in _instance.GetType().GetTypeInfo().GetMethods()
                                           where method is { Name: nameof(RunGenerators), IsStatic: false, IsPublic: true }
                                           let parameterTypes = method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray()
                                           where parameterTypes.SequenceEqual(new[] { typeof(Compilation), typeof(CancellationToken) })
                                           select method).Single();

                var transformedDriver = runGeneratorsMethod.Invoke(_instance, new object[] { compilation, cancellationToken })!;
                return new LightupGeneratorDriver(transformedDriver);
            }

            internal LightupGeneratorDriverRunResult GetRunResult()
            {
                var getRunResultMethod = (from method in _instance.GetType().GetTypeInfo().GetMethods()
                                          where method is { Name: nameof(GetRunResult), IsStatic: false, IsPublic: true }
                                          where method.GetParameters().Length == 0
                                          select method).Single();

                var runResult = getRunResultMethod.Invoke(_instance, new object[0])!;
                return new LightupGeneratorDriverRunResult(runResult);
            }
        }

        private sealed class LightupGeneratorDriverRunResult
        {
            private static readonly Type? s_generatorDriverRunResultType = typeof(Compilation).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.GeneratorDriverRunResult");

            private static readonly Func<object, ImmutableArray<SyntaxTree>> s_generatedTrees =
                LightupHelpers.CreatePropertyAccessor<object, ImmutableArray<SyntaxTree>>(
                    s_generatorDriverRunResultType,
                    nameof(GeneratedTrees),
                    defaultValue: ImmutableArray<SyntaxTree>.Empty);

            private static readonly Func<object, ImmutableArray<Diagnostic>> s_diagnostics =
                LightupHelpers.CreatePropertyAccessor<object, ImmutableArray<Diagnostic>>(
                    s_generatorDriverRunResultType,
                    nameof(Diagnostics),
                    defaultValue: ImmutableArray<Diagnostic>.Empty);

            private readonly object _instance;

            public LightupGeneratorDriverRunResult(object instance)
            {
                _instance = instance;
            }

            public ImmutableArray<SyntaxTree> GeneratedTrees => s_generatedTrees(_instance);

            public ImmutableArray<Diagnostic> Diagnostics => s_diagnostics(_instance);
        }
    }
}
