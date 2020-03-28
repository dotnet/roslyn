﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEditAndContinue : AbstractEditorTest
    {
        private const string module1FileName = "Module1.vb";

        public BasicEditAndContinue(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
        public void UpdateActiveStatementLeafNode()
        {
            VisualStudio.Editor.SetText(@"
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

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "names(0)");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("names(0)", "names(1)");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.CheckExpression("names(1)", "String", "\"goo\"");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.CheckExpression("names(1)", "String", "\"bar\"");
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
        public void AddTryCatchAroundActiveStatement()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Goo()
    End Sub

    Private Sub Goo()
        Console.WriteLine(1)
    End Sub
End Module");

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "Console.WriteLine(1)");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("Console.WriteLine(1)",
                @"Try
Console.WriteLine(1)
Catch ex As Exception
End Try");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Editor.Verify.CurrentLineText("End Try");
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
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

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "x * x", charsOffset: -1);

            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("x * x", "x * 2");

            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: false);
            VisualStudio.Debugger.Stop(waitForDesignMode: true);
            VisualStudio.ErrorList.Verify.NoBuildErrors();

            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("x * 2", "x * x");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.Stop(waitForDesignMode: true);
            VisualStudio.ErrorList.Verify.NoBuildErrors();
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
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

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "Dim x", charsOffset: 1);
            VisualStudio.Debugger.ExecuteStatement("Module1.Main()");
            VisualStudio.Editor.ReplaceText("x = 4", "x = 42");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: true);
            VisualStudio.Debugger.CheckExpression("x", "Integer", "42");
            VisualStudio.Debugger.ExecuteStatement("Module1.Main()");
        }

        private void SetupMultiProjectSolution()
        {
            var basicLibrary = new ProjectUtils.Project("BasicLibrary1");
            VisualStudio.SolutionExplorer.AddProject(basicLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);

            var cSharpLibrary = new ProjectUtils.Project("CSharpLibrary1");
            VisualStudio.SolutionExplorer.AddProject(cSharpLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.SolutionExplorer.AddFile(cSharpLibrary, "File1.cs");

            VisualStudio.SolutionExplorer.OpenFile(basicLibrary, "Class1.vb");
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
            VisualStudio.SolutionExplorer.OpenFile(project, module1FileName);

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

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        // Also https://github.com/dotnet/roslyn/issues/36763
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
        public void MultiProjectDebuggingWhereNotAllModulesAreLoaded()
        {
            SetupMultiProjectSolution();
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "PrintX", charsOffset: 1);
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("5", "42");
            VisualStudio.Debugger.StepOver(waitForBreakOrEnd: false);
            VisualStudio.ErrorList.Verify.NoErrors();
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
        public void LocalsWindowUpdatesAfterLocalGetsItsTypeUpdatedDuringEnC()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Module Module1
    Sub Main()
        Dim goo As String = ""abc""
        Console.WriteLine(goo)
    End Sub
End Module
");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "End Sub");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("Dim goo As String = \"abc\"", "Dim goo As Single = 10");
            VisualStudio.Editor.SelectTextInCurrentDocument("Sub Main()");
            VisualStudio.Debugger.SetNextStatement();
            VisualStudio.Debugger.Go(waitForBreakMode: true);

            VisualStudio.LocalsWindow.Verify.CheckEntry("goo", "Single", "10");
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
        public void LocalsWindowUpdatesCorrectlyDuringEnC()
        {
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
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "Function bar(ByVal moo As Long) As Decimal");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();
            VisualStudio.Editor.ReplaceText("Dim lLng As Long = 5", "Dim lLng As Long = 444");
            VisualStudio.Debugger.SetBreakPoint(module1FileName, "Return 4");
            VisualStudio.Debugger.Go(waitForBreakMode: true);

            VisualStudio.LocalsWindow.Verify.CheckEntry("bar", "Decimal", "0");
            VisualStudio.LocalsWindow.Verify.CheckEntry("moo", "Long", "5");
            VisualStudio.LocalsWindow.Verify.CheckEntry("iInt", "Integer", "30");
            VisualStudio.LocalsWindow.Verify.CheckEntry("lLng", "Long", "444");
        }

        // Also "https://github.com/dotnet/roslyn/issues/37689")]
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/35965")]
        [Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
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

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Editor.Activate();

            VisualStudio.Debugger.CheckExpression("iInt", "Integer", "0");

            VisualStudio.Editor.ReplaceText("System.Diagnostics.Debugger.Break()", @"iInt = 5
System.Diagnostics.Debugger.Break()");

            VisualStudio.Editor.SelectTextInCurrentDocument("iInt = 5");
            VisualStudio.Debugger.SetNextStatement();
            VisualStudio.Debugger.Go(waitForBreakMode: true);

            VisualStudio.Debugger.CheckExpression("iInt", "Integer", "5");
        }
    }
}
