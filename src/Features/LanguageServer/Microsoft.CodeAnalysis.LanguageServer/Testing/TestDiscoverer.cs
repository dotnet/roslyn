// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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
internal class TestDiscoverer
{
    /// <summary>
    /// TODO - localize messages. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
    /// </summary>
    private const string StageName = "Discovering tests...";

    private readonly TestFrameworkHelper _testFrameworkHelper;
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestDiscoverer(TestFrameworkHelper testFrameworkHelper, ILoggerFactory loggerFactory)
    {
        _testFrameworkHelper = testFrameworkHelper;
        _logger = loggerFactory.CreateLogger<TestDiscoverer>();
    }

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
        progress.Report(new RunTestsPartialResult
        {
            Stage = StageName,
            Message = $"{Environment.NewLine}Starting test discovery"
        });

        var potentialTestMethods = await GetPotentialTestMethodsAsync(range, document, cancellationToken);
        if (potentialTestMethods.IsEmpty)
        {
            progress.Report(new RunTestsPartialResult
            {
                Stage = StageName,
                Message = $"{Environment.NewLine}No test methods found in requested range"
            });

            return ImmutableArray<TestCase>.Empty;
        }

        var discoveryHandler = new DiscoveryHandler(progress);
        var stopwatch = SharedStopwatch.StartNew();

        // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
        // TODO - run settings.  https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
        var discoveryTask = Task.Run(() => vsTestConsoleWrapper.DiscoverTests(SpecializedCollections.SingletonEnumerable(projectOutputPath), discoverySettings: null, discoveryHandler), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelDiscovery());
        await discoveryTask;

        var testCases = discoveryHandler.GetTestCases();
        var elapsed = stopwatch.Elapsed;

        var matchedTests = MatchDiscoveredTestsToTestsInRange(testCases, potentialTestMethods);
        _logger.LogDebug($"Filtered {testCases.Length} to {matchedTests.Length} tests");

        progress.Report(new RunTestsPartialResult
        {
            Stage = StageName,
            Message = $"Found {matchedTests.Length} tests in {elapsed}"
        });

        return matchedTests;

        async Task<ImmutableArray<MethodDeclarationSyntax>> GetPotentialTestMethodsAsync(LSP.Range range, Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken);
            var textSpan = ProtocolConversions.RangeToTextSpan(range, text);
            var potentialTestMethods = await _testFrameworkHelper.GetPotentialTestMethodsAsync(document, textSpan, cancellationToken);
            _logger.LogDebug($"Potential test methods in range: {string.Join(Environment.NewLine, potentialTestMethods)}");
            return potentialTestMethods;
        }
    }

    private ImmutableArray<TestCase> MatchDiscoveredTestsToTestsInRange(ImmutableArray<TestCase> discoveredTests, ImmutableArray<MethodDeclarationSyntax> testMethods)
    {
        // Match the tests in the requested range to the ones we discovered.
        var matchedTests = discoveredTests.Where(d => IsMatch(d, testMethods)).ToImmutableArray();
        return matchedTests;

        bool IsMatch(TestCase discoveredTest, ImmutableArray<MethodDeclarationSyntax> testMethods)
        {
            // Since discovered tests don't run on a particular snapshot, we match optimistically based on test name.
            foreach (var testMethod in testMethods)
            {
                // Either of these could be missing in top level programs.
                var classForMethod = testMethod.GetAncestor<ClassDeclarationSyntax>()?.Identifier.Text ?? string.Empty;
                var namespaceForMethod = testMethod.GetAncestor<NamespaceDeclarationSyntax>()?.Name.ToString() ?? string.Empty;
                // Quick and dirty check - see if the discovered test's fully qualified name contains
                // the test method name and class name.  We could try and match against the test method's FQN, however
                // that won't always match correctly especially in the case of generics.  At worst we run 
                if (discoveredTest.FullyQualifiedName.Contains(testMethod.Identifier.Text, StringComparison.Ordinal)
                    && discoveredTest.FullyQualifiedName.Contains(classForMethod, StringComparison.Ordinal)
                    && discoveredTest.FullyQualifiedName.Contains(namespaceForMethod, StringComparison.Ordinal))
                {
                    return true;
                }
                else
                {
                    _logger.LogDebug($"Discovered test {testMethod} did not match any tests in requested range");
                }
            }

            return false;
        }
    }

    private class DiscoveryHandler : ITestDiscoveryEventsHandler
    {
        private readonly BufferedProgress<RunTestsPartialResult> _progress;
        private readonly ConcurrentBag<TestCase> _testCases = new();
        private bool _isComplete;

        public DiscoveryHandler(BufferedProgress<RunTestsPartialResult> progress)
        {
            _progress = progress;
        }

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
                _progress.Report(new RunTestsPartialResult
                {
                    Stage = StageName,
                    Message = message,
                    Progress = null,
                });
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
