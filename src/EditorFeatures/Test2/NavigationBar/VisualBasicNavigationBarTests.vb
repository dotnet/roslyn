﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Partial Public Class VisualBasicNavigationBarTests
        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545000")>
        Public Async Function TestEventsInInterfaces() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface I
                                Event Foo As EventHandler
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                Item("I", Glyph.InterfaceInternal, bolded:=True, children:={
                    Item("Foo", Glyph.EventPublic, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544996")>
        Public Async Function TestEmptyStructure() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Structure S
                            End Structure
                        </Document>
                    </Project>
                </Workspace>,
                Item("S", Glyph.StructureInternal, bolded:=True, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544996")>
        Public Async Function TestEmptyInterface() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface I
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                Item("I", Glyph.InterfaceInternal, bolded:=True, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestUserDefinedOperators() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Shared Operator -(x As C, y As C) As C

    End Operator

    Shared Operator +(x As C, y As C) As C

    End Operator

    Shared Operator +(x As C, y As Integer) As C

    End Operator
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Operator +(C, C) As C", Glyph.Operator, bolded:=True),
                    Item("Operator +(C, Integer) As C", Glyph.Operator, bolded:=True),
                    Item("Operator -", Glyph.Operator, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestSingleConversion() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Shared Narrowing Operator CType(x As C) As Integer

    End Operator
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType", Glyph.Operator, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestMultipleConversions() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Shared Narrowing Operator CType(x As C) As Integer

    End Operator

    Shared Narrowing Operator CType(x As C) As String

    End Operator
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType(C) As Integer", Glyph.Operator, bolded:=True),
                    Item("Narrowing Operator CType(C) As String", Glyph.Operator, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544993")>
        Public Async Function TestNestedClass() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Namespace N
                                Class C
                                    Class Nested
                                    End Class
                                End Class
                            End Namespace
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True),
                Item("Nested (N.C)", Glyph.ClassPublic, bolded:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544997, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544997")>
        Public Async Function TestDelegate() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Delegate Sub Foo()
                        </Document>
                    </Project>
                </Workspace>,
                Item("Foo", Glyph.DelegateInternal, children:={}, bolded:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544995"), WorkItem(545283, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545283")>
        Public Async Function TestGenericType() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface C(Of In T)
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                Item("C(Of In T)", Glyph.InterfaceInternal, bolded:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545113")>
        Public Async Function TestMethodGroupWithGenericMethod() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Sub S()
                                End Sub

                                Sub S(Of T)()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S()", Glyph.MethodPublic, bolded:=True),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545113")>
        Public Async Function TestSingleGenericMethod() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Sub S(Of T)()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545285")>
        Public Async Function TestSingleGenericFunction() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Function S(Of T)() As Integer
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)() As Integer", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestSingleNonGenericMethod() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Sub S(arg As Integer)
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544994")>
        Public Async Function TestSelectedItemForNestedClass() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Class Nested
                                    $$
                                End Class
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("Nested (C)", Glyph.ClassPublic, bolded:=True), False, Nothing, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(899330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899330")>
        Public Async Function TestSelectedItemForNestedClassAlphabeticallyBeforeContainingClass() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class Z
                                Class Nested
                                    $$
                                End Class
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("Nested (Z)", Glyph.ClassPublic, bolded:=True), False, Nothing, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544990")>
        Public Async Function TestFinalizer() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Protected Overrides Sub Finalize()
                                End Class
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(556, "https://github.com/dotnet/roslyn/issues/556")>
        Public Async Function TestFieldsAndConstants() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                    Private Const Co = 1
                                    Private F As Integer
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                    Item("Co", Glyph.ConstantPrivate, bolded:=True),
                    Item("F", Glyph.FieldPrivate, bolded:=True)}))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544988")>
        Public Async Function TestGenerateFinalizer() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "C", "Finalize",
                <Result>
Class C
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class
                </Result>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGenerateConstructor() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "C", VBEditorResources.New_,
                <Result>
Class C
    Public Sub New()

    End Sub
End Class
                </Result>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGenerateConstructorInDesignerGeneratedFile() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
&lt;Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute&gt;
Class C

    Sub InitializeComponent()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "C", VBEditorResources.New_,
                <Result>
&lt;Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute&gt;
Class C
    Public Sub New()

        ' <%= VBEditorResources.This_call_is_required_by_the_designer %>
        InitializeComponent()

        ' <%= VBEditorResources.Add_any_initialization_after_the_InitializeComponent_call %>

    End Sub

    Sub InitializeComponent()
    End Sub
End Class
                </Result>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGeneratePartialMethod() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
End Class
                        </Document>
                        <Document>
Partial Class C
    Private Partial Sub Foo()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "C", "Foo",
                <Result>
Partial Class C
    Private Sub Foo()

    End Sub
End Class
                </Result>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestPartialMethodInDifferentFile() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Partial Class C
                            End Class
                        </Document>
                        <Document>
                            Partial Class C
                                Sub Foo()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Foo", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544991")>
        Public Async Function TestWithEventsField() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Private WithEvents foo As System.Console
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("foo", Glyph.FieldPrivate, bolded:=False, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("CancelKeyPress", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(1185589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589")>
        Public Async Function TestWithEventsField_EventsFromInheritedInterfaces() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I1
    Event I1Event(sender As Object, e As EventArgs)
End Interface

Interface I2
    Event I2Event(sender As Object, e As EventArgs)
End Interface

Interface I3
    Inherits I1, I2
    Event I3Event(sender As Object, e As EventArgs)
End Interface

Class Test
    WithEvents i3 As I3
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("I1", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I1Event", Glyph.EventPublic, bolded:=True)}),
                Item("I2", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I2Event", Glyph.EventPublic, bolded:=True)}),
                Item("I3", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I3Event", Glyph.EventPublic, bolded:=True)}),
                Item("Test", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("i3", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("I1Event", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("I2Event", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("I3Event", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(1185589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem(530506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestDoNotIncludeShadowedEvents() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class B
    Event E(sender As Object, e As EventArgs)
End Class

Class C
    Inherits B

    Shadows Event E(sender As Object, e As EventArgs)
End Class

Class Test
    WithEvents c As C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("B", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBEditorResources._0_Events, "B"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}),
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBEditorResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}), ' Only one E under the "(C Events)" node
                Item("Test", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("c", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)})) ' Only one E for WithEvents handling
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(1185589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem(530506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_EnsureInternalEventsInEventListAndInInheritedEventList() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Event E()
End Class

Class D
    Inherits C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBEditorResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}),
                Item("D", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBEditorResources._0_Events, "D"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}))

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(1185589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem(530506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_EnsurePrivateEventsInEventListButNotInInheritedEventList() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Private Event E()
End Class

Class D
    Inherits C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPrivate, bolded:=True)}),
                Item(String.Format(VBEditorResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("D", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(1185589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem(530506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_TestAccessibilityThroughNestedAndDerivedTypes() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Public Event E0()
    Protected Event E1()
    Private Event E2()

    Class N1
        Class N2
            Inherits C

        End Class
    End Class
End Class

Class D2
    Inherits C

End Class

Class T
    WithEvents c As C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E0", Glyph.EventPublic, bolded:=True),
                     Item("E1", Glyph.EventProtected, bolded:=True),
                     Item("E2", Glyph.EventPrivate, bolded:=True)}),
                Item(String.Format(VBEditorResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False),
                     Item("E2", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("D2", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBEditorResources._0_Events, "D2"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False)}),
                Item("N1 (C)", Glyph.ClassPublic, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("N2 (C.N1)", Glyph.ClassPublic, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBEditorResources._0_Events, "N2"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False),
                     Item("E2", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("T", Glyph.ClassInternal, bolded:=True, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("c", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGenerateEventHandler() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Private WithEvents foo As System.Console
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "foo", "CancelKeyPress",
                <Result>
Class C
    Private WithEvents foo As System.Console

    Private Sub foo_CancelKeyPress(sender As Object, e As ConsoleCancelEventArgs) Handles foo.CancelKeyPress

    End Sub
End Class
                </Result>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(529946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529946")>
        Public Async Function TestGenerateEventHandlerWithEscapedName() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Event [Rem] As System.Action
End Class
                        </Document>
                    </Project>
                </Workspace>,
                String.Format(VBEditorResources._0_Events, "C"), "Rem",
                <Result>
Class C
    Event [Rem] As System.Action

    Private Sub C_Rem() Handles Me.[Rem]

    End Sub
End Class
                </Result>)
        End Function

        <WorkItem(546152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546152")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGenerateEventHandlerWithRemName() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System

Class C
    Event E As Action

    WithEvents [Rem] As C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "Rem", "E",
                <Result>
Imports System

Class C
    Event E As Action

    WithEvents [Rem] As C

    Private Sub Rem_E() Handles [Rem].E

    End Sub
End Class
                </Result>)
        End Function

        <WpfFact>
        <WorkItem(18792, "https://github.com/dotnet/roslyn/issues/18792")>
        <Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestGenerateEventHandlerWithDuplicate() As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class ExampleClass
    Public Event ExampleEvent()
    Public Event ExampleEvent()
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "(ExampleClass Events)",
                Function(items) items.First(Function(i) i.Text = "ExampleEvent"),
                <Result>
Public Class ExampleClass
    Public Event ExampleEvent()
    Public Event ExampleEvent()

    Private Sub ExampleClass_ExampleEvent() Handles Me.ExampleEvent

    End Sub
End Class
                </Result>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestNoListedEventToGenerateWithInvalidTypeName() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Event BindingError As System.FogBar
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                    Item("BindingError", Glyph.EventPublic, hasNavigationSymbolId:=True, bolded:=True)},
                    bolded:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(530657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530657")>
        Public Async Function TestCodeGenerationItemsShouldNotAppearWhenWorkspaceDoesNotSupportDocumentChanges() As Task
            Dim workspaceSupportsChangeDocument = False
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
    Private WithEvents M As System.Console
End Class

Partial Class C
    Partial Private Sub S()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                workspaceSupportsChangeDocument,
                Item("C", Glyph.ClassInternal, bolded:=True),
                Item("M", Glyph.FieldPrivate, indent:=1, hasNavigationSymbolId:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestEnum() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Enum MyEnum
    A
    B
    C
End Enum
                        </Document>
                    </Project>
                </Workspace>,
                Item("MyEnum", Glyph.EnumInternal, children:={
                    Item("A", Glyph.EnumMemberPublic),
                    Item("B", Glyph.EnumMemberPublic),
                    Item("C", Glyph.EnumMemberPublic)},
                    bolded:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestEvents() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Public Class Base

    Public WithEvents o1 As New Class1
    Public WithEvents o2 As New Class1

    Public Class Class1
        ' Declare an event.
        Public Event Ev_Event()
    End Class
$$
    Sub EventHandler() Handles o1.Ev_Event

    End Sub

End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("Base", Glyph.ClassPublic, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)},
                    bolded:=True),
                Item("o1", Glyph.FieldPublic, children:={
                     Item("Ev_Event", Glyph.EventPublic, bolded:=True)},
                     bolded:=False,
                     hasNavigationSymbolId:=False,
                     indent:=1),
                Item("o2", Glyph.FieldPublic, children:={
                     Item("Ev_Event", Glyph.EventPublic, hasNavigationSymbolId:=False)},
                     bolded:=False,
                     hasNavigationSymbolId:=False,
                     indent:=1),
                Item("Class1 (Base)", Glyph.ClassPublic, children:={
                     Item(VBEditorResources.New_, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Ev_Event", Glyph.EventPublic, bolded:=True)},
                     bolded:=True),
                Item(String.Format(VBEditorResources._0_Events, "Class1"), Glyph.EventPublic, children:={
                     Item("Ev_Event", Glyph.EventPublic, hasNavigationSymbolId:=False)},
                     bolded:=False,
                     indent:=1,
                     hasNavigationSymbolId:=False))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestNavigationBetweenFiles() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Source.vb">
Partial Class Program
    Sub StartingFile()
    End Sub
End Class
                        </Document>
                        <Document FilePath="Sink.vb">
Partial Class Program
    Sub MethodThatWastesTwoLines()
    End Sub

    Sub TargetMethod()
    $$End Sub
End Class            
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Source.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="TargetMethod")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(566752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566752")>
        Public Async Function TestNavigationWithMethodWithLineContinuation() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Iterator Function SomeNumbers() _
        As System.Collections.IEnumerable
        $$Yield 3
        Yield 5
        Yield 8
    End Function
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="SomeNumbers")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(531586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531586")>
        Public Async Function TestNavigationWithMethodWithNoTerminator() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    $$Private Sub S() 
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(531586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531586")>
        Public Async Function TestNavigationWithMethodWithDocumentationComment() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb"><![CDATA[
Partial Class Program
    ''' <summary></summary>
    $$Private Sub S() 
End Class
                        ]]></Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(567914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567914")>
        Public Async Function TestNavigationWithMethodWithMultipleLineDeclaration() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(
        value As Integer
    ) 
        $$Exit Sub
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingComment() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(value As Integer) 
        $$' Foo
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithSpaces() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(value As Integer) 
        $$
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithNoSpaces() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(value As Integer) 
$$
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S",
                expectedVirtualSpace:=8)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithSomeSpaces() As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(value As Integer) 
    $$
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                startingDocumentFilePath:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S",
                expectedVirtualSpace:=4)
        End Function

        <WorkItem(187865, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/187865")>
        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function DifferentMembersMetadataName() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Function Get_P(o As Object) As Object
        Return o
    End Function
    ReadOnly Property P As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item(VBEditorResources.New_, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Get_P", Glyph.MethodPublic, bolded:=True),
                    Item("P", Glyph.PropertyPublic, bolded:=True)}))
        End Function

    End Class
End Namespace
