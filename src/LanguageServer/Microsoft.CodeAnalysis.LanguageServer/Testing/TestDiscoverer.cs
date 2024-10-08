// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class TestDiscoverer(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<TestDiscoverer>();

    /// <summary>
    /// Finds tests in the specified document in the specified range.
    ///
    /// Note that since tests run against the last built dll,
    /// </summary>
    public async Task<ImmutableArray<TestCase>> DiscoverTestsAsync(
        LSP.Range range,
        Document document,
        string projectOutputPath,
        string? runSettings,
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        CancellationToken cancellationToken)
    {
        var partialResult = new RunTestsPartialResult(LanguageServerResources.Discovering_tests, $"{Environment.NewLine}{LanguageServerResources.Starting_test_discovery}", Progress: null);
        progress.Report(partialResult);

        var testMethodFinder = document.GetRequiredLanguageService<ITestMethodFinder>();

        // Find any potential test methods (based on attributes) that exist in the input range.
        var potentialTestMethods = await GetPotentialTestMethodsAsync(range, document, testMethodFinder, cancellationToken);
        if (potentialTestMethods.IsEmpty)
        {
            progress.Report(partialResult with { Message = LanguageServerResources.No_test_methods_found_in_requested_range });
            return ImmutableArray<TestCase>.Empty;
        }

        // Next, run the actual vs test discovery on the output dll to figure out what tests actually exist.
        var discoveryHandler = new DiscoveryHandler(progress);
        var stopwatch = SharedStopwatch.StartNew();

        // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
        var discoveryTask = Task.Run(() => vsTestConsoleWrapper.DiscoverTests([projectOutputPath], discoverySettings: runSettings, discoveryHandler), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelDiscovery());
        await discoveryTask;

        if (discoveryHandler.IsAborted())
        {
            progress.Report(partialResult with { Message = LanguageServerResources.Test_discovery_aborted });
            return ImmutableArray<TestCase>.Empty;
        }

        var testCases = discoveryHandler.GetTestCases();
        var elapsed = stopwatch.Elapsed;

        // Match what we found from vs test to what we found in the document to figure out exactly which tests to run.
        var matchedTests = await MatchDiscoveredTestsToTestsInRangeAsync(testCases, potentialTestMethods, testMethodFinder, document, cancellationToken);
        progress.Report(partialResult with { Message = string.Format(LanguageServerResources.Found_0_tests_in_1, matchedTests.Length, RunTestsHandler.GetShortTimespan(elapsed)) });

        return matchedTests;

        async Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(LSP.Range range, Document document, ITestMethodFinder testMethodFinder, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken);
            var textSpan = ProtocolConversions.RangeToTextSpan(range, text);
            var potentialTestMethods = await testMethodFinder.GetPotentialTestMethodsAsync(document, textSpan, cancellationToken);
            _logger.LogDebug(message: $"Potential test methods in range: {string.Join(Environment.NewLine, potentialTestMethods)}");
            return potentialTestMethods;
        }
    }

    private async Task<ImmutableArray<TestCase>> MatchDiscoveredTestsToTestsInRangeAsync(ImmutableArray<TestCase> discoveredTests, ImmutableArray<SyntaxNode> testMethods, ITestMethodFinder testMethodFinder, Document document, CancellationToken cancellationToken)
    {
        // Match the tests in the requested range to the ones we discovered.
        using var _ = ArrayBuilder<TestCase>.GetInstance(out var matchedTests);

        // While using semantics can be expensive, it is reasonable here for a couple reasons
        //   1.  This only runs when the user explicitly asks to run tests.  Any delay here would be dominated by build, discovery, and test running time.
        //   2.  We're only looking at semantic information (the FQN) for methods we've already determined have an appropriate test attribute.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);
        foreach (var discoveredTest in discoveredTests)
        {
            var isMatch = testMethods.Any(m => testMethodFinder.IsMatch(semanticModel, m, discoveredTest.FullyQualifiedName, cancellationToken));
            if (isMatch)
            {
                matchedTests.Add(discoveredTest);
            }
        }

        _logger.LogDebug($"Filtered {discoveredTests.Length} to {matchedTests.Count} tests");
        return matchedTests.ToImmutableAndClear();
    }
}
