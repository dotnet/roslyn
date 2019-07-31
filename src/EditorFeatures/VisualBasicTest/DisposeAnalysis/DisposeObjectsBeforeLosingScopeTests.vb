' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.DisposeAnalysis

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DisposeAnalysis
    Public Class DisposeObjectsBeforeLosingScopeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New DisposeObjectsBeforeLosingScopeDiagnosticAnalyzer(), Nothing)
        End Function

        ' Ensure that we explicitly test missing diagnostic, which has no corresponding code fix (non-fixable diagnostic).
        Private Overloads Function TestDiagnosticMissingAsync(initialMarkup As String) As Task
            Return TestDiagnosticMissingAsync(initialMarkup, New TestParameters(retainNonFixableDiagnostics:=True))
        End Function

        Private Overloads Function TestDiagnosticsAsync(initialMarkup As String, ParamArray expected As DiagnosticDescription()) As Task
            Return TestDiagnosticsAsync(initialMarkup, New TestParameters(retainNonFixableDiagnostics:=True), expected)
        End Function

        Private Shared Function Diagnostic(id As String, Optional sqiggledText As String = Nothing) As DiagnosticDescription
            Return TestHelpers.Diagnostic(id, sqiggledText)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableInitializer_DisposeCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As New A()|]
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableInitializer_NoDisposeCall_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As [|New A()|]
    End Sub
End Class",
    Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_DisposeCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As A
        a = New A()
        a.Dispose()

        Dim b As New A()
        a = b
        a.Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_NoDisposeCall_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        a = [|New A()|]
    End Sub
End Class",
    Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ParameterWithDisposableAssignment_DisposeCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        [|a = New A()
        a.Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ParameterWithDisposableAssignment_NoDisposeCall_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        a = [|New A()|]
    End Sub
End Class",
    Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function RefParametersWithDisposableAssignment_NoDisposeCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(ByRef a As A)
        [|a = New A()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithMultipleDisposableAssignment_DisposeCallOnSome_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        Dim a As A
        [|a = New A(1)
        a = New A(2)
        a.Dispose()
        a = New A(3)|]
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(1)").WithLocation(15, 13),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(3)").WithLocation(18, 13))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function FieldWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public a As A
    Sub M1(p As Test)
        [|p.a = New A()

        Dim l As New Test()
        l.a = New A()

        Me.a = New A()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function PropertyWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(p As Test)
        [|p.a = New A()

        Dim l As New Test()
        l.a = New A()

        Me.a = New A()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_DisposedInHelper_MethodInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(t2 As Test2)
        [|DisposeHelper(new A())
        t2.DisposeHelper_MethodOnDifferentType(new A())
        DisposeHelper_MultiLevelDown(new A())|]
    End Sub

    Sub DisposeHelper(a As A)
        a.Dispose()
    End Sub

    Sub DisposeHelper_MultiLevelDown(a As A)
        DisposeHelper(a)
    End Sub
End Class

Class Test2
    Sub DisposeHelper_MethodOnDifferentType(a As A)
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_DisposeOwnershipTransfer_MethodInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public a As A
    Sub M1()
        [|DisposeOwnershipTransfer(new A())
        Dim t2 = New Test2()
        t2.DisposeOwnershipTransfer_MethodOnDifferentType(new A())
        DisposeOwnershipTransfer_MultiLevelDown(new A())|]
    End Sub

    Sub DisposeOwnershipTransfer(a As A)
        Me.a = a
    End Sub

    Sub DisposeOwnershipTransfer_MultiLevelDown(a As A)
        DisposeOwnershipTransfer(a)
    End Sub
End Class

Class Test2
    Public a As A
    Sub DisposeOwnershipTransfer_MethodOnDifferentType(a As A)
        Me.a = a
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_NoDisposeOwnershipTransfer_MethodInvocation_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public a As A
    Sub M1(t2 As Test2)
        [|NoDisposeOwnershipTransfer(new A(1))
        t2.NoDisposeOwnershipTransfer_MethodOnDifferentType(new A(2))
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(3))|]
    End Sub

    Sub NoDisposeOwnershipTransfer(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub

    Sub NoDisposeOwnershipTransfer_MultiLevelDown(a As A)
        NoDisposeOwnershipTransfer(a)
    End Sub
End Class

Class Test2
    Public a As A
    Public Sub NoDisposeOwnershipTransfer_MethodOnDifferentType(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(15, 36),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(16, 61),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(3)").WithLocation(17, 51))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_DisposedInHelper_ConstructorInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim unused = new DisposeHelperType(new A())
        DisposeHelper_MultiLevelDown(new A())|]
    End Sub

    Sub DisposeHelper(a As A)
        Dim unused = new DisposeHelperType(a)
    End Sub

    Sub DisposeHelper_MultiLevelDown(a As A)
        DisposeHelper(a)
    End Sub
End Class

Class DisposeHelperType
    Public Sub New(a As A)
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_DisposeOwnershipTransfer_ConstructorInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim unused = new DisposableOwnerType(new A())
        DisposeOwnershipTransfer_MultiLevelDown(new A())|]
    End Sub

    Sub DisposeOwnershipTransfer(a As A)
        Dim unused = new DisposableOwnerType(a)
    End Sub

    Sub DisposeOwnershipTransfer_MultiLevelDown(a As A)
        DisposeOwnershipTransfer(a)
    End Sub
End Class

Class DisposableOwnerType
    Public a As A
    Public Sub New(a As A)
        Me.a = a
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function Interprocedural_NoDisposeOwnershipTransfer_ConstructorInvocation_Diagnostic() As Task
            Await TestDiagnosticsAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim unused = new NotDisposableOwnerType(new A(1))
        NoDisposeOwnershipTransfer_MultiLevelDown(new A(2))|]
    End Sub

    Sub NoDisposeOwnershipTransfer(a As A)
        Dim unused = new NotDisposableOwnerType(a)
    End Sub

    Sub NoDisposeOwnershipTransfer_MultiLevelDown(a As A)
        NoDisposeOwnershipTransfer(a)
    End Sub
