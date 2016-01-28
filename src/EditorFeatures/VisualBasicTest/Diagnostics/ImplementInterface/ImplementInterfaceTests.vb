' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementInterface

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ImplementInterface
    Partial Public Class ImplementInterfaceTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New ImplementInterfaceCodeFixProvider)
        End Function

        <WorkItem(540085)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSimpleMethod() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Public Sub M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMethodConflict1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Function M() As Integer \n End Function \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n Function M() As Integer \n End Function \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMethodConflict2() As Task
            Await TestAsync(
NewLines("Interface IFoo \n Sub Bar() \n End Interface \n Class C \n Implements [|IFoo|] \n Public Sub Bar() \n End Sub \n End Class"),
NewLines("Imports System \n Interface IFoo \n Sub Bar() \n End Interface \n Class C \n Implements IFoo \n Public Sub Bar() \n End Sub \n Private Sub IFoo_Bar() Implements IFoo.Bar \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542012)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMethodConflictWithField() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Private m As Integer \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Private m As Integer \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542015)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestAutoPropertyConflict() As Task
            Await TestAsync(
NewLines("Interface I \n Property M As Integer \n End Interface \n Class C \n Implements [|I|] \n Public Property M As Integer \n End Class"),
NewLines("Imports System \n Interface I \n Property M As Integer \n End Interface \n Class C \n Implements I \n Public Property M As Integer \n Private Property I_M As Integer Implements I.M \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <WorkItem(542015)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestFullPropertyConflict() As Task
            Await TestAsync(
NewLines("Interface I \n Property M As Integer \n End Interface \n Class C \n Implements [|I|] \n Private Property M As Integer \n Get \n Return 5 \n End Get \n Set(value As Integer) \n End Set \n End Property \n End Class"),
NewLines("Imports System \n Interface I \n Property M As Integer \n End Interface \n Class C \n Implements I \n Private Property I_M As Integer Implements I.M \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n Private Property M As Integer \n Get \n Return 5 \n End Get \n Set(value As Integer) \n End Set \n End Property \n End Class"))
        End Function

        <WorkItem(542019)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestConflictFromBaseClass1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class B \n Public Sub M() \n End Sub \n End Class \n Class C \n Inherits B \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class B \n Public Sub M() \n End Sub \n End Class \n Class C \n Inherits B \n Implements I \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542019)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestConflictFromBaseClass2() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class B \n Protected M As Integer \n End Class \n Class C \n Inherits B \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class B \n Protected M As Integer \n End Class \n Class C \n Inherits B \n Implements I \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542019)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestConflictFromBaseClass3() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class B \n Public Property M As Integer \n End Class \n Class C \n Inherits B \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class B \n Public Property M As Integer \n End Class \n Class C \n Inherits B \n Implements I \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementAbstractly1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n MustInherit Class C \n Implements [|I|] \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n MustInherit Class C \n Implements I \n Public MustOverride Sub M() Implements I.M \n End Class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementGenericType() As Task
            Await TestAsync(
NewLines("Interface IInterface1(Of T) \n Sub Method1(t As T) \n End Interface \n Class [Class] \n Implements [|IInterface1(Of Integer)|] \n End Class "),
NewLines("Imports System \n Interface IInterface1(Of T) \n Sub Method1(t As T) \n End Interface \n Class [Class] \n Implements IInterface1(Of Integer) \n Public Sub Method1(t As Integer) Implements IInterface1(Of Integer).Method1 \n Throw New NotImplementedException() \n End Sub \n End Class "))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementGenericTypeWithGenericMethod() As Task
            Await TestAsync(
NewLines("Interface IInterface1(Of T) \n Sub Method1(Of U)(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements [|IInterface1(Of Integer)|] \n End Class "),
NewLines("Imports System \n Interface IInterface1(Of T) \n Sub Method1(Of U)(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements IInterface1(Of Integer) \n Public Sub Method1(Of U)(arg As Integer, arg1 As U) Implements IInterface1(Of Integer).Method1 \n Throw New NotImplementedException() \n End Sub \n End Class "))
        End Function

        <Fact, WorkItem(6623, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementGenericTypeWithGenericMethodWithNaturalConstraint() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Interface IInterface1(Of T) \n Sub Method1(Of U As IList(Of T))(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements [|IInterface1(Of Integer)|] \n End Class "),
NewLines("Imports System \n Imports System.Collections.Generic \n Interface IInterface1(Of T) \n Sub Method1(Of U As IList(Of T))(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements IInterface1(Of Integer) \n Public Sub Method1(Of U As IList(Of Integer))(arg As Integer, arg1 As U) Implements IInterface1(Of Integer).Method1 \n Throw New NotImplementedException() \n End Sub \n End Class "))
        End Function

        <Fact, WorkItem(6623, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementGenericTypeWithGenericMethodWithUnexpressibleConstraint() As Task
            Await TestAsync(
NewLines("Interface IInterface1(Of T) \n Sub Method1(Of U As T)(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements [|IInterface1(Of Integer)|] \n End Class "),
NewLines("Imports System \n Interface IInterface1(Of T) \n Sub Method1(Of U As T)(arg As T, arg1 As U) \n End Interface \n Class [Class] \n Implements IInterface1(Of Integer) \n Public Sub Method1(Of U As Integer)(arg As Integer, arg1 As U) Implements IInterface1(Of Integer).Method1 \n Throw New NotImplementedException() \n End Sub \n End Class "))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementThroughFieldMember() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Private x As I \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Private x As I \n Public Sub M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementThroughFieldMember1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Private x As I \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Private x As I \n Public Sub M() Implements I.M \n x.M() \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementThroughFieldMemberRemoveUnnecessaryCast() As Task
            Await TestAsync(
"Imports System.Collections

NotInheritable Class X : Implements [|IComparer|]
    Private x As X
End Class",
"Imports System.Collections

NotInheritable Class X : Implements IComparer
    Private x As X

    Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Return Me.x.Compare(x, y)
    End Function
End Class",
index:=1)
        End Function

        <WorkItem(472, "https://github.com/dotnet/roslyn/issues/472")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementThroughFieldMemberRemoveUnnecessaryCastAndMe() As Task
            Await TestAsync(
"Imports System.Collections

NotInheritable Class X : Implements [|IComparer|]
    Private a As X
End Class",
"Imports System.Collections

NotInheritable Class X : Implements IComparer
    Private a As X

    Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Return a.Compare(x, y)
    End Function
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementThroughFieldMemberInterfaceWithNonStandardProperties() As Task
            Dim source =
<File>
Interface IFoo
    Property Blah(x As Integer) As Integer
    Default Property Blah1(x As Integer) As Integer
End Interface

Class C
    Implements [|IFoo|]
    Dim i1 As IFoo
End Class
</File>
            Dim expected =
<File>
Interface IFoo
    Property Blah(x As Integer) As Integer
    Default Property Blah1(x As Integer) As Integer
End Interface

Class C
    Implements IFoo
    Dim i1 As IFoo

    Public Property Blah(x As Integer) As Integer Implements IFoo.Blah
        Get
            Return i1.Blah(x)
        End Get
        Set(value As Integer)
            i1.Blah(x) = value
        End Set
    End Property

    Default Public Property Blah1(x As Integer) As Integer Implements IFoo.Blah1
        Get
            Return i1(x)
        End Get
        Set(value As Integer)
            i1(x) = value
        End Set
    End Property
End Class
</File>
            Await TestAsync(source, expected, index:=1)
        End Function



        <WorkItem(540355)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMissingOnImplementationWithDifferentName() As Task
            Await TestMissingAsync(
NewLines("Interface I1(Of T) \n Function Foo() As Double \n End Interface \n Class M \n Implements [|I1(Of Double)|] \n Public Function I_Foo() As Double Implements I1(Of Double).Foo \n Return 2 \n End Function \n End Class"))
        End Function

        <WorkItem(540366)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestWithMissingEndBlock() As Task
            Await TestAsync(
NewLines("Imports System \n Class M \n Implements [|IServiceProvider|]"),
NewLines("Imports System \n Class M \n Implements IServiceProvider \n Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(540367)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSimpleProperty() As Task
            Await TestAsync(
NewLines("Interface I1 \n Property Foo() As Integer \n End Interface \n Class M \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Property Foo() As Integer \n End Interface \n Class M \n Implements I1 \n Public Property Foo As Integer Implements I1.Foo \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestArrayType() As Task
            Await TestAsync(
NewLines("Interface I \n Function M() As String() \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Function M() As String() \n End Interface \n Class C \n Implements I \n Public Function M() As String() Implements I.M \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceWithByRefParameters() As Task
            Await TestAsync(
NewLines("Class C \n Implements [|I|] \n Private foo As I \n End Class \n Interface I \n Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) \n Function Method2() As Integer \n End Interface"),
NewLines("Imports System \n Class C \n Implements I \n Private foo As I \n Public Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) Implements I.Method1 \n Throw New NotImplementedException() \n End Sub \n Public Function Method2() As Integer Implements I.Method2 \n Throw New NotImplementedException() \n End Function \n End Class \n Interface I \n Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) \n Function Method2() As Integer \n End Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceWithTypeCharacter() As Task
            Await TestAsync(
NewLines("Interface I1 \n Function Method1$() \n End Interface \n Class C \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Function Method1$() \n End Interface \n Class C \n Implements I1 \n Public Function Method1() As String Implements I1.Method1 \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceWithParametersTypeSpecifiedAsTypeCharacter() As Task
            Await TestAsync(
NewLines("Interface I1 \n Sub Method1(ByRef arg#) \n End Interface \n Class C \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Sub Method1(ByRef arg#) \n End Interface \n Class C \n Implements I1 \n Public Sub Method1(ByRef arg As Double) Implements I1.Method1 \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(540403)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMissingOnInterfaceWithJustADelegate() As Task
            Await TestMissingAsync(
NewLines("Interface I1 \n Delegate Sub Del() \n End Interface \n Class C \n Implements [|I1|] \n End Class"))
        End Function

        <WorkItem(540381)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestOrdering1() As Task
            Await TestAsync(
NewLines("Class C \n Implements [|I|] \n Private foo As I \n End Class \n Interface I \n Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) \n Function Method2() As Integer \n End Interface"),
NewLines("Imports System \n Class C \n Implements I \n Private foo As I \n Public Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) Implements I.Method1 \n Throw New NotImplementedException() \n End Sub \n Public Function Method2() As Integer Implements I.Method2 \n Throw New NotImplementedException() \n End Function \n End Class \n Interface I \n Sub Method1(ByRef x As Integer, ByRef y As Integer, z As Integer) \n Function Method2() As Integer \n End Interface"))
        End Function

        <WorkItem(540415)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDefaultProperty1() As Task
            Await TestAsync(
NewLines("Interface I1 \n Default Property Foo(ByVal arg As Integer) \n End Interface \n Class C \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Default Property Foo(ByVal arg As Integer) \n End Interface \n Class C \n Implements I1 \n Default Public Property Foo(arg As Integer) As Object Implements I1.Foo \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Object) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementNestedInterface() As Task
            Await TestAsync(
NewLines("Interface I1 \n Sub Foo() \n Delegate Sub Del(ByVal arg As Integer) \n Interface I2 \n Sub Foo(ByVal arg As Del) \n End Interface \n End Interface \n Class C \n Implements [|I1.I2|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Sub Foo() \n Delegate Sub Del(ByVal arg As Integer) \n Interface I2 \n Sub Foo(ByVal arg As Del) \n End Interface \n End Interface \n Class C \n Implements I1.I2 \n Public Sub Foo(arg As I1.Del) Implements I1.I2.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(540402)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestArrayRankSpecifiers() As Task
            Await TestAsync(
NewLines("Interface I1 \n Sub Method1(ByVal arg() As Integer) \n End Interface \n Class C \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Sub Method1(ByVal arg() As Integer) \n End Interface \n Class C \n Implements I1 \n Public Sub Method1(arg() As Integer) Implements I1.Method1 \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(540398)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSimplifyImplementsClause() As Task
            Await TestAsync(
NewLines("Namespace ConsoleApplication \n Interface I1 \n Sub Method1() \n End Interface \n Class C \n Implements [|I1|] \n End Class \n End Namespace"),
NewLines("Imports System \n Namespace ConsoleApplication \n Interface I1 \n Sub Method1() \n End Interface \n Class C \n Implements I1 \n Public Sub Method1() Implements I1.Method1 \n Throw New NotImplementedException() \n End Sub \n End Class \n End Namespace"),
parseOptions:=Nothing) ' Namespaces not supported in script
        End Function

        <WorkItem(541078)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestParamArray() As Task
            Await TestAsync(
NewLines("Interface I2 \n Function G(ParamArray args As Double()) As Integer \n End Interface \n Class A \n Implements [|I2|] \n End Class"),
NewLines("Imports System \n Interface I2 \n Function G(ParamArray args As Double()) As Integer \n End Interface \n Class A \n Implements I2 \n Public Function G(ParamArray args() As Double) As Integer Implements I2.G \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(541092)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestShowForNonImplementedPrivateInterfaceMethod() As Task
            Await TestAsync(
NewLines("Interface I1 \n Private Sub Foo() \n End Interface \n Class A \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Private Sub Foo() \n End Interface \n Class A \n Implements I1 \n Public Sub Foo() Implements I1.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(541092)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDoNotShowForImplementedPrivateInterfaceMethod() As Task
            Await TestMissingAsync(
NewLines("Interface I1 \n Private Sub Foo() \n End Interface \n Class A \n Implements [|I1|] \n Public Sub Foo() Implements I1.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542010)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoImplementThroughSynthesizedFields() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Public Property X As I \n End Class"),
count:=2)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Public Property X As I \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Public Property X As I \n Public Sub M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements [|I|] \n Public Property X As I \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Class C \n Implements I \n Public Property X As I \n Public Sub M() Implements I.M \n X.M() \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIReadOnlyListThroughField() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Implements [|IReadOnlyList(Of Integer)|] \n Private field As Integer() \n End Class"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Class A \n Implements IReadOnlyList(Of Integer) \n
Private field As Integer() \n
Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of Integer).Count \n Get \n Return DirectCast(field, IReadOnlyList(Of Integer)).Count \n  End Get \n End Property \n
Default Public ReadOnly Property Item(index As Integer) As Integer Implements IReadOnlyList(Of Integer).Item \n Get \n Return DirectCast(field, IReadOnlyList(Of Integer))(index) \n End Get \n End Property \n
Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator \n Return DirectCast(field, IReadOnlyList(Of Integer)).GetEnumerator() \n End Function
Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Return DirectCast(field, IReadOnlyList(Of Integer)).GetEnumerator() \n End Function \n End Class"),
index:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIReadOnlyListThroughProperty() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Implements [|IReadOnlyList(Of Integer)|] \n Private Property field As Integer() \n End Class"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Class A \n Implements IReadOnlyList(Of Integer) \n
Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of Integer).Count \n Get \n Return DirectCast(field, IReadOnlyList(Of Integer)).Count \n  End Get \n End Property \n
Default Public ReadOnly Property Item(index As Integer) As Integer Implements IReadOnlyList(Of Integer).Item \n Get \n Return DirectCast(field, IReadOnlyList(Of Integer))(index) \n End Get \n End Property \n
Private Property field As Integer() \n
Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator \n Return DirectCast(field, IReadOnlyList(Of Integer)).GetEnumerator() \n End Function
Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Return DirectCast(field, IReadOnlyList(Of Integer)).GetEnumerator() \n End Function \n End Class"),
index:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceThroughField() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Dim x As A \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I \n Dim x As A \n Public Sub M() Implements I.M \n DirectCast(x, I).M() \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceThroughField_FieldImplementsMultipleInterfaces() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Implements I2 \n Dim x As A \n End Class"),
count:=2)

            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements I \n Implements [|I2|] \n Dim x As A \n End Class"),
count:=2)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Implements I2 \n Dim x As A \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements I \n Implements I2 \n Dim x As A \n Public Sub M() Implements I.M \n DirectCast(x, I).M() \n End Sub \n End Class"),
index:=1)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements I \n Implements [|I2|] \n Dim x As A \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I, I2 \n Public Sub M() Implements I.M, I2.M2 \n End Sub \n End Class \n
Class B \n Implements I \n Implements I2 \n Dim x As A \n Public Sub M2() Implements I2.M2 \n DirectCast(x, I2).M2() \n End Sub \n End Class"),
index:=1)
        End Function


        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceThroughField_MultipleFieldsCanImplementInterface() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Dim x As A \n Dim y As A \n End Class"),
count:=3)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Dim x As A \n Dim y As A \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I \n Dim x As A \n Dim y As A \n Public Sub M() Implements I.M \n DirectCast(x, I).M() \n End Sub \n End Class"),
index:=1)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Dim x As A \n Dim y As A \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I \n Dim x As A \n Dim y As A \n Public Sub M() Implements I.M \n DirectCast(y, I).M() \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceThroughField_MultipleFieldsForMultipleInterfaces() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements [|I|] \n Implements I2 \n Dim x As A \n Dim y as B \n End Class"),
count:=2)

            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements I \n Implements [|I2|] \n Dim x As A \n Dim y as B \n End Class"),
count:=2)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements [|I|] \n Implements I2 \n Dim x As A \n Dim y as B \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements I \n Implements I2 \n Dim x As A \n Dim y as B \n Public Sub M() Implements I.M \n DirectCast(x, I).M() \n End Sub \n End Class"),
index:=1)

            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements I \n Implements [|I2|] \n Dim x As A \n Dim y as B \n End Class"),
NewLines("Interface I \n Sub M() \n End Interface \n Interface I2 \n Sub M2() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements I2 \n Public Sub M2() Implements I2.M2 \n End Sub \n End Class \n
Class C \n Implements I \n Implements I2 \n Dim x As A \n Dim y as B \n Public Sub M2() Implements I2.M2 \n DirectCast(y, I2).M2() \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoImplementThroughDefaultProperty() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n Default ReadOnly Property x(index as Integer) As A \n Get \n Return Nothing \n End Get \n End Property \n End Class"),
count:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoImplementThroughParameterizedProperty() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n ReadOnly Property x(index as Integer) As A \n Get \n Return Nothing \n End Get \n End Property \n End Class"),
count:=1)
        End Function

        <WorkItem(768799)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoImplementThroughWriteOnlyProperty() As Task
            Await TestActionCountAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class A \n Implements I \n Public Sub M() Implements I.M \n End Sub \n End Class \n
Class B \n Implements [|I|] \n WriteOnly Property x(index as Integer) As A \n Set(value as A) \n End Set \n End Property \n End Class"),
count:=1)
        End Function

        <WorkItem(540469)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInsertBlankLineAfterImplementsAndInherits() As Task
            Await TestAsync(
<Text>Interface I1
    Function Foo()
End Interface

Class M
    Implements [|I1|]
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Interface I1
    Function Foo()
End Interface

Class M
    Implements I1

    Public Function Foo() As Object Implements I1.Foo
        Throw New NotImplementedException()
    End Function
End Class</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(542290)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMethodShadowsProperty() As Task
            Await TestAsync(
NewLines("Interface I \n Sub M() \n End Interface \n Class B \n 'Protected m As Integer \n Public Property M As Integer \n End Class \n Class C \n Inherits B \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub M() \n End Interface \n Class B \n 'Protected m As Integer \n Public Property M As Integer \n End Class \n Class C \n Inherits B \n Implements I \n Private Sub I_M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(542606)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestRemMethod() As Task
            Await TestAsync(
NewLines("Interface I \n Sub [Rem] \n End Interface \n Class C \n Implements [|I|] ' Implement interface \n End Class"),
NewLines("Imports System \n Interface I \n Sub [Rem] \n End Interface \n Class C \n Implements I ' Implement interface \n Public Sub [Rem]() Implements I.[Rem] \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(543425)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMissingIfEventAlreadyImplemented() As Task
            Await TestMissingAsync(
NewLines("Imports System.ComponentModel \n Class C \n Implements [|INotifyPropertyChanged|] \n Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged \n End Class"))
        End Function

        <WorkItem(543506)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestAddEvent1() As Task
            Await TestAsync(
NewLines("Imports System.ComponentModel \n Class C \n Implements [|INotifyPropertyChanged|] \n End Class"),
NewLines("Imports System.ComponentModel \n Class C \n Implements INotifyPropertyChanged \n Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged \n End Class"))
        End Function

        <WorkItem(543588)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNameSimplifyGenericType() As Task
            Await TestAsync(
NewLines("Interface I(Of In T, Out R) \n Sub Foo() \n End Interface \n Class C(Of T, R) \n Implements [|I(Of T, R)|] \n End Class"),
NewLines("Imports System \n Interface I(Of In T, Out R) \n Sub Foo() \n End Interface \n Class C(Of T, R) \n Implements I(Of T, R) \n Public Sub Foo() Implements I(Of T, R).Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(544156)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInterfacePropertyRedefinition() As Task
            Await TestAsync(
NewLines("Interface I1 \n Property Bar As Integer \n End Interface \n Interface I2 \n Inherits I1 \n Property Bar As Integer \n End Interface \n Class C \n Implements [|I2|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Property Bar As Integer \n End Interface \n Interface I2 \n Inherits I1 \n Property Bar As Integer \n End Interface \n Class C \n Implements I2 \n Public Property Bar As Integer Implements I2.Bar \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n Private Property I1_Bar As Integer Implements I1.Bar \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <WorkItem(544208)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMissingOnWrongArity() As Task
            Await TestMissingAsync(
NewLines("Interface I1(Of T) \n  ReadOnly Property Bar As Integer \n End Interface \n Class C \n Implements [|I1|] \n End Class"))
        End Function

        <WorkItem(529328)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestPropertyShadowing() As Task
            Await TestAsync(
NewLines("Interface I1 \n Property Bar As Integer \n Sub Foo() \n End Interface \n Class B \n Public Property Bar As Integer \n End Class \n Class C \n Inherits B \n Implements [|I1|] \n End Class"),
NewLines("Imports System \n Interface I1 \n Property Bar As Integer \n Sub Foo() \n End Interface \n Class B \n Public Property Bar As Integer \n End Class \n Class C \n Inherits B \n Implements I1 \n Private Property I1_Bar As Integer Implements I1.Bar \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n Public Sub Foo() Implements I1.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(544206)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEventWithImplicitDelegateCreation() As Task
            Await TestAsync(
NewLines("Interface I1 \n Event E(x As String) \n End Interface \n Class C \n Implements [|I1|] \n End Class"),
NewLines("Interface I1 \n Event E(x As String) \n End Interface \n Class C \n Implements I1 \n Public Event E(x As String) Implements I1.E \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestStringLiteral() As Task
            Await TestAsync(
NewLines("Interface IFoo \n Sub Foo(Optional s As String = """""""") \n End Interface \n Class Bar \n Implements [|IFoo|] \n End Class"),
NewLines("Imports System \n Interface IFoo \n Sub Foo(Optional s As String = """""""") \n End Interface \n Class Bar \n Implements IFoo \n Public Sub Foo(Optional s As String = """""""") Implements IFoo.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545643), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestVBConstantValue1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub VBNullChar(Optional x As String = Constants.vbNullChar) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub VBNullChar(Optional x As String = Constants.vbNullChar) \n End Interface \n  \n Class C \n Implements I \n Public Sub VBNullChar(Optional x As String = Constants.vbNullChar) Implements I.VBNullChar \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestVBConstantValue2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub VBNullChar(Optional x As String = Constants.vbNullChar) \n End Interface \n  \n Namespace N \n Class Microsoft \n Implements [|I|] \n End Class \n End Namespace"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub VBNullChar(Optional x As String = Constants.vbNullChar) \n End Interface \n  \n Namespace N \n Class Microsoft \n Implements I \n Public Sub VBNullChar(Optional x As String = Constants.vbNullChar) Implements I.VBNullChar \n Throw New NotImplementedException() \n End Sub \n End Class \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(545679), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestVBConstantValue3() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub ChrW(Optional x As String = Strings.ChrW(1)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub ChrW(Optional x As String = Strings.ChrW(1)) \n End Interface \n  \n Class C \n Implements I \n Public Sub ChrW(Optional x As String = Strings.ChrW(1)) Implements I.ChrW \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545674)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDateTimeLiteral1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Date = #6/29/2012#) \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Date = #6/29/2012#) \n End Interface \n Class C \n Implements I \n Public Sub Foo(Optional x As Date = #6/29/2012 12:00:00 AM#) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545675), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEnumConstant1() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek = DayOfWeek.Friday) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek = DayOfWeek.Friday) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As DayOfWeek = DayOfWeek.Friday) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545644)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMultiDimensionalArray1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(x As Integer()()) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(x As Integer()()) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(x As Integer()()) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545640), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestUnicodeQuote() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(8220)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(8220)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As String = ChrW(8220)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545563)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestLongMinValue() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Long = Long.MinValue) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Long = Long.MinValue) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Long = Long.MinValue) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMinMaxValues() As Task
            Await TestAsync(
<Text>Interface I
    Sub M01(Optional x As Short = Short.MinValue)
    Sub M02(Optional x As Short = Short.MaxValue)
    Sub M03(Optional x As UShort = UShort.MinValue)
    Sub M04(Optional x As UShort = UShort.MaxValue)
    Sub M05(Optional x As Integer = Integer.MinValue)
    Sub M06(Optional x As Integer = Integer.MaxValue)
    Sub M07(Optional x As UInteger = UInteger.MinValue)
    Sub M08(Optional x As UInteger = UInteger.MaxValue)
    Sub M09(Optional x As Long = Long.MinValue)
    Sub M10(Optional x As Long = Long.MaxValue)
    Sub M11(Optional x As ULong = ULong.MinValue)
    Sub M12(Optional x As ULong = ULong.MaxValue)
End Interface

Class C
    Implements [|I|]
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Interface I
    Sub M01(Optional x As Short = Short.MinValue)
    Sub M02(Optional x As Short = Short.MaxValue)
    Sub M03(Optional x As UShort = UShort.MinValue)
    Sub M04(Optional x As UShort = UShort.MaxValue)
    Sub M05(Optional x As Integer = Integer.MinValue)
    Sub M06(Optional x As Integer = Integer.MaxValue)
    Sub M07(Optional x As UInteger = UInteger.MinValue)
    Sub M08(Optional x As UInteger = UInteger.MaxValue)
    Sub M09(Optional x As Long = Long.MinValue)
    Sub M10(Optional x As Long = Long.MaxValue)
    Sub M11(Optional x As ULong = ULong.MinValue)
    Sub M12(Optional x As ULong = ULong.MaxValue)
End Interface

Class C
    Implements I

    Public Sub M01(Optional x As Short = Short.MinValue) Implements I.M01
        Throw New NotImplementedException()
    End Sub

    Public Sub M02(Optional x As Short = Short.MaxValue) Implements I.M02
        Throw New NotImplementedException()
    End Sub

    Public Sub M03(Optional x As UShort = 0) Implements I.M03
        Throw New NotImplementedException()
    End Sub

    Public Sub M04(Optional x As UShort = UShort.MaxValue) Implements I.M04
        Throw New NotImplementedException()
    End Sub

    Public Sub M05(Optional x As Integer = Integer.MinValue) Implements I.M05
        Throw New NotImplementedException()
    End Sub

    Public Sub M06(Optional x As Integer = Integer.MaxValue) Implements I.M06
        Throw New NotImplementedException()
    End Sub

    Public Sub M07(Optional x As UInteger = 0) Implements I.M07
        Throw New NotImplementedException()
    End Sub

    Public Sub M08(Optional x As UInteger = UInteger.MaxValue) Implements I.M08
        Throw New NotImplementedException()
    End Sub

    Public Sub M09(Optional x As Long = Long.MinValue) Implements I.M09
        Throw New NotImplementedException()
    End Sub

    Public Sub M10(Optional x As Long = Long.MaxValue) Implements I.M10
        Throw New NotImplementedException()
    End Sub

    Public Sub M11(Optional x As ULong = 0) Implements I.M11
        Throw New NotImplementedException()
    End Sub

    Public Sub M12(Optional x As ULong = ULong.MaxValue) Implements I.M12
        Throw New NotImplementedException()
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestFloatConstants() As Task
            Await TestAsync(
<Text>Interface I
    Sub D1(Optional x As Double = Double.Epsilon)
    Sub D2(Optional x As Double = Double.MaxValue)
    Sub D3(Optional x As Double = Double.MinValue)
    Sub D4(Optional x As Double = Double.NaN)
    Sub D5(Optional x As Double = Double.NegativeInfinity)
    Sub D6(Optional x As Double = Double.PositiveInfinity)
    Sub S1(Optional x As Single = Single.Epsilon)
    Sub S2(Optional x As Single = Single.MaxValue)
    Sub S3(Optional x As Single = Single.MinValue)
    Sub S4(Optional x As Single = Single.NaN)
    Sub S5(Optional x As Single = Single.NegativeInfinity)
    Sub S6(Optional x As Single = Single.PositiveInfinity)
End Interface

Class C
    Implements [|I|]
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Interface I
    Sub D1(Optional x As Double = Double.Epsilon)
    Sub D2(Optional x As Double = Double.MaxValue)
    Sub D3(Optional x As Double = Double.MinValue)
    Sub D4(Optional x As Double = Double.NaN)
    Sub D5(Optional x As Double = Double.NegativeInfinity)
    Sub D6(Optional x As Double = Double.PositiveInfinity)
    Sub S1(Optional x As Single = Single.Epsilon)
    Sub S2(Optional x As Single = Single.MaxValue)
    Sub S3(Optional x As Single = Single.MinValue)
    Sub S4(Optional x As Single = Single.NaN)
    Sub S5(Optional x As Single = Single.NegativeInfinity)
    Sub S6(Optional x As Single = Single.PositiveInfinity)
End Interface

Class C
    Implements I

    Public Sub D1(Optional x As Double = Double.Epsilon) Implements I.D1
        Throw New NotImplementedException()
    End Sub

    Public Sub D2(Optional x As Double = Double.MaxValue) Implements I.D2
        Throw New NotImplementedException()
    End Sub

    Public Sub D3(Optional x As Double = Double.MinValue) Implements I.D3
        Throw New NotImplementedException()
    End Sub

    Public Sub D4(Optional x As Double = Double.NaN) Implements I.D4
        Throw New NotImplementedException()
    End Sub

    Public Sub D5(Optional x As Double = Double.NegativeInfinity) Implements I.D5
        Throw New NotImplementedException()
    End Sub

    Public Sub D6(Optional x As Double = Double.PositiveInfinity) Implements I.D6
        Throw New NotImplementedException()
    End Sub

    Public Sub S1(Optional x As Single = Single.Epsilon) Implements I.S1
        Throw New NotImplementedException()
    End Sub

    Public Sub S2(Optional x As Single = Single.MaxValue) Implements I.S2
        Throw New NotImplementedException()
    End Sub

    Public Sub S3(Optional x As Single = Single.MinValue) Implements I.S3
        Throw New NotImplementedException()
    End Sub

    Public Sub S4(Optional x As Single = Single.NaN) Implements I.S4
        Throw New NotImplementedException()
    End Sub

    Public Sub S5(Optional x As Single = Single.NegativeInfinity) Implements I.S5
        Throw New NotImplementedException()
    End Sub

    Public Sub S6(Optional x As Single = Single.PositiveInfinity) Implements I.S6
        Throw New NotImplementedException()
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEnumParameters() As Task
            Await TestAsync(
<Text><![CDATA[Imports System

Enum E
    A = 1
    B = 2
End Enum

<FlagsAttribute>
Enum FlagE
    A = 1
    B = 2
End Enum

Interface I
    Sub M1(Optional e As E = E.A Or E.B)
    Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B)
End Interface

Class C
    Implements [|I|]
End Class]]></Text>.Value.Replace(vbLf, vbCrLf),
<Text><![CDATA[Imports System

Enum E
    A = 1
    B = 2
End Enum

<FlagsAttribute>
Enum FlagE
    A = 1
    B = 2
End Enum

Interface I
    Sub M1(Optional e As E = E.A Or E.B)
    Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B)
End Interface

Class C
    Implements I

    Public Sub M1(Optional e As E = 3) Implements I.M1
        Throw New NotImplementedException()
    End Sub

    Public Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B) Implements I.M2
        Throw New NotImplementedException()
    End Sub
End Class]]></Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEnumParameters2() As Task
            Await TestAsync(
<Text><![CDATA[
Option Strict On
Imports System

Enum E
    A = 1
    B = 2
End Enum

<FlagsAttribute>
Enum FlagE
    A = 1
    B = 2
End Enum

Interface I
    Sub M1(Optional e As E = E.A Or E.B)
    Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B)
End Interface

Class C
    Implements [|I|]
End Class]]></Text>.Value.Replace(vbLf, vbCrLf),
<Text><![CDATA[
Option Strict On
Imports System

Enum E
    A = 1
    B = 2
End Enum

<FlagsAttribute>
Enum FlagE
    A = 1
    B = 2
End Enum

Interface I
    Sub M1(Optional e As E = E.A Or E.B)
    Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B)
End Interface

Class C
    Implements I

    Public Sub M1(Optional e As E = CType(3, E)) Implements I.M1
        Throw New NotImplementedException()
    End Sub

    Public Sub M2(Optional e As FlagE = FlagE.A Or FlagE.B) Implements I.M2
        Throw New NotImplementedException()
    End Sub
End Class]]></Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(545691)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestMultiDimArray1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(x As Integer(,)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(x As Integer(,)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(x(,) As Integer) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545640), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestQuoteEscaping1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = ChrW(8220)) \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = ChrW(8220)) \n End Interface \n Class C \n Implements I \n Public Sub Foo(Optional x As Char = ChrW(8220)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545866)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestQuoteEscaping2() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Object = ""‟"") \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Object = ""‟"") \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = ""‟"") Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545689)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDecimalLiteral1() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = Decimal.MaxValue) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = Decimal.MaxValue) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Decimal = Decimal.MaxValue) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545687), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestRemoveParenthesesAroundTypeReference1() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek = DayOfWeek.Monday) \n End Interface \n  \n Class C \n Implements [|I|] \n  \n Property DayOfWeek As DayOfWeek \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek = DayOfWeek.Monday) \n End Interface \n  \n Class C \n Implements I \n  \n Property DayOfWeek As DayOfWeek \n Public Sub Foo(Optional x As DayOfWeek = DayOfWeek.Monday) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545694), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNullableDefaultValue1() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek? = DayOfWeek.Friday) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As DayOfWeek? = DayOfWeek.Friday) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As DayOfWeek? = DayOfWeek.Friday) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545688)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestHighPrecisionDouble() As Task
            Await TestAsync(
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Double = 2.8025969286496341E-45) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Double = 2.8025969286496341E-45) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Double = 2.8025969286496341E-45) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545729), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCharSurrogates() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = ChrW(55401)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = ChrW(55401)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Char = ChrW(55401)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545733), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestReservedChar() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = Chr(13)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Char = Chr(13)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Char = ChrW(13)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545685)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCastEnumValue() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As ConsoleColor = CType(-1, ConsoleColor)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As ConsoleColor = CType(-1, ConsoleColor)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As ConsoleColor = CType(-1, ConsoleColor)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545756)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestArrayOfNullables() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(x As Integer?()) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(x As Integer?()) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(x As Integer?()) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545753)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestOptionalArrayParameterWithDefault() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Integer() = Nothing) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Integer() = Nothing) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x() As Integer = Nothing) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545742), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestRemFieldEnum() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Enum E \n [Rem] \n End Enum \n  \n Interface I \n Sub Foo(Optional x As E = E.[Rem]) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Enum E \n [Rem] \n End Enum \n  \n Interface I \n Sub Foo(Optional x As E = E.[Rem]) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As E = E.[Rem]) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545790)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestByteParameter() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Byte = 1) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Byte = 1) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Byte = 1) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545789)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDefaultParameterSuffix1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Object = 1L) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Object = 1L) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = 1L) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545809)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestZeroValuedEnum() As Task
            Await TestAsync(
NewLines("Enum E \n A = 1 \n End Enum \n  \n Interface I \n Sub Foo(Optional x As E = 0) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Enum E \n A = 1 \n End Enum \n  \n Interface I \n Sub Foo(Optional x As E = 0) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As E = 0) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545824)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestByteCast() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As Object = CByte(1)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Object = CByte(1)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = CByte(1)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545825)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDecimalValues() As Task
            Await TestAsync(
<Text>Option Strict On

Interface I
    Sub M1(Optional x As Decimal = 2D)
    Sub M2(Optional x As Decimal = 2.0D)
    Sub M3(Optional x As Decimal = 0D)
    Sub M4(Optional x As Decimal = 0.0D)
    Sub M5(Optional x As Decimal = 0.1D)
    Sub M6(Optional x As Decimal = 0.10D)
End Interface
 
Class C
    Implements [|I|]
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Option Strict On
Imports System

Interface I
    Sub M1(Optional x As Decimal = 2D)
    Sub M2(Optional x As Decimal = 2.0D)
    Sub M3(Optional x As Decimal = 0D)
    Sub M4(Optional x As Decimal = 0.0D)
    Sub M5(Optional x As Decimal = 0.1D)
    Sub M6(Optional x As Decimal = 0.10D)
End Interface
 
Class C
    Implements I

    Public Sub M1(Optional x As Decimal = 2) Implements I.M1
        Throw New NotImplementedException()
    End Sub

    Public Sub M2(Optional x As Decimal = 2.0D) Implements I.M2
        Throw New NotImplementedException()
    End Sub

    Public Sub M3(Optional x As Decimal = 0) Implements I.M3
        Throw New NotImplementedException()
    End Sub

    Public Sub M4(Optional x As Decimal = 0.0D) Implements I.M4
        Throw New NotImplementedException()
    End Sub

    Public Sub M5(Optional x As Decimal = 0.1D) Implements I.M5
        Throw New NotImplementedException()
    End Sub

    Public Sub M6(Optional x As Decimal = 0.10D) Implements I.M6
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(545693)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSmallDecimal() As Task
            Await TestAsync(
NewLines("Option Strict On \n  \n Interface I \n Sub Foo(Optional x As Decimal = Long.MinValue) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = Long.MinValue) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Decimal = -9223372036854775808D) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545771)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEventConflict() As Task
            Await TestAsync(
NewLines("Interface IA \n Event E As EventHandler \n End Interface \n Interface IB \n Inherits IA \n Shadows Event E As Action \n End Interface \n Class C \n Implements [|IB|] \n End Class"),
NewLines("Interface IA \n Event E As EventHandler \n End Interface \n Interface IB \n Inherits IA \n Shadows Event E As Action \n End Interface \n Class C \n Implements IB \n Public Event E As Action Implements IB.E \n Private Event IA_E As EventHandler Implements IA.E \n End Class"))
        End Function

        <WorkItem(545826)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDecimalField() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Object = 1D) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Object = 1D) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = 1D) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545827)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDoubleInObjectContext() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Object = 1.0) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Object = 1.0) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = 1R) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545860)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestLargeDecimal() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = 10000000000000000000D) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = 10000000000000000000D) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Decimal = 10000000000000000000D) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545870)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSurrogatePair1() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Optional x As String = ""𪛖"") \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As String = ""𪛖"") \n End Interface \n Class C \n Implements I \n Public Sub Foo(Optional x As String = ""𪛖"") Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545893), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestVBTab() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = vbTab) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = vbTab) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As String = vbTab) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545912)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestEscapeTypeParameter() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Of [TO], TP, TQ)() \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Of [TO], TP, TQ)() \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Of [TO], TP, TQ)() Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545892)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestLargeUnsignedLong() As Task
            Await TestAsync(
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As ULong = 10000000000000000000UL) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As ULong = 10000000000000000000UL) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As ULong = 10000000000000000000UL) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545865)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSmallDecimalValues() As Task
            Dim markup =
