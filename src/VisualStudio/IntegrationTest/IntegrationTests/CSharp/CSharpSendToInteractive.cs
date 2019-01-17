// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpSendToInteractive : AbstractInteractiveWindowTest
    {
        private const string FileName = "Program.cs";

        public CSharpSendToInteractive() : base()
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(SolutionName);
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ConsoleApplication, Microsoft.CodeAnalysis.LanguageNames.CSharp);

            VisualStudioInstance.SolutionExplorer.UpdateFile(
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

            VisualStudioInstance.InteractiveWindow.SubmitText("using System;");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void SendSingleLineSubmissionToInteractive()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 1");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("int x = 1;");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("x.ToString()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("\"1\"");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void SendMultipleLineSubmissionToInteractive()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 2");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("int z = 3;");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("y.ToString()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("\"2\"");
            VisualStudioInstance.InteractiveWindow.SubmitText("z.ToString()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("\"3\"");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("int x = 1;");
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 3");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudioInstance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput(". x *= 4;");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("a + \"s\"");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("\"alphas\"");
            VisualStudioInstance.InteractiveWindow.SubmitText("b");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudioInstance.InteractiveWindow.SubmitText("x");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("4");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void SendToInteractiveWithKeyboardShortcut()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 4");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("int j = 7;");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("j.ToString()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("\"7\"");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 5");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("// scenario 5");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("x");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("1");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 6");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("// scenario 6");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("y");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("2");
            VisualStudioInstance.InteractiveWindow.SubmitText("z");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("3");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("int x = 1;");
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 7");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudioInstance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("// scenario 7");

            VisualStudioInstance.InteractiveWindow.ClearReplText();

            VisualStudioInstance.InteractiveWindow.SubmitText("a");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"alpha\"");

            VisualStudioInstance.InteractiveWindow.SubmitText("b");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudioInstance.InteractiveWindow.SubmitText("x");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("4");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("// scenario 8");
            var project = new Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, FileName);
            VisualStudioInstance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudioInstance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("// scenario 8");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("j");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("7");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudioInstance.InteractiveWindow.SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("42");

            VisualStudioInstance.InteractiveWindow.SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            VisualStudioInstance.InteractiveWindow.SubmitText("(new MyClass()).MyFunc()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"MyClass.MyFunc()\"");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20868")]
        public void ResetInteractiveFromProjectAndVerify()
        {
            var assembly = new ProjectUtils.AssemblyReference("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddMetadataReference(assembly, project);

            VisualStudioInstance.SolutionExplorer.SelectItem(ProjectName);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            int defaultTimeoutInMilliseconds = VisualStudioInstance.InteractiveWindow.GetTimeoutInMilliseconds();
            VisualStudioInstance.InteractiveWindow.SetTimeout(120000);
            VisualStudioInstance.InteractiveWindow.WaitForReplOutput("using TestProj;");
            VisualStudioInstance.InteractiveWindow.SetTimeout(defaultTimeoutInMilliseconds);

            VisualStudioInstance.InteractiveWindow.SubmitText("x");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS0103");

            VisualStudioInstance.InteractiveWindow.SubmitText("(new TestProj.C()).M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"C.M()\"");

            VisualStudioInstance.InteractiveWindow.SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"goo\";");
            VisualStudioInstance.InteractiveWindow.SubmitText("f.Text");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"goo\"");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
        }
    }
}
