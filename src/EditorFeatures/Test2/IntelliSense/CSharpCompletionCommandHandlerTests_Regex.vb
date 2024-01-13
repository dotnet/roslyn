' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_Regex
        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A", inlineDescription:=FeaturesResources.Regex_start_of_string_only_short)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""\\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke_Utf8(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex("$$"u8);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A", inlineDescription:=FeaturesResources.Regex_start_of_string_only_short)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""\\A""u8)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke_VerbatimString(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke_VerbatimUtf8String(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"$$"u8);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""\A""u8)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke_RawSingleLineString(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex("""$$ """);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""""""\A """""")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExplicitInvoke_RawMultiLineString(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex("""
            $$
            """);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains(" \A", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCaretPlacement(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("[")

                Await state.AssertSelectedCompletionItem($"[  {FeaturesResources.Regex_character_group}  ]")
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestBackslashBInCharacterClass(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("\b")

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=FeaturesResources.Regex_backspace_character_long)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestBackslashBOutOfCharacterClass(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("\b")

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=FeaturesResources.Regex_word_boundary_long)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function OnlyEscapes(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("\", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function OnlyClasses(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("[")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("[", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function OnlyGroups(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("(", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestKReferenceOutsideOfCharacterClass(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNoKReferenceInsideOfCharacterClass(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("\")
                Await state.AssertCompletionSession()

                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestCategory(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("{")
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "IsGreek"))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNegativeCategory(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class c
{
    void goo()
    {
        var r = new Regex(@"\P$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("{")
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "IsGreek"))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedString(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedStringPart(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Dim items = state.GetCompletionItems()

                Assert.False(items.Any(Function(c) c.DisplayText = "*"))
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedStringPrefix(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedStringSuffix(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedVerbatimString1(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NotInInterpolatedVerbatimString2(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
