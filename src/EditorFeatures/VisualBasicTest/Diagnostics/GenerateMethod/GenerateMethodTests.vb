' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateMethod
    Public Class GenerateMethodTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateParameterizedMemberCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationIntoSameType() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo()
    End Sub
    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        <WorkItem(11518, "https://github.com/dotnet/roslyn/issues/11518")>
        Public Async Function TestNameMatchesNamespaceName() As Task
            Await TestAsync(
"Namespace N
    Module Module1
        Sub Main()
            [|N|]()
        End Sub
    End Module
End Namespace",
"
Imports System

Namespace N
    Module Module1
        Sub Main()
            N()
        End Sub

        Private Sub N()
            Throw New NotImplementedException()
        End Sub
    End Module
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationOffOfMe() As Task
            Await TestAsync(
"Class C
    Sub M()
        Me.[|Foo|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Me.Foo()
    End Sub
    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationOffOfType() As Task
            Await TestAsync(
"Class C
    Sub M()
        C.[|Foo|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        C.Foo()
    End Sub
    Private Shared Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationValueExpressionArg() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|](0)
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo(0)
    End Sub
    Private Sub Foo(v As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationMultipleValueExpressionArg() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|](0, 0)
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo(0, 0)
    End Sub
    Private Sub Foo(v1 As Integer, v2 As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationValueArg() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](i)
    End Sub
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(i)
    End Sub
    Private Sub Foo(i As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationNamedValueArg() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](bar:=i)
    End Sub
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(bar:=i)
    End Sub
    Private Sub Foo(bar As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateAfterMethod() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|]()
    End Sub
    Sub NextMethod()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo()
    End Sub
    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
    Sub NextMethod()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInterfaceNaming() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](NextMethod())
    End Sub
    Function NextMethod() As IFoo
    End Function
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(NextMethod())
    End Sub
    Private Sub Foo(foo As IFoo)
        Throw New NotImplementedException()
    End Sub
    Function NextMethod() As IFoo
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestFuncArg0() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](NextMethod)
    End Sub
    Function NextMethod() As String
    End Function
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(NextMethod)
    End Sub
    Private Sub Foo(nextMethod As String)
        Throw New NotImplementedException()
    End Sub
    Function NextMethod() As String
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestFuncArg1() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](NextMethod)
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(NextMethod)
    End Sub
    Private Sub Foo(nextMethod As Func(Of Integer, String))
        Throw New NotImplementedException()
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class")
        End Function

        <WpfFact(Skip:="528229"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestAddressOf1() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](AddressOf NextMethod)
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(AddressOf NextMethod)
    End Sub
    Private Sub Foo(nextMethod As Global.System.Func(Of Integer, String))
        Throw New NotImplementedException()
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestActionArg() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](NextMethod) End Sub 
 Sub NextMethod()
    End Sub
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(NextMethod)
    End Sub
    Private Sub Foo(nextMethod As Object)
        Throw New NotImplementedException()
    End Sub
    Sub NextMethod()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestActionArg1() As Task
            Await TestAsync(
"Class C
    Sub M(i As Integer)
        [|Foo|](NextMethod)
    End Sub
    Sub NextMethod(i As Integer)
    End Sub
End Class",
"Imports System
Class C
    Sub M(i As Integer)
        Foo(NextMethod)
    End Sub
    Private Sub Foo(nextMethod As Action(Of Integer))
        Throw New NotImplementedException()
    End Sub
    Sub NextMethod(i As Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeInference() As Task
            Await TestAsync(
"Class C
    Sub M()
        If [|Foo|]()
        End If
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        If Foo()
        End If
    End Sub
    Private Function Foo() As Boolean
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMemberAccessArgumentName() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|](Me.Bar)
    End Sub
    Dim Bar As Integer
End Class",
"Imports System
Class C
    Sub M()
        Foo(Me.Bar)
    End Sub
    Private Sub Foo(bar As Integer)
        Throw New NotImplementedException()
    End Sub
    Dim Bar As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParenthesizedArgumentName() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|]((Bar))
    End Sub
    Dim Bar As Integer
End Class",
"Imports System
Class C
    Sub M()
        Foo((Bar))
    End Sub
    Private Sub Foo(bar As Integer)
        Throw New NotImplementedException()
    End Sub
    Dim Bar As Integer
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCastedArgumentName() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|](DirectCast(Me.Baz, Bar))
    End Sub
End Class
Class Bar
End Class",
"Imports System
Class C
    Sub M()
        Foo(DirectCast(Me.Baz, Bar))
    End Sub
    Private Sub Foo(baz As Bar)
        Throw New NotImplementedException()
    End Sub
End Class
Class Bar
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDuplicateNames() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo|](DirectCast(Me.Baz, Bar), Me.Baz)
    End Sub
    Dim Baz As Integer
End Class
Class Bar
End Class",
"Imports System
Class C
    Sub M()
        Foo(DirectCast(Me.Baz, Bar), Me.Baz)
    End Sub
    Private Sub Foo(baz1 As Bar, baz2 As Integer)
        Throw New NotImplementedException()
    End Sub
    Dim Baz As Integer
