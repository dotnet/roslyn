// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.Composition;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class OnDemandProjectLoadingTests(ITestOutputHelper testOutputHelper)
        : AbstractLanguageServerProtocolTests(testOutputHelper), IDisposable
{
    private readonly TempRoot _tempRoot = new();
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    protected override ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);
        return new(LanguageServerTestComposition.GetSharedExportProvider(AbstractLanguageServerHostTests.ServerConfigurationWithoutDevKit, _loggerFactory));
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerFactory.Dispose();
    }

    [Fact]
    public async Task OnDemandProjectLoading_Enabled_ResolvesUnknownDocument()
    {
        var workspace = _tempRoot.CreateDirectory();
        var srcDir = workspace.CreateDirectory("src");
        var appDir = srcDir.CreateDirectory("App");

        // Create a project file but don't load it yet.
        appDir.CreateFile("App.csproj").WriteAllText(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Create a source file in that project.
        var programPath = appDir.CreateFile("Program.cs").WriteAllText("class Program { static void Main() { } }").Path;
        var programUri = ProtocolConversions.CreateAbsoluteDocumentUri(programPath);

        // Create the LSP server with on-demand loading enabled but without pre-loading the project.
        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace: false,
            new InitializationOptions
            {
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                OptionUpdater = options => options.SetGlobalOption(
                    LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand,
                    true),
                WorkspaceFolders =
                [
                    new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.Path), Name = "workspace" }
                ]
            });

        // The project should automatically load and the document should be available in the workspace.
        var result = await GetLspWorkspaceAndDocumentAsync(programUri, testLspServer);
        Assert.NotNull(result.document);
        Assert.Equal("class Program { static void Main() { } }", (await result.document.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task OnDemandProjectLoading_Disabled_DoesNotResolveUnknownDocument()
    {
        var workspace = _tempRoot.CreateDirectory();
        var srcDir = workspace.CreateDirectory("src");
        var appDir = srcDir.CreateDirectory("App");

        // Create a project file but don't load it yet.
        appDir.CreateFile("App.csproj").WriteAllText(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Create a source file in that project.
        var programPath = appDir.CreateFile("Program.cs").WriteAllText("class Program { static void Main() { } }").Path;
        var programUri = ProtocolConversions.CreateAbsoluteDocumentUri(programPath);

        // Create the LSP server with on-demand loading disabled but without pre-loading the project.
        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace: false,
            new InitializationOptions
            {
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                OptionUpdater = options => options.SetGlobalOption(
                    LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand,
                    false),
                WorkspaceFolders =
                [
                    new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.Path), Name = "workspace" }
                ]
            });

        // The project should not load and the document should be null.
        var result = await GetLspWorkspaceAndDocumentAsync(programUri, testLspServer);
        Assert.Null(result.document);
    }

    private static async Task<(Workspace? workspace, Document? document)> GetLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None).ConfigureAwait(false);
        return (workspace, document as Document);
    }
}

public sealed class OnDemandProjectLoadingCdkTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper), IDisposable
{
    private readonly TempRoot _tempRoot = new();
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    protected override ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);
        return new(LanguageServerTestComposition.GetSharedExportProvider(AbstractLanguageServerHostTests.DefaultServerConfiguration, _loggerFactory));
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerFactory.Dispose();
    }

    [Fact]
    public async Task OnDemandProjectLoading_DevKit_DoesNotResolveUnknownDocument()
    {
        var workspace = _tempRoot.CreateDirectory();
        var srcDir = workspace.CreateDirectory("src");
        var appDir = srcDir.CreateDirectory("App");

        appDir.CreateFile("App.csproj").WriteAllText(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var programPath = appDir.CreateFile("Program.cs").WriteAllText("class Program { static void Main() { } }").Path;
        var programUri = ProtocolConversions.CreateAbsoluteDocumentUri(programPath);

        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace: false,
            new InitializationOptions
            {
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                OptionUpdater = options => options.SetGlobalOption(
                    LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand,
                    false),
                WorkspaceFolders =
                [
                    new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.Path), Name = "workspace" }
                ]
            });

        var result = await GetLspWorkspaceAndDocumentAsync(programUri, testLspServer);
        Assert.Null(result.document);
    }

    private static async Task<(Workspace? workspace, Document? document)> GetLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None).ConfigureAwait(false);
        return (workspace, document as Document);
    }
}
