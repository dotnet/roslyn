// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicEditAndContinue : AbstractEditorTest
    {
        private const string module1FileName = "Module1.vb";

        public BasicEditAndContinue() : base(nameof(BasicEditAndContinue))
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void UpdateActiveStatementLeafNode()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main()
        Dim names(2) As String
        names(0) = ""goo""
        names(1) = ""bar""

        For index = 0 To names.GetUpperBound(0)
            Console.WriteLine(names(index))
        Next
    End Sub
End Module
");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "names(0)");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("names(0)", "names(1)");
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudioInstance.Debugger.CheckExpression("names(1)", "String", "\"goo\"");
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudioInstance.Debugger.CheckExpression("names(1)", "String", "\"bar\"");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void AddTryCatchAroundActiveStatement()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Goo()
    End Sub

    Private Sub Goo()
        Console.WriteLine(1)
    End Sub
End Module");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "Console.WriteLine(1)");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("Console.WriteLine(1)",
                @"Try
Console.WriteLine(1)
Catch ex As Exception
End Try");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudioInstance.Editor.Verify.CurrentLineText("End Try");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void EditLambdaExpression()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Module Module1
    Private Delegate Function del(i As Integer) As Integer

    Sub Main()
        Dim myDel As del = Function(x) x * x
        Dim j As Integer = myDel(5)
    End Sub
End Module");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "x * x", charsOffset: -1);

            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("x * x", "x * 2");

            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: false);
            VisualStudioInstance.Debugger.Stop(waitForDesignMode: true);
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();

            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("x * 2", "x * x");
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudioInstance.Debugger.Stop(waitForDesignMode: true);
            VisualStudioInstance.ErrorList.Verify.NoBuildErrors();
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void EnCWhileDebuggingFromImmediateWindow()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System

Module Module1
    Sub Main()
        Dim x = 4
        Console.WriteLine(x)
    End Sub
