' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateConstructor
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateConstructor
    Public Class GenerateConstructorTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateConstructorCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateIntoContainingType()
            Test(
NewLines("Class C \n Sub Main() \n Dim f = New C([|4|], 5, 6) \n End Sub \n End Class"),
NewLines("Class C \n Private v1 As Integer \n Private v2 As Integer \n Private v3 As Integer \n Public Sub New(v1 As Integer, v2 As Integer, v3 As Integer) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.v3 = v3 \n End Sub \n Sub Main() \n Dim f = New C(4, 5, 6) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestInvokingFromInsideAnotherConstructor()
            Test(
NewLines("Class A \n Private v As B \n Public Sub New() \n Me.v = New B([|5|]) \n End Sub \n End Class \n Friend Class B \n End Class"),
NewLines("Class A \n Private v As B \n Public Sub New() \n Me.v = New B(5) \n End Sub \n End Class \n Friend Class B \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMissingGenerateDefaultConstructor()
            TestMissing(
NewLines("Class Test \n Sub Main() \n Dim a = New [|A|]() \n End Sub \n End Class \n Class A \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMissingGenerateDefaultConstructorInStructure()
            TestMissing(
NewLines("Class Test \n Sub Main() \n Dim a = New [|A|]() \n End Sub \n End Class \n Structure A \n End Structure"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOfferDefaultConstructorWhenOverloadExists()
            Test(
NewLines("Class Test \n Sub Main() \n Dim a = [|New A()|] \n End Sub \n End Class \n Class A \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A() \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestParameterizedConstructorOffered1()
            Test(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|1|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestParameterizedConstructorOffered2()
            Test(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|1|]) \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New() \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <WorkItem(527627), WorkItem(539735), WorkItem(539735)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestInAsNewExpression()
            Test(
NewLines("Class Test \n Sub Main() \n Dim a As New A([|1|]) \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a As New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New() \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInPartialClass1()
            Test(
NewLines("Public Partial Class Test \n Public Sub S1() \n End Sub \n End Class \n Public Class Test \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n Public Class Test \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test(5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInPartialClassWhenArityDoesntMatch()
            Test(
NewLines("Public Partial Class Test \n Public Sub S1() \n End Sub \n End Class \n Public Class Test(Of T) \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n Public Class Test(Of T) \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test(5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInPartialClassWithConflicts()
            Test(
NewLines("Public Partial Class Test2 \n End Class \n Private Partial Class Test2 \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test2([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test2 \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n Private Partial Class Test2 \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test2(5) \n End Sub \n End Class"))
        End Sub

        <WorkItem(528257)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateIntoInaccessibleType()
            TestMissing(
NewLines("Class Foo \n Private Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar([|5|]) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnNestedTypes()
            Test(
NewLines("Class Foo \n Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar([|5|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Class Bar \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar(5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnNestedPartialTypes()
            Test(
NewLines("Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S1() \n End Sub \n End Class \n End Class \n Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S2() \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Test.NestedTest([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Public Partial Class NestedTest \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n End Class \n Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S2() \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Test.NestedTest(5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnNestedGenericType()
            Test(
NewLines("Class Outer(Of T) \n Public Class Inner \n End Class \n Public i = New Inner([|5|]) \n End Class"),
NewLines("Class Outer(Of T) \n Public Class Inner \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n Public i = New Inner(5) \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnGenericTypes1()
            Test(
NewLines("Class Base(Of T, V) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)([|5|], 5) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n Private v1 As Integer \n Private v2 As Integer \n Public Sub New(v1 As Integer, v2 As Integer) \n Me.v1 = v1 \n Me.v2 = v2 \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnGenericTypes2()
            Test(
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)([|5|]) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n Private v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOnGenericTypes3()
            Test(
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class MoreDerived \n Inherits Derived(Of Double) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n Dim c = New MoreDerived([|5.5|]) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class MoreDerived \n Inherits Derived(Of Double) \n Private v As Double \n Public Sub New(v As Double) \n Me.v = v \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n Dim c = New MoreDerived(5.5) \n End Sub \n End Class"))
        End Sub

        <WorkItem(528244)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestDateTypeForInference()
            Test(
NewLines("Class Foo \n End Class \n Class A \n Sub Main() \n Dim s = New Foo([|Date.Now|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Private now As Date \n Public Sub New(now As Date) \n Me.now = now \n End Sub \n End Class \n Class A \n Sub Main() \n Dim s = New Foo(Date.Now) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestBaseConstructor()
            Test(
NewLines("Class Base \n End Class \n Class Derived \n Inherits Base \n Private x As Integer \n Public Sub New(x As Integer) \n MyBase.New([|x|]) \n Me.x = x \n End Sub \n End Class"),
NewLines("Class Base \n Private x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n End Class \n Class Derived \n Inherits Base \n Private x As Integer \n Public Sub New(x As Integer) \n MyBase.New(x) \n Me.x = x \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMustInheritBase()
            Test(
NewLines("MustInherit Class Base \n End Class \n Class Derived \n Inherits Base \n Shared x As Integer \n Public Sub New(x As Integer) \n MyBase.New([|x|]) 'This should generate a protected ctor in Base \n Derived.x = x \n End Sub \n Sub Test1() \n Dim a As New Derived(1) \n End Sub \n End Class"),
NewLines("MustInherit Class Base \n  Private x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n End Class \n Class Derived \n Inherits Base \n Shared x As Integer \n Public Sub New(x As Integer) \n MyBase.New(x) 'This should generate a protected ctor in Base \n Derived.x = x \n End Sub \n Sub Test1() \n Dim a As New Derived(1) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540586)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMissingOnNoCloseParen()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim c = New [|foo|]( \n End Sub \n End Module \n Class foo \n End Class"))
        End Sub

        <WorkItem(540545)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestConversionError()
            Test(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim i As Char \n Dim cObject As C = New C([|i|]) \n Console.WriteLine(cObject.v1) \n End Sub \n End Module \n Class C \n Public v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim i As Char \n Dim cObject As C = New C(i) \n Console.WriteLine(cObject.v1) \n End Sub \n End Module \n Class C \n Public v1 As Integer \n Private i As Char \n Public Sub New(i As Char) \n Me.i = i \n End Sub Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n \n End Class"))
        End Sub

        <WorkItem(540642)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestNotOnNestedConstructor()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New C([|New C()|]) \n End Sub \n End Module \n Friend Class C \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New C(New C()) \n End Sub \n End Module \n Friend Class C \n Private c As C \n Public Sub New(c As C) \n Me.c = c \n End Sub \n End Class"))
        End Sub

        <WorkItem(540607)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestUnavailableTypeParameters()
            Test(
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New Test([|x|], y) \n End Sub \n End Class \n Friend Class Test \n End Class"),
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New Test(x, y) \n End Sub \n End Class \n Friend Class Test \n Private x As Object \n Private y As Object \n Public Sub New(x As Object, y As Object) \n Me.x = x \n Me.y = y \n End Sub \n End Class"))
        End Sub

        <WorkItem(540748)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestKeywordArgument1()
            Test(
NewLines("Class Test \n Private [Class] As Integer = 5 \n Sub Main() \n Dim a = New A([|[Class]|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Private [Class] As Integer = 5 \n Sub Main() \n Dim a = New A([Class]) \n End Sub \n End Class \n Class A \n Private [class] As Integer \n Public Sub New([class] As Integer) \n Me.class = [class] \n End Sub \n End Class"))
        End Sub

        <WorkItem(540747)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestKeywordArgument2()
            Test(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|Class|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(Class) \n End Sub \n End Class \n Class A \n Private p As Object \n Public Sub New(p As Object) \n Me.p = p \n End Sub \n End Class"))
        End Sub

        <WorkItem(540746)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestConflictWithTypeParameterName()
            Test(
NewLines("Class Test \n Sub Foo() \n Dim a = New Bar(Of Integer)([|5|]) \n End Sub \n End Class \n Class Bar(Of V) \n End Class"),
NewLines("Class Test \n Sub Foo() \n Dim a = New Bar(Of Integer)(5) \n End Sub \n End Class \n Class Bar(Of V) \n Private v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class"))
        End Sub

        <WorkItem(541174)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingReadonlyField()
            Test(
NewLines("Class C \n ReadOnly x As Integer \n Sub Test() \n Dim x As Integer = 1 \n Dim obj As New C([|x|]) \n End Sub \n End Class"),
NewLines("Class C \n ReadOnly x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n Sub Test() \n Dim x As Integer = 1 \n Dim obj As New C(x) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingProperty()
            Test(
NewLines("Class Program \n Sub Test() \n Dim x = New A([|P|]:=5) \n End Sub \n End Class \n Class A \n Public Property P As Integer \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim x = New A(P:=5) \n End Sub \n End Class \n Class A \n Public Sub New(P As Integer) \n Me.P = P \n End Sub \n Public Property P As Integer \n End Class"))
        End Sub

        <WorkItem(542055)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingMethod()
            Test(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Public Sub u() \n End Sub \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Private u1 As Integer \n Public Sub New(u As Integer) \n Me.u1 = u \n End Sub \n Public Sub u() \n End Sub \n End Class"))
        End Sub

        <WorkItem(542055)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestDetectAssignmentToSharedFieldFromInstanceConstructor()
            Test(
NewLines("Class Program \n Sub Test() \n Dim x = New A([|P|]:=5) \n End Sub \n End Class \n Class A \n Shared Property P As Integer \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim x = New A(P:=5) \n End Sub \n End Class \n Class A \n Private P1 As Integer \n Public Sub New(P As Integer) \n Me.P1 = P \n End Sub \n Shared Property P As Integer \n End Class"))
        End Sub

        <WorkItem(542055)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingFieldWithSameNameButIncompatibleType()
            Test(
NewLines("Class A \n Sub Test() \n Dim t As New B([|x|]:=5) \n End Sub \n End Class \n Class B \n Private x As String \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New B(x:=5) \n End Sub \n End Class \n Class B \n Private x As String \n Private x1 As Integer \n Public Sub New(x As Integer) \n Me.x1 = x \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingFieldFromBaseClass()
            Test(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n End Class \n Class B \n Protected u As Integer \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n Public Sub New(u As Integer) \n Me.u = u \n End Sub \n End Class \n Class B \n Protected u As Integer \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMeConstructorInitializer()
            Test(
NewLines("Class C \n Sub New \n Me.New([|1|]) \n End Sub \n End Class"),
NewLines("Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New \n Me.New(1) \n End Sub \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingMeConstructorInitializer()
            TestMissing(
NewLines("Class C \n Private v As Integer \n Sub New \n Me.[|New|](1) \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMyBaseConstructorInitializer()
            Test(
NewLines("Class C \n Sub New \n MyClass.New([|1|]) \n End Sub \n End Class"),
NewLines("Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New \n MyClass.New(1) \n End Sub \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingMyBaseConstructorInitializer()
            TestMissing(
NewLines("Class C \n Private v As Integer \n Sub New \n MyClass.[|New|](1) \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMyClassConstructorInitializer()
            Test(
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New([|1|]) \n End Sub \n End Class \n Class B \n End Class"),
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New(1) \n End Sub \n End Class \n Class B \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <WorkItem(542098)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestExistingMyClassConstructorInitializer()
            TestMissing(
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New([|1|]) \n End Sub \n End Class \n Class B \n Protected Sub New(v As Integer) \n End Sub \n End Class"))
        End Sub

        <WorkItem(542056)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestConflictingFieldNameInSubclass()
            Test(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n End Class \n Class B \n Protected u As String \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private u1 As Integer \n Private x As String \n Public Sub New(u As Integer) \n Me.u1 = u \n End Sub \n End Class \n Class B \n Protected u As String \n End Class"))
        End Sub

        <WorkItem(542442)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestNothingArgument()
            TestMissing(
NewLines("Class C1 \n Public Sub New(ByVal accountKey As Integer) \n Me.new() \n Me.[|new|](accountKey, Nothing) \n End Sub \n Public Sub New(ByVal accountKey As Integer, ByVal accountName As String) \n Me.New(accountKey, accountName, Nothing) \n End Sub \n Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540641)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMissingOnExistingConstructor()
            TestMissing(
NewLines("Module Program \n Sub M() \n Dim x As C = New [|C|](P) \n End Sub \n End Module \n Class C \n Private v As Object \n Public Sub New(v As Object) \n Me.v = v \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerationIntoVisibleType()
            Test(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Class C \n Sub Foo() \n Dim x As New D([|5|]) \n End Sub \n End Class \n Class D \n End Class \n #End ExternalSource"),
NewLines("#ExternalSource (""Default.aspx"", 1) \n Class C \n Sub Foo() \n Dim x As New D(5) \n End Sub \n End Class \n Class D \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n #End ExternalSource"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestNoGenerationIntoEntirelyHiddenType()
            TestMissing(
<Text>#ExternalSource (""Default.aspx"", 1)
Class C
    Sub Foo()
        Dim x As New D([|5|])
    End Sub
End Class
#End ExternalSource

Class D

End Class
</Text>.Value)
        End Sub

        <WorkItem(546030)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestConflictingDelegatedParameterNameAndNamedArgumentName1()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim objc As New C(1, [|prop|]:=""Property"") \n End Sub \n End Module \n  \n Class C \n Private prop As String \n  \n Public Sub New(prop As String) \n Me.prop = prop \n End Sub \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim objc As New C(1, prop:=""Property"") \n End Sub \n End Module \n  \n Class C \n Private prop As String \n Private v As Integer \n Public Sub New(prop As String) \n Me.prop = prop \n End Sub \n Public Sub New(v As Integer, prop As String) \n Me.v = v \n Me.prop = prop \n End Sub \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestFormattingInGenerateConstructor()
            Test(
<Text>Class C
    Sub New()
        MyClass.New([|1|])
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Private v As Integer

    Sub New()
        MyClass.New(1)
    End Sub

    Public Sub New(v As Integer)
        Me.v = v
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithArgument()
            Test(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(123)>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n <MyAttribute(123)> \n Public Class D \n End Class"))
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithMultipleArguments()
            Test(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(true, 1, ""hello"")>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v1 As Boolean \n Private v2 As Integer \n Private v3 As String \n Public Sub New(v1 As Boolean, v2 As Integer, v3 As String) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.v3 = v3 \n End Sub \n End Class \n <MyAttribute(true, 1, ""hello"")> \n Public Class D \n End Class"))
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithNamedArguments()
            Test(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(true, 1, Topic:= ""hello"")>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private Topic As String \n Private v1 As Boolean \n Private v2 As Integer \n Public Sub New(v1 As Boolean, v2 As Integer, Topic As String) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.Topic = Topic \n End Sub \n End Class \n <MyAttribute(true, 1, Topic:= ""hello"")> \n Public Class D \n End Class"))
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithAdditionalConstructors()
            Test(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n [|<MyAttribute(True, 2)>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Private v1 As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub New(v As Integer, v1 As Integer) \n Me.New(v) \n Me.v1 = v1 \n End Sub \n End Class \n <MyAttribute(True, 2)> \n Public Class D \n End Class"))
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithAllValidArguments()
            Test(
NewLines("Enum A \n A1 \n End Enum \n <AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(New Short(1) {1, 2, 3}, A.A1, True, 1, ""Z""c, 5S, 1I, 5L, 6.0R, 2.1F, ""abc"")>|] \n Public Class D End Class"),
NewLines("Enum A \n A1 \n End Enum \n <AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private a1 As A \n Private v1 As Short() \n Private v10 As String \n Private v2 As Boolean \n Private v3 As Integer \n Private v4 As Char \n Private v5 As Short \n Private v6 As Integer \n Private v7 As Long \n Private v8 As Double \n Private v9 As Single \n Public Sub New(v1() As Short, a1 As A, v2 As Boolean, v3 As Integer, v4 As Char, v5 As Short, v6 As Integer, v7 As Long, v8 As Double, v9 As Single, v10 As String) \n Me.v1 = v1 \n Me.a1 = a1 \n Me.v2 = v2 \n Me.v3 = v3 \n Me.v4 = v4 \n Me.v5 = v5 \n Me.v6 = v6 \n Me.v7 = v7 \n Me.v8 = v8 \n Me.v9 = v9 \n Me.v10 = v10 \n End Sub \n End Class \n <MyAttribute(New Short(1) {1, 2, 3}, A.A1, True, 1, ""Z""c, 5S, 1I, 5L, 6.0R, 2.1F, ""abc"")> \n Public Class D \n End Class "))
        End Sub

        <WorkItem(530003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestAttributesWithLambda()
            TestMissing(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute End Class \n [|<MyAttribute(Function(x) x+1)>|] \n Public Class D \n End Class"))
        End Sub

        <WorkItem(889349)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestConstructorGenerationForDifferentNamedParameter()
            Test(
<Text>Class Program
    Sub Main(args As String())
        Dim a = New Program([|y:=4|])
    End Sub

    Sub New(x As Integer)

    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class Program
    Private y As Integer

    Sub Main(args As String())
        Dim a = New Program(y:=4)
    End Sub

    Sub New(x As Integer)

    End Sub

    Public Sub New(y As Integer)
        Me.y = y
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Sub

        <WorkItem(897355)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestOptionStrictOn()
            Test(
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim int As Integer = 3 \n Dim obj As Object = New Object() \n Dim c1 As Classic = New Classic(int) \n Dim c2 As Classic = [|New Classic(obj)|] \n End Sub \n Class Classic \n Private int As Integer \n Public Sub New(int As Integer) \n Me.int = int \n End Sub \n End Class \n End Module"),
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim int As Integer = 3 \n Dim obj As Object = New Object() \n Dim c1 As Classic = New Classic(int) \n Dim c2 As Classic = New Classic(obj) \n End Sub \n Class Classic \n Private int As Integer \n Private obj As Object \n Public Sub New(obj As Object) \n Me.obj = obj \n End Sub \n Public Sub New(int As Integer) \n Me.int = int \n End Sub \n End Class \n End Module "))
        End Sub

        <WorkItem(528257)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInInaccessibleType()
            Test(
NewLines("Class Foo \n Private Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New [|Foo.Bar(5)|] \n End Sub \n End Class"),
NewLines("Class Foo \n Private Class Bar \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar(5) \n End Sub \n End Class"))
        End Sub

        Public Class GenerateConstructorTestsWithFindMissingIdentifiersAnalyzer
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(), New GenerateConstructorCodeFixProvider())
            End Function

            <WorkItem(1241, "https://github.com/dotnet/roslyn/issues/1241")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
            Public Sub TestGenerateConstructorInIncompleteLambda()
                Test(
NewLines("Imports System.Linq \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|C|](0)"),
NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Sub New() \n Dim s As Action = Sub() \n Dim a = New C(0)Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
            End Sub

            <WorkItem(5920, "https://github.com/dotnet/roslyn/issues/5920")>
            <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
            Public Sub TestGenerateConstructorInIncompleteLambda2()
                Test(
    NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|C|](0, 0)"),
    NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Private v1 As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New() \n Dim s As Action = Sub() \n Dim a = New C(0, 0) \n Public Sub New(v As Integer, v1 As Integer) \n Me.New(v) \n Me.v1 = v1 \n End Sub \n End Class"))
            End Sub
        End Class

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInDerivedType1()
            Test(
"
Public Class Base
    Public Sub New(a As String)

    End Sub
End Class

Public Class [||]Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As String)

    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Sub New(a As String)
        MyBase.New(a)
    End Sub
End Class")
        End Sub

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInDerivedType2()
            Test(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class [||]Derived
    Inherits Base

End Class",
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class Derived
    Inherits Base

    Public Sub New(a As Integer, Optional b As String = Nothing)
        MyBase.New(a, b)
    End Sub
End Class")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateInDerivedType_Crash()
            TestMissing(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class [|;;|]Derived
    Inherits Base

End Class")
        End Sub
    End Class
End Namespace