<File>
Option Strict On

Interface I
    Sub F1(Optional x As Decimal = 1E-25D)
    Sub F2(Optional x As Decimal = 1E-26D)
    Sub F3(Optional x As Decimal = 1E-27D)
    Sub F4(Optional x As Decimal = 1E-28D)
    Sub F5(Optional x As Decimal = 1E-29D)
    Sub M1(Optional x As Decimal = 1.1E-25D)
    Sub M2(Optional x As Decimal = 1.1E-26D)
    Sub M3(Optional x As Decimal = 1.1E-27D)
    Sub M4(Optional x As Decimal = 1.1E-28D)
    Sub M5(Optional x As Decimal = 1.1E-29D)
    Sub S1(Optional x As Decimal = -1E-25D)
    Sub S2(Optional x As Decimal = -1E-26D)
    Sub S3(Optional x As Decimal = -1E-27D)
    Sub S4(Optional x As Decimal = -1E-28D)
    Sub S5(Optional x As Decimal = -1E-29D)
    Sub T1(Optional x As Decimal = -1.1E-25D)
    Sub T2(Optional x As Decimal = -1.1E-26D)
    Sub T3(Optional x As Decimal = -1.1E-27D)
    Sub T4(Optional x As Decimal = -1.1E-28D)
    Sub T5(Optional x As Decimal = -1.1E-29D)
