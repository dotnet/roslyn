// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Microsoft.VisualStudio.Razor.Rename;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost;

public class RazorRefactorNotifyServiceTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task Component()
    {
        var movedFiles = await GetRefactorRenamesAsync(
            razorContents: """
                <div>
                </div>
                """,
            additionalFiles: [
                (FilePath("File.cs"), """
                    using SomeProject;

                    nameof(Comp$$onent).ToString();
                    """)],
            newName: "DifferentName");

        var move = Assert.Single(movedFiles);
        Assert.Equal(FilePath("Component.razor"), move.source);
        Assert.Equal(FilePath("DifferentName.razor"), move.destination);
    }

    [Fact]
    public async Task NotComponent()
    {
        var movedFiles = await GetRefactorRenamesAsync(
            razorContents: """
                <div>
                </div>

                @code {
                    public class NotAComponent
                    {
                    }
                }
                """,
            additionalFiles: [
                (FilePath("File.cs"), """
                    using SomeProject;

                    nameof(Component.NotAComp$$onent).ToString();
                    """)],
            newName: "DifferentName");

        Assert.Empty(movedFiles);
    }

    [Fact]
    public async Task Component_WithCodeBehind()
    {
        var movedFiles = await GetRefactorRenamesAsync(
            razorContents: """
                <div>
                </div>
                """,
            additionalFiles: [
                (FilePath("File.cs"), """
                    using SomeProject;

                    nameof(Comp$$onent).ToString();
                    """),
                (FilePath("Component.razor.cs"), """
                    namespace SomeProject;

                    public partial class Component
                    {
                    }
                    """)],
            newName: "DifferentName");

        Assert.Collection(movedFiles,
            m =>
            {
                Assert.Equal(FilePath("Component.razor"), m.source);
                Assert.Equal(FilePath("DifferentName.razor"), m.destination);
            },
            m =>
            {
                Assert.Equal(FilePath("Component.razor.cs"), m.source);
                Assert.Equal(FilePath("DifferentName.razor.cs"), m.destination);
            });
    }

    private async Task<List<(string source, string destination)>> GetRefactorRenamesAsync(string razorContents, string newName, params (string fileName, TestCode contents)[] additionalFiles)
    {
        var additionalContent = additionalFiles.Select(f => (f.fileName, f.contents.Text)).ToArray();
        var razorDocument = CreateProjectAndRazorDocument(razorContents, documentFilePath: FilePath("Component.razor"), additionalFiles: additionalContent);
        var project = razorDocument.Project;
        var csharpDocument = project.Documents.First();

        var compilation = await project.GetCompilationAsync(DisposalToken);

        var csharpPosition = additionalFiles.Single(d => d.contents.Positions.Length == 1).contents.Position;
        var node = await GetSyntaxNodeAsync(csharpDocument, csharpPosition);
        var symbol = FindSymbolToRename(compilation.AssumeNotNull(), node);

        var solution = await Renamer.RenameSymbolAsync(project.Solution, symbol, new SymbolRenameOptions(), newName, DisposalToken);

        Assert.True(LocalWorkspace.TryApplyChanges(solution));

        var expectedChanges = (additionalContent ?? []).Concat([(razorDocument.FilePath!, razorContents)]);
        var fileSystem = new TestFileSystem([.. expectedChanges]);
        var service = new RazorRefactorNotifyService(LoggerFactory);
        Assert.True(service.GetTestAccessor().OnAfterGlobalSymbolRenamed(symbol, newName, throwOnFailure: true, fileSystem));
        return fileSystem.MovedFiles;
    }

    private ISymbol FindSymbolToRename(Compilation compilation, SyntaxNode node)
    {
        var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(node, DisposalToken);
        if (symbol is null)
        {
            symbol = semanticModel.GetSymbolInfo(node, DisposalToken).Symbol;
        }

        Assert.NotNull(symbol);
        return symbol;
    }

    private async Task<SyntaxNode> GetSyntaxNodeAsync(Document document, int position)
    {
        var sourceText = await document.GetTextAsync(DisposalToken);
        var csharpPosition = sourceText.GetLinePosition(position);

        var span = sourceText.GetTextSpan(csharpPosition, csharpPosition);
        var tree = await document.GetSyntaxTreeAsync(DisposalToken);
        var root = await tree.AssumeNotNull().GetRootAsync(DisposalToken);

        return root.FindNode(span, getInnermostNodeForTie: true);
    }
}
