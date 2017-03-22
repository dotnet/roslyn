// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;


namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    public abstract class WorkspaceBase : AbstractEditorTest
    {
        public WorkspaceBase(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            EnableFullSolutionAnalysis();
        }
        public void OpenCSharpThenVBSolutionCommon()
        {
            Editor.SetText(@"using System; class Program { Exception e; }");
            PlaceCaret("Exception");
            VerifyCurrentTokenType(tokenType: "class name");
            CloseSolution();
            CreateSolution(nameof(WorkspacesDesktop));
            AddProject(WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic);
            Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            PlaceCaret("Exception");
            VerifyCurrentTokenType(tokenType: "class name");
        }

        public void MetadataReferenceCommon()
        {
            var windowsBase = new ProjectUtils.AssemblyReference("WindowsBase");
            AddMetadataReference(windowsBase);
            Editor.SetText("class C { System.Windows.Point p; }");
            PlaceCaret("Point");
            VerifyCurrentTokenType("struct name");
            RemoveMetadataReference(windowsBase);
            VerifyCurrentTokenType("identifier");
        }

        public void ProjectReferenceCommon(string projectTemplate)
        {
            AddProject(projectName: "CSProj2", projectTemplate: projectTemplate);
            var csProj2 = new ProjectUtils.Project("CSProj2");
            var projectName = new ProjectUtils.ProjectReference(ProjectName);
            AddProjectReference(fromProjectName: csProj2, toProjectName: projectName);
            AddFile("Program.cs", open: true, contents: "public class Class1 { }");
            AddFile("Program.cs", projectName: "CSProj2", open: true, contents: "public class Class2 { Class1 c; }");
            OpenFile("Program.cs", projectName: "CSProj2");
            PlaceCaret("Class1");
            VerifyCurrentTokenType("class name");
            RemoveProjectReference(projectReferenceName: projectName, projectName: csProj2);
            VerifyCurrentTokenType("identifier");
        }

        public void ProjectPropertiesCommon()
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
            PlaceCaret("(x)", charsOffset: -1);
            EnableQuickInfo();
            EnableOptionInfer();
            InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Integer‎)‎ ‎(‎+‎ 1‎ overload‎)", Editor.GetQuickInfo());
            DisableOptionInfer();
            InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Object‎)‎ ‎(‎+‎ 1‎ overload‎)", Editor.GetQuickInfo());
        }
    }
}
