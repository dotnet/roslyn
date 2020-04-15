' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_DateAndTime
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
                Await state.AssertSelectedCompletionItem("d", inlineDescription:=FeaturesResources.short_date)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("d.ToString(""d"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists,
                extraExportedTypes:=New List(Of Type) From {GetType(TestCurrentCultureProvider)})

                Dim culture = state.Workspace.Services.GetService(Of TestCurrentCultureProvider)
                culture.SetCurrentCulture(CultureInfo.GetCultureInfo("en-US"))

                state.SendInvokeCompletionList()
                state.SendTypeChars("ss")
                Await state.AssertSelectedCompletionItem("ss", inlineDescription:=FeaturesResources.second_2_digits)

                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Assert.Contains(description.Text, "hh:mm:ss → 01:45:30")
                Dim text = description.Text
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExample2(showCompletionInArgumentLists As Boolean) As Task
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
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists,
                extraExportedTypes:=New List(Of Type) From {GetType(TestCurrentCultureProvider)})

                Dim culture = state.Workspace.Services.GetService(Of TestCurrentCultureProvider)
                culture.SetCurrentCulture(CultureInfo.GetCultureInfo("fr-FR"))

                state.SendInvokeCompletionList()
                state.SendTypeChars("ss")
                Await state.AssertSelectedCompletionItem("ss", inlineDescription:=FeaturesResources.second_2_digits)

                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Dim text = description.Text
                Assert.Contains(description.Text, "hh:mm:ss → 01:45:30")
            End Using
        End Function
    End Class
End Namespace