End Class
Class Bar
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgs1() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo(Of Integer)|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo(Of Integer)()
    End Sub
    Private Sub Foo(Of T)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgs2() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|Foo(Of Integer, String)|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo(Of Integer, String)()
    End Sub
    Private Sub Foo(Of T1, T2)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgsFromMethod() As Task
            Await TestAsync(
"Class C
    Sub M(Of X, Y)(x As X, y As Y)
        [|Foo|](x)
    End Sub
End Class",
"Imports System
Class C
    Sub M(Of X, Y)(x As X, y As Y)
        Foo(x)
    End Sub
    Private Sub Foo(Of X)(x1 As X)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgThatIsTypeParameter() As Task
            Await TestAsync(
"Class C
    Sub M(Of X)(y1 As X(), x1 As System.Func(Of X))
        [|Foo(Of X)|](y1, x1)
    End Sub
End Class",
"Imports System
Class C
    Sub M(Of X)(y1 As X(), x1 As System.Func(Of X))
        Foo(Of X)(y1, x1)
    End Sub
    Private Sub Foo(Of X)(y1() As X, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsThatAreTypeParameters() As Task
            Await TestAsync(
"Class C
    Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X))
        [|Foo(Of X, Y)|](y1, x1)
    End Sub
End Class",
"Imports System
Class C
    Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X))
        Foo(Of X, Y)(y1, x1)
    End Sub
    Private Sub Foo(Of X, Y)(y1() As Y, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsFromMethod() As Task
            Await TestAsync(
"Class C
    Sub M(Of X, Y)(x As X, y As Y)
        [|Foo|](x, y)
    End Sub
End Class",
"Imports System
Class C
    Sub M(Of X, Y)(x As X, y As Y)
        Foo(x, y)
    End Sub
    Private Sub Foo(Of X, Y)(x1 As X, y1 As Y)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsFromMethod2() As Task
            Await TestAsync(
"Class C
    Sub M(Of X, Y)(y As Y(), x As System.Func(Of X))
        [|Foo|](y, x)
    End Sub
End Class",
"Imports System
Class C
    Sub M(Of X, Y)(y As Y(), x As System.Func(Of X))
        Foo(y, x)
    End Sub
    Private Sub Foo(Of Y, X)(y1() As Y, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoOuterThroughInstance() As Task
            Await TestAsync(
"Class Outer
    Class C
        Sub M(o As Outer)
            o.[|Foo|]()
        End Sub
    End Class
End Class",
"Imports System
Class Outer
    Class C
        Sub M(o As Outer)
            o.Foo()
        End Sub
    End Class
    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoOuterThroughClass() As Task
            Await TestAsync(
"Class Outer
    Class C
        Sub M(o As Outer)
            Outer.[|Foo|]()
        End Sub
    End Class
End Class",
"Imports System
Class Outer
    Class C
        Sub M(o As Outer)
            Outer.Foo()
        End Sub
    End Class Private Shared Sub Foo() 
 Throw New NotImplementedException()
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoSiblingThroughInstance() As Task
            Await TestAsync(
"Class C
    Sub M(s As Sibling)
        s.[|Foo|]()
    End Sub
End Class
Class Sibling
End Class",
"Imports System
Class C
    Sub M(s As Sibling)
        s.Foo()
    End Sub
End Class
Class Sibling
    Friend Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoSiblingThroughClass() As Task
            Await TestAsync(
"Class C
    Sub M(s As Sibling)
        [|Sibling.Foo|]()
    End Sub
End Class
Class Sibling
End Class",
"Imports System
Class C
    Sub M(s As Sibling)
        Sibling.Foo()
    End Sub
End Class
Class Sibling
    Friend Shared Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoInterfaceThroughInstance() As Task
            Await TestAsync(
"Class C
    Sub M(s As ISibling)
        s.[|Foo|]()
    End Sub
End Class
Interface ISibling
End Interface",
"Class C
    Sub M(s As ISibling)
        s.Foo()
    End Sub
End Class
Interface ISibling
    Sub Foo()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateAbstractIntoSameType() As Task
            Await TestAsync(
"MustInherit Class C
    Sub M()
        [|Foo|]()
    End Sub
End Class",
"MustInherit Class C
    Sub M()
        Foo()
    End Sub
    Friend MustOverride Sub Foo()
End Class",
index:=1)
        End Function

        <WorkItem(539297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539297")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoModule() As Task
            Await TestAsync(
"Module Class C 
 Sub M()
        [|Foo|]()
    End Sub
End Module",
"Imports System
Module Class C 
 Sub M()
        Foo()
    End Sub
    Private Sub Foo()
        Throw New NotImplementedException() End Sub 
 End Module")
        End Function

        <WorkItem(539506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539506")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInference1() As Task
            Await TestAsync(
"Class C
    Sub M()
        Do While [|Foo|]()
        Loop
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Do While Foo()
        Loop
    End Sub
    Private Function Foo() As Boolean
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539505")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestEscaping1() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|[Sub]|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        [Sub]()
    End Sub
    Private Sub [Sub]()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539504")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestExplicitCall() As Task
            Await TestAsync(
"Class C
    Sub M()
        Call [|S|]
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Call S
    End Sub
    Private Sub S()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539504")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestImplicitCall() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S|]
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S
    End Sub
    Private Sub S()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539537")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArrayAccess1() As Task
            Await TestMissingAsync("Class C
    Sub M(x As Integer())
        Foo([|x|](4))
    End Sub
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterInteger() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S%|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S%()
    End Sub
    Private Function S() As Integer
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterLong() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S&|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S&()
    End Sub
    Private Function S() As Long
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterDecimal() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S@|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S@()
    End Sub
    Private Function S() As Decimal
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterSingle() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S!|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S!()
    End Sub
    Private Function S() As Single
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterDouble() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S#|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S#()
    End Sub
    Private Function S() As Double
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterString() As Task
            Await TestAsync(
"Class C
    Sub M()
        [|S$|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        S$()
    End Sub
    Private Function S() As String
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(539283, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539283")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNewLines() As Task
            Await TestAsync(
                <text>Public Class C
    Sub M()
        [|Foo|]()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf),
                <text>Imports System

Public Class C
    Sub M()
        Foo()
    End Sub

    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(539283, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539283")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNewLines2() As Task
            Await TestAsync(
                <text>Public Class C
    Sub M()
        D.[|Foo|]()
    End Sub
End Class

Public Class D
End Class</text>.Value.Replace(vbLf, vbCrLf),
                <text>Imports System

Public Class C
    Sub M()
        D.Foo()
    End Sub
End Class

Public Class D
    Friend Shared Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArgumentTypeVoid() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main()
        Dim v As Void
        [|Foo|](v)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Dim v As Void
        Foo(v)
    End Sub
    Private Sub Foo(v As Object)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateFromImplementsClause() As Task
            Await TestAsync(
"Class Program
    Implements IFoo
    Public Function Bip(i As Integer) As String Implements [|IFoo.Snarf|]
    End Function
End Class
Interface IFoo
End Interface",
"Class Program
    Implements IFoo
    Public Function Bip(i As Integer) As String Implements IFoo.Snarf
    End Function
End Class
Interface IFoo
    Function Snarf(i As Integer) As String
End Interface")
        End Function

        <WorkItem(537929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537929")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInScript1() As Task
            Await TestAsync(
"Imports System
Shared Sub Main(args As String())
    [|Foo|]()
End Sub",
"Imports System
Shared Sub Main(args As String())
    Foo()
End Sub
Private Shared Sub Foo()
    Throw New NotImplementedException()
End Sub",
            parseOptions:=GetScriptOptions())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInTopLevelImplicitClass1() As Task
            Await TestAsync(
"Imports System
Shared Sub Main(args As String())
    [|Foo|]()
End Sub",
"Imports System
Shared Sub Main(args As String())
    Foo()
End Sub
Private Shared Sub Foo()
    Throw New NotImplementedException()
End Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInNamespaceImplicitClass1() As Task
            Await TestAsync(
"Imports System
Namespace N
    Shared Sub Main(args As String())
        [|Foo|]()
    End Sub
End Namespace",
"Imports System
Namespace N
    Shared Sub Main(args As String())
        Foo()
    End Sub
    Private Shared Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInNamespaceImplicitClass_FieldInitializer() As Task
            Await TestAsync(
"Imports System
Namespace N
    Dim a As Integer = [|Foo|]()
End Namespace",
"Imports System
Namespace N
    Dim a As Integer = Foo()
    Private Function Foo() As Integer
        Throw New NotImplementedException()
    End Function
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod1() As Task
            Await TestMissingAsync(
"Class Program
    Implements IFoo
    Public Function Blah() As String Implements [|IFoo.Blah|]
    End Function
End Class
Interface IFoo
    Sub Blah()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod2() As Task
            Await TestMissingAsync(
"Class Program
    Implements IFoo
    Public Function Blah() As String Implements [|IFoo.Blah|]
    End Function
End Class
Interface IFoo
    Sub Blah()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod3() As Task
            Await TestAsync(
"Class C
    Implements IFoo
    Sub Snarf() Implements [|IFoo.Blah|]
    End Sub
End Class
Interface IFoo
    Sub Blah(ByRef i As Integer)
End Interface",
"Class C
    Implements IFoo
    Sub Snarf() Implements IFoo.Blah
    End Sub
End Class
Interface IFoo
    Sub Blah(ByRef i As Integer)
    Sub Blah()
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod4() As Task
            Await TestAsync(
"Class C
    Implements IFoo
    Sub Snarf(i As String) Implements [|IFoo.Blah|]
    End Sub
End Class
Interface IFoo
    Sub Blah(ByRef i As Integer)
End Interface",
"Class C
    Implements IFoo
    Sub Snarf(i As String) Implements IFoo.Blah
    End Sub
End Class
Interface IFoo
    Sub Blah(ByRef i As Integer)
    Sub Blah(i As String)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod5() As Task
            Await TestAsync(
"Class C
    Implements IFoo
    Sub Blah(i As Integer) Implements [|IFoo.Snarf|]
    End Sub
End Class
Friend Interface IFoo
    Sub Snarf(i As String)
End Interface",
"Class C
    Implements IFoo
    Sub Blah(i As Integer) Implements IFoo.Snarf
    End Sub
End Class
Friend Interface IFoo
    Sub Snarf(i As String)
    Sub Snarf(i As Integer)
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod6() As Task
            Await TestAsync(
"Class C
    Implements IFoo
    Sub Blah(i As Integer, s As String) Implements [|IFoo.Snarf|]
    End Sub
End Class
Friend Interface IFoo
    Sub Snarf(i As Integer, b As Boolean)
End Interface",
"Class C
    Implements IFoo
    Sub Blah(i As Integer, s As String) Implements IFoo.Snarf
    End Sub
End Class
Friend Interface IFoo
    Sub Snarf(i As Integer, b As Boolean)
    Sub Snarf(i As Integer, s As String)
End Interface")
        End Function

        <WorkItem(539708, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539708")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNoStaticGenerationIntoInterface() As Task
            Await TestMissingAsync(
"Interface IFoo
End Interface
Class Program
    Sub Main
        IFoo.[|Bar|]
    End Sub
End Class")
        End Function

        <WorkItem(539821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539821")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestEscapeParametername() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        Dim [string] As String = ""hello"" 
 [|[Me]|]([string])
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        Dim [string] As String = ""hello"" 
 [Me]([string])
    End Sub
    Private Sub [Me]([string] As String)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(539810, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539810")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotUseUnavailableTypeParameter() As Task
            Await TestAsync(
"Class Test
    Sub M(Of T)(x As T)
        [|Foo(Of Integer)|](x)
    End Sub
End Class",
"Imports System
Class Test
    Sub M(Of T)(x As T)
        Foo(Of Integer)(x)
    End Sub
    Private Sub Foo(Of T)(x As T)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539808")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotUseTypeParametersFromContainingType() As Task
            Await TestAsync(
"Class Test(Of T)
    Sub M()
        [|Method(Of T)|]()
    End Sub
End Class",
"Imports System
Class Test(Of T)
    Sub M()
        Method(Of T)()
    End Sub
    Private Sub Method(Of T1)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNameSimplification1() As Task
            Await TestAsync(
"Imports System
Class C
    Sub M()
        [|Foo|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Foo()
    End Sub
    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(539809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539809")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestFormattingOfMembers() As Task
            Await TestAsync(
<Text>Class Test
    Private id As Integer

    Private name As String

    Sub M()
        [|Foo|](id)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Class Test
    Private id As Integer

    Private name As String

    Sub M()
        Foo(id)
    End Sub

    Private Sub Foo(id As Integer)
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(540013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540013")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInAddressOfExpression1() As Task
            Await TestAsync(
"Delegate Sub D(x As Integer)
Class C
    Public Sub Foo()
        Dim x As D = New D(AddressOf [|Method|])
    End Sub
End Class",
"Imports System
Delegate Sub D(x As Integer)
Class C
    Public Sub Foo()
        Dim x As D = New D(AddressOf Method)
    End Sub
    Private Sub Method(x As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(527986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527986")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOfferedForInferredGenericMethodArgs() As Task
            Await TestMissingAsync(
"Class Foo(Of T)
    Sub Main(Of T, X)(k As Foo(Of T))
        [|Bar|](k)
    End Sub
    Private Sub Bar(Of T)(k As Foo(Of T))
    End Sub
End Class")
        End Function

        <WorkItem(540740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540740")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDelegateInAsClause() As Task
            Await TestAsync(
"Delegate Sub D(x As Integer)
Class C
    Private Sub M()
        Dim d As New D(AddressOf [|Test|])
    End Sub
End Class",
"Imports System
Delegate Sub D(x As Integer)
Class C
    Private Sub M()
        Dim d As New D(AddressOf Test)
    End Sub
    Private Sub Test(x As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <WorkItem(541405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541405")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMissingOnImplementedInterfaceMethod() As Task
            Await TestMissingAsync(
"Class C(Of U)
    Implements ITest
    Public Sub Method(x As U) Implements [|ITest.Method|]
    End Sub
End Class
Friend Interface ITest
    Sub Method(x As Object)
End Interface")
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOnConstructorInitializer() As Task
            Await TestMissingAsync(
"Class C
    Sub New
        Me.[|New|](1)
    End Sub
End Class")
        End Function

        <WorkItem(542838, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542838")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleImportsAdded() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        For Each v As Integer In [|HERE|]() : Next
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Module Program
    Sub Main(args As String())
        For Each v As Integer In HERE() : Next
    End Sub
    Private Function HERE() As IEnumerable(Of Integer)
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(543007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543007")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCompilationMemberImports() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        For Each v As Integer In [|HERE|]() : Next
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        For Each v As Integer In HERE() : Next
    End Sub
    Private Function HERE() As IEnumerable(Of Integer)
        Throw New NotImplementedException()
    End Function
End Module",
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Function

        <WorkItem(531301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestForEachWithNoControlVariableType() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        For Each v In [|HERE|] : Next
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        For Each v In HERE : Next
    End Sub
    Private Function HERE() As IEnumerable(Of Object)
        Throw New NotImplementedException()
    End Function
End Module",
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Function

        <WorkItem(531301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestElseIfStatement() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        If x Then
        ElseIf [|HERE|] Then
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        If x Then
        ElseIf HERE Then
        End If
    End Sub
    Private Function HERE() As Boolean
        Throw New NotImplementedException()
    End Function
End Module",
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Function

        <WorkItem(531301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestForStatement() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        For x As Integer = 1 To [|HERE|]
 End Sub
End Module",
"Module Program
    Sub Main(args As String())
        For x As Integer = 1 To HERE
 End Sub
    Private Function HERE() As Integer
        Throw New NotImplementedException()
    End Function
End Module",
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Function

        <WorkItem(543216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543216")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArrayOfAnonymousTypes() As Task
            Await TestAsync(
"Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim product = New With {Key .Name = """", Key .Price = 0}
        Dim products = ToList(product)
        [|HERE|](products)
    End Sub
    Function ToList(Of T)(a As T) As IEnumerable(Of T)
        Return Nothing
    End Function
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim product = New With {Key .Name = """", Key .Price = 0}
        Dim products = ToList(product)
        HERE(products)
    End Sub
    Private Sub HERE(products As IEnumerable(Of Object))
        Throw New NotImplementedException()
    End Sub
    Function ToList(Of T)(a As T) As IEnumerable(Of T)
        Return Nothing
    End Function
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMissingOnHiddenType() As Task
            Await TestMissingAsync(
<text>
#externalsource("file", num)
class C
    sub Foo()
        D.[|Bar|]()
    end sub
end class
#end externalsource

class D
EndClass
</text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotGenerateIntoHiddenRegion1_NoImports() As Task
            Await TestAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Foo()
        [|Bar|]()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
#ExternalSource ("file", num)
Class C
    Private Sub Bar()
        Throw New System.NotImplementedException()
    End Sub

    Sub Foo()
        Bar()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotGenerateIntoHiddenRegion1_WithImports() As Task
            Await TestAsync(
<text>
#ExternalSource ("file", num)
Imports System.Threading
#End ExternalSource

#ExternalSource ("file", num)
Class C
    Sub Foo()
        [|Bar|]()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
#ExternalSource ("file", num)
Imports System
Imports System.Threading
#End ExternalSource

#ExternalSource ("file", num)
Class C
    Private Sub Bar()
        Throw New NotImplementedException()
    End Sub

    Sub Foo()
        Bar()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotGenerateIntoHiddenRegion2() As Task
            Await TestAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Foo()
        [|Bar|]()
#End ExternalSource
    End Sub

    Sub Baz()
#ExternalSource ("file", num)
    End Sub
End Class
#End ExternalSource
</text>.Value.Replace(vbLf, vbCrLf),
<text>
#ExternalSource ("file", num)
Class C
    Sub Foo()
        Bar()
#End ExternalSource
    End Sub

    Sub Baz()
#ExternalSource ("file", num)
    End Sub

    Private Sub Bar()
        Throw New System.NotImplementedException()
    End Sub
End Class
#End ExternalSource
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotGenerateIntoHiddenRegion3() As Task
            Await TestAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Foo()
        [|Bar|]()
#End ExternalSource
    End Sub

    Sub Baz()
#ExternalSource ("file", num)
    End Sub

    Sub Quux()
    End Sub
End Class
#End ExternalSource
</text>.Value.Replace(vbLf, vbCrLf),
<text>
#ExternalSource ("file", num)
Class C
    Sub Foo()
        Bar()
#End ExternalSource
    End Sub

    Sub Baz()
#ExternalSource ("file", num)
    End Sub

    Private Sub Bar()
        Throw New System.NotImplementedException()
    End Sub

    Sub Quux()
    End Sub
End Class
#End ExternalSource
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestAddressOfInference1() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main(ByVal args As String())
        Dim v As Func(Of String) = Nothing
        Dim a1 = If(False, v, AddressOf [|TestMethod|])
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(ByVal args As String())
        Dim v As Func(Of String) = Nothing
        Dim a1 = If(False, v, AddressOf TestMethod)
    End Sub
    Private Function TestMethod() As String
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(544641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestAsync(
"Class C : End Class
Class B
    Sub Foo()
        C.[|Bar|]()
    End Sub
End Class",
"Imports System
Class C
    Friend Shared Sub Bar()
        Throw New NotImplementedException()
    End Sub
End Class
Class B
    Sub Foo()
        C.Bar()
    End Sub
End Class")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments1() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](,,)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(,,)
    End Sub
    Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional p3 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments2() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](1,,)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(1,,)
    End Sub
    Private Sub foo(v As Integer, Optional p1 As Object = Nothing, Optional p2 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments3() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](, 1,)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(, 1,)
    End Sub
    Private Sub foo(Optional p1 As Object = Nothing, Optional v As Integer = Nothing, Optional p2 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments4() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](,, 1)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(,, 1)
    End Sub
    Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional v As Integer = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments5() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](1,, 1)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(1,, 1)
    End Sub
    Private Sub foo(v1 As Integer, Optional p As Object = Nothing, Optional v2 As Integer = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments6() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|foo|](1, 1, )
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        foo(1, 1, )
    End Sub
    Private Sub foo(v1 As Integer, v2 As Integer, Optional p As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546683")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOnMissingMethodName() As Task
            Await TestMissingAsync("Class C
    Sub M()
        Me.[||] 
 End Sub
End Class")
        End Function

        <WorkItem(546684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546684")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateFromEventHandler() As Task
            Await TestAsync(
"Module Module1
    Sub Main()
        Dim c1 As New Class1
        AddHandler c1.AnEvent, AddressOf [|EventHandler1|]
    End Sub
    Public Class Class1
        Public Event AnEvent()
    End Class
End Module",
"Imports System
Module Module1
    Sub Main()
        Dim c1 As New Class1
        AddHandler c1.AnEvent, AddressOf EventHandler1
    End Sub
    Private Sub EventHandler1()
        Throw New NotImplementedException()
    End Sub
    Public Class Class1
        Public Event AnEvent()
    End Class
End Module")
        End Function

        <WorkItem(530814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530814")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCapturedMethodTypeParameterThroughLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Module M
    Sub Foo(Of T, S)(x As List(Of T), y As List(Of S))
        [|Bar|](x, Function() y) ' Generate Bar 
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Module M
    Sub Foo(Of T, S)(x As List(Of T), y As List(Of S))
        Bar(x, Function() y) ' Generate Bar 
    End Sub
    Private Sub Bar(Of T, S)(x As List(Of T), p As Func(Of List(Of S)))
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeParameterAndParameterConflict1() As Task
            Await TestAsync(
"Imports System
Class C(Of T)
    Sub Foo(x As T)
        M.[|Bar|](T:=x)
    End Sub
End Class

Module M
End Module",
"Imports System
Class C(Of T)
    Sub Foo(x As T)
        M.Bar(T:=x)
    End Sub
End Class

Module M
    Friend Sub Bar(Of T1)(T As T1)
    End Sub
End Module")
        End Function

        <WorkItem(530968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530968")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeParameterAndParameterConflict2() As Task
            Await TestAsync(
"Imports System
Class C(Of T)
    Sub Foo(x As T)
        M.[|Bar|](t:=x) ' Generate Bar 
    End Sub
End Class

Module M
End Module",
"Imports System
Class C(Of T)
    Sub Foo(x As T)
        M.Bar(t:=x) ' Generate Bar 
    End Sub
End Class

Module M
    Friend Sub Bar(Of T1)(t As T1)
    End Sub
End Module")
        End Function

        <WorkItem(546850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546850")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCollectionInitializer1() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main(args As String())
        [|Bar|](1, {1})
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        Bar(1, {1})
    End Sub
    Private Sub Bar(v As Integer, p() As Integer)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(546925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546925")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCollectionInitializer2() As Task
            Await TestAsync(
"Imports System
Module M
    Sub Main()
        [|Foo|]({{1}})
    End Sub
End Module",
"Imports System
Module M
    Sub Main()
        Foo({{1}})
    End Sub
    Private Sub Foo(p(,) As Integer)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <WorkItem(530818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530818")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParameterizedProperty1() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main()
        [|Prop|](1) = 2
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Prop(1) = 2
    End Sub
    Private Function Prop(v As Integer) As Integer
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(530818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530818")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParameterizedProperty2() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main()
        [|Prop|](1) = 2
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Prop(1) = 2
    End Sub
    Private Property Prop(v As Integer) As Integer
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As Integer)
            Throw New NotImplementedException()
        End Set
    End Property
End Module",
index:=1)
        End Function

        <WorkItem(907612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithLambda_1() As Task
            Await TestAsync(
<text>
Imports System

Module Program
    Public Sub CallIt()
        Baz([|Function()
                Return ""
            End Function|])
    End Sub

    Public Sub Baz()
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Module Program
    Public Sub CallIt()
        Baz(Function()
                Return ""
            End Function)
    End Sub

    Private Sub Baz(p As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz()
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(907612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithLambda_2() As Task
            Await TestAsync(
<text>
Imports System

Module Program
    Public Sub CallIt()
        Baz([|Function()
                Return ""
            End Function|])
    End Sub

    Public Sub Baz(one As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Module Program
    Public Sub CallIt()
        Baz(Function()
                Return ""
            End Function)
    End Sub

    Private Sub Baz(p As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz(one As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(907612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithLambda_3() As Task
            Await TestAsync(
<text>
Imports System

Module Program
    Public Sub CallIt()
        [|Baz|](Function()
                Return ""
            End Function)
    End Sub

    Public Sub Baz(one As Func(Of String), two As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Module Program
    Public Sub CallIt()
        Baz(Function()
                Return ""
            End Function)
    End Sub

    Private Sub Baz(p As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz(one As Func(Of String), two As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodForDifferentParameterName() As Task
            Await TestAsync(
<text>
Class Program
    Sub M()
        [|M|](x:=3)
    End Sub

    Sub M(y As Integer)
        M()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class Program
    Sub M()
        M(x:=3)
    End Sub

    Private Sub M(x As Integer)
        Throw New NotImplementedException()
    End Sub

    Sub M(y As Integer)
        M()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(769760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769760")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodForSameNamedButGenericUsage_1() As Task
            Await TestAsync(
<text>
Class Program
    Sub Main(args As String())
        Foo()
        [|Foo(Of Integer)|]()
    End Sub

    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class Program
    Sub Main(args As String())
        Foo()
        Foo(Of Integer)()
    End Sub

    Private Sub Foo(Of T)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(769760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769760")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodForSameNamedButGenericUsage_2() As Task
            Await TestAsync(
<text>Imports System
Class Program
    Sub Main(args As String())
        Foo()
        [|Foo(Of Integer, Integer)|]()
    End Sub

    Private Sub Foo(Of T)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Class Program
    Sub Main(args As String())
        Foo()
        Foo(Of Integer, Integer)()
    End Sub

    Private Sub Foo(Of T1, T2)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Foo(Of T)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Foo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(935731, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/935731")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodForAwaitWithoutParenthesis() As Task
            Await TestAsync(
<text>Module Module1
    Async Sub Method_ASub()
        Dim x = [|Await Foo|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Imports System.Threading.Tasks

Module Module1
    Async Sub Method_ASub()
        Dim x = Await Foo
    End Sub

    Private Function Foo() As Task(Of Object)
        Throw New NotImplementedException()
    End Function
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodTooManyArgs1() As Task
            Await TestAsync(
<text>Module M1
    Sub Main()
        [|test("CC", 15, 45)|]
    End Sub
    Sub test(ByVal name As String, ByVal age As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module M1
    Sub Main()
        test("CC", 15, 45)
    End Sub

    Private Sub test(v1 As String, v2 As Integer, v3 As Integer)
        Throw New NotImplementedException()
    End Sub

    Sub test(ByVal name As String, ByVal age As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNamespaceNotExpression1() As Task
            Await TestAsync(
<text>Imports System
Module M1
    Sub Foo()
        [|Text|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Module M1
    Sub Foo()
        Text
    End Sub

    Private Sub Text()
        Throw New NotImplementedException()
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoArgumentCountOverloadCandidates1() As Task
            Await TestAsync(
<text>Module Module1
    Class C0
        Public whichOne As String
        Sub Foo(ByVal t1 As String)
            whichOne = "T"
        End Sub
    End Class
    Class C1
        Inherits C0
        Overloads Sub Foo(ByVal y1 As String)
            whichOne = "Y"
        End Sub
    End Class
    Sub test()
        Dim clsNarg2get As C1 = New C1()
        [|clsNarg2get.Foo(1, y1:=2)|]
    End Sub

End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0
        Public whichOne As String
        Sub Foo(ByVal t1 As String)
            whichOne = "T"
        End Sub
    End Class
    Class C1
        Inherits C0
        Overloads Sub Foo(ByVal y1 As String)
            whichOne = "Y"
        End Sub

        Friend Sub Foo(v As Integer, y1 As Integer)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim clsNarg2get As C1 = New C1()
        clsNarg2get.Foo(1, y1:=2)
    End Sub

End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodFunctionResultCannotBeIndexed1() As Task
            Await TestAsync(
<text>Imports Microsoft.VisualBasic.FileSystem
Module M1
    Sub foo()
        If [|FreeFile(1)|] = 255 Then
        End If
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Imports Microsoft.VisualBasic.FileSystem
Module M1
    Sub foo()
        If FreeFile(1) = 255 Then
        End If
    End Sub

    Private Function FreeFile(v As Integer) As Integer
        Throw New NotImplementedException()
    End Function
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoCallableOverloadCandidates2() As Task
            Await TestAsync(
<text>Class M1
    Sub sub1(Of U, V)(ByVal p1 As U, ByVal p2 As V)
    End Sub
    Sub sub1(Of U, V)(ByVal p1() As V, ByVal p2() As U)
    End Sub
    Sub GenMethod6210()
        [|sub1(Of Integer, String)|](New Integer() {1, 2, 3}, New String() {"a", "b"})
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Class M1
    Sub sub1(Of U, V)(ByVal p1 As U, ByVal p2 As V)
    End Sub
    Sub sub1(Of U, V)(ByVal p1() As V, ByVal p2() As U)
    End Sub
    Sub GenMethod6210()
        sub1(Of Integer, String)(New Integer() {1, 2, 3}, New String() {"a", "b"})
    End Sub

    Private Sub sub1(Of T1, T2)(v1() As T1, v2() As T2)
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates2() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property
    End Class
    Structure S1
        Dim i As Integer
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
            Return New C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
            Return New S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim tc2 As New C1(Of S1, C1(Of Integer, Integer))
        Call [|tc2.Foo(New Scenario11)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property

        Friend Sub Foo(scenario11 As Scenario11)
            Throw New NotImplementedException()
        End Sub
    End Class
    Structure S1
        Dim i As Integer
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
            Return New C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
            Return New S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim tc2 As New C1(Of S1, C1(Of Integer, Integer))
        Call tc2.Foo(New Scenario11)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates3() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property
    End Class
    Structure S1
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim tc2 As New C1(Of S1, C1(Of Integer, Integer))
        Dim sc11 As New Scenario11
        Call [|tc2.Foo(sc11)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Foo(sc11 As Scenario11)
            Throw New NotImplementedException()
        End Sub
    End Class
    Structure S1
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim tc2 As New C1(Of S1, C1(Of Integer, Integer))
        Dim sc11 As New Scenario11
        Call tc2.Foo(sc11)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates4() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property
    End Class
    Structure S1
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim dTmp As Decimal = CDec(2000000)
        Dim tc3 As New C1(Of Short, Long)
        Call [|tc3.Foo(dTmp)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Foo(dTmp As Decimal)
            Throw New NotImplementedException()
        End Sub
    End Class
    Structure S1
    End Structure
    Class Scenario11
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
        End Operator
        Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
        End Operator
    End Class
    Sub GenUnif0060()
        Dim dTmp As Decimal = CDec(2000000)
        Dim tc3 As New C1(Of Short, Long)
        Call tc3.Foo(dTmp)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodArgumentNarrowing() As Task
            Await TestAsync(
<text>Option Strict Off
Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Foo(sample7C1(Of Long).E.e1)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Option Strict Off
Imports System

Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub

        Friend Sub Foo(e1 As sample7C1(Of Long).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sample7C1(Of Long).E.e1)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodArgumentNarrowing2() As Task
            Await TestAsync(
<text>Option Strict Off
Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Foo(sample7C1(Of Short).E.e2)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Option Strict Off
Imports System

Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub

        Friend Sub Foo(e2 As sample7C1(Of Short).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sample7C1(Of Short).E.e2)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodArgumentNarrowing3() As Task
            Await TestAsync(
<text>Option Strict Off
Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Foo(sc7.E.e3)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Option Strict Off
Imports System

Module Module1
    Class sample7C1(Of X)
        Enum E
            e1
            e2
            e3
        End Enum
    End Class
    Class sample7C2(Of T, Y)
        Public whichOne As String
        Sub Foo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
        End Sub

        Friend Sub Foo(e3 As sample7C1(Of Byte).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sc7.E.e3)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoMostSpecificOverload2() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
        End Sub
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub
    End Class
    Structure S1
    End Structure
    Class C2
        Public Shared Widening Operator CType(ByVal Arg As C2) As C1(Of Integer, Integer)
        End Operator
        Public Shared Widening Operator CType(ByVal Arg As C2) As S1
        End Operator
    End Class
    Sub test()
        Dim C As New C1(Of S1, C1(Of Integer, Integer))
        Call [|C.Foo(New C2)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
        End Sub
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
        End Sub

        Friend Sub Foo(c2 As C2)
            Throw New NotImplementedException()
        End Sub
    End Class
    Structure S1
    End Structure
    Class C2
        Public Shared Widening Operator CType(ByVal Arg As C2) As C1(Of Integer, Integer)
        End Operator
        Public Shared Widening Operator CType(ByVal Arg As C2) As S1
        End Operator
    End Class
    Sub test()
        Dim C As New C1(Of S1, C1(Of Integer, Integer))
        Call C.Foo(New C2)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodInsideNameOf() As Task
            Await TestAsync(
<text>
Imports System

Class C
    Sub M()
        Dim x = NameOf ([|Z|])
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class C
    Sub M()
        Dim x = NameOf (Z)
    End Sub

    Private Function Z() As Object
        Throw New NotImplementedException()
    End Function
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodInsideNameOf2() As Task
            Await TestAsync(
<text>
Imports System

Class C
    Sub M()
        Dim x = NameOf ([|Z.X.Y|])
    End Sub
End Class

Namespace Z
    Class X

    End Class
End Namespace
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class C
    Sub M()
        Dim x = NameOf (Z.X.Y)
    End Sub
End Class

Namespace Z
    Class X
        Friend Shared Function Y() As Object
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithNameOfArgument() As Task
            Await TestAsync(
<text>
Class C
    Sub M()
        [|M2(NameOf(M))|]
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class C
    Sub M()
        M2(NameOf(M))
    End Sub

    Private Sub M2(v As String)
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithLambdaAndNameOfArgument() As Task
            Await TestAsync(
<text>
Class C
    Sub M()
        [|M2(Function() NameOf(M))|]
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Class C
    Sub M()
        M2(Function() NameOf(M))
    End Sub

    Private Sub M2(p As Func(Of String))
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B
    End Sub
    Private Function B() As C
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis2() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B
    End Sub
    Private Function B() As Object
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis3() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B
    End Sub
    Private Function B() As Integer
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis4() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C? = a?.B
    End Sub
    Private Function B() As C
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis5() As Task
            Await TestAsync(
"Option Strict On
Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Option Strict On
Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Integer
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis6() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Integer
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis7() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Object
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis8() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As C
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis9() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Integer
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis10() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Integer
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis11() As Task
            Await TestAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Function Z() As Object
            Throw New NotImplementedException()
        End Function
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B()
    End Sub
    Private Function B() As C
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess2() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B()
    End Sub
    Private Function B() As Object
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess3() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B()
    End Sub
    Private Function B() As Integer
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess4() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C? = a?.B()
    End Sub
    Private Function B() As C
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B()
    End Sub
    Private ReadOnly Property B As C
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess2() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B()
    End Sub
    Private ReadOnly Property B As Object
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess3() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B()
    End Sub
    Private ReadOnly Property B As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess4() As Task
            Await TestAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]()
    End Sub
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C? = a?.B()
    End Sub
    Private ReadOnly Property B As C
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer() As Task
            Await TestAsync(
"Module Program
    Property a As Integer = [|y|]
End Module",
"Imports System

Module Program
    Property a As Integer = y

    Private Function y() As Integer
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer2() As Task
            Await TestAsync(
"Module Program
    Property a As Integer = [|y|]()
End Module",
"Imports System

Module Program
    Property a As Integer = y()

    Private Function y() As Integer
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodTypeOf() As Task
            Await TestAsync(
"Module C
    Sub Test()
        If TypeOf [|B|] Is String Then
        End If
    End Sub
End Module",
"Imports System
Module C
    Sub Test()
        If TypeOf B Is String Then
        End If
    End Sub
    Private Function B() As String
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodTypeOf2() As Task
            Await TestAsync(
"Module C
    Sub Test()
        If TypeOf [|B|]() Is String Then
        End If
    End Sub
End Module",
"Imports System
Module C
    Sub Test()
        If TypeOf B() Is String Then
        End If
    End Sub
    Private Function B() As String
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConfigureAwaitFalse() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await Foo().ConfigureAwait(False)
    End Sub

    Private Function Foo() As Task(Of Boolean)
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConfigureAwaitFalse() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await Foo().ConfigureAwait(False)
    End Sub

    Private ReadOnly Property Foo As Task(Of Boolean)
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Module",
index:=1)
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithMethodChaining() As Task
            Await TestAsync(
"Imports System 
Imports System.Linq 
Module M 
    Async Sub T() 
        Dim x As Boolean = Await [|F|]().ConfigureAwait(False)
    End Sub 
End Module",
"Imports System 
Imports System.Linq 
Imports System.Threading.Tasks
Module M 
    Async Sub T() 
        Dim x As Boolean = Await F().ConfigureAwait(False)
    End Sub 
    Private Function F() As Task(Of Boolean)
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodInTypeOfIsNot() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf Prop IsNot TypeOfIsNotDerived Then
        End If
    End Sub
    Private Function Prop() As TypeOfIsNotDerived
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInCollectionInitializers1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Module Program
    Sub M()
        Dim x = New List(Of Integer) From {[|T|]()}
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Module Program
    Sub M()
        Dim x = New List(Of Integer) From {T()}
    End Sub
    Private Function T() As Integer
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(529480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInCollectionInitializers2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Module Program
    Sub M()
        Dim x = New Dictionary(Of Integer, Boolean) From {{1, [|T|]()}}
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Module Program
    Sub M()
        Dim x = New Dictionary(Of Integer, Boolean) From {{1, T()}}
    End Sub
    Private Function T() As Boolean
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <WorkItem(10004, "https://github.com/dotnet/roslyn/issues/10004")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithMultipleOfSameGenericType() As Task
            Await TestAsync(
<text>
Namespace TestClasses
    Public Class C
    End Class

    Module Ex
        Public Function M(Of T As C)(a As T) As T
            Return [|a.Test(Of T, T)()|]
        End Function
    End Module
End Namespace
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Namespace TestClasses
    Public Class C
        Friend Function Test(Of T1 As C, T2 As C)() As T2
        End Function
    End Class

    Module Ex
        Public Function M(Of T As C)(a As T) As T
            Return a.Test(Of T, T)()
        End Function
    End Module
End Namespace
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(11461, "https://github.com/dotnet/roslyn/issues/11461")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodOffOfExistingProperty() As Task
            Await TestAsync(
<text>
Imports System

Public NotInheritable Class Repository
    Shared ReadOnly Property agreementtype As AgreementType
        Get
        End Get
    End Property
End Class

Public Class Agreementtype
End Class

Class C
    Shared Sub TestError()
        [|Repository.AgreementType.NewFunction|]("", "")
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Imports System

Public NotInheritable Class Repository
    Shared ReadOnly Property agreementtype As AgreementType
        Get
        End Get
    End Property
End Class

Public Class Agreementtype
    Friend Sub NewFunction(v1 As String, v2 As String)
        Throw New NotImplementedException()
    End Sub
End Class

Class C
    Shared Sub TestError()
        Repository.AgreementType.NewFunction("", "")
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function MethodWithTuple() As Task
            Await TestAsync(
"Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (Integer, String) = [|NewMethod|]((1, ""hello""))
    End Sub
End Class",
"Imports System

Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (Integer, String) = NewMethod((1, ""hello""))
    End Sub

    Private Shared Function NewMethod(p As (Integer, String)) As (Integer, String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function MethodWithTupleWithNames() As Task
            Await TestAsync(
"Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (a As Integer, b As String) = [|NewMethod|]((c:=1, d:=""hello""))
    End Sub
End Class",
"Imports System

Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (a As Integer, b As String) = NewMethod((c:=1, d:=""hello""))
    End Sub

    Private Shared Function NewMethod(p As (c As Integer, d As String)) As (a As Integer, b As String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function MethodWithTupleWithOneName() As Task
            Await TestAsync(
"Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (a As Integer, String) = [|NewMethod|]((c:=1, ""hello""))
    End Sub
End Class",
"Imports System

Class Program
    Private Shared Async Sub Main(args As String())
        Dim d As (a As Integer, String) = NewMethod((c:=1, ""hello""))
    End Sub

    Private Shared Function NewMethod(p As (c As Integer, String)) As (a As Integer, String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        Public Class GenerateConversionTests
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateConversionCodeFixProvider())
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateExplicitConversionGenericClass() As Task
                Await TestAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = CType([|1|], C(Of Integer))
    End Sub
End Class

Class C(Of T)
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = CType(1, C(Of Integer))
    End Sub
End Class

Class C(Of T)
    Public Shared Narrowing Operator CType(v As Integer) As C(Of T)
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateExplicitConversionClass() As Task
                Await TestAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = CType([|1|], C)
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = CType(1, C)
    End Sub
End Class

Class C
    Public Shared Narrowing Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateExplicitConversionAwaitExpression() As Task
                Await TestAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = CType([|Await a|], C)
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = CType(Await a, C)
    End Sub
End Class

Class C
    Public Shared Narrowing Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateImplicitConversionTargetTypeNotInSource() As Task
                Await TestAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = [|dig|]
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = dig
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub

    Public Shared Widening Operator CType(v As Digit) As Double
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateImplicitConversionGenericClass() As Task
                Await TestAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = [|1|]
    End Sub
End Class

Class C(Of T)
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C(Of Integer) = 1
    End Sub
End Class

Class C(Of T)
    Public Shared Widening Operator CType(v As Integer) As C(Of T)
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateImplicitConversionClass() As Task
                Await TestAsync(
    <text>Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = [|1|]
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System

Class Program
    Private Shared Sub Main(args As String())
        Dim a As C = 1
    End Sub
End Class

Class C
    Public Shared Widening Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateImplicitConversionAwaitExpression() As Task
                Await TestAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = [|Await a|]
    End Sub
End Class

Class C
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim a = Task.FromResult(1)
        Dim b As C = Await a
    End Sub
End Class

Class C
    Public Shared Widening Operator CType(v As Integer) As C
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

            <WorkItem(774321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774321")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Async Function TestGenerateExplicitConversionTargetTypeNotInSource() As Task
                Await TestAsync(
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = CType([|dig|], Double)
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
    <text>Imports System
Imports System.Threading.Tasks

Class Program
    Private Shared Async Sub Main(args As String())
        Dim dig As Digit = New Digit(7)
        Dim number As Double = CType(dig, Double)
    End Sub
End Class

Class Digit
    Private val As Double

    Public Sub New(v As Double)
        Me.val = v
    End Sub

    Public Shared Narrowing Operator CType(v As Digit) As Double
        Throw New NotImplementedException()
    End Operator
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
            End Function

        End Class
    End Class
End Namespace
