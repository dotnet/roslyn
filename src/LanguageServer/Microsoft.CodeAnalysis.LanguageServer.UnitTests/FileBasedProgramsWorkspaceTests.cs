// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
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
    public async Task TestLooseFilesInCanonicalProject(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        // File should be initially added as a primordial document in the canonical misc files project with no metadata references.
        var (_, looseDocumentOne) = await GetLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotNull(looseDocumentOne);
        // Should have the primordial canonical document and the loose document.
        Assert.Equal(2, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
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

        // File should be initially added as a primordial document in the canonical misc files project with no metadata references.
        var (_, primordialDocument) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        // Should have the primordial canonical document and the loose document.
        Assert.Equal(2, primordialDocument.Project.Documents.Count());
        Assert.Empty(primordialDocument.Project.MetadataReferences);

        var primordialSyntaxTree = await primordialDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        // TODO: we probably don't want to report syntax errors for '#:' in the primordial non-file document.
        // The logic which decides whether to add '-features:FileBasedProgram' probably needs to be adjusted.
        primordialSyntaxTree.GetDiagnostics(CancellationToken.None).Verify(
            // vscode-notebook-cell://dev-container/test.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
            // #:sdk Microsoft.Net.Sdk
            TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (miscWorkspace, canonicalDocument) = await GetRequiredLspWorkspaceAndDocumentAsync(nonFileUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscWorkspace.Kind);
        Assert.NotNull(canonicalDocument);
        Assert.NotEqual(primordialDocument, canonicalDocument);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocument.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

        var canonicalSyntaxTree = await canonicalDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        // TODO: we probably don't want to report syntax errors for '#:' in the canonical non-file document.
        // The logic which decides whether to add '-features:FileBasedProgram' probably needs to be adjusted.
        canonicalSyntaxTree.GetDiagnostics(CancellationToken.None).Verify(
            // vscode-notebook-cell://dev-container/test.cs(1,2): error CS9298: '#:' directives can be only used in file-based programs ('-features:FileBasedProgram')"
            // #:sdk Microsoft.Net.Sdk
            TestHelpers.Diagnostic(code: 9298, squiggledText: ":").WithLocation(1, 2));
    }

    [Theory, CombinatorialData]
    public async Task TestSemanticDiagnosticsEnabledWhenTopLevelStatementsAdded(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            class C { }
            """).ConfigureAwait(false);

        // File should be initially added as a primordial document in the canonical misc files project with no metadata references.
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        // Should have the primordial canonical document and the loose document.
        Assert.Equal(2, looseDocumentOne.Project.Documents.Count());
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
        await testLspServer.InsertTextAsync(looseFileUriOne, (Line: 0, Column: 0, Text: textToInsert));
        var (workspace, canonicalDocumentTwo) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal("""
            Console.WriteLine("Hello World!");
            class C { }
            """,
            (await canonicalDocumentTwo.GetSyntaxRootAsync())!.ToFullString());
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace.Kind);
        // When presence of top-level statements changes, the misc project is forked again in order to change attributes.
        Assert.NotEqual(canonicalDocumentOne.Project.Id, canonicalDocumentTwo.Project.Id);
        // Now that it has top-level statements, it should be considered to have all information.
        Assert.True(canonicalDocumentTwo.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestSemanticDiagnosticsNotEnabledWhenFineGrainedFlagDisabled(bool mutatingLspWorkspace)
    {
        // Verify that using top-level statements does not enable semantic diagnostics when option 'UseFileBasedProgramsWithoutDirectives' is false.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            OptionUpdater = options => options.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedProgramsWithoutDirectives, false)
        });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            Console.WriteLine("Hello World!");
            class C { }
            """).ConfigureAwait(false);

        // File should be initially added as a primordial document in the canonical misc files project with no metadata references.
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        // Should have the primordial canonical document and the loose document.
        Assert.Equal(2, looseDocumentOne.Project.Documents.Count());
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
        // The 'EnableFileBasedProgramsWithoutDirectives' setting is false, and there are no directives, so semantic errors are not expected.
        Assert.False(canonicalDocumentOne.Project.State.HasAllInformation);
    }

    [Theory, CombinatorialData]
    public async Task TestFileBecomesFileBasedProgramWhenDirectiveAdded(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUriOne = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            Console.WriteLine("Hello World!");
            """).ConfigureAwait(false);

        // File should be initially added as a primordial document in the canonical misc files project with no metadata references.
        var (miscFilesWorkspace, looseDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        // Should have the primordial canonical document and the loose document.
        Assert.Equal(2, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);
        // Semantic diagnostics are not expected because we haven't loaded references
        Assert.False(looseDocumentOne.Project.State.HasAllInformation);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        (miscFilesWorkspace, var canonicalDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
        // This is not loaded as a file-based program (no dedicated restore done for it etc.), so it should be in the misc workspace.
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscFilesWorkspace.Kind);
        // Because we have top-level statements, it should be considered to have all information (semantic diagnostics should be reported etc.)
        Assert.True(canonicalDocumentOne.Project.State.HasAllInformation);

        // Adding a #! directive to a misc file causes it to move to a file-based program project.
        var textToInsert = $"#!/usr/bin/env dotnet{Environment.NewLine}";
        await testLspServer.InsertTextAsync(looseFileUriOne, (Line: 0, Column: 0, Text: textToInsert));
        var (_, fileBasedDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);

        // The document is now in a primordial state in the FileBasedProgramsProjectSystem.
        Assert.NotEqual(fileBasedDocumentOne, canonicalDocumentOne);
        var fileBasedProject = fileBasedDocumentOne.Project;
        Assert.Same(miscFilesWorkspace, fileBasedProject.Solution.Workspace);
        Assert.NotEqual(canonicalDocumentOne.Project.Id, fileBasedProject.Id);
        Assert.Equal("""
            #!/usr/bin/env dotnet
            Console.WriteLine("Hello World!");
            """,
            (await fileBasedDocumentOne.GetSyntaxRootAsync())!.ToFullString());

        // Verify that the project system remains in a good state, when intermediate requests come in while the file-based program project is still loaded.
        var (_, alsoFileBasedDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(fileBasedProject.Id, alsoFileBasedDocumentOne.Project.Id);

        // Wait for the file-based program project to load.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        var (hostWorkspace, fullFileBasedDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, hostWorkspace!.Kind);
        Assert.NotEqual(fileBasedProject.Id, fullFileBasedDocumentOne!.Project.Id);
        Assert.Contains(fullFileBasedDocumentOne!.Project.Documents, d => d.Name == "SomeFile.AssemblyInfo.cs");
        // Because it is loaded as a file-based program, it should be considered to have all information (semantic diagnostics should be reported etc.)
        Assert.True(canonicalDocumentOne.Project.State.HasAllInformation);
    }
}
