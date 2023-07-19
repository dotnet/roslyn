' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class EnumCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(EnumCompletionProvider)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545678")>
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

            HideAdvancedMembers = True

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)

            HideAdvancedMembers = False

            Await VerifyItemInEditorBrowsableContextsAsync(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566787")>
        Public Async Function TestTriggeredOnOpenParen() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        ' type after this line
        Bar($$
    End Sub
 
    Sub Bar(f As Goo)
    End Sub
End Module
 
Enum Goo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674390")>
        Public Async Function TestRightSideOfAssignment() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as Goo
        x = $$
    End Sub
End Module
 
Enum Goo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            Await VerifyItemExistsAsync(markup, "Goo.AMember", usePreviousCharAsTrigger:=True)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530491")>
        Public Async Function TestDoNotCrashInObjectInitializer() As Task
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim z = New Goo() With {.z$$ }
    End Sub

    Class Goo
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809332")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854099")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/827897")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900625")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916483")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916467")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916467")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/815963")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015797")>
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

            Await VerifyProviderCommitAsync(markup, "E.A", expected, ","c)
        End Function

        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=201807&triage=true&_a=edit")>
        Public Async Function TestDoNotCrashAtPosition1AfterEquals() As Task
            Dim markup = <Text><![CDATA[=$$     
]]></Text>.Value
            Await VerifyNoItemsExistAsync(markup)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12733")>
        Public Async Function NotAfterDot() As Task
            Dim markup = <Text>Module Module1
    Sub Main()
            Do Until (System.Console.ReadKey.Key = System.ConsoleKey.$$
        Loop
    End Sub
End Module</Text>.Value
            Await VerifyNoItemsExistAsync(markup)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3133")>
        Public Async Function TestInCollectionInitializer1() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Sub Main()
        Dim y = New List(Of DayOfWeek) From {
            $$
        }
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "DayOfWeek.Monday")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3133")>
        Public Async Function TestInCollectionInitializer2() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Sub Main()
        Dim y = New List(Of DayOfWeek) From {
            DayOfWeek.Monday, $$
        }
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "DayOfWeek.Monday")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3133")>
        Public Async Function TestInCollectionInitializer3() As Task
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Sub Main()
        Dim y = New List(Of DayOfWeek) From {
            DayOfWeek.Monday,
            $$
        }
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "DayOfWeek.Monday")
        End Function

        <Fact>
        Public Async Function TestInEnumHasFlag() As Task
            Dim markup = <Text><![CDATA[
Imports System.IO

Class C
    Sub Main()
        Dim f As FileInfo
        f.Attributes.HasFlag($$
    End Sub
End Class
]]></Text>.Value
            Await VerifyItemExistsAsync(markup, "FileAttributes.Hidden")
        End Function
    End Class
End Namespace
