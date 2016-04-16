' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class EnumCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        <Fact>
        <WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_EnumTypeDotMemberAlways() As Task
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)> Member
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact>
        <WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_EnumTypeDotMemberNever() As Task
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> Member
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact>
        <WorkItem(545678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEditorBrowsable_EnumTypeDotMemberAdvanced() As Task
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)> Member
End Enum
]]></Text>.Value
            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Function

        <Fact>
        <WorkItem(566787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566787")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTriggeredOnOpenParen() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        ' type after this line
        Bar($$
    End Sub
 
    Sub Bar(f As Foo)
    End Sub
End Module
 
Enum Foo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Foo.AMember", usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        <WorkItem(674390, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674390")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRightSideOfAssignment() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as Foo
        x = $$
    End Sub
End Module
 
Enum Foo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Foo.AMember", usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        <WorkItem(530491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530491")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotCrashInObjectInitializer() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim z = New Foo() With {.z$$ }
    End Sub

    Class Foo
        Property A As Integer
            Get

            End Get
            Set(value As Integer)

            End Set
        End Property
    End Class
End Module
]]></Text>.Value

            Await VerifyNoItemsExistAsync(markup)
        End Function

        <Fact>
        <WorkItem(809332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809332")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaseStatement() As Task
            Dim markup = <Text><![CDATA[
Enum E
    A
    B
    C
End Enum
 
Module Module1
    Sub Main(args As String())
        Dim value = E.A
 
        Select Case value
            Case $$
        End Select
 
    End Sub
End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "E.A", usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        <WorkItem(854099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854099")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInComment() As Task
            Dim markup = <Text><![CDATA[
Enum E
    A
    B
    C
End Enum
 
Module Module1
    Sub Main(args As String())
        Dim value = E.A
 
        Select Case value
            Case E.A | $$
        End Select
 
    End Sub
End Module
]]></Text>.Value

            Await VerifyNoItemsExistAsync(markup, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestInYieldReturn() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function M() As IEnumerable(Of DayOfWeek)
        Yield $$
    End Function
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "DayOfWeek.Friday")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
        Public Async Function TestInAsyncMethodReturnStatement() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Function M() As Task(Of DayOfWeek)
        Await Task.Delay(1)
        Return $$
    End Function
End Class
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "DayOfWeek.Friday")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(900625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900625")>
        Public Async Function TestInIndexedProperty() As Task
            Dim markup = <Text><![CDATA[
Module Module1

    Enum MyEnum
        flower
    End Enum

    Public Class MyClass1
        Public WriteOnly Property MyProperty(ByVal val1 As MyEnum) As Boolean
            Set(ByVal value As Boolean)

            End Set
        End Property

        Public Sub MyMethod(ByVal val1 As MyEnum)

        End Sub
    End Class

    Sub Main()
        Dim var As MyClass1 = New MyClass1
        ' MARKER
        var.MyMethod(MyEnum.flower)
        var.MyProperty($$MyEnum.flower) = True
    End Sub

End Module
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "MyEnum.flower")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916483")>
        Public Async Function TestFullyQualified() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As System.DayOfWeek)
        M($$)
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "System.DayOfWeek.Friday")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916467")>
        Public Async Function TestTriggeredForNamedArgument() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        M(day:=$$)
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916467")>
        Public Async Function TestNotTriggeredAfterAssignmentEquals() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Dim x = $$
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            Await VerifyItemIsAbsentAsync(markup, "DayOfWeek.A", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
        Public Async Function TestCaseStatementWithInt32InferredType() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Select Case day
            Case DayOfWeek.A
            Case $$
        End Select
    End Sub

    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "DayOfWeek.A")
            Await VerifyItemExistsAsync(markup, "DayOfWeek.B")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
        Public Async Function TestNotInTrivia() As Task
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Select Case day
            Case DayOfWeek.A,
                 DayOfWeek.B'$$
            Case
        End Select
    End Sub

    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            Await VerifyNoItemsExistAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
        Public Async Function TestLocalNoAs() As Task
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = e$$
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "e")
            Await VerifyItemIsAbsentAsync(markup, "e As E")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
        Public Async Function TestIncludeEnumAfterTyping() As Task
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = e$$
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "E")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(1015797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015797")>
        Public Async Function TestCommitOnComma() As Task
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = $$
    End Sub
End Class
]]></Text>.Value

            Dim expected = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = E.A,
    End Sub
End Class
]]></Text>.Value

            Await VerifyProviderCommitAsync(markup, "E.A", expected, ","c, textTypedSoFar:="")
        End Function

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New EnumCompletionProvider()
        End Function
    End Class
End Namespace