// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEditAndContinue : AbstractEditorTest
    {
        public BasicEditAndContinue(VisualStudioInstanceFactory instanceFactory) : base(instanceFactory)
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [Fact]
        public void UpdateActiveStatementLeafNode()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main()
        Dim names(2) As String
        names(0) = ""foo""
        names(1) = ""bar""

        For index = 0 To names.GetUpperBound(0)
            Console.WriteLine(names(index))
        Next
    End Sub
End Module
");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SetBreakPoint("names(0)");
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("names(0)", "names(1)");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.Verify.EvaluateExpression("names(1)", "\"foo\"");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.Verify.EvaluateExpression("names(1)", "\"bar\"");
        }

        // TODO refactor?
        private void SetBreakPoint(string text, int charsOffset = 0)
        {
            VisualStudio.Editor.SelectTextInCurrentDocument(text);
            int lineNumber = VisualStudio.Editor.GetLine();
            int columnIndex = VisualStudio.Editor.GetColumn();

            // TODO is it a correct way to approach charOffset?
            VisualStudio.Debugger.SetBreakPoint("Module1.vb", lineNumber, columnIndex + charsOffset);
        }

        [Fact]
        public void AddTryCatchAroundActiveStatement()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Foo()
    End Sub

    Private Sub Foo()
        Console.WriteLine(1)
    End Sub
End Module");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SetBreakPoint("Console.WriteLine(1)");
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("Console.WriteLine(1)", 
                @"Try
Console.WriteLine(1)
Catch ex As Exception

End Try");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Editor.Verify.CurrentLineText("End Try");
        }

        [Fact]
        public void EditLambdaExpression()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Module Module1
    Private Delegate Function del(i As Integer) As Integer

    Sub Main()
        Dim myDel As del = Function(x) x * x
        Dim j As Integer = myDel(5)
    End Sub
