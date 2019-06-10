' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Await state.AssertSelectedCompletionItem("\A", inlineDescription:=WorkspacesResources.Regex_start_of_string_only_short)
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
        <WpfTheory(Skip := "https://github.com/dotnet/roslyn/issues/35631"), Trait(Traits.Feature, Traits.Features.Completion)>
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

                ' WaitForAsynchronousOperationsAsync is not enough for waiting in the async completion.
                ' To be sure that calculations are done, need to check session.GetComputedItems,
                ' E.g. via AssertSelectedCompletionItem.
                Await state.WaitForAsynchronousOperationsAsync()
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackslashBInCharacterClass(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=WorkspacesResources.Regex_backspace_character_long)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackslashBOutOfCharacterClass(completionImplementation As CompletionImplementation) As Task
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

                state.SendTypeChars("\b")

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("\b", description:=WorkspacesResources.Regex_word_boundary_long)
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyClasses(completionImplementation As CompletionImplementation) As Task
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

                state.SendTypeChars("(")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()

                For Each item In state.GetCompletionItems()
                    Assert.StartsWith("(", item.DisplayText)
                Next

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKReferenceOutsideOfCharacterClass(completionImplementation As CompletionImplementation) As Task
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

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoKReferenceInsideOfCharacterClass(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()

                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText.StartsWith("\k")))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCategory(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()

                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "IsGreek"))

                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedString(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringPart(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Dim items = state.GetCompletionItems()

                Assert.False(items.Any(Function(c) c.DisplayText = "*"))
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringPrefix(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedStringSuffix(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedVerbatimString1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInInterpolatedVerbatimString2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function
    End Class
End Namespace
