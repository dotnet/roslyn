// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[ExportCSharpVisualBasicStatelessLspService(typeof(RunTestsHandler)), Shared]
[Method(RunTestsMethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RunTestsHandler(DotnetCliHelper dotnetCliHelper, TestDiscoverer testDiscoverer, TestRunner testRunner, ServerConfiguration serverConfiguration)
    : ILspServiceDocumentRequestHandler<RunTestsParams, RunTestsPartialResult[]>
{
    private const string RunTestsMethodName = "textDocument/runTests";

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
        var vsTestConsolePath = await dotnetCliHelper.GetVsTestConsolePathAsync(cancellationToken);

        // Instantiate the test platform wrapper.
        var vsTestConsoleWrapper = new VsTestConsoleWrapper(vsTestConsolePath, new ConsoleParameters
        {
            LogFilePath = Path.Combine(serverConfiguration.ExtensionLogDirectory, "testLogs", "vsTestLogs.txt"),
            TraceLevel = GetTraceLevel(serverConfiguration),
            EnvironmentVariables = new()
            {
                // Reset dotnet root so that vs test console can find the right runtimes.
                { DotnetCliHelper.DotnetRootEnvVar, string.Empty },
            }
        });

        var testCases = await testDiscoverer.DiscoverTestsAsync(request.Range, context.Document, projectOutputPath, progress, vsTestConsoleWrapper, cancellationToken);
        if (!testCases.IsEmpty)
        {
            await testRunner.RunTestsAsync(testCases, progress, vsTestConsoleWrapper, cancellationToken);
        }

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
        // https://github.com/dotnet/vscode-csharp/issues/5725
        var arguments = "build";
        using var process = dotnetCliHelper.Run(arguments, workingDirectory, shouldLocalizeOutput: true);

        process.OutputDataReceived += (sender, args) => ReportProgress(progress, args.Data);

        process.ErrorDataReceived += (sender, args) => ReportProgress(progress, args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Failed to run build, see test output for details");
        }

        static void ReportProgress(BufferedProgress<RunTestsPartialResult> progress, string? buildOutput)
        {
            if (buildOutput != null)
            {
                progress.Report(new RunTestsPartialResult("Building project...", buildOutput, Progress: null));
            }
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
