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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using IComparer = System.Collections.IComparer;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        private static readonly Lazy<IExportProviderFactory> ExportProviderFactory;

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

        protected static TVerifier Verify { get; } = new TVerifier();

        protected virtual string DefaultFilePathPrefix { get; } = "Test";

        protected virtual string DefaultTestProjectName { get; } = "TestProject";

        protected virtual string DefaultFilePath => DefaultFilePathPrefix + 0 + "." + DefaultFileExt;

        protected abstract string DefaultFileExt { get; }

        protected AnalyzerTest()
        {
            TestState = new SolutionState(DefaultFilePathPrefix, DefaultFileExt);
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

        public ReferenceAssemblies ReferenceAssemblies { get; set; } = ReferenceAssemblies.Default;

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

        public virtual async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty($"{nameof(TestState)}.{nameof(SolutionState.Sources)}", TestState.Sources);

            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = ImmutableArray<string>.Empty;
            var testState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics).WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            await VerifyDiagnosticsAsync(testState.Sources.ToArray(), testState.AdditionalFiles.ToArray(), testState.AdditionalProjects.ToArray(), testState.AdditionalReferences.ToArray(), testState.ExpectedDiagnostics.ToArray(), Verify, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// General method that gets a collection of actual <see cref="Diagnostic"/>s found in the source after the
        /// analyzer is run, then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on.</param>
        /// <param name="additionalFiles">Additional documents to include in the project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="additionalMetadataReferences">Additional metadata references to include in the project.</param>
        /// <param name="expected">A collection of <see cref="DiagnosticResult"/>s that should appear after the analyzer
        /// is run on the sources.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyDiagnosticsAsync((string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            var analyzers = GetDiagnosticAnalyzers().ToImmutableArray();
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(sources, additionalFiles, additionalProjects, additionalMetadataReferences, analyzers, verifier, cancellationToken).ConfigureAwait(false), analyzers, expected, verifier);
            await VerifyGeneratedCodeDiagnosticsAsync(analyzers, sources, additionalFiles, additionalProjects, additionalMetadataReferences, expected, verifier, cancellationToken).ConfigureAwait(false);
            await VerifySuppressionDiagnosticsAsync(analyzers, sources, additionalFiles, additionalProjects, additionalMetadataReferences, expected, verifier, cancellationToken).ConfigureAwait(false);
        }

        private async Task VerifyGeneratedCodeDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, (string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (TestBehaviors.HasFlag(TestBehaviors.SkipGeneratedCodeCheck))
            {
                return;
            }

            if (!expected.Any(x => IsSubjectToExclusion(x, sources)))
            {
                return;
            }

            // Diagnostics reported by the compiler and analyzer diagnostics which don't have a location will
            // still be reported. We also insert a new line at the beginning so we have to move all diagnostic
            // locations which have a specific position down by one line.
            var expectedResults = expected
                .Where(x => !IsSubjectToExclusion(x, sources))
                .Select(x => IsInSourceFile(x, sources) ? x.WithLineOffset(1) : x)
                .ToArray();

            var generatedCodeVerifier = verifier.PushContext("Verifying exclusions in <auto-generated> code");
            var commentPrefix = Language == LanguageNames.CSharp ? "//" : "'";
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(sources.Select(x => (x.filename, x.content.Replace(new TextSpan(0, 0), $" {commentPrefix} <auto-generated>\r\n"))).ToArray(), additionalFiles, additionalProjects, additionalMetadataReferences, analyzers, generatedCodeVerifier, cancellationToken).ConfigureAwait(false), analyzers, expectedResults, generatedCodeVerifier);
        }

        private async Task VerifySuppressionDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, (string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, DiagnosticResult[] expected, IVerifier verifier, CancellationToken cancellationToken)
        {
            if (TestBehaviors.HasFlag(TestBehaviors.SkipSuppressionCheck))
            {
                return;
            }

            if (!expected.Any(x => IsSubjectToExclusion(x, sources)))
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
            var suppressedDiagnostics = expected.Where(x => IsSubjectToExclusion(x, sources)).Select(x => x.Id).Distinct();
            var suppression = prefix + " " + string.Join(", ", suppressedDiagnostics);
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(sources.Select(x => (x.filename, x.content.Replace(new TextSpan(0, 0), $"{suppression}\r\n"))).ToArray(), additionalFiles, additionalProjects, additionalMetadataReferences, analyzers, suppressionVerifier, cancellationToken).ConfigureAwait(false), analyzers, expectedResults, suppressionVerifier);
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
        private void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult[] expectedResults, IVerifier verifier)
        {
            var matchedDiagnostics = MatchDiagnostics(actualResults.ToArray(), expectedResults);
            verifier.Equal(actualResults.Count(), matchedDiagnostics.Count(x => x.actual is object), $"{nameof(MatchDiagnostics)} failed to include all actual diagnostics in the result");
            verifier.Equal(expectedResults.Length, matchedDiagnostics.Count(x => x.expected is object), $"{nameof(MatchDiagnostics)} failed to include all expected diagnostics in the result");

            actualResults = matchedDiagnostics.Select(x => x.actual).WhereNotNull();
            expectedResults = matchedDiagnostics.Where(x => x.expected is object).Select(x => x.expected.GetValueOrDefault()).ToArray();

            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count();

            var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, actualResults.ToArray()) : "    NONE.";
            var message = $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n";
            verifier.Equal(expectedCount, actualCount, message);

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (!expected.HasLocation)
                {
                    verifier.Equal(Location.None, actual.Location, $"Expected:\r\nA project diagnostic with No location\r\nActual:\r\n{FormatDiagnostics(analyzers, actual)}");
                }
                else
                {
                    VerifyDiagnosticLocation(analyzers, actual, actual.Location, expected.Spans[0], verifier);
                    if (!expected.Spans[0].Options.HasFlag(DiagnosticLocationOptions.IgnoreAdditionalLocations))
                    {
                        var additionalLocations = actual.AdditionalLocations.ToArray();

                        verifier.Equal(
                            expected.Spans.Length - 1,
                            additionalLocations.Length,
                            $"Expected {expected.Spans.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                        for (var j = 0; j < additionalLocations.Length; ++j)
                        {
                            VerifyDiagnosticLocation(analyzers, actual, additionalLocations[j], expected.Spans[j + 1], verifier);
                        }
                    }
                }

                verifier.Equal(
                    expected.Id,
                    actual.Id,
                    $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                verifier.Equal(
                    expected.Severity,
                    actual.Severity,
                    $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                if (expected.Message != null)
                {
                    verifier.Equal(expected.Message, actual.GetMessage(), $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");
                }
                else if (expected.MessageArguments?.Length > 0)
                {
                    verifier.SequenceEqual(
                        expected.MessageArguments.Select(argument => argument?.ToString() ?? string.Empty),
                        GetArguments(actual).Select(argument => argument?.ToString() ?? string.Empty),
                        StringComparer.Ordinal,
                        $"Expected diagnostic message arguments to match\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");
                }
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
        ///
        /// <para>If no exact match is found (all actual diagnostics are matched to an expected diagnostic without
        /// errors), this method is <em>allowed</em> to attempt fall-back matching using a strategy intended to minimize
        /// the total number of mismatched pairs.</para>
        /// </list>
        /// </returns>
        private ImmutableArray<(Diagnostic? actual, DiagnosticResult? expected)> MatchDiagnostics(Diagnostic[] actualResults, DiagnosticResult[] expectedResults)
        {
            expectedResults = expectedResults.ToOrderedArray();

            // Initialize the best match to a trivial result where everything is unmatched. This will be updated if/when
            // better matches are found.
            var bestMatchCount = actualResults.Length + expectedResults.Length;
            var bestMatch = actualResults.Select(result => ((Diagnostic?)result, default(DiagnosticResult?))).Concat(expectedResults.Select(result => (default(Diagnostic?), (DiagnosticResult?)result))).ToImmutableArray();

            var builder = ImmutableArray.CreateBuilder<(Diagnostic? actual, DiagnosticResult? expected)>();
            var usedExpected = new bool[expectedResults.Length];
            _ = RecursiveMatch(0, 0, 0, usedExpected);

            return bestMatch;

            // Match items using recursive backtracking. Returns the distance the best match under this path is from an
            // ideal result of 0 (1-1 matching of actual and expected results). Currently the distance is calculated as
            // the number of unmatched items.
            int RecursiveMatch(int firstActualIndex, int firstExpectedIndex, int unmatchedActualResults, bool[] usedExpected)
            {
                var matchedOnEntry = firstActualIndex - unmatchedActualResults;
                var bestPossibleUnmatchedExpected = Math.Max(0, expectedResults.Length - matchedOnEntry - (actualResults.Length - unmatchedActualResults));
                var bestPossible = unmatchedActualResults + bestPossibleUnmatchedExpected;

                if (firstActualIndex == actualResults.Length)
                {
                    // We reached the end of the actual diagnostics. Any remaning unmatched expected diagnostics should
                    // be added to the end. If this path produced a better result than the best known path so far,
                    // update the best match to this one.
                    var totalUnmatched = unmatchedActualResults + (expectedResults.Length - matchedOnEntry);

                    // Avoid manipulating the builder if we know the current path is no better than the previous best.
                    if (totalUnmatched < bestMatchCount)
                    {
                        var addedCount = 0;

                        // Add the remaining unmatched expected diagnostics
                        for (var i = firstExpectedIndex; i < expectedResults.Length; i++)
                        {
                            if (!usedExpected[i])
                            {
                                addedCount++;
                                builder.Add((null, (DiagnosticResult?)expectedResults[i]));
                            }
                        }

                        bestMatchCount = totalUnmatched;
                        bestMatch = builder.ToImmutable();

                        for (var i = 0; i < addedCount; i++)
                        {
                            builder.RemoveAt(builder.Count - 1);
                        }
                    }

                    return totalUnmatched;
                }

                var currentBest = actualResults.Length + expectedResults.Length - (2 * matchedOnEntry);
                for (var i = firstExpectedIndex; i < expectedResults.Length; i++)
                {
                    if (usedExpected[i])
                    {
                        continue;
                    }

                    if (!IsMatch(actualResults[firstActualIndex], expectedResults[i]))
                    {
                        continue;
                    }

                    try
                    {
                        usedExpected[i] = true;
                        builder.Add((actualResults[firstActualIndex], expectedResults[i]));
                        var bestResultWithCurrentMatch = RecursiveMatch(firstActualIndex + 1, i == firstExpectedIndex ? firstExpectedIndex + 1 : firstExpectedIndex, unmatchedActualResults, usedExpected);
                        currentBest = Math.Min(bestResultWithCurrentMatch, currentBest);
                        if (currentBest == bestPossible)
                        {
                            // Return immediately if we know the current actual result cannot be paired with a different
                            // expected result to produce a better match.
                            return bestPossible;
                        }
                    }
                    finally
                    {
                        usedExpected[i] = false;
                        builder.RemoveAt(builder.Count - 1);
                    }
                }

                if (currentBest > unmatchedActualResults)
                {
                    // We might be able to improve the results by leaving the current actual diagnostic unmatched
                    try
                    {
                        builder.Add((actualResults[firstActualIndex], null));
                        var bestResultWithCurrentUnmatched = RecursiveMatch(firstActualIndex + 1, firstExpectedIndex, unmatchedActualResults + 1, usedExpected);
                        return Math.Min(bestResultWithCurrentUnmatched, currentBest);
                    }
                    finally
                    {
                        builder.RemoveAt(builder.Count - 1);
                    }
                }

                Debug.Assert(currentBest == unmatchedActualResults, $"Assertion failure: {currentBest} == {unmatchedActualResults}");
                return currentBest;
            }

            static bool IsMatch(Diagnostic diagnostic, DiagnosticResult diagnosticResult)
            {
                return IsLocationMatch(diagnostic, diagnosticResult)
                    && diagnostic.Id == diagnosticResult.Id
                    && diagnostic.Severity == diagnosticResult.Severity
                    && IsMessageMatch(diagnostic, diagnosticResult);
            }

            static bool IsLocationMatch(Diagnostic diagnostic, DiagnosticResult diagnosticResult)
            {
                if (!diagnosticResult.HasLocation)
                {
                    return Equals(Location.None, diagnostic.Location);
                }
                else
                {
                    if (!IsLocationMatch2(diagnostic.Location, diagnosticResult.Spans[0]))
                    {
                        return false;
                    }

                    if (diagnosticResult.Spans[0].Options.HasFlag(DiagnosticLocationOptions.IgnoreAdditionalLocations))
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
                        if (!IsLocationMatch2(additionalLocations[i], diagnosticResult.Spans[i + 1]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            static bool IsLocationMatch2(Location actual, DiagnosticLocation expected)
            {
                var actualSpan = actual.GetLineSpan();

                var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains("Test0.") == true && expected.Span.Path.Contains("Test."));
                if (!assert)
                {
                    // Expected diagnostic to be in file "{expected.Span.Path}" was actually in file "{actualSpan.Path}"
                    return false;
                }

                if (actualSpan.StartLinePosition != expected.Span.StartLinePosition)
                {
                    return false;
                }

                if (!expected.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength)
                    && actualSpan.EndLinePosition != expected.Span.EndLinePosition)
                {
                    return false;
                }

                return true;
            }

            static bool IsMessageMatch(Diagnostic actual, DiagnosticResult expected)
            {
                if (expected.Message is null)
                {
                    if (expected.MessageArguments?.Length > 0)
                    {
                        var actualArguments = GetArguments(actual).Select(argument => argument?.ToString() ?? string.Empty);
                        var expectedArguments = expected.MessageArguments.Select(argument => argument?.ToString() ?? string.Empty);
                        return actualArguments.SequenceEqual(expectedArguments);
                    }

                    return true;
                }

                return string.Equals(expected.Message, actual.GetMessage());
            }
        }

        /// <summary>
        /// Helper method to <see cref="VerifyDiagnosticResults"/> that checks the location of a
        /// <see cref="Diagnostic"/> and compares it with the location described by a
        /// <see cref="FileLinePositionSpan"/>.
        /// </summary>
        /// <param name="analyzers">The analyzer that have been run on the sources.</param>
        /// <param name="diagnostic">The diagnostic that was found in the code.</param>
        /// <param name="actual">The location of the diagnostic found in the code.</param>
        /// <param name="expected">The <see cref="FileLinePositionSpan"/> describing the expected location of the
        /// diagnostic.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        private void VerifyDiagnosticLocation(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, Location actual, DiagnosticLocation expected, IVerifier verifier)
        {
            var actualSpan = actual.GetLineSpan();

            var assert = actualSpan.Path == expected.Span.Path || (actualSpan.Path?.Contains("Test0.") == true && expected.Span.Path.Contains("Test."));
            verifier.True(assert, $"Expected diagnostic to be in file \"{expected.Span.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");

            VerifyLinePosition(analyzers, diagnostic, actualSpan.StartLinePosition, expected.Span.StartLinePosition, "start", verifier);
            if (!expected.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
            {
                VerifyLinePosition(analyzers, diagnostic, actualSpan.EndLinePosition, expected.Span.EndLinePosition, "end", verifier);
            }
        }

        private void VerifyLinePosition(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, LinePosition actualLinePosition, LinePosition expectedLinePosition, string positionText, IVerifier verifier)
        {
            verifier.Equal(
                expectedLinePosition.Line,
                actualLinePosition.Line,
                $"Expected diagnostic to {positionText} on line \"{expectedLinePosition.Line + 1}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");

            verifier.Equal(
                expectedLinePosition.Character,
                actualLinePosition.Character,
                $"Expected diagnostic to {positionText} at column \"{expectedLinePosition.Character + 1}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");
        }

        /// <summary>
        /// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
        /// </summary>
        /// <param name="analyzers">The analyzers that this verifier tests.</param>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be formatted.</param>
        /// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
        private static string FormatDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, params Diagnostic[] diagnostics)
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
                    var rule = applicableAnalyzer.SupportedDiagnostics.Length == 1 ? string.Empty : $"{analyzerType.Name}.{diagnosticsId}";

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
                else if (!location.IsInSource)
                {
                    var lineSpan = diagnostics[i].Location.GetLineSpan();
                    builder.Append($".WithSpan(\"{lineSpan.Path}\", {lineSpan.StartLinePosition.Line + 1}, {lineSpan.StartLinePosition.Character + 1}, {lineSpan.EndLinePosition.Line + 1}, {lineSpan.EndLinePosition.Character + 1})");
                }
                else
                {
                    var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;
                    var endLinePosition = diagnostics[i].Location.GetLineSpan().EndLinePosition;

                    builder.Append($".WithSpan({linePosition.Line + 1}, {linePosition.Character + 1}, {endLinePosition.Line + 1}, {endLinePosition.Character + 1})");
                }

                var arguments = GetArguments(diagnostics[i]);
                if (arguments.Count > 0)
                {
                    builder.Append($".{nameof(DiagnosticResult.WithArguments)}(");
                    builder.Append(string.Join(", ", arguments.Select(a => "\"" + a?.ToString() + "\"")));
                    builder.Append(")");
                }

                if (i != diagnostics.Length - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static bool IsSubjectToExclusion(DiagnosticResult result, (string filename, SourceText content)[] sources)
        {
            if (result.Id.StartsWith("CS", StringComparison.Ordinal)
                || result.Id.StartsWith("BC", StringComparison.Ordinal))
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

            return true;
        }

        private static bool IsSuppressible(ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult result, (string filename, SourceText content)[] sources)
        {
            if (!IsSubjectToExclusion(result, sources))
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
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="additionalFiles">Additional documents to include in the project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="additionalMetadataReferences">Additional metadata references to include in the project.</param>
        /// <param name="analyzers">The analyzers to be run on the sources.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        private async Task<ImmutableArray<Diagnostic>> GetSortedDiagnosticsAsync((string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, ImmutableArray<DiagnosticAnalyzer> analyzers, IVerifier verifier, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(sources, additionalFiles, additionalProjects, additionalMetadataReferences, verifier, cancellationToken);
            return await GetSortedDiagnosticsAsync(solution, analyzers, CompilerDiagnostics, cancellationToken);
        }

        /// <summary>
        /// Given an analyzer and a collection of documents to apply it to, run the analyzer and gather an array of
        /// diagnostics found. The returned diagnostics are then ordered by location in the source documents.
        /// </summary>
        /// <param name="solution">The <see cref="Solution"/> that the analyzer(s) will be run on.</param>
        /// <param name="analyzers">The analyzer to run on the documents.</param>
        /// <param name="compilerDiagnostics">The behavior of compiler diagnostics in validation scenarios.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        protected async Task<ImmutableArray<Diagnostic>> GetSortedDiagnosticsAsync(Solution solution, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilerDiagnostics compilerDiagnostics, CancellationToken cancellationToken)
        {
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, GetAnalyzerOptions(project), cancellationToken);
                var allDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);

                diagnostics.AddRange(allDiagnostics.Where(diagnostic => !IsCompilerDiagnostic(diagnostic) || IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics)));
            }

            var results = SortDistinctDiagnostics(diagnostics);
            return results.ToImmutableArray();
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
        /// Given an array of strings as sources and a language, turn them into a <see cref="Project"/> and return the
        /// solution.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="additionalFiles">Additional documents to include in the project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="additionalMetadataReferences">Additional metadata references to include in the project.</param>
        /// <param name="verifier">The verifier to use for test assertions.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A solution containing a project with the specified sources and additional files.</returns>
        private async Task<Solution> GetSolutionAsync((string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, IVerifier verifier, CancellationToken cancellationToken)
        {
            verifier.LanguageIsSupported(Language);

            var project = await CreateProjectAsync(sources, additionalFiles, additionalProjects, additionalMetadataReferences, Language, cancellationToken);
            var documents = project.Documents.ToArray();

            verifier.Equal(sources.Length, documents.Length, "Amount of sources did not match amount of Documents created");

            return project.Solution;
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <remarks>
        /// <para>This method first creates a <see cref="Project"/> by calling <see cref="CreateProjectImplAsync"/>, and then
        /// applies compilation options to the project by calling <see cref="ApplyCompilationOptions"/>.</para>
        /// </remarks>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="additionalFiles">Additional documents to include in the project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="additionalMetadataReferences">Additional metadata references to include in the project.</param>
        /// <param name="language">The language the source classes are in. Values may be taken from the
        /// <see cref="LanguageNames"/> class.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected async Task<Project> CreateProjectAsync((string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, string language, CancellationToken cancellationToken)
        {
            var project = await CreateProjectImplAsync(sources, additionalFiles, additionalProjects, additionalMetadataReferences, language, cancellationToken);
            return ApplyCompilationOptions(project);
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="additionalFiles">Additional documents to include in the project.</param>
        /// <param name="additionalProjects">Additional projects to include in the solution.</param>
        /// <param name="additionalMetadataReferences">Additional metadata references to include in the project.</param>
        /// <param name="language">The language the source classes are in. Values may be taken from the
        /// <see cref="LanguageNames"/> class.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected virtual async Task<Project> CreateProjectImplAsync((string filename, SourceText content)[] sources, (string filename, SourceText content)[] additionalFiles, ProjectState[] additionalProjects, MetadataReference[] additionalMetadataReferences, string language, CancellationToken cancellationToken)
        {
            var fileNamePrefix = DefaultFilePathPrefix;
            var fileExt = DefaultFileExt;

            var projectId = ProjectId.CreateNewId(debugName: DefaultTestProjectName);
            var solution = await CreateSolutionAsync(projectId, language, cancellationToken);

            foreach (var projectState in additionalProjects)
            {
                var additionalProjectId = ProjectId.CreateNewId(debugName: projectState.Name);
                solution = solution.AddProject(additionalProjectId, projectState.Name, projectState.AssemblyName, projectState.Language);

                for (var i = 0; i < projectState.Sources.Count; i++)
                {
                    (var newFileName, var source) = projectState.Sources[i];
                    var documentId = DocumentId.CreateNewId(additionalProjectId, debugName: newFileName);
                    solution = solution.AddDocument(documentId, newFileName, source);
                }

                solution = solution.AddProjectReference(projectId, new ProjectReference(additionalProjectId));
            }

            solution = solution.AddMetadataReferences(projectId, additionalMetadataReferences);

            for (var i = 0; i < sources.Length; i++)
            {
                (var newFileName, var source) = sources[i];
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, source);
            }

            for (var i = 0; i < additionalFiles.Length; i++)
            {
                (var newFileName, var source) = additionalFiles[i];
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddAdditionalDocument(documentId, newFileName, source);
            }

            foreach (var transform in SolutionTransforms)
            {
                solution = transform(solution, projectId);
            }

            return solution.GetProject(projectId);
        }

        /// <summary>
        /// Creates a solution that will be used as parent for the sources that need to be checked.
        /// </summary>
        /// <param name="projectId">The project identifier to use.</param>
        /// <param name="language">The language for which the solution is being created.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The created solution.</returns>
        protected virtual async Task<Solution> CreateSolutionAsync(ProjectId projectId, string language, CancellationToken cancellationToken)
        {
            var compilationOptions = CreateCompilationOptions();

            var xmlReferenceResolver = new TestXmlReferenceResolver();
            foreach (var xmlReference in XmlReferences)
            {
                xmlReferenceResolver.XmlReferences.Add(xmlReference.Key, xmlReference.Value);
            }

            compilationOptions = compilationOptions
                .WithXmlReferenceResolver(xmlReferenceResolver)
                .WithAssemblyIdentityComparer(ReferenceAssemblies.AssemblyIdentityComparer);

            var workspace = CreateWorkspace();
            foreach (var transform in OptionsTransforms)
            {
                workspace.Options = transform(workspace.Options);
            }

            var solution = workspace
                .CurrentSolution
                .AddProject(projectId, DefaultTestProjectName, DefaultTestProjectName, language)
                .WithProjectCompilationOptions(projectId, compilationOptions);

            var metadataReferences = await ReferenceAssemblies.ResolveAsync(language, cancellationToken);
            solution = solution.AddMetadataReferences(projectId, metadataReferences);

            var parseOptions = solution.GetProject(projectId).ParseOptions;
            solution = solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

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
                    supportedDiagnosticsSpecificOptions[diagnostic.Id] = ReportDiagnostic.Default;
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

        public virtual AdhocWorkspace CreateWorkspace()
        {
            var exportProvider = ExportProviderFactory.Value.CreateExportProvider();
            var host = MefHostServices.Create(exportProvider.AsCompositionContext());
            return new AdhocWorkspace(host);
        }

        protected abstract CompilationOptions CreateCompilationOptions();

        /// <summary>
        /// Sort <see cref="Diagnostic"/>s by location in source document.
        /// </summary>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be sorted.</param>
        /// <returns>A collection containing the input <paramref name="diagnostics"/>, sorted by
        /// <see cref="Diagnostic.Location"/> and <see cref="Diagnostic.Id"/>.</returns>
        private static Diagnostic[] SortDistinctDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics
                .OrderBy(d => d.Location.GetLineSpan().Path, StringComparer.Ordinal)
                .ThenBy(d => d.Location.SourceSpan.Start)
                .ThenBy(d => d.Location.SourceSpan.End)
                .ThenBy(d => d.Id)
                .ThenBy(d => GetArguments(d), LexicographicComparer.Instance).ToArray();
        }

        private static IReadOnlyList<object?> GetArguments(Diagnostic diagnostic)
        {
            return (IReadOnlyList<object?>?)diagnostic.GetType().GetProperty("Arguments", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(diagnostic)
                ?? new object[0];
        }

        /// <summary>
        /// Gets the analyzers being tested.
        /// </summary>
        /// <returns>
        /// New instances of all the analyzers being tested.
        /// </returns>
        protected abstract IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers();

        private sealed class LexicographicComparer : IComparer<IEnumerable<object?>>
        {
            public static LexicographicComparer Instance { get; } = new LexicographicComparer();

            public int Compare(IEnumerable<object?> x, IEnumerable<object?> y)
            {
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
    }
}
