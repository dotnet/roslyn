// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    public abstract class WorkspaceBase : AbstractIdeEditorTest
    {
        protected WorkspaceBase(string projectTemplate)
            : base(nameof(WorkspaceBase), projectTemplate)
        {
            DefaultProjectTemplate = projectTemplate;
        }

        protected override string LanguageName => LanguageNames.CSharp;

        protected string DefaultProjectTemplate { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await VisualStudio.Workspace.SetFullSolutionAnalysisAsync(true);
        }

        public override async Task DisposeAsync()
        {
            await VisualStudio.Workspace.SetFullSolutionAnalysisAsync(false);
            await base.DisposeAsync();
        }

        public virtual async Task OpenCSharpThenVBSolutionAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"using System; class Program { Exception e; }");
            await VisualStudio.Editor.PlaceCaretAsync("Exception");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.SolutionExplorer.CloseSolutionAsync();
            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(WorkspacesDesktop));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic);
            await VisualStudio.SolutionExplorer.RestoreNuGetPackagesAsync("TestProj", HangMitigatingCancellationToken);
            await VisualStudio.Editor.SetTextAsync(@"Imports System
Class Program
    Private e As Exception
End Class");
            await VisualStudio.Editor.PlaceCaretAsync("Exception");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
        }

        public virtual async Task MetadataReferenceAsync()
        {
            var windowsBase = "WindowsBase";
            var project = ProjectName;
            await VisualStudio.SolutionExplorer.AddMetadataReferenceAsync(windowsBase, project);
            await VisualStudio.Editor.SetTextAsync("class C { System.Windows.Point p; }");
            await VisualStudio.Editor.PlaceCaretAsync("Point");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync("struct name");
            await VisualStudio.SolutionExplorer.RemoveMetadataReferenceAsync(windowsBase, project);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync("identifier");
        }

        public virtual async Task ProjectReferenceAsync()
        {
            var project = ProjectName;
            var csProj2 = "CSProj2";
            var projectName = ProjectName;
            await VisualStudio.SolutionExplorer.AddProjectAsync(csProj2, projectTemplate: DefaultProjectTemplate, languageName: LanguageName);
            await VisualStudio.SolutionExplorer.AddProjectReferenceAsync(projectName: csProj2, projectToReferenceName: projectName);
            await VisualStudio.SolutionExplorer.RestoreNuGetPackagesAsync(csProj2, HangMitigatingCancellationToken);
            await VisualStudio.SolutionExplorer.AddFileAsync(project, "Program.cs", open: true, contents: "public class Class1 { }");
            await VisualStudio.SolutionExplorer.AddFileAsync(csProj2, "Program.cs", open: true, contents: "public class Class2 { Class1 c; }");
            await VisualStudio.SolutionExplorer.OpenFileAsync(csProj2, "Program.cs");
            await VisualStudio.Editor.PlaceCaretAsync("Class1");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync("class name");
            await VisualStudio.SolutionExplorer.RemoveProjectReferenceAsync(projectReferenceName: projectName, projectName: csProj2);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync("identifier");
        }

        public virtual async Task ProjectPropertiesAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            await VisualStudio.Editor.PlaceCaretAsync("(x)", charsOffset: -1);
            await VisualStudio.Workspace.EnableQuickInfoAsync(true);
            await VisualStudio.Workspace.SetOptionInferAsync(ProjectName, true);
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Integer‎)‎ ‎(‎+‎ 1‎ overload‎)", await VisualStudio.Editor.GetQuickInfoAsync());
            await VisualStudio.Workspace.SetOptionInferAsync(ProjectName, false);
            await VisualStudio.Editor.InvokeQuickInfoAsync();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Object‎)‎ ‎(‎+‎ 1‎ overload‎)", await VisualStudio.Editor.GetQuickInfoAsync());
        }

        public virtual async Task RenamingOpenFilesAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "BeforeRename.cs", open: true);
            
            // Verify we are connected to the project before...
            Assert.Contains(ProjectName, await VisualStudio.Editor.GetProjectNavBarItemsAsync());

            VisualStudio.SolutionExplorer.RenameFile(ProjectName, "BeforeRename.cs", "AfterRename.cs");

            // ...and after.
            Assert.Contains(ProjectName, await VisualStudio.Editor.GetProjectNavBarItemsAsync());
        }
    }
}
