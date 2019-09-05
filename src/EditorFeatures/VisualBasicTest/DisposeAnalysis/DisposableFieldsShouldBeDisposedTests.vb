' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.DisposeAnalysis

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DisposeAnalysis
    Public Class DisposableFieldsShouldBeDisposedTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New DisposableFieldsShouldBeDisposedDiagnosticAnalyzer(), Nothing)
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
        Public Async Function DisposableAllocationInConstructor_AssignedDirectly_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private ReadOnly a As A|]
    Sub New()
        a = New A()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInConstructor_AssignedDirectly_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private ReadOnly [|a|] As A
    Sub New()
        a = New A()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInMethod_AssignedDirectly_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub SomeMethod()
        a = New A()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInMethod_AssignedDirectly_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A
    Sub SomeMethod()
        a = New A()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInFieldInitializer_AssignedDirectly_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()
    Private ReadOnly a2 As New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
        a2.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInFieldInitializer_AssignedDirectly_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()
    Private ReadOnly a2 As New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a").WithLocation(12, 13),
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a2").WithLocation(13, 22))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function StaticField_NotDisposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private Shared a As A = New A()
    Private Shared ReadOnly a2 As New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughLocal_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub SomeMethod()
        Dim l = New A()
        a = l
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughLocal_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A
    Sub SomeMethod()
        Dim l = New A()
        a = l
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughParameter_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub New(p As A)
        p = New A()
        a = p
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughParameter_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A
    Sub New(p As A)
        p = New A()
        a = p
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableSymbolWithoutAllocation_AssignedThroughParameter_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub New(p As A)
        a = p
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableSymbolWithoutAllocation_AssignedThroughParameter_NotDisposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub New(p As A)
        a = p
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughInstanceInvocation_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub New()
        a = GetA()
    End Sub

    Private Function GetA() As A
        Return New A()
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughInstanceInvocation_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A
    Sub New()
        a = GetA()
    End Sub

    Private Function GetA() As A
        Return New A()
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughStaticCreateInvocation_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A|]
    Sub New()
        a = Create()
    End Sub

    Private Shared Function Create() As A
        Return New A()
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedThroughStaticCreateInvocation_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A
    Sub New()
        a = Create()
    End Sub

    Private Shared Function Create() As A
        Return New A()
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedInDifferentType_DisposedInContainingType_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Public a As A|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class

Class WrapperB
    Dim b As B
    Public Sub Create()
        b.a = new A()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedInDifferentType_DisposedInDifferentNonDisposableType_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Public a As A|]

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class WrapperB
    Dim b As B

    Public Sub Create()
        b.a = new A()
    End Sub

    Public Sub Dispose()
        b.a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedInDifferentType_NotDisposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Public a As A|]

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Test
    Public Sub M(b As B)
        b.a = new A()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedWithConditionalAccess_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a?.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedToLocal_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        Dim l = a
        l.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AssignedToLocal_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A = New A()

    Public Sub Dispose() Implements IDisposable.Dispose
        Dim l = a
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_IfElseStatement_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A
    Private b As A|]

    Public Sub New(ByVal flag As Boolean)
        Dim l As A = New A()
        If flag Then
            a = l
        Else
            b = l
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dim l As A = Nothing
        If a IsNot Nothing Then
            l = a
        ElseIf b IsNot Nothing Then
            l = b
        End If
        l.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_IfElseStatement_NotDisposed_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A
    Private b As A|]

    Public Sub New(ByVal flag As Boolean)
        Dim l As A = New A()
        If flag Then
            a = l
        Else
            b = l
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dim l As A = Nothing
        If a IsNot Nothing Then
            l = a
        ElseIf b IsNot Nothing Then
            l = b
        End If
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "a").WithLocation(12, 13),
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId, "b").WithLocation(13, 13))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_EscapedField_NotDisposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeA(a)
    End Sub

    Private Shared Sub DisposeA(ByRef a As A)
        a.Dispose()
        a = Nothing
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_OptimisticPointsToAnalysis_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub PerformSomeCleanup()
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.PerformSomeCleanup()
        ClearMyState()
        a.Dispose()
    End Sub

    Private Sub ClearMyState()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_OptimisticPointsToAnalysis_WithReturn_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub PerformSomeCleanup()
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]
    Public Disposed As Boolean

    Public Sub Dispose() Implements IDisposable.Dispose
        If Disposed Then
            Return
        End If

        a.PerformSomeCleanup()
        ClearMyState()
        a.Dispose()
    End Sub

    Private Sub ClearMyState()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_IfStatementInDispose_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class Test
    Implements IDisposable
    [|Private ReadOnly a As A = New A()|]
    Private cancelled As Boolean

    Public Sub Dispose() Implements IDisposable.Dispose
        If cancelled Then
            a.GetType()
        End If
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedinDisposeOverride_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

