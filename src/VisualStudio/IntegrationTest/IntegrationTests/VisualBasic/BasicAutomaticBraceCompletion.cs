// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicAutomaticBraceCompletion : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicAutomaticBraceCompletion(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicAutomaticBraceCompletion))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
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
               VirtualKeyCode.ESCAPE,
               VirtualKeyCode.TAB);

            VisualStudio.Editor.Verify.CurrentLineText("Dim x = {New Object}$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim x = {");
            VisualStudio.Editor.SendKeys(VirtualKeyCode.RETURN);
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudio.Editor.SendKeys("Sub Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("x As Long");
            VisualStudio.Editor.SendKeys(VirtualKeyCode.ESCAPE);
            VisualStudio.Editor.SendKeys(VirtualKeyCode.TAB);
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo(x As Long)$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudio.Editor.SendKeys("Sub Goo(");
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKeyCode.ESCAPE);
            VisualStudio.Editor.SendKeys(')');
            VisualStudio.Editor.Verify.CurrentLineText("Sub Goo()$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys("Dim str = \"");
            VisualStudio.Editor.Verify.CurrentLineText("Dim str = \"$$\"", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKeyCode.TAB);
            VisualStudio.Editor.Verify.CurrentLineText("Dim str = \"\"$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
               VirtualKeyCode.ESCAPE,
               "]:=\"hello({[\")}",
               VirtualKeyCode.RETURN);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
               VirtualKeyCode.ESCAPE,
               VirtualKeyCode.TAB,
               ":=\"hello({[",
               VirtualKeyCode.TAB,
               VirtualKeyCode.TAB,
               VirtualKeyCode.TAB,
               VirtualKeyCode.RETURN);
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VirtualWhitespaceIssue()
        {
            SetUpEditor(@"
Class C
    Sub Goo()$$
    End Sub
End Class");

            VisualStudio.Editor.SendKeys(VirtualKeyCode.RETURN);
            VisualStudio.Editor.SendKeys('(');
            VisualStudio.Editor.SendKeys(VirtualKeyCode.BACK);

            VisualStudio.Editor.Verify.CurrentLineText("        $$", assertCaretPosition: true, trimWhitespace: false);
        }

        [WorkItem(659684, "DevDiv")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
