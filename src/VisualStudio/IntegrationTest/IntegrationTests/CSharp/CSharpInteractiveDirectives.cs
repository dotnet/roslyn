// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveDirectives : AbstractIdeInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyHostCommandsCompletionListAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(true);
            VisualStudio.InteractiveWindow.InsertCode("#");
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();

            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("cls",
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
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsDoNotExistAsync("int", "return", "System");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            VisualStudio.InteractiveWindow.InsertCode(@"int x = 1; //
#");
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();

            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync(
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

            await VisualStudio.InteractiveWindow.Verify.CompletionItemsDoNotExistAsync("cls",
                "help",
                "load",
                "prompt",
                "reset",
                "undef",
                "define");
        }

        [IdeFact]
        public async Task VerifyHashRDirectiveAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt");

            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("[0]");
        }

        [IdeFact]
        public async Task VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashRAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Numerics;
class Complex { public int goo() { return 4; } }
var comp = new Complex();
comp.goo()");

            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("4");
        }

        [IdeFact]
        public async Task VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashRAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("using System.Numerics;");
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx",
                "class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                await VisualStudio.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                await VisualStudio.InteractiveWindow.SubmitTextAsync(@"var comp = new Complex();
comp.goo()");
                await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("4");
            }
        }

        [IdeFact]
        public async Task VerifyAssembliesReferencedByDefaultAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"InteractiveHost64\"");
        }

        [IdeFact]
        public async Task VerifyHashLoadDirectiveAsync()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx",
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                await VisualStudio.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("2");
                await VisualStudio.InteractiveWindow.SubmitTextAsync("#load text");
                await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("CS7010: Quoted file name expected");
            }
        }

        [IdeFact]
        public async Task VerifySquiggleAndErrorMessageUnderIncorrectDirectiveAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#goo");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(1);
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset");

            await VisualStudio.InteractiveWindow.SubmitTextAsync("#bar");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("(1,2): error CS1024: Preprocessor directive expected");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(2);
        }

        [IdeFact]
        public async Task VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelpAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#help");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync(@"Keyboard shortcuts:
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

        [IdeFact]
        public async Task VerifyHashClsAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#cls");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task VerifyHashResetAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("1+1");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("2");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelpAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset /help");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync(@"Usage:
  #reset [noconfig]");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#load /help");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS7010: Quoted file name expected");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/8281")]
        public async Task VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterResetAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("using");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "keyword");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("{");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "punctuation");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Main");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Hello");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "string");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("<summary", charsOffset: -1);
            await VisualStudio.SendKeys.SendAsync(Alt(VirtualKey.Right));
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("summary");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - name");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("innertext");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - text");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("--");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - text");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("comment");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("CDATA");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("cdata");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("attribute");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Environment");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task WorkspaceClearedAfterResetAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("double M() { return 13.1; }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("13.1");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("double M() { return M(); }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("Process is terminated due to StackOverflowException.");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("double M() { return M(); }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("Process is terminated due to StackOverflowException.");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("double M() { return 13.2; }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("M()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("13.2");
        }

        [IdeFact]
        public async Task InitializationAfterResetAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#reset noconfig");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("Resetting execution engine.");
        }
    }
}
