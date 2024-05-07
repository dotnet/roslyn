// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInteractiveDirectives : AbstractInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyHostCommandsCompletionList()
        {
            await TestServices.InteractiveWindow.InsertCodeAsync("#", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);

            var completionItems = (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
            Assert.All(
                [
                    "cls",
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
                    "warning",
                ],
                item => Assert.Contains(item, completionItems));
            Assert.All(
                ["int", "return", "System"],
                item => Assert.DoesNotContain(item, completionItems));

            await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.InsertCodeAsync(@"int x = 1; //
#", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.InvokeCompletionListAsync(HangMitigatingCancellationToken);

            completionItems = (await TestServices.InteractiveWindow.GetCompletionItemsAsync(HangMitigatingCancellationToken)).SelectAsArray(item => item.DisplayText);
            Assert.All(
                [
                    "elif",
                    "else",
                    "endif",
                    "endregion",
                    "error",
                    "if",
                    "line",
                    "pragma",
                    "region",
                    "warning",
                ],
                item => Assert.Contains(item, completionItems));
            Assert.All(
                [
                    "cls",
                    "help",
                    "load",
                    "prompt",
                    "reset",
                    "undef",
                    "define",
                ],
                item => Assert.DoesNotContain(item, completionItems));
        }

        [IdeFact]
        public async Task VerifyHashRDirective()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Numerics;
var bigInt = new BigInteger();
bigInt", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("[0]", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyLocalDeclarationWithTheSameNameHidesImportedMembersFromHashR()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Numerics;
class Complex { public int goo() { return 4; } }
var comp = new Complex();
comp.goo()", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("4", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyLocalDeclarationInCsxFileWithTheSameNameHidesImportedMembersFromHashR()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#r \"System.Numerics\"", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("using System.Numerics;", HangMitigatingCancellationToken);
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario4.csx",
                "class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                await TestServices.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName), HangMitigatingCancellationToken);
                await TestServices.InteractiveWindow.SubmitTextAsync(@"var comp = new Complex();
comp.goo()", HangMitigatingCancellationToken);
                await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("4", HangMitigatingCancellationToken);
            }
        }

        [IdeFact]
        public async Task VerifyAssembliesReferencedByDefault()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Diagnostics;
Process.GetCurrentProcess().ProcessName", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("\"InteractiveHost64\"", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyHashLoadDirective()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "directivesScenario6.csx",
                "System.Console.WriteLine(2);"))
            {
                temporaryTextFile.Create();
                await TestServices.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName), HangMitigatingCancellationToken);
                await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("2", HangMitigatingCancellationToken);
                await TestServices.InteractiveWindow.SubmitTextAsync("#load text", HangMitigatingCancellationToken);
                await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("(1,7): error CS7010: Quoted file name expected", HangMitigatingCancellationToken);
            }
        }

        [IdeFact]
        public async Task VerifySquiggleAndErrorMessageUnderIncorrectDirective()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#goo", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("(1,2): error CS1024: Preprocessor directive expected", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(1);
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync("#bar", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("(1,2): error CS1024: Preprocessor directive expected", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(2);
        }

        [IdeFact]
        public async Task VerifyHashHelpDirectiveOutputNoSquigglesUnderHashHelp()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#help", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync(@"Keyboard shortcuts:
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
  #reset               Reset the execution environment to the initial state and keep history, with the option to switch the runtime of the host process.
Script directives:
  #r                   Add a metadata reference to specified assembly and all its dependencies, e.g. #r ""myLib.dll"".
  #load                Load specified script file and execute it, e.g. #load ""myScript.csx"".", HangMitigatingCancellationToken);

            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task VerifyHashCls()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#cls", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task VerifyHashReset()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("1+1", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("2", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeFact]
        public async Task VerifyDisplayCommandUsageOutputNoSquigglesUnderSlashHelp()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset /help", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync(@"Usage:
  #reset [noconfig]", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
            await TestServices.InteractiveWindow.SubmitTextAsync("#load /help", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS7010: Quoted file name expected", HangMitigatingCancellationToken);
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/8281")]
        public async Task VerifyNoSquigglesErrorMessagesAndIntellisenseFeaturesContinueWorkingAfterReset()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"using static System.Console;
/// <summary>innertext
/// </summary>
/// --><!--comment--><!--
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
{
    WriteLine(""Hello World"");
}", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("using", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "keyword", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("{", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "punctuation", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("Main", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("Hello", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "string", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("<summary", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.RIGHT, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("summary", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - name", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("innertext", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - text", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("--", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - text", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("comment", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("CDATA", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("cdata", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("attribute", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("Environment", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            // TODO implement GetErrorListErrorCount: https://github.com/dotnet/roslyn/issues/18035
            // VerifyErrorCount(0);
        }

        [IdeTheory]
        [InlineData("32")]
        [InlineData("64")]
        [InlineData("core")]
        public async Task WorkspaceClearedAfterReset(string environment)
        {
            await TestServices.InteractiveWindow.SubmitTextAsync($"#reset {environment}", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.", HangMitigatingCancellationToken);

            var errorText = environment switch
            {
                "core" => "Stack overflow.",
                _ => "StackOverflowException.",
            };

            await TestServices.InteractiveWindow.SubmitTextAsync("double M() { return 13.1; }", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("M()", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("13.1", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("double M() { return M(); }", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("M()", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync(errorText, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("M()", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS0103", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("double M() { return M(); }", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("M()", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync(errorText, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("double M() { return 13.2; }", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("M()", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("13.2", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task InitializationAfterReset()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset noconfig", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("Resetting execution engine.", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("#reset 64", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync(@"Resetting execution engine.
Loading context from 'CSharpInteractive.rsp'.", HangMitigatingCancellationToken);
        }
    }
}
