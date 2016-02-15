' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
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
        Public Async Function TestGenerateIntoContainingType() As Task
            Await TestAsync(
NewLines("Class C \n Sub Main() \n Dim f = New C([|4|], 5, 6) \n End Sub \n End Class"),
NewLines("Class C \n Private v1 As Integer \n Private v2 As Integer \n Private v3 As Integer \n Public Sub New(v1 As Integer, v2 As Integer, v3 As Integer) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.v3 = v3 \n End Sub \n Sub Main() \n Dim f = New C(4, 5, 6) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestInvokingFromInsideAnotherConstructor() As Task
            Await TestAsync(
NewLines("Class A \n Private v As B \n Public Sub New() \n Me.v = New B([|5|]) \n End Sub \n End Class \n Friend Class B \n End Class"),
NewLines("Class A \n Private v As B \n Public Sub New() \n Me.v = New B(5) \n End Sub \n End Class \n Friend Class B \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMissingGenerateDefaultConstructor() As Task
            Await TestMissingAsync(
NewLines("Class Test \n Sub Main() \n Dim a = New [|A|]() \n End Sub \n End Class \n Class A \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMissingGenerateDefaultConstructorInStructure() As Task
            Await TestMissingAsync(
NewLines("Class Test \n Sub Main() \n Dim a = New [|A|]() \n End Sub \n End Class \n Structure A \n End Structure"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOfferDefaultConstructorWhenOverloadExists() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Main() \n Dim a = [|New A()|] \n End Sub \n End Class \n Class A \n Sub New(x As Integer) \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A() \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n Sub New(x As Integer) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestParameterizedConstructorOffered1() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|1|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestParameterizedConstructorOffered2() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|1|]) \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New() \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <WorkItem(527627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527627"), WorkItem(539735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539735"), WorkItem(539735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539735")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestInAsNewExpression() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Main() \n Dim a As New A([|1|]) \n End Sub \n End Class \n Class A \n Public Sub New() \n End Sub \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a As New A(1) \n End Sub \n End Class \n Class A \n Private v As Integer \n Public Sub New() \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInPartialClass1() As Task
            Await TestAsync(
NewLines("Public Partial Class Test \n Public Sub S1() \n End Sub \n End Class \n Public Class Test \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n Public Class Test \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test(5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInPartialClassWhenArityDoesntMatch() As Task
            Await TestAsync(
NewLines("Public Partial Class Test \n Public Sub S1() \n End Sub \n End Class \n Public Class Test(Of T) \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n Public Class Test(Of T) \n Public Sub S2() \n End Sub \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test(5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInPartialClassWithConflicts() As Task
            Await TestAsync(
NewLines("Public Partial Class Test2 \n End Class \n Private Partial Class Test2 \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test2([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test2 \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n Private Partial Class Test2 \n End Class \n Public Class A \n Sub Main() \n Dim s = New Test2(5) \n End Sub \n End Class"))
        End Function

        <WorkItem(528257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528257")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateIntoInaccessibleType() As Task
            Await TestMissingAsync(
NewLines("Class Foo \n Private Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar([|5|]) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnNestedTypes() As Task
            Await TestAsync(
NewLines("Class Foo \n Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar([|5|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Class Bar \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar(5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnNestedPartialTypes() As Task
            Await TestAsync(
NewLines("Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S1() \n End Sub \n End Class \n End Class \n Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S2() \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Test.NestedTest([|5|]) \n End Sub \n End Class"),
NewLines("Public Partial Class Test \n Public Partial Class NestedTest \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub S1() \n End Sub \n End Class \n End Class \n Public Partial Class Test \n Public Partial Class NestedTest \n Public Sub S2() \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Test.NestedTest(5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnNestedGenericType() As Task
            Await TestAsync(
NewLines("Class Outer(Of T) \n Public Class Inner \n End Class \n Public i = New Inner([|5|]) \n End Class"),
NewLines("Class Outer(Of T) \n Public Class Inner \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n Public i = New Inner(5) \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnGenericTypes1() As Task
            Await TestAsync(
NewLines("Class Base(Of T, V) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)([|5|], 5) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n Private v1 As Integer \n Private v2 As Integer \n Public Sub New(v1 As Integer, v2 As Integer) \n Me.v1 = v1 \n Me.v2 = v2 \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnGenericTypes2() As Task
            Await TestAsync(
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)([|5|]) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n Private v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOnGenericTypes3() As Task
            Await TestAsync(
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class MoreDerived \n Inherits Derived(Of Double) \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n Dim c = New MoreDerived([|5.5|]) \n End Sub \n End Class"),
NewLines("Class Base(Of T, V) \n End Class \n Class Derived(Of V) \n Inherits Base(Of Integer, V) \n End Class \n Class MoreDerived \n Inherits Derived(Of Double) \n Private v As Double \n Public Sub New(v As Double) \n Me.v = v \n End Sub \n End Class \n Class Test \n Sub Foo() \n Dim a = New Base(Of Integer, Integer)(5, 5) \n Dim b = New Derived(Of Integer)(5) \n Dim c = New MoreDerived(5.5) \n End Sub \n End Class"))
        End Function

        <WorkItem(528244, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528244")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestDateTypeForInference() As Task
            Await TestAsync(
NewLines("Class Foo \n End Class \n Class A \n Sub Main() \n Dim s = New Foo([|Date.Now|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Private now As Date \n Public Sub New(now As Date) \n Me.now = now \n End Sub \n End Class \n Class A \n Sub Main() \n Dim s = New Foo(Date.Now) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestBaseConstructor() As Task
            Await TestAsync(
NewLines("Class Base \n End Class \n Class Derived \n Inherits Base \n Private x As Integer \n Public Sub New(x As Integer) \n MyBase.New([|x|]) \n Me.x = x \n End Sub \n End Class"),
NewLines("Class Base \n Private x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n End Class \n Class Derived \n Inherits Base \n Private x As Integer \n Public Sub New(x As Integer) \n MyBase.New(x) \n Me.x = x \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMustInheritBase() As Task
            Await TestAsync(
NewLines("MustInherit Class Base \n End Class \n Class Derived \n Inherits Base \n Shared x As Integer \n Public Sub New(x As Integer) \n MyBase.New([|x|]) 'This should generate a protected ctor in Base \n Derived.x = x \n End Sub \n Sub Test1() \n Dim a As New Derived(1) \n End Sub \n End Class"),
NewLines("MustInherit Class Base \n  Private x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n End Class \n Class Derived \n Inherits Base \n Shared x As Integer \n Public Sub New(x As Integer) \n MyBase.New(x) 'This should generate a protected ctor in Base \n Derived.x = x \n End Sub \n Sub Test1() \n Dim a As New Derived(1) \n End Sub \n End Class"))
        End Function

        <WorkItem(540586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540586")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMissingOnNoCloseParen() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim c = New [|foo|]( \n End Sub \n End Module \n Class foo \n End Class"))
        End Function

        <WorkItem(540545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540545")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestConversionError() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim i As Char \n Dim cObject As C = New C([|i|]) \n Console.WriteLine(cObject.v1) \n End Sub \n End Module \n Class C \n Public v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim i As Char \n Dim cObject As C = New C(i) \n Console.WriteLine(cObject.v1) \n End Sub \n End Module \n Class C \n Public v1 As Integer \n Private i As Char \n Public Sub New(i As Char) \n Me.i = i \n End Sub Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n \n End Class"))
        End Function

        <WorkItem(540642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540642")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestNotOnNestedConstructor() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New C([|New C()|]) \n End Sub \n End Module \n Friend Class C \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New C(New C()) \n End Sub \n End Module \n Friend Class C \n Private c As C \n Public Sub New(c As C) \n Me.c = c \n End Sub \n End Class"))
        End Function

        <WorkItem(540607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540607")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestUnavailableTypeParameters() As Task
            Await TestAsync(
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New Test([|x|], y) \n End Sub \n End Class \n Friend Class Test \n End Class"),
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New Test(x, y) \n End Sub \n End Class \n Friend Class Test \n Private x As Object \n Private y As Object \n Public Sub New(x As Object, y As Object) \n Me.x = x \n Me.y = y \n End Sub \n End Class"))
        End Function

        <WorkItem(540748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540748")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestKeywordArgument1() As Task
            Await TestAsync(
NewLines("Class Test \n Private [Class] As Integer = 5 \n Sub Main() \n Dim a = New A([|[Class]|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Private [Class] As Integer = 5 \n Sub Main() \n Dim a = New A([Class]) \n End Sub \n End Class \n Class A \n Private [class] As Integer \n Public Sub New([class] As Integer) \n Me.class = [class] \n End Sub \n End Class"))
        End Function

        <WorkItem(540747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540747")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestKeywordArgument2() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Main() \n Dim a = New A([|Class|]) \n End Sub \n End Class \n Class A \n End Class"),
NewLines("Class Test \n Sub Main() \n Dim a = New A(Class) \n End Sub \n End Class \n Class A \n Private p As Object \n Public Sub New(p As Object) \n Me.p = p \n End Sub \n End Class"))
        End Function

        <WorkItem(540746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540746")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestConflictWithTypeParameterName() As Task
            Await TestAsync(
NewLines("Class Test \n Sub Foo() \n Dim a = New Bar(Of Integer)([|5|]) \n End Sub \n End Class \n Class Bar(Of V) \n End Class"),
NewLines("Class Test \n Sub Foo() \n Dim a = New Bar(Of Integer)(5) \n End Sub \n End Class \n Class Bar(Of V) \n Private v1 As Integer \n Public Sub New(v1 As Integer) \n Me.v1 = v1 \n End Sub \n End Class"))
        End Function

        <WorkItem(541174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541174")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingReadonlyField() As Task
            Await TestAsync(
NewLines("Class C \n ReadOnly x As Integer \n Sub Test() \n Dim x As Integer = 1 \n Dim obj As New C([|x|]) \n End Sub \n End Class"),
NewLines("Class C \n ReadOnly x As Integer \n Public Sub New(x As Integer) \n Me.x = x \n End Sub \n Sub Test() \n Dim x As Integer = 1 \n Dim obj As New C(x) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingProperty() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Test() \n Dim x = New A([|P|]:=5) \n End Sub \n End Class \n Class A \n Public Property P As Integer \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim x = New A(P:=5) \n End Sub \n End Class \n Class A \n Public Sub New(P As Integer) \n Me.P = P \n End Sub \n Public Property P As Integer \n End Class"))
        End Function

        <WorkItem(542055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542055")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingMethod() As Task
            Await TestAsync(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Public Sub u() \n End Sub \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Private u1 As Integer \n Public Sub New(u As Integer) \n Me.u1 = u \n End Sub \n Public Sub u() \n End Sub \n End Class"))
        End Function

        <WorkItem(542055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542055")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestDetectAssignmentToSharedFieldFromInstanceConstructor() As Task
            Await TestAsync(
NewLines("Class Program \n Sub Test() \n Dim x = New A([|P|]:=5) \n End Sub \n End Class \n Class A \n Shared Property P As Integer \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim x = New A(P:=5) \n End Sub \n End Class \n Class A \n Private P1 As Integer \n Public Sub New(P As Integer) \n Me.P1 = P \n End Sub \n Shared Property P As Integer \n End Class"))
        End Function

        <WorkItem(542055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542055")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingFieldWithSameNameButIncompatibleType() As Task
            Await TestAsync(
NewLines("Class A \n Sub Test() \n Dim t As New B([|x|]:=5) \n End Sub \n End Class \n Class B \n Private x As String \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New B(x:=5) \n End Sub \n End Class \n Class B \n Private x As String \n Private x1 As Integer \n Public Sub New(x As Integer) \n Me.x1 = x \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingFieldFromBaseClass() As Task
            Await TestAsync(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n End Class \n Class B \n Protected u As Integer \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n Public Sub New(u As Integer) \n Me.u = u \n End Sub \n End Class \n Class B \n Protected u As Integer \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMeConstructorInitializer() As Task
            Await TestAsync(
NewLines("Class C \n Sub New \n Me.New([|1|]) \n End Sub \n End Class"),
NewLines("Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New \n Me.New(1) \n End Sub \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingMeConstructorInitializer() As Task
            Await TestMissingAsync(
NewLines("Class C \n Private v As Integer \n Sub New \n Me.[|New|](1) \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMyBaseConstructorInitializer() As Task
            Await TestAsync(
NewLines("Class C \n Sub New \n MyClass.New([|1|]) \n End Sub \n End Class"),
NewLines("Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New \n MyClass.New(1) \n End Sub \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingMyBaseConstructorInitializer() As Task
            Await TestMissingAsync(
NewLines("Class C \n Private v As Integer \n Sub New \n MyClass.[|New|](1) \n End Sub \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMyClassConstructorInitializer() As Task
            Await TestAsync(
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New([|1|]) \n End Sub \n End Class \n Class B \n End Class"),
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New(1) \n End Sub \n End Class \n Class B \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <WorkItem(542098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542098")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestExistingMyClassConstructorInitializer() As Task
            Await TestMissingAsync(
NewLines("Class C \n Inherits B \n Sub New \n MyBase.New([|1|]) \n End Sub \n End Class \n Class B \n Protected Sub New(v As Integer) \n End Sub \n End Class"))
        End Function

        <WorkItem(542056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542056")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestConflictingFieldNameInSubclass() As Task
            Await TestAsync(
NewLines("Class A \n Sub Test() \n Dim t As New C([|u|]:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private x As String \n End Class \n Class B \n Protected u As String \n End Class"),
NewLines("Class A \n Sub Test() \n Dim t As New C(u:=5) \n End Sub \n End Class \n Class C \n Inherits B \n Private u1 As Integer \n Private x As String \n Public Sub New(u As Integer) \n Me.u1 = u \n End Sub \n End Class \n Class B \n Protected u As String \n End Class"))
        End Function

        <WorkItem(542442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542442")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestNothingArgument() As Task
            Await TestMissingAsync(
NewLines("Class C1 \n Public Sub New(ByVal accountKey As Integer) \n Me.new() \n Me.[|new|](accountKey, Nothing) \n End Sub \n Public Sub New(ByVal accountKey As Integer, ByVal accountName As String) \n Me.New(accountKey, accountName, Nothing) \n End Sub \n Public Sub New(ByVal accountKey As Integer, ByVal accountName As String, ByVal accountNumber As String) \n End Sub \n End Class"))
        End Function

        <WorkItem(540641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540641")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMissingOnExistingConstructor() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub M() \n Dim x As C = New [|C|](P) \n End Sub \n End Module \n Class C \n Private v As Object \n Public Sub New(v As Object) \n Me.v = v \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerationIntoVisibleType() As Task
            Await TestAsync(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Class C \n Sub Foo() \n Dim x As New D([|5|]) \n End Sub \n End Class \n Class D \n End Class \n #End ExternalSource"),
NewLines("#ExternalSource (""Default.aspx"", 1) \n Class C \n Sub Foo() \n Dim x As New D(5) \n End Sub \n End Class \n Class D \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n #End ExternalSource"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestNoGenerationIntoEntirelyHiddenType() As Task
            Await TestMissingAsync(
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
        End Function

        <WorkItem(546030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546030")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestConflictingDelegatedParameterNameAndNamedArgumentName1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim objc As New C(1, [|prop|]:=""Property"") \n End Sub \n End Module \n  \n Class C \n Private prop As String \n  \n Public Sub New(prop As String) \n Me.prop = prop \n End Sub \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim objc As New C(1, prop:=""Property"") \n End Sub \n End Module \n  \n Class C \n Private prop As String \n Private v As Integer \n Public Sub New(prop As String) \n Me.prop = prop \n End Sub \n Public Sub New(v As Integer, prop As String) \n Me.v = v \n Me.prop = prop \n End Sub \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestFormattingInGenerateConstructor() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithArgument() As Task
            Await TestAsync(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(123)>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n <MyAttribute(123)> \n Public Class D \n End Class"))
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithMultipleArguments() As Task
            Await TestAsync(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(true, 1, ""hello"")>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v1 As Boolean \n Private v2 As Integer \n Private v3 As String \n Public Sub New(v1 As Boolean, v2 As Integer, v3 As String) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.v3 = v3 \n End Sub \n End Class \n <MyAttribute(true, 1, ""hello"")> \n Public Class D \n End Class"))
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithNamedArguments() As Task
            Await TestAsync(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(true, 1, Topic:= ""hello"")>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private Topic As String \n Private v1 As Boolean \n Private v2 As Integer \n Public Sub New(v1 As Boolean, v2 As Integer, Topic As String) \n Me.v1 = v1 \n Me.v2 = v2 \n Me.Topic = Topic \n End Sub \n End Class \n <MyAttribute(true, 1, Topic:= ""hello"")> \n Public Class D \n End Class"))
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithAdditionalConstructors() As Task
            Await TestAsync(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n [|<MyAttribute(True, 2)>|] \n Public Class D \n End Class"),
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private v As Integer \n Private v1 As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Public Sub New(v As Integer, v1 As Integer) \n Me.New(v) \n Me.v1 = v1 \n End Sub \n End Class \n <MyAttribute(True, 2)> \n Public Class D \n End Class"))
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithAllValidArguments() As Task
            Await TestAsync(
NewLines("Enum A \n A1 \n End Enum \n <AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n End Class \n [|<MyAttribute(New Short(1) {1, 2, 3}, A.A1, True, 1, ""Z""c, 5S, 1I, 5L, 6.0R, 2.1F, ""abc"")>|] \n Public Class D End Class"),
NewLines("Enum A \n A1 \n End Enum \n <AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute \n Private a1 As A \n Private v1 As Short() \n Private v10 As String \n Private v2 As Boolean \n Private v3 As Integer \n Private v4 As Char \n Private v5 As Short \n Private v6 As Integer \n Private v7 As Long \n Private v8 As Double \n Private v9 As Single \n Public Sub New(v1() As Short, a1 As A, v2 As Boolean, v3 As Integer, v4 As Char, v5 As Short, v6 As Integer, v7 As Long, v8 As Double, v9 As Single, v10 As String) \n Me.v1 = v1 \n Me.a1 = a1 \n Me.v2 = v2 \n Me.v3 = v3 \n Me.v4 = v4 \n Me.v5 = v5 \n Me.v6 = v6 \n Me.v7 = v7 \n Me.v8 = v8 \n Me.v9 = v9 \n Me.v10 = v10 \n End Sub \n End Class \n <MyAttribute(New Short(1) {1, 2, 3}, A.A1, True, 1, ""Z""c, 5S, 1I, 5L, 6.0R, 2.1F, ""abc"")> \n Public Class D \n End Class "))
        End Function

        <WorkItem(530003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530003")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestAttributesWithLambda() As Task
            Await TestMissingAsync(
NewLines("<AttributeUsage(AttributeTargets.Class)> \n Public Class MyAttribute \n Inherits System.Attribute End Class \n [|<MyAttribute(Function(x) x+1)>|] \n Public Class D \n End Class"))
        End Function

        <WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestConstructorGenerationForDifferentNamedParameter() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(897355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/897355")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestOptionStrictOn() As Task
            Await TestAsync(
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim int As Integer = 3 \n Dim obj As Object = New Object() \n Dim c1 As Classic = New Classic(int) \n Dim c2 As Classic = [|New Classic(obj)|] \n End Sub \n Class Classic \n Private int As Integer \n Public Sub New(int As Integer) \n Me.int = int \n End Sub \n End Class \n End Module"),
NewLines("Option Strict On \n Module Module1 \n Sub Main() \n Dim int As Integer = 3 \n Dim obj As Object = New Object() \n Dim c1 As Classic = New Classic(int) \n Dim c2 As Classic = New Classic(obj) \n End Sub \n Class Classic \n Private int As Integer \n Private obj As Object \n Public Sub New(obj As Object) \n Me.obj = obj \n End Sub \n Public Sub New(int As Integer) \n Me.int = int \n End Sub \n End Class \n End Module "))
        End Function

        <WorkItem(528257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528257")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInInaccessibleType() As Task
            Await TestAsync(
NewLines("Class Foo \n Private Class Bar \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New [|Foo.Bar(5)|] \n End Sub \n End Class"),
NewLines("Class Foo \n Private Class Bar \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class \n End Class \n Class A \n Sub Main() \n Dim s = New Foo.Bar(5) \n End Sub \n End Class"))
        End Function

        Public Class GenerateConstructorTestsWithFindMissingIdentifiersAnalyzer
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(), New GenerateConstructorCodeFixProvider())
            End Function

            <WorkItem(1241, "https://github.com/dotnet/roslyn/issues/1241")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
            Public Async Function TestGenerateConstructorInIncompleteLambda() As Task
                Await TestAsync(
NewLines("Imports System.Linq \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|C|](0)"),
NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Sub New() \n Dim s As Action = Sub() \n Dim a = New C(0)Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"))
            End Function

            <WorkItem(5920, "https://github.com/dotnet/roslyn/issues/5920")>
            <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
            Public Async Function TestGenerateConstructorInIncompleteLambda2() As Task
                Await TestAsync(
    NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|C|](0, 0)"),
    NewLines("Imports System.Linq \n Class C \n Private v As Integer \n Private v1 As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n Sub New() \n Dim s As Action = Sub() \n Dim a = New C(0, 0) \n Public Sub New(v As Integer, v1 As Integer) \n Me.New(v) \n Me.v1 = v1 \n End Sub \n End Class"))
            End Function
        End Class

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType1() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType2() As Task
            Await TestAsync(
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateInDerivedType_Crash() As Task
            Await TestMissingAsync(
"
Public Class Base
    Public Sub New(a As Integer, Optional b As String = Nothing)

    End Sub
End Class

Public Class [|;;|]Derived
    Inherits Base

End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateConstructorNotOfferedForDuplicate() As Task
            Await TestMissingAsync(
"Imports System

Class X
    Private v As String

    Public Sub New(v As String)
        Me.v = v
    End Sub

    Sub Test()
        Dim x As X = New X(New [|String|]())
    End Sub
End Class")
        End Function
    End Class
End Namespace