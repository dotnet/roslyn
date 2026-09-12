// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal sealed partial class VsTestRunner(
    ILoggerFactory loggerFactory,
    ServerConfiguration serverConfiguration,
    DotnetCliHelper dotnetCliHelper,
    LogConfiguration logConfiguration)
{
    private const string DefaultRunSettings = "<RunSettings/>";

    private readonly ILogger _logger = loggerFactory.CreateLogger<VsTestRunner>();

    public async Task RunTestsAsync(
        LSP.Range range,
        Document document,
        string projectOutputPath,
        string projectOutputDirectory,
        bool attachDebugger,
        string? runSettings,
        BufferedProgress<RunTestsPartialResult> progress,
        IClientLanguageServerManager clientLanguageServerManager,
        bool useSemanticTestDiscovery,
        CancellationToken cancellationToken)
    {
        var vsTestConsolePath = await dotnetCliHelper.GetVsTestConsolePathAsync(projectOutputDirectory, cancellationToken);
        var testLogPath = serverConfiguration.ExtensionLogDirectory is not null
            ? Path.Combine(serverConfiguration.ExtensionLogDirectory, "testLogs", "vsTestLogs.txt")
            : null;
        var vsTestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsolePath, new ConsoleParameters
        {
            LogFilePath = testLogPath,
            TraceLevel = GetTraceLevel(logConfiguration),
            EnvironmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables()
        });

        var testCases = await DiscoverTestsAsync(
            range,
            document,
            projectOutputPath,
            runSettings,
            progress,
            vsTestConsoleWrapper,
            useSemanticTestDiscovery,
            cancellationToken).ConfigureAwait(false);

        if (!testCases.IsEmpty)
        {
            await RunDiscoveredTestsAsync(
                testCases,
                progress,
                vsTestConsoleWrapper,
                attachDebugger,
                runSettings,
                clientLanguageServerManager,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunDiscoveredTestsAsync(
        ImmutableArray<TestCase> testCases,
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        bool attachDebugger,
        string? runSettings,
        IClientLanguageServerManager clientLanguageServerManager,
        CancellationToken cancellationToken)
    {
        var initialProgress = new TestProgress
        {
            TotalTests = testCases.Length
        };
        progress.Report(new RunTestsPartialResult(LanguageServerResources.Running_tests, $"{Environment.NewLine}{LanguageServerResources.Starting_test_run}", initialProgress));

        var handler = new TestRunHandler(progress, initialProgress, _logger);

        var runTask = Task.Run(() => RunTests(testCases, progress, vsTestConsoleWrapper, handler, attachDebugger, runSettings, clientLanguageServerManager), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelTestRun());
        await runTask;
    }

    private static void RunTests(
        ImmutableArray<TestCase> testCases,
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        TestRunHandler handler,
        bool attachDebugger,
        string? runSettings,
        IClientLanguageServerManager clientLanguageServerManager)
    {
        runSettings ??= DefaultRunSettings;
        if (attachDebugger)
        {
            // When we want to debug tests we need to use a custom test launcher so that we get called back with the process to attach to.
            vsTestConsoleWrapper.RunTestsWithCustomTestHost(testCases, runSettings: runSettings, handler, new DebugTestHostLauncher(progress, clientLanguageServerManager));
        }
        else
        {
            // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
            vsTestConsoleWrapper.RunTests(testCases, runSettings: runSettings, handler);
        }
    }

    private static TraceLevel GetTraceLevel(LogConfiguration logConfiguration)
    {
        var level = logConfiguration.LogLevel;
        return level switch
        {
            LogLevel.Trace or LogLevel.Debug => TraceLevel.Verbose,
            LogLevel.Information => TraceLevel.Info,
            LogLevel.Warning => TraceLevel.Warning,
            LogLevel.Error or LogLevel.Critical => TraceLevel.Error,
            LogLevel.None => TraceLevel.Off,
            _ => throw new InvalidOperationException($"Unexpected log level {level}"),
        };
    }
}
