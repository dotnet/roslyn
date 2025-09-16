' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateMethod
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
    Partial Public Class GenerateMethodTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New GenerateParameterizedMemberCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationIntoSameType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|]()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNotForExpressionOnLeftOfAssign() As Task
            Await TestMissingAsync(
"Class C
    Sub M()
        [|Goo|] = Bar()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11518")>
        Public Async Function TestNameMatchesNamespaceName() As Task
            Await TestInRegularAndScriptAsync(
"Namespace N
    Module Module1
        Sub Main()
            [|N|]()
        End Sub
    End Module
End Namespace",
"Imports System

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

        <Fact>
        Public Async Function TestSimpleInvocationOffOfMe() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        Me.[|Goo|]()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Me.Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationOffOfType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        C.[|Goo|]()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        C.Goo()
    End Sub

    Private Shared Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationValueExpressionArg() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|](0)
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo(0)
    End Sub

    Private Sub Goo(v As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationMultipleValueExpressionArg() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|](0, 0)
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo(0, 0)
    End Sub

    Private Sub Goo(v1 As Integer, v2 As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationValueArg() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](i)
    End Sub
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(i)
    End Sub

    Private Sub Goo(i As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSimpleInvocationNamedValueArg() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](bar:=i)
    End Sub
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(bar:=i)
    End Sub

    Private Sub Goo(bar As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateAfterMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|]()
    End Sub
    Sub NextMethod()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub

    Sub NextMethod()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInterfaceNaming() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](NextMethod())
    End Sub
    Function NextMethod() As IGoo
    End Function
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(NextMethod())
    End Sub

    Private Sub Goo(goo As IGoo)
        Throw New NotImplementedException()
    End Sub

    Function NextMethod() As IGoo
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestFuncArg0() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](NextMethod)
    End Sub
    Function NextMethod() As String
    End Function
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(NextMethod)
    End Sub

    Private Sub Goo(nextMethod As String)
        Throw New NotImplementedException()
    End Sub

    Function NextMethod() As String
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestFuncArg1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](NextMethod)
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(NextMethod)
    End Sub

    Private Sub Goo(nextMethod As Func(Of Integer, String))
        Throw New NotImplementedException()
    End Sub

    Function NextMethod(i As Integer) As String
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestAddressOf1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](AddressOf NextMethod)
    End Sub
    Function NextMethod(i As Integer) As String
    End Function
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(AddressOf NextMethod)
    End Sub

    Private Sub Goo(value As Func(Of Integer, String))
        Throw New NotImplementedException()
    End Sub

    Function NextMethod(i As Integer) As String
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestActionArg() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](NextMethod) End Sub 
 Sub NextMethod()
    End Sub
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(NextMethod) End Sub 
Private Sub Goo(nextMethod As Object)
        Throw New NotImplementedException()
    End Sub

    Sub NextMethod()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestActionArg1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [|Goo|](NextMethod)
    End Sub
    Sub NextMethod(i As Integer)
    End Sub
End Class",
"Imports System

Class C
    Sub M(i As Integer)
        Goo(NextMethod)
    End Sub

    Private Sub Goo(nextMethod As Action(Of Integer))
        Throw New NotImplementedException()
    End Sub

    Sub NextMethod(i As Integer)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestTypeInference() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        If [|Goo|]()
        End If
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        If Goo()
        End If
    End Sub

    Private Function Goo() As Boolean
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestMemberAccessArgumentName() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|](Me.Bar)
    End Sub
    Dim Bar As Integer
End Class",
"Imports System

Class C
    Sub M()
        Goo(Me.Bar)
    End Sub

    Private Sub Goo(bar As Integer)
        Throw New NotImplementedException()
    End Sub

    Dim Bar As Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestParenthesizedArgumentName() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|]((Bar))
    End Sub
    Dim Bar As Integer
End Class",
"Imports System

Class C
    Sub M()
        Goo((Bar))
    End Sub

    Private Sub Goo(bar As Integer)
        Throw New NotImplementedException()
    End Sub

    Dim Bar As Integer
End Class")
        End Function

        <Fact>
        Public Async Function TestCastedArgumentName() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|](DirectCast(Me.Baz, Bar))
    End Sub
End Class
Class Bar
End Class",
"Imports System

Class C
    Sub M()
        Goo(DirectCast(Me.Baz, Bar))
    End Sub

    Private Sub Goo(baz As Bar)
        Throw New NotImplementedException()
    End Sub
End Class
Class Bar
End Class")
        End Function

        <Fact>
        Public Async Function TestDuplicateNames() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo|](DirectCast(Me.Baz, Bar), Me.Baz)
    End Sub
    Dim Baz As Integer
End Class
Class Bar
End Class",
"Imports System

