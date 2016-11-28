// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicAutomaticBraceCompletion : AbstractEditorTests
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

            SendKeys("Dim x = {");
            VerifyCurrentLineText("Dim x = {$$}", assertCaretPosition: true);

            SendKeys(
                "New Object",
                VirtualKey.Escape,
                VirtualKey.Tab);

            VerifyCurrentLineText("Dim x = {New Object}$$", assertCaretPosition: true);
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

            SendKeys("Dim x = {");
            SendKeys('}');
            VerifyCurrentLineText("Dim x = {}$$", assertCaretPosition: true);
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

            SendKeys("Console.Write(");
            VerifyCurrentLineText("Console.Write($$)", assertCaretPosition: true);

            SendKeys('"');
            VerifyCurrentLineText("Console.Write(\"$$\")", assertCaretPosition: true);

            SendKeys('"');
            VerifyCurrentLineText("Console.Write(\"\"$$)", assertCaretPosition: true);

            SendKeys(')');
            VerifyCurrentLineText("Console.Write(\"\")$$", assertCaretPosition: true);
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

            SendKeys("Dim x = {");
            SendKeys(VirtualKey.Enter);
            VerifyCurrentLineText("            $$}", assertCaretPosition: true, trimWhitespace: false);

            VerifyTextContains(@"
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

            SendKeys("Sub Foo(");
            VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);

            SendKeys("x As Long");
            SendKeys(VirtualKey.Escape);
            SendKeys(VirtualKey.Tab);
            VerifyCurrentLineText("Sub Foo(x As Long)$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            SendKeys("Sub Foo(");
            VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);

            SendKeys(VirtualKey.Escape);
            SendKeys(')');
            VerifyCurrentLineText("Sub Foo()$$", assertCaretPosition: true);
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

            SendKeys("Dim [Dim");
            VerifyCurrentLineText("Dim [Dim$$]", assertCaretPosition: true);
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

            SendKeys("Dim [Dim");
            VerifyCurrentLineText("Dim [Dim$$]", assertCaretPosition: true);

            SendKeys("] As Long");
            VerifyCurrentLineText("Dim [Dim] As Long$$", assertCaretPosition: true);
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

            SendKeys("Dim str = \"");
            VerifyCurrentLineText("Dim str = \"$$\"", assertCaretPosition: true);

            SendKeys(VirtualKey.Tab);
            VerifyCurrentLineText("Dim str = \"\"$$", assertCaretPosition: true);
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

            SendKeys(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                "]:=\"hello({[\")}",
                VirtualKey.Enter);

            VerifyTextContains("Dim y = {New C([dim]:=\"hello({[\")}");
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

            SendKeys(
                "Dim y = {New C([dim",
                VirtualKey.Escape,
                VirtualKey.Tab,
                ":=\"hello({[",
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Tab,
                VirtualKey.Enter);

            VerifyTextContains("Dim y = {New C([dim]:=\"hello({[\")}");
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

            SendKeys("{([\"");
            VerifyCurrentLineText("' {([\"$$", assertCaretPosition: true);
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

            SendKeys("Dim s = \"{([");
            VerifyCurrentLineText("Dim s = \"{([$$\"", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComment()
        {
            SetUpEditor(@"
$$
Class C
End Class");

            SendKeys("'''");
            SendKeys('{');
            SendKeys('(');
            SendKeys('[');
            SendKeys('"');
            VerifyCurrentLineText("''' {([\"$$", assertCaretPosition: true);
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

            SendKeys("(");
            VerifyCurrentLineText("''' <see></see>($$", assertCaretPosition: true);
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

            SendKeys("Dim x=\"\" '");
            VerifyCurrentLineText("Dim x=\"\" '$$", assertCaretPosition: true);
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

            SendKeys(VirtualKey.Enter);
            SendKeys('(');
            SendKeys(VirtualKey.Backspace);

            VerifyCurrentLineText("        $$", assertCaretPosition: true, trimWhitespace: false);
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

            SendKeys("Foo(");
            VerifyCurrentLineText("Foo($$)", assertCaretPosition: true);
        }

        [WorkItem(657451, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CompletionAtTheEndOfFile()
        {
            SetUpEditor(@"
Class C
    $$");

            SendKeys("Sub Foo(");
            VerifyCurrentLineText("Sub Foo($$)", assertCaretPosition: true);
        }
    }
}
