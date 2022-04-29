' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports RoslynCompletion = Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ObjectInitializerCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Private Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, displayTextPrefix As String, inlineDescription As String,
                isComplexTextEdit As Boolean?, matchingFilters As List(Of CompletionFilter), flags As CompletionItemFlags?, Optional skipSpeculation As Boolean = False) As Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> SourceCodeKind.Regular Then
                Return
            End If

            Await BaseVerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph,
                matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags, skipSpeculation)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingToShow() As Task
            Dim text = <a>Public Class C
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Class</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(530075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530075")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInArgumentList() As Task
            Dim text = <a>Public Class C
    Property A As Integer
End Class

Class Program
    Sub goo()
        Dim a = new C(1, .$$
    End Sub
End Class</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOneItem() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldAndProperty() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldAndPropertyBaseTypes() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Public Class D
    Inherits C
End Class

Class Program
    Sub goo()
        Dim a as D = new D With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersFromObjectInitializerSyntax() As Task
            Dim text = <a>Public Class C
End Class

Public Class D
    Inherits C

    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new D With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectInitializerOfGenericTypeСonstraint1() As Task
            Dim text = <a>Class C
    Public Function testSub(Of T As {IExample, New})()
        Return New T With { .$$
    End Function
End Class

Interface IExample
    Property A As String
    Property B As String
End Interface</a>.Value

            Await VerifyItemExistsAsync(text, "A")
            Await VerifyItemExistsAsync(text, "B")
        End Function

        <WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectInitializerOfGenericTypeСonstraint2() As Task
            Dim text = <a>Class C
    Public Function testSub(Of T As {New})()
        Return New T With { .$$
    End Function
End Class
</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(24612, "https://github.com/dotnet/roslyn/issues/24612")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectInitializerOfGenericTypeСonstraint3() As Task
            Dim text = <a>Class C
    Public Function testSub(Of T As {Structure})()
        Return New T With {.$$
    End Function
End Class
</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOneItemAfterComma() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .goo = 3, .b$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemIsAbsentAsync(text, "goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingLeftToShow() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .goo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithoutAsClause() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithoutAsClauseNothingLeftToShow() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property goo as Integer
End Class

Class Program
    Sub goo()
        Dim a = new C With { .goo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(544326, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544326")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInactiveInRValue() As Task
            Dim text = <a>Class C
    Public X As Long = 1
    Public Y As Long = 2
End Class
Module Program
    Sub Main(args As String())
        Dim a As C = New C() With {.X = .$$}
    End Sub
End Module</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBackingFields() As Task
            Dim text = <a>Class C
    Public Property Goo As Integer

    Sub M()
        Dim c As New C With { .$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "Goo")
            Await VerifyItemIsAbsentAsync(text, "_Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReadOnlyPropertiesAreNotPresentOnLeftSide() As Task
            Dim text = <a>Class C
    Public Property Goo As Integer
    Public ReadOnly Property Bar As Integer
        Get
            Return 0
        End Get
    End Property

    Sub M()
        Dim c As New C With { .$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "Goo")
            Await VerifyItemIsAbsentAsync(text, "Bar")
        End Function

        <WorkItem(545881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545881")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoReadonlyFieldsOrProperties() As Task
            Dim text = <a>Module M
    Sub Main()
        Dim x = New Exception With { .$$
    End Sub
End Module
</a>.Value
            Await VerifyItemIsAbsentAsync(text, "Data")
        End Function

        <WorkItem(545844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545844")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoParameterizedProperties() As Task
            Dim text = <a>Module M
    Module M
    Sub Main()
        Dim y = New List(Of Integer()) With {.Capacity = 10, .$$
    End Sub
End Module
</a>.Value
            Await VerifyItemIsAbsentAsync(text, "Item")
        End Function

        <WorkItem(545844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545844")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestShowParameterizedPropertiesWithAllOptionalArguments() As Task
            Dim text = <a>Imports System
Public Class AImpl
    Property P(Optional x As Integer = 3, Optional y As Integer = 2) As Object
        Get
            Console.WriteLine("P[{0}, {1}].get", x, y)
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine("P[{0}, {1}].set", x, y)
        End Set
    End Property

    Sub Goo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "P")
        End Function

        <WorkItem(545844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545844")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotShowParameterizedPropertiesWithSomeMandatoryArguments() As Task
            Dim text = <a>Imports System
Public Class AImpl
    Property P(x As Integer, Optional y As Integer = 2) As Object
        Get
            Console.WriteLine("P[{0}, {1}].get", x, y)
            Return Nothing
        End Get
        Set(value As Object)
            Console.WriteLine("P[{0}, {1}].set", x, y)
        End Set
    End Property

    Sub Goo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            Await VerifyItemIsAbsentAsync(text, "P")
        End Function

        <WorkItem(545844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545844")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterizedPropertiesWithParamArrays() As Task
            Dim text = <a>Option Strict On
Class C
    Property P(ParamArray args As Object()) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Property Q(o As Object, ParamArray args As Object()) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
    Shared Sub M()
        Dim o As C
        o = New C With {.$$
    End Sub
End Class
</a>.Value
            Await VerifyItemExistsAsync(text, "P")
            Await VerifyItemIsAbsentAsync(text, "Q")
        End Function

        <WorkItem(530491, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530491")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectInitializerOnInterface() As Task
            Dim text = <a><![CDATA[Option Strict On
Imports System.Runtime.InteropServices

Module Program
    Sub Main(args As String())
        Dim x = New I With {.$$}
    End Sub
End Module

<ComImport>
<Guid("EAA4976A-45C3-4BC5-BC0B-E474F4C3C83F")>
<CoClass(GetType(C))>
Interface I
    Property c As Integer
End Interface

Class C
    Public Property c As Integer
End Class
]]></a>.Value
            Await VerifyItemExistsAsync(text, "c")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function IsCommitCharacterTest() As Task
            Const code = "
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Program"

            Await VerifyCommonCommitCharactersAsync(code, textTypedSoFar:="")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIsExclusive() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="VBDocument">
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Program</Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.Create(text, exportProvider:=ExportProvider)
                Dim hostDocument = workspace.Documents.First()
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim service = GetCompletionService(document.Project)
                Dim completionList = Await GetCompletionListAsync(service, document, caretPosition, RoslynCompletion.CompletionTrigger.Invoke)
                Assert.True(completionList.IsEmpty OrElse completionList.GetTestAccessor().IsExclusive, "Expected always exclusive")
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SendEnterThroughToEditorTest() As Task
            Const code = "
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub goo()
        Dim a as C = new C With { .$$
    End Sub
End Program"

            Await VerifySendEnterThroughToEditorAsync(code, "bar", expected:=False)
        End Function

        <WorkItem(26560, "https://github.com/dotnet/roslyn/issues/26560")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordsEscaped() As Task
            Dim text = <a>Class C
    Public Property [Wend] As Integer

    Public Property [New] As Integer

    Public Property A As Integer
End Class


Class Program
    Sub Main()
        Dim c As New C With { .$$ }
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "[Wend]")
            Await VerifyItemExistsAsync(text, "[New]")
            Await VerifyItemExistsAsync(text, "A")

            Await VerifyItemIsAbsentAsync(text, "Wend")
            Await VerifyItemIsAbsentAsync(text, "New")
        End Function

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(ObjectInitializerCompletionProvider)
        End Function
    End Class
End Namespace
