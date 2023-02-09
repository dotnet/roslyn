// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    [Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
    public class BasicAutomaticBraceCompletion : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicAutomaticBraceCompletion(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicAutomaticBraceCompletion))
        {
        }

        [WpfTheory, CombinatorialData]
        public void Braces_InsertionAndTabCompleting(bool argumentCompletion)
        {
            VisualStudio.Workspace.SetArgumentCompletionSnippetsOption(argumentCompletion);

            // Disable new rename UI for now, it's causing these tests to fail.
            // https://github.com/dotnet/roslyn/issues/63576
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_UseNewUI, language: null, false);

            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim x = {");
            VisualStudio.Editor.Verify.CurrentLineText("Dim x = {$$}", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(
               "New Object",
               VirtualKey.Escape,
               VirtualKey.Tab);

            if (argumentCompletion)
            {
                VisualStudio.Editor.Verify.CurrentLineText("Dim x = {New Object($$)}", assertCaretPosition: true);
                VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);

                VisualStudio.Editor.SendKeys(VirtualKey.Tab);
                VisualStudio.Editor.Verify.CurrentLineText("Dim x = {New Object()$$}", assertCaretPosition: true);

                VisualStudio.Editor.SendKeys(VirtualKey.Tab);
                VisualStudio.Editor.Verify.CurrentLineText("Dim x = {New Object()}$$", assertCaretPosition: true);
            }
            else
            {
                VisualStudio.Editor.Verify.CurrentLineText("Dim x = {New Object}$$", assertCaretPosition: true);
            }
        }

        [WpfFact]
        public void Braces_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim x = {");
            VisualStudio.Editor.SendKeys('}');
            VisualStudio.Editor.Verify.CurrentLineText("Dim x = {}$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void ParenthesesTypeoverAfterStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Console.Write(");
            VisualStudio.Editor.Verify.CurrentLineText("Console.Write($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys('"');
            VisualStudio.Editor.Verify.CurrentLineText("Console.Write(\"$$\")", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys('"');
            VisualStudio.Editor.Verify.CurrentLineText("Console.Write(\"\"$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(')');
            VisualStudio.Editor.Verify.CurrentLineText("Console.Write(\"\")$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim x = {");
            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.Verify.CurrentLineText("            $$}", assertCaretPosition: true, trimWhitespace: false);
            VisualStudio.Editor.Verify.TextContains(@"
Class C
    Sub Goo()
        Dim x = {
            $$}
    End Sub
End Class",
assertCaretPosition: true);
        }

        [WpfFact]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudio.Editor.SendKeys("Sub Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("x As Long");
            VisualStudio.Editor.SendKeys(VirtualKey.Escape);
            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo(x As Long)$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudio.Editor.SendKeys("Sub Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Escape);
            VisualStudio.Editor.SendKeys(')');
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo()$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Bracket_Insertion()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim [Dim");
            VisualStudio.Editor.Verify.CurrentLineText("Dim [Dim$$]", assertCaretPosition: true);
        }

        [WpfFact]
        public void Bracket_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim [Dim");
            VisualStudio.Editor.Verify.CurrentLineText("Dim [Dim$$]", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("] As Long");
            VisualStudio.Editor.Verify.CurrentLineText("Dim [Dim] As Long$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            // Disable new rename UI for now, it's causing these tests to fail.
            // https://github.com/dotnet/roslyn/issues/63576
            VisualStudio.Workspace.SetGlobalOption(WellKnownGlobalOption.InlineRenameSessionOptions_UseNewUI, language: null, false);

            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim str = \"");
            VisualStudio.Editor.Verify.CurrentLineText("Dim str = \"$$\"", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Dim str = \"\"$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Nested_AllKinds_1()
        {
            SetUpEditor(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys(
               "Dim y = {New C([dim",
               VirtualKey.Escape,
               "]:=\"hello({[\")}",
               VirtualKey.Enter);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [WpfFact]
        public void Nested_AllKinds_2()
        {
            SetUpEditor(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys(
               "Dim y = {New C([dim",
               VirtualKey.Escape,
               VirtualKey.Tab,
               ":=\"hello({[",
               VirtualKey.Tab,
               VirtualKey.Tab,
               VirtualKey.Tab,
               VirtualKey.Enter);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [WpfFact]
        public void Negative_NoCompletionInComments()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        ' $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("{([\"");
            VisualStudio.Editor.Verify.CurrentLineText("' {([\"$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Negative_NoCompletionInStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim s = \"{([");
            VisualStudio.Editor.Verify.CurrentLineText("Dim s = \"{([$$\"", assertCaretPosition: true);
        }

        [WpfFact]
        public void Negative_NoCompletionInXmlDocComment()
        {
            SetUpEditor(@"
$$
Class C
End Class");

            VisualStudio.Editor.SendKeys("'''");
            VisualStudio.Editor.SendKeys('{');
            VisualStudio.Editor.SendKeys('(');
            VisualStudio.Editor.SendKeys('[');
            VisualStudio.Editor.SendKeys('"');
            VisualStudio.Editor.Verify.CurrentLineText("''' {([\"$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void Negative_NoCompletionInXmlDocCommentAtEndOfTag()
        {
            SetUpEditor(@"
Class C
    ''' <summary>
    ''' <see></see>$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("(");
            VisualStudio.Editor.Verify.CurrentLineText("''' <see></see>($$", assertCaretPosition: true);
        }

        [WorkItem(652015, "DevDiv")]
        [WpfFact]
        public void LineCommittingIssue()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim x=\"\" '");
            VisualStudio.Editor.Verify.CurrentLineText("Dim x=\"\" '$$", assertCaretPosition: true);
        }

        [WorkItem(653399, "DevDiv")]
        [WpfFact]
        public void VirtualWhitespaceIssue()
        {
            SetUpEditor(@"
Class C
    Sub Goo()$$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys('(');
            VisualStudio.Editor.SendKeys(VirtualKey.Backspace);

            VisualStudio.Editor.Verify.CurrentLineText("        $$", assertCaretPosition: true, trimWhitespace: false);
        }

        [WorkItem(659684, "DevDiv")]
        [WpfFact]
        public void CompletionWithIntelliSenseWindowUp()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Goo($$)", assertCaretPosition: true);
        }

        [WorkItem(657451, "DevDiv")]
        [WpfFact]
        public void CompletionAtTheEndOfFile()
        {
            SetUpEditor(@"
Class C
    $$");

            VisualStudio.Editor.SendKeys("Sub Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);
        }
    }
}
