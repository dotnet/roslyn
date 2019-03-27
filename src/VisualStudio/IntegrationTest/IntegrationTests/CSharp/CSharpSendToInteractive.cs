// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSendToInteractive : AbstractInteractiveWindowTest
    {
        private const string FileName = "Program.cs";

        public CSharpSendToInteractive(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ConsoleApplication, Microsoft.CodeAnalysis.LanguageNames.CSharp);

            VisualStudio.SolutionExplorer.UpdateFile(
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

            VisualStudio.InteractiveWindow.SubmitText("using System;");
        }

        [WpfFact]
        public void SendSingleLineSubmissionToInteractive()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 1");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("int x = 1;");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("x.ToString()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("\"1\"");
        }

        [WpfFact]
        public void SendMultipleLineSubmissionToInteractive()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 2");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("int z = 3;");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("y.ToString()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("\"2\"");
            VisualStudio.InteractiveWindow.SubmitText("z.ToString()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("\"3\"");
        }

        [WpfFact]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            VisualStudio.InteractiveWindow.SubmitText("int x = 1;");
            VisualStudio.InteractiveWindow.InsertCode("// scenario 3");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput(". x *= 4;");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("a + \"s\"");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("\"alphas\"");
            VisualStudio.InteractiveWindow.SubmitText("b");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudio.InteractiveWindow.SubmitText("x");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("4");
        }

        [WpfFact]
        public void SendToInteractiveWithKeyboardShortcut()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 4");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("int j = 7;");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("j.ToString()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("\"7\"");
        }

        [WpfFact]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 5");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("// scenario 5");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("x");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("1");
        }

        [WpfFact]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 6");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("// scenario 6");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("y");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("2");
            VisualStudio.InteractiveWindow.SubmitText("z");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("3");
        }

        [WpfFact]
        public void ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudio.InteractiveWindow.SubmitText("int x = 1;");
            VisualStudio.InteractiveWindow.InsertCode("// scenario 7");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("// scenario 7");

            VisualStudio.InteractiveWindow.ClearReplText();

            VisualStudio.InteractiveWindow.SubmitText("a");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"alpha\"");

            VisualStudio.InteractiveWindow.SubmitText("b");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudio.InteractiveWindow.SubmitText("x");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("4");
        }

        [WpfFact]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            VisualStudio.InteractiveWindow.InsertCode("// scenario 8");
            var project = new Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, FileName);
            VisualStudio.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("// scenario 8");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("j");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("7");
        }

        [WpfFact]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudio.InteractiveWindow.SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("42");

            VisualStudio.InteractiveWindow.SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            VisualStudio.InteractiveWindow.SubmitText("(new MyClass()).MyFunc()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"MyClass.MyFunc()\"");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
        }

        [WpfFact]
        public void ResetInteractiveFromProjectAndVerify()
        {
            var assembly = new ProjectUtils.AssemblyReference("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddMetadataReference(assembly, project);

            VisualStudio.SolutionExplorer.SelectItem(ProjectName);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            int defaultTimeoutInMilliseconds = VisualStudio.InteractiveWindow.GetTimeoutInMilliseconds();
            VisualStudio.InteractiveWindow.SetTimeout(120000);
            VisualStudio.InteractiveWindow.WaitForReplOutput("using TestProj;");
            VisualStudio.InteractiveWindow.SetTimeout(defaultTimeoutInMilliseconds);

            VisualStudio.InteractiveWindow.SubmitText("x");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS0103");

            VisualStudio.InteractiveWindow.SubmitText("(new TestProj.C()).M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"C.M()\"");

            VisualStudio.InteractiveWindow.SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"goo\";");
            VisualStudio.InteractiveWindow.SubmitText("f.Text");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"goo\"");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
        }
    }
}