Class C
    Sub M()
        Goo(DirectCast(Me.Baz, Bar), Me.Baz)
    End Sub

    Private Sub Goo(baz1 As Bar, baz2 As Integer)
        Throw New NotImplementedException()
    End Sub

    Dim Baz As Integer
End Class
Class Bar
End Class")
        End Function

        <Fact>
        Public Async Function TestGenericArgs1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo(Of Integer)|]()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo(Of Integer)()
    End Sub

    Private Sub Goo(Of T)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenericArgs2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        [|Goo(Of Integer, String)|]()
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Goo(Of Integer, String)()
    End Sub

    Private Sub Goo(Of T1, T2)()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        Public Async Function TestGenericArgsFromMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(Of X, Y)(x As X, y As Y)
        [|Goo|](x)
    End Sub
End Class",
"Imports System

Class C
    Sub M(Of X, Y)(x As X, y As Y)
        Goo(x)
    End Sub

    Private Sub Goo(Of X)(x1 As X)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenericArgThatIsTypeParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(Of X)(y1 As X(), x1 As System.Func(Of X))
        [|Goo(Of X)|](y1, x1)
    End Sub
End Class",
"Imports System

Class C
    Sub M(Of X)(y1 As X(), x1 As System.Func(Of X))
        Goo(Of X)(y1, x1)
    End Sub

    Private Sub Goo(Of X)(y1() As X, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMultipleGenericArgsThatAreTypeParameters() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X))
        [|Goo(Of X, Y)|](y1, x1)
    End Sub
End Class",
"Imports System

