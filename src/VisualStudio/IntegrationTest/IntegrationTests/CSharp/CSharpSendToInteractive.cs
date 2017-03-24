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
            WaitForLastReplOutput("int x = 1;");

            ClearReplText();
            SubmitText("x.ToString()");
            WaitForLastReplOutputContains("\"1\"");
        }

        [Fact]
        public void SendMultipleLineSubmissionToInteractive()
        {
            InsertCode("// scenario 2");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitForLastReplOutput("int z = 3;");

            ClearReplText();
            SubmitText("y.ToString()");
            WaitForLastReplOutputContains("\"2\"");
            SubmitText("z.ToString()");
            WaitForLastReplOutputContains("\"3\"");
        }

        [Fact]
        public void SendMultipleLineBlockSelectedSubmissionToInteractive()
        {
            SubmitText("int x = 1;");
            InsertCode("// scenario 3");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 5 */", charsOffset: 6);
            VisualStudio.Instance.Editor.PlaceCaret("/* 6 */", charsOffset: -3, extendSelection: true, selectBlock: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitForLastReplOutput(". x *= 4;");

            ClearReplText();
            SubmitText("a + \"s\"");
            WaitForLastReplOutputContains("\"alphas\"");
            SubmitText("b");
            WaitForLastReplOutputContains("CS0103");
            SubmitText("x");
            WaitForLastReplOutputContains("4");
        }

        [Fact]
        public void SendToInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 4");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            WaitForLastReplOutput("int j = 7;");

            ClearReplText();
            SubmitText("j.ToString()");
            WaitForLastReplOutputContains("\"7\"");
        }

        [Fact]
        public void ExecuteSingleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 5");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 1 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 2 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitForLastReplInputContains("// scenario 5");

            ClearReplText();
            SubmitText("x");
            WaitForLastReplOutput("1");
        }

        [Fact]
        public void ExecuteMultipleLineSubmissionInInteractiveWhilePreservingReplSubmissionBuffer()
        {
            InsertCode("// scenario 6");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 3 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 4 */", charsOffset: -1, extendSelection: true);
            ExecuteCommand(WellKnownCommandNames.InteractiveConsole_ExecuteInInteractive);
            WaitForLastReplInputContains("// scenario 6");

            ClearReplText();
            SubmitText("y");
            WaitForLastReplOutput("2");
            SubmitText("z");
            WaitForLastReplOutput("3");
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
            WaitForLastReplInputContains("// scenario 7");

            ClearReplText();

            SubmitText("a");
            WaitForLastReplOutput("\"alpha\"");

            SubmitText("b");
            WaitForLastReplOutputContains("CS0103");
            SubmitText("x");
            WaitForLastReplOutput("4");
        }

        [Fact]
        public void ExecuteInInteractiveWithKeyboardShortcut()
        {
            InsertCode("// scenario 8");
            OpenFile(ProjectName, FileName);
            VisualStudio.Instance.Editor.PlaceCaret("/* 7 */", charsOffset: 1);
            VisualStudio.Instance.Editor.PlaceCaret("/* 8 */", charsOffset: -1, extendSelection: true);
            SendKeys(Ctrl(VirtualKey.E), Ctrl(VirtualKey.E));
            WaitForLastReplInputContains("// scenario 8");

            ClearReplText();
            SubmitText("j");
            WaitForLastReplOutput("7");
        }

        [Fact]
        public void AddAssemblyReferenceAndTypesToInteractive()
        {
            ClearReplText();
            SubmitText("#r \"System.Numerics\"");
            SubmitText("Console.WriteLine(new System.Numerics.BigInteger(42));");
            WaitForLastReplOutput("42");

            SubmitText("public class MyClass { public string MyFunc() { return \"MyClass.MyFunc()\"; } }");
            SubmitText("(new MyClass()).MyFunc()");
            WaitForLastReplOutput("\"MyClass.MyFunc()\"");
        }

        [Fact]
        public void ResetInteractiveFromProjectAndVerify()
        {
            AddReference(ProjectName,
                "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            VisualStudio.Instance.SolutionExplorer.SelectItem(ProjectName);
            ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject);

            // Waiting for a long operation: build + reset from project
            int defaultTimeoutInMilliseconds = InteractiveWindow.GetTimeoutInMilliseconds();
            InteractiveWindow.SetTimeout(120000);
            WaitForReplOutput("using TestProj;");
            InteractiveWindow.SetTimeout(defaultTimeoutInMilliseconds);

            SubmitText("x");
            WaitForLastReplOutputContains("CS0103");

            SubmitText("(new TestProj.C()).M()");
            WaitForLastReplOutput("\"C.M()\"");

            SubmitText("System.Windows.Forms.Form f = new System.Windows.Forms.Form(); f.Text = \"foo\";");
            SubmitText("f.Text");
            WaitForLastReplOutput("\"foo\"");

            // TODO https://github.com/dotnet/roslyn/issues/18133
            Wait(seconds: 5);
        }
    }
}