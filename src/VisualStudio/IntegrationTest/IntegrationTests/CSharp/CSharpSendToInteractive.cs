// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSendToInteractive : AbstractInteractiveWindowTest
    {
        private const string FileName = "test.cs";
        public CSharpSendToInteractive(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(SolutionName);
            var project = new Project(ProjectName);
            this.AddProject(WellKnownProjectTemplates.ConsoleApplication, project, Microsoft.CodeAnalysis.LanguageNames.CSharp);

            this.AddFile(
                FileName, 
                project,
                @"using System;

 namespace TestProj
 {
     public class Program1
     {
         public static void Main1(string[] args)
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
 }");

            this.SubmitText("using System;");
        }

        [Fact]
        public void SendSingleLineSubmissionToInteractive()
        {
            this.InsertCode("// scenario 1");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput("int x = 1;");

            this.ClearReplText();
            this.SubmitText("x.ToString()");
            this.WaitForLastReplOutputContains("\"1\"");
        }

        [Fact]
        public void SendMultipleLineSubmissionToInteractive()
        {
            this.InsertCode("// scenario 2");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput("int z = 3;");

            this.ClearReplText();
            this.SubmitText("y.ToString()");
            this.WaitForLastReplOutputContains("\"2\"");
            this.SubmitText("z.ToString()");
            this.WaitForLastReplOutputContains("\"3\"");
        }

        [Fact]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            this.SubmitText("int x = 1;");
            this.InsertCode("// scenario 3");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput(". x *= 4;");

            this.ClearReplText();
            this.SubmitText("a + \"s\"");
            this.WaitForLastReplOutputContains("\"alphas\"");
            this.SubmitText("b");
            this.WaitForLastReplOutputContains("CS0103");
            this.SubmitText("x");
            this.WaitForLastReplOutputContains("4");
        }

        [Fact]
        public void SendToInteractiveWithKeyboardShortcut()
        {
            this.InsertCode("// scenario 4");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            this.SendKeys(this.Ctrl(VirtualKey.E), this.Ctrl(VirtualKey.E));
            this.WaitForLastReplOutput("int j = 7;");

            this.ClearReplText();
            this.SubmitText("j.ToString()");
            this.WaitForLastReplOutputContains("\"7\"");
        }

        [Fact]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            this.InsertCode("// scenario 5");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 5");

            this.ClearReplText();
            this.SubmitText("x");
            this.WaitForLastReplOutput("1");
        }

        [Fact]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            this.InsertCode("// scenario 6");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 6");

            this.ClearReplText();
            this.SubmitText("y");
            this.WaitForLastReplOutput("2");
            this.SubmitText("z");
            this.WaitForLastReplOutput("3");
        }

        [Fact]
        public void ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            this.SubmitText("int x = 1;");
            this.InsertCode("// scenario 7");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            this.ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 7");

            this.ClearReplText();

            this.SubmitText("a");
            this.WaitForLastReplOutput("\"alpha\"");

            this.SubmitText("b");
            this.WaitForLastReplOutputContains("CS0103");
            this.SubmitText("x");
            this.WaitForLastReplOutput("4");
        }

        [Fact]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            this.InsertCode("// scenario 8");
            var project = new Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            this.SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            this.WaitForLastReplInputContains("// scenario 8");

            this.ClearReplText();
            this.SubmitText("j");
            this.WaitForLastReplOutput("7");
        }

        [Fact]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            this.ClearReplText();
            this.SubmitText("#r \"System.Numerics\"");
            this.SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            this.WaitForLastReplOutput("42");

            this.SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            this.SubmitText("(new MyClass()).MyFunc()");
            this.WaitForLastReplOutput("\"MyClass.MyFunc()\"");
        }

        [Fact]
        public void ResetInteractiveFromProjectAndVerify()
        {
            this.AddReference(ProjectName,
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            VisualStudio.Instance.SolutionExplorer.SelectItem(ProjectName);
            this.ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            int defaultTimeoutInMilliseconds = InteractiveWindow.GetTimeoutInMilliseconds();
            InteractiveWindow.SetTimeout(120000);
            this.WaitForReplOutput("using TestProj;");
            InteractiveWindow.SetTimeout(defaultTimeoutInMilliseconds);

            this.SubmitText("x");
            this.WaitForLastReplOutputContains("CS0103");

            this.SubmitText("(new TestProj.C()).M()");
            this.WaitForLastReplOutput("\"C.M()\"");

            this.SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"foo\";");
            this.SubmitText("f.Text");
            this.WaitForLastReplOutput("\"foo\"");

            // TODO https://github.com/dotnet/roslyn/issues/18133
            Wait(seconds: 5);
        }
    }
}