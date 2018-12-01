// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpInteractiveDirectives : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveDirectives( )
            : base()
        {
        }

        [TestMethod]
        public void VerifyHostCommandsCompletionList()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(true);
            VisualStudioInstance.InteractiveWindow.InsertCode("#");
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();

            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("cls",
                "help",
                "load",
                "r",
                "reset",
                "define",
                "elif",
                "else",
                "endif",
                "endregion",
                "error",
                "if",
                "line",
                "pragma",
                "region",
                "undef",
                "warning");
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsDoNotExist("int", "return", "System");

            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.InsertCode(@"int x = 1; //
#");
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();

            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist(
                "elif",
                "else",
                "endif",
                "endregion",
                "error",
                "if",
                "line",
                "pragma",
                "region",
                "warning");

            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsDoNotExist("cls",
                "help",
                "load",
                "prompt",
                "reset",
                "undef",
                "define");
        }

        [TestMethod]
        public void VerifyHashRDirective()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudioInstance.InteractiveWindow.SubmitText(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt");

            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("[0]");
        }

        [TestMethod]
        public void VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashR()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudioInstance.InteractiveWindow.SubmitText(@"using System.Numerics;
class Complex { public int goo() { return 4; } }
var comp = new Complex();
comp.goo()");

            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("4");
        }

        [TestMethod]
        public void VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashR()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudioInstance.InteractiveWindow.SubmitText("using System.Numerics;");
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx",
                "class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                VisualStudioInstance.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudioInstance.InteractiveWindow.SubmitText(@"var comp = new Complex();
comp.goo()");
                VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("4");
            }
        }

        [TestMethod]
        public void VerifyAssembliesReferencedByDefault()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"InteractiveHost64\"");
        }

        [TestMethod]
        public void VerifyHashLoadDirective()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx",
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                VisualStudioInstance.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("2");
                VisualStudioInstance.InteractiveWindow.SubmitText("#load text");
                VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("CS7010: Quoted file name expected");
            }
        }

        [TestMethod]
        public void VerifySquiggleAndErrorMessageUnderIncorrectDirective()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#goo");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(1);
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset");

            VisualStudioInstance.InteractiveWindow.SubmitText("#bar");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(2);
        }

        [TestMethod]
        public void VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelp()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#help");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput(@"Keyboard shortcuts:
  Enter                If the current submission appears to be complete, evaluate it.  Otherwise, insert a new line.
  Ctrl-Enter           Within the current submission, evaluate the current submission.
                       Within a previous submission, append the previous submission to the current submission.
  Shift-Enter          Insert a new line.
  Escape               Clear the current submission.
  Alt-UpArrow          Replace the current submission with a previous submission.
  Alt-DownArrow        Replace the current submission with a subsequent submission (after having previously navigated backwards).
  Ctrl-Alt-UpArrow     Replace the current submission with a previous submission beginning with the same text.
  Ctrl-Alt-DownArrow   Replace the current submission with a subsequent submission beginning with the same text (after having previously navigated backwards).
  Ctrl-K, Ctrl-Enter   Paste the selection at the end of interactive buffer, leave caret at the end of input.
  Ctrl-E, Ctrl-Enter   Paste and execute the selection before any pending input in the interactive buffer.
  Ctrl-A               First press, select the submission containing the cursor.  Second press, select all text in the window.
REPL commands:
  #cls, #clear         Clears the contents of the editor window, leaving history and execution context intact.
  #help                Display help on specified command, or all available commands and key bindings if none specified.
  #reset               Reset the execution environment to the initial state, keep history.
Script directives:
  #r                   Add a metadata reference to specified assembly and all its dependencies, e.g. #r ""myLib.dll"".
  #load                Load specified script file and execute it, e.g. #load ""myScript.csx"".");

            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [TestMethod]
        public void VerifyHashCls()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#cls");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [TestMethod]
        public void VerifyHashReset()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("1+1");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("2");
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [TestMethod]
        public void VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelp()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset /help");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains(@"Usage:
  #reset [noconfig]");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
            VisualStudioInstance.InteractiveWindow.SubmitText("#load /help");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS7010: Quoted file name expected");
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/8281")]
        public void VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterReset()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}");
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("using");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("{");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Main");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Hello");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "string");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("<summary", charsOffset: -1);
            VisualStudioInstance.SendKeys.Send(Alt(VirtualKey.Right));
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("summary");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("innertext");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("--");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("comment");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("CDATA");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("cdata");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("attribute");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Environment");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "class name");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [TestMethod]
        public void WorkspaceClearedAfterReset()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("double M() { return 13.1; }");
            VisualStudioInstance.InteractiveWindow.SubmitText("M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("13.1");
            VisualStudioInstance.InteractiveWindow.SubmitText("double M() { return M(); }");
            VisualStudioInstance.InteractiveWindow.SubmitText("M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            VisualStudioInstance.InteractiveWindow.SubmitText("M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudioInstance.InteractiveWindow.SubmitText("double M() { return M(); }");
            VisualStudioInstance.InteractiveWindow.SubmitText("M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            VisualStudioInstance.InteractiveWindow.SubmitText("double M() { return 13.2; }");
            VisualStudioInstance.InteractiveWindow.SubmitText("M()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("13.2");
        }

        [TestMethod]
        public void InitializationAfterReset()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.");
            VisualStudioInstance.InteractiveWindow.SubmitText("#reset noconfig");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("Resetting execution engine.");
        }
    }
}
