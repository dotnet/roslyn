// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.Test.Utilities;
using Xunit;
using System;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.DebuggingEditAndContinue)]
public abstract class BasicEditAndContinue(string projectTemplate)
    : AbstractEditorTest()
{
#if false // https://github.com/dotnet/roslyn/issues/70938
    public class LegacyProject()
        : BasicEditAndContinue(WellKnownProjectTemplates.ConsoleApplication)
    {
        protected override string FileName => "Module1.vb";
    }
#endif

    public class CommonProjectSystem()
        : BasicEditAndContinue(WellKnownProjectTemplates.VisualBasicNetCoreConsoleApplication)
    {
        protected override string FileName => "Program.vb";
    }

    protected abstract string FileName { get; }

    private readonly string _projectTemplate = projectTemplate;

    protected override string LanguageName => LanguageNames.VisualBasic;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(BasicEditAndContinue), HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectAsync(ProjectName, _projectTemplate, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task UpdateActiveStatementLeafNode()
    {
        await TestServices.Editor.SetTextAsync(@"
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
", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "names(0)", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("names(0)", "names(1)", HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: true, HangMitigatingCancellationToken);
        await TestServices.Debugger.CheckExpressionAsync("names(1)", "String", "\"goo\"", HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: true, HangMitigatingCancellationToken);
        await TestServices.Debugger.CheckExpressionAsync("names(1)", "String", "\"bar\"", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task AddTryCatchAroundActiveStatement()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System
Module Module1
    Sub Main()
        Goo()
    End Sub

    Private Sub Goo()
        Console.WriteLine(1)
    End Sub
End Module", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "Console.WriteLine(1)", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("Console.WriteLine(1)",
            @"Try
Console.WriteLine(1)
Catch ex As Exception
End Try", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: true, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        End Try", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task EditLambdaExpression()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System
Module Module1
    Private Delegate Function del(i As Integer) As Integer

    Sub Main()
        Dim myDel As del = Function(x) x * x
        Dim j As Integer = myDel(5)
    End Sub
End Module", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "x * x", charsOffset: -1, HangMitigatingCancellationToken);

        var succeed = await TestServices.SolutionExplorer.BuildSolutionAndWaitAsync(HangMitigatingCancellationToken);

        await TestServices.ErrorList.ShowBuildErrorsAsync(HangMitigatingCancellationToken);

        var errors = await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(string.Empty, string.Join(Environment.NewLine, errors));

        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("x * x", "x * 2", HangMitigatingCancellationToken);

        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: false, HangMitigatingCancellationToken);
        await TestServices.Debugger.StopAsync(waitForDesignMode: true, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));

        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("x * 2", "x * x", HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: true, HangMitigatingCancellationToken);
        await TestServices.Debugger.StopAsync(waitForDesignMode: true, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        Assert.Empty(await TestServices.ErrorList.GetBuildErrorsAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task EnCWhileDebuggingFromImmediateWindow()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System

Module Module1
    Sub Main()
        Dim x = 4
        Console.WriteLine(x)
    End Sub
End Module", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "Dim x", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Debugger.ExecuteStatementAsync("Module1.Main()", HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("x = 4", "x = 42", HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: true, HangMitigatingCancellationToken);
        await TestServices.Debugger.CheckExpressionAsync("x", "Integer", "42", HangMitigatingCancellationToken);
        await TestServices.Debugger.ExecuteStatementAsync("Module1.Main()", HangMitigatingCancellationToken);
    }

    private async Task SetupMultiProjectSolutionAsync(CancellationToken cancellationToken)
    {
        var basicLibrary = "BasicLibrary1";
        await TestServices.SolutionExplorer.AddProjectAsync(basicLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, cancellationToken);

        var cSharpLibrary = "CSharpLibrary1";
        await TestServices.SolutionExplorer.AddProjectAsync(cSharpLibrary, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp, cancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(cSharpLibrary, "File1.cs", cancellationToken: cancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(basicLibrary, "Class1.vb", cancellationToken);
        await TestServices.Editor.SetTextAsync(@"
Imports System
Public Class Class1
    Public Sub New()
    End Sub

    Public Sub PrintX(x As Integer)
        Console.WriteLine(x)
    End Sub
End Class
", cancellationToken);

        await TestServices.SolutionExplorer.AddProjectReferenceAsync(ProjectName, basicLibrary, cancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, FileName, cancellationToken);

        await TestServices.Editor.SetTextAsync(@"
Imports System
Imports BasicLibrary1

Module Module1
    Sub Main()
        Dim c As New Class1()
        c.PrintX(5)
    End Sub
End Module
", cancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, cancellationToken);
    }

    [IdeFact]
    public async Task MultiProjectDebuggingWhereNotAllModulesAreLoaded()
    {
        await SetupMultiProjectSolutionAsync(HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "PrintX", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("5", "42", HangMitigatingCancellationToken);
        await TestServices.Debugger.StepOverAsync(waitForBreakOrEnd: false, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.EditAndContinue,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList,
            ],
            HangMitigatingCancellationToken);

        AssertEx.Empty(await TestServices.ErrorList.GetErrorsAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task LocalsWindowUpdatesAfterLocalGetsItsTypeUpdatedDuringEnC()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System
Module Module1
    Sub Main()
        Dim goo As String = ""abc""
        Console.WriteLine(goo)
    End Sub
End Module
", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "End Sub", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("Dim goo As String = \"abc\"", "Dim goo As Single = 10", HangMitigatingCancellationToken);
        await TestServices.Editor.SelectTextInCurrentDocumentAsync("Sub Main()", HangMitigatingCancellationToken);
        await TestServices.Debugger.SetNextStatementAsync(HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);

        Assert.Equal(("Single", "10"), await TestServices.LocalsWindow.GetEntryAsync(["goo"], HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task LocalsWindowUpdatesCorrectlyDuringEnC()
    {
        await TestServices.Editor.SetTextAsync(@"
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
", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "Function bar(ByVal moo As Long) As Decimal", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.ReplaceTextAsync("Dim lLng As Long = 5", "Dim lLng As Long = 444", HangMitigatingCancellationToken);
        await TestServices.Debugger.SetBreakpointAsync(ProjectName, FileName, "Return 4", HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);

        Assert.Equal(("Decimal", "0"), await TestServices.LocalsWindow.GetEntryAsync(["bar"], HangMitigatingCancellationToken));
        Assert.Equal(("Long", "5"), await TestServices.LocalsWindow.GetEntryAsync(["moo"], HangMitigatingCancellationToken));
        Assert.Equal(("Integer", "30"), await TestServices.LocalsWindow.GetEntryAsync(["iInt"], HangMitigatingCancellationToken));
        Assert.Equal(("Long", "444"), await TestServices.LocalsWindow.GetEntryAsync(["lLng"], HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task WatchWindowUpdatesCorrectlyDuringEnC()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System

Module Module1
    Sub Main()
        Dim iInt As Integer = 0
        System.Diagnostics.Debugger.Break()
    End Sub
End Module
", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);

        await TestServices.Debugger.CheckExpressionAsync("iInt", "Integer", "0", HangMitigatingCancellationToken);

        await TestServices.Editor.ReplaceTextAsync("System.Diagnostics.Debugger.Break()", @"iInt = 5
System.Diagnostics.Debugger.Break()", HangMitigatingCancellationToken);

        await TestServices.Editor.SelectTextInCurrentDocumentAsync("iInt = 5", HangMitigatingCancellationToken);
        await TestServices.Debugger.SetNextStatementAsync(HangMitigatingCancellationToken);
        await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);

        await TestServices.Debugger.CheckExpressionAsync("iInt", "Integer", "5", HangMitigatingCancellationToken);
    }
}
