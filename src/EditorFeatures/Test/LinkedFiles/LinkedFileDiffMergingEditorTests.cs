// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.LinkedFiles
{
    public partial class LinkedFileDiffMergingEditorTests : AbstractCodeActionTest
    {
        private const string WorkspaceXml = @"<Workspace>
                    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj"" PreprocessorSymbols=""Proj1"">
                        <Document FilePath = ""C.cs""><![CDATA[public class [|C|] { }]]></Document>
                    </Project>
                    <Project Language = ""C#"" CommonReferences=""true"" PreprocessorSymbols=""Proj2"">
                        <Document IsLinkFile = ""true"" LinkAssemblyName=""CSProj"" LinkFilePath=""C.cs""/>
                    </Project>
                </Workspace>";

        protected internal override string GetLanguage()
            => LanguageNames.CSharp;

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
            => new TestCodeRefactoringProvider();

        [WpfFact]
        public async Task TestCodeActionPreviewAndApply()
        {
            // TODO: WPF required due to https://github.com/dotnet/roslyn/issues/46153
            using var workspace = EditorTestWorkspace.Create(WorkspaceXml, composition: EditorTestCompositions.EditorFeaturesWpf);
            var codeIssueOrRefactoring = await GetCodeRefactoringAsync(workspace, new TestParameters());

            var expectedCode = "private class D { }";

            await TestActionOnLinkedFiles(
                workspace,
                expectedText: expectedCode,
                action: codeIssueOrRefactoring.CodeActions[0].action,
                expectedPreviewContents: expectedCode);
        }

        [Fact]
        public async Task TestWorkspaceTryApplyChangesDirectCall()
        {
            using var workspace = EditorTestWorkspace.Create(WorkspaceXml);
            var solution = workspace.CurrentSolution;

            var documentId = workspace.Documents.Single(d => !d.IsLinkFile).Id;
            var text = await workspace.CurrentSolution.GetDocument(documentId).GetTextAsync();

            var linkedDocumentId = workspace.Documents.Single(d => d.IsLinkFile).Id;
            var linkedText = await workspace.CurrentSolution.GetDocument(linkedDocumentId).GetTextAsync();

            var newSolution = solution
                .WithDocumentText(documentId, text.Replace(13, 1, "D"))
                .WithDocumentText(linkedDocumentId, linkedText.Replace(0, 6, "private"));

            workspace.TryApplyChanges(newSolution);

            var expectedMergedText = "private class D { }";
            Assert.Equal(expectedMergedText, (await workspace.CurrentSolution.GetDocument(documentId).GetTextAsync()).ToString());
            Assert.Equal(expectedMergedText, (await workspace.CurrentSolution.GetDocument(linkedDocumentId).GetTextAsync()).ToString());
        }

        protected override ParseOptions GetScriptOptions()
            => throw new NotSupportedException();

        private class TestCodeRefactoringProvider : CodeRefactorings.CodeRefactoringProvider
        {
            public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var linkedDocument = document.Project.Solution.Projects.Single(p => p != document.Project).Documents.Single();

                var newSolution = document.Project.Solution
                    .WithDocumentText(document.Id, (await document.GetTextAsync()).Replace(13, 1, "D"))
                    .WithDocumentText(linkedDocument.Id, (await linkedDocument.GetTextAsync()).Replace(0, 6, "private"));

#pragma warning disable RS0005
                context.RegisterRefactoring(CodeAction.Create("Description", (ct) => Task.FromResult(newSolution)), context.Span);
#pragma warning restore RS0005
            }
        }
    }
}
