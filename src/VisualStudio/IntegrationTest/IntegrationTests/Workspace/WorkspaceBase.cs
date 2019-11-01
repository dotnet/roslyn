// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

#pragma warning disable xUnit1013 // currently there are public virtual methods that are overridden by derived types

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    public abstract class WorkspaceBase : AbstractEditorTest
    {
        public WorkspaceBase(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper, string projectTemplate, string targetFrameworkMoniker = null)
            : base(instanceFactory, testOutputHelper, nameof(WorkspaceBase), projectTemplate, targetFrameworkMoniker)
        {
            DefaultProjectTemplate = projectTemplate;
            TargetFrameworkMoniker = targetFrameworkMoniker;
        }

        protected override string LanguageName => LanguageNames.CSharp;

        protected string DefaultProjectTemplate { get; }
        protected string TargetFrameworkMoniker { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.Workspace.SetFullSolutionAnalysis(true);
        }

        public virtual void OpenCSharpThenVBSolution()
        {
            VisualStudio.Editor.SetText(@"using System; class Program { Exception e; }");
            VisualStudio.Editor.PlaceCaret("Exception");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.SolutionExplorer.CloseSolution();
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic);
            VisualStudio.SolutionExplorer.RestoreNuGetPackages(testProj);
            VisualStudio.Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            VisualStudio.Editor.PlaceCaret("Exception");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }

        public virtual void MetadataReference()
        {
            var windowsBase = new ProjectUtils.AssemblyReference("WindowsBase");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddMetadataReference(windowsBase, project);
            VisualStudio.Editor.SetText("class C { System.Windows.Point p; }");
            VisualStudio.Editor.PlaceCaret("Point");
            VisualStudio.Editor.Verify.CurrentTokenType("struct name");
            VisualStudio.SolutionExplorer.RemoveMetadataReference(windowsBase, project);
            VisualStudio.Editor.Verify.CurrentTokenType("identifier");
        }

        public virtual void ProjectReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProj2 = new ProjectUtils.Project("CSProj2");
            VisualStudio.SolutionExplorer.AddProject(csProj2, projectTemplate: DefaultProjectTemplate, languageName: LanguageName);

            if (!string.IsNullOrEmpty(TargetFrameworkMoniker))
            {
                UpdateProjectTargetFramework(csProj2, TargetFrameworkMoniker);
            }

            var projectName = new ProjectUtils.ProjectReference(ProjectName);
            VisualStudio.SolutionExplorer.AddProjectReference(fromProjectName: csProj2, toProjectName: projectName);
            VisualStudio.SolutionExplorer.RestoreNuGetPackages(csProj2);
            VisualStudio.SolutionExplorer.AddFile(project, "Program.cs", open: true, contents: "public class Class1 { }");
            VisualStudio.SolutionExplorer.AddFile(csProj2, "Program.cs", open: true, contents: "public class Class2 { Class1 c; }");
            VisualStudio.SolutionExplorer.OpenFile(csProj2, "Program.cs");
            VisualStudio.Editor.PlaceCaret("Class1");
            VisualStudio.Editor.Verify.CurrentTokenType("class name");
            VisualStudio.SolutionExplorer.RemoveProjectReference(projectReferenceName: projectName, projectName: csProj2);
            VisualStudio.Editor.Verify.CurrentTokenType("identifier");
        }

        public virtual void ProjectProperties()
        {
            VisualStudio.Editor.SetText(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            VisualStudio.Editor.PlaceCaret("(x)", charsOffset: -1);
            VisualStudio.Workspace.SetQuickInfo(true);
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.Workspace.SetOptionInfer(project.Name, true);
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("Sub Program.M(p As Integer) (+ 1 overload)", VisualStudio.Editor.GetQuickInfo());
            VisualStudio.Workspace.SetOptionInfer(project.Name, false);
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("Sub Program.M(p As Object) (+ 1 overload)", VisualStudio.Editor.GetQuickInfo());
        }

        [WpfFact]
        public void RenamingOpenFiles()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "BeforeRename.cs", open: true);

            // Verify we are connected to the project before...
            Assert.Contains(ProjectName, VisualStudio.Editor.GetProjectNavBarItems());

            VisualStudio.SolutionExplorer.RenameFile(project, "BeforeRename.cs", "AfterRename.cs");

            // ...and after.
            Assert.Contains(ProjectName, VisualStudio.Editor.GetProjectNavBarItems());
        }

        [WpfFact]
        public virtual void RenamingOpenFilesViaDTE()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "BeforeRename.cs", open: true);

            // Verify we are connected to the project before...
            Assert.Contains(ProjectName, VisualStudio.Editor.GetProjectNavBarItems());

            VisualStudio.SolutionExplorer.RenameFileViaDTE(project, "BeforeRename.cs", "AfterRename.cs");

            // ...and after.
            Assert.Contains(ProjectName, VisualStudio.Editor.GetProjectNavBarItems());
        }
    }
}