End Class

Class NotDisposableOwnerType
    Public a As A
    Public Sub New(a As A)
        Dim str = a.ToString()
        Dim b = a
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(1)").WithLocation(14, 49),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "new A(2)").WithLocation(15, 51))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposeOwnershipTransfer_AtConstructorInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""A1"" CommonReferences=""true"">
        <ProjectReference>A2</ProjectReference>
        <Document>
Imports System

Class Test
    Private Function M1() As DisposableOwnerType
        Return [|New DisposableOwnerType(New A())|]
    End Function
End Class
        </Document>
    </Project>
    <Project Language =""Visual Basic"" AssemblyName=""A2"" CommonReferences=""true"">
        <Document>
Imports System

Public Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class DisposableOwnerType
    Public Sub New(ByVal a As A)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableCreationInLoop_DisposedInFinally_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class Test
    Public Sub M()
        [|Dim disposeMe As IDisposable = Nothing
        Try
            For Each c In ""Foo""
                If disposeMe Is Nothing Then
                    disposeMe = New A()
                End If
            Next
        Finally
            If disposeMe IsNot Nothing Then
                disposeMe.Dispose()
            End If
        End Try|]
    End Sub
End Class

Public Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_DisposeBoolCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub Dispose(b As Boolean)
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As A
        a = New A()
        a.Dispose(true)

        Dim b As New A()
        a = b
        a.Dispose(true)|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_CloseCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub

    Public Sub Close()
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As A
        a = New A()
        a.Close()

        Dim b As New A()
        a = b
        a.Close()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_DisposeAsyncCall_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System
Imports System.Threading.Tasks

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeAsync()
    End Sub

    Public Function DisposeAsync() As Task
        Return Task.CompletedTask
    End Function
End Class

