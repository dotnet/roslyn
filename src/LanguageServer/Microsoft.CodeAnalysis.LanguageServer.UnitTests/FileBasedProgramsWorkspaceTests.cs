// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class FileBasedProgramsWorkspaceTests : AbstractLspMiscellaneousFilesWorkspaceTests, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly TempRoot _tempRoot;
    private readonly TempDirectory _mefCacheDirectory;

    public FileBasedProgramsWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        _tempRoot = new();
        _mefCacheDirectory = _tempRoot.CreateDirectory();
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerProvider.Dispose();
    }

    protected override async ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            _loggerFactory,
            includeDevKitComponents: false,
            cacheDirectory: _mefCacheDirectory.Path,
            extensionPaths: []);

        return exportProvider;
    }

    private protected override async ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath, string content)
    {
        // For the file-based programs, we want to put them in the real workspace via the real host service
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var project = await workspaceFactory.HostProjectFactory.CreateAndAddToWorkspaceAsync(
            Guid.NewGuid().ToString(),
            LanguageNames.CSharp,
            new ProjectSystemProjectCreationInfo { AssemblyName = Guid.NewGuid().ToString() },
            workspaceFactory.ProjectSystemHostInfo);

        project.AddSourceFile(filePath);

        return workspaceFactory.HostWorkspace.CurrentSolution.GetRequiredProject(project.Id).Documents.Single();
    }

    private protected override Workspace GetHostWorkspace(TestLspServer testLspServer)
    {
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        return workspaceFactory.HostWorkspace;
    }

    [Theory, CombinatorialData]
    public async Task TestFileBasedProgram_Simple(bool mutatingLspWorkspace)
    {
        // Simple case where document is classified as file-based program and virtual project is loaded.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText).ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        // Diagnostics not reported for '#:'
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));

        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        // Diagnostics not reported for '#:'
        syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));
    }

    [Theory, CombinatorialData]
    public async Task TestFileBasedProgram_Extensionless(bool mutatingLspWorkspace)
    {
        // Unix utility case. Users want to mark C# files as executable, remove the '.cs' extension and use them in the shell, like any other unix CLI utility.
        // Users should be able to get full editor support for such files, as long as they set the correct language mode.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("greeter").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText, languageId: "csharp").ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        // Diagnostics not reported for '#:'/'#!'
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));

        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        // Diagnostics not reported for '#:'/'#!'
        syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));
    }

    [Theory, CombinatorialData]
    public async Task TestFileBasedProgram_Multitargeting(bool mutatingLspWorkspace)
    {
        // Load a file-based app which multitargets via `#:property TargetFrameworks`.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #:property TargetFrameworks=net8.0;net10.0
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("greeter").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText, languageId: "csharp").ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        // Just ensure that we got some fully formed document which has all the information for one of the targets.
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);
    }

    [Theory, CombinatorialData]
    public async Task TestFileBasedProgram_EntryPointClosed(bool mutatingLspWorkspace)
    {
        // Show that a file-based program project is unloaded when the entry point file is closed.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText).ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        // Document is in misc workspace in a primordial state before project load completes.
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        await testLspServer.CloseDocumentAsync(looseFileUri);

        (workspace, document) = await GetLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Null(workspace);
        Assert.Null(document);
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFilesInCanonicalProject(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUriOne = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        // Document should be initially found in a primordial misc files project
        var (_, looseDocumentOne) = await GetLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotNull(looseDocumentOne);
        Assert.Equal(1, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is found in a forked canonical project.
        var (_, canonicalDocumentOne) = await GetLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotNull(canonicalDocumentOne);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

        // Add another loose virtual document and verify it goes into a forked canonical project.
        var looseFileUriTwo = ProtocolConversions.CreateAbsoluteDocumentUri(@"vscode-notebook-cell://dev-container/test.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriTwo, """
            class Other
            {
                void OtherMethod()
                {
                }
            }
            """).ConfigureAwait(false);

        var (_, canonicalDocumentTwo) = await GetLspWorkspaceAndDocumentAsync(looseFileUriTwo, testLspServer).ConfigureAwait(false);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        (_, canonicalDocumentTwo) = await GetLspWorkspaceAndDocumentAsync(looseFileUriTwo, testLspServer).ConfigureAwait(false);

        Assert.NotNull(canonicalDocumentTwo);
        Assert.NotEqual(canonicalDocumentOne.Project.Id, canonicalDocumentTwo.Project.Id);
        Assert.DoesNotContain(canonicalDocumentTwo.Project.Documents, d => d.Name == looseDocumentOne.Name);
        // Semantic diagnostics are not expected due to absence of top-level statements
        Assert.False(canonicalDocumentTwo.Project.State.HasAllInformation);
        // Should have the appropriate generated files from the base misc files project now that we ran a design time build
        Assert.Contains(canonicalDocumentTwo.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
    }

    /// <summary>Test that a document which does not have an on-disk path, is never treated as a file-based program.</summary>
    [Theory, CombinatorialData]
    public async Task TestNonFileDocumentsAreNotFileBasedPrograms(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var nonFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"vscode-notebook-cell://dev-container/test.cs");
        await testLspServer.OpenDocumentAsync(nonFileUri, """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World");
            """).ConfigureAwait(false);

        // File should be initially found in a primordial misc files project
        var (_, primordialDocument) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(1, primordialDocument.Project.Documents.Count());
        Assert.Empty(primordialDocument.Project.MetadataReferences);

        // No errors for '#:' are expected.
        var primordialSyntaxTree = await primordialDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(primordialSyntaxTree.GetDiagnostics(CancellationToken.None));

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (miscWorkspace, canonicalDocument) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscWorkspace.Kind);
        Assert.NotNull(canonicalDocument);
        Assert.NotEqual(primordialDocument, canonicalDocument);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocument.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

        // No errors for '#:' are expected.
        var canonicalSyntaxTree = await canonicalDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(canonicalSyntaxTree.GetDiagnostics(CancellationToken.None));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81644")]
    public async Task TestScriptsWithIgnoredDirectives(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var nonFileUri = CreateAbsoluteDocumentUri("script.csx");
        await testLspServer.OpenDocumentAsync(nonFileUri, """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World");
            """).ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        await verifyAsync(workspace, document);

        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        await verifyAsync(workspace, document);

        async Task verifyAsync(Workspace workspace, Document document)
        {
            Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
            Assert.Equal(1, document.Project.Documents.Count());
            Assert.Empty(document.Project.MetadataReferences);

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
            syntaxTree.GetDiagnostics(CancellationToken.None).Verify(
                // script.csx(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
                // #:sdk Microsoft.Net.Sdk
                TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));
        }
    }

    [Theory, CombinatorialData]
    public async Task TestSemanticDiagnosticsEnabledWhenTopLevelStatementsAdded(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var tempDir = _tempRoot.CreateDirectory();
        var initialText = """
            class C { }
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(initialText);
        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUriOne, initialText).ConfigureAwait(false);

        // File should be initially found in a primordial misc files project
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        Assert.Equal(1, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);
        // Semantic diagnostics are not expected because we haven't loaded references
        Assert.False(looseDocumentOne.Project.State.HasAllInformation);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (_, canonicalDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
        // There are no top-level statements, so semantic errors are still not expected.
        Assert.False(canonicalDocumentOne.Project.State.HasAllInformation);

        // Adding a top-level statement to a misc file causes it to report semantic errors.
        var textToInsert = $"""Console.WriteLine("Hello World!");{Environment.NewLine}""";
        // Write updated content to disk so the project system can pick up the change.
        sourceFile.WriteAllText($"{textToInsert}{initialText}");
        await testLspServer.InsertTextAsync(looseFileUriOne, (Line: 0, Column: 0, Text: textToInsert));
        await Task.Delay(100);
        await WaitForProjectLoad(looseFileUriOne, testLspServer);
        var (workspace, canonicalDocumentTwo) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal("""
            Console.WriteLine("Hello World!");
            class C { }
            """,
            (await canonicalDocumentTwo.GetSyntaxRootAsync())!.ToFullString());
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        // Now that it has top-level statements, it should be considered to have all information.
        Assert.True(canonicalDocumentTwo.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestSemanticDiagnosticsNotEnabledWhenCoarseGrainedFlagDisabled(bool mutatingLspWorkspace)
    {
        // Verify that using top-level statements and '#:' directives does not enable semantic diagnostics when option 'EnableFileBasedPrograms' is false.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            OptionUpdater = options => options.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false)
        });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var looseFileUriOne = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            class C { }
            """).ConfigureAwait(false);

        // File should be a "primordial" miscellaneous document and stay that way.
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        await verifyAsync(looseDocumentOne);

        // Wait for project initialization to complete
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Document is still in a primordial miscellaneous project
        var (_, looseDocumentTwo) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        await verifyAsync(looseDocumentTwo);

        async Task verifyAsync(Document looseDocument)
        {
            Assert.Single(looseDocument.Project.Documents);
            Assert.Empty(looseDocument.Project.MetadataReferences);
            // Semantic diagnostics are not expected because we haven't loaded references
            Assert.False(looseDocument.Project.State.HasAllInformation);
            Assert.DoesNotContain("FileBasedProgram", looseDocument.Project.ParseOptions!.Features);

            // FileBasedProgram feature flag is not passed, so an error is expected on '#:'.
            var primordialSyntaxTree = await looseDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
            primordialSyntaxTree.GetDiagnostics(CancellationToken.None).Verify(
                // C:\SomeFile.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
                // #:sdk Microsoft.Net.Sdk
                TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));
        }
    }

    [Theory, CombinatorialData]
    public async Task TestSemanticDiagnosticsNotEnabledWhenFineGrainedFlagDisabled(bool mutatingLspWorkspace)
    {
        // Verify that using top-level statements does not enable semantic diagnostics when option 'EnableFileBasedProgramsWhenAmbiguous' is false.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            OptionUpdater = options => options.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableSemanticErrorsInMiscellaneousFiles, false)
        });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var looseFileUriOne = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            Console.WriteLine("Hello World!");
            class C { }
            """).ConfigureAwait(false);

        // File should be initially found in a primordial misc files project
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        Assert.Equal(1, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);
        // Semantic diagnostics are not expected because we haven't loaded references
        Assert.False(looseDocumentOne.Project.State.HasAllInformation);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (_, canonicalDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
        // The 'EnableFileBasedProgramsWhenAmbiguous' setting is false, and there are no directives, so semantic errors are not expected.
        Assert.False(canonicalDocumentOne.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestEnableFileBasedProgramsChangedDynamically_01(bool mutatingLspWorkspace)
    {
        // Toggle the EnableFileBasedPrograms setting while file-based program project is not fully loaded.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText).ConfigureAwait(false);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Equal(1, document.Project.Documents.Count());
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));

        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false);

        (_, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.DoesNotContain("FileBasedProgram", document.Project.ParseOptions!.Features);
        syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        syntaxTree.GetDiagnostics(CancellationToken.None).Verify(
            // C:\SomeFile.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
            // #:sdk Microsoft.Net.Sdk
            TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));

        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, true);

        (_, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);

        syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));
    }

    private async ValueTask WaitForProjectLoad(DocumentUri looseFileUri, TestLspServer testLspServer)
    {
        _ = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
    }

    [Theory, CombinatorialData]
    public async Task TestEnableFileBasedProgramsChangedDynamically_02(bool mutatingLspWorkspace)
    {
        // Toggle the EnableFileBasedPrograms setting after a file-based program project is fully loaded.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText).ConfigureAwait(false);

        await WaitForProjectLoad(looseFileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.True(document.Project.State.HasAllInformation);

        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.DoesNotContain("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.False(document.Project.State.HasAllInformation);

        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, true);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestEnableFileBasedProgramsChangedDynamically_03(bool mutatingLspWorkspace)
    {
        // Toggle the EnableFileBasedPrograms setting after the canonical misc files project is fully loaded.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var tempDir = _tempRoot.CreateDirectory();
        var sourceText = """
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(sourceText);
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUri, sourceText).ConfigureAwait(false);

        await WaitForProjectLoad(looseFileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.True(document.Project.State.HasAllInformation);

        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.DoesNotContain("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.False(document.Project.State.HasAllInformation);

        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, true);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Contains("FileBasedProgram", document.Project.ParseOptions!.Features);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestEnableFileBasedProgramsChangedDynamically_Script(bool mutatingLspWorkspace)
    {
        // Test that scripts are never file based programs, even when changing the setting while running
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.csx");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            #:sdk Microsoft.Net.Sdk
            Console.WriteLine("Hello World!");
            """).ConfigureAwait(false);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        var (_, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        await verifyAsync(document);

        var globalOptions = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (_, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        await verifyAsync(document);

        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, true);
        await WaitForProjectLoad(looseFileUri, testLspServer);

        (_, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUri, testLspServer).ConfigureAwait(false);
        await verifyAsync(document);

        async ValueTask verifyAsync(Document document)
        {
            Assert.Equal(WorkspaceKind.MiscellaneousFiles, document.Project.Solution.WorkspaceKind);
            Assert.Equal(1, document.Project.Documents.Count());
            Assert.DoesNotContain("FileBasedProgram", document.Project.ParseOptions!.Features);

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
            syntaxTree.GetDiagnostics(CancellationToken.None).Verify(
                // C:\SomeFile.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
                // #:sdk Microsoft.Net.Sdk
                TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));
        }
    }

    [Theory, CombinatorialData]
    public async Task TestFileBecomesFileBasedProgramWhenDirectiveAdded(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var tempDir = _tempRoot.CreateDirectory();
        var initialText = """
            Console.WriteLine("Hello World!");
            """;
        var sourceFile = tempDir.CreateFile("SomeFile.cs").WriteAllText(initialText);
        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(sourceFile.Path);
        await testLspServer.OpenDocumentAsync(looseFileUriOne, initialText).ConfigureAwait(false);

        // File should be initially found in a primordial misc files project
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Equal(1, document.Project.Documents.Count());
        Assert.Empty(document.Project.MetadataReferences);
        // Semantic diagnostics are not expected because we haven't loaded references
        Assert.False(document.Project.State.HasAllInformation);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        // This is not loaded as a file-based program (no dedicated restore done for it etc.), so it should be in the misc workspace.
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Contains(document.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
        Assert.True(document.Project.State.HasAllInformation);

        // Adding a #! directive to a misc file causes it to move to a file-based program project.
        var textToInsert = $"#!/usr/bin/env dotnet{Environment.NewLine}";
        // Write updated content to disk so the build host can load it.
        sourceFile.WriteAllText($"#!/usr/bin/env dotnet{Environment.NewLine}{initialText}");
        await testLspServer.InsertTextAsync(looseFileUriOne, (Line: 0, Column: 0, Text: textToInsert));

        // Still in canonical project until next DTB finished.
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.Contains(document.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
        Assert.True(document.Project.State.HasAllInformation);

        // Verify that the project system remains in a good state, when intermediate requests come in while the file-based program project is still loaded.
        var (_, document2) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(document.Project.Id, document2.Project.Id);

        // Wait for the file-based program project to load.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.Contains(document.Project.Documents, d => d.Name == "SomeFile.AssemblyInfo.cs");
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestFileStopsBeingFileBasedProgramWhenDirectivesDeleted(bool mutatingLspWorkspace)
    {
        var tempDir = _tempRoot.CreateDirectory();
        var appCsText = """
            #!/usr/bin/env dotnet
            Console.WriteLine("Hello World!");
            """;
        var appCsFile = tempDir.CreateFile("App.cs").WriteAllText(appCsText);

        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer
            });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var appCsUri = CreateAbsoluteDocumentUri(appCsFile.Path);
        await testLspServer.OpenDocumentAsync(appCsUri, """
            #!/usr/bin/env dotnet
            Console.WriteLine("Hello World!");
            """).ConfigureAwait(false);
        await WaitForProjectLoad(appCsUri, testLspServer);

        // Document is loaded as a file-based app
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        // Removing a #! directive (line 0) causes it to stop being a file-based app
        var newAppCsText = """
            Console.WriteLine("Hello World!");
            """;
        await testLspServer.DeleteTextAsync(appCsUri,
            (StartLine: 0, StartColumn: 0, EndLine: 1, EndColumn: 0));
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(newAppCsText, (await document.GetTextAsync()).ToString());

        // Flush the document change to disk to trigger a reload of the FBA project.
        appCsFile.WriteAllText(newAppCsText);
        // Wait for the batching queue timeout.
        await Task.Delay(100);
        await WaitForProjectLoad(appCsUri, testLspServer);

        // Now the document is a miscellaneous file
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestDiagnosticsRequestedAfterDocumentClosed(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var nonFileUri = ProtocolConversions.CreateAbsoluteDocumentUri("untitled:Untitled-1");
        await testLspServer.OpenDocumentAsync(nonFileUri, """
            Console.WriteLine("Hello World");
            """, languageId: "csharp").ConfigureAwait(false);

        // Get the document info once to kickoff the canonical project loading process
        _ = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (miscWorkspace, canonicalDocument) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscWorkspace.Kind);
        Assert.NotNull(canonicalDocument);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocument.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

        // File was saved to disk. Simulate this by opening the document under its new name and closing it under its old name.
        var fileUri = CreateAbsoluteDocumentUri("MyFile.cs");
        await testLspServer.OpenDocumentAsync(fileUri, """
            Console.WriteLine("Hello World");
            """).ConfigureAwait(false);

        await testLspServer.CloseDocumentAsync(nonFileUri).ConfigureAwait(false);

        // Issue a "textDocument/diagnostic" request for the closed document
        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(() =>
            testLspServer.ExecuteRequestAsync<DocumentDiagnosticParams, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>(
                Methods.TextDocumentDiagnosticName,
                new DocumentDiagnosticParams() { TextDocument = new TextDocumentIdentifier { DocumentUri = nonFileUri } },
                CancellationToken.None));
        Assert.Equal(RoslynLspErrorCodes.NonFatalRequestFailure, exception.ErrorCode);

        // Issue a "textDocument/hover" request for the closed document
        // At the time of authoring this test, the HoverHandler calls 'GetRequiredDocument'.
        // Demonstrate that instead of calling the handler (which would fail, due to the request not having any document),
        // we throw an exception with a known error code.
        exception = await Assert.ThrowsAsync<RemoteInvocationException>(() =>
            testLspServer.ExecuteRequestAsync<HoverParams, Hover>(
                Methods.TextDocumentHoverName,
                new HoverParams() { Position = new Position(0, 0), TextDocument = new TextDocumentIdentifier { DocumentUri = nonFileUri } },
                CancellationToken.None));
        Assert.Equal(RoslynLspErrorCodes.NonFatalRequestFailure, exception.ErrorCode);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestMultiFile_Simulated_01(bool mutatingLspWorkspace)
    {
        // Use a Directory.Build.props in the same directory to simulate an '#:include' directive.
        var tempDir = _tempRoot.CreateDirectory();
        var dbPropsText = """
            <Project>
                <ItemGroup>
                    <Compile Include="Util.cs" />
                </ItemGroup>
            </Project>
            """;
        var dbPropsFile = tempDir.CreateFile("Directory.Build.props").WriteAllText(dbPropsText);

        var utilCsText = """
            internal class Util { }
            """;
        var utilCsFile = tempDir.CreateFile("Util.cs").WriteAllText(utilCsText);

        var appCsText = """
            #:property A=B
            new Util();
            """;
        var appCsFile = tempDir.CreateFile("App.cs").WriteAllText(appCsText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var appCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(appCsFile.Path);
        await testLspServer.OpenDocumentAsync(appCsUri, appCsText).ConfigureAwait(false);
        await WaitForProjectLoad(appCsUri, testLspServer);

        // Verify no semantic errors for App.cs
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        var model = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
        Assert.Empty(model.GetDiagnostics());

        // Verify no semantic errors for Util.cs
        var appCsProject = document.Project;
        var utilCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(utilCsFile.Path);

        // app.cs and util.cs are part of the same project
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(utilCsUri, testLspServer).ConfigureAwait(false);
        Assert.True(appCsProject.Id.Equals(document.Project.Id),
            $"Unexpected false: ({appCsProject.FilePath}, {appCsProject.Id}), != ({document.Project.FilePath}, {document.Project.Id})");

        model = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
        Assert.Empty(model.GetDiagnostics());
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestMultiFile_Simulated_02(bool mutatingLspWorkspace)
    {
        // Reference the same non-entry-point file in both an `#:include` and `<Compile Include=...` of an ordinary project
        var tempDir = _tempRoot.CreateDirectory();
        var dbPropsText = """
            <Project>
                <ItemGroup>
                    <Compile Include="Util.cs" />
                </ItemGroup>
            </Project>
            """;
        var dbPropsFile = tempDir.CreateFile("Directory.Build.props").WriteAllText(dbPropsText);

        var utilCsText = """
            internal class Util { }
            """;
        var utilCsFile = tempDir.CreateFile("Util.cs").WriteAllText(utilCsText);

        var appCsText = """
            #:property A=B
            new Util();
            """;
        var appCsFile = tempDir.CreateFile("App.cs").WriteAllText(appCsText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var appCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(appCsFile.Path);
        await testLspServer.OpenDocumentAsync(appCsUri, appCsText).ConfigureAwait(false);
        await WaitForProjectLoad(appCsUri, testLspServer);

        // app.cs project was loaded and includes util.cs as one of the files
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
        Assert.True(document.Project.Documents.Contains(document => document.Name == "Util.cs"));

        // Add an ordinary project containing the same "util.cs" file
        workspace.SetCurrentSolution(
            solution => solution.AddProject("Ordinary", "Ordinary", LanguageNames.CSharp)
                .AddDocument("Util.cs", SourceText.From(utilCsText), filePath: utilCsFile.Path)
                .Project.Solution,
            WorkspaceChangeKind.ProjectAdded);
        var solution = workspace.CurrentSolution;
        var projects = solution.Projects.ToImmutableArray();
        Assert.Equal(2, projects.Length);
        Assert.Equal("App", projects[0].AssemblyName);
        Assert.Equal("Ordinary", projects[1].AssemblyName);

        // Lookup "Util.cs" in the "App" project context
        var utilCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(utilCsFile.Path);
        (_, _, var textDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(utilCsUri, projects[0].Id), CancellationToken.None);
        Assert.NotNull(textDocument);
        Assert.Equal("App", textDocument.Project.AssemblyName);

        // Lookup "Util.cs" in the "Ordinary" project context
        (_, _, textDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(utilCsUri, projects[1].Id), CancellationToken.None);
        Assert.NotNull(textDocument);
        Assert.Equal("Ordinary", textDocument.Project.AssemblyName);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestOrdinaryProjectContainsFileBasedAppEntryPoint_01(bool mutatingLspWorkspace)
    {
        // Error scenario: open a file-based app entry point document,
        // then load a project which contains the same document.
        var tempDir = _tempRoot.CreateDirectory();
        var appCsText = """
            #:property A=B
            Console.WriteLine("Hello");
            """;
        var appCsFile = tempDir.CreateFile("App.cs").WriteAllText(appCsText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open the file-based app entry point first
        var appCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(appCsFile.Path);
        await testLspServer.OpenDocumentAsync(appCsUri, appCsText).ConfigureAwait(false);
        await WaitForProjectLoad(appCsUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        // Add an ordinary project containing the same "App.cs" file
        workspace.SetCurrentSolution(
            solution => solution.AddProject("Ordinary", "Ordinary", LanguageNames.CSharp)
                .AddDocument("App.cs", SourceText.From(appCsText), filePath: appCsFile.Path)
                .Project.Solution,
            WorkspaceChangeKind.ProjectAdded);
        var solution = workspace.CurrentSolution;
        var projects = solution.Projects.ToImmutableArray();
        Assert.Equal(2, projects.Length);
        Assert.Equal("App", projects[0].AssemblyName);
        Assert.Equal("Ordinary", projects[1].AssemblyName);

        // Lookup "App.cs" in the "App" project context
        var utilCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(appCsFile.Path);
        (_, _, var textDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(
            CreateTextDocumentIdentifier(utilCsUri, projects[0].Id), CancellationToken.None);
        document = (Document)textDocument!;
        Assert.Equal("App", document.Project.AssemblyName);

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Contains("FileBasedProgram", syntaxTree.Options.Features.Keys);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));

        // Lookup "App.cs" in the "Ordinary" project context
        (_, _, textDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(
            CreateTextDocumentIdentifier(utilCsUri, projects[1].Id), CancellationToken.None);
        Assert.NotNull(textDocument);
        document = (Document)textDocument!;
        Assert.Equal("Ordinary", document.Project.AssemblyName);

        // TODO: it's unclear why, but, a syntax error is not being reported for '#:' here despite absence of FileBasedProgram feature flag
        // Perhaps a syntax tree from the other document is being reused?
        syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.DoesNotContain("FileBasedProgram", syntaxTree.Options.Features.Keys);
        Assert.Empty(syntaxTree.GetDiagnostics(CancellationToken.None));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestOrdinaryProjectContainsFileBasedAppEntryPoint_02(bool mutatingLspWorkspace)
    {
        // Error scenario: load a project which contains a file-based app entry point,
        // then open the file-based app entry point.
        var tempDir = _tempRoot.CreateDirectory();
        var appCsText = """
            #:property A=B
            Console.WriteLine("Hello");
            """;
        var appCsFile = tempDir.CreateFile("App.cs").WriteAllText(appCsText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Add an ordinary project containing the "App.cs" file
        testLspServer.TestWorkspace.SetCurrentSolution(
            solution => solution.AddProject("Ordinary", "Ordinary", LanguageNames.CSharp)
                .AddDocument("App.cs", SourceText.From(appCsText), filePath: appCsFile.Path)
                .Project.Solution,
            WorkspaceChangeKind.ProjectAdded);

        // Document is found in the ordinary project and syntax error reported on '#:'
        var appCsUri = ProtocolConversions.CreateAbsoluteDocumentUri(appCsFile.Path);
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.Equal("Ordinary", document.Project.AssemblyName);

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        syntaxTree.GetDiagnostics(CancellationToken.None).Verify(
            // App.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
            // #:property A=B
            TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));

        // Now open the file-based app entry point file
        await testLspServer.OpenDocumentAsync(appCsUri, appCsText).ConfigureAwait(false);
        await WaitForProjectLoad(appCsUri, testLspServer);

        // The file-based app project doesn't end up getting loaded.
        // That's OK, as long as something in the editor reports an error (in this case the parse error on '#:' above).
        // User needs to either remove App.cs from the ordinary project, or, delete the '#:' directives in App.cs. 
        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(appCsUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.Equal("Ordinary", document.Project.AssemblyName);
        var assemblyNames = workspace.CurrentSolution.Projects.SelectAsArray(project => project.AssemblyName);
        AssertEx.SetEqual(["Test", "Ordinary"], assemblyNames);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_01(bool mutatingLspWorkspace)
    {
        // in-cone: csproj in a containing directory (one level of nesting)
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var srcDir = tempDir.CreateDirectory("src");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = srcDir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders = [new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace" }]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);

        // Check that changes are observed when when closing/reopening the document.
        // Note that just deleting/recreating the csproj file doesn't cause us to observe a change, for a document which is already open.
        File.Delete(csprojFile.Path);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation); // We didn't observe the file system change

        await testLspServer.CloseDocumentAsync(fileUri).ConfigureAwait(false);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        csprojFile = tempDir.CreateFile("Project.csproj");
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation); // We didn't observe the file system change

        await testLspServer.CloseDocumentAsync(fileUri).ConfigureAwait(false);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_02(bool mutatingLspWorkspace)
    {
        // in-cone: csproj in same directory
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = tempDir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders = [new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace" }]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_03(bool mutatingLspWorkspace)
    {
        // in-cone: csproj in a nested containing directory
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var src1Dir = tempDir.CreateDirectory("src1");
        var src2Dir = src1Dir.CreateDirectory("src2");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = src2Dir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders = [new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace" }]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_04(bool mutatingLspWorkspace)
    {
        // not-in-cone: csproj in a sibling directory
        var tempDir = _tempRoot.CreateDirectory();

        var src1Dir = tempDir.CreateDirectory("src");
        var csprojFile = src1Dir.CreateFile("Project.csproj");

        var src2Dir = tempDir.CreateDirectory("src2");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = src2Dir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders = [new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace" }]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        // Test that deleting/re-creating an irrelevant csproj doesn't result in a bad project system/workspace behavior.
        // i.e. HasAllInformation is unaffected by the irrelevant delete.
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        File.Delete(csprojFile.Path);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        await testLspServer.CloseDocumentAsync(fileUri).ConfigureAwait(false);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        csprojFile = src1Dir.CreateFile("Project.csproj");
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);

        await testLspServer.CloseDocumentAsync(fileUri).ConfigureAwait(false);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_05(bool mutatingLspWorkspace)
    {
        // not-in-cone: csproj in a child directory
        var tempDir = _tempRoot.CreateDirectory();
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = tempDir.CreateFile("file.cs").WriteAllText(fileText);

        var src1Dir = tempDir.CreateDirectory("src1");
        src1Dir.CreateFile("Project.csproj");

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders = [new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace" }]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_06(bool mutatingLspWorkspace)
    {
        // not-in-cone: csproj in a parent directory above a workspace directory.
        // even though the csproj file is "in-cone" in the file system, it's not within the workspace folder, so we act as if it's not-in-cone.
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var src1Dir = tempDir.CreateDirectory("src1");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = src1Dir.CreateFile("file.cs").WriteAllText(fileText);
        var src2Dir = tempDir.CreateDirectory("src2");

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(src1Dir.Path), Name = "workspace1" },
                new() { DocumentUri = CreateAbsoluteDocumentUri(src2Dir.Path), Name = "workspace2" },
            ]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_07(bool mutatingLspWorkspace)
    {
        // in-cone: Test an edge case where multiple workspace folders are in-cone with each other.
        // The csproj-in-cone check may do unnecessary work in this case, but, observable behavior must be correct
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var src1Dir = tempDir.CreateDirectory("src1");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = src1Dir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(src1Dir.Path), Name = "workspace1" },
                new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace2" },
            ]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.False(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_08(bool mutatingLspWorkspace)
    {
        // not-in-cone: No workspace folder is opened at all. Therefore no search is done for a csproj in cone.
        var tempDir = _tempRoot.CreateDirectory();
        var csprojFile = tempDir.CreateFile("Project.csproj");

        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = tempDir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/81410")]
    public async Task TestCsprojInCone_09(bool mutatingLspWorkspace)
    {
        // not-in-cone: A workspace folder is open, but a .cs file outside that folder was opened.
        // No search is done for a .csproj-in-cone.
        var tempDir = _tempRoot.CreateDirectory();
        var src1Dir = tempDir.CreateDirectory("src1");
        var src2Dir = tempDir.CreateDirectory("src2");
        var csprojFile = src2Dir.CreateFile("Project.csproj");
        var fileText = """
            Console.WriteLine("Hello World");
            """;
        var file = src2Dir.CreateFile("file.cs").WriteAllText(fileText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(src1Dir.Path), Name = "workspace1" }
            ]
        });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var fileUri = CreateAbsoluteDocumentUri(file.Path);
        await testLspServer.OpenDocumentAsync(fileUri, fileText).ConfigureAwait(false);
        await WaitForProjectLoad(fileUri, testLspServer);

        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(fileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        Assert.True(document.Project.State.HasAllInformation);
    }
}
