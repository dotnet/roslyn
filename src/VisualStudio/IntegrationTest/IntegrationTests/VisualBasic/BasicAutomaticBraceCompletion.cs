// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Fixtures;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicAutomaticBraceCompletion : AbstractIdeIntegrationTest, IClassFixture<VisualBasicClassLibraryProjectFixture>
    {
        private readonly VisualBasicClassLibraryProjectFixture _visualBasicClassLibraryProject;

        public BasicAutomaticBraceCompletion(VisualBasicClassLibraryProjectFixture visualBasicClassLibraryProject)
        {
            _visualBasicClassLibraryProject = visualBasicClassLibraryProject;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await _visualBasicClassLibraryProject.CreateOrOpenAsync(nameof(BasicAutomaticBraceCompletion));
        }

        protected override async Task CleanUpOpenSolutionAsync()
        {
            // Close but do not delete the solution.
            await TestServices.SolutionExplorer.CloseSolutionAsync();
        }

        protected override async Task CleanUpPendingOperationsAsync()
        {
            // Only wait for Workspace during cleanup. The class fixture will wait for all operations before moving to
            // the next test class.
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        private async Task SetUpEditorAsync(string markupCode)
        {
            await _visualBasicClassLibraryProject.SetUpEditorAsync(markupCode);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_InsertionAndTabCompletingAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim x = {");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim x = {$$}", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(
                "New Object",
                VirtualKey.Escape,
                VirtualKey.Tab);

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim x = {New Object}$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

             await VisualStudio.Editor.SendKeysAsync("Dim x = {");
             await VisualStudio.Editor.SendKeysAsync('}');
             await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim x = {}$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ParenthesesTypeoverAfterStringLiteralsAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Console.Write(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Console.Write($$)", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync('"');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Console.Write(\"$$\")", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync('"');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Console.Write(\"\"$$)", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(')');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Console.Write(\"\")$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBraceAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim x = {");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Enter);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("            $$}", assertCaretPosition: true, trimWhitespace: false);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Class C
    Sub Goo()
        Dim x = {
            $$}
    End Sub
End Class",
assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_InsertionAndTabCompletingAsync()
        {
            await SetUpEditorAsync(@"
Class C
    $$
End Class");

            await VisualStudio.Editor.SendKeysAsync("Sub Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Sub Goo($$)", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync("x As Long");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Escape);
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Sub Goo(x As Long)$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Paren_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
Class C
    $$
End Class");

            await VisualStudio.Editor.SendKeysAsync("Sub Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Sub Goo($$)", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Escape);
            await VisualStudio.Editor.SendKeysAsync(')');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Sub Goo()$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Bracket_InsertionAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim [Dim");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim [Dim$$]", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Bracket_OvertypingAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim [Dim");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim [Dim$$]", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync("] As Long");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim [Dim] As Long$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DoubleQuote_InsertionAndTabCompletionAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim str = \"");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim str = \"$$\"", assertCaretPosition: true);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Tab);
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim str = \"\"$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Nested_AllKinds_1Async()
        {
            await SetUpEditorAsync(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                "]:=\"hello({[\")}",
                VirtualKey.Enter);
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Nested_AllKinds_2Async()
        {
            await SetUpEditorAsync(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                VirtualKey.Tab,
                ":=\"hello({[",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Enter);
            var actualText = await VisualStudio.Editor.GetTextAsync();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInCommentsAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        ' $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("{([\"");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("' {([\"$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInStringLiteralsAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim s = \"{([");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim s = \"{([$$\"", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInXmlDocCommentAsync()
        {
            await SetUpEditorAsync(@"
$$
Class C
End Class");

            await VisualStudio.Editor.SendKeysAsync("'''");
            await VisualStudio.Editor.SendKeysAsync('{');
            await VisualStudio.Editor.SendKeysAsync('(');
            await VisualStudio.Editor.SendKeysAsync('[');
            await VisualStudio.Editor.SendKeysAsync('"');
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("''' {([\"$$", assertCaretPosition: true);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Negative_NoCompletionInXmlDocCommentAtEndOfTagAsync()
        {
            await SetUpEditorAsync(@"
Class C
    ''' <summary>
    ''' <see></see>$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("''' <see></see>($$", assertCaretPosition: true);
        }

        [WorkItem(652015, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task LineCommittingIssueAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Dim x=\"\" '");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Dim x=\"\" '$$", assertCaretPosition: true);
        }

        [WorkItem(653399, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task VirtualWhitespaceIssueAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()$$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Enter);
            await VisualStudio.Editor.SendKeysAsync('(');
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Backspace);

            await VisualStudio.Editor.Verify.CurrentLineTextAsync("        $$", assertCaretPosition: true, trimWhitespace: false);
        }

        [WorkItem(659684, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task CompletionWithIntelliSenseWindowUpAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Goo($$)", assertCaretPosition: true);
        }

        [WorkItem(657451, "DevDiv")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task CompletionAtTheEndOfFileAsync()
        {
            await SetUpEditorAsync(@"
Class C
    $$");

            await VisualStudio.Editor.SendKeysAsync("Sub Goo(");
            await VisualStudio.Editor.Verify.CurrentLineTextAsync("Sub Goo($$)", assertCaretPosition: true);
        }
    }
}