Class Test
    Async Function M1() As Task
        [|Dim a As A
        a = New A()
        Await a.DisposeAsync()

        Dim b As New A()
        a = b
        Await a.DisposeAsync()|]
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayElementWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A())
        [|a(0) = New A()|]     ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayElementWithDisposableAssignment_ConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A())
        [|a(0) = New A()
        a(0).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A(), i As Integer)
        [|a(i) = New A()
        a(i).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayElementWithDisposableAssignment_NonConstantIndex_02_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
$"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public Property a As A
    Sub M1(a As A(), i As Integer, j As Integer)
        [|a(i) = New A()
        i = j                 ' Value of i is now unknown
        a(i).Dispose()|]      ' We don't know the points to value of a(i), so don't flag 'New A()'
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayInitializer_ElementWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As A() = New A() { New A() }|]    ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As A() = New A() {New A()}
        a(0).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ArrayInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(i As Integer)
        [|Dim a As A() = New A() {New A()}
        a(i).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function CollectionInitializer_ElementWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As List(Of A) = New List(Of A) From { New A() }|]    ' TODO: https://github.com/dotnet/roslyn-analyzers/issues/1577
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function CollectionInitializer_ElementWithDisposableAssignment_ConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
        Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1()
        [|Dim a As List(Of A) = New List(Of A) From {New A()}
        a(0).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function CollectionInitializer_ElementWithDisposableAssignment_NonConstantIndex_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Sub M1(i As Integer)
        [|Dim a As List(Of A) = New List(Of A) From {New A()}
        a(i).Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function CollectionAdd_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class NonGenericList
    Implements ICollection

    Public Sub Add(item As A)
    End Sub

    Public ReadOnly Property Count As Integer Implements ICollection.Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property SyncRoot As Object Implements ICollection.SyncRoot
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property IsSynchronized As Boolean Implements ICollection.IsSynchronized
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Sub CopyTo(array As Array, index As Integer) Implements ICollection.CopyTo
        Throw New NotImplementedException()
    End Sub

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test
    Private Sub M1()
        [|Dim a As New List(Of A)()
        a.Add(New A(1))

        Dim b As A = New A(2)
        a.Add(b)

        Dim l As New NonGenericList()
        l.Add(New A(3))

        b = New A(4)
        l.Add(b)|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function CollectionAdd_IReadOnlyCollection_SpecialCases_ElementWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections
Imports System.Collections.Concurrent
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class MyReadOnlyCollection
    Implements IReadOnlyCollection(Of A)

    Public Sub Add(ByVal item As A)
    End Sub

    Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of A).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator(Of A) Implements IEnumerable(Of A).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class

Class Test
    Private Sub M1()
        [|Dim myReadOnlyCollection = New MyReadOnlyCollection()
        myReadOnlyCollection.Add(New A(1))
        Dim a As A = New A(2)
        myReadOnlyCollection.Add(a)

        Dim bag = New ConcurrentBag(Of A)()
        bag.Add(New A(3))
        Dim a2 As A = New A(4)
        bag.Add(a2)|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function MemberInitializerWithDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public X As Integer
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    Public a As A
    Sub M1()
        [|Dim a = New Test With {.a = New A() With { .X = 1 }} |]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function StructImplementingIDisposable_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Structure A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Structure

Class Test
    Sub M1()
        [|Dim a As New A()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function NonUserDefinedConversions_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Sub M1()
        [|Dim obj As Object = New A()           ' Implicit conversion from A to object
        DirectCast(obj, A).Dispose()            ' Explicit conversion from object to A

        Dim a As A = new B()                    ' Implicit conversion from B to A
        a.Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function NonUserDefinedConversions_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    Sub M1()
        [|Dim obj As Object = New A()             ' Implicit conversion from A to object        
        Dim a As A = DirectCast(New B(), A)|]     ' Explicit conversion from B to A
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(15, 29),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New B()").WithLocation(16, 33))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function UserDefinedConversions_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Shared Widening Operator CType(ByVal value As A) As B
        value.Dispose()
        Return Nothing
    End Operator

    Public Shared Widening Operator CType(ByVal value As B) As A
        value.Dispose()
        Return Nothing
    End Operator
End Class

Class B
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Private Sub M1()
        [|Dim a As A = New B()            ' Implicit user defined conversion
        Dim b As B = CType(New A(), B)|]  ' Explicit user defined conversion
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_ByRef_DisposedInCallee_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Sub M1()
        Dim a As New A()
        M2(a)
    End Sub

    Sub M2(ByRef a as A)
        a.Dispose()
        a = Nothing
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDisposableAssignment_ByRefEscape_AbstractVirtualMethod_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Public Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public MustInherit Class Test
    Sub M1()
        [|Dim a As New A()
        M2(a)

        a = New A()
        M3(a)|]
    End Sub

    Public Overridable Sub M2(ByRef a as A)
    End Sub

    Public MustOverride Sub M3(ByRef a as A)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LocalWithDefaultOfDisposableAssignment_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    Sub M1()
        [|Dim a As A = Nothing|]
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function NullCoalesce_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        [|Dim b As A = If(a, New A())
        b.Dispose()

        Dim c As New A()
        Dim d As A = If(c, a)
        d.Dispose()

        a = New A()
        Dim e As A = If(a, New A())
        e.Dispose()|]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function NullCoalesce_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Sub M1(a As A)
        Dim b As A = If(a, [|New A()|])
        a.Dispose()
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(11, 28))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function WhileLoop_DisposeOnBackEdge_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    Sub M1(flag As Boolean)
        [|Dim a As New A()
        While True
            a.Dispose()
            If flag Then
                Exit While    ' All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            End If
            a = New A()
        End While|]
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        <WorkItem(1648, "https://github.com/dotnet/roslyn-analyzers/issues/1648")>
        Public Async Function WhileLoop_MissingDisposeOnExit_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)   ' Allocated outside the loop and disposed inside a loop is not a recommended pattern and is flagged.
        While True
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        End While
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(1)").WithLocation(14, 18),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(2)").WithLocation(17, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function WhileLoop_MissingDisposeOnEntry_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)    ' This instance will never be disposed.
        While True
            a = New A(2)
            a.Dispose()
        End While
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(1)").WithLocation(14, 18))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DoWhileLoop_DisposeOnBackEdge_NoDiagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1(flag As Boolean)
        Dim a As New A()
        Do While True
            a.Dispose()
            If flag Then
                Exit Do    ' All 'A' instances have been disposed on this path, so no diagnostic should be reported.
            End If
            a = New A()
        Loop
    End Sub|]
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DoWhileLoop_MissingDisposeOnExit_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)
        Do
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        Loop While True
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(2)").WithLocation(17, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DoWhileLoop_MissingDisposeOnEntry_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)    ' This instance will never be disposed.
        Do While True
            a = New A(2)
            a.Dispose()
        Loop
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(1)").WithLocation(14, 18))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ForLoop_DisposeOnBackEdge_MayBeNotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1(flag As Boolean)
        Dim a As New A(1)   ' Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        For i As Integer = 0 To 10
            a.Dispose()
            If flag Then
                Exit For    ' All 'A' instances have been disposed on this path.
            End If
            a = New A(2)    ' This can leak on loop exit, and is flagged as a maybe disposed violation.
        Next
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(1)").WithLocation(14, 18),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(2)").WithLocation(20, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ForLoop_MissingDisposeOnExit_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)    ' Allocation outside a loop, dispose inside a loop is not a recommended pattern and should fire diagnostic.
        For i As Integer = 0 To 10
            a.Dispose()
            a = New A(2)   ' This instance will not be disposed on loop exit.
        Next
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(1)").WithLocation(14, 18),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(2)").WithLocation(17, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ForLoop_MissingDisposeOnEntry_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A(1)    ' This instance will never be disposed.
        For i As Integer = 0 To 10
            a = New A(2)
            a.Dispose()
        Next
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(1)").WithLocation(14, 18))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function IfStatement_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    [|Private Sub M1(a As A, param As String)
        Dim a1 As New A()
        Dim a2 As B = new B()
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = new B()
        Else
            a = a2
            b = new A()
        End If

        a.Dispose()          ' a points to either a1 or a2.
        b.Dispose()          ' b points to either instance created in if or else.
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function IfStatement_02_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class Test
    [|Private Sub M1(a As A, param As String, param2 As String)
        Dim a1 As New A()
        Dim a2 As B = new B()
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = new B()
            If param = """" Then
                a = new B()
            Else
                If param2 IsNot Nothing Then
                    b = new A()
                Else
                    b = new B()
                End If
            End If
        Else
            a = a2
            b = new A()
        End If

        a.Dispose()          ' a points to either a1 or a2 or instance created in 'if(param == """")'.
        b.Dispose()          ' b points to either instance created in outer if or outer else or innermost if or innermost else.
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function IfStatement_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class C
    Inherits B
End Class

Class D
    Inherits C
End Class

Class E
    Inherits D
End Class

Class Test
    [|Private Sub M1(ByVal a As A, ByVal param As String, ByVal param2 As String)
        Dim a1 As A = New A(1)   ' Maybe disposed.
        Dim a2 As B = New B()   ' Never disposed.
        Dim b As A

        If param IsNot Nothing Then
            a = a1
            b = New C()     ' Never disposed.
        Else
            a = a2
            b = New D()     ' Never disposed.
        End If

        ' a points to either a1 or a2.
        ' b points to either instance created in if or else.

        If param IsNot Nothing Then
            Dim c As A = New A(2)
            a = c
            b = a1
        Else
            Dim d As C = New E()
            b = d
            a = b
        End If

        a.Dispose()         ' a points to either c or d.
        b.Dispose()         ' b points to either a1 or d.
    End Sub|]
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New B()").WithLocation(34, 23),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New C()").WithLocation(39, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New D()").WithLocation(42, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function IfStatement_02_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Class C
    Inherits B
End Class

Class D
    Inherits C
End Class

Class E
    Inherits D
End Class

Class Test
    [|Private Sub M1(ByVal a As A, ByVal param As String, ByVal param2 As String)
        Dim a1 As A = New B()       ' Never disposed
        Dim a2 As B = New C()       ' Never disposed
        Dim b As A
        If param IsNot Nothing Then
            a = a1
            b = New A(1)       ' Always disposed
            If param = """" Then
                a = New D()       ' Never disposed
            Else
                If param2 IsNot Nothing Then
                    b = New A(2)       ' Maybe disposed
                Else
                    b = New A(3)       ' Maybe disposed
                    If param = """" Then
                        b = New A(4)   ' Maybe disposed
                    End If
                End If

                If param2 = """" Then
                    b.Dispose()     ' b points to one of the three instances of A created above.
                    b = New A(5)     ' Always disposed
                End If
            End If
        Else
            a = a2
            b = New A(6)       ' Maybe disposed
            If param2 IsNot Nothing Then
                a = New A(7)       ' Always disposed
            Else
                a = New A(8)       ' Always disposed
                b = New A(9)       ' Always disposed
            End If

            a.Dispose()
        End If

        b.Dispose()
    End Sub|]
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New B()").WithLocation(33, 23),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New C()").WithLocation(34, 23),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New D()").WithLocation(40, 21))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function UsingStatement_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Private Sub M1()
        Using a As New A()
        End Using

        Dim b As A = New A()
        Using b
        End Using

        Using c As New A(), d = New A()
        End Using

        Using a As A = Nothing
        End Using
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function UsingStatementInTryCatch_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System.IO

Class Test
    Private Sub M1()
        Try
            [|Using ms = New MemoryStream()|]
            End Using
        Catch
        End Try
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function NestedTryFinallyInTryCatch_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System.IO

Class Test
    Private Sub M1()
        Try
            [|Dim ms = New MemoryStream()|]
            Try
            Finally
                ms?.Dispose()
            End Try
        Catch
        End Try
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ReturnStatement_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections.Generic

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Private Function M1() As A
        Return New A()
    End Function

    Private Function M2(a As A) As A
        a = New A()
        Return a
    End Function

    Private Function M3(a As A) As A
        a = New A()
        Dim b = a
        Return b
    End Function

    Public Iterator Function M4() As IEnumerable(Of A)
        Yield New A
    End Function|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ReturnStatement_02_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Collections.Generic

Class A
    Implements I, IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Interface I
End Interface

Class Test
    [|Private Function M1() As I
        Return New A()
    End Function

    Private Function M2() As I
        Return TryCast(New A(), I)
    End Function|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LambdaInvocation_EmptyBody_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A
        a = New A()

        Dim myLambda As System.Action = Sub()
                                        End Sub

        myLambda()      ' This should not change state of 'a'.
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(12, 13))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LambdaInvocation_DisposesCapturedValue_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As New A()

        Dim myLambda As System.Action = Sub()
                                            a.Dispose()
                                        End Sub

        myLambda()      '  This should change state of 'a' to be Disposed.
    End Sub|]
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LambdaInvocation_CapturedValueAssignedNewDisposable_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A

        Dim myLambda As System.Action = Sub()
                                            a = New A()
                                        End Sub

        myLambda()      ' This should change state of 'a' to be NotDisposed and fire a diagnostic.
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(14, 49))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function LambdaInvocation_ChangesCapturedValueContextSensitive_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A

        Dim myLambda As System.Action(Of A) = Sub(b As A)
                                                a = b
                                                End Sub

        myLambda(New A())      ' This should change state of 'a' to be NotDisposed and fire a diagnostic.
    End Sub|]
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(18, 18))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DelegateInvocation_EmptyBody_NoArguments_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action = AddressOf M2
        myDelegate()      ' This should not change state of 'a' as it is not passed as argument.
    End Sub|]

    Sub M2()
    End Sub
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(12, 13))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DelegateInvocation_PassedAsArgumentButNotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action(Of A) = AddressOf M2
        myDelegate(a)      ' This should not change state of 'a'.
    End Sub|]

    Sub M2(a As A)
    End Sub
End Module",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A()").WithLocation(12, 13))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DelegateInvocation_DisposesCapturedValue_NoDiagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Module Test
    [|Sub M1()
        Dim a As A
        a = New A()

        Dim myDelegate As System.Action(Of A) = AddressOf M2
        myDelegate(a)      ' This should change state of 'a' to be disposed as we perform interprocedural analysis.
    End Sub|]

    Sub M2(a As A)
        a.Dispose()
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableCreationNotAssignedToAVariable_BailOut_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public X As Integer

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub M()
    End Sub
End Class

Class Test
    [|Private Sub M1()
        New A(1)    ' Error
        New A(2).M()    ' Error
        Dim x = New A(3).X
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableCreationPassedToDisposableConstructor_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable

    Public X As Integer
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private ReadOnly _a As A
    Public Sub New(ByVal a As A)
        _a = a
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Private Sub M1()
        Dim b = New B(New A())
        b.Dispose()
        Dim a = New A()
        Dim b2 As B = Nothing
        Try
            b2 = New B(a)
        Finally
            If b2 IsNot Nothing Then
                b2.Dispose()
            End If
        End Try

        Dim a2 = New A()
        Dim b3 As B = Nothing
        Try
            b3 = New B(a2)
        Finally
            If b3 IsNot Nothing Then
                b3.Dispose()
            End If
        End Try
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableObjectOnlyDisposedOnExceptionPath_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Private Sub M1()
        Dim a = New A(1)
        Try
            ThrowException()
        Catch ex As Exception
            a.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a = New A(2)
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            a.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a = New A(3)
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            a.Dispose()
        Catch ex As Exception
            a.Dispose()
        End Try
    End Sub

    Private Sub M4(flag As Boolean)
        Dim a = New A(4)
        Try
            ThrowException()
        Catch ex As System.IO.IOException
            If flag Then
                a.Dispose()
            End If
        End Try
    End Sub|]

    Private Sub ThrowException()
        Throw New NotImplementedException()
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(1)").WithLocation(14, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(2)").WithLocation(23, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(3)").WithLocation(32, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(4)").WithLocation(43, 17))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableObjectDisposed_FinallyPath_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|Private Sub M1()
        Dim a = New A()
        Try
            ThrowException()
        Finally
            a.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a = New A()
        Try
            ThrowException()
        Catch ex As Exception
        Finally
            a.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a = New A()
        Try
            ThrowException()
            a.Dispose()
            a = Nothing
        Catch ex As System.IO.IOException
        Finally
            If a IsNot Nothing Then
                a.Dispose()
            End If
        End Try
    End Sub|]

    Private Sub ThrowException()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DelegateCreation_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class

Class Test
    [|Sub M1()
        Dim createA As Func(Of A) = AddressOf M2
        Dim a As A = createA()
        a.Dispose()
    End Sub

    Function M2() As A
        Return New A()
    End Function|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function MultipleReturnStatements_AllInstancesReturned_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class Test
    [|Private Function M1(ByVal flag As Boolean) As A
        Dim a As A
        If flag Then
            Dim a2 As New A()
            a = a2
            Return a
        End If

        Dim a3 As New A()
        a = a3
        Return a
    End Function|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function MultipleReturnStatements_AllInstancesEscapedWithOutParameter_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class Test
    [|Private Sub M1(ByVal flag As Boolean, <Out> ByRef a As A)
        If flag Then
            Dim a2 As New A()
            a = a2
            Return
        End If

        Dim a3 As New A()
        a = a3
        Return
    End Sub|]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function MultipleReturnStatements_AllButOneInstanceReturned_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class Test
    [|Private Function M1(flag As Integer, flag2 As Boolean, flag3 As Boolean) As A
        Dim a As A = Nothing
        If flag = 0 Then
            Dim a2 As A = New A(1)   ' Escaped with return inside below nested 'if', not disposed on other paths.
            a = a2
            If Not flag2 Then
                If flag3 Then
                    Return a
                End If
            End If
        Else
            a = New A(2)     ' Escaped with return inside below nested 'else', not disposed on other paths.
            If flag = 1 Then
                a = New A(3)     ' Never disposed on any path.
            Else
                If flag3 Then
                    a = New A(4)     ' Escaped with return inside below 'else', not disposed on other paths.
                End If

                If flag2 Then
                Else
                    Return a
                End If
            End If
        End If

        Dim a3 As A = New A(5)     ' Always escaped with below return, ensure no diagnostic.
        a = a3
        Return a
    End Function|]
End Class",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(1)").WithLocation(19, 27),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(2)").WithLocation(27, 17),
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New A(3)").WithLocation(29, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(4)").WithLocation(32, 25))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function MultipleReturnStatements_AllButOneInstanceEscapedWithOutParameter_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Inherits A
End Class

Public Class Test
    [|Private Sub M1(flag As Integer, flag2 As Boolean, flag3 As Boolean, <Out> ByRef a As A)
        a = Nothing
        If flag = 0 Then
            Dim a2 As A = New A()   ' Escaped with return inside below nested 'if'.
            a = a2
            If Not flag2 Then
                If flag3 Then
                    Return
                End If
            End If
        Else
            a = New A()     ' Escaped with return inside below nested 'else'.
            If flag = 1 Then
                a = New B()     ' Never disposed
            Else
                If flag3 Then
                    a = New A()     ' Escaped with return inside below 'else'.
                End If

                If flag2 Then
                Else
                    Return
                End If
            End If
        End If

        Dim a3 As A = New A()     ' Escaped with below return.
        a = a3
        Return
    End Sub|]
End Class",
            Diagnostic(IDEDiagnosticIds.DisposeObjectsBeforeLosingScopeDiagnosticId, "New B()").WithLocation(29, 21))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DifferentDisposePatternsInFinally_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|
    Private Sub M1()
        Dim a As A = New A(1)

        Try
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M2()
        Dim a As A = Nothing

        Try
            a = New A(2)
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M3()
        Dim a As A = New A(3)

        Try
        Finally
            If a IsNot Nothing Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M4()
        Dim a As A = Nothing

        Try
            a = New A(4)
        Finally
            If a IsNot Nothing Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M5()
        Dim a As A = New A(5)

        Try
        Finally
            DisposeHelper(a)
        End Try
    End Sub

    Private Sub M6()
        Dim a As A = Nothing

        Try
            a = New A(6)
        Finally
            DisposeHelper(a)
        End Try
    End Sub

    Private Sub DisposeHelper(a As IDisposable)
        If a IsNot Nothing Then
            a?.Dispose()
        End If
    End Sub

    Private Sub M7(flag As Boolean)
        Dim a As A = New A(7)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M8(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(8)
            End If

            If flag2 Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M9(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            a.Dispose()
        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private Sub M10(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                a?.Dispose()
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
            End If

        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private A As IDisposable

    Private Sub M11(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            Me.A = a
            a = Nothing
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M12(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                Me.A = a
                a = Nothing
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
                a = Nothing
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub
    |]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DifferentDisposePatternsInFinally_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable

    Public Sub New(i As Integer)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    [|
    Private Sub M1(flag As Boolean)
        Dim a As A = New A(1)

        Try
        Finally
            If flag Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M2(flag As Boolean)
        Dim a As A = Nothing

        Try
            a = New A(2)
        Finally
            If flag Then
                a?.Dispose()
            End If
        End Try
    End Sub

    Private Sub M3(flag As Boolean)
        Dim a As A = Nothing
        Dim b As A = Nothing

        Try
            If flag Then
                a = New A(3)
                b = New A(31)
            End If
        Finally
            If b IsNot Nothing Then
                a.Dispose()
                b.Dispose()
            End If
        End Try
    End Sub

    Private Sub M4(flag As Boolean)
        Dim a As A = Nothing
        Dim b As A = Nothing

        Try
            If flag Then
                a = New A(4)
                b = New A(41)
            End If
        Finally
            If a IsNot Nothing AndAlso b IsNot Nothing Then
                a.Dispose()
                b.Dispose()
            End If
        End Try
    End Sub

    Private Sub M5(flag As Boolean)
        Dim a As A = New A(5)

        Try
        Finally
            DisposeHelper(a, flag)
        End Try
    End Sub

    Private Sub M6(flag As Boolean)
        Dim a As A = Nothing

        Try
            If flag Then
                a = New A(6)
            End If
        Finally
            DisposeHelper(a, flag)
        End Try
    End Sub

    Private Sub DisposeHelper(a As IDisposable, flag As Boolean)
        If flag Then
            a?.Dispose()
        End If
    End Sub

    Private Sub M7(flag As Boolean)
        Dim a As A = New A(7)

        Try
            If flag Then
                a = Nothing
            End If
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M8(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(8)
            End If

            If flag2 Then
                a.Dispose()
                a = Nothing
            Else
                a = Nothing
            End If
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M9(flag As Boolean)
        Dim a As A = New A(9)

        Try
            If flag Then
                a = Nothing
                Return
            End If

            a.Dispose()
        Catch ex As Exception
            a?.Dispose()
        Finally
        End Try
    End Sub

    Private Sub M10(flag1 As Boolean, flag2 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(10)
            End If

            If flag2 Then
                a?.Dispose()
                Return
            End If

            If a IsNot Nothing Then
                a.Dispose()
            End If
        Catch ex As Exception
            If flag1 Then
                a?.Dispose()
            End If

        Finally
        End Try
    End Sub

    Private A As IDisposable

    Private Sub M11(flag As Boolean)
        Dim a As A = New A(11)

        Try
            If flag Then
                a.Dispose()
                a = Nothing
                Return
            End If

            a = Nothing
            Me.A = a
        Finally
            a?.Dispose()
        End Try
    End Sub

    Private Sub M12(flag1 As Boolean, flag2 As Boolean, flag3 As Boolean)
        Dim a As A = Nothing

        Try
            If flag1 Then
                a = New A(12)
            End If

            If flag2 Then
                Me.A = a
                a = Nothing
                Return
            ElseIf flag3 Then
                a = New A(121)
            End If

        Finally
            a?.Dispose()
        End Try
    End Sub
    |]
End Class",
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(1)").WithLocation(16, 22),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(2)").WithLocation(30, 17),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(3)").WithLocation(44, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(4)").WithLocation(61, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(41)").WithLocation(62, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(5)").WithLocation(73, 22),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(6)").WithLocation(86, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(8)").WithLocation(116, 21),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(9)").WithLocation(131, 22),
            Diagnostic(IDEDiagnosticIds.UseRecommendedDisposePatternDiagnosticId, "New A(11)").WithLocation(174, 22))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function ReturnDisposableObjectWrappenInTask_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function M1_Task() As Task(Of C)
        [|Return Task.FromResult(New C())|]
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function AwaitedButNotDisposed_NoDiagnostic() As Task
            ' We are conservative when disposable object gets wrapped in a task and consider it as escaped.
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    [|
    Public Function M1_Task() As Task(Of C)
        Return Task.FromResult(New C())
    End Function

    Public Async Function M2_Task() As Task
        Dim c = Await M1_Task().ConfigureAwait(False)
    End Function
    |]
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function AwaitedButNotDisposed_TaskWrappingField_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Implements IDisposable

    Private _c As C

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    [|
    Public Function M1_Task() As Task(Of C)
        Return Task.FromResult(_c)
    End Function

    Public Async Function M2_Task() As Task
        Dim c = Await M1_Task().ConfigureAwait(False)
    End Function
    |]
End Class")
        End Function
    End Class
End Namespace
