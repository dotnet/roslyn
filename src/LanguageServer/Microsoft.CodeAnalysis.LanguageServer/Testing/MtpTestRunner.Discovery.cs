// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Features.Testing;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.ServerMode.Client;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal sealed partial class MtpTestRunner
{
    private const string VsTestFullyQualifiedNameKey = "vstest.TestCase.FullyQualifiedName";
    private const string ManagedTypeKey = "location.type";
    private const string ManagedMethodKey = "location.method";

    private async Task<ImmutableArray<string>> DiscoverTestsAsync(
        LSP.Range range,
        Document document,
        MtpServerClient client,
        BufferedProgress<RunTestsPartialResult> progress,
        bool useSemanticTestDiscovery,
        CancellationToken cancellationToken)
    {
        var testMethodFinder = document.GetRequiredLanguageService<ITestMethodFinder>();
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var potentialTestMethods = await testMethodFinder.GetPotentialTestMethodsAsync(
            document,
            ProtocolConversions.RangeToTextSpan(range, text),
            useSemanticTestDiscovery,
            cancellationToken).ConfigureAwait(false);
        if (potentialTestMethods.IsEmpty)
        {
            progress.Report(new RunTestsPartialResult(
                LanguageServerResources.Discovering_tests,
                LanguageServerResources.No_test_methods_found_in_requested_range,
                Progress: null));
            return [];
        }

        var partialResult = new RunTestsPartialResult(
            LanguageServerResources.Discovering_tests,
            $"{Environment.NewLine}{LanguageServerResources.Starting_test_discovery}",
            Progress: null);
        progress.Report(partialResult);

        var stopwatch = Stopwatch.StartNew();
        var discoveredTests = new ConcurrentDictionary<string, MtpTestNodeUpdate>(StringComparer.Ordinal);

        void OnTestNodesUpdated(object? sender, MtpTestNodeUpdateEventArgs args)
        {
            foreach (var change in args.Changes)
            {
                if (change.Uid is { } uid)
                    discoveredTests[uid] = change;
            }
        }

        void OnLogReceived(object? sender, MtpLogEventArgs args)
        {
            progress.Report(new RunTestsPartialResult(LanguageServerResources.Discovering_tests, args.Message, Progress: null));
        }

        client.TestNodesUpdated += OnTestNodesUpdated;
        client.LogReceived += OnLogReceived;
        try
        {
            await client.DiscoverTestsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            client.TestNodesUpdated -= OnTestNodesUpdated;
            client.LogReceived -= OnLogReceived;
        }

        ImmutableArray<MtpTestNodeUpdate> tests = [.. discoveredTests.Values.Where(static test => test.NodeType == ActionNodeType)];
        var discoveryElapsed = stopwatch.Elapsed;
        var matchedTestUids = await MatchDiscoveredTestsAsync(
            tests,
            potentialTestMethods,
            testMethodFinder,
            document,
            cancellationToken).ConfigureAwait(false);

        progress.Report(partialResult with
        {
            Message = string.Format(
                LanguageServerResources.Found_0_tests_in_1,
                matchedTestUids.Length,
                RunTestsHandler.GetShortTimespan(discoveryElapsed))
        });

        return matchedTestUids;
    }

    private async Task<ImmutableArray<string>> MatchDiscoveredTestsAsync(
        ImmutableArray<MtpTestNodeUpdate> discoveredTests,
        ImmutableArray<SyntaxNode> potentialTestMethods,
        ITestMethodFinder testMethodFinder,
        Document document,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var matchedTests = ImmutableArray.CreateBuilder<string>();

        foreach (var discoveredTest in discoveredTests)
        {
            var fullyQualifiedName = TryGetFullyQualifiedName(discoveredTest);
            if (fullyQualifiedName is null || discoveredTest.Uid is not { } uid)
                continue;

            if (potentialTestMethods.Any(method => testMethodFinder.IsMatch(semanticModel, method, fullyQualifiedName, cancellationToken)))
                matchedTests.Add(uid);
        }

        _logger.LogDebug("Filtered {DiscoveredTestCount} to {MatchedTestCount} MTP tests", discoveredTests.Length, matchedTests.Count);
        return matchedTests.ToImmutable();
    }

    internal static string? TryGetFullyQualifiedName(MtpTestNodeUpdate test)
    {
        if (test.Node.TryGetValue(VsTestFullyQualifiedNameKey, out var fullyQualifiedName) &&
            fullyQualifiedName is string fullyQualifiedNameString)
        {
            return fullyQualifiedNameString;
        }

        if (test.Node.TryGetValue(ManagedTypeKey, out var managedType) &&
            managedType is string managedTypeString &&
            test.Node.TryGetValue(ManagedMethodKey, out var managedMethod) &&
            managedMethod is string managedMethodString)
        {
            return $"{managedTypeString}.{managedMethodString}";
        }

        return null;
    }
}