End Interface

Class C
    Implements [|I|]
End Class
</File>

            Dim expected =
<File>
Option Strict On
Imports System

Interface I
    Sub F1(Optional x As Decimal = 1E-25D)
    Sub F2(Optional x As Decimal = 1E-26D)
    Sub F3(Optional x As Decimal = 1E-27D)
    Sub F4(Optional x As Decimal = 1E-28D)
    Sub F5(Optional x As Decimal = 1E-29D)
    Sub M1(Optional x As Decimal = 1.1E-25D)
    Sub M2(Optional x As Decimal = 1.1E-26D)
    Sub M3(Optional x As Decimal = 1.1E-27D)
    Sub M4(Optional x As Decimal = 1.1E-28D)
    Sub M5(Optional x As Decimal = 1.1E-29D)
    Sub S1(Optional x As Decimal = -1E-25D)
    Sub S2(Optional x As Decimal = -1E-26D)
    Sub S3(Optional x As Decimal = -1E-27D)
    Sub S4(Optional x As Decimal = -1E-28D)
    Sub S5(Optional x As Decimal = -1E-29D)
    Sub T1(Optional x As Decimal = -1.1E-25D)
    Sub T2(Optional x As Decimal = -1.1E-26D)
    Sub T3(Optional x As Decimal = -1.1E-27D)
    Sub T4(Optional x As Decimal = -1.1E-28D)
    Sub T5(Optional x As Decimal = -1.1E-29D)
