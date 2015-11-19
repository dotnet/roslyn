' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ObjectInitializerCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Sub VerifyWorker(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, experimental As Boolean, glyph As Integer?)
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> SourceCodeKind.Regular Then
                Return
            End If

            BaseVerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NothingToShow()
            Dim text = <a>Public Class C
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Class</a>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(530075)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInArgumentList()
            Dim text = <a>Public Class C
    Property A As Integer
End Class

Class Program
    Sub foo()
        Dim a = new C(1, .$$
    End Sub
End Class</a>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OneItem()
            Dim text = <a>Public Class C
    Public bar as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldAndProperty()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldAndPropertyBaseTypes()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Public Class D
    Inherits C
End Class

Class Program
    Sub foo()
        Dim a as D = new D With { .$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MembersFromObjectInitializerSyntax()
            Dim text = <a>Public Class C
End Class

Public Class D
    Inherits C

    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new D With { .$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OneItemAfterComma()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .foo = 3, .b$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
            VerifyItemIsAbsent(text, "foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NothingLeftToShow()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .foo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WithoutAsClause()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a = new C With { .$$
    End Sub
End Program</a>.Value

            VerifyItemExists(text, "bar")
            VerifyItemExists(text, "foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub WithoutAsClauseNothingLeftToShow()
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a = new C With { .foo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WorkItem(544326)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InactiveInRValue()
            Dim text = <a>Class C
    Public X As Long = 1
    Public Y As Long = 2
End Class
Module Program
    Sub Main(args As String())
        Dim a As C = New C() With {.X = .$$}
    End Sub
End Module</a>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoBackingFields()
            Dim text = <a>Class C
    Public Property Foo As Integer

    Sub M()
        Dim c As New C With { .$$
    End Sub
End Class</a>.Value

            VerifyItemExists(text, "Foo")
            VerifyItemIsAbsent(text, "_Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReadOnlyPropertiesAreNotPresentOnLeftSide()
            Dim text = <a>Class C
    Public Property Foo As Integer
    Public ReadOnly Property Bar As Integer
        Get
            Return 0
        End Get
    End Property

    Sub M()
        Dim c As New C With { .$$
    End Sub
End Class</a>.Value

            VerifyItemExists(text, "Foo")
            VerifyItemIsAbsent(text, "Bar")
        End Sub

        <WorkItem(545881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoReadonlyFieldsOrProperties()
            Dim text = <a>Module M
    Sub Main()
        Dim x = New Exception With { .$$
    End Sub
End Module
</a>.Value
            VerifyItemIsAbsent(text, "Data")
        End Sub

        <WorkItem(545844)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoParameterizedProperties()
            Dim text = <a>Module M
    Module M
    Sub Main()
        Dim y = New List(Of Integer()) With {.Capacity = 10, .$$
    End Sub
End Module
</a>.Value
            VerifyItemIsAbsent(text, "Item")
        End Sub

        <WorkItem(545844)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ShowParameterizedPropertiesWithAllOptionalArguments()
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

    Sub Foo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            VerifyItemExists(text, "P")
        End Sub

        <WorkItem(545844)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotShowParameterizedPropertiesWithSomeMandatoryArguments()
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

    Sub Foo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            VerifyItemIsAbsent(text, "P")
        End Sub

        <WorkItem(545844)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterizedPropertiesWithParamArrays()
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
            VerifyItemExists(text, "P")
            VerifyItemIsAbsent(text, "Q")
        End Sub

        <WorkItem(530491)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ObjectInitializerOnInterface()
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
            VerifyItemExists(text, "c")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function IsCommitCharacterTest() As Threading.Tasks.Task
            Const code = "
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program"

            Await VerifyCommonCommitCharactersAsync(code, textTypedSoFar:="")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub IsExclusive()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="VBDocument">
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program</Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(text)
                Dim hostDocument = workspace.Documents.First()
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()

                Dim completionList = GetCompletionList(document, caretPosition, triggerInfo)
                Assert.True(completionList Is Nothing OrElse completionList.IsExclusive, "Expected always exclusive")
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SendEnterThroughToEditorTest() As Threading.Tasks.Task
            Const code = "
Public Class C
    Public bar as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program"

            Await VerifySendEnterThroughToEditorAsync(code, "bar", expected:=False)
        End Function

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New ObjectInitializerCompletionProvider()
        End Function
    End Class
End Namespace
