// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    public abstract class WorkspaceBase : AbstractEditorTest
    {
        public WorkspaceBase(string projectTemplate)
            : base(nameof(WorkspaceBase), projectTemplate)
        {
            DefaultProjectTemplate = projectTemplate;
        }

        protected override string LanguageName => LanguageNames.CSharp;

        protected string DefaultProjectTemplate { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudioInstance.Workspace.SetFullSolutionAnalysis(true);
        }

        public virtual void OpenCSharpThenVBSolution()
        {
            VisualStudioInstance.Editor.SetText(@"using System; class Program { Exception e; }");
            VisualStudioInstance.Editor.PlaceCaret("Exception");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.SolutionExplorer.CloseSolution();
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic);
            VisualStudioInstance.SolutionExplorer.RestoreNuGetPackages(testProj);
            VisualStudioInstance.Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            VisualStudioInstance.Editor.PlaceCaret("Exception");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }

        public virtual void MetadataReference()
        {
            var windowsBase = new ProjectUtils.AssemblyReference("WindowsBase");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddMetadataReference(windowsBase, project);
            VisualStudioInstance.Editor.SetText("class C { System.Windows.Point p; }");
            VisualStudioInstance.Editor.PlaceCaret("Point");
            VisualStudioInstance.Editor.Verify.CurrentTokenType("struct name");
            VisualStudioInstance.SolutionExplorer.RemoveMetadataReference(windowsBase, project);
            VisualStudioInstance.Editor.Verify.CurrentTokenType("identifier");
        }

        public virtual void ProjectReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProj2 = new ProjectUtils.Project("CSProj2");
            VisualStudioInstance.SolutionExplorer.AddProject(csProj2, projectTemplate: DefaultProjectTemplate, languageName: LanguageName);
            var projectName = new ProjectUtils.ProjectReference(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProjectReference(fromProjectName: csProj2, toProjectName: projectName);
            VisualStudioInstance.SolutionExplorer.RestoreNuGetPackages(csProj2);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "Program.cs", open: true, contents: "public class Class1 { }");
            VisualStudioInstance.SolutionExplorer.AddFile(csProj2, "Program.cs", open: true, contents: "public class Class2 { Class1 c; }");
            VisualStudioInstance.SolutionExplorer.OpenFile(csProj2, "Program.cs");
            VisualStudioInstance.Editor.PlaceCaret("Class1");
            VisualStudioInstance.Editor.Verify.CurrentTokenType("class name");
            VisualStudioInstance.SolutionExplorer.RemoveProjectReference(projectReferenceName: projectName, projectName: csProj2);
            VisualStudioInstance.Editor.Verify.CurrentTokenType("identifier");
        }

        public virtual void ProjectProperties()
        {
            VisualStudioInstance.Editor.SetText(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            VisualStudioInstance.Editor.PlaceCaret("(x)", charsOffset: -1);
            VisualStudioInstance.Workspace.SetQuickInfo(true);
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.Workspace.SetOptionInfer(project.Name, true);
            VisualStudioInstance.Editor.InvokeQuickInfo();
            Assert.AreEqual("Sub Program.M(p As Integer) (+ 1 overload)", VisualStudioInstance.Editor.GetQuickInfo());
            VisualStudioInstance.Workspace.SetOptionInfer(project.Name, false);
            VisualStudioInstance.Editor.InvokeQuickInfo();
            Assert.AreEqual("Sub Program.M(p As Object) (+ 1 overload)", VisualStudioInstance.Editor.GetQuickInfo());
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/30599")]
        public void RenamingOpenFiles()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "BeforeRename.cs", open: true);

            // Verify we are connected to the project before...
            ExtendedAssert.Contains(ProjectName, VisualStudioInstance.Editor.GetProjectNavBarItems());

            VisualStudioInstance.SolutionExplorer.RenameFile(project, "BeforeRename.cs", "AfterRename.cs");

            // ...and after.
            ExtendedAssert.Contains(ProjectName, VisualStudioInstance.Editor.GetProjectNavBarItems());
        }
    }
}