End Interface

Class C
    Implements I

    Public Sub F1(Optional x As Decimal = 0.0000000000000000000000001D) Implements I.F1
        Throw New NotImplementedException()
    End Sub

    Public Sub F2(Optional x As Decimal = 0.00000000000000000000000001D) Implements I.F2
        Throw New NotImplementedException()
    End Sub

    Public Sub F3(Optional x As Decimal = 0.000000000000000000000000001D) Implements I.F3
        Throw New NotImplementedException()
    End Sub

    Public Sub F4(Optional x As Decimal = 0.0000000000000000000000000001D) Implements I.F4
        Throw New NotImplementedException()
    End Sub

    Public Sub F5(Optional x As Decimal = 0.0000000000000000000000000000D) Implements I.F5
        Throw New NotImplementedException()
    End Sub

    Public Sub M1(Optional x As Decimal = 0.00000000000000000000000011D) Implements I.M1
        Throw New NotImplementedException()
    End Sub

    Public Sub M2(Optional x As Decimal = 0.000000000000000000000000011D) Implements I.M2
        Throw New NotImplementedException()
    End Sub

    Public Sub M3(Optional x As Decimal = 0.0000000000000000000000000011D) Implements I.M3
        Throw New NotImplementedException()
    End Sub

    Public Sub M4(Optional x As Decimal = 0.0000000000000000000000000001D) Implements I.M4
        Throw New NotImplementedException()
    End Sub

    Public Sub M5(Optional x As Decimal = 0.0000000000000000000000000000D) Implements I.M5
        Throw New NotImplementedException()
    End Sub

    Public Sub S1(Optional x As Decimal = -0.0000000000000000000000001D) Implements I.S1
        Throw New NotImplementedException()
    End Sub

    Public Sub S2(Optional x As Decimal = -0.00000000000000000000000001D) Implements I.S2
        Throw New NotImplementedException()
    End Sub

    Public Sub S3(Optional x As Decimal = -0.000000000000000000000000001D) Implements I.S3
        Throw New NotImplementedException()
    End Sub

    Public Sub S4(Optional x As Decimal = -0.0000000000000000000000000001D) Implements I.S4
        Throw New NotImplementedException()
    End Sub

    Public Sub S5(Optional x As Decimal = 0.0000000000000000000000000000D) Implements I.S5
        Throw New NotImplementedException()
    End Sub

    Public Sub T1(Optional x As Decimal = -0.00000000000000000000000011D) Implements I.T1
        Throw New NotImplementedException()
    End Sub

    Public Sub T2(Optional x As Decimal = -0.000000000000000000000000011D) Implements I.T2
        Throw New NotImplementedException()
    End Sub

    Public Sub T3(Optional x As Decimal = -0.0000000000000000000000000011D) Implements I.T3
        Throw New NotImplementedException()
    End Sub

    Public Sub T4(Optional x As Decimal = -0.0000000000000000000000000001D) Implements I.T4
        Throw New NotImplementedException()
    End Sub

    Public Sub T5(Optional x As Decimal = 0.0000000000000000000000000000D) Implements I.T5
        Throw New NotImplementedException()
    End Sub
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(544641)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestClassStatementTerminators1() As Task
            Await TestAsync(
NewLines("Imports System \n Class C : Implements [|IServiceProvider|] : End Class"),
NewLines("Imports System \n Class C : Implements IServiceProvider \n Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(544641)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestClassStatementTerminators2() As Task
            Await TestAsync(
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class C : Inherits D : Implements [|IServiceProvider|] : End Class"),
NewLines("Imports System \n MustInherit Class D \n MustOverride Sub Foo() \n End Class \n Class C : Inherits D : Implements IServiceProvider \n Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(544652), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestConvertNonprintableCharToString() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Object = CStr(Chr(1))) \n End Interface \n  \n Class C \n Implements [|I|] ' Implement \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As Object = CStr(Chr(1))) \n End Interface \n  \n Class C \n Implements I ' Implement \n Public Sub Foo(Optional x As Object = CStr(ChrW(1))) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545684), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSimplifyModuleNameWhenPossible1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(1)) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(1)) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As String = ChrW(1)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545684), WorkItem(715013)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestSimplifyModuleNameWhenPossible2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(1)) \n End Interface \n  \n Class C \n Implements [|I|] \n Public Sub ChrW(x As Integer) \n End Sub \n End Class"),
NewLines("Imports System \n Imports Microsoft.VisualBasic \n Interface I \n Sub Foo(Optional x As String = ChrW(1)) \n End Interface \n  \n Class C \n Implements I \n Public Sub ChrW(x As Integer) \n End Sub \n Public Sub Foo(Optional x As String = Strings.ChrW(1)) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(544676)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDoubleWideREM() As Task
            Await TestAsync(
NewLines("Interface I \n Sub ［ＲＥＭ］() \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub ［ＲＥＭ］() \n End Interface \n  \n Class C \n Implements I \n Public Sub [ＲＥＭ]() Implements I.[ＲＥＭ] \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545917)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDoubleWideREM2() As Task
            Await TestAsync(
NewLines("Interface I \n Sub Foo(Of ［ＲＥＭ］)() \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Of ［ＲＥＭ］)() \n End Interface \n Class C \n Implements I \n  \n Public Sub Foo(Of [ＲＥＭ])() Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545953)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestGenericEnumWithRenamedTypeParameters1() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class C(Of T) \n Enum E \n X \n End Enum \n End Class \n Interface I \n Sub Foo(Of M)(Optional x As C(Of M()).E = C(Of M()).E.X) \n End Interface \n Class C \n Implements [|I|] ' Implement \n End Class"),
NewLines("Option Strict On \n Imports System \n Class C(Of T) \n Enum E \n X \n End Enum \n End Class \n Interface I \n Sub Foo(Of M)(Optional x As C(Of M()).E = C(Of M()).E.X) \n End Interface \n Class C \n Implements I ' Implement \n Public Sub Foo(Of M)(Optional x As C(Of M()).E = C(Of M()).E.X) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(545953)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestGenericEnumWithRenamedTypeParameters2() As Task
            Await TestAsync(
NewLines("Option Strict On \n Class C(Of T) \n Enum E \n X \n End Enum \n End Class \n Interface I \n Sub Foo(Of T)(Optional x As C(Of T()).E = C(Of T()).E.X) \n End Interface \n Class C \n Implements [|I|] \n End Class"),
NewLines("Option Strict On \n Imports System \n Class C(Of T) \n Enum E \n X \n End Enum \n End Class \n Interface I \n Sub Foo(Of T)(Optional x As C(Of T()).E = C(Of T()).E.X) \n End Interface \n Class C \n Implements I \n Public Sub Foo(Of T)(Optional x As C(Of T()).E = C(Of T()).E.X) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(546197)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDoubleQuoteChar() As Task
            Await TestAsync(
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Object = """"""""c) \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n Sub Foo(Optional x As Object = """"""""c) \n End Interface \n  \n Class C \n Implements I \n Public Sub Foo(Optional x As Object = """"""""c) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(530165)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestGenerateIntoAppropriatePartial() As Task
            Await TestAsync(
NewLines("Interface I \n  \n Sub M() \n  \n End Interface \n  \n Class C \n  \n End Class \n  \n Partial Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n  \n Sub M() \n  \n End Interface \n  \n Class C \n  \n End Class \n  \n Partial Class C \n Implements I \n Public Sub M() Implements I.M \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(546325)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestAttributes() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.InteropServices \n  \n Interface I \n Function Foo(<MarshalAs(UnmanagedType.U1)> x As Boolean) As <MarshalAs(UnmanagedType.U1)> Boolean \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports System.Runtime.InteropServices \n  \n Interface I \n Function Foo(<MarshalAs(UnmanagedType.U1)> x As Boolean) As <MarshalAs(UnmanagedType.U1)> Boolean \n End Interface \n  \n Class C \n Implements I \n Public Function Foo(<MarshalAs(UnmanagedType.U1)> \n x As Boolean) As <MarshalAs(UnmanagedType.U1)> \n Boolean Implements I.Foo \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(530564)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestShortenedDecimal() As Task
            Await TestAsync(
NewLines("Option Strict On \n Interface I \n Sub Foo(Optional x As Decimal = 1000000000000000000D) \n End Interface \n Class C \n Implements [|I|] ' Implement \n End Class"),
NewLines("Option Strict On \n Imports System \n Interface I \n Sub Foo(Optional x As Decimal = 1000000000000000000D) \n End Interface \n Class C \n Implements I ' Implement \n Public Sub Foo(Optional x As Decimal = 1000000000000000000) Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(530713)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementAbstractly2() As Task
            Await TestAsync(
NewLines("Interface I \n Property Foo() As Integer \n End Interface \n  \n MustInherit Class C \n Implements [|I|] ' Implement interface abstractly \n End Class"),
NewLines("Imports System \n Interface I \n Property Foo() As Integer \n End Interface \n  \n MustInherit Class C \n Implements I ' Implement interface abstractly \n Public Property Foo As Integer Implements I.Foo \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <WorkItem(916114)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestOptionalNullableStructParameter() As Task
            Await TestAsync(
NewLines("Interface I \n ReadOnly Property g(Optional x As S? = Nothing) \n End Interface \n Class c \n Implements [|I|] \n End Class \n Structure S \n End Structure"),
NewLines("Imports System \n Interface I \n ReadOnly Property g(Optional x As S? = Nothing) \n End Interface \n Class c \n Implements I \n Public ReadOnly Property g(Optional x As S? = Nothing) As Object Implements I.g \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class \n Structure S \n End Structure"))
        End Function

        <WorkItem(916114)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestOptionalNullableLongParameter() As Task
            Await TestAsync(
NewLines("Interface I \n ReadOnly Property g(Optional x As Long? = Nothing, Optional y As Long? = 5) \n End Interface \n Class c \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Interface I \n ReadOnly Property g(Optional x As Long? = Nothing, Optional y As Long? = 5) \n End Interface \n Class c \n Implements I \n Public ReadOnly Property g(Optional x As Long? = Nothing, Optional y As Long? = 5) As Object Implements I.g \n Get \n Throw New NotImplementedException() \n End Get \n End Property \n End Class"))
        End Function

        <WorkItem(530345)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestAttributeFormattingInNonStatementContext() As Task
            Await TestAsync(
<Text>Imports System.Runtime.InteropServices

Interface I
    Function Foo(&lt;MarshalAs(UnmanagedType.U1)&gt; x As Boolean) As &lt;MarshalAs(UnmanagedType.U1)&gt; Boolean
End Interface

Class C
    Implements [|I|] ' Implement
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System
Imports System.Runtime.InteropServices

Interface I
    Function Foo(&lt;MarshalAs(UnmanagedType.U1)&gt; x As Boolean) As &lt;MarshalAs(UnmanagedType.U1)&gt; Boolean
End Interface

Class C
    Implements I ' Implement

    Public Function Foo(&lt;MarshalAs(UnmanagedType.U1)&gt; x As Boolean) As &lt;MarshalAs(UnmanagedType.U1)&gt; Boolean Implements I.Foo
        Throw New NotImplementedException()
    End Function
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(546779)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestPropertyReturnTypeAttributes() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.InteropServices \n  \n Interface I \n Property P(<MarshalAs(UnmanagedType.I4)> x As Integer) As <MarshalAs(UnmanagedType.I4)> Integer \n End Interface \n  \n Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports System.Runtime.InteropServices \n  \n Interface I \n Property P(<MarshalAs(UnmanagedType.I4)> x As Integer) As <MarshalAs(UnmanagedType.I4)> Integer \n End Interface \n  \n Class C \n Implements I \n Public Property P(<MarshalAs(UnmanagedType.I4)> x As Integer) As <MarshalAs(UnmanagedType.I4)> Integer Implements I.P \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n End Property \n End Class"))
        End Function

        <WorkItem(847464)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceForPartialType() As Task
            Await TestAsync(
NewLines("Public Interface I \n Sub Foo() \n End Interface \n Partial Class C \n End Class \n Partial Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Public Interface I \n Sub Foo() \n End Interface \n Partial Class C \n End Class \n Partial Class C \n Implements I \n Public Sub Foo() Implements I.Foo \n Throw New NotImplementedException() \n End Sub \n End Class"))
        End Function

        <WorkItem(617698)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestBugfix_617698_RecursiveSimplificationOfQualifiedName() As Task
            Await TestAsync(
<Text>Interface A(Of B)
    Sub M()
    Interface C(Of D)
        Inherits A(Of C(Of D))
        Interface E
            Inherits C(Of E)
            Class D
                Implements [|E|]
            End Class
        End Interface
    End Interface
End Interface
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System

Interface A(Of B)
    Sub M()
    Interface C(Of D)
        Inherits A(Of C(Of D))
        Interface E
            Inherits C(Of E)
            Class D
                Implements E

                Public Sub M() Implements A(Of A(Of A(Of A(Of B).C(Of D)).C(Of A(Of B).C(Of D).E)).C(Of A(Of A(Of B).C(Of D)).C(Of A(Of B).C(Of D).E).E)).M
                    Throw New NotImplementedException()
                End Sub
            End Class
        End Interface
    End Interface
End Interface
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceForIDisposable() As Task
            Await TestAsync(
<Text>Imports System
Class Program
    Implements [|IDisposable|]

End Class
</Text>.Value.Replace(vbLf, vbCrLf),
$"Imports System
Class Program
    Implements IDisposable
{DisposePattern("Overridable ")}

End Class
",
index:=1,
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceForIDisposableNonApplicable1() As Task
            Await TestAsync(
<Text>Imports System
Class Program
    Implements [|IDisposable|]

    Private DisposedValue As Boolean

End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System
Class Program
    Implements IDisposable

    Private DisposedValue As Boolean

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceForIDisposableNonApplicable2() As Task
            Await TestAsync(
<Text>Imports System
Class Program
    Implements [|IDisposable|]

    Public Sub Dispose(flag As Boolean)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System
Class Program
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub

    Public Sub Dispose(flag As Boolean)
    End Sub
End Class
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceForIDisposableWithSealedClass() As Task
            Await TestAsync(
<Text>Imports System
Public NotInheritable Class Program
    Implements [|IDisposable|]

End Class
</Text>.Value.Replace(vbLf, vbCrLf),
$"Imports System
Public NotInheritable Class Program
    Implements IDisposable
{DisposePattern("")}

End Class
",
index:=1,
compareTokens:=False)
        End Function

        <WorkItem(939123)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoComAliasNameAttributeOnMethodParameters() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.InteropServices \n
Interface I \n Function F(<ComAliasName(""pAlias"")> p As Long) As Integer \n End Interface \n
MustInherit Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports System.Runtime.InteropServices \n
Interface I \n Function F(<ComAliasName(""pAlias"")> p As Long) As Integer \n End Interface \n
MustInherit Class C \n Implements I \n
Public Function F(p As Long) As Integer Implements I.F \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(939123)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoComAliasNameAttributeOnMethodReturnType() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.InteropServices \n
Interface I \n Function F(<ComAliasName(""pAlias1"")> p As Long) As <ComAliasName(""pAlias2"")> Integer \n End Interface \n
MustInherit Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports System.Runtime.InteropServices \n
Interface I \n Function F(<ComAliasName(""pAlias1"")> p As Long) As <ComAliasName(""pAlias2"")> Integer \n End Interface \n
MustInherit Class C \n Implements I \n
Public Function F(p As Long) As Integer Implements I.F \n Throw New NotImplementedException() \n End Function \n End Class"))
        End Function

        <WorkItem(939123)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNoComAliasNameAttributeOnPropertyParameters() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.InteropServices \n
Interface I \n Default Property Prop(<ComAliasName(""pAlias"")> p As Long) As Integer \n End Interface \n
Class C \n Implements [|I|] \n End Class"),
NewLines("Imports System \n Imports System.Runtime.InteropServices \n
Interface I \n Default Property Prop(<ComAliasName(""pAlias"")> p As Long) As Integer \n End Interface \n
Class C \n Implements I \n
Default Public Property Prop(p As Long) As Integer Implements I.Prop \n
Get \n Throw New NotImplementedException() \n End Get \n
Set(value As Integer) \n Throw New NotImplementedException() \n End Set \n
End Property \n End Class"))
        End Function

        <WorkItem(529920)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestNewLineBeforeDirective() As Task
            Await TestAsync(
"Imports System
Class C 
    Implements [|IServiceProvider|]
#Disable Warning",
"Imports System
Class C 
    Implements IServiceProvider

    Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
        Throw New NotImplementedException()
    End Function
End Class
#Disable Warning", compareTokens:=False)
        End Function

        <WorkItem(529947)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCommentAfterInterfaceList1() As Task
            Await TestAsync(
"Imports System
Class C 
    Implements [|IServiceProvider|] REM Comment",
"Imports System
Class C 
    Implements IServiceProvider REM Comment

    Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
        Throw New NotImplementedException()
    End Function
End Class
", compareTokens:=False)
        End Function

        <WorkItem(529947)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestCommentAfterInterfaceList2() As Task
            Await TestAsync(
"Imports System
Class C
    Implements [|IServiceProvider|]
REM Comment",
"Imports System
Class C
    Implements IServiceProvider

    Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
        Throw New NotImplementedException()
    End Function
End Class
REM Comment", compareTokens:=False)
        End Function

        <WorkItem(994456)>
        <WorkItem(958699)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposable_NoDisposePattern() As Task
            Await TestAsync(
"Imports System
Class C : Implements [|IDisposable|]",
"Imports System
Class C : Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Class
", index:=0, compareTokens:=False)
        End Function

        <WorkItem(994456)>
        <WorkItem(958699)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposable1_DisposePattern() As Task
            Await TestAsync(
"Imports System
Class C : Implements [|IDisposable|]",
$"Imports System
Class C : Implements IDisposable
{DisposePattern("Overridable ")}
End Class
", index:=1, compareTokens:=False)
        End Function

        <WorkItem(994456)>
        <WorkItem(958699)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposableAbstractly_NoDisposePattern() As Task
            Await TestAsync(
"Imports System
MustInherit Class C : Implements [|IDisposable|]",
"Imports System
MustInherit Class C : Implements IDisposable

    Public MustOverride Sub Dispose() Implements IDisposable.Dispose
End Class
", index:=2, compareTokens:=False)
        End Function

        <WorkItem(994456)>
        <WorkItem(958699)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposableThroughMember_NoDisposePattern() As Task
            Await TestAsync(
"Imports System
Class C : Implements [|IDisposable|]
    Dim foo As IDisposable
End Class",
"Imports System
Class C : Implements IDisposable
    Dim foo As IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        foo.Dispose()
    End Sub
End Class", index:=2, compareTokens:=False)
        End Function

        <WorkItem(941469)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposable2() As Task
            Await TestAsync(
"Imports System
Class C : Implements [|System.IDisposable|]
    Class IDisposable
    End Class
End Class",
$"Imports System
Class C : Implements System.IDisposable
    Class IDisposable
    End Class
{DisposePattern("Overridable ", simplifySystem:=False)}
End Class", index:=1, compareTokens:=False)
        End Function

        <WorkItem(958699)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposable_NoNamespaceImportForSystem() As Task
            Await TestAsync(
"Class C : Implements [|System.IDisposable|]
",
$"Class C : Implements System.IDisposable
{DisposePattern("Overridable ", simplifySystem:=False)}
End Class
", index:=1, compareTokens:=False)
        End Function

        <WorkItem(951968)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposableViaBaseInterface_NoDisposePattern() As Task
            Await TestAsync(
"Imports System
Interface I : Inherits IDisposable
    Sub F()
End Interface
Class C : Implements [|I|]
End Class",
"Imports System
Interface I : Inherits IDisposable
    Sub F()
End Interface
Class C : Implements I

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub

    Public Sub F() Implements I.F
        Throw New NotImplementedException()
    End Sub
End Class", index:=0, compareTokens:=False)
        End Function

        <WorkItem(951968)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementIDisposableViaBaseInterface() As Task
            Await TestAsync(
"Imports System
Interface I : Inherits IDisposable
    Sub F()
End Interface
Class C : Implements [|I|]
End Class",
$"Imports System
Interface I : Inherits IDisposable
    Sub F()
End Interface
Class C : Implements I

    Public Sub F() Implements I.F
        Throw New NotImplementedException()
    End Sub
{DisposePattern("Overridable ")}
End Class", index:=1, compareTokens:=False)
        End Function

        <WorkItem(951968)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDontImplementDisposePatternForLocallyDefinedIDisposable() As Task
            Await TestAsync(
"Namespace System
    Interface IDisposable
        Sub Dispose
    End Interface

    Class C : Implements [|IDisposable|]
End Namespace",
"Namespace System
    Interface IDisposable
        Sub Dispose
    End Interface

    Class C : Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDontImplementDisposePatternForStructures() As Task
            Await TestAsync(
"Imports System
Structure S : Implements [|IDisposable|]",
"Imports System
Structure S : Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Throw New NotImplementedException()
    End Sub
End Structure
", compareTokens:=False)
        End Function

        <WorkItem(994328)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDisposePatternWhenAdditionalImportsAreIntroduced1() As Task
            Await TestAsync(
"Interface I(Of T, U As T) : Inherits System.IDisposable, System.IEquatable(Of Integer)
    Function M(a As System.Collections.Generic.Dictionary(Of T, System.Collections.Generic.List(Of U)), b As T, c As U) As System.Collections.Generic.List(Of U)
    Function M(Of TT, UU As TT)(a As System.Collections.Generic.Dictionary(Of TT, System.Collections.Generic.List(Of UU)), b As TT, c As UU) As System.Collections.Generic.List(Of UU)
End Interface

Class _
    C
    Implements [|I(Of System.Exception, System.AggregateException)|]
End Class

Partial Class C
    Implements IDisposable
End Class",
$"Imports System
Imports System.Collections.Generic

Interface I(Of T, U As T) : Inherits System.IDisposable, System.IEquatable(Of Integer)
    Function M(a As System.Collections.Generic.Dictionary(Of T, System.Collections.Generic.List(Of U)), b As T, c As U) As System.Collections.Generic.List(Of U)
    Function M(Of TT, UU As TT)(a As System.Collections.Generic.Dictionary(Of TT, System.Collections.Generic.List(Of UU)), b As TT, c As UU) As System.Collections.Generic.List(Of UU)
End Interface

Class _
    C
    Implements I(Of System.Exception, System.AggregateException)

    Public Function Equals(other As Integer) As Boolean Implements IEquatable(Of Integer).Equals
        Throw New NotImplementedException()
    End Function

    Public Function M(a As Dictionary(Of Exception, List(Of AggregateException)), b As Exception, c As AggregateException) As List(Of AggregateException) Implements I(Of Exception, AggregateException).M
        Throw New NotImplementedException()
    End Function

    Public Function M(Of TT, UU As TT)(a As Dictionary(Of TT, List(Of UU)), b As TT, c As UU) As List(Of UU) Implements I(Of Exception, AggregateException).M
        Throw New NotImplementedException()
    End Function
{DisposePattern("Overridable ")}
End Class

Partial Class C
    Implements IDisposable
End Class", index:=1, compareTokens:=False)
        End Function

        <WorkItem(994328)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestDisposePatternWhenAdditionalImportsAreIntroduced2() As Task
            Await TestAsync(
"Class C
End Class

Partial Class C
    Implements [|I(Of System.Exception, System.AggregateException)|]
    Implements IDisposable
End Class

Interface I(Of T, U As T) : Inherits System.IDisposable, System.IEquatable(Of Integer)
    Function M(a As System.Collections.Generic.Dictionary(Of T, System.Collections.Generic.List(Of U)), b As T, c As U) As System.Collections.Generic.List(Of U)
    Function M(Of TT, UU As TT)(a As System.Collections.Generic.Dictionary(Of TT, System.Collections.Generic.List(Of UU)), b As TT, c As UU) As System.Collections.Generic.List(Of UU)
End Interface",
$"Imports System
Imports System.Collections.Generic

Class C
End Class

Partial Class C
    Implements I(Of System.Exception, System.AggregateException)
    Implements IDisposable

    Public Function Equals(other As Integer) As Boolean Implements IEquatable(Of Integer).Equals
        Throw New NotImplementedException()
    End Function

    Public Function M(a As Dictionary(Of Exception, List(Of AggregateException)), b As Exception, c As AggregateException) As List(Of AggregateException) Implements I(Of Exception, AggregateException).M
        Throw New NotImplementedException()
    End Function

    Public Function M(Of TT, UU As TT)(a As Dictionary(Of TT, List(Of UU)), b As TT, c As UU) As List(Of UU) Implements I(Of Exception, AggregateException).M
        Throw New NotImplementedException()
    End Function
{DisposePattern("Overridable ")}
End Class

Interface I(Of T, U As T) : Inherits System.IDisposable, System.IEquatable(Of Integer)
    Function M(a As System.Collections.Generic.Dictionary(Of T, System.Collections.Generic.List(Of U)), b As T, c As U) As System.Collections.Generic.List(Of U)
    Function M(Of TT, UU As TT)(a As System.Collections.Generic.Dictionary(Of TT, System.Collections.Generic.List(Of UU)), b As TT, c As UU) As System.Collections.Generic.List(Of UU)
End Interface", index:=1, compareTokens:=False)
        End Function

        Private Shared Function DisposePattern(disposeMethodModifiers As String, Optional simplifySystem As Boolean = True) As String
            Dim code = $"
#Region ""IDisposable Support""
    Private disposedValue As Boolean ' {FeaturesResources.ToDetectRedundantCalls}

    ' IDisposable
    Protected {disposeMethodModifiers}Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' {FeaturesResources.DisposeManagedStateTodo}
            End If

            ' {VBFeaturesResources.FreeUnmanagedResourcesTodo}
            ' {FeaturesResources.SetLargeFieldsToNullTodo}
        End If
        disposedValue = True
    End Sub

    ' {VBFeaturesResources.OverrideFinalizerTodo}
    'Protected Overrides Sub Finalize()
    '    ' {VBFeaturesResources.DoNotChangeThisCodeUseDispose}
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' {VBFeaturesResources.ThisCodeAddedToCorrectlyImplementDisposable}
    Public Sub Dispose() Implements System.IDisposable.Dispose
        ' {VBFeaturesResources.DoNotChangeThisCodeUseDispose}
        Dispose(True)
        ' {VBFeaturesResources.UncommentTheFollowingLineIfFinalizeIsOverridden}
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region"

            ' some tests count on "System." being simplified out
            If simplifySystem Then
                code = code.Replace("System.IDisposable.Dispose", "IDisposable.Dispose")
            End If

            Return code
        End Function

        <WorkItem(1132014)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestInaccessibleAttributes() As Task
            Await TestAsync(
"Imports System

Public Class Foo
    Implements [|Holder.SomeInterface|]
End Class

Public Class Holder
	Public Interface SomeInterface
		Sub Something(<SomeAttribute> helloWorld As String)
	End Interface

	Private Class SomeAttribute
		Inherits Attribute
	End Class
End Class",
"Imports System

Public Class Foo
    Implements Holder.SomeInterface

    Public Sub Something(helloWorld As String) Implements Holder.SomeInterface.Something
        Throw New NotImplementedException()
    End Sub
End Class

Public Class Holder
	Public Interface SomeInterface
		Sub Something(<SomeAttribute> helloWorld As String)
	End Interface

	Private Class SomeAttribute
		Inherits Attribute
	End Class
End Class", compareTokens:=False)
        End Function


        <WorkItem(2785, "https://github.com/dotnet/roslyn/issues/2785")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function TestImplementInterfaceThroughStaticMemberInGenericClass() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Class Program(Of T) \n Implements [|IList(Of Object)|] \n Private Shared innerList As List(Of Object) = New List(Of Object) \n End Class"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Collections.Generic \n Class Program(Of T) \n Implements IList(Of Object) \n Private Shared innerList As List(Of Object) = New List(Of Object) \n Public ReadOnly Property Count As Integer Implements ICollection(Of Object).Count \n Get \n Return DirectCast(innerList, IList(Of Object)).Count \n End Get \n End Property \n Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of Object).IsReadOnly \n Get \n Return DirectCast(innerList, IList(Of Object)).IsReadOnly \n End Get \n End Property \n Default Public Property Item(index As Integer) As Object Implements IList(Of Object).Item \n Get \n Return DirectCast(innerList, IList(Of Object))(index) \n End Get \n Set(value As Object) \n DirectCast(innerList, IList(Of Object))(index) = value \n End Set \n End Property \n Public Sub Add(item As Object) Implements ICollection(Of Object).Add \n DirectCast(innerList, IList(Of Object)).Add(item) \n End Sub \n Public Sub Clear() Implements ICollection(Of Object).Clear \n DirectCast(innerList, IList(Of Object)).Clear() \n End Sub \n Public Sub CopyTo(array() As Object, arrayIndex As Integer) Implements ICollection(Of Object).CopyTo \n DirectCast(innerList, IList(Of Object)).CopyTo(array, arrayIndex) \n End Sub \n Public Sub Insert(index As Integer, item As Object) Implements IList(Of Object).Insert \n DirectCast(innerList, IList(Of Object)).Insert(index, item) \n End Sub \n Public Sub RemoveAt(index As Integer) Implements IList(Of Object).RemoveAt \n DirectCast(innerList, IList(Of Object)).RemoveAt(index) \n End Sub \n Public Function Contains(item As Object) As Boolean Implements ICollection(Of Object).Contains \n Return DirectCast(innerList, IList(Of Object)).Contains(item) \n End Function \n Public Function GetEnumerator() As IEnumerator(Of Object) Implements IEnumerable(Of Object).GetEnumerator \n Return DirectCast(innerList, IList(Of Object)).GetEnumerator() \n End Function \n Public Function IndexOf(item As Object) As Integer Implements IList(Of Object).IndexOf \n Return DirectCast(innerList, IList(Of Object)).IndexOf(item) \n End Function \n Public Function Remove(item As Object) As Boolean Implements ICollection(Of Object).Remove \n Return DirectCast(innerList, IList(Of Object)).Remove(item) \n End Function \n Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Return DirectCast(innerList, IList(Of Object)).GetEnumerator() \n End Function \n End Class"),
index:=1)
        End Function
    End Class
End Namespace
