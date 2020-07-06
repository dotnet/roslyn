' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_DateAndTime
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionListIsShownWithDefaultFormatStringAfterTypingFirstQuote(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString($$);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("""")
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString(""G)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionListIsNotShownAfterTypingEndingQuote(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString(" $$);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("""")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionListIsShownWithDefaultFormatStringAfterTypingFormattingComponentColon(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {d$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(":")
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("_ = $""Text {d:G}"";", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionListIsNotShownAfterTypingColonWithinFormattingComponent(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {d:hh$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(":")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString("$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString(""G"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_OverwriteExisting(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString(":ff$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ff")
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("FF")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString("":FF"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_BeginningOfWord(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString("$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("f")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_MiddleOfWord(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString("f$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExample1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        d.ToString("hh:mm:$$");
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("ss")
                Await state.AssertSelectedCompletionItem("ss", inlineDescription:=FeaturesResources.second_2_digits)

                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Dim text = description.Text

                If CultureInfo.CurrentCulture.Name <> "en-US" Then
                    Assert.Contains($"hh:mm:ss (en-US) → 01:45:30", text)
                    Assert.Contains($"hh:mm:ss ({CultureInfo.CurrentCulture.Name}) → 01:45:30", text)
                Else
                    Assert.Contains("hh:mm:ss → 01:45:30", text)
                End If
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_StringInterpolation1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {d:$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("_ = $""Text {d:G}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_StringInterpolation2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = @$"Text {d:$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("_ = @$""Text {d:G}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_OverwriteExisting_StringInterpolation1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {d:ff$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ff")
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("FF")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("_ = $""Text {d:FF}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_OverwriteExisting_StringInterpolation2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = @$"Text {d:ff$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ff")
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("FF")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("_ = @$""Text {d:FF}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_BeginningOfWord_StringInterpolation1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {d:$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("f")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_BeginningOfWord_StringInterpolation2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $@"Text {d:$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("f")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExample1_StringInterpolation1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[
        using System;
        class c
        {
            void goo(DateTime d)
            {
                _ = $"Text {d:hh:mm:$$}";
            }
        }
        ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("ss")
                Await state.AssertSelectedCompletionItem("ss", inlineDescription:=FeaturesResources.second_2_digits)

                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Dim text = description.Text

                If CultureInfo.CurrentCulture.Name <> "en-US" Then
                    Assert.Contains($"hh:mm:ss (en-US) → 01:45:30", text)
                    Assert.Contains($"hh:mm:ss ({CultureInfo.CurrentCulture.Name}) → 01:45:30", text)
                Else
                    Assert.Contains("hh:mm:ss → 01:45:30", text)
                End If
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExample1_StringInterpolation2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[
        using System;
        class c
        {
            void goo(DateTime d)
            {
                _ = @$"Text {d:hh:mm:$$}";
            }
        }
        ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("ss")
                Await state.AssertSelectedCompletionItem("ss", inlineDescription:=FeaturesResources.second_2_digits)

                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Dim text = description.Text

                If CultureInfo.CurrentCulture.Name <> "en-US" Then
                    Assert.Contains($"hh:mm:ss (en-US) → 01:45:30", text)
                    Assert.Contains($"hh:mm:ss ({CultureInfo.CurrentCulture.Name}) → 01:45:30", text)
                Else
                    Assert.Contains("hh:mm:ss → 01:45:30", text)
                End If
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_MiddleOfWord_StringInterpolation1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = $"Text {date:f$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeChar_MiddleOfWord_StringInterpolation2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class c
{
    void goo(DateTime d)
    {
        _ = @$"Text {date:f$$}";
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
