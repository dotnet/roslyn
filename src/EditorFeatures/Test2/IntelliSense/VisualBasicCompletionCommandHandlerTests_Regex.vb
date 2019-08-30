' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class VisualBasicCompletionCommandHandlerTests_Regex
        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExplicitInvoke(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = new Regex("$$")
    end sub
end class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("\A")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new Regex(""\A"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory(Skip := "https://github.com/dotnet/roslyn/issues/35631"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaretPlacement(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = New Regex("$$")
    end sub
end class
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
                Assert.Contains("New Regex(""[^-]"")", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertLineTextAroundCaret("        dim r = New Regex(""[^", "-]"")")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyEscapes(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = new Regex("$$")
    end sub
end class
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyClasses(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = new Regex("$$")
    end sub
end class
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyGroups(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = new Regex("$$")
    end sub
end class
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
    End Class
End Namespace
