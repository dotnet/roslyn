' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.NavigationBar)>
    Partial Public Class VisualBasicNavigationBarTests
        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545000")>
        Public Async Function TestEventsInInterfaces(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface I
                                Event Goo As EventHandler
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("I", Glyph.InterfaceInternal, bolded:=True, children:={
                    Item("Goo", Glyph.EventPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544996")>
        Public Async Function TestEmptyStructure(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Structure S
                            End Structure
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("S", Glyph.StructureInternal, bolded:=True, children:={}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544996")>
        Public Async Function TestEmptyInterface(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface I
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("I", Glyph.InterfaceInternal, bolded:=True, children:={}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestUserDefinedOperators(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Operator +(C, C) As C", Glyph.OperatorPublic, bolded:=True),
                    Item("Operator +(C, Integer) As C", Glyph.OperatorPublic, bolded:=True),
                    Item("Operator -", Glyph.OperatorPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestSingleConversion(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType", Glyph.OperatorPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797455")>
        Public Async Function TestMultipleConversions(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType(C) As Integer", Glyph.OperatorPublic, bolded:=True),
                    Item("Narrowing Operator CType(C) As String", Glyph.OperatorPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544993")>
        Public Async Function TestNestedClass(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True),
                Item("Nested (N.C)", Glyph.ClassPublic, bolded:=True))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544997")>
        Public Async Function TestDelegate(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Delegate Sub Goo()
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("Goo", Glyph.DelegateInternal, children:={}, bolded:=True))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544995"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545283")>
        Public Async Function TestGenericType(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface C(Of In T)
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C(Of In T)", Glyph.InterfaceInternal, bolded:=True))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545113")>
        Public Async Function TestMethodGroupWithGenericMethod(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S()", Glyph.MethodPublic, bolded:=True),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545113")>
        Public Async Function TestSingleGenericMethod(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545285")>
        Public Async Function TestSingleGenericFunction(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)() As Integer", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleNonGenericMethod(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S", Glyph.MethodPublic, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544994")>
        Public Async Function TestSelectedItemForNestedClass(host As TestHost) As Task
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
                host,
                Item("Nested (C)", Glyph.ClassPublic, bolded:=True), False, Nothing, False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899330")>
        Public Async Function TestSelectedItemForNestedClassAlphabeticallyBeforeContainingClass(host As TestHost) As Task
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
                host,
                Item("Nested (Z)", Glyph.ClassPublic, bolded:=True), False, Nothing, False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544990")>
        Public Async Function TestFinalizer(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/556")>
        Public Async Function TestFieldsAndConstants(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                    Item("Co", Glyph.ConstantPrivate, bolded:=True),
                    Item("F", Glyph.FieldPrivate, bolded:=True)}))
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544988")>
        Public Async Function TestGenerateFinalizer(host As TestHost) As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                "C", "Finalize",
                <Result>
Class C
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class
                </Result>)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestGenerateConstructor(host As TestHost) As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                "C", "New",
                <Result>
Class C
    Public Sub New()

    End Sub
End Class
                </Result>)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestGenerateConstructorInDesignerGeneratedFile(host As TestHost) As Task
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
                host,
                "C", "New",
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestGeneratePartialMethod(host As TestHost) As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Partial Class C
End Class
                        </Document>
                        <Document>
Partial Class C
    Private Partial Sub Goo()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                "C", "Goo",
                <Result>
Partial Class C
    Private Sub Goo()

    End Sub
End Class
                </Result>)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPartialMethodInDifferentFile(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Partial Class C
                            End Class
                        </Document>
                        <Document>
                            Partial Class C
                                Sub Goo()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Goo", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544991")>
        Public Async Function TestWithEventsField(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class C
                                Private WithEvents goo As System.Console
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("goo", Glyph.FieldPrivate, bolded:=False, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("CancelKeyPress", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589")>
        Public Async Function TestWithEventsField_EventsFromInheritedInterfaces(host As TestHost) As Task
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
                host,
                Item("I1", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I1Event", Glyph.EventPublic, bolded:=True)}),
                Item("I2", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I2Event", Glyph.EventPublic, bolded:=True)}),
                Item("I3", Glyph.InterfaceInternal, bolded:=True, children:={
                     Item("I3Event", Glyph.EventPublic, bolded:=True)}),
                Item("Test", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("i3", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("I1Event", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("I2Event", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("I3Event", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestDoNotIncludeShadowedEvents(host As TestHost) As Task
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
                host,
                Item("B", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBFeaturesResources._0_Events, "B"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}),
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBFeaturesResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}), ' Only one E under the "(C Events)" node
                Item("Test", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("c", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)})) ' Only one E for WithEvents handling
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_EnsureInternalEventsInEventListAndInInheritedEventList(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPublic, bolded:=True)}),
                Item(String.Format(VBFeaturesResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}),
                Item("D", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBFeaturesResources._0_Events, "D"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPublic, hasNavigationSymbolId:=False)}))

        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_EnsurePrivateEventsInEventListButNotInInheritedEventList(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E", Glyph.EventPrivate, bolded:=True)}),
                Item(String.Format(VBFeaturesResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("D", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185589"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530506")>
        Public Async Function TestEventList_TestAccessibilityThroughNestedAndDerivedTypes(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("E0", Glyph.EventPublic, bolded:=True),
                     Item("E1", Glyph.EventProtected, bolded:=True),
                     Item("E2", Glyph.EventPrivate, bolded:=True)}),
                Item(String.Format(VBFeaturesResources._0_Events, "C"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False),
                     Item("E2", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("D2", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBFeaturesResources._0_Events, "D2"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False)}),
                Item("N1 (C)", Glyph.ClassPublic, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("N2 (C.N1)", Glyph.ClassPublic, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item(String.Format(VBFeaturesResources._0_Events, "N2"), Glyph.EventPublic, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False),
                     Item("E1", Glyph.EventProtected, hasNavigationSymbolId:=False),
                     Item("E2", Glyph.EventPrivate, hasNavigationSymbolId:=False)}),
                Item("T", Glyph.ClassInternal, bolded:=True, children:={
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("c", Glyph.FieldPrivate, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("E0", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestGenerateEventHandler(host As TestHost) As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Private WithEvents goo As System.Console
End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                "goo", "CancelKeyPress",
                <Result>
Class C
    Private WithEvents goo As System.Console

    Private Sub goo_CancelKeyPress(sender As Object, e As ConsoleCancelEventArgs) Handles goo.CancelKeyPress

    End Sub
End Class
                </Result>)
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529946")>
        Public Async Function TestGenerateEventHandlerWithEscapedName(host As TestHost) As Task
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
                host,
                String.Format(VBFeaturesResources._0_Events, "C"), "Rem",
                <Result>
Class C
    Event [Rem] As System.Action

    Private Sub C_Rem() Handles Me.[Rem]

    End Sub
End Class
                </Result>)
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546152")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenerateEventHandlerWithRemName(host As TestHost) As Task
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
                host,
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

        <ConditionalWpfTheory(GetType(IsEnglishLocal)), CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25763")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/18792")>
        Public Async Function TestGenerateEventHandlerWithDuplicate(host As TestHost) As Task
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
                host,
                $"(ExampleClass { FeaturesResources.Events })",
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

        <Theory, CombinatorialData>
        Public Async Function TestNoListedEventToGenerateWithInvalidTypeName(host As TestHost) As Task
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
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                    Item("BindingError", Glyph.EventPublic, hasNavigationSymbolId:=True, bolded:=True)},
                    bolded:=True))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530657")>
        Public Async Function TestCodeGenerationItemsShouldNotAppearWhenWorkspaceDoesNotSupportDocumentChanges(host As TestHost) As Task
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
                host,
                workspaceSupportsChangeDocument,
                Item("C", Glyph.ClassInternal, bolded:=True),
                Item("M", Glyph.FieldPrivate, indent:=1, hasNavigationSymbolId:=False))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestEnum(host As TestHost) As Task
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
                host,
                Item("MyEnum", Glyph.EnumInternal, children:={
                    Item("A", Glyph.EnumMemberPublic),
                    Item("B", Glyph.EnumMemberPublic),
                    Item("C", Glyph.EnumMemberPublic)},
                    bolded:=True))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEvents(host As TestHost) As Task
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
                host,
                Item("Base", Glyph.ClassPublic, children:={
                    Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
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
                     Item("New", Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Ev_Event", Glyph.EventPublic, bolded:=True)},
                     bolded:=True),
                Item(String.Format(VBFeaturesResources._0_Events, "Class1"), Glyph.EventPublic, children:={
                     Item("Ev_Event", Glyph.EventPublic, hasNavigationSymbolId:=False)},
                     bolded:=False,
                     indent:=1,
                     hasNavigationSymbolId:=False))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNavigationBetweenFiles(host As TestHost) As Task
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
                host,
                startingDocumentName:="Source.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="TargetMethod")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566752")>
        Public Async Function TestNavigationWithMethodWithLineContinuation(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="SomeNumbers")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531586")>
        Public Async Function TestNavigationWithMethodWithNoTerminator(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531586")>
        Public Async Function TestNavigationWithMethodWithDocumentationComment(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567914")>
        Public Async Function TestNavigationWithMethodWithMultipleLineDeclaration(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingComment(host As TestHost) As Task
            Await AssertNavigationPointAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document FilePath="Code.vb">
Partial Class Program
    Private Sub S(value As Integer) 
        $$' Goo
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>,
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithSpaces(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S")
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithNoSpaces(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S",
                expectedVirtualSpace:=8)
        End Function

        <WpfTheory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605074")>
        Public Async Function TestNavigationWithMethodContainingBlankLineWithSomeSpaces(host As TestHost) As Task
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
                host,
                startingDocumentName:="Code.vb",
                leftItemToSelectText:="Program",
                rightItemToSelectText:="S",
                expectedVirtualSpace:=4)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/187865")>
        Public Async Function DifferentMembersMetadataName(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Function Get_P(o As Object) As Object
        Return od
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
                host,
                Item("C", Glyph.ClassInternal, bolded:=True, children:={
                    Item("New", Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Get_P", Glyph.MethodPublic, bolded:=True),
                    Item("P", Glyph.PropertyPublic, bolded:=True)}))
        End Function

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/37621")>
        Public Async Function TestGenerateEventWithAttributedDelegateType(host As TestHost) As Task
            Await AssertGeneratedResultIsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>LibraryWithInaccessibleAttribute</ProjectReference>
                        <Document>
Class C
    Inherits BaseType
End Class
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" Name="LibraryWithInaccessibleAttribute" CommonReferences="true">
                        <Document><![CDATA[[
Friend Class AttributeType
    Inherits Attribute
End Class

Delegate Sub DelegateType(<AttributeType> parameterWithInaccessibleAttribute As Object)

Public Class BaseType
    Public Event E As DelegateType
End Class
                    ]]></Document></Project>
                </Workspace>,
                host,
                 String.Format(VBFeaturesResources._0_Events, "C"), "E",
                <Result>
Class C
    Inherits BaseType

    Private Sub C_E(parameterWithInaccessibleAttribute As Object) Handles Me.E

    End Sub
End Class
                </Result>)
        End Function

    End Class
End Namespace
