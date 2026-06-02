// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
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
    protected TempRoot TempRoot => new();
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

        // Create server and open the project
        var lspClient = await TestLspClient.CreateAsync(
            clientCapabilities ?? new ClientCapabilities(),
            ExtensionLogsDirectory.Path,
            launchOptions,
            LoggerFactory,
            workspaceContent,
            projectDirectory.Path,
            locations: annotatedLocations);

        if (workspaceContent.ShouldRestore)
        {
            foreach (var projectPath in workspaceContent.Files.Keys.Where(static path => PathUtilities.GetExtension(path) == ".csproj"))
                ProcessUtilities.Run("dotnet", $"restore --project \"{GetFullPath(projectDirectory.Path, projectPath)}\"");
        }

        lspClient.AddClientLocalRpcTarget(new WorkDoneProgressTarget());

        // Listen for project initialization
        var projectInitialized = new TaskCompletionSource();
        lspClient.AddClientLocalRpcTarget(ProjectInitializationHandler.ProjectInitializationCompleteName, () => projectInitialized.SetResult());

#pragma warning disable RS0030 // Do not use banned APIs
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
        }
#pragma warning restore RS0030 // Do not use banned APIs

        // Wait for initialization
        await projectInitialized.Task;

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

    private class WorkDoneProgressTarget
    {
        [JsonRpcMethod(Methods.WindowWorkDoneProgressCreateName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleCreateWorkDoneProgress(WorkDoneProgressCreateParams _1, CancellationToken _2) => Task.CompletedTask;

        [JsonRpcMethod(Methods.ProgressNotificationName, UseSingleObjectParameterDeserialization = true)]
        public Task HandleProgress((string token, object value) _1, CancellationToken _2) => Task.CompletedTask;
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
