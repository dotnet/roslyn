// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.ServerMode.Client;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal sealed partial class MtpTestRunner(ILoggerFactory loggerFactory)
{
    // MTP also reports container nodes; action nodes are the executable tests used for discovery and progress.
    private const string ActionNodeType = "action";
    private const string RetryIsSupersededKey = "retry.is-superseded";

    private readonly ILogger _logger = loggerFactory.CreateLogger<MtpTestRunner>();

    public async Task RunTestsAsync(
        LSP.Range range,
        Document document,
        string projectOutputPath,
        bool attachDebugger,
        BufferedProgress<RunTestsPartialResult> progress,
        IClientLanguageServerManager clientLanguageServerManager,
        bool useSemanticTestDiscovery,
        CancellationToken cancellationToken)
    {
        using var client = await CreateClientAsync(projectOutputPath, cancellationToken).ConfigureAwait(false);
        var capabilities = await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (!capabilities.SupportsDiscovery)
            throw new InvalidOperationException("The Microsoft.Testing.Platform application does not support test discovery.");

        var matchedTestUids = await DiscoverTestsAsync(
            range,
            document,
            client,
            progress,
            useSemanticTestDiscovery,
            cancellationToken).ConfigureAwait(false);

        if (matchedTestUids.IsEmpty)
        {
            await StopClientAsync(client, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (capabilities.MultiRequestSupport)
        {
            await RunMatchedTestsAsync(
                client,
                matchedTestUids,
                attachDebugger,
                progress,
                clientLanguageServerManager,
                cancellationToken).ConfigureAwait(false);

            await StopClientAsync(client, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Servers without multi-request support terminate after discovery, so execution needs a new client.
        await StopClientAsync(client, cancellationToken).ConfigureAwait(false);

        using var runClient = await CreateClientAsync(projectOutputPath, cancellationToken).ConfigureAwait(false);
        await runClient.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await RunMatchedTestsAsync(
            runClient,
            matchedTestUids,
            attachDebugger,
            progress,
            clientLanguageServerManager,
            cancellationToken).ConfigureAwait(false);
        await StopClientAsync(runClient, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunMatchedTestsAsync(
        MtpServerClient client,
        ImmutableArray<string> matchedTestUids,
        bool attachDebugger,
        BufferedProgress<RunTestsPartialResult> progress,
        IClientLanguageServerManager clientLanguageServerManager,
        CancellationToken cancellationToken)
    {
        var testProgress = new TestProgress { TotalTests = matchedTestUids.Length };
        progress.Report(new RunTestsPartialResult(
            LanguageServerResources.Running_tests,
            $"{Environment.NewLine}{LanguageServerResources.Starting_test_run}",
            testProgress));

        var stopwatch = Stopwatch.StartNew();
        var terminalStates = new Dictionary<string, string>(StringComparer.Ordinal);

        void OnTestNodesUpdated(object? sender, MtpTestNodeUpdateEventArgs args)
        {
            var message = new StringBuilder();
            foreach (var change in args.Changes)
            {
                if (!TryAggregateTerminalState(terminalStates, change))
                    continue;

                // A theory can report multiple data-row results with the same UID, so report every
                // non-superseded result even when it does not change the aggregate state for that UID.
                AppendTestResult(message, change);
            }

            if (message.Length > 0)
            {
                progress.Report(new RunTestsPartialResult(
                    LanguageServerResources.Running_tests,
                    message.ToString(),
                    CreateProgress(matchedTestUids.Length, terminalStates.Values)));
            }
        }

        void OnLogReceived(object? sender, MtpLogEventArgs args)
        {
            progress.Report(new RunTestsPartialResult(LanguageServerResources.Running_tests, args.Message, Progress: null));
        }

        client.TestNodesUpdated += OnTestNodesUpdated;
        client.LogReceived += OnLogReceived;
        try
        {
            if (attachDebugger)
            {
                var didAttach = await TestDebugger.AttachAsync(
                    client.ProcessId,
                    progress,
                    clientLanguageServerManager,
                    cancellationToken).ConfigureAwait(false);
                if (!didAttach)
                    return;
            }

            await client.RunTestsAsync(matchedTestUids, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            client.TestNodesUpdated -= OnTestNodesUpdated;
            client.LogReceived -= OnLogReceived;
        }

        string[] finalStates = [.. terminalStates.Values];
        var finalProgress = CreateProgress(matchedTestUids.Length, finalStates);
        var state = finalStates.Contains("canceled")
            ? LanguageServerResources.Canceled
            : finalProgress.TestsFailed == 0
                ? LanguageServerResources.Passed
                : LanguageServerResources.Failed;
        var summary = @$"==== {LanguageServerResources.Summary} ===={Environment.NewLine}{state}  - {string.Format(
            LanguageServerResources.Failed_0_Passed_1_Skipped_2_Total_3_Duration_4,
            finalProgress.TestsFailed,
            finalProgress.TestsPassed,
            finalProgress.TestsSkipped,
            finalProgress.TotalTests,
            RunTestsHandler.GetShortTimespan(stopwatch.Elapsed))}{Environment.NewLine}";

        progress.Report(new RunTestsPartialResult(LanguageServerResources.Running_tests, summary, finalProgress));
    }

    private Task<MtpServerClient> CreateClientAsync(string projectOutputPath, CancellationToken cancellationToken)
    {
        var options = new MtpServerClientOptions
        {
            ClientName = "Roslyn Language Server",
            // The runner consumes each request's update stream immediately; it does not retain a test-node graph.
            IsStateful = false,
            Logger = new DelegateMtpClientLogger(LogClientMessage),
        };

        foreach (var (name, value) in TestRunnerEnvironment.CreateEnvironmentVariables())
            options.EnvironmentVariables[name] = value;

        return MtpServerClient.LaunchAsync(projectOutputPath, options, cancellationToken);
    }

    private static async Task StopClientAsync(MtpServerClient client, CancellationToken cancellationToken)
    {
        await client.ExitAsync(cancellationToken).ConfigureAwait(false);
        await client.ShutdownAsync().ConfigureAwait(false);
    }

    private void LogClientMessage(MtpClientLogLevel level, string message)
    {
        switch (level)
        {
            case MtpClientLogLevel.Trace:
                _logger.LogTrace("[MTP] {Message}", message);
                break;
            case MtpClientLogLevel.Debug:
                _logger.LogDebug("[MTP] {Message}", message);
                break;
            case MtpClientLogLevel.Information:
                _logger.LogInformation("[MTP] {Message}", message);
                break;
            case MtpClientLogLevel.Warning:
                _logger.LogWarning("[MTP] {Message}", message);
                break;
            case MtpClientLogLevel.Error:
                _logger.LogError("[MTP] {Message}", message);
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(level);
        }
    }

    internal static TestProgress CreateProgress(int totalTests, IEnumerable<string> states)
    {
        long passed = 0;
        long failed = 0;
        long skipped = 0;

        foreach (var state in states)
        {
            switch (state)
            {
                case "passed":
                    passed++;
                    break;
                case "skipped":
                    skipped++;
                    break;
                case "failed":
                case "timed-out":
                case "error":
                    failed++;
                    break;
            }
        }

        return new TestProgress(passed, failed, skipped, totalTests);
    }

    private static bool IsTerminalState(string? state)
        => state is "passed" or "skipped" or "failed" or "timed-out" or "error" or "canceled";

    internal static bool TryAggregateTerminalState(Dictionary<string, string> terminalStates, MtpTestNodeUpdate change)
    {
        if (change.NodeType != ActionNodeType ||
            change.Uid is not { } uid ||
            change.ExecutionState is not { } executionState ||
            !IsTerminalState(executionState) ||
            change.Node.TryGetValue(RetryIsSupersededKey, out var isSuperseded) && isSuperseded is true)
        {
            return false;
        }

        if (!terminalStates.TryGetValue(uid, out var previousState) ||
            GetTerminalStatePriority(executionState) > GetTerminalStatePriority(previousState))
        {
            // Multiple theory rows can share a UID. Aggregate them so cancellation or failure wins,
            // otherwise any passing row wins over a run where every row was skipped.
            terminalStates[uid] = executionState;
        }

        return true;
    }

    private static int GetTerminalStatePriority(string state)
        => state switch
        {
            "canceled" => 4,
            "failed" or "timed-out" or "error" => 3,
            "passed" => 2,
            "skipped" => 1,
            _ => 0,
        };

    private static void AppendTestResult(StringBuilder builder, MtpTestNodeUpdate test)
    {
        builder.AppendLine($"[{test.ExecutionState}] {test.DisplayName}");

        if (!string.IsNullOrWhiteSpace(test.ErrorMessage))
        {
            builder.AppendLine($"    {LanguageServerResources.Message}:");
            AppendIndented(builder, test.ErrorMessage, 8);
        }

        if (!string.IsNullOrWhiteSpace(test.ErrorStackTrace))
        {
            builder.AppendLine($"    {LanguageServerResources.Stack_Trace}:");
            AppendIndented(builder, test.ErrorStackTrace, 8);
        }

        if (!string.IsNullOrWhiteSpace(test.StandardOutput))
        {
            builder.AppendLine($"    {LanguageServerResources.Standard_Output_Messages}:");
            AppendIndented(builder, test.StandardOutput, 8);
        }

        if (!string.IsNullOrWhiteSpace(test.StandardError))
        {
            builder.AppendLine($"    {LanguageServerResources.Standard_Error_Messages}:");
            AppendIndented(builder, test.StandardError, 8);
        }
    }

    private static void AppendIndented(StringBuilder builder, string text, int indentation)
    {
        var prefix = new string(' ', indentation);
        foreach (var line in text.Split([Environment.NewLine], StringSplitOptions.None))
            builder.Append(prefix).AppendLine(line);
    }
}
