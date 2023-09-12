// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[ExportCSharpVisualBasicStatelessLspService(typeof(RunTestsHandler)), Shared]
[Method(RunTestsMethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RunTestsHandler(DotnetCliHelper dotnetCliHelper, TestDiscoverer testDiscoverer, TestRunner testRunner, ServerConfiguration serverConfiguration, ILoggerFactory loggerFactory)
    : ILspServiceDocumentRequestHandler<RunTestsParams, RunTestsPartialResult[]>
{
    private const string RunTestsMethodName = "textDocument/runTests";

    private readonly ILogger _logger = loggerFactory.CreateLogger<RunTestsHandler>();

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
        var projectOutputDirectory = Path.GetDirectoryName(projectOutputPath);
        Contract.ThrowIfNull(projectOutputDirectory, $"Could not get project output directory from {projectOutputPath}");

        // Find the appropriate vstest.console.dll from the SDK.
        var vsTestConsolePath = await dotnetCliHelper.GetVsTestConsolePathAsync(projectOutputDirectory, cancellationToken);

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

        var runSettingsPath = request.RunSettingsPath;
        var runSettings = await GetRunSettingsAsync(runSettingsPath, progress, cancellationToken);
        var testCases = await testDiscoverer.DiscoverTestsAsync(request.Range, context.Document, projectOutputPath, runSettings, progress, vsTestConsoleWrapper, cancellationToken);
        if (!testCases.IsEmpty)
        {
            var clientLanguageServerManager = context.GetRequiredLspService<IClientLanguageServerManager>();
            await testRunner.RunTestsAsync(testCases, progress, vsTestConsoleWrapper, request.AttachDebugger, runSettings, clientLanguageServerManager, cancellationToken);
        }

        return progress.GetValues() ?? Array.Empty<RunTestsPartialResult>();
    }

    /// <summary>
    /// Format a timespan as a string similar to '5m 2s', omitting any value that is not present.
    /// </summary>
    internal static string GetShortTimespan(TimeSpan t)
    {
        var shortForm = "";
        // Only output milliseconds if less than a second duration
        if (t.TotalSeconds < 1)
        {
            shortForm += string.Format("{0}ms", t.Milliseconds.ToString());
            return shortForm;
        }

        if (t.Days > 0)
        {
            shortForm += string.Format("{0}d ", t.Days.ToString());
        }

        if (t.Hours > 0)
        {
            shortForm += string.Format("{0}h ", t.Hours.ToString());
        }

        if (t.Minutes > 0)
        {
            shortForm += string.Format("{0}m ", t.Minutes.ToString());
        }

        if (t.Seconds > 0)
        {
            shortForm += string.Format("{0}s ", t.Seconds.ToString());
        }

        return shortForm.Trim();
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

        var projectFileName = Path.GetFileName(document.Project.FilePath);
        Contract.ThrowIfNull(projectFileName, $"Unable to get project file name for project {document.Project.Name}");

        // TODO - we likely need to pass the no-restore flag once we have automatic restore enabled.
        // https://github.com/dotnet/vscode-csharp/issues/5725
        var arguments = $"build {projectFileName}";
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
                progress.Report(new RunTestsPartialResult(LanguageServerResources.Building_project, buildOutput, Progress: null));
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

    private async Task<string?> GetRunSettingsAsync(string? runSettingsPath, BufferedProgress<RunTestsPartialResult> progress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(runSettingsPath))
        {
            return null;
        }

        try
        {
            var contents = await File.ReadAllTextAsync(runSettingsPath, cancellationToken);
            var message = string.Format(LanguageServerResources.Using_runsettings_file_at_0, runSettingsPath);
            progress.Report(new(LanguageServerResources.Discovering_tests, message, Progress: null));
            _logger.LogTrace($".runsettings:{Environment.NewLine}{contents}");
            return contents;
        }
        catch (FileNotFoundException)
        {
            var message = string.Format(LanguageServerResources.Runsettings_file_does_not_exist_at_0, runSettingsPath);
            progress.Report(new(LanguageServerResources.Discovering_tests, message, Progress: null));
        }
        catch (Exception ex)
        {
            var message = string.Format(LanguageServerResources.Failed_to_read_runsettings_file_at_0_1, runSettingsPath, ex);
            progress.Report(new(LanguageServerResources.Discovering_tests, message, Progress: null));
        }

        return null;
    }
}
