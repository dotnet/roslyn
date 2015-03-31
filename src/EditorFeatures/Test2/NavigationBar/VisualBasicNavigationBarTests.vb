' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Partial Public Class VisualBasicNavigationBarTests
        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545000)>
        Public Sub EventsInInterfaces()
            AssertItemsAre(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544996)>
        Public Sub EmptyStructure()
            AssertItemsAre(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Structure S
                            End Structure
                        </Document>
                    </Project>
                </Workspace>,
                Item("S", Glyph.StructureInternal, bolded:=True, children:={}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544996)>
        Public Sub EmptyInterface()
            AssertItemsAre(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface I
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                Item("I", Glyph.InterfaceInternal, bolded:=True, children:={}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455)>
        Public Sub UserDefinedOperators()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Operator +(C, C) As C", Glyph.Operator, bolded:=True),
                    Item("Operator +(C, Integer) As C", Glyph.Operator, bolded:=True),
                    Item("Operator -", Glyph.Operator, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455)>
        Public Sub SingleConversion()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType", Glyph.Operator, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(797455)>
        Public Sub MultipleConversions()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                    Item("Narrowing Operator CType(C) As Integer", Glyph.Operator, bolded:=True),
                    Item("Narrowing Operator CType(C) As String", Glyph.Operator, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544993)>
        Public Sub NestedClass()
            AssertItemsAre(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544997)>
        Public Sub [Delegate]()
            AssertItemsAre(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Delegate Sub Foo()
                        </Document>
                    </Project>
                </Workspace>,
                Item("Foo", Glyph.DelegateInternal, children:={}, bolded:=True))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544995), WorkItem(545283)>
        Public Sub GenericType()
            AssertItemsAre(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Interface C(Of In T)
                            End Interface
                        </Document>
                    </Project>
                </Workspace>,
                Item("C(Of In T)", Glyph.InterfaceInternal, bolded:=True))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545113)>
        Public Sub MethodGroupWithGenericMethod()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S()", Glyph.MethodPublic, bolded:=True),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545113)>
        Public Sub SingleGenericMethod()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)()", Glyph.MethodPublic, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545285)>
        Public Sub SingleGenericFunction()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S(Of T)() As Integer", Glyph.MethodPublic, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub SingleNonGenericMethod()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, bolded:=False, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, bolded:=False, hasNavigationSymbolId:=False),
                     Item("S", Glyph.MethodPublic, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544994)>
        Public Sub SelectedItemForNestedClass()
            AssertSelectedItemsAre(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(899330)>
        Public Sub SelectedItemForNestedClassAlphabeticallyBeforeContainingClass()
            AssertSelectedItemsAre(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544990)>
        Public Sub Finalizer()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, bolded:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544988)>
        Public Sub GenerateFinalizer()
            AssertGeneratedResultIs(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub GenerateConstructor()
            AssertGeneratedResultIs(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
End Class
                        </Document>
                    </Project>
                </Workspace>,
                "C", NavigationItemNew,
                <Result>
Class C
    Public Sub New()

    End Sub
End Class
                </Result>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub GenerateConstructorInDesignerGeneratedFile()
            AssertGeneratedResultIs(
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
                "C", NavigationItemNew,
                <Result>
&lt;Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute&gt;
Class C
    Public Sub New()

        ' <%= ThisCallIsRequiredByTheDesigner %>
        InitializeComponent()

        ' <%= AddAnyInitializationAfter %>

    End Sub

    Sub InitializeComponent()
    End Sub
End Class
                </Result>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub GeneratePartialMethod()
            AssertGeneratedResultIs(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub PartialMethodInDifferentFile()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Foo", Glyph.MethodPublic, grayed:=True)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544991)>
        Public Sub WithEventsField()
            AssertItemsAre(
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
                     Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False)}),
                Item("foo", Glyph.FieldPrivate, bolded:=False, hasNavigationSymbolId:=False, indent:=1, children:={
                     Item("CancelKeyPress", Glyph.EventPublic, hasNavigationSymbolId:=False)}))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub GenerateEventHandler()
            AssertGeneratedResultIs(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(529946)>
        Public Sub GenerateEventHandlerWithEscapedName()
            AssertGeneratedResultIs(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Event [Rem] As System.Action
End Class
                        </Document>
                    </Project>
                </Workspace>,
                String.Format(VBEditorResources.Events, "C"), "Rem",
                <Result>
Class C
    Event [Rem] As System.Action

    Private Sub C_Rem() Handles Me.[Rem]

    End Sub
End Class
                </Result>)
        End Sub

        <WorkItem(546152)>
        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub GenerateEventHandlerWithRemName()
            AssertGeneratedResultIs(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub NoListedEventToGenerateWithInvalidTypeName()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                    Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                    Item("BindingError", Glyph.EventPublic, hasNavigationSymbolId:=True, bolded:=True)},
                    bolded:=True))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(530657)>
        Public Sub CodeGenerationItemsShouldNotAppearWhenWorkspaceDoesNotSupportDocumentChanges()
            Dim workspaceSupportsChangeDocument = False
            AssertItemsAre(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220)>
        Public Sub [Enum]()
            AssertItemsAre(
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
                    Item("A", Glyph.EnumMember),
                    Item("B", Glyph.EnumMember),
                    Item("C", Glyph.EnumMember)},
                    bolded:=True))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub Events()
            AssertItemsAre(
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
                    Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
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
                     Item(NavigationItemNew, Glyph.MethodPublic, hasNavigationSymbolId:=False),
                     Item("Finalize", Glyph.MethodProtected, hasNavigationSymbolId:=False),
                     Item("Ev_Event", Glyph.EventPublic, bolded:=True)},
                     bolded:=True),
                Item(String.Format(VBEditorResources.Events, "Class1"), Glyph.EventPublic, children:={
                     Item("Ev_Event", Glyph.EventPublic, hasNavigationSymbolId:=False)},
                     bolded:=False,
                     indent:=1,
                     hasNavigationSymbolId:=False))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub NavigationBetweenFiles()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(566752)>
        Public Sub NavigationWithMethodWithLineContinuation()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(531586)>
        Public Sub NavigationWithMethodWithNoTerminator()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(531586)>
        Public Sub NavigationWithMethodWithDocumentationComment()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(567914)>
        Public Sub NavigationWithMethodWithMultipleLineDeclaration()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074)>
        Public Sub NavigationWithMethodContainingComment()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074)>
        Public Sub NavigationWithMethodContainingBlankLineWithSpaces()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074)>
        Public Sub NavigationWithMethodContainingBlankLineWithNoSpaces()
            AssertNavigationPoint(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(605074)>
        Public Sub NavigationWithMethodContainingBlankLineWithSomeSpaces()
            AssertNavigationPoint(
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
        End Sub
    End Class
End Namespace
