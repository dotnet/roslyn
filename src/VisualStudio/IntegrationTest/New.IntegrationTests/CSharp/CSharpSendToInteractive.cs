// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpSendToInteractive : AbstractInteractiveWindowTest
{
    private const string FileName = "Program.cs";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddProjectAsync(project, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp, HangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync("""
            using System;

             namespace TestProj
             {
                 public class Program
                 {
                     public static void Main(string[] args)
                     {
                        /* 1 */int x = 1;/* 2 */
                        
                        /* 3 */int y = 2;
                        int z = 3;/* 4 */
                        
                        /* 5 */     string a = "alpha";
                        string b = "x *= 4;            ";/* 6 */

                        /* 7 */int j = 7;/* 8 */
                    }
                }

                 public class C
                 {
                     public string M()
                     {
                         return "C.M()";
                     }
                 }
             }

            """, HangMitigatingCancellationToken);
        await TestServices.Editor.ActivateAsync(HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("using System;", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task SendSingleLineSubmissionToInteractive()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 1", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 1 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 2 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("> int x = 1;", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("x.ToString()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("""
            "1"
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task SendMultipleLineSubmissionToInteractive()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 2", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 3 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 4 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("\n.             int z = 3;", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("y.ToString()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("""
            "2"
            """, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("z.ToString()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("""
            "3"
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task SendMultipleLineBlockSelectedSubmissionToInteractive()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("int x = 1;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 3", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 5 */", charsOffset: 6, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 6 */", charsOffset: -3, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("\n. x *= 4;            ", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("""
            a + "s"
            """, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("""
            "alphas"
            """, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("b", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("x", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("4", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task SendToInteractiveWithKeyboardShortcut()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 4", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 7 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 8 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([(VirtualKeyCode.VK_E, VirtualKeyCode.CONTROL), (VirtualKeyCode.VK_E, VirtualKeyCode.CONTROL)], HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("> int j = 7;", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("j.ToString()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("""
            "7"
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 5", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 1 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 2 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 5", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("x", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("1", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 6", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 3 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 4 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 6", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("y", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("2", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("z", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("3", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
    {
        await TestServices.InteractiveWindow.SubmitTextAsync("int x = 1;", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 7", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 5 */", charsOffset: 6, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 6 */", charsOffset: -3, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ExecuteInInteractive, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 7", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("a", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "alpha"
            """, HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("b", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("x", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("4", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExecuteInInteractiveWithKeyboardShortcut()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("// scenario 8", HangMitigatingCancellationToken);
        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, FileName, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 7 */", charsOffset: 1, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("/* 8 */", charsOffset: -1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([(VirtualKeyCode.VK_E, VirtualKeyCode.CONTROL), (VirtualKeyCode.VK_E, VirtualKeyCode.CONTROL)], HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 8", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("j", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("7", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task AddAssemblyReferenceAndTypesToInteractive()
    {
        await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("""
            #r "System.Numerics"
            """, HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("Console.WriteLine(new System.Numerics.BigInteger(42));", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("42", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("(new MyClass()).MyFunc()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "MyClass.MyFunc()"
            """, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawlerLegacy, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ResetInteractiveFromProjectAndVerify()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddMetadataReferenceAsync("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", project, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.SelectItemAsync(ProjectName, HangMitigatingCancellationToken);
        await TestServices.Shell.ExecuteCommandAsync<ResetInteractiveWindowFromProjectCommand>(HangMitigatingCancellationToken);

        // Waiting for a long operation: build + reset from project
        await TestServices.InteractiveWindow.WaitForReplOutputAsync("using TestProj;", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("x", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("(new TestProj.C()).M()", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "C.M()"
            """, HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"goo\";", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.SubmitTextAsync("f.Text", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("""
            "goo"
            """, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawlerLegacy, HangMitigatingCancellationToken);
    }
}
