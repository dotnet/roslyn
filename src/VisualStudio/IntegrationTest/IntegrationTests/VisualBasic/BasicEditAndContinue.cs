// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEditAndContinue : AbstractIdeEditorTest
    {
        private const string module1FileName = "Module1.vb";

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(BasicBuild));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact]
        public async Task UpdateActiveStatementLeafNodeAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
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

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "names(0)");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("names(0)", "names(1)");
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: true);
            await VisualStudio.Debugger.CheckExpressionAsync("names(1)", "String", "\"goo\"");
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: true);
            await VisualStudio.Debugger.CheckExpressionAsync("names(1)", "String", "\"bar\"");
        }

        [IdeFact]
        public async Task AddTryCatchAroundActiveStatementAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Module Module1
    Sub Main()
        Goo()
    End Sub

    Private Sub Goo()
        Console.WriteLine(1)
    End Sub
End Module");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "Console.WriteLine(1)");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("Console.WriteLine(1)",
                @"Try
Console.WriteLine(1)
Catch ex As Exception
End Try");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: true);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("End Try");
        }

        [IdeFact]
        public async Task EditLambdaExpressionAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Module Module1
    Private Delegate Function del(i As Integer) As Integer

    Sub Main()
        Dim myDel As del = Function(x) x * x
        Dim j As Integer = myDel(5)
    End Sub
End Module");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "x * x", charsOffset: -1);

            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("x * x", "x * 2");

            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: false);
            await VisualStudio.Debugger.StopAsync(waitForDesignMode: true);
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();

            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("x * 2", "x * x");
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: true);
            await VisualStudio.Debugger.StopAsync(waitForDesignMode: true);
            await VisualStudio.ErrorList.Verify.NoBuildErrorsAsync();
        }

        [IdeFact]
        public async Task EnCWhileDebuggingFromImmediateWindowAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System

Module Module1
    Sub Main()
        Dim x = 4
        Console.WriteLine(x)
    End Sub
