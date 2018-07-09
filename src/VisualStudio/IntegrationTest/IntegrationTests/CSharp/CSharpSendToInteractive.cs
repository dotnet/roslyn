// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSendToInteractive : AbstractIdeInteractiveWindowTest
    {
        private const string FileName = "Program.cs";

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(SolutionName);
            await VisualStudio.SolutionExplorer.AddProjectAsync(ProjectName, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);

            await VisualStudio.SolutionExplorer.UpdateFileAsync(
                ProjectName,
                FileName,
                @"using System;

 namespace TestProj
 {
     public class Program
     {
         public static void Main(string[] args)
         {
            /* 1 */int x = 1;/* 2 */
            
            /* 3 */int y = 2;
            int z = 3;/* 4 */
            
            /* 5 */     string a = ""alpha"";
            string b = ""x *= 4;            "";/* 6 */

            /* 7 */int j = 7;/* 8 */
        }
    }

     public class C
     {
         public string M()
         {
             return ""C.M()"";
         }
     }
 }
", 
                open: true);

            await VisualStudio.InteractiveWindow.SubmitTextAsync("using System;");
        }

        [IdeFact]
        public async Task SendSingleLineSubmissionToInteractiveAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 1");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 1 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 2 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("int x = 1;");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("x.ToString()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("\"1\"");
        }

        [IdeFact]
        public async Task SendMultipleLineSubmissionToInteractiveAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 2");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 3 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 4 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("int z = 3;");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("y.ToString()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("\"2\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("z.ToString()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("\"3\"");
        }

        [IdeFact]
        public async Task SendMultipleLineBlockSelectedSubmissionToInteractiveAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("int x = 1;");
            VisualStudio.InteractiveWindow.InsertCode("// scenario 3");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 5 */", charsOffset: 6);
            await VisualStudio.Editor.PlaceCaretAsync("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync(". x *= 4;");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("a + \"s\"");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("\"alphas\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("b");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("x");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("4");
        }

        [IdeFact]
        public async Task SendToInteractiveWithKeyboardShortcutAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 4");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 7 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 8 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("int j = 7;");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("j.ToString()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("\"7\"");
        }

        [IdeFact]
        public async Task ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBufferAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 5");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 1 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 2 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 5");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("x");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("1");
        }

        [IdeFact]
        public async Task ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBufferAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 6");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 3 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 4 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 6");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("y");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("2");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("z");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("3");
        }

        [IdeFact]
        public async Task ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBufferAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("int x = 1;");
            VisualStudio.InteractiveWindow.InsertCode("// scenario 7");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 5 */", charsOffset: 6);
            await VisualStudio.Editor.PlaceCaretAsync("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 7");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();

            await VisualStudio.InteractiveWindow.SubmitTextAsync("a");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"alpha\"");

            await VisualStudio.InteractiveWindow.SubmitTextAsync("b");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("x");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("4");
        }

        [IdeFact]
        public async Task ExecuteInInteractiveWithKeyboardShortcutAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 8");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, FileName);
            await VisualStudio.Editor.PlaceCaretAsync("/* 7 */", charsOffset: 1);
            await VisualStudio.Editor.PlaceCaretAsync("/* 8 */", charsOffset: -1, extendSelection: true);
            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("// scenario 8");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("j");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("7");
        }

        [IdeFact]
        public async Task AddAssemblyReferenceAndTypesToInteractiveAsync()
        {
            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Console.WriteLine(new System.Numerics.BigInteger(42));");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("42");

            await VisualStudio.InteractiveWindow.SubmitTextAsync("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("(new MyClass()).MyFunc()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"MyClass.MyFunc()\"");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler);
        }

        [IdeFact]
        public async Task ResetInteractiveFromProjectAndVerifyAsync()
        {
            var assemblyName = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            await VisualStudio.SolutionExplorer.AddMetadataReferenceAsync(assemblyName, ProjectName);

            await VisualStudio.SolutionExplorer.SelectItemAsync(ProjectName);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            var defaultTimeout = VisualStudio.InteractiveWindow.GetTimeout();
            VisualStudio.InteractiveWindow.SetTimeout(TimeSpan.FromSeconds(120));
            await VisualStudio.InteractiveWindow.WaitForReplOutputAsync("using TestProj;");
            VisualStudio.InteractiveWindow.SetTimeout(defaultTimeout);

            await VisualStudio.InteractiveWindow.SubmitTextAsync("x");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103");

            await VisualStudio.InteractiveWindow.SubmitTextAsync("(new TestProj.C()).M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"C.M()\"");

            await VisualStudio.InteractiveWindow.SubmitTextAsync("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"goo\";");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("f.Text");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"goo\"");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler);
        }
    }
}
