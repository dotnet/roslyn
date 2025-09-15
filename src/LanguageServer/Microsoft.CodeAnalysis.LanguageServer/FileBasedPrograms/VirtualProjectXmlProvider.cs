// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Export(typeof(VirtualProjectXmlProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VirtualProjectXmlProvider(IDiagnosticsRefresher diagnosticRefresher, DotnetCliHelper dotnetCliHelper)
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly Dictionary<string, ImmutableArray<SimpleDiagnostic>> _diagnosticsByFilePath = [];

    internal async ValueTask<ImmutableArray<SimpleDiagnostic>> GetCachedDiagnosticsAsync(string path, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            _diagnosticsByFilePath.TryGetValue(path, out var diagnostics);
            return diagnostics;
        }
    }

    internal async ValueTask UnloadCachedDiagnosticsAsync(string path)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            _diagnosticsByFilePath.Remove(path);
        }
    }

    internal async Task<(string VirtualProjectXml, ImmutableArray<SimpleDiagnostic> Diagnostics)?> GetVirtualProjectContentAsync(string documentFilePath, ILogger logger, CancellationToken cancellationToken)
    {
        var result = await GetVirtualProjectContentImplAsync(documentFilePath, logger, cancellationToken);
        if (result is { } project)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken))
            {
                _diagnosticsByFilePath.TryGetValue(documentFilePath, out var previousCachedDiagnostics);
                _diagnosticsByFilePath[documentFilePath] = project.Diagnostics;

                // check for difference, and signal to host to update if so.
                if (previousCachedDiagnostics.IsDefault || !project.Diagnostics.SequenceEqual(previousCachedDiagnostics))
                    diagnosticRefresher.RequestWorkspaceRefresh();
            }
        }
        else
        {
            using (await _gate.DisposableWaitAsync(CancellationToken.None))
            {
                if (_diagnosticsByFilePath.TryGetValue(documentFilePath, out var diagnostics))
                {
                    _diagnosticsByFilePath.Remove(documentFilePath);
                    if (!diagnostics.IsDefaultOrEmpty)
                    {
                        // diagnostics have changed from "non-empty" to "unloaded". refresh.
                        diagnosticRefresher.RequestWorkspaceRefresh();
                    }
                }
            }
        }

        return result;
    }

    private async Task<(string VirtualProjectXml, ImmutableArray<SimpleDiagnostic> Diagnostics)?> GetVirtualProjectContentImplAsync(string documentFilePath, ILogger logger, CancellationToken cancellationToken)
    {
        var workingDirectory = Path.GetDirectoryName(documentFilePath);
        var process = dotnetCliHelper.Run(["run-api"], workingDirectory, shouldLocalizeOutput: true, keepStandardInputOpen: true);

        cancellationToken.Register(() =>
        {
            process?.Kill();
        });

        var input = new RunApiInput.GetProject() { EntryPointFileFullPath = documentFilePath };
        var inputJson = JsonSerializer.Serialize(input, RunFileApiJsonSerializerContext.Default.RunApiInput);
        await process.StandardInput.WriteAsync(inputJson);
        process.StandardInput.Close();

        // Debug severity is used for these because we think it will be common for the user environment to have too old of an SDK for the call to work.
        // Rather than representing a hard error condition, it represents a condition where we need to gracefully downgrade the experience.
        process.ErrorDataReceived += (sender, args) => logger.LogDebug($"[stderr] dotnet run-api: {args.Data}");
        process.BeginErrorReadLine();

        var responseJson = await process.StandardOutput.ReadLineAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            logger.LogDebug($"dotnet run-api exited with exit code '{process.ExitCode}'.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            logger.LogError($"dotnet run-api exited with exit code 0, but did not return any response.");
            return null;
        }

        try
        {
            var response = JsonSerializer.Deserialize(responseJson, RunFileApiJsonSerializerContext.Default.RunApiOutput);
            if (response is RunApiOutput.Error error)
            {
                logger.LogError($"dotnet run-api version: {error.Version}. Latest known version: {RunApiOutput.LatestKnownVersion}");
                logger.LogError($"dotnet run-api returned error: '{error.Message}'");
                return null;
            }

            if (response is RunApiOutput.Project project)
            {
                return (project.Content, project.Diagnostics);
            }

            throw ExceptionUtilities.UnexpectedValue(response);
        }
        catch (JsonException ex)
        {
            // In this case, run-api returned 0 exit code, but gave us back JSON that we don't know how to parse.
            logger.LogError(ex, "Could not deserialize run-api response.");
            logger.LogTrace($"""
              Full run-api response:
              {responseJson}
              """);
            return null;
        }
    }

    /// <summary>
    /// Adjusts a path to a file-based program for use in passing the virtual project to msbuild.
    /// (msbuild needs the path to end in .csproj to recognize as a C# project and apply all the standard props/targets to it.)
    /// </summary>
    internal static string GetVirtualProjectPath(string documentFilePath)
        => Path.ChangeExtension(documentFilePath, ".csproj");

    internal static bool IsFileBasedProgram(string documentFilePath, SourceText text)
    {
        // https://github.com/dotnet/roslyn/issues/78878: this needs to be adjusted to be more sustainable.
        // When we adopt the dotnet run-api, we need to get rid of this or adjust it to be more sustainable (e.g. using the appropriate document to get a syntax tree)
        var tree = CSharpSyntaxTree.ParseText(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path: documentFilePath);
        var root = tree.GetRoot();
        var isFileBasedProgram = root.GetLeadingTrivia().Any(SyntaxKind.IgnoredDirectiveTrivia) || root.ChildNodes().Any(node => node.IsKind(SyntaxKind.GlobalStatement));
        return isFileBasedProgram;
    }
}
