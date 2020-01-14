' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_Regex

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex("$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A", inlineDescription:=WorkspacesResources.Regex_start_of_string_only_short)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""\\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_VerbatimString() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/35631"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaretPlacement() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("[")

                Await state.AssertSelectedCompletionItem("[  character-group  ]")
                state.SendDownKey()
                state.SendDownKey()
                state.SendDownKey()
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("[^  firstCharacter-lastCharacter  ]")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""[^-]"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertLineTextAroundCaret("        var r = new Regex(@""[^", "-]"");")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackslashBInCharacterClass() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"[$$]");
    }
}
]]></Document>)

                state.SendTypeChars("\b")

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=WorkspacesResources.Regex_backspace_character_long)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackslashBOutOfCharacterClass() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("\b")

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=WorkspacesResources.Regex_word_boundary_long)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyEscapes() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("\", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyClasses() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("[")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("[", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyGroups() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("(", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKReferenceOutsideOfCharacterClass() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$");
    }
}
]]></Document>)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoKReferenceInsideOfCharacterClass() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"[$$]");
    }
}
]]></Document>)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCategory() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"\p$$");
    }
}
]]></Document>)

                state.SendTypeChars("{")
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "IsGreek"))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedString() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex($"$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringPart() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex($"goo{$$}bar");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Dim items = state.GetCompletionItems()

                Assert.False(items.Any(Function(c) c.DisplayText = "*"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringPrefix() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex($"go$$o{0}bar");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringSuffix() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex($"goo{0}$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedVerbatimString1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex($@"$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedVerbatimString2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@$"$$");
    }
}
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
