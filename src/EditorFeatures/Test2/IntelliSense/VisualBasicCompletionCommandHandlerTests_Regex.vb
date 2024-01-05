' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class VisualBasicCompletionCommandHandlerTests_Regex

        <WpfFact>
        Public Async Function ExplicitInvoke() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
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

        <WpfFact>
        Public Async Function TestCaretPlacement() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System.Text.RegularExpressions
class c
    sub goo()
        dim r = New Regex("$$")
    end sub
end class
]]></Document>)

                state.SendTypeChars("[")

                Await state.AssertSelectedCompletionItem($"[  {FeaturesResources.Regex_character_group}  ]")
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

        <WpfFact>
        Public Async Function OnlyEscapes() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
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

        <WpfFact>
        Public Async Function OnlyClasses() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
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

        <WpfFact>
        Public Async Function OnlyGroups() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
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
