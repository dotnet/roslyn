// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
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

            AddFile(FileName,
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
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();
            VerifyLastReplOutputContains("int x = 1;");

            ClearReplText();
            SubmitText("x.ToString()", waitForPrompt: false);
            VerifyLastReplOutputContains("\"1\"");
        }

        [Fact]
        public void SendMultipleLineSubmissionToInteractive()
        {
            // problem here
            InsertCode("// scenario 2");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();
            
            VerifyLastReplOutputContains("int z = 3;");
            ClearReplText();
            SubmitText("y.ToString()");
            VerifyLastReplOutputContains("\"2\"");
            SubmitText("z.ToString()");
            VerifyLastReplOutputContains("\"3\"");
        }

        [Fact]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            InsertCode("// scenario 3");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();
            VerifyLastReplOutputContains(@"string a = ""alpha"";
. x *= 4;");

            ClearReplText();
            SubmitText("a + \"s\"");
            VerifyLastReplOutputContains("\"alphas\"");
            SubmitText("b");
            VerifyLastReplOutputContains("CS0103");
            SubmitText("x");
            VerifyLastReplOutputContains("4");
        }

        [Fact]





        public void SendToInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 4");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            VerifyLastReplOutputContains("int j = 7;");

            ClearReplText();
            SubmitText("j.ToString()");
            VerifyLastReplOutputContains("\"7\"");
        }

        [Fact]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 5");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();

            VerifyLastReplInput("// scenario 5");
            ClearReplText();
            SubmitText("x");
            VerifyLastReplOutput("1");
        }

        [Fact]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 6");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();
            VerifyLastReplInput("// scenario 6");

            ClearReplText();
            SubmitText("y");
            VerifyLastReplOutput("2");
            SubmitText("z");
            VerifyLastReplOutput("3");
        }

        [Fact]
        public void ExecuteMultipleLineBlockSelectedSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            SubmitText("int x = 1;");
            InsertCode("// scenario 7");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitWhileInteractiveIsRunning();
            VerifyLastReplInput("// scenario 7");

            ClearReplText();

            SubmitText("a", waitForPrompt: false);
            VerifyLastReplOutput("\"alpha\"");

            SubmitText("b", waitForPrompt: false);
            VerifyLastReplOutputContains("CS0103");
            SubmitText("x", waitForPrompt: false);
            VerifyLastReplOutput("4");
        }

        [Fact]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 8");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            WaitWhileInteractiveIsRunning();
            VerifyLastReplInput("// scenario 8");

            ClearReplText();
            SubmitText("j", waitForPrompt: false);
            VerifyLastReplOutput("7");
        }

        [Fact]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            ClearReplText();
            SubmitText("#r \"System.Numerics\"");
            SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            VerifyLastReplOutput("42");

            SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            SubmitText("(new MyClass()).MyFunc()");
            VerifyLastReplOutput("\"MyClass.MyFunc()\"");
        }

        [Fact]
        public void ResetInteractiveFromProjectAndVerify()
        {
            AddReference(ProjectName,
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            VisualStudio.Instance.SolutionExplorer.SelectItem(ProjectName);
            ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            Wait(seconds: 10); // TODO
            SubmitText("x");
            VerifyLastReplOutputContains("CS0103");

            SubmitText("(new TestProj.C()).M()");
            VerifyLastReplOutput("\"C.M()\"");

            SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"foo\";");
            SubmitText("f.Text");
            VerifyLastReplOutput("\"foo\"");
        }
    }
}