MustInherit Class Base
    Implements IDisposable
    Public Overridable Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class Derived
    Inherits Base

    [|Private ReadOnly a As A = New A()|]

    Public Overrides Sub Dispose()
        MyBase.Dispose()
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedWithDisposeBoolInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Dispose(disposed As Boolean)
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose(True)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInsideDisposeBool_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Dispose(disposed As Boolean)
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub

    Public Sub Dispose(disposed As Boolean)
        a.Dispose(disposed)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedWithDisposeCloseInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Close()
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Close()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_AllDisposedMethodsMixed_Disposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Dispose(disposed As Boolean)
    End Sub

    Public Sub Close()
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()
    Private a2 As A = New A()
    Private a3 As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.Close()
    End Sub

    Public Sub Dispose(disposed As Boolean)
        a2.Dispose()
    End Sub

    Public Sub Close()
        a3.Dispose(True)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInsideDisposeClose_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Sub Dispose(disposed As Boolean)
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub

    Public Sub Dispose(disposed As Boolean)
        a.Dispose(disposed)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function SystemThreadingTask_SpecialCase_NotDisposed_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks

Public Class A
    Implements IDisposable

    [|Private ReadOnly t As Task|]

    Public Sub New()
        t = New Task(Nothing)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedWithDisposeAsyncInvocation_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
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

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        a.DisposeAsync()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInsideDisposeCoreAsync_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.Threading.Tasks

MustInherit Class A
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeAsync()
    End Sub

    Public Function DisposeAsync() As Task
        Return Task.CompletedTask
    End Function

    Protected MustOverride Function DisposeCoreAsync(initialized As Boolean) As Task
End Class

Class A2
    Inherits A

    Protected Overrides Function DisposeCoreAsync(initialized As Boolean) As Task
        Return Task.CompletedTask
    End Function
End Class

Class B
    Inherits A

    [|Private a As New A2()|]

    Protected Overrides Function DisposeCoreAsync(initialized As Boolean) As Task
        Return a.DisposeAsync()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInInvokedMethod_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private a As A = New A()|]

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Public Sub DisposeHelper()
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_NotDisposedInInvokedMethod_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    Private [|a|] As A = New A()

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Public Sub DisposeHelper()
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInInvokedMethod_DisposableTypeInMetadata_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.IO

Class B
    Implements IDisposable

    [|Private a As FileStream = File.Open("""", FileMode.Create)|]

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Private Sub DisposeHelper()
        a.Dispose()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_NotDisposedInInvokedMethod_DisposableTypeInMetadata_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System
Imports System.IO

Class B
    Implements IDisposable

    Private [|a|] As FileStream = File.Open("""", FileMode.Create)

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Private Sub DisposeHelper()
    End Sub
End Class",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_DisposedInInvokedMethodMultipleLevelsDown_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System
Imports System.IO

Class B
    Implements IDisposable

    [|Private a As FileStream = File.Open("""", FileMode.Create)|]

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Private Sub DisposeHelper()
        Helper.PerformDispose(a)
    End Sub
End Class

Public Module Helper
    Public Sub PerformDispose(ByVal a As IDisposable)
        a.Dispose()
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocation_NotDisposedInInvokedMethodMultipleLevelsDown_Diagnostic() As Task
            Await TestDiagnosticsAsync(
"Imports System
Imports System.IO

Class B
    Implements IDisposable

    Private [|a|] As FileStream = File.Open("""", FileMode.Create)

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeHelper()
    End Sub

    Private Sub DisposeHelper()
        Helper.PerformDispose(a)
    End Sub
End Class

Public Module Helper
    Public Sub PerformDispose(ByVal a As IDisposable)
    End Sub
End Module",
            Diagnostic(IDEDiagnosticIds.DisposableFieldsShouldBeDisposedDiagnosticId))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DisposeAnalysis)>
        Public Async Function DisposableAllocationInConstructor_DisposedInGeneratedCodeFile_NoDiagnostic() As Task
            Await TestDiagnosticMissingAsync(
"Imports System

Class A
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class B
    Implements IDisposable

    [|Private ReadOnly a As A|]
    Sub New()
        a = New A()
    End Sub

    <System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")> _
    Public Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class")
        End Function
    End Class
End Namespace
