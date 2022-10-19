// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;
using static Microsoft.CodeAnalysis.UnitTests.SolutionUtilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class ProjectSemanticVersionTests
    {
        [Fact]
        public async Task AddingDocumentWithNewClassChangesVersion()
        {
            using var workspace = CreateWorkspace();
            var project = AddEmptyProject(workspace.CurrentSolution);

            await AssertSemanticVersionChangedAsync(project, project.AddDocument("Hello.cs", "class C { }").Project);
        }

        [Fact]
        public async Task RemovingDocumentWithNewClassChangesVersion()
        {
            using var workspace = CreateWorkspace();
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { }").Project;

            await AssertSemanticVersionChangedAsync(project, project.RemoveDocument(project.DocumentIds.Single()));
        }

        [Fact]
        public async Task AddingMethodChangesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { }");
            var text = await document.GetTextAsync();
            var position = text.ToString().LastIndexOf('{') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "public async Task M() { }")).Project);
        }

        [Fact]
        public async Task ChangingMethodPreservesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { void M() { } }");
            var text = await document.GetTextAsync();
            var position = text.ToString().LastIndexOf('{') + 1;

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "int x = 10;")).Project);
        }

        [Fact]
        public async Task ChangingMethodSignatureChangesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { void M() { } }");
            var text = await document.GetTextAsync();
            var position = text.ToString().LastIndexOf('(') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "int x = 10")).Project);
        }

        [Fact]
        public async Task AddingWhitespacePreservesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { void M() { } }");
            var text = await document.GetTextAsync();
            var position = text.ToString().IndexOf('{') + 1;

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "     \r\n")).Project);
        }

        [Fact]
        public async Task AddingFieldWithInitializerChangesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { }");
            var text = await document.GetTextAsync();
            var position = text.ToString().IndexOf('{') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "public int X = 20;")).Project);
        }

        [Fact]
        public async Task ChangingFieldInitializerPreservesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { public int X = 20; }");
            var text = await document.GetTextAsync();
            var span = new TextSpan(text.ToString().IndexOf("20"), length: 2);

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(span, "100")).Project);
        }

        [Fact]
        public async Task AddingConstantChangesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { }");
            var text = await document.GetTextAsync();
            var position = text.ToString().IndexOf('{') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "public const int X = 20;")).Project);
        }

        [Fact]
        public async Task ChangingConstantInitializerChangesSemanticVersion_CSharp()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { public const int X = 20; }");
            var text = await document.GetTextAsync();
            var span = new TextSpan(text.ToString().IndexOf("20"), length: 2);

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(span, "100")).Project);
        }

        [Fact]
        public async Task AddingMethodChangesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\n\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.Lines[1].Start;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "Public Sub M()\r\nEnd Sub")).Project);
        }

        [Fact]
        public async Task ChangingMethodPreservesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\nSub M()\r\n\r\nEnd Sub\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.Lines[2].Start;

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "Dim x As Integer = 10")).Project);
        }

        [Fact]
        public async Task ChangingMethodSignatureChangesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\nSub M()\r\n\r\nEnd Sub\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.ToString().IndexOf('(') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "Optional x As Integer = 10")).Project);
        }

        [Fact]
        public async Task AddingWhitespacePreservesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\n\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.Lines[1].Start;

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "     \r\n")).Project);
        }

        [Fact]
        public async Task AddingFieldWithInitializerChangesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\n\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.Lines[1].Start;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "Public X As Integer = 20")).Project);
        }

        [Fact]
        public async Task ChangingFieldInitializerPreservesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\nPublic X As Integer = 20\r\nEnd Class");
            var text = await document.GetTextAsync();
            var span = new TextSpan(text.ToString().IndexOf("20"), length: 2);

            await AssertSemanticVersionUnchangedAsync(
                document.Project,
                document.WithText(text.Replace(span, "100")).Project);
        }

        [Fact]
        public async Task AddingConstantChangesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\n\r\nEnd Class");
            var text = await document.GetTextAsync();
            var position = text.ToString().IndexOf('{') + 1;

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(position, length: 0, "Public Const X As Integer = 20")).Project);
        }

        [Fact]
        public async Task ChangingConstantInitializerChangesSemanticVersion_VisualBasic()
        {
            using var workspace = CreateWorkspace();
            var document = AddEmptyProject(workspace.CurrentSolution, LanguageNames.VisualBasic)
                .AddDocument("Hello.vb", "Class C\r\nPublic Const X As Integer = 20\r\nEnd Class");
            var text = await document.GetTextAsync();
            var span = new TextSpan(text.ToString().IndexOf("20"), length: 2);

            await AssertSemanticVersionChangedAsync(
                document.Project,
                document.WithText(text.Replace(span, "100")).Project);
        }

        private static async Task AssertSemanticVersionChangedAsync(Project project1, Project project2)
        {
            Assert.NotEqual(await project1.GetSemanticVersionAsync(), await project2.GetSemanticVersionAsync());
        }

        private static async Task AssertSemanticVersionUnchangedAsync(Project project1, Project project2)
        {
            Assert.Equal(await project1.GetSemanticVersionAsync(), await project2.GetSemanticVersionAsync());
        }
    }
}
