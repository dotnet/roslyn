' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ObjectInitializerCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Async Function VerifyWorkerAsync(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, experimental As Boolean, glyph As Integer?) As Threading.Tasks.Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> SourceCodeKind.Regular Then
                Return
            End If

            Await BaseVerifyWorkerAsync(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingToShow() As Task
            Dim text = <a>Public Class C
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Class</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(530075)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInArgumentList() As Task
            Dim text = <a>Public Class C
    Property A As Integer
End Class

Class Program
    Sub foo()
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
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldAndProperty() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldAndPropertyBaseTypes() As Task
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

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMembersFromObjectInitializerSyntax() As Task
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

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOneItemAfterComma() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .foo = 3, .b$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemIsAbsentAsync(text, "foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNothingLeftToShow() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a as C = new C With { .foo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithoutAsClause() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a = new C With { .$$
    End Sub
End Program</a>.Value

            Await VerifyItemExistsAsync(text, "bar")
            Await VerifyItemExistsAsync(text, "foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestWithoutAsClauseNothingLeftToShow() As Task
            Dim text = <a>Public Class C
    Public bar as Integer
    Public Property foo as Integer
End Class

Class Program
    Sub foo()
        Dim a = new C With { .foo = 3, .bar = 3, .$$
    End Sub
End Program</a>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <WorkItem(544326)>
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
    Public Property Foo As Integer

    Sub M()
        Dim c As New C With { .$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "Foo")
            Await VerifyItemIsAbsentAsync(text, "_Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReadOnlyPropertiesAreNotPresentOnLeftSide() As Task
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

            Await VerifyItemExistsAsync(text, "Foo")
            Await VerifyItemIsAbsentAsync(text, "Bar")
        End Function

        <WorkItem(545881)>
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

        <WorkItem(545844)>
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

        <WorkItem(545844)>
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

    Sub Foo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            Await VerifyItemExistsAsync(text, "P")
        End Function

        <WorkItem(545844)>
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

    Sub Foo()
        Dim z = New AImpl With {.$$
    End Sub
End Class</a>.Value

            Await VerifyItemIsAbsentAsync(text, "P")
        End Function

        <WorkItem(545844)>
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

        <WorkItem(530491)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIsExclusive() As Task
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

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(text)
                Dim hostDocument = workspace.Documents.First()
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()

                Dim completionList = Await GetCompletionListAsync(document, caretPosition, triggerInfo)
                Assert.True(completionList Is Nothing OrElse completionList.IsExclusive, "Expected always exclusive")
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
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
