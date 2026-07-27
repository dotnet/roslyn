// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

public abstract partial class AbstractLanguageServerClientTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    protected ILoggerFactory LoggerFactory => new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
    protected TempRoot TempRoot { get; } = new();
    protected TempDirectory ExtensionLogsDirectory => TempRoot.CreateDirectory();

    public void Dispose()
    {
        TempRoot.Dispose();
    }

    private protected async Task<TestLspClient> CreateLanguageServerAsync(
        LspWorkspaceContent workspaceContent,
        LspServerLaunchOptions launchOptions,
        ClientCapabilities? clientCapabilities = null)
    {
        var projectDirectory = TempRoot.CreateDirectory();
        var annotatedLocations = new Dictionary<string, IList<LSP.Location>>();

        foreach (var (relativePath, file) in workspaceContent.Files)
        {
            var filePath = GetFullPath(projectDirectory.Path, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, file.Content);

            if (Path.GetExtension(relativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
                var text = SourceText.From(file.Content);

                AddAnnotatedLocations(annotatedLocations, GetAnnotatedLocations(documentUri, text, file.MarkupSpans));
            }
        }

        if (workspaceContent.ShouldRestore)
        {
            foreach (var projectPath in workspaceContent.Files.Keys.Where(static path => PathUtilities.GetExtension(path) == ".csproj"))
                ProcessUtilities.Run("dotnet", $"restore --project \"{GetFullPath(projectDirectory.Path, projectPath)}\"");
        }

        var workDoneProgressTarget = new WorkDoneProgressTarget();

        // Create server and open the project
        var effectiveClientCapabilities = clientCapabilities ?? new ClientCapabilities();
        TestLspClient lspClient = (launchOptions.DaemonMode, launchOptions.UseNamedPipe) switch
        {
            (DaemonMode: true, UseNamedPipe: true) => await TestLspClient.CreateDaemonPipeAsync(
                effectiveClientCapabilities,
                ExtensionLogsDirectory.Path,
                launchOptions,
                LoggerFactory,
                workspaceContent,
                projectDirectory.Path,
                workDoneProgressTarget,
                locations: annotatedLocations),
            (DaemonMode: true, UseNamedPipe: false) => await TestLspClient.CreateDaemonStdioAsync(
                effectiveClientCapabilities,
                ExtensionLogsDirectory.Path,
                launchOptions,
                LoggerFactory,
                workspaceContent,
                projectDirectory.Path,
                workDoneProgressTarget,
                locations: annotatedLocations),
            (DaemonMode: false, UseNamedPipe: true) => await TestLspClient.CreateSingleServerPipeAsync(
                effectiveClientCapabilities,
                ExtensionLogsDirectory.Path,
                launchOptions,
                LoggerFactory,
                workspaceContent,
                projectDirectory.Path,
                workDoneProgressTarget,
                locations: annotatedLocations),
            (DaemonMode: false, UseNamedPipe: false) => await TestLspClient.CreateSingleServerStdioAsync(
                effectiveClientCapabilities,
                ExtensionLogsDirectory.Path,
                launchOptions,
                LoggerFactory,
                workspaceContent,
                projectDirectory.Path,
                workDoneProgressTarget,
                locations: annotatedLocations),
        };

        if (workspaceContent.LoadPath is not null)
        {
            var fullLoadPath = GetFullPath(projectDirectory.Path, workspaceContent.LoadPath);
            switch (PathUtilities.GetExtension(workspaceContent.LoadPath))
            {
                case ".sln":
                case ".slnx":
                    await lspClient.OpenSolutionAsync(ProtocolConversions.CreateAbsoluteDocumentUri(fullLoadPath));
                    break;
                case ".csproj":
                    await lspClient.OpenProjectsAsync([ProtocolConversions.CreateAbsoluteDocumentUri(fullLoadPath)]);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported load path extension: {PathUtilities.GetExtension(workspaceContent.LoadPath)}");
            }

            await lspClient.WaitForProjectInitializationAsync();
            lspClient.ProjectInitializationCompleted = true;
        }

        return lspClient;

        static string GetFullPath(string workspaceRootPath, string relativePath)
            => PathUtilities.CombinePathsUnchecked(workspaceRootPath, relativePath);

        static void AddAnnotatedLocations(Dictionary<string, IList<LSP.Location>> locations, Dictionary<string, IList<LSP.Location>> locationsToAdd)
        {
            foreach (var (name, newLocations) in locationsToAdd)
            {
                var locationsForName = locations.GetValueOrDefault(name, []);
                locationsForName.AddRange(newLocations);
                locations[name] = [.. locationsForName.Distinct()];
            }
        }
    }

    internal sealed class WorkDoneProgressTarget
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, WorkDoneProgressUnit> _unitsByToken = [];
        private readonly List<(string Title, TaskCompletionSource<WorkDoneProgressUnit> Source)> _unitSourcesByTitle = [];

        [JsonRpcMethod(Methods.WindowWorkDoneProgressCreateName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleCreateWorkDoneProgress(WorkDoneProgressCreateParams workDoneProgressCreateParams, CancellationToken _)
        {
            var token = GetToken(workDoneProgressCreateParams.Token);
            var unit = new WorkDoneProgressUnit(token, workDoneProgressCreateParams);

            lock (_gate)
            {
                _unitsByToken.Add(token, unit);
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ProgressNotificationName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleProgress(JsonElement progressParams, CancellationToken _)
        {
            var progressReport = progressParams.Deserialize<ProgressReportParams>(ProtocolConversions.LspJsonSerializerOptions);
            WorkDoneProgressUnit unit;

            lock (_gate)
            {
                unit = _unitsByToken[progressReport.Token];
            }

            unit.AddProgress(progressReport.Value);

            if (progressReport.Value is WorkDoneProgressBegin begin)
            {
                lock (_gate)
                {
                    foreach (var (title, source) in _unitSourcesByTitle)
                    {
                        if (title == begin.Title)
                            source.TrySetResult(unit);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task<WorkDoneProgressUnit> WaitForWorkDoneProgressCreation(string title)
        {
            lock (_gate)
            {
                if (_unitsByToken.Values.FirstOrDefault(unit => unit.Title == title) is { } unit)
                    return Task.FromResult(unit);

                var unitSource = new TaskCompletionSource<WorkDoneProgressUnit>(TaskCreationOptions.RunContinuationsAsynchronously);
                _unitSourcesByTitle.Add((title, unitSource));
                return unitSource.Task;
            }
        }

        private static string GetToken(SumType<int, string> token)
            => token.Value?.ToString() ?? throw new InvalidOperationException("Work-done progress token must not be null.");

        internal sealed class WorkDoneProgressUnit(string token, WorkDoneProgressCreateParams createParams)
        {
            private readonly object _gate = new();
            private readonly List<WorkDoneProgress> _progressReports = [];
            private readonly TaskCompletionSource<WorkDoneProgressEnd> _endSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public string Token { get; } = token;
            public WorkDoneProgressCreateParams CreateParams { get; } = createParams;

            public string? Title { get; private set; }

            public void AddProgress(WorkDoneProgress progress)
            {
                lock (_gate)
                {
                    _progressReports.Add(progress);

                    if (progress is WorkDoneProgressBegin begin)
                        Title = begin.Title;

                    if (progress is WorkDoneProgressEnd end)
                        _endSource.TrySetResult(end);
                }
            }

            public Task<WorkDoneProgressEnd> WaitForEndAsync()
                => _endSource.Task;

            public ImmutableArray<WorkDoneProgress> GetProgressReports()
            {
                lock (_gate)
                {
                    return [.. _progressReports];
                }
            }
        }

        private readonly record struct ProgressReportParams(
            [property: System.Text.Json.Serialization.JsonPropertyName("token")] string Token,
            [property: System.Text.Json.Serialization.JsonPropertyName("value")] WorkDoneProgress Value);
    }

    private protected static Dictionary<string, IList<LSP.Location>> GetAnnotatedLocations(DocumentUri codeUri, SourceText text, ImmutableDictionary<string, ImmutableArray<TextSpan>> spanMap)
    {
        var locations = new Dictionary<string, IList<LSP.Location>>();
        foreach (var (name, spans) in spanMap)
        {
            var locationsForName = locations.GetValueOrDefault(name, []);
            locationsForName.AddRange(spans.Select(span => ConvertTextSpanWithTextToLocation(span, text, codeUri)));

            // Linked files will return duplicate annotated Locations for each document that links to the same file.
            // Since the test output only cares about the actual file, make sure we de-dupe before returning.
            locations[name] = [.. locationsForName.Distinct()];
        }

        return locations;

        static LSP.Location ConvertTextSpanWithTextToLocation(TextSpan span, SourceText text, DocumentUri documentUri)
        {
            var location = new LSP.Location
            {
                DocumentUri = documentUri,
                Range = ProtocolConversions.TextSpanToRange(span, text),
            };

            return location;
        }
    }

    private protected static TextDocumentIdentifier CreateTextDocumentIdentifier(DocumentUri uri, ProjectId? projectContext = null)
    {
        var documentIdentifier = new VSTextDocumentIdentifier { DocumentUri = uri };

        if (projectContext != null)
        {
            documentIdentifier.ProjectContext = new VSProjectContext
            {
                Id = ProtocolConversions.ProjectIdToProjectContextId(projectContext),
                Label = projectContext.DebugName!,
                Kind = VSProjectKind.CSharp
            };
        }

        return documentIdentifier;
    }

    private protected static CodeActionParams CreateCodeActionParams(LSP.Location location)
        => new()
        {
            TextDocument = CreateTextDocumentIdentifier(location.DocumentUri),
            Range = location.Range,
            Context = new CodeActionContext
            {
                // TODO - Code actions should respect context.
            }
        };
}
