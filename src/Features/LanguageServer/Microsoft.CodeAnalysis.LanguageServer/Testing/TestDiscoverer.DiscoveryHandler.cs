// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal partial class TestDiscoverer
{
    /// <summary>
    /// Implementation of <see cref="ITestDiscoveryEventsHandler"/>
    /// Calls to implementation methods will be synchronous.
    /// </summary>
    private class DiscoveryHandler(BufferedProgress<RunTestsPartialResult> progress) : ITestDiscoveryEventsHandler
    {
        private readonly BufferedProgress<RunTestsPartialResult> _progress = progress;
        private readonly ConcurrentBag<TestCase> _testCases = new();
        private bool _isComplete;
        private bool _isAborted;

        public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases) => AddTests(discoveredTestCases);

        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
        {
            AddTests(lastChunk);
            _isComplete = true;
            _isAborted = isAborted;
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            if (message != null)
            {
                _progress.Report(new RunTestsPartialResult(LanguageServerResources.Discovering_tests, message, Progress: null));
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

        public bool IsAborted()
        {
            Contract.ThrowIfFalse(_isComplete, "Tried to get discovery status before completion");
            return _isAborted;

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
