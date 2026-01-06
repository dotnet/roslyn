// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public abstract partial class AbstractLanguageServerClientTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    protected ILoggerFactory LoggerFactory => new LoggerFactory([new TestOutputLoggerProvider(testOutputHelper)]);
    protected TempRoot TempRoot => new();
    protected TempDirectory ExtensionLogsDirectory => TempRoot.CreateDirectory();

    public void Dispose()
    {
        TempRoot.Dispose();
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

        // To ensure our TargetFramework is buildable in each test environment we are
        // setting it to match the $(NetVSCode) version.
        await File.WriteAllTextAsync(projectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Write code file
        var codePath = Path.Combine(projectDirectory.Path, "Code.cs");
        await File.WriteAllTextAsync(codePath, code);

        var codeUri = ProtocolConversions.CreateAbsoluteDocumentUri(codePath);
        var text = SourceText.From(code);
        Dictionary<DocumentUri, SourceText> files = new() { [codeUri] = text };
        var annotatedLocations = GetAnnotatedLocations(codeUri, text, spans);

        // Create server and open the project
        var lspClient = await TestLspClient.CreateAsync(
            new ClientCapabilities(),
            ExtensionLogsDirectory.Path,
            includeDevKitComponents,
            debugLsp,
            LoggerFactory,
            documents: files,
            locations: annotatedLocations);

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
