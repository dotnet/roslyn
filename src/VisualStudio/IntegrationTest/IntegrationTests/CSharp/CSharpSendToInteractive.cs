// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
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
            VisualStudio.Instance.SolutionExplorer.AddProject(
                ProjectName,
                WellKnownProjectTemplates.ConsoleApplication,
                LanguageNames.CSharp);

            this.AddFile(FileName,
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

            SubmitText("using System;");
        }

        [Fact]
        public void SendSingleLineSubmissionToInteractive()
        {
            InsertCode("// scenario 1");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput("int x = 1;");

            ClearReplText();
            SubmitText("x.ToString()");
            this.WaitForLastReplOutputContains("\"1\"");
        }

        [Fact]
        public void SendMultipleLineSubmissionToInteractive()
        {
            InsertCode("// scenario 2");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput("int z = 3;");

            ClearReplText();
            SubmitText("y.ToString()");
            this.WaitForLastReplOutputContains("\"2\"");
            SubmitText("z.ToString()");
            this.WaitForLastReplOutputContains("\"3\"");
        }

        [Fact]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            SubmitText("int x = 1;");
            InsertCode("// scenario 3");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplOutput(". x *= 4;");

            ClearReplText();
            SubmitText("a + \"s\"");
            this.WaitForLastReplOutputContains("\"alphas\"");
            SubmitText("b");
            this.WaitForLastReplOutputContains("CS0103");
            SubmitText("x");
            this.WaitForLastReplOutputContains("4");
        }

        [Fact]
        public void SendToInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 4");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            this.WaitForLastReplOutput("int j = 7;");

            ClearReplText();
            SubmitText("j.ToString()");
            this.WaitForLastReplOutputContains("\"7\"");
        }

        [Fact]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 5");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 5");

            ClearReplText();
            SubmitText("x");
            this.WaitForLastReplOutput("1");
        }

        [Fact]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 6");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 6");

            ClearReplText();
            SubmitText("y");
            this.WaitForLastReplOutput("2");
            SubmitText("z");
            this.WaitForLastReplOutput("3");
        }

        [Fact]
        public void ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            SubmitText("int x = 1;");
            InsertCode("// scenario 7");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            this.WaitForLastReplInputContains("// scenario 7");

            ClearReplText();

            SubmitText("a");
            this.WaitForLastReplOutput("\"alpha\"");

            SubmitText("b");
            this.WaitForLastReplOutputContains("CS0103");
            SubmitText("x");
            this.WaitForLastReplOutput("4");
        }

        [Fact]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 8");
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            this.OpenFile(FileName, project);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            this.WaitForLastReplInputContains("// scenario 8");

            ClearReplText();
            SubmitText("j");
            this.WaitForLastReplOutput("7");
        }

        [Fact]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            ClearReplText();
            SubmitText("#r \"System.Numerics\"");
            SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            this.WaitForLastReplOutput("42");

            SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            SubmitText("(new MyClass()).MyFunc()");
            this.WaitForLastReplOutput("\"MyClass.MyFunc()\"");
        }

        [Fact]
        public void ResetInteractiveFromProjectAndVerify()
        {
            this.AddReference(ProjectName,
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            VisualStudio.Instance.SolutionExplorer.SelectItem(ProjectName);
            ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            int defaultTimeoutInMilliseconds = InteractiveWindow.GetTimeoutInMilliseconds();
            InteractiveWindow.SetTimeout(120000);
            this.WaitForReplOutput("using TestProj;");
            InteractiveWindow.SetTimeout(defaultTimeoutInMilliseconds);

            SubmitText("x");
            this.WaitForLastReplOutputContains("CS0103");

            SubmitText("(new TestProj.C()).M()");
            this.WaitForLastReplOutput("\"C.M()\"");

            SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"foo\";");
            SubmitText("f.Text");
            this.WaitForLastReplOutput("\"foo\"");

            // TODO https://github.com/dotnet/roslyn/issues/18133
            Wait(seconds: 5);
        }
    }
}