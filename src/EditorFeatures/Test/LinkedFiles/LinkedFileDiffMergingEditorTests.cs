// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }

        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new CodeRefactoringProvider();
        }

        [WpfFact]
        public async Task TestCodeActionPreviewAndApply()
        {
            using (var workspace = TestWorkspaceFactory.CreateWorkspace(WorkspaceXml))
            {
                var codeIssueOrRefactoring = GetCodeRefactoring(workspace);

                var expectedCode = "private class D { }";

                await TestActionsOnLinkedFiles(
                    workspace,
                    expectedText: expectedCode,
                    index: 0,
                    actions: codeIssueOrRefactoring.Actions.ToList(),
                    expectedPreviewContents: expectedCode).ConfigureAwait(true);
            }
        }

        [WpfFact]
        public void TestWorkspaceTryApplyChangesDirectCall()
        {
            using (var workspace = TestWorkspaceFactory.CreateWorkspace(WorkspaceXml))
            {
                var solution = workspace.CurrentSolution;

                var documentId = workspace.Documents.Single(d => !d.IsLinkFile).Id;
                var text = workspace.CurrentSolution.GetDocument(documentId).GetTextAsync().Result;

                var linkedDocumentId = workspace.Documents.Single(d => d.IsLinkFile).Id;
                var linkedText = workspace.CurrentSolution.GetDocument(linkedDocumentId).GetTextAsync().Result;

                var newSolution = solution
                    .WithDocumentText(documentId, text.Replace(13, 1, "D"))
                    .WithDocumentText(linkedDocumentId, linkedText.Replace(0, 6, "private"));

                workspace.TryApplyChanges(newSolution);

                var expectedMergedText = "private class D { }";
                Assert.Equal(expectedMergedText, workspace.CurrentSolution.GetDocument(documentId).GetTextAsync().Result.ToString());
                Assert.Equal(expectedMergedText, workspace.CurrentSolution.GetDocument(linkedDocumentId).GetTextAsync().Result.ToString());
            }
        }

        protected override TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            throw new NotSupportedException();
        }

        protected override ParseOptions GetScriptOptions()
        {
            throw new NotSupportedException();
        }

        private class CodeRefactoringProvider : CodeRefactorings.CodeRefactoringProvider
        {
            public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var linkedDocument = document.Project.Solution.Projects.Single(p => p != document.Project).Documents.Single();

                var newSolution = document.Project.Solution
                    .WithDocumentText(document.Id, document.GetTextAsync().Result.Replace(13, 1, "D"))
                    .WithDocumentText(linkedDocument.Id, linkedDocument.GetTextAsync().Result.Replace(0, 6, "private"));

#pragma warning disable RS0005
                context.RegisterRefactoring(CodeAction.Create("Description", (ct) => Task.FromResult(newSolution)));
#pragma warning restore RS0005

                return SpecializedTasks.EmptyTask;
            }
        }
    }
}
