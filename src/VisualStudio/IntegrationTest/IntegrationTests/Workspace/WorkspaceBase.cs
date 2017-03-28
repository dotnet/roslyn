// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Options;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    public abstract class WorkspaceBase : AbstractEditorTest
    {
        public WorkspaceBase(VisualStudioInstanceFactory instanceFactory, string projectTemplate)
            : base(instanceFactory, nameof(WorkspaceBase), projectTemplate)
        {
            DefaultProjectTemplate = projectTemplate;
            this.SetFullSolutionAnalysis(true);
        }

        protected override string LanguageName => LanguageNames.CSharp;

        protected string DefaultProjectTemplate { get; }

        public virtual void OpenCSharpThenVBSolution()
        {
            Editor.SetText(@"using System; class Program { Exception e; }");
            this.PlaceCaret("Exception");
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.CloseSolution();
            this.CreateSolution(nameof(WorkspacesDesktop));
            var testProj = new ProjectUtils.Project("TestProj");
            this.AddProject(WellKnownProjectTemplates.ClassLibrary, project: testProj, languageName: LanguageNames.VisualBasic);
            Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            this.PlaceCaret("Exception");
            this.VerifyCurrentTokenType(tokenType: "class name");
        }

        public virtual void MetadataReference()
        {
            var windowsBase = new ProjectUtils.AssemblyReference("WindowsBase");
            var project = new ProjectUtils.Project(ProjectName);
            this.AddMetadataReference(windowsBase, project);
            Editor.SetText("class C { System.Windows.Point p; }");
            this.PlaceCaret("Point");
            this.VerifyCurrentTokenType("struct name");
            this.RemoveMetadataReference(windowsBase, project);
            this.VerifyCurrentTokenType("identifier");
        }

        public virtual void ProjectReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProj2 = new ProjectUtils.Project("CSProj2");
            this.AddProject(project: csProj2, projectTemplate: DefaultProjectTemplate, languageName: LanguageName);
            var projectName = new ProjectUtils.ProjectReference(ProjectName);
            this.AddProjectReference(fromProjectName: csProj2, toProjectName: projectName);
            this.AddFile("Program.cs", project: project, open: true, contents: "public class Class1 { }");
            this.AddFile("Program.cs", project: csProj2, open: true, contents: "public class Class2 { Class1 c; }");
            this.OpenFile("Program.cs", project: csProj2);
            this.PlaceCaret("Class1");
            this.VerifyCurrentTokenType("class name");
            this.RemoveProjectReference(projectReferenceName: projectName, projectName: csProj2);
            this.VerifyCurrentTokenType("identifier");
        }

        public virtual void ProjectProperties()
        {
            Editor.SetText(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            this.PlaceCaret("(x)", charsOffset: -1);
            this.SetQuickInfo(true);
            var project = new ProjectUtils.Project(ProjectName);
            this.SetOptionInfer(project, true);
            this.InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Integer‎)‎ ‎(‎+‎ 1‎ overload‎)", Editor.GetQuickInfo());
            this.SetOptionInfer(project, false);
            this.InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Object‎)‎ ‎(‎+‎ 1‎ overload‎)", Editor.GetQuickInfo());
        }
    }
}