End Module");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SetBreakPoint("x * x", charsOffset: -1);
            
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("x * x", "x * 2");

            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: false);
            VisualStudio.Debugger.Stop(waitForDesignMode: true);
            VisualStudio.ErrorList.Verify.NoBuildErrors();

            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("x * 2", "x * x");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.Stop(waitForDesignMode: true);
            VisualStudio.ErrorList.Verify.NoBuildErrors();
        }

        [Fact]
        public void EnCWhileDebuggingFromImmediateWindow()
        {
            VisualStudio.Editor.SetText(@"
Imports System

Module Module1
    Sub Main()
        Dim x = 4
        Console.WriteLine(x)
    End Sub
End Module");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true); // ?
            SetBreakPoint("Dim x", charsOffset: 1);
            //      <ImmediateWindow>
            //        <TypeText text = "Module1.Main{(}{)}{ENTER}" />
            //      </ ImmediateWindow >
            VisualStudio.ImmediateWindow.ExecuteCommand("Module1.Main{(}{)}");

            //      < WaitForBuild />
            //      < Debug >
            //        < TryWaitForBreakMode />

            VisualStudio.Editor.ReplaceText("x = 4", "x = 42");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);

            VisualStudio.ImmediateWindow.Verify.ValidateCommand("?x", "42");
            VisualStudio.ImmediateWindow.ExecuteCommand("Module1.Main{(}{)}");
        }

        [Fact]
        public void MultiProjectDebuggingWhereNotAllModulesAreLoaded()
        {

            //      <AddProject ProjectName = "CSharpLibrary1" LanguageName="C#" ProjectTemplate="None"/>
            //      <AddItem ProjectName = "CSharpLibrary1" FileName="File1.cs"/>
            //      <AddProject ProjectName = "BasicLibrary1" LanguageName="Visual Basic" ProjectTemplate="None"/>
            //      <AddItem ProjectName = "BasicLibrary1" FileName="Class1.vb"/>

            var basicLibrary1 = new ProjectUtils.Project("BasicLibrary1");
            VisualStudio.SolutionExplorer.OpenFile(basicLibrary1, "Class1.vb");
            VisualStudio.Editor.SetText(@"
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
            VisualStudio.SolutionExplorer.AddProjectReference(project, new ProjectUtils.ProjectReference("BasicLibrary1"));
            VisualStudio.SolutionExplorer.OpenFile(project, "Module1.vb");

            VisualStudio.Editor.SetText(@"
Imports System
Imports BasicLibrary1

Module Module1
    Sub Main()
        Dim c As New Class1()
        c.PrintX(5)
    End Sub
End Module
");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            //      < Debug ExpectModalDialog="false">

            SetBreakPoint("PrintX", charsOffset: 1);
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("5", "42");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: false);

            //      < VerifyErrorList Count="0" />
            VisualStudio.ErrorList.Verify.NoBuildErrors();
        }

        [Fact]
        public void DocumentStateTrackingReadonlyInRunMode()
        {

            //    <!-- Note: this scenario re-uses projects/reference set up created by Multi project debugging scenario.
            //    Ideally, separation between scenarios is preferred, but to save time i'm not unloading and creating 3 projects
            //    and wiring up references. The scenario starts by resetting code, so hopefully it is clean enough. -->
            //    <Scenario Name = "Document state tracking: readonly in run mode" >
            //      < OpenFile FileName= "Module1.vb" />
            //      < SetEditorText >
            //        < ![CDATA[Imports System
            //Imports BasicLibrary1

            //Module Module1
            //    Sub Main()
            //        Console.Read()
            //    End Sub
            //End Module]] >
            //      </ SetEditorText >
            //      < WaitForWorkspace />
            //      < !--this module is loaded but one cannot edit while in run mode-->
            //      <Debug ExpectModalDialog = "true" >
            //        < StartDebugging waitForBreakMode= "false" />
            //        < TypeChars text= "t" />
            //      </ Debug >
            //      < VerifyActiveDocumentWindow >
            //        < IsDirty value= "false" />
            //      </ VerifyActiveDocumentWindow >
            //      < !--This module is referred by the loaded module, but not used.So this will not be loaded-->
            //      <OpenFile FileName = "Class1.vb" />
            //      < WaitForWorkspace />
            //      < Debug ExpectModalDialog= "true" >
            //        < TypeChars text= "t" />
            //      </ Debug >
            //      < VerifyActiveDocumentWindow >
            //        < IsDirty value= "false" />
            //      </ VerifyActiveDocumentWindow >
            //      < !--This module is not referred by the loaded module. this will not be loaded-->
            //      <OpenFile FileName = "File1.cs" />
            //      < WaitForWorkspace />
            //      < Debug ExpectModalDialog= "true" >
            //        < TypeChars text= "t" />
            //      </ Debug >
            //      < VerifyActiveDocumentWindow >
            //        < IsDirty value= "false" />
            //      </ VerifyActiveDocumentWindow >
            //    </ Scenario >
        }

        [Fact]
        // BUG: 1048346
        public void LocalsWindowUpdatesAfterLocalGetsItsTypeUpdatedDuringEnC()
        {
            //      < OpenFile FileName= "Module1.vb" />
            VisualStudio.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Dim foo As String = ""abc""
        Console.WriteLine(foo)
    End Sub
End Module
");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);

            SetBreakPoint("End Sub");
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);

            VisualStudio.Editor.ReplaceText("Dim foo As String = \"abc\"", "Dim foo As Single = 10");


            //        < SetNextStatement code= "Sub Main()" />
            VisualStudio.Editor.SelectTextInCurrentDocument("Sub Main()");
            // placeCursor.Execute(engine);
            VisualStudio.Debugger.SetNextStatement();

            // Single
            VisualStudio.LocalsWindow.Verify.CheckEntry("foo", "10", typeof(float));
        }

        [Fact]
        //    BUG: 1137131
        public void LocalsWindowUpdatesCorrectlyDuringEnC()
        {
            //      < OpenFile FileName= "Module1.vb" />
            VisualStudio.Editor.SetText(@"
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
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            SetBreakPoint("Function bar(ByVal moo As Long) As Decimal");
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);
            VisualStudio.Editor.ReplaceText("Dim lLng As Long = 5", "Dim lLng As Long = 444");
            SetBreakPoint("Return 4");

            //        < Continue />
            // TODO: actually this continue debugging not re-starts it
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);

            VisualStudio.LocalsWindow.Verify.CheckEntry("bar", "0", typeof(decimal));
            VisualStudio.LocalsWindow.Verify.CheckEntry("moo", "5", typeof(long));
            VisualStudio.LocalsWindow.Verify.CheckEntry("iInt", "30", typeof(int));
            VisualStudio.LocalsWindow.Verify.CheckEntry("lLng", "444", typeof(long));
        }

        [Fact]
        // BUG: 1137131 (Attachment)
        public void WatchWindowUpdatesCorrectlyDuringEnC()
        {

            VisualStudio.Editor.SetText(@"
Imports System

Module Module1
    Sub Main()
        Dim iInt As Integer = 0
        System.Diagnostics.Debugger.Break()
    End Sub
End Module
");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Debugger.StartDebugging(waitForBreakMode: true);

            VisualStudio.WatchWindow.AddEntry("iInt");
            VisualStudio.WatchWindow.Verify.CheckEntry("iInt", "0", typeof(int));

            VisualStudio.Editor.ReplaceText("System.Diagnostics.Debugger.Break()", @"iInt = 5
System.Diagnostics.Debugger.Break()");

            //        < SetNextStatement code= "iInt = 5" />
            VisualStudio.Editor.SelectTextInCurrentDocument("iInt = 5");
            // placeCursor.Execute(engine);
            VisualStudio.Debugger.SetNextStatement();

            VisualStudio.WatchWindow.Verify.CheckEntry("iInt", "5", typeof(int));
            VisualStudio.WatchWindow.DeleteAllEntries();
        }

        public new void Dispose()
        {
            //  < CleanupScenario >
            //    < Debug >
            //      < RemoveAllBreakPointsAndStopDebugging />
            //    </ Debug >
            //    < VerifyInstanceCount TypeName= "EditAndContinueSession" Count= "0" Comparison= "Equal" ForceGC= "true" />
            //  </ CleanupScenario >
            //  < CleanupTest >
            //    < CloseTarget >
            //      < LinesToIgnore >
            //        < string > 'DocumentWindowTestExtension' taken ownership of 'DocumentWindowVerifier'...</string>
            //        <string>'WindowManagementService' taken ownership of 'DocumentWindowTestExtension'...</string>
            //        <string>'TextEditorDocumentWindowTestExtension' taken ownership of 'TextEditorDocumentWindowVerifier'...</string>
            //        <string>'WindowManagementService' taken ownership of 'TextEditorDocumentWindowTestExtension'...</string>
            //        <string>'VisualStudioTextEditorTestExtension' taken ownership of 'EditorCommandTarget'...</string>
            //        <string>'VisualStudioTextEditorTestExtension' taken ownership of 'VisualStudioTextEditorVerifier'...</string>
            //        <string>'VisualStudioEditorTestService' taken ownership of 'VisualStudioTextEditorTestExtension'...</string>
            //        <string>'TextEditorDocumentWindowTestExtension' taken ownership of 'VisualStudioTextEditorTestExtension'...</string>
            //        <string>'VisualStudioCaretTestExtension' taken ownership of 'CaretVerifier'...</string>
            //        <string>'VisualStudioTextEditorTestExtension' taken ownership of 'VisualStudioCaretTestExtension'...</string>
            //        <string>'DocumentWindowTestExtension' taken ownership of 'DocumentWindowVerifier'...</string>
            //        <string>'BreakPointTestExtension' taken ownership of 'BreakPointVerifier'...</string>
            //        <string>'DebuggerService' taken ownership of 'BreakPointTestExtension'...</string>
            //        <string>'CommandingService' taken ownership of 'CommandTarget'...</string>
            //        <string>'ToolWindowWithEditorTestExtension' taken ownership of 'ToolWindowVerifier'...</string>
            //        <string>'WindowManagementService' taken ownership of 'ToolWindowWithEditorTestExtension'...</string>
            //        <string>'FrameworkElementTestExtension' taken ownership of 'FrameworkElementVerifier'...</string>
            //        <string>'ToolWindowWithEditorTestExtension' taken ownership of 'FrameworkElementTestExtension'...</string>
            //      </LinesToIgnore>
            //    </CloseTarget>
            //  </CleanupTest>
            //</TaoTest>
            base.Dispose();
        }
    }
}