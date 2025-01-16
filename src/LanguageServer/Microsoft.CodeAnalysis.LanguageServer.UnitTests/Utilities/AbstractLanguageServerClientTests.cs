// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public abstract partial class AbstractLanguageServerClientTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly List<TestLspClient> _lspClients = [];

    protected TestOutputLogger TestOutputLogger => new(testOutputHelper);
    protected TempRoot TempRoot => new();
    protected TempDirectory ExtensionLogsDirectory => TempRoot.CreateDirectory();

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        TempRoot.Dispose();

        List<TestLspClient> clientsToDispose;

        // Copy the list out while we're in the lock, otherwise as we dispose these events will get fired, which
        // may try to mutate the list while we're enumerating.
        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            clientsToDispose = [.. _lspClients];
            _lspClients.Clear();
        }

        foreach (var client in clientsToDispose)
            await client.DisposeAsync().ConfigureAwait(false);
    }

    private protected async Task<TestLspClient> CreateCSharpLanguageServerAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markupCode,
        bool includeDevKitComponents,
        bool debugLsp = false)
    {
        string code;
        int? cursorPosition;
        ImmutableDictionary<string, ImmutableArray<TextSpan>> spans;
        TestFileMarkupParser.GetPositionAndSpans(markupCode, out code, out cursorPosition, out spans);

        // Write project file
        var projectDirectory = TempRoot.CreateDirectory();
        var projectPath = Path.Combine(projectDirectory.Path, "Project.csproj");
        await File.WriteAllTextAsync(projectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>net{Environment.Version.Major}.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Write code file
        var codePath = Path.Combine(projectDirectory.Path, "Code.cs");
        await File.WriteAllTextAsync(codePath, code);

#pragma warning disable RS0030 // Do not use banned APIs
        Uri codeUri = new(codePath);
#pragma warning restore RS0030 // Do not use banned APIs
        var text = SourceText.From(code);
        Dictionary<Uri, SourceText> files = new() { [codeUri] = text };
        var annotatedLocations = GetAnnotatedLocations(codeUri, text, spans);

        TestLspClient? lspClient;
        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            // Create server and open the project
            lspClient = await TestLspClient.CreateAsync(
                new ClientCapabilities(),
                ExtensionLogsDirectory.Path,
                includeDevKitComponents,
                debugLsp,
                TestOutputLogger,
                documents: files,
                locations: annotatedLocations);
            lspClient.Disconnected += LSP_Disconnected;
            _lspClients.Add(lspClient);
        }

        // Perform restore and mock up project restore client handler
        ProcessUtilities.Run("dotnet", $"restore --project {projectPath}");
        lspClient.AddClientLocalRpcTarget(ProjectDependencyHelper.ProjectNeedsRestoreName, (string[] projectFilePaths) => { });

        // Listen for project initialization
        var projectInitialized = new TaskCompletionSource();
        lspClient.AddClientLocalRpcTarget(ProjectInitializationHandler.ProjectInitializationCompleteName, () => projectInitialized.SetResult());

#pragma warning disable RS0030 // Do not use banned APIs
        await lspClient.OpenProjectsAsync([new(projectPath)]);
#pragma warning restore RS0030 // Do not use banned APIs

        // Wait for initialization
        await projectInitialized.Task;

        return lspClient;
    }

    private void LSP_Disconnected(object? sender, EventArgs e)
    {
        Contract.ThrowIfNull(sender, $"{nameof(TestLspClient)}.{nameof(TestLspClient.Disconnected)} was raised with a null sender.");

        Task.Run(async () =>
        {
            TestLspClient? clientToDispose = null;

            using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
            {
                // Remove it from our map; it's possible it might have already been removed if we had more than one way we observed a disconnect.
                clientToDispose = _lspClients.FirstOrDefault(c => c == sender);
                if (clientToDispose is not null)
                {
                    _lspClients.Remove(clientToDispose);
                }
            }

            // Dispose outside of the lock (even though we don't expect much to happen at this point)
            if (clientToDispose is not null)
            {
                await clientToDispose.DisposeAsync().ConfigureAwait(false);
            }
        });
    }

    private protected static Dictionary<string, IList<LSP.Location>> GetAnnotatedLocations(Uri codeUri, SourceText text, ImmutableDictionary<string, ImmutableArray<TextSpan>> spanMap)
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

        static LSP.Location ConvertTextSpanWithTextToLocation(TextSpan span, SourceText text, Uri documentUri)
        {
            var location = new LSP.Location
            {
                Uri = documentUri,
                Range = ProtocolConversions.TextSpanToRange(span, text),
            };

            return location;
        }
    }

    private protected static TextDocumentIdentifier CreateTextDocumentIdentifier(Uri uri, ProjectId? projectContext = null)
    {
        var documentIdentifier = new VSTextDocumentIdentifier { Uri = uri };

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
            TextDocument = CreateTextDocumentIdentifier(location.Uri),
            Range = location.Range,
            Context = new CodeActionContext
            {
                // TODO - Code actions should respect context.
            }
        };
}
