// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveDirectives : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveDirectives(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [WpfFact]
        public void VerifyHostCommandsCompletionList()
        {
            VisualStudio.InteractiveWindow.InsertCode("#");
            VisualStudio.InteractiveWindow.InvokeCompletionList();

            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("cls",
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
            VisualStudio.InteractiveWindow.Verify.CompletionItemsDoNotExist("int", "return", "System");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.InsertCode(@"int x = 1; //
#");
            VisualStudio.InteractiveWindow.InvokeCompletionList();

            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist(
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

            VisualStudio.InteractiveWindow.Verify.CompletionItemsDoNotExist("cls",
                "help",
                "load",
                "prompt",
                "reset",
                "undef",
                "define");
        }

        [WpfFact]
        public void VerifyHashRDirective()
        {
            VisualStudio.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudio.InteractiveWindow.SubmitText(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt");

            VisualStudio.InteractiveWindow.WaitForLastReplOutput("[0]");
        }

        [WpfFact]
        public void VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashR()
        {
            VisualStudio.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudio.InteractiveWindow.SubmitText(@"using System.Numerics;
class Complex { public int goo() { return 4; } }
var comp = new Complex();
comp.goo()");

            VisualStudio.InteractiveWindow.WaitForLastReplOutput("4");
        }

        [WpfFact]
        public void VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashR()
        {
            VisualStudio.InteractiveWindow.SubmitText("#r \"System.Numerics\"");
            VisualStudio.InteractiveWindow.SubmitText("using System.Numerics;");
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx",
                "class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                VisualStudio.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudio.InteractiveWindow.SubmitText(@"var comp = new Complex();
comp.goo()");
                VisualStudio.InteractiveWindow.WaitForLastReplOutput("4");
            }
        }

        [WpfFact]
        public void VerifyAssembliesReferencedByDefault()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"InteractiveHost64\"");
        }

        [WpfFact]
        public void VerifyHashLoadDirective()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx",
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                VisualStudio.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudio.InteractiveWindow.WaitForLastReplOutput("2");
                VisualStudio.InteractiveWindow.SubmitText("#load text");
                VisualStudio.InteractiveWindow.WaitForLastReplOutput("CS7010: Quoted file name expected");
            }
        }

        [WpfFact]
        public void VerifySquiggleAndErrorMessageUnderIncorrectDirective()
        {
            VisualStudio.InteractiveWindow.SubmitText("#goo");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(1);
            VisualStudio.InteractiveWindow.SubmitText("#reset");

            VisualStudio.InteractiveWindow.SubmitText("#bar");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(2);
        }

        [WpfFact]
        public void VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelp()
        {
            VisualStudio.InteractiveWindow.SubmitText("#help");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput(@"Keyboard shortcuts:
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

        [WpfFact]
        public void VerifyHashCls()
        {
            VisualStudio.InteractiveWindow.SubmitText("#cls");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [WpfFact]
        public void VerifyHashReset()
        {
            VisualStudio.InteractiveWindow.SubmitText("1+1");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("2");
            VisualStudio.InteractiveWindow.SubmitText("#reset");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [WpfFact]
        public void VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelp()
        {
            VisualStudio.InteractiveWindow.SubmitText("#reset /help");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains(@"Usage:
  #reset [noconfig]");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
            VisualStudio.InteractiveWindow.SubmitText("#load /help");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS7010: Quoted file name expected");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/8281")]
        public void VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterReset()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}");
            VisualStudio.InteractiveWindow.SubmitText("#reset");
            VisualStudio.InteractiveWindow.PlaceCaret("using");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudio.InteractiveWindow.PlaceCaret("{");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudio.InteractiveWindow.PlaceCaret("Main");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.InteractiveWindow.PlaceCaret("Hello");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "string");
            VisualStudio.InteractiveWindow.PlaceCaret("<summary", charsOffset: -1);
            VisualStudio.SendKeys.Send(Alt(VirtualKey.Right));
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.InteractiveWindow.PlaceCaret("summary");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudio.InteractiveWindow.PlaceCaret("innertext");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudio.InteractiveWindow.PlaceCaret("--");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudio.InteractiveWindow.PlaceCaret("comment");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudio.InteractiveWindow.PlaceCaret("CDATA");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.InteractiveWindow.PlaceCaret("cdata");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudio.InteractiveWindow.PlaceCaret("attribute");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.InteractiveWindow.PlaceCaret("Environment");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "class name");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [WpfFact]
        public void WorkspaceClearedAfterReset()
        {
            VisualStudio.InteractiveWindow.SubmitText("double M() { return 13.1; }");
            VisualStudio.InteractiveWindow.SubmitText("M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("13.1");
            VisualStudio.InteractiveWindow.SubmitText("double M() { return M(); }");
            VisualStudio.InteractiveWindow.SubmitText("M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            VisualStudio.InteractiveWindow.SubmitText("M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS0103");
            VisualStudio.InteractiveWindow.SubmitText("double M() { return M(); }");
            VisualStudio.InteractiveWindow.SubmitText("M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("Process is terminated due to StackOverflowException.");
            VisualStudio.InteractiveWindow.SubmitText("double M() { return 13.2; }");
            VisualStudio.InteractiveWindow.SubmitText("M()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("13.2");
        }

        [WpfFact]
        public void InitializationAfterReset()
        {
            VisualStudio.InteractiveWindow.SubmitText("#reset");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.");
            VisualStudio.InteractiveWindow.SubmitText("#reset noconfig");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("Resetting execution engine.");
        }
    }
}
