// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestDiscoverer(ILoggerFactory loggerFactory)
{
    /// <summary>
    /// TODO - localize messages. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
    /// </summary>
    private const string StageName = "Discovering tests...";
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
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        CancellationToken cancellationToken)
    {
        var partialResult = new RunTestsPartialResult(StageName, $"{Environment.NewLine}Starting test discovery", Progress: null);
        progress.Report(partialResult);

        var testMethodFinder = document.GetRequiredLanguageService<ITestMethodFinder>();

        // Find any potential test methods (based on attributes) that exist in the input range.
        var potentialTestMethods = await GetPotentialTestMethodsAsync(range, document, testMethodFinder, cancellationToken);
        if (potentialTestMethods.IsEmpty)
        {
            progress.Report(partialResult with { Message = "No test methods found in requested range" });
            return ImmutableArray<TestCase>.Empty;
        }

        // Next, run the actual vs test discovery on the output dll to figure out what tests actually exist.
        var discoveryHandler = new DiscoveryHandler(progress);
        var stopwatch = SharedStopwatch.StartNew();

        // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
        // TODO - run settings.  https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
        var discoveryTask = Task.Run(() => vsTestConsoleWrapper.DiscoverTests(SpecializedCollections.SingletonEnumerable(projectOutputPath), discoverySettings: null, discoveryHandler), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelDiscovery());
        await discoveryTask;

        var testCases = discoveryHandler.GetTestCases();
        var elapsed = stopwatch.Elapsed;

        // Match what we found from vs test to what we found in the document to figure out exactly which tests to run.
        var matchedTests = await MatchDiscoveredTestsToTestsInRangeAsync(testCases, potentialTestMethods, testMethodFinder, document, cancellationToken);
        progress.Report(partialResult with { Message = $"Found {matchedTests.Length} tests in {elapsed:g}"});

        return matchedTests;

        async Task<ImmutableArray<SyntaxNode>> GetPotentialTestMethodsAsync(LSP.Range range, Document document, ITestMethodFinder testMethodFinder, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken);
            var textSpan = ProtocolConversions.RangeToTextSpan(range, text);
            var potentialTestMethods = await testMethodFinder.GetPotentialTestMethodsAsync(textSpan, document, cancellationToken);
            _logger.LogDebug(message: $"Potential test methods in range: {string.Join(Environment.NewLine, potentialTestMethods)}");
            return potentialTestMethods;
        }
    }

    private async Task<ImmutableArray<TestCase>> MatchDiscoveredTestsToTestsInRangeAsync(ImmutableArray<TestCase> discoveredTests, ImmutableArray<SyntaxNode> testMethods, ITestMethodFinder testMethodFinder, Document document, CancellationToken cancellationToken)
    {
        // Match the tests in the requested range to the ones we discovered.
        using var _ = ArrayBuilder<TestCase>.GetInstance(out var matchedTests);
        foreach(var discoveredTest in discoveredTests)
        {
            var isMatch = await testMethods.AnyAsync(async (m) => await testMethodFinder.IsMatchAsync(m, discoveredTest.FullyQualifiedName, document, cancellationToken));
            if (isMatch)
            {
                matchedTests.Add(discoveredTest);
            }
        }

        _logger.LogDebug($"Filtered {discoveredTests.Length} to {matchedTests.Count} tests");
        return matchedTests.ToImmutable();
    }

    private class DiscoveryHandler(BufferedProgress<RunTestsPartialResult> progress) : ITestDiscoveryEventsHandler
    {
        private readonly BufferedProgress<RunTestsPartialResult> _progress = progress;
        private readonly ConcurrentBag<TestCase> _testCases = new();
        private bool _isComplete;

        public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases) => AddTests(discoveredTestCases);

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
        {
            AddTests(lastChunk);
            _isComplete = true;
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            if (message != null)
            {
                _progress.Report(new RunTestsPartialResult(StageName, message, Progress: null));
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No need to do anything with raw messages.
            return;
        }

        public ImmutableArray<TestCase> GetTestCases()
        {
            Contract.ThrowIfFalse(_isComplete, "Tried to get test cases before discovery completed");
            return _testCases.ToImmutableArray();
        }

        private void AddTests(IEnumerable<TestCase>? testCases)
        {
            if (testCases != null)
            {
                foreach (var test in testCases)
                {
                    _testCases.Add(test);
                }
            }
        }
    }
}
