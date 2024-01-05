﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions
{
    public class ApplyChangesOperationTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new MyCodeRefactoringProvider((Func<Solution, Solution>)parameters.fixProviderData);

        private class MyCodeRefactoringProvider : CodeRefactoringProvider
        {
            private readonly Func<Solution, Solution> _changeSolution;

            public MyCodeRefactoringProvider(Func<Solution, Solution> changeSolution)
            {
                _changeSolution = changeSolution;
            }

            public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var codeAction = new TestCodeAction(_changeSolution(context.Document.Project.Solution));
                context.RegisterRefactoring(codeAction);
                return Task.CompletedTask;
            }

            private sealed class TestCodeAction : CodeAction
            {
                private readonly Solution _changedSolution;

                public TestCodeAction(Solution changedSolution)
                {
                    _changedSolution = changedSolution;
                }

                public override string Title => "Title";

                protected override Task<Solution?> GetChangedSolutionAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
                    => Task.FromResult<Solution?>(_changedSolution);
            }
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1419139")]
        public async Task TestMakeTextChangeWithInterveningEditToDifferentFile()
        {
            // This should succeed as the code action is trying to edit a file that is not touched by the actual
            // workspace edit that already went in.
            await TestSuccessfulApplicationAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Program1.cs"">
class Program1
{
}
        </Document>
        <Document FilePath=""Program2.cs"">
class Program2
{
}
        </Document>
    </Project>
</Workspace>",
                codeActionTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.WithDocumentText(document1.Id, SourceText.From("NewProgram1Content"));
                },
                intermediaryTransform: solution =>
                {
                    var document2 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program2"));
                    return solution.WithDocumentText(document2.Id, SourceText.From("NewProgram2Content"));
                });
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1419139")]
        public async Task TestMakeTextChangeWithInterveningRemovalToDifferentFile()
        {
            // This should succeed as the code action is trying to edit a file that is not touched by the actual
            // workspace edit that already went in.
            await TestSuccessfulApplicationAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Program1.cs"">
class Program1
{
}
        </Document>
        <Document FilePath=""Program2.cs"">
class Program2
{
}
        </Document>
    </Project>
</Workspace>",
                codeActionTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.WithDocumentText(document1.Id, SourceText.From("NewProgram1Content"));
                },
                intermediaryTransform: solution =>
                {
                    var document2 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program2"));
                    return solution.RemoveDocument(document2.Id);
                });
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1419139")]
        public async Task TestMakeTextChangeWithInterveningEditToSameFile()
        {
            // This should fail as the code action is trying to edit a file that is was already edited by the actual
            // workspace edit that already went in.
            await TestFailureApplicationAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Program1.cs"">
class Program1
{
}
        </Document>
        <Document FilePath=""Program2.cs"">
class Program2
{
}
        </Document>
    </Project>
</Workspace>",
                codeActionTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.WithDocumentText(document1.Id, SourceText.From("NewProgram1Content1"));
                },
                intermediaryTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.WithDocumentText(document1.Id, SourceText.From("NewProgram1Content2"));
                });
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1419139")]
        public async Task TestMakeTextChangeWithInterveningRemovalOfThatFile()
        {
            // This should fail as the code action is trying to edit a file that is subsequently removed.
            await TestFailureApplicationAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Program1.cs"">
class Program1
{
}
        </Document>
        <Document FilePath=""Program2.cs"">
class Program2
{
}
        </Document>
    </Project>
</Workspace>",
                codeActionTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.WithDocumentText(document1.Id, SourceText.From("NewProgram1Content1"));
                },
                intermediaryTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.RemoveDocument(document1.Id);
                });
        }

        [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1419139")]
        public async Task TestMakeProjectChangeWithInterveningTextEdit()
        {
            // This should fail as we don't want to make non-text changes that may have undesirable results to the solution
            // given the intervening edits.
            await TestFailureApplicationAsync(
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Program1.cs"">
class Program1
{
}
        </Document>
        <Document FilePath=""Program2.cs"">
class Program2
{
}
        </Document>
    </Project>
</Workspace>",
                codeActionTransform: solution =>
                {
                    var document1 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program1"));
                    return solution.RemoveDocument(document1.Id);
                },
                intermediaryTransform: solution =>
                {
                    var document2 = solution.Projects.Single().Documents.Single(d => d.FilePath!.Contains("Program2"));
                    return solution.WithDocumentText(document2.Id, SourceText.From("NewProgram1Content2"));
                });
        }

        private async Task TestSuccessfulApplicationAsync(
            string workspaceXml,
            Func<Solution, Solution> codeActionTransform,
            Func<Solution, Solution> intermediaryTransform)
        {
            await TestApplicationAsync(workspaceXml, codeActionTransform, intermediaryTransform, success: true);
        }

        private async Task TestFailureApplicationAsync(
            string workspaceXml,
            Func<Solution, Solution> codeActionTransform,
            Func<Solution, Solution> intermediaryTransform)
        {
            await TestApplicationAsync(workspaceXml, codeActionTransform, intermediaryTransform, success: false);
        }

        private async Task TestApplicationAsync(
            string workspaceXml,
            Func<Solution, Solution> codeActionTransform,
            Func<Solution, Solution> intermediaryTransform,
            bool success)
        {
            var parameters = new TestParameters(fixProviderData: codeActionTransform);
            using var workspace = CreateWorkspaceFromOptions(workspaceXml, parameters);

            var originalSolution = workspace.CurrentSolution;

            var document = GetDocument(workspace);
            var provider = CreateCodeRefactoringProvider(workspace, parameters);

            var refactorings = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, new TextSpan(), refactorings.Add, CancellationToken.None);

            // Compute refactorings based on the original solution.
            await provider.ComputeRefactoringsAsync(context);
            var action = refactorings.Single();
            var operations = await action.GetOperationsAsync(CancellationToken.None);
            var operation = operations.Single();

            // Now make an intermediary edit to the workspace that is applied back in.
            var changedSolution = intermediaryTransform(originalSolution);
            Assert.True(workspace.TryApplyChanges(changedSolution));

            // Now try to apply the refactoring, even though an intervening edit happened.
            var result = await operation.TryApplyAsync(workspace, originalSolution, CodeAnalysisProgress.None, CancellationToken.None);

            Assert.Equal(success, result);
        }
    }
}