End Module");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "Dim x", charsOffset: 1);
            await VisualStudio.Debugger.ExecuteStatementAsync("Module1.Main()");
            await VisualStudio.Editor.ReplaceTextAsync("x = 4", "x = 42");
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: true);
            await VisualStudio.Debugger.CheckExpressionAsync("x", "Integer", "42");
            await VisualStudio.Debugger.ExecuteStatementAsync("Module1.Main()");
        }

        private async Task SetupMultiProjectSolutionAsync()
        {
            var basicLibrary = "BasicLibrary1";
            await VisualStudio.SolutionExplorer.AddProjectAsync(basicLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            var cSharpLibrary = "CSharpLibrary1";
            await VisualStudio.SolutionExplorer.AddProjectAsync(cSharpLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            await VisualStudio.SolutionExplorer.AddFileAsync(cSharpLibrary, "File1.cs");

            await VisualStudio.SolutionExplorer.OpenFileAsync(basicLibrary, "Class1.vb");
            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Public Class Class1
    Public Sub New()
    End Sub

    Public Sub PrintX(x As Integer)
        Console.WriteLine(x)
    End Sub
End Class
");

            VisualStudio.SolutionExplorer.AddProjectReference(ProjectName, basicLibrary);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, module1FileName);

            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Imports BasicLibrary1

Module Module1
    Sub Main()
        Dim c As New Class1()
        c.PrintX(5)
    End Sub
End Module
");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        [IdeFact]
        public async Task MultiProjectDebuggingWhereNotAllModulesAreLoadedAsync()
        {
            await SetupMultiProjectSolutionAsync();
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "PrintX", charsOffset: 1);
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("5", "42");
            await VisualStudio.Debugger.StepOverAsync(waitForBreakOrEnd: false);
            await VisualStudio.ErrorList.Verify.NoErrorsAsync();
        }

        [IdeFact]
        public async Task DocumentStateTrackingReadonlyInRunModeAsync()
        {
            await SetupMultiProjectSolutionAsync();
            var basicLibrary = "BasicLibrary1";
            var cSharpLibrary = "CSharpLibrary1";

            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Imports BasicLibrary1
Module Module1
    Sub Main()
        Console.Read()
    End Sub
End Module
");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: false);
            await VisualStudio.VisualStudio.ActivateMainWindowAsync(skipAttachingThreads: true);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, module1FileName);

            await VisualStudio.SendKeys.SendAsync(VirtualKey.T);
            var editAndContinueDialogName = "Edit and Continue";
            await VisualStudio.Dialog.VerifyOpenAsync(editAndContinueDialogName);
            await VisualStudio.Dialog.ClickOKAsync(editAndContinueDialogName);
            await VisualStudio.Dialog.VerifyClosedAsync(editAndContinueDialogName);
            await VisualStudio.Editor.Verify.IsProjectItemDirtyAsync(expectedValue: false);

            // This module is referred by the loaded module, but not used. So this will not be loaded
            await VisualStudio.SolutionExplorer.OpenFileAsync(basicLibrary, "Class1.vb");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.T);
            await VisualStudio.Dialog.VerifyOpenAsync(editAndContinueDialogName);
            await VisualStudio.Dialog.ClickOKAsync(editAndContinueDialogName);
            await VisualStudio.Dialog.VerifyClosedAsync(editAndContinueDialogName);
            await VisualStudio.Editor.Verify.IsProjectItemDirtyAsync(expectedValue: false);

            //  This module is not referred by the loaded module. this will not be loaded
            await VisualStudio.SolutionExplorer.OpenFileAsync(cSharpLibrary, "File1.cs");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.T);

            var microsoftVisualStudionDialogName = "Microsoft Visual Studio";
            await VisualStudio.Dialog.VerifyOpenAsync(microsoftVisualStudionDialogName);
            await VisualStudio.Dialog.ClickOKAsync(microsoftVisualStudionDialogName);
            await VisualStudio.Dialog.VerifyClosedAsync(microsoftVisualStudionDialogName);
            await VisualStudio.Editor.Verify.IsProjectItemDirtyAsync(expectedValue: false);
        }

        [IdeFact]
        public async Task LocalsWindowUpdatesAfterLocalGetsItsTypeUpdatedDuringEnCAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Module Module1
    Sub Main()
        Dim goo As String = ""abc""
        Console.WriteLine(goo)
    End Sub
End Module
");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "End Sub");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("Dim goo As String = \"abc\"", "Dim goo As Single = 10");
            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync("Sub Main()");
            await VisualStudio.Debugger.SetNextStatementAsync();
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("goo", "Single", "10");
        }

        [IdeFact]
        public async Task LocalsWindowUpdatesCorrectlyDuringEnCAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
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
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "Function bar(ByVal moo As Long) As Decimal");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Editor.ReplaceTextAsync("Dim lLng As Long = 5", "Dim lLng As Long = 444");
            await VisualStudio.Debugger.SetBreakPointAsync(module1FileName, "Return 4");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("bar", "Decimal", "0");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("moo", "Long", "5");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("iInt", "Integer", "30");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("lLng", "Long", "444");
        }

        [IdeFact]
        public async Task WatchWindowUpdatesCorrectlyDuringEnCAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System

Module Module1
    Sub Main()
        Dim iInt As Integer = 0
        System.Diagnostics.Debugger.Break()
    End Sub
End Module
");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            await VisualStudio.Debugger.CheckExpressionAsync("iInt", "Integer", "0");

            await VisualStudio.Editor.ReplaceTextAsync("System.Diagnostics.Debugger.Break()", @"iInt = 5
System.Diagnostics.Debugger.Break()");

            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync("iInt = 5");
            await VisualStudio.Debugger.SetNextStatementAsync();
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            await VisualStudio.Debugger.CheckExpressionAsync("iInt", "Integer", "5");
        }
    }
}
