// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.NestedFiles;

public class RemoteAddNestedFileServiceTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CssNestedFile_CreatesEmptyFile()
    {
        var document = CreateProjectAndRazorDocument("<div></div>");
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.Css, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.css"), "/* CSS for File1 component */\r\n")],
            DisposalToken);
    }

    [Fact]
    public async Task JavaScriptNestedFile_CreatesFileWithTemplate()
    {
        var document = CreateProjectAndRazorDocument("<div></div>");
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.JavaScript, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.js"), "// JavaScript for File1 component\r\n")],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_GeneratesCodeBehind()
    {
        var document = CreateProjectAndRazorDocument("<div></div>");
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace SomeProject
                {
                    public partial class File1
                    {
                    }
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_WithNamespaceDirective()
    {
        var document = CreateProjectAndRazorDocument("""
            @namespace My.Custom.Namespace

            <div></div>
            """);
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace My.Custom.Namespace
                {
                    public partial class File1
                    {
                    }
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_IncludesUsingDirectives()
    {
        // Note: The @using directive is removed by Roslyn formatting since the code-behind
        // class body is empty and has no references to types in the imported namespace.
        var document = CreateProjectAndRazorDocument(
            """
            @using System.Diagnostics

            <div></div>
            """);
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace SomeProject
                {
                    public partial class File1
                    {
                    }
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task CshtmlFile_CssNestedFile()
    {
        var document = CreateProjectAndRazorDocument(
            "<div></div>",
            fileKind: AspNetCore.Razor.Language.RazorFileKind.Legacy,
            documentFilePath: FilePath("Page1.cshtml"));
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.Css, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("Page1.cshtml.css"), "/* CSS for Page1 view */\r\n")],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_DefaultsToBlockScopedNamespace()
    {
        // No editorconfig present — should use block-scoped namespace (with braces)
        var document = CreateProjectAndRazorDocument("<div></div>");
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace SomeProject
                {
                    public partial class File1
                    {
                    }
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_WithFileScopedNamespaceEditorConfig()
    {
        var editorConfigPath = FilePath(".editorconfig");
        var editorConfigContent = """
            root = true

            [*.cs]
            csharp_style_namespace_declarations = file_scoped
            """;

        var document = CreateProjectAndRazorDocument(
            "<div></div>",
            projectConfigure: builder => builder.AddAnalyzerConfigDocument(
                editorConfigPath,
                Microsoft.CodeAnalysis.Text.SourceText.From(editorConfigContent)));
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace SomeProject;

                public partial class File1
                {
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task CSharpNestedFile_GlobalNamespace_UsesUnkownNamespace()
    {
        var document = CreateProjectAndRazorDocument("<div></div>", inGlobalNamespace: true);
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, NestedFileKind.CSharp, ct),
            DisposalToken);

        Assert.NotNull(result);
        await result.AssertWorkspaceEditAsync(
            document.Project.Solution,
            [(FileUri("File1.razor.cs"), """
                namespace Unknown
                {
                    public partial class File1
                    {
                    }
                }

                """)],
            DisposalToken);
    }

    [Fact]
    public async Task InvalidFileKind_ReturnsNull()
    {
        var document = CreateProjectAndRazorDocument("<div></div>");
        var result = await RemoteServiceInvoker.TryInvokeAsync<IRemoteAddNestedFileService, WorkspaceEdit?>(
            document.Project.Solution,
            (service, solutionInfo, ct) => service.GetNewNestedFileWorkspaceEditAsync(solutionInfo, document.Id, (NestedFileKind)999, ct),
            DisposalToken);

        Assert.Null(result);
    }
}
