' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
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
        Public Sub TestSimpleInvocationIntoSameType()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationOffOfMe()
            Test(
NewLines("Class C \n Sub M() \n Me.[|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Me.Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationOffOfType()
            Test(
NewLines("Class C \n Sub M() \n C.[|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n C.Foo() \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationValueExpressionArg()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|](0) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(0) \n End Sub \n Private Sub Foo(v As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationMultipleValueExpressionArg()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|](0, 0) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(0, 0) \n End Sub \n Private Sub Foo(v1 As Integer, v2 As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationValueArg()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](i) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(i) \n End Sub \n Private Sub Foo(i As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestSimpleInvocationNamedValueArg()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](bar:= i) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(bar:= i) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateAfterMethod()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|]() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInterfaceNaming()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod()) \n End Sub \n Function NextMethod() As IFoo \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod()) \n End Sub \n Private Sub Foo(foo As IFoo) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod() As IFoo \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestFuncArg0()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Function NextMethod() As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As String) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod() As String \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestFuncArg1()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Func(Of Integer, String)) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"))
        End Sub

        <Fact(Skip:="528229"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestAddressOf1()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](AddressOf NextMethod) \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(AddressOf NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Global.System.Func(Of Integer, String)) \n Throw New NotImplementedException() \n End Sub \n Function NextMethod(i As Integer) As String \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestActionArg()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) End Sub \n Sub NextMethod() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Object) \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestActionArg1()
            Test(
NewLines("Class C \n Sub M(i As Integer) \n [|Foo|](NextMethod) \n End Sub \n Sub NextMethod(i As Integer) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(i As Integer) \n Foo(NextMethod) \n End Sub \n Private Sub Foo(nextMethod As Action(Of Integer)) \n Throw New NotImplementedException() \n End Sub \n Sub NextMethod(i As Integer) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeInference()
            Test(
NewLines("Class C \n Sub M() \n If [|Foo|]() \n End If \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n If Foo() \n End If \n End Sub \n Private Function Foo() As Boolean \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMemberAccessArgumentName()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|](Me.Bar) \n End Sub \n Dim Bar As Integer \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Me.Bar) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Bar As Integer \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestParenthesizedArgumentName()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|]((Bar)) \n End Sub \n Dim Bar As Integer \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo((Bar)) \n End Sub \n Private Sub Foo(bar As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Bar As Integer \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestCastedArgumentName()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|](DirectCast(Me.Baz, Bar)) \n End Sub \n End Class \n Class Bar \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(DirectCast(Me.Baz, Bar)) \n End Sub \n Private Sub Foo(baz As Bar) \n Throw New NotImplementedException() \n End Sub \n End Class \n Class Bar \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDuplicateNames()
            Test(
NewLines("Class C \n Sub M() \n [|Foo|](DirectCast(Me.Baz, Bar), Me.Baz) \n End Sub \n Dim Baz As Integer \n End Class \n Class Bar \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(DirectCast(Me.Baz, Bar), Me.Baz) \n End Sub \n Private Sub Foo(baz1 As Bar, baz2 As Integer) \n Throw New NotImplementedException() \n End Sub \n Dim Baz As Integer \n End Class \n Class Bar \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenericArgs1()
            Test(
NewLines("Class C \n Sub M() \n [|Foo(Of Integer)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Of Integer)() \n End Sub \n Private Sub Foo(Of T)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenericArgs2()
            Test(
NewLines("Class C \n Sub M() \n [|Foo(Of Integer, String)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo(Of Integer, String)() \n End Sub \n Private Sub Foo(Of T1,T2)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539984)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenericArgsFromMethod()
            Test(
NewLines("Class C \n Sub M(Of X,Y)(x As X, y As Y) \n [|Foo|](x) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X,Y)(x As X, y As Y) \n Foo(x) \n End Sub \n Private Sub Foo(Of X)(x1 As X) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenericArgThatIsTypeParameter()
            Test(
NewLines("Class C \n Sub M(Of X)(y1 As X(), x1 As System.Func(Of X)) \n [|Foo(Of X)|](y1, x1) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X)(y1 As X(), x1 As System.Func(Of X)) \n Foo(Of X)(y1, x1) \n End Sub \n Private Sub Foo(Of X)(y1() As X, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMultipleGenericArgsThatAreTypeParameters()
            Test(
NewLines("Class C \n Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X)) \n [|Foo(Of X, Y)|](y1, x1) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(y1 As Y(), x1 As System.Func(Of X)) \n Foo(Of X, Y)(y1, x1) \n End Sub \n Private Sub Foo(Of X, Y)(y1() As Y, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539984)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMultipleGenericArgsFromMethod()
            Test(
NewLines("Class C \n Sub M(Of X, Y)(x As X, y As Y) \n [|Foo|](x, y) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(x As X, y As Y) \n Foo(x, y) \n End Sub \n Private Sub Foo(Of X, Y)(x1 As X, y1 As Y) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539984)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMultipleGenericArgsFromMethod2()
            Test(
NewLines("Class C \n Sub M(Of X, Y)(y As Y(), x As System.Func(Of X)) \n [|Foo|](y, x) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M(Of X, Y)(y As Y(), x As System.Func(Of X)) \n Foo(y, x) \n End Sub \n Private Sub Foo(Of Y, X)(y1() As Y, x1 As Func(Of X)) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoOuterThroughInstance()
            Test(
NewLines("Class Outer \n Class C \n Sub M(o As Outer) \n o.[|Foo|]() \n End Sub \n End Class \n End Class"),
NewLines("Imports System \n Class Outer \n Class C \n Sub M(o As Outer) \n o.Foo() \n End Sub \n End Class \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoOuterThroughClass()
            Test(
NewLines("Class Outer \n Class C \n Sub M(o As Outer) \n Outer.[|Foo|]() \n End Sub \n End Class \n End Class"),
NewLines("Imports System \n Class Outer \n Class C \n Sub M(o As Outer) \n Outer.Foo() \n End Sub \n End Class Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoSiblingThroughInstance()
            Test(
NewLines("Class C \n Sub M(s As Sibling) \n s.[|Foo|]() \n End Sub \n End Class \n Class Sibling \n End Class"),
NewLines("Imports System \n Class C \n Sub M(s As Sibling) \n s.Foo() \n End Sub \n End Class \n Class Sibling \n Friend Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoSiblingThroughClass()
            Test(
NewLines("Class C \n Sub M(s As Sibling) \n [|Sibling.Foo|]() \n End Sub \n End Class \n Class Sibling \n End Class"),
NewLines("Imports System \n Class C \n Sub M(s As Sibling) \n Sibling.Foo() \n End Sub \n End Class \n Class Sibling \n Friend Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoInterfaceThroughInstance()
            Test(
NewLines("Class C \n Sub M(s As ISibling) \n s.[|Foo|]() \n End Sub \n End Class \n Interface ISibling \n End Interface"),
NewLines("Class C \n Sub M(s As ISibling) \n s.Foo() \n End Sub \n End Class \n Interface ISibling \n Sub Foo() \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateAbstractIntoSameType()
            Test(
NewLines("MustInherit Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("MustInherit Class C \n Sub M() \n Foo() \n End Sub \n Friend MustOverride Sub Foo() \n End Class"),
index:=1)
        End Sub

        <WorkItem(539297)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateIntoModule()
            Test(
NewLines("Module Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Module"),
NewLines("Imports System \n Module Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() End Sub \n End Module"))
        End Sub

        <WorkItem(539506)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInference1()
            Test(
NewLines("Class C \n Sub M() \n Do While [|Foo|]() \n Loop \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Do While Foo() \n Loop \n End Sub \n Private Function Foo() As Boolean \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539505)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestEscaping1()
            Test(
NewLines("Class C \n Sub M() \n [|[Sub]|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n [Sub]() \n End Sub \n Private Sub [Sub]() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539504)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestExplicitCall()
            Test(
NewLines("Class C \n Sub M() \n Call [|S|] \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Call S \n End Sub \n Private Sub S() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539504)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestImplicitCall()
            Test(
NewLines("Class C \n Sub M() \n [|S|] \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S \n End Sub \n Private Sub S() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539537)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestArrayAccess1()
            TestMissing(NewLines("Class C \n Sub M(x As Integer()) \n Foo([|x|](4)) \n End Sub \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterInteger()
            Test(
NewLines("Class C \n Sub M() \n [|S%|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S%() \n End Sub \n Private Function S() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterLong()
            Test(
NewLines("Class C \n Sub M() \n [|S&|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S&() \n End Sub \n Private Function S() As Long \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterDecimal()
            Test(
NewLines("Class C \n Sub M() \n [|S@|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S@() \n End Sub \n Private Function S() As Decimal \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterSingle()
            Test(
NewLines("Class C \n Sub M() \n [|S!|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S!() \n End Sub \n Private Function S() As Single \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterDouble()
            Test(
NewLines("Class C \n Sub M() \n [|S#|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S#() \n End Sub \n Private Function S() As Double \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeCharacterString()
            Test(
NewLines("Class C \n Sub M() \n [|S$|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n S$() \n End Sub \n Private Function S() As String \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(539283)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNewLines()
            Test(
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
        End Sub

        <WorkItem(539283)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNewLines2()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestArgumentTypeVoid()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n Dim v As Void \n [|Foo|](v) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Dim v As Void \n Foo(v) \n End Sub \n Private Sub Foo(v As Object) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateFromImplementsClause()
            Test(
NewLines("Class Program \n Implements IFoo \n Public Function Bip(i As Integer) As String Implements [|IFoo.Snarf|] \n End Function \n End Class \n Interface IFoo \n End Interface"),
NewLines("Class Program \n Implements IFoo \n Public Function Bip(i As Integer) As String Implements IFoo.Snarf \n End Function \n End Class \n Interface IFoo \n Function Snarf(i As Integer) As String \n End Interface"))
        End Sub

        <WorkItem(537929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInScript1()
            Test(
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub"),
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub"),
            parseOptions:=GetScriptOptions())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInTopLevelImplicitClass1()
            Test(
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub"),
NewLines("Imports System \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInNamespaceImplicitClass1()
            Test(
NewLines("Imports System \n Namespace N \n Shared Sub Main ( args As String() ) \n [|Foo|] ( ) \n End Sub \n End Namespace"),
NewLines("Imports System \n Namespace N \n Shared Sub Main ( args As String() ) \n Foo ( ) \n End Sub \n Private Shared Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInNamespaceImplicitClass_FieldInitializer()
            Test(
NewLines("Imports System \n Namespace N \n Dim a As Integer = [|Foo|]() \n End Namespace"),
NewLines("Imports System \n Namespace N \n Dim a As Integer = Foo() \n Private Function Foo() As Integer \n Throw New NotImplementedException() \n End Function \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod1()
            TestMissing(
NewLines("Class Program \n Implements IFoo \n Public Function Blah() As String Implements [|IFoo.Blah|] \n End Function \n End Class \n Interface IFoo \n Sub Blah() \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod2()
            TestMissing(
NewLines("Class Program \n Implements IFoo \n Public Function Blah() As String Implements [|IFoo.Blah|] \n End Function \n End Class \n Interface IFoo \n Sub Blah() \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod3()
            Test(
NewLines("Class C \n Implements IFoo \n Sub Snarf() Implements [|IFoo.Blah|] \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Snarf() Implements IFoo.Blah \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n Sub Blah() \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod4()
            Test(
NewLines("Class C \n Implements IFoo \n Sub Snarf(i As String) Implements [|IFoo.Blah|] \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Snarf(i As String) Implements IFoo.Blah \n End Sub \n End Class \n Interface IFoo \n Sub Blah(ByRef i As Integer) \n Sub Blah(i As String) \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod5()
            Test(
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer) Implements [|IFoo.Snarf|] \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As String) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer) Implements IFoo.Snarf \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As String) \n Sub Snarf(i As Integer) \n End Interface"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClashesWithMethod6()
            Test(
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer, s As String) Implements [|IFoo.Snarf|] \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As Integer, b As Boolean) \n End Interface"),
NewLines("Class C \n Implements IFoo \n Sub Blah(i As Integer, s As String) Implements IFoo.Snarf \n End Sub \n End Class \n Friend Interface IFoo \n Sub Snarf(i As Integer, b As Boolean) \n Sub Snarf(i As Integer, s As String) \n End Interface"))
        End Sub

        <WorkItem(539708)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNoStaticGenerationIntoInterface()
            TestMissing(
NewLines("Interface IFoo \n End Interface \n Class Program \n Sub Main \n IFoo.[|Bar|] \n End Sub \n End Class"))
        End Sub

        <WorkItem(539821)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestEscapeParametername()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim [string] As String = ""hello"" \n [|[Me]|]([string]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim [string] As String = ""hello"" \n [Me]([string]) \n End Sub \n Private Sub [Me]([string] As String) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(539810)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotUseUnavailableTypeParameter()
            Test(
NewLines("Class Test \n Sub M(Of T)(x As T) \n [|Foo(Of Integer)|](x) \n End Sub \n End Class"),
NewLines("Imports System \n Class Test \n Sub M(Of T)(x As T) \n Foo(Of Integer)(x) \n End Sub \n Private Sub Foo(Of T)(x As T) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539808)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotUseTypeParametersFromContainingType()
            Test(
NewLines("Class Test(Of T) \n Sub M() \n [|Method(Of T)|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class Test(Of T) \n Sub M() \n Method(Of T)() \n End Sub \n Private Sub Method(Of T1)() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNameSimplification1()
            Test(
NewLines("Imports System \n Class C \n Sub M() \n [|Foo|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Foo() \n End Sub \n Private Sub Foo() \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(539809)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestFormattingOfMembers()
            Test(
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
        End Sub

        <WorkItem(540013)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInAddressOfExpression1()
            Test(
NewLines("Delegate Sub D(x As Integer) \n Class C \n Public Sub Foo() \n Dim x As D = New D(AddressOf [|Method|]) \n End Sub \n End Class"),
NewLines("Imports System \n Delegate Sub D(x As Integer) \n Class C \n Public Sub Foo() \n Dim x As D = New D(AddressOf Method) \n End Sub \n Private Sub Method(x As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(527986)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNotOfferedForInferredGenericMethodArgs()
            TestMissing(
NewLines("Class Foo(Of T) \n Sub Main(Of T, X)(k As Foo(Of T)) \n [|Bar|](k) \n End Sub \n Private Sub Bar(Of T)(k As Foo(Of T)) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540740)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDelegateInAsClause()
            Test(
NewLines("Delegate Sub D(x As Integer) \n Class C \n Private Sub M() \n Dim d As New D(AddressOf [|Test|]) \n End Sub \n End Class"),
NewLines("Imports System \n Delegate Sub D(x As Integer) \n Class C \n Private Sub M() \n Dim d As New D(AddressOf Test) \n End Sub \n Private Sub Test(x As Integer) \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Sub

        <WorkItem(541405)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMissingOnImplementedInterfaceMethod()
            TestMissing(
NewLines("Class C(Of U) \n Implements ITest \n Public Sub Method(x As U) Implements [|ITest.Method|] \n End Sub \n End Class \n Friend Interface ITest \n Sub Method(x As Object) \n End Interface"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNotOnConstructorInitializer()
            TestMissing(
NewLines("Class C \n Sub New \n Me.[|New|](1) \n End Sub \n End Class"))
        End Sub

        <WorkItem(542838)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMultipleImportsAdded()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In [|HERE|]() : Next \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n For Each v As Integer In HERE() : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Integer) \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(543007)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestCompilationMemberImports()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In [|HERE|]() : Next \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For Each v As Integer In HERE() : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Integer) \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Sub

        <WorkItem(531301)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestForEachWithNoControlVariableType()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n For Each v In [|HERE|] : Next \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For Each v In HERE : Next \n End Sub \n Private Function HERE() As IEnumerable(Of Object) \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System"), GlobalImport.Parse("System.Collections.Generic")))
        End Sub

        <WorkItem(531301)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestElseIfStatement()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n If x Then \n ElseIf [|HERE|] Then \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n If x Then \n ElseIf HERE Then \n End If \n End Sub \n Private Function HERE() As Boolean \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Sub

        <WorkItem(531301)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestForStatement()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n For x As Integer = 1 To [|HERE|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n For x As Integer = 1 To HERE \n End Sub \n Private Function HERE() As Integer \n Throw New NotImplementedException() \n End Function \n End Module"),
parseOptions:=Nothing,
compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("System")))
        End Sub

        <WorkItem(543216)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestArrayOfAnonymousTypes()
            Test(
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim product = New With {Key .Name = """", Key .Price = 0} \n Dim products = ToList(product) \n [|HERE|](products) \n End Sub \n Function ToList(Of T)(a As T) As IEnumerable(Of T) \n Return Nothing \n End Function \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim product = New With {Key .Name = """", Key .Price = 0} \n Dim products = ToList(product) \n HERE(products) \n End Sub \n Private Sub HERE(products As IEnumerable(Of Object)) \n Throw New NotImplementedException() \n End Sub \n Function ToList(Of T)(a As T) As IEnumerable(Of T) \n Return Nothing \n End Function \n End Module"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestMissingOnHiddenType()
            TestMissing(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotGenerateIntoHiddenRegion1_NoImports()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotGenerateIntoHiddenRegion1_WithImports()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotGenerateIntoHiddenRegion2()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestDoNotGenerateIntoHiddenRegion3()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestAddressOfInference1()
            Test(
NewLines("Imports System \n Module Program \n Sub Main(ByVal args As String()) \n Dim v As Func(Of String) = Nothing \n Dim a1 = If(False, v, AddressOf [|TestMethod|]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(ByVal args As String()) \n Dim v As Func(Of String) = Nothing \n Dim a1 = If(False, v, AddressOf TestMethod) \n End Sub \n Private Function TestMethod() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(544641)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestClassStatementTerminators1()
            Test(
NewLines("Class C : End Class \n Class B \n Sub Foo() \n C.[|Bar|]() \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Friend Shared Sub Bar() \n Throw New NotImplementedException() \n End Sub \n End Class \n Class B \n Sub Foo() \n C.Bar() \n End Sub \n End Class"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments1()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,,) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional p3 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments2()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1,,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1,,) \n End Sub \n Private Sub foo(v As Integer, Optional p1 As Object = Nothing, Optional p2 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments3()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,1,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,1,) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional v As Integer = Nothing, Optional p2 As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments4()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](,,1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(,,1) \n End Sub \n Private Sub foo(Optional p1 As Object = Nothing, Optional p2 As Object = Nothing, Optional v As Integer = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments5()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1,, 1) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1,, 1) \n End Sub \n Private Sub foo(v1 As Integer, Optional p As Object = Nothing, Optional v2 As Integer = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546037)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestOmittedArguments6()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|foo|](1, 1, ) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n foo(1, 1, ) \n End Sub \n Private Sub foo(v1 As Integer, v2 As Integer, Optional p As Object = Nothing) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546683)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestNotOnMissingMethodName()
            TestMissing(NewLines("Class C \n Sub M() \n Me.[||] \n End Sub \n End Class"))
        End Sub

        <WorkItem(546684)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateFromEventHandler()
            Test(
NewLines("Module Module1 \n Sub Main() \n Dim c1 As New Class1 \n AddHandler c1.AnEvent, AddressOf [|EventHandler1|] \n End Sub \n Public Class Class1 \n Public Event AnEvent() \n End Class \n End Module"),
NewLines("Imports System \n Module Module1 \n Sub Main() \n Dim c1 As New Class1 \n AddHandler c1.AnEvent, AddressOf EventHandler1 \n End Sub \n Private Sub EventHandler1() \n Throw New NotImplementedException() \n End Sub \n Public Class Class1 \n Public Event AnEvent() \n End Class \n End Module"))
        End Sub

        <WorkItem(530814)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestCapturedMethodTypeParameterThroughLambda()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Module M \n Sub Foo(Of T, S)(x As List(Of T), y As List(Of S)) \n [|Bar|](x, Function() y) ' Generate Bar \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module M \n Sub Foo(Of T, S)(x As List(Of T), y As List(Of S)) \n Bar(x, Function() y) ' Generate Bar \n End Sub \n Private Sub Bar(Of T, S)(x As List(Of T), p As Func(Of List(Of S))) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeParameterAndParameterConflict1()
            Test(
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.[|Bar|](T:=x) \n End Sub \n End Class \n  \n Module M \n End Module"),
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.Bar(T:=x) \n End Sub \n End Class \n  \n Module M \n Friend Sub Bar(Of T1)(T As T1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(530968)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestTypeParameterAndParameterConflict2()
            Test(
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.[|Bar|](t:=x) ' Generate Bar \n End Sub \n End Class \n  \n Module M \n End Module"),
NewLines("Imports System \n Class C(Of T) \n Sub Foo(x As T) \n M.Bar(t:=x) ' Generate Bar \n End Sub \n End Class \n  \n Module M \n Friend Sub Bar(Of T1)(t As T1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(546850)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestCollectionInitializer1()
            Test(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n [|Bar|](1, {1}) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Bar(1, {1}) \n End Sub \n Private Sub Bar(v As Integer, p() As Integer) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(546925)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestCollectionInitializer2()
            Test(
NewLines("Imports System \n Module M \n Sub Main() \n [|Foo|]({{1}}) \n End Sub \n End Module"),
NewLines("Imports System \n Module M \n Sub Main() \n Foo({{1}}) \n End Sub \n Private Sub Foo(p(,) As Integer) \n Throw New NotImplementedException() \n End Sub \n End Module"))
        End Sub

        <WorkItem(530818)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestParameterizedProperty1()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n [|Prop|](1) = 2 \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Prop(1) = 2 \n End Sub \n Private Function Prop(v As Integer) As Integer \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(530818)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestParameterizedProperty2()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n [|Prop|](1) = 2 \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Prop(1) = 2 \n End Sub \n Private Property Prop(v As Integer) As Integer \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Module"),
index:=1)
        End Sub

        <WorkItem(907612)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithLambda_1()
            Test(
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
        End Sub

        <WorkItem(907612)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithLambda_2()
            Test(
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
        End Sub

        <WorkItem(907612)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithLambda_3()
            Test(
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
        End Sub

        <WorkItem(889349)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodForDifferentParameterName()
            Test(
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
        End Sub

        <WorkItem(769760)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodForSameNamedButGenericUsage_1()
            Test(
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
        End Sub

        <WorkItem(769760)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodForSameNamedButGenericUsage_2()
            Test(
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
        End Sub

        <WorkItem(935731)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodForAwaitWithoutParenthesis()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodTooManyArgs1()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNamespaceNotExpression1()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoArgumentCountOverloadCandidates1()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodFunctionResultCannotBeIndexed1()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoCallableOverloadCandidates2()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoNonNarrowingOverloadCandidates2()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoNonNarrowingOverloadCandidates3()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoNonNarrowingOverloadCandidates4()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodArgumentNarrowing()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodArgumentNarrowing2()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodArgumentNarrowing3()
            Test(
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
        End Sub

        <WorkItem(939941)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodNoMostSpecificOverload2()
            Test(
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
        End Sub

        <WorkItem(1032176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodInsideNameOf()
            Test(
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
        End Sub

        <WorkItem(1032176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodInsideNameOf2()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithNameOfArgument()
            Test(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithLambdaAndNameOfArgument()
            Test(
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
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis2()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B \n End Sub \n Private Function B() As Object \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis3()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B \n End Sub \n Private Function B() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis4()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis5()
            Test(
NewLines("Option Strict On \n Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Option Strict On \n Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis6()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis7()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Object \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis8()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As C \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis9()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis10()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Integer \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccessNoParenthesis11()
            Test(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Function Z() As Object \n Throw New NotImplementedException() \n End Function \n End Class \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccess()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B() \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccess2()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B() \n End Sub \n Private Function B() As Object \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccess3()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B() \n End Sub \n Private Function B() As Integer \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalAccess4()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B() \n End Sub \n Private Function B() As C \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Sub TestGeneratePropertyConditionalAccess()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B() \n End Sub \n Private ReadOnly Property B As C \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Sub TestGeneratePropertyConditionalAccess2()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B() \n End Sub \n Private ReadOnly Property B As Object \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Sub TestGeneratePropertyConditionalAccess3()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B() \n End Sub \n Private ReadOnly Property B As Integer \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Sub

        <WorkItem(1064815)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Sub TestGeneratePropertyConditionalAccess4()
            Test(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|]() \n End Sub \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C? = a?.B() \n End Sub \n Private ReadOnly Property B As C \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"),
index:=1)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalInPropertyInitializer()
            Test(
NewLines("Module Program \n Property a As Integer = [|y|] \n End Module"),
NewLines("Imports System\n\nModule Program\nProperty a As Integer = y\n\nPrivate Function y() As Integer\nThrow New NotImplementedException()\nEnd Function\nEnd Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConditionalInPropertyInitializer2()
            Test(
NewLines("Module Program \n Property a As Integer = [|y|]() \n End Module"),
NewLines("Imports System\n\nModule Program\nProperty a As Integer = y()\n\n Private Function y() As Integer\nThrow New NotImplementedException()\nEnd Function\nEnd Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodTypeOf()
            Test(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Module C \n Sub Test() \n If TypeOf B Is String Then \n End If \n End Sub \n Private Function B() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodTypeOf2()
            Test(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|]() Is String Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Module C \n Sub Test() \n If TypeOf B() Is String Then \n End If \n End Sub \n Private Function B() As String \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodConfigureAwaitFalse()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Async Sub Main(args As String()) \n Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Collections.Generic\nImports System.Linq\nImports System.Threading.Tasks\n\nModule Program\n    Async Sub Main(args As String())\n        Dim x As Boolean = Await Foo().ConfigureAwait(False)\n    End Sub\n\n    Private Function Foo() As Task(Of Boolean)\n        Throw New NotImplementedException()\n    End Function\nEnd Module"))
        End Sub

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Sub TestGeneratePropertyConfigureAwaitFalse()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Async Sub Main(args As String()) \n Dim x As Boolean = Await [|Foo|]().ConfigureAwait(False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Collections.Generic\nImports System.Linq\nImports System.Threading.Tasks\n\nModule Program\n    Async Sub Main(args As String())\n        Dim x As Boolean = Await Foo().ConfigureAwait(False)\n    End Sub\n\n    Private ReadOnly Property Foo As Task(Of Boolean)\n        Get\n            Throw New NotImplementedException()\n        End Get\n    End Property\nEnd Module"),
index:=1)
        End Sub

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithMethodChaining()
            Test(
NewLines("Imports System \n Imports System.Linq \n Module M \n Async Sub T() \n Dim x As Boolean = Await [|F|]().ContinueWith(Function(a) True).ContinueWith(Function(a) False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Linq\nImports System.Threading.Tasks\n\nModule M\n    Async Sub T()\n        Dim x As Boolean = Await F().ContinueWith(Function(a) True).ContinueWith(Function(a) False)\n    End Sub\n\n    Private Function F() As Task(Of Boolean)\n        Throw New NotImplementedException()\n    End Function\nEnd Module"))
        End Sub

        <WorkItem(643, "https://github.com/dotnet/roslyn/issues/643")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodWithMethodChaining2()
            Test(
NewLines("Imports System \n Imports System.Linq \n Module M \n Async Sub T() \n Dim x As Boolean = Await [|F|]().ContinueWith(Function(a) True).ContinueWith(Function(a) False) \n End Sub \n End Module"),
NewLines("Imports System\nImports System.Linq\nImports System.Threading.Tasks\n\nModule M\n    Async Sub T()\n        Dim x As Boolean = Await F().ContinueWith(Function(a) True).ContinueWith(Function(a) False)\n    End Sub\n\n    Private ReadOnly Property F As Task(Of Boolean)\n        Get\n            Throw New NotImplementedException()\n        End Get\n    End Property\nEnd Module"),
index:=1)
        End Sub

        <WorkItem(1130960)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestGenerateMethodInTypeOfIsNot()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n Private Function Prop() As TypeOfIsNotDerived \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(529480)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInCollectionInitializers1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New List ( Of Integer ) From { [|T|]() } \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New List ( Of Integer ) From { T() } \n End Sub \n Private Function T() As Integer \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        <WorkItem(529480)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Sub TestInCollectionInitializers2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New Dictionary ( Of Integer , Boolean ) From { { 1, [|T|]() } } \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Module Program \n Sub M() \n Dim x = New Dictionary ( Of Integer , Boolean ) From { { 1, T() } } \n End Sub \n Private Function T() As Boolean \n Throw New NotImplementedException() \n End Function \n End Module"))
        End Sub

        Public Class GenerateConversionTests
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateConversionCodeFixProvider())
            End Function

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateExplicitConversionGenericClass()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateExplicitConversionClass()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateExplicitConversionAwaitExpression()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateImplicitConversionTargetTypeNotInSource()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateImplicitConversionGenericClass()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateImplicitConversionClass()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateImplicitConversionAwaitExpression()
                Test(
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
            End Sub

            <WorkItem(774321)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
            Public Sub TestGenerateExplicitConversionTargetTypeNotInSource()
                Test(
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
            End Sub

        End Class
    End Class
End Namespace
