// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[ExportCSharpVisualBasicStatelessLspService(typeof(RunTestsHandler)), Shared]
[Method(RunTestsMethodName)]
internal class RunTestsHandler : ILspServiceDocumentRequestHandler<RunTestsParams, RunTestsPartialResult[]>
{
    private const string RunTestsMethodName = "textDocument/runTests";

    private readonly DotnetCliHelper _dotnetCliHelper;
    private readonly TestDiscoverer _testDiscoverer;
    private readonly TestRunner _testRunner;
    private readonly ServerConfiguration _serverConfiguration;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RunTestsHandler(DotnetCliHelper dotnetCliHelper, TestDiscoverer testDiscoverer, TestRunner testRunner, ServerConfiguration serverConfiguration)
    {
        _dotnetCliHelper = dotnetCliHelper;
        _testDiscoverer = testDiscoverer;
        _testRunner = testRunner;
        _serverConfiguration = serverConfiguration;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(RunTestsParams request)
    {
        return request.TextDocument;
    }

    public async Task<RunTestsPartialResult[]> HandleRequestAsync(RunTestsParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Document);
        using var progress = BufferedProgress.Create(request.PartialResultToken);

        // First, build to make sure we have a relatively up to date project.
        await BuildAsync(context.Document, progress, cancellationToken);

        var projectOutputPath = context.Document.Project.OutputFilePath;
        Contract.ThrowIfFalse(File.Exists(projectOutputPath), $"Output path {projectOutputPath} is missing");

        // Find the appropriate vstest.console.dll from the SDK.
        var vsTestConsolePath = await _dotnetCliHelper.GetVsTestConsolePathAsync(cancellationToken);

        // Instantiate the test platform wrapper.
        var vsTestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsolePath, new ConsoleParameters
        {
            LogFilePath = Path.Combine(_serverConfiguration.ExtensionLogDirectory, "vsTestConsoleLogs.txt"),
            TraceLevel = GetTraceLevel(_serverConfiguration),
        });

        var testCases = await _testDiscoverer.DiscoverTestsAsync(request.Range, context.Document, projectOutputPath, progress, vsTestConsoleWrapper, cancellationToken);
        if (testCases.IsEmpty)
        {
            return progress.GetValues() ?? Array.Empty<RunTestsPartialResult>();
        }

        await _testRunner.RunTestsAsync(testCases, progress, vsTestConsoleWrapper, cancellationToken);

        return progress.GetValues() ?? Array.Empty<RunTestsPartialResult>();
    }

    /// <summary>
    /// Shells out to the .NET CLI to build the project to ensure that its relatively up to date as of this request.
    /// We can never be entirely sure that this build happens on exactly the same snapshot that this request was made on
    /// as something could have changed on the client side immediately after this request was triggered.
    ///
    /// However if we don't do a build it is likely the user code is very different from what they last built
    /// which results in a confusing experience.
    /// </summary>
    private async Task BuildAsync(Document document, BufferedProgress<RunTestsPartialResult> progress, CancellationToken cancellationToken)
    {
        var workingDirectory = Path.GetDirectoryName(document.Project.FilePath);
        Contract.ThrowIfNull(workingDirectory, $"Unable to get working directory for project {document.Project.Name}");

        // TODO - we likely need to pass the no-restore flag once we have automatic restore enabled.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1776138/
        var arguments = "build";
        using var process = _dotnetCliHelper.Run(arguments, workingDirectory);

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                ReportProgress(progress, args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                ReportProgress(progress, args.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Failed to run build, see test output for details");
        }

        static void ReportProgress(BufferedProgress<RunTestsPartialResult> progress, string buildOutput)
        {
            progress.Report(new RunTestsPartialResult
            {
                Message = buildOutput,
                Stage = "Building project...",
                Progress = null,
            });
        }
    }

    private static TraceLevel GetTraceLevel(ServerConfiguration serverConfiguration)
    {
        return serverConfiguration.MinimumLogLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace or Microsoft.Extensions.Logging.LogLevel.Debug => TraceLevel.Verbose,
            Microsoft.Extensions.Logging.LogLevel.Information => TraceLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => TraceLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error or Microsoft.Extensions.Logging.LogLevel.Critical => TraceLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.None => TraceLevel.Off,
            _ => throw new InvalidOperationException($"Unexpected log level {serverConfiguration.MinimumLogLevel}"),
        };
    }
}
