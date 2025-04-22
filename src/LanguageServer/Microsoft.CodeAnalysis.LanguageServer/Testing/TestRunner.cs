// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class TestRunner(ILoggerFactory loggerFactory)
{
    /// <summary>
    /// A default value for run settings.  While the vstest console included with newer SDKs does
    /// support passing in a null run settings value, the vstest console in older SDKs (.net 6 for example)
    /// will throw if we pass a null value.  So for our default we hardcode an empty XML configuration.
    /// </summary>
    private const string DefaultRunSettings = "<RunSettings/>";
    private readonly ILogger _logger = loggerFactory.CreateLogger<TestRunner>();

    public async Task RunTestsAsync(
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
}
