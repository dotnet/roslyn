// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.LinkedFiles;

public sealed class LinkedFileDiffMergingEditorTests : AbstractCodeActionTest
{
    private const string WorkspaceXml = """
        <Workspace>
            <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                <Document FilePath = "C.cs"><![CDATA[public class [|C|]
        {
            public class D
            {
            }
        }]]></Document>
            </Project>
            <Project Language = "C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                <Document IsLinkFile = "true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
            </Project>
        </Workspace>
        """;

    private const string s_expectedCode = """
        internal class C
        {
            private class D
            {
            }
        }
        """;

    protected internal override string GetLanguage()
        => LanguageNames.CSharp;

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new TestCodeRefactoringProvider();

    [WpfFact]
    public async Task TestCodeActionPreviewAndApply()
    {
        // TODO: WPF required due to https://github.com/dotnet/roslyn/issues/46153
        using var workspace = EditorTestWorkspace.Create(WorkspaceXml, composition: EditorTestCompositions.EditorFeatures);
        var codeIssueOrRefactoring = await GetCodeRefactoringAsync(workspace, TestParameters.Default);

        await TestActionOnLinkedFiles(
            workspace,
            expectedText: s_expectedCode,
            action: codeIssueOrRefactoring.CodeActions[0].action,
            expectedPreviewContents: """
                internal class C
                {
                    private class D
                    {
                ...
                """);
    }

    [Fact]
    public async Task TestWorkspaceTryApplyChangesDirectCall()
    {
        using var workspace = EditorTestWorkspace.Create(WorkspaceXml);
        var solution = workspace.CurrentSolution;

        var documentId = workspace.Documents.Single(d => !d.IsLinkFile).Id;
        var text = await workspace.CurrentSolution.GetRequiredDocument(documentId).GetTextAsync();

        var linkedDocumentId = workspace.Documents.Single(d => d.IsLinkFile).Id;
        var linkedText = await workspace.CurrentSolution.GetRequiredDocument(linkedDocumentId).GetTextAsync();

        var textString = linkedText.ToString();

        var newSolution = solution
            .WithDocumentText(documentId, text.Replace(textString.IndexOf("public"), "public".Length, "internal"))
            .WithDocumentText(linkedDocumentId, linkedText.Replace(textString.LastIndexOf("public"), "public".Length, "private"));

        workspace.TryApplyChanges(newSolution);

        Assert.Equal(s_expectedCode, (await workspace.CurrentSolution.GetRequiredDocument(documentId).GetTextAsync()).ToString());
        Assert.Equal(s_expectedCode, (await workspace.CurrentSolution.GetRequiredDocument(linkedDocumentId).GetTextAsync()).ToString());
    }

    protected override ParseOptions GetScriptOptions()
        => throw new NotSupportedException();

    private sealed class TestCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var linkedDocument = document.Project.Solution.Projects.Single(p => p != document.Project).Documents.Single();
            var sourceText = await linkedDocument.GetTextAsync();
            var textString = sourceText.ToString();

            var newSolution = document.Project.Solution
                .WithDocumentText(document.Id, (await document.GetTextAsync()).Replace(textString.IndexOf("public"), "public".Length, "internal"))
                .WithDocumentText(linkedDocument.Id, sourceText.Replace(textString.LastIndexOf("public"), "public".Length, "private"));

            context.RegisterRefactoring(CodeAction.Create("Description", async _ => newSolution), context.Span);
        }
    }
}
