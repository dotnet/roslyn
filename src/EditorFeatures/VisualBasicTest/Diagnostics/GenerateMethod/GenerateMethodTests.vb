' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateMethod
    Public Class GenerateMethodTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateParameterizedMemberCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationIntoSameType() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationOffOfMe() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n Me.[|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Me.Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationOffOfType() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n C.[|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n C.Foo() \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationValueExpressionArg() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|](0) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(0) \n End Sub \n Private Sub Foo(v As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationMultipleValueExpressionArg() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|](0, 0) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(0, 0) \n End Sub \n Private Sub Foo(v1 As Integer, v2 As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationValueArg() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](i) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(i) \n End Sub \n Private Sub Foo(i As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestSimpleInvocationNamedValueArg() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](bar:= i) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(bar:= i) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateAfterMethod() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|]() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInterfaceNaming() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod()) \n End Sub \n Function NextMethod() As IFoo \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod()) \n End Sub \n Private Sub Foo(foo As IFoo) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod() As IFoo \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestFuncArg0() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Function NextMethod() As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As String) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod() As String \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestFuncArg1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Func(Of Integer, String)) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"))
        End Function

        <WpfFact(Skip:="528229"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestAddressOf1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](AddressOf NextMethod) \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(AddressOf NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Global.System.Func(Of Integer, String)) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestActionArg() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) End Sub \n Sub NextMethod() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Object) \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestActionArg1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Sub NextMethod(i As Integer) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Action(Of Integer)) \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod(i As Integer) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeInference() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n If [|Foo|]() \n End If \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n If Foo() \n End If \n End Sub \n Private Function Foo() As Boolean \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMemberAccessArgumentName() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|](Me.Bar) \n End Sub \n Dim Bar As Integer \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Me.Bar) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Bar As Integer \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParenthesizedArgumentName() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|]((Bar)) \n End Sub \n Dim Bar As Integer \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo((Bar)) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Bar As Integer \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCastedArgumentName() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|](DirectCast(Me.Baz, Bar)) \n End Sub \n End Class \n Class Bar \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(DirectCast(Me.Baz, Bar)) \n End Sub \n Private Sub Foo(baz As Bar) \n Throw New NotImplementedException() \n End Sub \n End Class \n Class Bar \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDuplicateNames() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo|](DirectCast(Me.Baz, Bar), Me.Baz) \n End Sub \n Dim Baz As Integer \n End Class \n Class Bar \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(DirectCast(Me.Baz, Bar), Me.Baz) \n End Sub \n Private Sub Foo(baz1 As Bar, baz2 As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Baz As Integer \n End Class \n Class Bar \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgs1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo(Of Integer)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Of Integer)() \n End Sub \n Private Sub Foo(Of T)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgs2() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|Foo(Of Integer, String)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Of Integer, String)() \n End Sub \n Private Sub Foo(Of T1,T2)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539984)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgsFromMethod() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(Of X,Y)(x As X, y As Y) \n [|Foo|](x) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X,Y)(x As X, y As Y) \n Foo(x) \n End Sub \n Private Sub Foo(Of X)(x1 As X) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenericArgThatIsTypeParameter() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(Of X)(y1 As X(), x1 As System.Func(Of X)) \n [|Foo(Of X)|](y1, x1) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X)(y1 As X(), x1 As System.Func(Of X)) \n Foo(Of X)(y1, x1) \n End Sub \n Private Sub Foo(Of X)(y1() As X, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsThatAreTypeParameters() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X)) \n [|Foo(Of X, Y)|](y1, x1) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X)) \n Foo(Of X, Y)(y1, x1) \n End Sub \n Private Sub Foo(Of X, Y)(y1() As Y, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539984)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsFromMethod() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(Of X, Y)(x As X, y As Y) \n [|Foo|](x, y) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(x As X, y As Y) \n Foo(x, y) \n End Sub \n Private Sub Foo(Of X, Y)(x1 As X, y1 As Y) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539984)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleGenericArgsFromMethod2() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(Of X, Y)(y As Y(), x As System.Func(Of X)) \n [|Foo|](y, x) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(y As Y(), x As System.Func(Of X)) \n Foo(y, x) \n End Sub \n Private Sub Foo(Of Y, X)(y1() As Y, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoOuterThroughInstance() As Task
            Await TestAsync(
NewLines("Class Outer \n Class C \n Sub M(o As Outer) \n o.[|Foo|]() \n End Sub \n End Class \n End Class"),
NewLines("Imports System \n Class Outer \n Class C \n Sub M(o As Outer) \n o.Foo() \n End Sub \n End Class \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoOuterThroughClass() As Task
            Await TestAsync(
NewLines("Class Outer \n Class C \n Sub M(o As Outer) \n Outer.[|Foo|]() \n End Sub \n End Class \n End Class"),
NewLines("Imports System \n Class Outer \n Class C \n Sub M(o As Outer) \n Outer.Foo() \n End Sub \n End Class Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoSiblingThroughInstance() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(s As Sibling) \n s.[|Foo|]() \n End Sub \n End Class \n Class Sibling \n End Class"),
NewLines("Imports System \n Class C \n Sub M(s As Sibling) \n s.Foo() \n End Sub \n End Class \n Class Sibling \n Friend Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoSiblingThroughClass() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(s As Sibling) \n [|Sibling.Foo|]() \n End Sub \n End Class \n Class Sibling \n End Class"),
NewLines("Imports System \n Class C \n Sub M(s As Sibling) \n Sibling.Foo() \n End Sub \n End Class \n Class Sibling \n Friend Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoInterfaceThroughInstance() As Task
            Await TestAsync(
NewLines("Class C \n Sub M(s As ISibling) \n s.[|Foo|]() \n End Sub \n End Class \n Interface ISibling \n End Interface"),
NewLines("Class C \n Sub M(s As ISibling) \n s.Foo() \n End Sub \n End Class \n Interface ISibling \n Sub Foo() \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateAbstractIntoSameType() As Task
            Await TestAsync(
NewLines("MustInherit Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("MustInherit Class C \n Sub M() \n Foo() \n End Sub \n Friend MustOverride Sub Foo() \n End Class"),
index:=1)
        End Function

        <WorkItem(539297)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateIntoModule() As Task
            Await TestAsync(
NewLines("Module Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Module"),
NewLines("Imports System \n Module Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() End Sub \n End Module"))
        End Function

        <WorkItem(539506)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInference1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n Do While [|Foo|]() \n Loop \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Do While Foo() \n Loop \n End Sub \n Private Function Foo() As Boolean \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539505)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestEscaping1() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|[Sub]|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n [Sub]() \n End Sub \n Private Sub [Sub]() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539504)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestExplicitCall() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n Call [|S|] \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Call S \n End Sub \n Private Sub S() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539504)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestImplicitCall() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S|] \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S \n End Sub \n Private Sub S() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539537)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArrayAccess1() As Task
            Await TestMissingAsync(NewLines("Class C \n Sub M(x As Integer()) \n Foo([|x|](4)) \n End Sub \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterInteger() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S%|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S%() \n End Sub \n Private Function S() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterLong() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S&|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S&() \n End Sub \n Private Function S() As Long \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterDecimal() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S@|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S@() \n End Sub \n Private Function S() As Decimal \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterSingle() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S!|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S!() \n End Sub \n Private Function S() As Single \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterDouble() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S#|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S#() \n End Sub \n Private Function S() As Double \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeCharacterString() As Task
            Await TestAsync(
NewLines("Class C \n Sub M() \n [|S$|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S$() \n End Sub \n Private Function S() As String \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(539283)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(539283)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArgumentTypeVoid() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main() \n Dim v As Void \n [|Foo|](v) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Dim v As Void \n Foo(v) \n End Sub \n Private Sub Foo(v As Object) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateFromImplementsClause() As Task
            Await TestAsync(
NewLines("Class Program \n Implements IFoo \n Public Function Bip(i As Integer) As String Implements [|IFoo.Snarf|] \n End Function \n End Class \n Interface IFoo \n End Interface"),
NewLines("Class Program \n Implements IFoo \n Public Function Bip(i As Integer) As String Implements IFoo.Snarf \n End Function \n End Class \n Interface IFoo \n Function Snarf(i As Integer) As String \n End Interface"))
        End Function

        <WorkItem(537929)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInScript1() As Task
            Await TestAsync(
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub"),
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub"),
            parseOptions:=GetScriptOptions())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInTopLevelImplicitClass1() As Task
            Await TestAsync(
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub"),
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInNamespaceImplicitClass1() As Task
            Await TestAsync(
NewLines("Imports System \n Namespace N \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub \n End Namespace"),
NewLines("Imports System \n Namespace N \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInNamespaceImplicitClass_FieldInitializer() As Task
            Await TestAsync(
NewLines("Imports System \n Namespace N \n Dim a As Integer = [|Foo|]() \n End Namespace"),
NewLines("Imports System \n Namespace N \n Dim a As Integer = Foo() \n Private Function Foo() As Integer \n Throw New NotImplementedException() \n End Function \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod1() As Task
            Await TestMissingAsync(
NewLines("Class Program \n Implements IFoo \n Public Function Blah() As String Implements [|IFoo.Blah|] \n End Function \n End Class \n Interface IFoo \n Sub Blah() \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod2() As Task
            Await TestMissingAsync(
NewLines("Class Program \n Implements IFoo \n Public Function Blah() As String Implements [|IFoo.Blah|] \n End Function \n End Class \n Interface IFoo \n Sub Blah() \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod3() As Task
            Await TestAsync(
NewLines("Class C \n Implements IFoo \n Sub Snarf() Implements [|IFoo.Blah|] \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Snarf() Implements IFoo.Blah \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n Sub Blah() \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod4() As Task
            Await TestAsync(
NewLines("Class C \n Implements IFoo \n Sub Snarf(i As String) Implements [|IFoo.Blah|] \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Snarf(i As String) Implements IFoo.Blah \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n Sub Blah(i As String) \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod5() As Task
            Await TestAsync(
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer) Implements [|IFoo.Snarf|] \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As String) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer) Implements IFoo.Snarf \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As String) \n Sub Snarf(i As Integer) \n End Interface"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClashesWithMethod6() As Task
            Await TestAsync(
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer, s As String) Implements [|IFoo.Snarf|] \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As Integer, b As Boolean) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer, s As String) Implements IFoo.Snarf \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As Integer, b As Boolean) \n Sub Snarf(i As Integer, s As String) \n End Interface"))
        End Function

        <WorkItem(539708)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNoStaticGenerationIntoInterface() As Task
            Await TestMissingAsync(
NewLines("Interface IFoo \n End Interface \n Class Program \n Sub Main \n IFoo.[|Bar|] \n End Sub \n End Class"))
        End Function

        <WorkItem(539821)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestEscapeParametername() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim [string] As String = ""hello"" \n [|[Me]|]([string]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim [string] As String = ""hello"" \n [Me]([string]) \n End Sub \n Private Sub [Me]([string] As String) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(539810)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotUseUnavailableTypeParameter() As Task
            Await TestAsync(
NewLines("Class Test \n Sub M(Of T)(x As T) \n [|Foo(Of Integer)|](x) \n End Sub \n End Class"),
NewLines("Imports System \n Class Test \n Sub M(Of T)(x As T) \n Foo(Of Integer)(x) \n End Sub \n Private Sub Foo(Of T)(x As T) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539808)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDoNotUseTypeParametersFromContainingType() As Task
            Await TestAsync(
NewLines("Class Test(Of T) \n Sub M() \n [|Method(Of T)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class Test(Of T) \n Sub M() \n Method(Of T)() \n End Sub \n Private Sub Method(Of T1)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNameSimplification1() As Task
            Await TestAsync(
NewLines("Imports System \n Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(539809)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(540013)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInAddressOfExpression1() As Task
            Await TestAsync(
NewLines("Delegate Sub D(x As Integer) \n Class C \n Public Sub Foo() \n Dim x As D = New D(AddressOf [|Method|]) \n End Sub \n End Class"),
NewLines("Imports System \n Delegate Sub D(x As Integer) \n Class C \n Public Sub Foo() \n Dim x As D = New D(AddressOf Method) \n End Sub \n Private Sub Method(x As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(527986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOfferedForInferredGenericMethodArgs() As Task
            Await TestMissingAsync(
NewLines("Class Foo(Of T) \n Sub Main(Of T, X)(k As Foo(Of T)) \n [|Bar|](k) \n End Sub \n Private Sub Bar(Of T)(k As Foo(Of T)) \n End Sub \n End Class"))
        End Function

        <WorkItem(540740)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestDelegateInAsClause() As Task
            Await TestAsync(
NewLines("Delegate Sub D(x As Integer) \n Class C \n Private Sub M() \n Dim d As New D(AddressOf [|Test|]) \n End Sub \n End Class"),
NewLines("Imports System \n Delegate Sub D(x As Integer) \n Class C \n Private Sub M() \n Dim d As New D(AddressOf Test) \n End Sub \n Private Sub Test(x As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(541405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMissingOnImplementedInterfaceMethod() As Task
            Await TestMissingAsync(
NewLines("Class C(Of U) \n Implements ITest \n Public Sub Method(x As U) Implements [|ITest.Method|] \n End Sub \n End Class \n Friend Interface ITest \n Sub Method(x As Object) \n End Interface"))
        End Function

        <WorkItem(542098)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOnConstructorInitializer() As Task
            Await TestMissingAsync(
NewLines("Class C \n Sub New \n Me.[|New|](1) \n End Sub \n End Class"))
        End Function

        <WorkItem(542838)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestMultipleImportsAdded() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In [|HERE|]() : Next \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n For Each v As Integer In HERE() : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Integer) \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(543007)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCompilationMemberImports() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In [|HERE|]() : Next \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In HERE() : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Integer) \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Function

        <WorkItem(531301)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestForEachWithNoControlVariableType() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v In [|HERE|] : Next \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For Each v In HERE : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Object) \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Function

        <WorkItem(531301)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestElseIfStatement() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n If x Then \n ElseIf [|HERE|] Then \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n If x Then \n ElseIf HERE Then \n End If \n End Sub \n Private Function HERE() As Boolean \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Function

        <WorkItem(531301)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestForStatement() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n For x As Integer = 1 To [|HERE|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For x As Integer = 1 To HERE \n End Sub \n Private Function HERE() As Integer \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Function

        <WorkItem(543216)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestArrayOfAnonymousTypes() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim product = New With {Key .Name = """", Key .Price = 0} \n Dim products = ToList(product) \n [|HERE|](products) \n End Sub \n Function ToList(Of T)(a As T) As IEnumerable(Of T) \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim product = New With {Key .Name = """", Key .Price = 0} \n Dim products = ToList(product) \n HERE(products) \n End Sub \n Private Sub HERE(products As IEnumerable(Of Object)) \n Throw New NotImplementedException() \n End Sub \n Function ToList(Of T)(a As T) As IEnumerable(Of T) \n Return Nothing \n End Function \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestAddressOfInference1() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main(ByVal args As String()) \n Dim v As Func(Of String) = Nothing \n Dim a1 = If(False, v, AddressOf [|TestMethod|]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(ByVal args As String()) \n Dim v As Func(Of String) = Nothing \n Dim a1 = If(False, v, AddressOf TestMethod) \n End Sub \n Private Function TestMethod() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(544641)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestAsync(
NewLines("Class C : End Class \n Class B \n Sub Foo() \n C.[|Bar|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Friend Shared Sub Bar() \n Throw New NotImplementedException() \n End Sub \n End Class \n Class B \n Sub Foo() \n C.Bar() \n End Sub \n End Class"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,,) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional p3 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1,,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1,,) \n End Sub \n Private Sub foo(v As Integer, Optional p1 As Object = Nothing, Optional p2 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments3() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,1,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,1,) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional v As Integer = Nothing, Optional p2 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments4() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,,1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,,1) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional v As Integer = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments5() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1,, 1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1,, 1) \n End Sub \n Private Sub foo(v1 As Integer, Optional p As Object = Nothing, Optional v2 As Integer = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546037)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestOmittedArguments6() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1, 1, ) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1, 1, ) \n End Sub \n Private Sub foo(v1 As Integer, v2 As Integer, Optional p As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546683)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestNotOnMissingMethodName() As Task
            Await TestMissingAsync(NewLines("Class C \n Sub M() \n Me.[||] \n End Sub \n End Class"))
        End Function

        <WorkItem(546684)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateFromEventHandler() As Task
            Await TestAsync(
NewLines("Module Module1 \n Sub Main() \n Dim c1 As New Class1 \n AddHandler c1.AnEvent, AddressOf [|EventHandler1|] \n End Sub \n Public Class Class1 \n Public Event AnEvent() \n End Class \n End Module"),
NewLines("Imports System \n Module Module1 \n Sub Main() \n Dim c1 As New Class1 \n AddHandler c1.AnEvent, AddressOf EventHandler1 \n End Sub \n Private Sub EventHandler1() \n Throw New NotImplementedException() \n End Sub \n Public Class Class1 \n Public Event AnEvent() \n End Class \n End Module"))
        End Function

        <WorkItem(530814)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCapturedMethodTypeParameterThroughLambda() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Module M \n Sub Foo(Of T, S)(x As List(Of T), y As List(Of S)) \n [|Bar|](x, Function() y) ' Generate Bar \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module M \n Sub Foo(Of T, S)(x As List(Of T), y As List(Of S)) \n Bar(x, Function() y) ' Generate Bar \n End Sub \n Private Sub Bar(Of T, S)(x As List(Of T), p As Func(Of List(Of S))) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeParameterAndParameterConflict1() As Task
            Await TestAsync(
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.[|Bar|](T:=x) \n End Sub \n End Class \n  \n Module M \n End Module"),
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.Bar(T:=x) \n End Sub \n End Class \n  \n Module M \n Friend Sub Bar(Of T1)(T As T1) \n End Sub \n End Module"))
        End Function

        <WorkItem(530968)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestTypeParameterAndParameterConflict2() As Task
            Await TestAsync(
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.[|Bar|](t:=x) ' Generate Bar \n End Sub \n End Class \n  \n Module M \n End Module"),
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.Bar(t:=x) ' Generate Bar \n End Sub \n End Class \n  \n Module M \n Friend Sub Bar(Of T1)(t As T1) \n End Sub \n End Module"))
        End Function

        <WorkItem(546850)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCollectionInitializer1() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n [|Bar|](1, {1}) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Bar(1, {1}) \n End Sub \n Private Sub Bar(v As Integer, p() As Integer) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(546925)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestCollectionInitializer2() As Task
            Await TestAsync(
NewLines("Imports System \n Module M \n Sub Main() \n [|Foo|]({{1}}) \n End Sub \n End Module"),
NewLines("Imports System \n Module M \n Sub Main() \n Foo({{1}}) \n End Sub \n Private Sub Foo(p(,) As Integer) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Function

        <WorkItem(530818)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParameterizedProperty1() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main() \n [|Prop|](1) = 2 \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Prop(1) = 2 \n End Sub \n Private Function Prop(v As Integer) As Integer \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(530818)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestParameterizedProperty2() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main() \n [|Prop|](1) = 2 \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Prop(1) = 2 \n End Sub \n Private Property Prop(v As Integer) As Integer \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Module"),
index:=1)
        End Function

        <WorkItem(907612)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(907612)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(907612)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(889349)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(769760)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(769760)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(935731)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoArgumentCountOverloadCandidates1() As Task
            Await TestAsync(
<text>Module Module1
    Class C0
        Public whichOne As String
        Sub Foo(ByVal t1 As String)
            whichOne = "T"
            End Function
    End Class
    Class C1
        Inherits C0
        Overloads Sub Foo(ByVal y1 As String)
            whichOne = "Y"
            End Function
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
            End Function
    End Class
    Class C1
        Inherits C0
        Overloads Sub Foo(ByVal y1 As String)
            whichOne = "Y"
            End Function

        Friend Sub Foo(v As Integer, y1 As Integer)
            Throw New NotImplementedException()
            End Function
    End Class
    Sub test()
        Dim clsNarg2get As C1 = New C1()
        clsNarg2get.Foo(1, y1:=2)
    End Sub

End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

    Private Sub sub1(Of T1, T2)(v1() As Integer, v2() As String)
        Throw New NotImplementedException()
    End Sub
End Class
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates2() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
            End Function
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
            End Function
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
            End Function
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
            End Function
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
            Get
            End Get
            Set(ByVal Value As Integer)
            End Set
        End Property

        Friend Sub Foo(scenario11 As Scenario11)
            Throw New NotImplementedException()
            End Function
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates3() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
            End Function
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function
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
            End Function
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Foo(sc11 As Scenario11)
            Throw New NotImplementedException()
            End Function
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoNonNarrowingOverloadCandidates4() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Public whichOne As String
        Sub Foo(ByVal t1 As T)
            End Function
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function
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
            End Function
        Default Property Prop1(ByVal t1 As T) As Integer
        End Property
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function
        Default Overloads Property Prop1(ByVal y1 As Y) As Integer
        End Property

        Friend Sub Foo(dTmp As Decimal)
            Throw New NotImplementedException()
            End Function
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

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function

        Friend Sub Foo(e1 As sample7C1(Of Long).E)
            Throw New NotImplementedException()
            End Function
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sample7C1(Of Long).E.e1)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function

        Friend Sub Foo(e2 As sample7C1(Of Short).E)
            Throw New NotImplementedException()
            End Function
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sample7C1(Of Short).E.e2)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function
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
            End Function
        Sub Foo(ByVal p1 As sample7C1(Of Y).E)
            whichOne = "2"
            End Function
        Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
            Call Me.Foo(p1)
            End Function

        Friend Sub Foo(e3 As sample7C1(Of Byte).E)
            Throw New NotImplementedException()
            End Function
    End Class
    Sub test()
        Dim tc7 As New sample7C2(Of Integer, Integer)
        Dim sc7 As New sample7C1(Of Byte)
        Call tc7.Foo(sc7.E.e3)
    End Sub
End Module
</text>.Value.Replace(vbLf, vbCrLf), compareTokens:=False)
        End Function

        <WorkItem(939941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodNoMostSpecificOverload2() As Task
            Await TestAsync(
<text>Module Module1
    Class C0(Of T)
        Sub Foo(ByVal t1 As T)
            End Function
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function
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
            End Function
    End Class
    Class C1(Of T, Y)
        Inherits C0(Of T)
        Overloads Sub Foo(ByVal y1 As Y)
            End Function

        Friend Sub Foo(c2 As C2)
            Throw New NotImplementedException()
            End Function
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

        <WorkItem(1032176)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(1032176)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B \n End Sub \n Private Function B() As Object \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B \n End Sub \n Private Function B() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis5() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Option Strict On \n Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis6() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis7() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Object \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis8() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As C \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis9() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis10() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccessNoParenthesis11() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Object \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B() \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B() \n End Sub \n Private Function B() As Object \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B() \n End Sub \n Private Function B() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalAccess4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B() \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B() \n End Sub \n Private ReadOnly Property B As C \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B() \n End Sub \n Private ReadOnly Property B As Object \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B() \n End Sub \n Private ReadOnly Property B As Integer \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConditionalAccess4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B() \n End Sub \n Private ReadOnly Property B As C \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer() As Task
            Await TestAsync(
NewLines("Module Program \n Property a As Integer = [|y|] \n End Module"),
NewLines("Imports System\n\nModule Program\nProperty a As Integer = y\n\nPrivate Function y() As Integer\nThrow New NotImplementedException()\nEnd Function\nEnd Module"))
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConditionalInPropertyInitializer2() As Task
            Await TestAsync(
NewLines("Module Program \n Property a As Integer = [|y|]() \n End Module"),
NewLines("Imports System\n\nModule Program\nProperty a As Integer = y()\n\n Private Function y() As Integer\nThrow New NotImplementedException()\nEnd Function\nEnd Module"))
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodTypeOf() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Module C \n Sub Test() \n If TypeOf B Is String Then \n End If \n End Sub \n Private Function B() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodTypeOf2() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|]() Is String Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Module C \n Sub Test() \n If TypeOf B() Is String Then \n End If \n End Sub \n Private Function B() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodConfigureAwaitFalse() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Async Sub Main(args As String()) \n Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Collections.Generic\nImports System.Linq\nImports System.Threading.Tasks\n\nModule Program\n    Async Sub Main(args As String())\n        Dim x As Boolean = Await Foo().ConfigureAwait(False)\n    End Sub\n\n    Private Function Foo() As Task(Of Boolean)\n        Throw New NotImplementedException()\n    End Function\nEnd Module"))
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyConfigureAwaitFalse() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Async Sub Main(args As String()) \n Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Collections.Generic\nImports System.Linq\nImports System.Threading.Tasks\n\nModule Program\n    Async Sub Main(args As String())\n        Dim x As Boolean = Await Foo().ConfigureAwait(False)\n    End Sub\n\n    Private ReadOnly Property Foo As Task(Of Boolean)\n        Get\n            Throw New NotImplementedException()\n        End Get\n    End Property\nEnd Module"),
index:=1)
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithMethodChaining() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Linq \n Module M \n Async Sub T() \n Dim x As Boolean = Await [|F|]().ContinueWith(Function(a) True).ContinueWith(Function(a) False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Linq\nImports System.Threading.Tasks\n\nModule M\n    Async Sub T()\n        Dim x As Boolean = Await F().ContinueWith(Function(a) True).ContinueWith(Function(a) False)\n    End Sub\n\n    Private Function F() As Task(Of Boolean)\n        Throw New NotImplementedException()\n    End Function\nEnd Module"))
        End Function

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodWithMethodChaining2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Linq \n Module M \n Async Sub T() \n Dim x As Boolean = Await [|F|]().ContinueWith(Function(a) True).ContinueWith(Function(a) False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Linq\nImports System.Threading.Tasks\n\nModule M\n    Async Sub T()\n        Dim x As Boolean = Await F().ContinueWith(Function(a) True).ContinueWith(Function(a) False)\n    End Sub\n\n    Private ReadOnly Property F As Task(Of Boolean)\n        Get\n            Throw New NotImplementedException()\n        End Get\n    End Property\nEnd Module"),
index:=1)
        End Function

        <WorkItem(1130960)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGenerateMethodInTypeOfIsNot() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n Private Function Prop() As TypeOfIsNotDerived \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(529480)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInCollectionInitializers1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New List ( Of Integer ) From { [|T|]() } \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New List ( Of Integer ) From { T() } \n End Sub \n Private Function T() As Integer \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        <WorkItem(529480)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestInCollectionInitializers2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New Dictionary ( Of Integer , Boolean ) From { { 1, [|T|]() } } \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New Dictionary ( Of Integer , Boolean ) From { { 1, T() } } \n End Sub \n Private Function T() As Boolean \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Function

        Public Class GenerateConversionTests
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateConversionCodeFixProvider())
            End Function

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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

            <WorkItem(774321)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
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