Class C
    Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X))
        Goo(Of X, Y)(y1, x1)
    End Sub

    Private Sub Goo(Of X, Y)(y1() As Y, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        Public Async Function TestMultipleGenericArgsFromMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(Of X, Y)(x As X, y As Y)
        [|Goo|](x, y)
    End Sub
End Class",
"Imports System

Class C
    Sub M(Of X, Y)(x As X, y As Y)
        Goo(x, y)
    End Sub

    Private Sub Goo(Of X, Y)(x1 As X, y1 As Y)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539984")>
        Public Async Function TestMultipleGenericArgsFromMethod2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(Of X, Y)(y As Y(), x As System.Func(Of X))
        [|Goo|](y, x)
    End Sub
End Class",
"Imports System

Class C
    Sub M(Of X, Y)(y As Y(), x As System.Func(Of X))
        Goo(y, x)
    End Sub

    Private Sub Goo(Of Y, X)(y1() As Y, x1 As Func(Of X))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoOuterThroughInstance() As Task
            Await TestInRegularAndScriptAsync(
"Class Outer
    Class C
        Sub M(o As Outer)
            o.[|Goo|]()
        End Sub
    End Class
End Class",
"Imports System

Class Outer
    Class C
        Sub M(o As Outer)
            o.Goo()
        End Sub
    End Class

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoOuterThroughClass() As Task
            Await TestInRegularAndScriptAsync(
"Class Outer
    Class C
        Sub M(o As Outer)
            Outer.[|Goo|]()
        End Sub
    End Class
End Class",
"Imports System

Class Outer
    Class C
        Sub M(o As Outer)
            Outer.Goo()
        End Sub
    End Class

    Private Shared Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoSiblingThroughInstance() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(s As Sibling)
        s.[|Goo|]()
    End Sub
End Class
Class Sibling
End Class",
"Imports System

Class C
    Sub M(s As Sibling)
        s.Goo()
    End Sub
End Class
Class Sibling
    Friend Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoSiblingThroughClass() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(s As Sibling)
        [|Sibling.Goo|]()
    End Sub
End Class
Class Sibling
End Class",
"Imports System

Class C
    Sub M(s As Sibling)
        Sibling.Goo()
    End Sub
End Class
Class Sibling
    Friend Shared Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestGenerateIntoInterfaceThroughInstance() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(s As ISibling)
        s.[|Goo|]()
    End Sub
End Class
Interface ISibling
End Interface",
"Class C
    Sub M(s As ISibling)
        s.Goo()
    End Sub
End Class
Interface ISibling
    Sub Goo()
End Interface")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/29584")>
        Public Async Function TestGenerateAbstractIntoSameType() As Task
            Await TestInRegularAndScriptAsync(
"MustInherit Class C
    Sub M()
        [|Goo|]()
    End Sub
End Class",
"MustInherit Class C
    Sub M()
        Goo()
    End Sub

    Protected MustOverride Sub Goo()
End Class",
index:=1)
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539297")>
        Public Async Function TestGenerateIntoModule() As Task
            Await TestInRegularAndScriptAsync(
"Module Class C 
 Sub M()
        [|Goo|]()
    End Sub
End Module",
"Imports System

Module Class C 
 Sub M()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539506")>
        Public Async Function TestInference1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M()
        Do While [|Goo|]()
        Loop
    End Sub
End Class",
"Imports System

Class C
    Sub M()
        Do While Goo()
        Loop
    End Sub

    Private Function Goo() As Boolean
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539505")>
        Public Async Function TestEscaping1() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539504")>
        Public Async Function TestExplicitCall() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539504")>
        Public Async Function TestImplicitCall() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539537")>
        Public Async Function TestArrayAccess1() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(x As Integer())
        Goo([|x|](4))
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterInteger() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterLong() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterDecimal() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterSingle() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterDouble() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539560")>
        Public Async Function TestTypeCharacterString() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539283")>
        Public Async Function TestNewLines() As Task
            Await TestInRegularAndScriptAsync(
                <text>Public Class C
    Sub M()
        [|Goo|]()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf),
                <text>Imports System

Public Class C
    Sub M()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539283")>
        Public Async Function TestNewLines2() As Task
            Await TestInRegularAndScriptAsync(
                <text>Public Class C
    Sub M()
        D.[|Goo|]()
    End Sub
End Class

Public Class D
End Class</text>.Value.Replace(vbLf, vbCrLf),
                <text>Imports System

Public Class C
    Sub M()
        D.Goo()
    End Sub
End Class

Public Class D
    Friend Shared Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function TestArgumentTypeVoid() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Program
    Sub Main()
        Dim v As Void
        [|Goo|](v)
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Dim v As Void
        Goo(v)
    End Sub

    Private Sub Goo(v As Object)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestGenerateFromImplementsClause() As Task
            Await TestInRegularAndScriptAsync(
"Class Program
    Implements IGoo
    Public Function Bip(i As Integer) As String Implements [|IGoo.Snarf|]
    End Function
End Class
Interface IGoo
End Interface",
"Class Program
    Implements IGoo
    Public Function Bip(i As Integer) As String Implements IGoo.Snarf
    End Function
End Class
Interface IGoo
    Function Snarf(i As Integer) As String
End Interface")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537929")>
        Public Async Function TestInScript1() As Task
            Await TestAsync(
"Imports System
Shared Sub Main(args As String())
    [|Goo|]()
End Sub",
"Imports System
Shared Sub Main(args As String())
    Goo()
End Sub

Private Shared Sub Goo()
    Throw New NotImplementedException()
End Sub
",
            New TestParameters(parseOptions:=GetScriptOptions()))
        End Function

        <Fact>
        Public Async Function TestInTopLevelImplicitClass1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Shared Sub Main(args As String())
    [|Goo|]()
End Sub",
"Imports System
Shared Sub Main(args As String())
    Goo()
End Sub

Private Shared Sub Goo()
    Throw New NotImplementedException()
End Sub
")
        End Function

        <Fact>
        Public Async Function TestInNamespaceImplicitClass1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Namespace N
    Shared Sub Main(args As String())
        [|Goo|]()
    End Sub
End Namespace",
"Imports System
Namespace N
    Shared Sub Main(args As String())
        Goo()
    End Sub

    Private Shared Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Namespace")
        End Function

        <Fact>
        Public Async Function TestInNamespaceImplicitClass_FieldInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Namespace N
    Dim a As Integer = [|Goo|]()
End Namespace",
"Imports System
Namespace N
    Dim a As Integer = Goo()

    Private Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Namespace")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Program
    Implements IGoo
    Public Function Blah() As String Implements [|IGoo.Blah|]
    End Function
End Class
Interface IGoo
    Sub Blah()
End Interface")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Program
    Implements IGoo
    Public Function Blah() As String Implements [|IGoo.Blah|]
    End Function
End Class
Interface IGoo
    Sub Blah()
End Interface")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod3() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Implements IGoo
    Sub Snarf() Implements [|IGoo.Blah|]
    End Sub
End Class
Interface IGoo
    Sub Blah(ByRef i As Integer)
End Interface",
"Class C
    Implements IGoo
    Sub Snarf() Implements IGoo.Blah
    End Sub
End Class
Interface IGoo
    Sub Blah(ByRef i As Integer)
    Sub Blah()
End Interface")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod4() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Implements IGoo
    Sub Snarf(i As String) Implements [|IGoo.Blah|]
    End Sub
End Class
Interface IGoo
    Sub Blah(ByRef i As Integer)
End Interface",
"Class C
    Implements IGoo
    Sub Snarf(i As String) Implements IGoo.Blah
    End Sub
End Class
Interface IGoo
    Sub Blah(ByRef i As Integer)
    Sub Blah(i As String)
End Interface")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod5() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Implements IGoo
    Sub Blah(i As Integer) Implements [|IGoo.Snarf|]
    End Sub
End Class
Friend Interface IGoo
    Sub Snarf(i As String)
End Interface",
"Class C
    Implements IGoo
    Sub Blah(i As Integer) Implements IGoo.Snarf
    End Sub
End Class
Friend Interface IGoo
    Sub Snarf(i As String)
    Sub Snarf(i As Integer)
End Interface")
        End Function

        <Fact>
        Public Async Function TestClashesWithMethod6() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Implements IGoo
    Sub Blah(i As Integer, s As String) Implements [|IGoo.Snarf|]
    End Sub
End Class
Friend Interface IGoo
    Sub Snarf(i As Integer, b As Boolean)
End Interface",
"Class C
    Implements IGoo
    Sub Blah(i As Integer, s As String) Implements IGoo.Snarf
    End Sub
End Class
Friend Interface IGoo
    Sub Snarf(i As Integer, b As Boolean)
    Sub Snarf(i As Integer, s As String)
End Interface")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539708")>
        Public Async Function TestNoStaticGenerationIntoInterface() As Task
            Await TestMissingInRegularAndScriptAsync(
"Interface IGoo
End Interface
Class Program
    Sub Main
        IGoo.[|Bar|]
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539821")>
        Public Async Function TestEscapeParameterName() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539810")>
        Public Async Function TestDoNotUseUnavailableTypeParameter() As Task
            Await TestInRegularAndScriptAsync(
"Class Test
    Sub M(Of T)(x As T)
        [|Goo(Of Integer)|](x)
    End Sub
End Class",
"Imports System

Class Test
    Sub M(Of T)(x As T)
        Goo(Of Integer)(x)
    End Sub

    Private Sub Goo(Of T)(x As T)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539808")>
        Public Async Function TestDoNotUseTypeParametersFromContainingType() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestNameSimplification1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub M()
        [|Goo|]()
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539809")>
        Public Async Function TestFormattingOfMembers() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class Test
    Private id As Integer

    Private name As String

    Sub M()
        [|Goo|](id)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Class Test
    Private id As Integer

    Private name As String

    Sub M()
        Goo(id)
    End Sub

    Private Sub Goo(id As Integer)
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540013")>
        Public Async Function TestInAddressOfExpression1() As Task
            Await TestInRegularAndScriptAsync(
"Delegate Sub D(x As Integer)
Class C
    Public Sub Goo()
        Dim x As D = New D(AddressOf [|Method|])
    End Sub
End Class",
"Imports System

Delegate Sub D(x As Integer)
Class C
    Public Sub Goo()
        Dim x As D = New D(AddressOf Method)
    End Sub

    Private Sub Method(x As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527986")>
        Public Async Function TestNotOfferedForInferredGenericMethodArgs() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Goo(Of T)
    Sub Main(Of T, X)(k As Goo(Of T))
        [|Bar|](k)
    End Sub
    Private Sub Bar(Of T)(k As Goo(Of T))
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540740")>
        Public Async Function TestDelegateInAsClause() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541405")>
        Public Async Function TestMissingOnImplementedInterfaceMethod() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C(Of U)
    Implements ITest
    Public Sub Method(x As U) Implements [|ITest.Method|]
    End Sub
End Class
Friend Interface ITest
    Sub Method(x As Object)
End Interface")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        Public Async Function TestNotOnConstructorInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub New
        Me.[|New|](1)
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542838")>
        Public Async Function TestMultipleImportsAdded() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543007")>
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
New TestParameters(parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic"))))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
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
New TestParameters(parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic"))))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
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
New TestParameters(parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"))))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531301")>
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
New TestParameters(parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"))))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543216")>
        Public Async Function TestArrayOfAnonymousTypes() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestMissingOnHiddenType() As Task
            Await TestMissingInRegularAndScriptAsync(
<text>
#externalsource("file", num)
class C
    sub Goo()
        D.[|Bar|]()
    end sub
end class
#end externalsource

class D
EndClass
</text>.Value)
        End Function

        <Fact>
        Public Async Function TestDoNotGenerateIntoHiddenRegion1_NoImports() As Task
            Await TestInRegularAndScriptAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Goo()
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

    Sub Goo()
        Bar()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function TestDoNotGenerateIntoHiddenRegion1_WithImports() As Task
            Await TestInRegularAndScriptAsync(
<text>
#ExternalSource ("file", num)
Imports System.Threading
#End ExternalSource

#ExternalSource ("file", num)
Class C
    Sub Goo()
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

    Sub Goo()
        Bar()
#End ExternalSource
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function TestDoNotGenerateIntoHiddenRegion2() As Task
            Await TestInRegularAndScriptAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Goo()
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
    Sub Goo()
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
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function TestDoNotGenerateIntoHiddenRegion3() As Task
            Await TestInRegularAndScriptAsync(
<text>
#ExternalSource ("file", num)
Class C
    Sub Goo()
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
    Sub Goo()
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
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function TestAddressOfInference1() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544641")>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestInRegularAndScriptAsync(
"Class C : End Class
Class B
    Sub Goo()
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
    Sub Goo()
        C.Bar()
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](,,)
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(,,)
    End Sub

    Private Sub goo(Optional value1 As Object = Nothing, Optional value2 As Object = Nothing, Optional value3 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](1,,)
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(1,,)
    End Sub

    Private Sub goo(v As Integer, Optional value1 As Object = Nothing, Optional value2 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments3() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](, 1,)
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(, 1,)
    End Sub

    Private Sub goo(Optional value1 As Object = Nothing, Optional v As Integer = Nothing, Optional value2 As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments4() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](,, 1)
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(,, 1)
    End Sub

    Private Sub goo(Optional value1 As Object = Nothing, Optional value2 As Object = Nothing, Optional v As Integer = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments5() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](1,, 1)
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(1,, 1)
    End Sub

    Private Sub goo(v1 As Integer, Optional value As Object = Nothing, Optional v2 As Integer = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546037")>
        Public Async Function TestOmittedArguments6() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|goo|](1, 1, )
    End Sub
End Module",
"Imports System

Module Program
    Sub Main(args As String())
        goo(1, 1, )
    End Sub

    Private Sub goo(v1 As Integer, v2 As Integer, Optional value As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546683")>
        Public Async Function TestNotOnMissingMethodName() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M()
        Me.[||] 
 End Sub
End Class")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546684")>
        Public Async Function TestGenerateFromEventHandler() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530814")>
        Public Async Function TestCapturedMethodTypeParameterThroughLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Module M
    Sub Goo(Of T, S)(x As List(Of T), y As List(Of S))
        [|Bar|](x, Function() y) ' Generate Bar 
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Module M
    Sub Goo(Of T, S)(x As List(Of T), y As List(Of S))
        Bar(x, Function() y) ' Generate Bar 
    End Sub

    Private Sub Bar(Of T, S)(x As List(Of T), value As Func(Of List(Of S)))
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestTypeParameterAndParameterConflict1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C(Of T)
    Sub Goo(x As T)
        M.[|Bar|](T:=x)
    End Sub
End Class

Module M
End Module",
"Imports System
Class C(Of T)
    Sub Goo(x As T)
        M.Bar(T:=x)
    End Sub
End Class

Module M
    Friend Sub Bar(Of T1)(T As T1)
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530968")>
        Public Async Function TestTypeParameterAndParameterConflict2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C(Of T)
    Sub Goo(x As T)
        M.[|Bar|](t:=x) ' Generate Bar 
    End Sub
End Class

Module M
End Module",
"Imports System
Class C(Of T)
    Sub Goo(x As T)
        M.Bar(t:=x) ' Generate Bar 
    End Sub
End Class

Module M
    Friend Sub Bar(Of T1)(t As T1)
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546850")>
        Public Async Function TestCollectionInitializer1() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub Bar(v As Integer, value() As Integer)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546925")>
        Public Async Function TestCollectionInitializer2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module M
    Sub Main()
        [|Goo|]({{1}})
    End Sub
End Module",
"Imports System
Module M
    Sub Main()
        Goo({{1}})
    End Sub

    Private Sub Goo(value(,) As Integer)
        Throw New NotImplementedException()
    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530818")>
        Public Async Function TestParameterizedProperty1() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530818")>
        Public Async Function TestParameterizedProperty2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        Public Async Function TestGenerateMethodWithLambda_1() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub Baz(value As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz()
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        Public Async Function TestGenerateMethodWithLambda_2() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub Baz(value As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz(one As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907612")>
        Public Async Function TestGenerateMethodWithLambda_3() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub Baz(value As Func(Of String))
        Throw New NotImplementedException()
    End Sub

    Public Sub Baz(one As Func(Of String), two As Integer)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        Public Async Function TestGenerateMethodForDifferentParameterName() As Task
            Await TestInRegularAndScriptAsync(
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
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769760")>
        Public Async Function TestGenerateMethodForSameNamedButGenericUsage_1() As Task
            Await TestInRegularAndScriptAsync(
<text>
Class Program
    Sub Main(args As String())
        Goo()
        [|Goo(Of Integer)|]()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>
Class Program
    Sub Main(args As String())
        Goo()
        Goo(Of Integer)()
    End Sub

    Private Sub Goo(Of T)()
        Throw New System.NotImplementedException()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769760")>
        Public Async Function TestGenerateMethodForSameNamedButGenericUsage_2() As Task
            Await TestInRegularAndScriptAsync(
<text>Imports System
Class Program
    Sub Main(args As String())
        Goo()
        [|Goo(Of Integer, Integer)|]()
    End Sub

    Private Sub Goo(Of T)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Class Program
    Sub Main(args As String())
        Goo()
        Goo(Of Integer, Integer)()
    End Sub

    Private Sub Goo(Of T1, T2)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Goo(Of T)()
        Throw New NotImplementedException()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/935731")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14467")>
        Public Async Function TestGenerateMethodForAwaitWithoutParenthesis() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Async Sub Method_ASub()
        Dim x = [|Await Goo|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Imports System.Threading.Tasks

Module Module1
    Async Sub Method_ASub()
        Dim x = Await Goo
    End Sub

    Private Async Function Goo() As Task(Of Object)
        Throw New NotImplementedException()
    End Function
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodTooManyArgs1() As Task
            Await TestInRegularAndScriptAsync(
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
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNamespaceNotExpression1() As Task
            Await TestInRegularAndScriptAsync(
<text>Imports System
Module M1
    Sub Goo()
        [|Text|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Module M1
    Sub Goo()
        Text
    End Sub

    Private Sub Text()
        Throw New NotImplementedException()
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoArgumentCountOverloadCandidates1() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Class C0
        Public whichOne As String
        Sub Goo(ByVal t1 As String)
            whichOne = "T"
        End Sub
    End Class
    Class C1
        Inherits C0
        Overloads Sub Goo(ByVal y1 As String)
            whichOne = "Y"
        End Sub
    End Class
    Sub test()
        Dim clsNarg2get As C1 = New C1()
        [|clsNarg2get.Goo(1, y1:=2)|]
    End Sub

End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0
        Public whichOne As String
        Sub Goo(ByVal t1 As String)
            whichOne = "T"
        End Sub
    End Class
    Class C1
        Inherits C0
        Overloads Sub Goo(ByVal y1 As String)
            whichOne = "Y"
        End Sub

        Friend Sub Goo(v As Integer, y1 As Integer)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim clsNarg2get As C1 = New C1()
        clsNarg2get.Goo(1, y1:=2)
    End Sub

End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodFunctionResultCannotBeIndexed1() As Task
            Await TestInRegularAndScriptAsync(
<text>Imports Microsoft.VisualBasic.FileSystem
Module M1
    Sub goo()
        If [|FreeFile(1)|] = 255 Then
        End If
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System
Imports Microsoft.VisualBasic.FileSystem
Module M1
    Sub goo()
        If FreeFile(1) = 255 Then
        End If
    End Sub

    Private Function FreeFile(v As Integer) As Integer
        Throw New NotImplementedException()
    End Function
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoCallableOverloadCandidates2() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub sub1(Of T1, T2)(integers() As T1, strings() As T2)
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates2() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Goo(ByVal t1 As T)
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
        Overloads Sub Goo(ByVal y1 As Y)
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
        Call [|tc2.Goo(New Scenario11)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Goo(ByVal t1 As T)
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
        Overloads Sub Goo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property

        Friend Sub Goo(scenario11 As Scenario11)
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
        Call tc2.Goo(New Scenario11)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates3() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Goo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
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
        Call [|tc2.Goo(sc11)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Sub Goo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Goo(sc11 As Scenario11)
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
        Call tc2.Goo(sc11)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates4() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Goo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
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
        Call [|tc3.Goo(dTmp)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Goo(ByVal t1 As T)
        End Sub
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
        End Sub
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Goo(dTmp As Decimal)
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
        Call tc3.Goo(dTmp)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodArgumentNarrowing() As Task
            Await TestInRegularAndScriptAsync(
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Goo(sample7C1(Of Long).E.e1)|]
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub

        Friend Sub Goo(e1 As sample7C1(Of Long).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Goo(sample7C1(Of Long).E.e1)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodArgumentNarrowing2() As Task
            Await TestInRegularAndScriptAsync(
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Goo(sample7C1(Of Short).E.e2)|]
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub

        Friend Sub Goo(e2 As sample7C1(Of Short).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Goo(sample7C1(Of Short).E.e2)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodArgumentNarrowing3() As Task
            Await TestInRegularAndScriptAsync(
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call [|tc7.Goo(sc7.E.e3)|]
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
        Sub Goo(ByVal p1 As sample7C1(Of T).E)
            whichOne = "1"
        End Sub
        Sub Goo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
        End Sub
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Goo(p1)
        End Sub

        Friend Sub Goo(e3 As sample7C1(Of Byte).E)
            Throw New NotImplementedException()
        End Sub
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Goo(sc7.E.e3)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939941")>
        Public Async Function TestGenerateMethodNoMostSpecificOverload2() As Task
            Await TestInRegularAndScriptAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Goo(ByVal t1 As T)
        End Sub
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
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
        Call [|C.Goo(New C2)|]
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf),
<text>Imports System

Module Module1
    Class C0(Of T)
        Sub Goo(ByVal t1 As T)
        End Sub
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Goo(ByVal y1 As Y)
        End Sub

        Friend Sub Goo(c2 As C2)
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
        Call C.Goo(New C2)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        Public Async Function TestGenerateMethodInsideNameOf() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        Public Async Function TestGenerateMethodInsideNameOf2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestGenerateMethodWithNameOfArgument() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestGenerateMethodWithLambdaAndNameOfArgument() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Sub M2(value As Func(Of String))
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis3() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis4() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis5() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis6() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis7() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis8() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis9() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis10() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis11() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccess() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccess2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccess3() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccess4() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGeneratePropertyConditionalAccess() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGeneratePropertyConditionalAccess2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGeneratePropertyConditionalAccess3() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGeneratePropertyConditionalAccess4() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/39001")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        Public Async Function TestGenerateMethodConditionalAccess5() As Task
            Await TestInRegularAndScriptAsync(
"Public Structure C
    Sub Main(a As C?)
        Dim x As Integer? = a?[|.B|]()
    End Sub
End Structure",
"Imports System

Public Structure C
    Sub Main(a As C?)
        Dim x As Integer? = a?.B()
    End Sub

    Private Function B() As Integer
        Throw New NotImplementedException()
    End Function
End Structure")
        End Function

        <Fact>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestGenerateMethodTypeOf() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        Public Async Function TestGenerateMethodTypeOf2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/643")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14467")>
        Public Async Function TestGenerateMethodConfigureAwaitFalse() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await [|Goo|]().ConfigureAwait(False)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await Goo().ConfigureAwait(False)
    End Sub

    Private Async Function Goo() As Task(Of Boolean)
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/643")>
        Public Async Function TestGeneratePropertyConfigureAwaitFalse() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await [|Goo|]().ConfigureAwait(False)
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Module Program
    Async Sub Main(args As String())
        Dim x As Boolean = Await Goo().ConfigureAwait(False)
    End Sub

    Private ReadOnly Property Goo As Task(Of Boolean)
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Module",
index:=1)
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/643")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/14467")>
        Public Async Function TestGenerateMethodWithMethodChaining() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Async Function F() As Task(Of Boolean)
        Throw New NotImplementedException()
    End Function
End Module")
        End Function

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        Public Async Function TestGenerateMethodInTypeOfIsNot() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        Public Async Function TestInCollectionInitializers1() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529480")>
        Public Async Function TestInCollectionInitializers2() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10004")>
        Public Async Function TestGenerateMethodWithMultipleOfSameGenericType() As Task
            Await TestInRegularAndScriptAsync(
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

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11461")>
        Public Async Function TestGenerateMethodOffOfExistingProperty() As Task
            Await TestInRegularAndScriptAsync(
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
End Class
</text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <Fact>
        Public Async Function MethodWithTuple() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Shared Function NewMethod(value As (Integer, String)) As (Integer, String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18969")>
        Public Async Function TupleElement1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Public Class Q
    Sub Main()
        Dim x As (Integer, String) = ([|Goo|](), """")
    End Sub
End Class
",
"
Imports System

Public Class Q
    Sub Main()
        Dim x As (Integer, String) = (Goo(), """")
    End Sub

    Private Function Goo() As Integer
        Throw New NotImplementedException()
    End Function
End Class
")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18969")>
        Public Async Function TupleElement2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Public Class Q
    Sub Main()
        Dim x As (Integer, String) = (0, [|Goo|]())
    End Sub
End Class
",
"
Imports System

Public Class Q
    Sub Main()
        Dim x As (Integer, String) = (0, Goo())
    End Sub

    Private Function Goo() As String
        Throw New NotImplementedException()
    End Function
End Class
")
        End Function

        <Fact>
        Public Async Function MethodWithTupleWithNames() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Shared Function NewMethod(value As (c As Integer, d As String)) As (a As Integer, b As String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function MethodWithTupleWithOneName() As Task
            Await TestInRegularAndScriptAsync(
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

    Private Shared Function NewMethod(value As (c As Integer, String)) As (a As Integer, String)
        Throw New NotImplementedException()
    End Function
End Class")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub
End Class

Enum Goo
    One
End Enum",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Enum Goo
    One
End Enum")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub
End Class

Delegate Sub Goo()",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Delegate Sub Goo()")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub

End Class

Class Goo
    
End Class",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Class Goo
    
End Class")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub
End Class

Structure Goo

End Structure",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Structure Goo

End Structure")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName5() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub
End Class

Interface Goo
    
End Interface",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Interface Goo
    
End Interface")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/16975")>
        Public Async Function TestWithSameMethodNameAsTypeName6() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub Bar()
        [|Goo|]()
    End Sub
End Class

Namespace Goo

End Namespace",
"Imports System
Class C
    Sub Bar()
        Goo()
    End Sub

    Private Sub Goo()
        Throw New NotImplementedException()
    End Sub
End Class

Namespace Goo

End Namespace")
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/61542")>
        Public Async Function TestAcrossFiles() As Task
            Await TestInRegularAndScriptAsync(
"<Workspace>
    <Project Language=""Visual Basic"" CommonReferences=""true"">
        <Document>
Public Class DataContainer
    Property PossibleInProcessTests As string
    Property PossibleEndProcessTests As string
    Property Mixtures As string
    Property Customers As string
    Property Synonyms As string
    Property Ingredients As string
    Property Preservatives As string
    Property TeamMembers As string
    Property Vessels As string

    Sub Goo()
    End Sub

    Sub Bar()
    End Sub

    Function Bazz() As Object
        Return Nothing
    End Function

End Class</Document>
        <Document>
Public Class FileContainer
    Sub S()
        Dim DC As New DataContainer
         ' importantly, we don't want use the position of 'S' to determine where in Doc1 we generate this method. 
        DC.[|ArbitraryPositionMethod|]()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>",
"
Public Class DataContainer
    Property PossibleInProcessTests As string
    Property PossibleEndProcessTests As string
    Property Mixtures As string
    Property Customers As string
    Property Synonyms As string
    Property Ingredients As string
    Property Preservatives As string
    Property TeamMembers As string
    Property Vessels As string

    Sub Goo()
    End Sub

    Sub Bar()
    End Sub

    Friend Sub ArbitraryPositionMethod()
        Throw New NotImplementedException()
    End Sub

    Function Bazz() As Object
        Return Nothing
    End Function

End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64631")>
        Public Async Function TestMethodReference1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function OtherMethod() As Integer
        return 0
    End Function

    Sub M()
        [|Goo|](OtherMethod)
    End Sub
End Class",
"Imports System

Class C
    Function OtherMethod() As Integer
        return 0
    End Function

    Sub M()
        Goo(OtherMethod)
    End Sub

    Private Sub Goo(otherMethod As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64631")>
        Public Async Function TestMethodReference2() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function OtherMethod() As Integer
        return 0
    End Function

    Sub M()
        [|Goo|](AddressOf OtherMethod)
    End Sub
End Class",
"Imports System

Class C
    Function OtherMethod() As Integer
        return 0
    End Function

    Sub M()
        Goo(AddressOf OtherMethod)
    End Sub

    Private Sub Goo(value As Func(Of Integer))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64631")>
        Public Async Function TestMethodReference3() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function OtherMethod(x as Integer) As Integer
        return 0
    End Function

    Sub M()
        [|Goo|](OtherMethod)
    End Sub
End Class",
"Imports System

Class C
    Function OtherMethod(x as Integer) As Integer
        return 0
    End Function

    Sub M()
        Goo(OtherMethod)
    End Sub

    Private Sub Goo(otherMethod As Func(Of Integer, Integer))
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64631")>
        Public Async Function TestMethodReference4() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function OtherMethod(optional x as Integer = 0) As Integer
        return 0
    End Function

    Sub M()
        [|Goo|](OtherMethod)
    End Sub
End Class",
"Imports System

Class C
    Function OtherMethod(optional x as Integer = 0) As Integer
        return 0
    End Function

    Sub M()
        Goo(OtherMethod)
    End Sub

    Private Sub Goo(otherMethod As Integer)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64631")>
        Public Async Function TestMethodReference5() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub OtherMethod()
        return 0
    End Sub

    Sub M()
        [|Goo|](OtherMethod)
    End Sub
End Class",
"Imports System

Class C
    Sub OtherMethod()
        return 0
    End Sub

    Sub M()
        Goo(OtherMethod)
    End Sub

    Private Sub Goo(otherMethod As Object)
        Throw New NotImplementedException()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47153")>
        Public Async Function TestSingleLineIf() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub X()
        If [|Goo()|] Then Bar()
    End Sub

    Sub Bar()
    End Sub
End Module
",
"Imports System

Module Program
    Sub X()
        If Goo() Then Bar()
    End Sub

    Private Function Goo() As Boolean
        Throw New NotImplementedException()
    End Function

    Sub Bar()
    End Sub
End Module
")
        End Function
    End Class
End Namespace
