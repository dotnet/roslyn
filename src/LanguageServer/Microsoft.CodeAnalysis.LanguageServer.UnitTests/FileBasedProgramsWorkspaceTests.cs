// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
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

        // Add another loose virtual document and verify it goes into the same canonical project.
        var looseFileUriTwo = ProtocolConversions.CreateAbsoluteDocumentUri(@"vscode-notebook-cell://dev-container/test.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriTwo, """
            class Other
            {
                void OtherMethod()
                {
                }
            }
            """).ConfigureAwait(false);

        // Add another misc file and verify it gets added to the same canonical project.
        var (_, canonicalDocumentTwo) = await GetLspWorkspaceAndDocumentAsync(looseFileUriTwo, testLspServer).ConfigureAwait(false);
        Assert.NotNull(canonicalDocumentTwo);
        Assert.Equal(canonicalDocumentOne.Project.Id, canonicalDocumentTwo.Project.Id);
        // The project should also contain the other misc document.
        Assert.Contains(canonicalDocumentTwo.Project.Documents, d => d.Name == looseDocumentOne.Name);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentTwo.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
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

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is loaded in the canonical project.
        var (_, canonicalDocumentOne) = await GetRequiredLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

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
    }
}
