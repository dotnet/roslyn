' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_Regex
        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""\\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke_VerbatimString(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaretPlacement(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                state.SendDownKey()
                state.SendDownKey()
                state.SendDownKey()
                state.SendDownKey()
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(@""[^-]"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertLineTextAroundCaret("        var r = new Regex(@""[^", "-]"");")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyEscapes(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("\", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
