// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim x = {");
            this.VerifyCurrentLineText("Dim x = {$$}", assertCaretPosition: true);

            this.SendKeys(
                "New Object",
                VirtualKey.Escape,
                VirtualKey.Tab);

            this.VerifyCurrentLineText("Dim x = {New Object}$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim x = {");
            this.SendKeys('}');
            this.VerifyCurrentLineText("Dim x = {}$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ParenthesesTypeoverAfterStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Console.Write(");
            this.VerifyCurrentLineText("Console.Write($$)", assertCaretPosition: true);

            this.SendKeys('"');
            this.VerifyCurrentLineText("Console.Write(\"$$\")", assertCaretPosition: true);

            this.SendKeys('"');
            this.VerifyCurrentLineText("Console.Write(\"\"$$)", assertCaretPosition: true);

            this.SendKeys(')');
            this.VerifyCurrentLineText("Console.Write(\"\")$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim x = {");
            this.SendKeys(VirtualKey.Enter);
            this.VerifyCurrentLineText("            $$}", assertCaretPosition: true, trimWhitespace: false);
            this.VerifyTextContains(@"
Class C
    Sub Foo()
        Dim x = {
            $$}
    End Sub
End Class",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            this.SendKeys("Sub Foo(");
            this.VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);

            this.SendKeys("x As Long");
            this.SendKeys(VirtualKey.Escape);
            this.SendKeys(VirtualKey.Tab);
            this.VerifyCurrentLineText("Sub Foo(x As Long)$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            this.SendKeys("Sub Foo(");
            this.VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);

            this.SendKeys(VirtualKey.Escape);
            this.SendKeys(')');
            this.VerifyCurrentLineText("Sub Foo()$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Bracket_Insertion()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim [Dim");
            this.VerifyCurrentLineText("Dim [Dim$$]", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Bracket_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim [Dim");
            this.VerifyCurrentLineText("Dim [Dim$$]", assertCaretPosition: true);

            this.SendKeys("] As Long");
            this.VerifyCurrentLineText("Dim [Dim] As Long$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim str = \"");
            this.VerifyCurrentLineText("Dim str = \"$$\"", assertCaretPosition: true);

            this.SendKeys(VirtualKey.Tab);
            this.VerifyCurrentLineText("Dim str = \"\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Nested_AllKinds_1()
        {
            SetUpEditor(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                "]:=\"hello({[\")}",
                VirtualKey.Enter);
            var actualText = Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Nested_AllKinds_2()
        {
            SetUpEditor(@"
Class C
    Sub New([dim] As String)
    End Sub

    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                VirtualKey.Tab,
                ":=\"hello({[",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Enter);
            var actualText = Editor.GetText();
            Assert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInComments()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        ' $$
    End Sub
End Class");

            this.SendKeys("{([\"");
            this.VerifyCurrentLineText("' {([\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim s = \"{([");
            this.VerifyCurrentLineText("Dim s = \"{([$$\"", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComment()
        {
            SetUpEditor(@"
$$
Class C
End Class");

            this.SendKeys("'''");
            this.SendKeys('{');
            this.SendKeys('(');
            this.SendKeys('[');
            this.SendKeys('"');
            this.VerifyCurrentLineText("''' {([\"$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocCommentAtEndOfTag()
        {
            SetUpEditor(@"
Class C
    ''' <summary>
    ''' <see></see>$$
    ''' </summary>
    Sub Foo()
    End Sub
End Class");

            this.SendKeys("(");
            this.VerifyCurrentLineText("''' <see></see>($$", assertCaretPosition: true);
        }

        [WorkItem(652015, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void LineCommittingIssue()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            this.SendKeys("Dim x=\"\" '");
            this.VerifyCurrentLineText("Dim x=\"\" '$$", assertCaretPosition: true);
        }

        [WorkItem(653399, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VirtualWhitespaceIssue()
        {
            SetUpEditor(@"
Class C
    Sub Foo()$$
    End Sub
End Class");

            this.SendKeys(VirtualKey.Enter);
            this.SendKeys('(');
            this.SendKeys(VirtualKey.Backspace);

            this.VerifyCurrentLineText("        $$", assertCaretPosition: true, trimWhitespace: false);
        }

        [WorkItem(659684, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CompletionWithIntelliSenseWindowUp()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            this.SendKeys("Foo(");
            this.VerifyCurrentLineText("Foo($$)", assertCaretPosition: true);
        }

        [WorkItem(657451, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CompletionAtTheEndOfFile()
        {
            SetUpEditor(@"
Class C
    $$");

            this.SendKeys("Sub Foo(");
            this.VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);
        }
    }
}
