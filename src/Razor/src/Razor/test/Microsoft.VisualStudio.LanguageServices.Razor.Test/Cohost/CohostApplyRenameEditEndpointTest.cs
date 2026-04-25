// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostApplyRenameEditEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Theory]
    [CombinatorialData]
    public async Task Component(bool cpsRenameAdditionalFile)
    {
        var oldName = "Component.razor";
        var newName = "DifferentName.razor";
        var additionalFileContents = """
            namespace SomeProject;

            public partial class Component
            {
            }
            """;

        var files = new[]
        {
            (FilePath(oldName), ""),
            (FilePath($"{oldName}.cs"), additionalFileContents)
        };

        var document = CreateProjectAndRazorDocument(contents: "", additionalFiles: files);
        var solution = document.Project.Solution;

        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(files));

        var endpoint = new WorkspaceWillRenameEndpoint(RemoteServiceInvoker, LoggerFactory);

        var renameParams = new RenameFilesParams
        {
            Files = [
                new FileRename
                {
                    OldUri = new(FileUri(oldName)),
                    NewUri = new(FileUri(newName)),
                }
            ]
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(renameParams, document.Project.Solution, DisposalToken);

        Assert.NotNull(result);

        if (cpsRenameAdditionalFile)
        {
            // Simulate CPS renaming the files that Roslyn is editing
            files = [
                (FilePath($"{newName}.cs"), additionalFileContents)
            ];

            var oldFileId = Assert.Single(solution.GetDocumentIdsWithFilePath(FilePath($"{oldName}.cs")));
            solution = document.Project
                .RemoveDocument(oldFileId)
                .AddDocument($"{newName}.cs", SourceText.From(additionalFileContents), filePath: FilePath($"{newName}.cs")).Project.Solution;
        }

        var request = new ApplyRenameEditParams
        {
            Edit = result,
            OldFilePath = FilePath(oldName),
            NewFilePath = FilePath(newName)
        };

        CohostApplyRenameEditEndpoint.TestAccessor.FixUpWorkspaceEdit(request, new TestFileSystem(files));

        await result.AssertWorkspaceEditAsync(solution, [
            (FileUri($"{newName}.cs"), """
                namespace SomeProject;
                
                public partial class DifferentName
                {
                }
                """)], DisposalToken);
    }

    [Fact]
    public async Task Component_Self()
    {
        var oldName = "Component.razor";
        var newName = "DifferentName.razor";
        var contents = """
            <Component />
            """;

        var files = new[]
        {
            (FilePath(oldName), contents)
        };

        var document = CreateProjectAndRazorDocument(contents: "", additionalFiles: files);
        var solution = document.Project.Solution;

        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(files));

        var endpoint = new WorkspaceWillRenameEndpoint(RemoteServiceInvoker, LoggerFactory);

        var renameParams = new RenameFilesParams
        {
            Files = [
                new FileRename
                {
                    OldUri = new(FileUri(oldName)),
                    NewUri = new(FileUri(newName)),
                }
            ]
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(renameParams, document.Project.Solution, DisposalToken);

        Assert.NotNull(result);

        // Simulate CPS renaming the files that Roslyn is editing
        files = [
            (FilePath(newName), contents)
        ];

        var oldFileId = Assert.Single(solution.GetDocumentIdsWithFilePath(FilePath(oldName)));
        solution = document.Project
            .RemoveAdditionalDocument(oldFileId)
            .AddAdditionalDocument(newName, SourceText.From(contents), filePath: FilePath(newName)).Project.Solution;

        var request = new ApplyRenameEditParams
        {
            Edit = result,
            OldFilePath = FilePath(oldName),
            NewFilePath = FilePath(newName)
        };

        CohostApplyRenameEditEndpoint.TestAccessor.FixUpWorkspaceEdit(request, new TestFileSystem(files));

        await result.AssertWorkspaceEditAsync(solution, [
            (FileUri(newName), """
                <DifferentName />
                """)], DisposalToken);
    }

    [Fact]
    public async Task UnrelatedExtraFile()
    {
        var oldName = "Component.razor";
        var newName = "DifferentName.razor";
        var additionalFileName = "Not.This.Component.razor";
        var additionalFileContents = """
            namespace SomeProject;

            public partial class Component
            {
            }
            """;

        var files = new[]
        {
            (FilePath(oldName), ""),
            (FilePath($"{additionalFileName}.cs"), additionalFileContents)
        };

        var document = CreateProjectAndRazorDocument(contents: "", additionalFiles: files);
        var solution = document.Project.Solution;

        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(files));

        var endpoint = new WorkspaceWillRenameEndpoint(RemoteServiceInvoker, LoggerFactory);

        var renameParams = new RenameFilesParams
        {
            Files = [
                new FileRename
                {
                    OldUri = new(FileUri(oldName)),
                    NewUri = new(FileUri(newName)),
                }
            ]
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(renameParams, document.Project.Solution, DisposalToken);

        Assert.NotNull(result);

        var request = new ApplyRenameEditParams
        {
            Edit = result,
            OldFilePath = FilePath(oldName),
            NewFilePath = FilePath(newName)
        };

        CohostApplyRenameEditEndpoint.TestAccessor.FixUpWorkspaceEdit(request, fileSystem);

        await result.AssertWorkspaceEditAsync(solution, [
            (FileUri($"{additionalFileName}.cs"), """
                namespace SomeProject;
                
                public partial class DifferentName
                {
                }
                """)], DisposalToken);
    }
}