End Module");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "Dim x", charsOffset: 1);
            VisualStudioInstance.Debugger.ExecuteStatement("Module1.Main()");
            VisualStudioInstance.Editor.ReplaceText("x = 4", "x = 42");
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudioInstance.Debugger.CheckExpression("x", "Integer", "42");
            VisualStudioInstance.Debugger.ExecuteStatement("Module1.Main()");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        private void SetupMultiProjectSolution()
        {
            var basicLibrary = new ProjectUtils.Project("BasicLibrary1");
            VisualStudioInstance.SolutionExplorer.AddProject(basicLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            var cSharpLibrary = new ProjectUtils.Project("CSharpLibrary1");
            VisualStudioInstance.SolutionExplorer.AddProject(cSharpLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudioInstance.SolutionExplorer.AddFile(cSharpLibrary, "File1.cs");

            VisualStudioInstance.SolutionExplorer.OpenFile(basicLibrary, "Class1.vb");
            VisualStudioInstance.Editor.SetText(@"
Imports System
Public Class Class1
    Public Sub New()
    End Sub

    Public Sub PrintX(x As Integer)
        Console.WriteLine(x)
    End Sub
End Class
");

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProjectReference(project, new ProjectUtils.ProjectReference("BasicLibrary1"));
            VisualStudioInstance.SolutionExplorer.OpenFile(project, module1FileName);

            VisualStudioInstance.Editor.SetText(@"
Imports System
Imports BasicLibrary1

Module Module1
    Sub Main()
        Dim c As New Class1()
        c.PrintX(5)
    End Sub
End Module
");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void MultiProjectDebuggingWhereNotAllModulesAreLoaded()
        {
            SetupMultiProjectSolution();
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "PrintX", charsOffset: 1);
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("5", "42");
            VisualStudioInstance.Debugger.StepOver(waitForBreakOrEnd: false);
            VisualStudioInstance.ErrorList.Verify.NoErrors();
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void DocumentStateTrackingReadonlyInRunMode()
        {
            SetupMultiProjectSolution();
            var project = new ProjectUtils.Project(ProjectName);
            var basicLibrary = new ProjectUtils.Project("BasicLibrary1");
            var cSharpLibrary = new ProjectUtils.Project("CSharpLibrary1");

            VisualStudioInstance.Editor.SetText(@"
Imports System
Imports BasicLibrary1
Module Module1
    Sub Main()
        Console.Read()
    End Sub
End Module
");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.Go(waitForBreakMode: false);
            VisualStudioInstance.ActivateMainWindow();
            VisualStudioInstance.SolutionExplorer.OpenFile(project, module1FileName);

            VisualStudioInstance.SendKeys.Send(VirtualKey.T);
            string editAndContinueDialogName = "Edit and Continue";
            VisualStudioInstance.Dialog.VerifyOpen(editAndContinueDialogName);
            VisualStudioInstance.Dialog.Click(editAndContinueDialogName, "OK");
            VisualStudioInstance.Dialog.VerifyClosed(editAndContinueDialogName);
            VisualStudioInstance.Editor.Verify.IsProjectItemDirty(expectedValue: false);

            // This module is referred by the loaded module, but not used. So this will not be loaded
            VisualStudioInstance.SolutionExplorer.OpenFile(basicLibrary, "Class1.vb");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.SendKeys.Send(VirtualKey.T);
            VisualStudioInstance.Dialog.VerifyOpen(editAndContinueDialogName);
            VisualStudioInstance.Dialog.Click(editAndContinueDialogName, "OK");
            VisualStudioInstance.Dialog.VerifyClosed(editAndContinueDialogName);
            VisualStudioInstance.Editor.Verify.IsProjectItemDirty(expectedValue: false);

            //  This module is not referred by the loaded module. this will not be loaded
            VisualStudioInstance.SolutionExplorer.OpenFile(cSharpLibrary, "File1.cs");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.SendKeys.Send(VirtualKey.T);

            string microsoftVisualStudioInstanceDialogName = "Microsoft Visual Studio";
            VisualStudioInstance.Dialog.VerifyOpen(microsoftVisualStudioInstanceDialogName);
            VisualStudioInstance.Dialog.Click(microsoftVisualStudioInstanceDialogName, "OK");
            VisualStudioInstance.Dialog.VerifyClosed(microsoftVisualStudioInstanceDialogName);
            VisualStudioInstance.Editor.Verify.IsProjectItemDirty(expectedValue: false);
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void LocalsWindowUpdatesAfterLocalGetsItsTypeUpdatedDuringEnC()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Dim goo As String = ""abc""
        Console.WriteLine(goo)
    End Sub
End Module
");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "End Sub");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("Dim goo As String = \"abc\"", "Dim goo As Single = 10");
            VisualStudioInstance.Editor.SelectTextInCurrentDocument("Sub Main()");
            VisualStudioInstance.Debugger.SetNextStatement();
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("goo", "Single", "10");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void LocalsWindowUpdatesCorrectlyDuringEnC()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System

Module Module1
    Sub Main()
        bar(5)
    End Sub

    Function bar(ByVal moo As Long) As Decimal
        Dim iInt As Integer = 0
        Dim lLng As Long = 5
        
        iInt += 30
        Return 4
    End Function
End Module
");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "Function bar(ByVal moo As Long) As Decimal");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Editor.ReplaceText("Dim lLng As Long = 5", "Dim lLng As Long = 444");
            VisualStudioInstance.Debugger.SetBreakPoint(module1FileName, "Return 4");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("bar", "Decimal", "0");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("moo", "Long", "5");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("iInt", "Integer", "30");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("lLng", "Long", "444");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/21925")]
        public void WatchWindowUpdatesCorrectlyDuringEnC()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System

Module Module1
    Sub Main()
        Dim iInt As Integer = 0
        System.Diagnostics.Debugger.Break()
    End Sub
End Module
");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            VisualStudioInstance.Debugger.CheckExpression("iInt", "Integer", "0");

            VisualStudioInstance.Editor.ReplaceText("System.Diagnostics.Debugger.Break()", @"iInt = 5
System.Diagnostics.Debugger.Break()");

            VisualStudioInstance.Editor.SelectTextInCurrentDocument("iInt = 5");
            VisualStudioInstance.Debugger.SetNextStatement();
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            VisualStudioInstance.Debugger.CheckExpression("iInt", "Integer", "5");
        }
    }
}
