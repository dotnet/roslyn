// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicAutomaticBraceCompletion : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicAutomaticBraceCompletion()
            : base(nameof(BasicAutomaticBraceCompletion))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim x = {");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim x = {$$}", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(
               "New Object",
               VirtualKey.Escape,
               VirtualKey.Tab);

            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim x = {New Object}$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim x = {");
            VisualStudioInstance.Editor.SendKeys('}');
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim x = {}$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ParenthesesTypeoverAfterStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Console.Write(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Console.Write($$)", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys('"');
            VisualStudioInstance.Editor.Verify.CurrentLineText("Console.Write(\"$$\")", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys('"');
            VisualStudioInstance.Editor.Verify.CurrentLineText("Console.Write(\"\"$$)", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(')');
            VisualStudioInstance.Editor.Verify.CurrentLineText("Console.Write(\"\")$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim x = {");
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.CurrentLineText("            $$}", assertCaretPosition: true, trimWhitespace: false);
            VisualStudioInstance.Editor.Verify.TextContains(@"
Class C
    Sub Goo()
        Dim x = {
            $$}
    End Sub
End Class",
assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudioInstance.Editor.SendKeys("Sub Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys("x As Long");
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Escape);
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("Sub Goo(x As Long)$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Paren_Overtyping()
        {
            SetUpEditor(@"
Class C
    $$
End Class");

            VisualStudioInstance.Editor.SendKeys("Sub Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Escape);
            VisualStudioInstance.Editor.SendKeys(')');
            VisualStudioInstance.Editor.Verify.CurrentLineText("Sub Goo()$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Bracket_Insertion()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim [Dim");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim [Dim$$]", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Bracket_Overtyping()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim [Dim");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim [Dim$$]", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys("] As Long");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim [Dim] As Long$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoubleQuote_InsertionAndTabCompletion()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim str = \"");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim str = \"$$\"", assertCaretPosition: true);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim str = \"\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys(
               "Dim y = {New C([dim",
               VirtualKey.Escape,
               "]:=\"hello({[\")}",
               VirtualKey.Enter);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys(
               "Dim y = {New C([dim",
               VirtualKey.Escape,
               VirtualKey.Tab,
               ":=\"hello({[",
               VirtualKey.Tab,
               VirtualKey.Tab,
               VirtualKey.Tab,
               VirtualKey.Enter);
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains("Dim y = {New C([dim]:=\"hello({[\")}", actualText);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInComments()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        ' $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("{([\"");
            VisualStudioInstance.Editor.Verify.CurrentLineText("' {([\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInStringLiterals()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim s = \"{([");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim s = \"{([$$\"", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Negative_NoCompletionInXmlDocComment()
        {
            SetUpEditor(@"
$$
Class C
End Class");

            VisualStudioInstance.Editor.SendKeys("'''");
            VisualStudioInstance.Editor.SendKeys('{');
            VisualStudioInstance.Editor.SendKeys('(');
            VisualStudioInstance.Editor.SendKeys('[');
            VisualStudioInstance.Editor.SendKeys('"');
            VisualStudioInstance.Editor.Verify.CurrentLineText("''' {([\"$$", assertCaretPosition: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys("(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("''' <see></see>($$", assertCaretPosition: true);
        }

        [WorkItem(652015, "DevDiv")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void LineCommittingIssue()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Dim x=\"\" '");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Dim x=\"\" '$$", assertCaretPosition: true);
        }

        [WorkItem(653399, "DevDiv")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void VirtualWhitespaceIssue()
        {
            SetUpEditor(@"
Class C
    Sub Goo()$$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Enter);
            VisualStudioInstance.Editor.SendKeys('(');
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Backspace);

            VisualStudioInstance.Editor.Verify.CurrentLineText("        $$", assertCaretPosition: true, trimWhitespace: false);
        }

        [WorkItem(659684, "DevDiv")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

            VisualStudioInstance.Editor.SendKeys("Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Goo($$)", assertCaretPosition: true);
        }

        [WorkItem(657451, "DevDiv")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CompletionAtTheEndOfFile()
        {
            SetUpEditor(@"
Class C
    $$");

            VisualStudioInstance.Editor.SendKeys("Sub Goo(");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Sub Goo($$)", assertCaretPosition: true);
        }
    }
}
