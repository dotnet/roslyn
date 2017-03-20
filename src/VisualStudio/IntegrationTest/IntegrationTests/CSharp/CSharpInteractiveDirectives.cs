// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{

    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveDirectives : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveDirectives(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void VerifyHostCommandsCompletionList()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
            InsertCode("#");
            InvokeCompletionList();

            VerifyCompletionItemExists("cls",
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
            VerifyCompletionUnexpectedItemDoesNotExist("int", "return", "System");

            ClearReplText();
            InsertCode(@"int x = 1; //
#");
            InvokeCompletionList();

            VerifyCompletionItemExists(
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

            VerifyCompletionUnexpectedItemDoesNotExist("cls",
                "help",
                "load",
                "prompt",
                "reset",
                "undef",
                "define");
        }

        [Fact]
        public void VerifyHashRDirective()
        {
            SubmitText("#r \"System.Numerics\"");
            SubmitText(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt");

            VerifyLastReplOutput("[0]");
        }

        [Fact]
        public void VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashR()
        {
            SubmitText("#r \"System.Numerics\"");
            SubmitText(@"using System.Numerics;
class Complex { public int foo() { return 4; } }
var comp = new Complex();
comp.foo()");

            VerifyLastReplOutput("4");
        }

        [Fact]
        public void VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashR()
        {
            SubmitText("#r \"System.Numerics\"");
            SubmitText("using System.Numerics;");
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx", 
                "class Complex { public int foo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                SubmitText(@"var comp = new Complex();
comp.foo()");
                VerifyLastReplOutput("4");
            }
        }

        [Fact]
        public void VerifyAssembliesReferencedByDefault()
        {
            SubmitText(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName");
            VerifyLastReplOutput("\"InteractiveHost\"");
        }

        [Fact]
        public void VerifyHashLoadDirective()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx", 
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VerifyLastReplOutput("2");
                SubmitText("#load text");
                VerifyLastReplOutputContains("CS7010: Quoted file name expected");
            }
        }

        [Fact]
        public void VerifySquiggleAndErrorMessageUnderIncorrectDirective()
        {
            SubmitText("#foo");
            VerifyLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            VerifyErrorCount(1);
            SubmitText("#bar");
            VerifyLastReplOutput("(1,/2): error CS1024: Preprocessor directive expected");
            VerifyErrorCount(2);
        }

        [Fact]
        public void VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelp()
        {
            SubmitText("#help");
            VerifyLastReplOutput(@"Keyboard shortcuts:
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

            VerifyErrorCount(0);
        }

        [Fact]
        public void VerifyHashCls()
        {
            SubmitText("#cls");
            VerifyErrorCount(0);
        }

        [Fact]
        public void VerifyHashReset()
        {
            SubmitText("1+1");
            VerifyLastReplOutput("2");
            SubmitText("#reset");
            VerifyLastReplOutputContains(@"Resetting execution engine.
Loading context from");
            VerifyErrorCount(0);
        }

        [Fact]
        public void VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelp()
        {
            SubmitText("#reset /help");
            VerifyLastReplOutputContains(@"Usage:
  #reset [noconfig]");
            VerifyErrorCount(0);
            SubmitText("#load /help");
            VerifyLastReplOutputContains("CS7010: Quoted file name expected");
        }

        [Fact]
        public void VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterReset()
        {
            SubmitText(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}");
            SubmitText("#reset");
            PlaceCaret("using");
            VerifyCurrentTokenType(tokenType: "keyword");
            PlaceCaret("{");
            VerifyCurrentTokenType(tokenType: "punctuation");
            PlaceCaret("Main");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Hello");
            VerifyCurrentTokenType(tokenType: "string");
            PlaceCaret("<summary", charsOffset: -1);
            SendKeys(Alt(VirtualKey.Right));
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("summary");
            VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            PlaceCaret("innertext");
            VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            PlaceCaret("--");
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("comment");
            VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            PlaceCaret("CDATA");
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("cdata");
            VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            PlaceCaret("attribute");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Environment");
            VerifyCurrentTokenType(tokenType: "class name");
            VerifyErrorCount(0);
        }

        [Fact]
        public void WorkspaceClearedAfterReset()
        {
            SubmitText("double M() { return 13.1; }");
            SubmitText("M()");
            VerifyLastReplOutput("13.1");
            SubmitText("double M() { return M(); }");
            SubmitText("M()");
            VerifyLastReplOutputContains("Process is terminated due to StackOverflowException.");
            SubmitText("M()");
            VerifyLastReplOutputContains("CS0103");
            SubmitText("double M() { return M(); }");
            SubmitText("M()");
            VerifyLastReplOutputContains("Process is terminated due to StackOverflowException.");
            SubmitText("double M() { return 13.2; }");
            SubmitText("M()");
            VerifyLastReplOutput("13.2");
        }

        [Fact]
        public void InitializationAfterReset()
        {
            SubmitText("#reset");
            VerifyLastReplOutput(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.");
            SubmitText("#reset noconfig");
            VerifyLastReplOutput("Resetting execution engine.");
        }
    }
}