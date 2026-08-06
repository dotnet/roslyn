// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class RoslynRenameTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task RenameFromCSharp()
        => VerifyRenameAsync(
            csharpFile: """
                public class MyClass
                {
                    public string MyMe$$thod()
                    {
                        return $"Hi from {nameof(MyMethod)}";
                    }
                }
                """,
            razorFile: """
                This is a Razor document.

                <h1>@_myClass.MyMethod()</h1>

                @code
                {
                    private MyClass _myClass = new MyClass();

                    public string M()
                    {
                        return _myClass.MyMethod();
                    }
                }

                The end.
                """,
            newName: "CallThisFunction",
            expectedCSharpFile: """
                public class MyClass
                {
                    public string CallThisFunction()
                    {
                        return $"Hi from {nameof(CallThisFunction)}";
                    }
                }
                """,
            expectedRazorFile: """
                This is a Razor document.
                
                <h1>@_myClass.CallThisFunction()</h1>
                
                @code
                {
                    private MyClass _myClass = new MyClass();
                
                    public string M()
                    {
                        return _myClass.CallThisFunction();
                    }
                }
                
                The end.
                """);

    private async Task VerifyRenameAsync(
        TestCode csharpFile,
        TestCode razorFile,
        string newName,
        string expectedCSharpFile,
        string expectedRazorFile)
    {
        var razorDocument = CreateProjectAndRazorDocument(razorFile.Text, additionalFiles: [(Path.Combine(TestProjectData.SomeProjectPath, "File.cs"), csharpFile.Text)]);
        var project = razorDocument.Project;
        var csharpDocument = project.Documents.First();

        var compilation = await project.GetCompilationAsync(DisposalToken);

        // Get the syntax node from the C# file
        var sourceText = await csharpDocument.GetTextAsync(DisposalToken);
        var csharpPosition = sourceText.GetLinePosition(csharpFile.Position);

        var span = sourceText.GetTextSpan(csharpPosition, csharpPosition);
        var tree = await csharpDocument.GetSyntaxTreeAsync(DisposalToken);
        var root = await tree.AssumeNotNull().GetRootAsync(DisposalToken);

        var node = root.FindNode(span, getInnermostNodeForTie: true);

        // Find the symbol to rename
        var semanticModel = compilation.AssumeNotNull().GetSemanticModel(node.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(node, DisposalToken);
        if (symbol is null)
        {
            symbol = semanticModel.GetSymbolInfo(node, DisposalToken).Symbol;
        }

        Assert.NotNull(symbol);

        // allowRenamesInRazorSourceGeneratedDocuments looks weird, but we are just using this as a convenient shortcut
        // to calling the standard C# LSP handler.
        var workspaceEdit = await RenameHandler.GetRenameEditAsync(
            csharpDocument,
            csharpPosition,
            newName,
            allowRenamesInRazorSourceGeneratedDocuments: false,
            DisposalToken);

        Assert.NotNull(workspaceEdit);

        var csharpSourceText = await csharpDocument.GetTextAsync(DisposalToken);
        var csharpDocAfterRename = ApplyDocumentEdits(csharpSourceText, csharpDocument.GetURI(), workspaceEdit);
        AssertEx.EqualOrDiff(expectedCSharpFile, csharpDocAfterRename);

        var razorSourceText = await razorDocument.GetTextAsync(DisposalToken);
        var razorDocAfterRename = ApplyDocumentEdits(razorSourceText, razorDocument.GetURI(), workspaceEdit);
        AssertEx.EqualOrDiff(expectedRazorFile, razorDocAfterRename);
    }

    private static string ApplyDocumentEdits(SourceText inputText, DocumentUri documentUri, WorkspaceEdit result)
    {
        var textDocumentEdits = result.EnumerateTextDocumentEdits().ToArray();
        Assert.NotEmpty(textDocumentEdits);
        var changes = textDocumentEdits
            .Where(e => e.TextDocument.DocumentUri == documentUri)
            .SelectMany(e => e.Edits)
            .Select(e => inputText.GetTextChange((TextEdit)e));
        inputText = inputText.WithChanges(changes);

        return inputText.ToString();
    }
}
