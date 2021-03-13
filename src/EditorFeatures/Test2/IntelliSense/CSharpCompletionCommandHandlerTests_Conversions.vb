' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_Conversions
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x)", state.GetLineTextFromCaretPosition())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_BetweenDots(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x).ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_PartiallyWritten_Before(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.$$by.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("CompareTo")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_PartiallyWritten_After(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = 0;
        var y = x.by$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("(byte)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte)x).ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_NullableType_Dot(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = (int?)0;
        var y = x.$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte?)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte?)x)", state.GetLineTextFromCaretPosition())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuiltInConversion_NullableType_Question_BetweenDots(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Text.RegularExpressions;
class C
{
    void goo()
    {
        var x = (int?)0;
        var y = x?.$$.ToString();
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("by")
                Await state.AssertSelectedCompletionItem("(byte?)")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        var y = ((byte?)x)?.ToString();", state.GetLineTextFromCaretPosition())
                Assert.Equal(".", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function
    End Class
End Namespace
