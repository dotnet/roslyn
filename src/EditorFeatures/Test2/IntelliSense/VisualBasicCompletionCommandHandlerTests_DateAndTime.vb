' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class VisualBasicCompletionCommandHandlerTests_DateAndTime
        <WpfFact>
        Public Async Function ExplicitInvoke() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        d.ToString("$$")
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString(""G"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function ExplicitInvoke_LanguageComment() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo()
        ' lang=datetime
        dim d = "$$"
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("dim d = ""G""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function ExplicitInvoke_StringSyntaxAttribute_Argument() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
imports system.diagnostics.codeanalysis

class c
    sub goo()
        M("$$")
    end sub

    sub M(<StringSyntax(StringSyntaxAttribute.DateTimeFormat)> p as string)
    end sub
end class
]]>
                    <%= EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeVBXml %>
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("M(""G"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function ExplicitInvoke_OverwriteExisting() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        d.ToString(":ff$$")
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ff")
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("FF")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString("":FF"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function TypeChar_BeginningOfWord() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        d.ToString("$$")
    end sub
end class
]]></Document>)

                state.SendTypeChars("f")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("f")
            End Using
        End Function

        <WpfFact>
        Public Async Function TypeChar_MiddleOfWord() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        d.ToString("f$$")
    end sub
end class
]]></Document>)

                state.SendTypeChars("f")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function TestExample1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
<Document><![CDATA[
class c
    sub goo(d As Date)
        d.ToString("hh:mm:$$")
    end sub
end class
]]></Document>)

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

        <WpfFact>
        Public Async Function ExplicitInvoke_StringInterpolation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        Dim str = $"Text {d:$$}"
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("G", inlineDescription:=FeaturesResources.general_long_date_time)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Dim str = $""Text {d:G}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function ExplicitInvoke_OverwriteExisting_StringInterpolation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        Dim str = $"Text {d:ff$$}"
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ff")
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("FF")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Dim str = $""Text {d:FF}""", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact>
        Public Async Function TypeChar_BeginningOfWord_StringInterpolation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        Dim str = $"Text {d:$$}"
    end sub
end class
]]></Document>)

                state.SendTypeChars("f")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("f")
            End Using
        End Function

        <WpfFact>
        Public Async Function TestExample1_StringInterpolation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
<Document><![CDATA[
class c
    sub goo(d As Date)
        Dim str = $"Text {d:hh:mm:$$}"
    end sub
end class
        ]]></Document>)

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

        <WpfFact>
        Public Async Function TypeChar_MiddleOfWord_StringInterpolation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
class c
    sub goo(d As Date)
        Dim str = $"Text {date:f$$}"
    end sub
end class
]]></Document>)

                state.SendTypeChars("f")
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
