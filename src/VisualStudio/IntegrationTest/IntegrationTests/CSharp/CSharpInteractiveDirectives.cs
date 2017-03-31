// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
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
            this.InsertCode("#");
            this.InvokeCompletionList();

            this.VerifyCompletionItemExists("cls",
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
            this.VerifyCompletionUnexpectedItemDoesNotExist("int", "return", "System");

            this.ClearReplText();
            this.InsertCode(@"int x = 1; //
#");
            this.InvokeCompletionList();

            this.VerifyCompletionItemExists(
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

            this.VerifyCompletionUnexpectedItemDoesNotExist("cls",
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
            this.SubmitText("#r \"System.Numerics\"");
            this.SubmitText(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt");

            this.WaitForLastReplOutput("[0]");
        }

        [Fact]
        public void VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashR()
        {
            this.SubmitText("#r \"System.Numerics\"");
            this.SubmitText(@"using System.Numerics;
class Complex { public int foo() { return 4; } }
var comp = new Complex();
comp.foo()");

            this.WaitForLastReplOutput("4");
        }

        [Fact]
        public void VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashR()
        {
            this.SubmitText("#r \"System.Numerics\"");
            this.SubmitText("using System.Numerics;");
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx",
                "class Complex { public int foo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                this.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                this.SubmitText(@"var comp = new Complex();
comp.foo()");
                this.WaitForLastReplOutput("4");
            }
        }

        [Fact]
        public void VerifyAssembliesReferencedByDefault()
        {
            this.SubmitText(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName");
            this.WaitForLastReplOutput("\"InteractiveHost\"");
        }

        [Fact]
        public void VerifyHashLoadDirective()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx",
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                this.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                this.WaitForLastReplOutput("2");
                this.SubmitText("#load text");
                this.WaitForLastReplOutput("CS7010: Quoted file name expected");
            }
        }

        [Fact]
        public void VerifySquiggleAndErrorMessageUnderIncorrectDirective()
        {
            this.SubmitText("#foo");
            this.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(1);
            this.SubmitText("#reset");

            this.SubmitText("#bar");
            this.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(2);
        }

        [Fact]
        public void VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelp()
        {
            this.SubmitText("#help");
            this.WaitForLastReplOutput(@"Keyboard shortcuts:
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

        [Fact]
        public void VerifyHashCls()
        {
            this.SubmitText("#cls");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [Fact]
        public void VerifyHashReset()
        {
            this.SubmitText("1+1");
            this.WaitForLastReplOutput("2");
            this.SubmitText("#reset");
            this.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [Fact]
        public void VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelp()
        {
            this.SubmitText("#reset /help");
            this.WaitForLastReplOutputContains(@"Usage:
  #reset [noconfig]");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
            this.SubmitText("#load /help");
            this.WaitForLastReplOutputContains("CS7010: Quoted file name expected");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8281")]
        public void VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterReset()
        {
            this.SubmitText(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}");
            this.SubmitText("#reset");
            this.PlaceCaret("using");
            this.VerifyCurrentTokenType(tokenType: "keyword");
            this.PlaceCaret("{");
            this.VerifyCurrentTokenType(tokenType: "punctuation");
            this.PlaceCaret("Main");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Hello");
            this.VerifyCurrentTokenType(tokenType: "string");
            this.PlaceCaret("<summary", charsOffset: -1);
            this.SendKeys(Alt(VirtualKey.Right));
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("summary");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            this.PlaceCaret("innertext");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            this.PlaceCaret("--");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            this.PlaceCaret("comment");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            this.PlaceCaret("CDATA");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("cdata");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            this.PlaceCaret("attribute");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Environment");
            this.VerifyCurrentTokenType(tokenType: "class name");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [Fact]
        public void WorkspaceClearedAfterReset()
        {
            this.SubmitText("double M() { return 13.1; }");
            this.SubmitText("M()");
            this.WaitForLastReplOutput("13.1");
            this.SubmitText("double M() { return M(); }");
            this.SubmitText("M()");
            this.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            this.SubmitText("M()");
            this.WaitForLastReplOutputContains("CS0103");
            this.SubmitText("double M() { return M(); }");
            this.SubmitText("M()");
            this.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            this.SubmitText("double M() { return 13.2; }");
            this.SubmitText("M()");
            this.WaitForLastReplOutput("13.2");
        }

        [Fact]
        public void InitializationAfterReset()
        {
            this.SubmitText("#reset");
            this.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.");
            this.SubmitText("#reset noconfig");
            this.WaitForLastReplOutput("Resetting execution engine.");
        }
    }
}