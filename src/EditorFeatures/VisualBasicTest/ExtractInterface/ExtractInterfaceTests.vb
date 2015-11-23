' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractInterface
Imports Microsoft.CodeAnalysis.ExtractInterface

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractInterface
    Public Class ExtractInterfaceTests
        Inherits AbstractExtractInterfaceTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_CaretInMethod()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()
        $$
    End Sub
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_CaretAfterEndClass()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()
    End Sub
End Class$$
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_CaretBeforeClassKeyword()
            Dim markup = <text>Imports System
$$Class TestClass
    Public Sub Foo()
    End Sub
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromInnerClass1()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()
    End Sub

    Class AnotherClass
        $$Public Sub Bar()
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromInnerClass2()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()
    End Sub

    $$Class AnotherClass
        Public Sub Bar()
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromOuterClass()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()
    End Sub$$

    Class AnotherClass
        Public Sub Bar()
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromInterface()
            Dim markup = <text>Imports System
Interface IMyInterface
    Sub Foo()$$
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Foo", expectedInterfaceName:="IMyInterface1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromStruct()
            Dim markup = <text>Imports System
Structure SomeStruct
    Sub Foo()$$
    End Sub
End Structure
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Foo", expectedInterfaceName:="ISomeStruct")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_Invocation_FromNamespace()
            Dim markup = <text>
Namespace Ns$$
    Class TestClass
        Public Sub Foo()
        End Sub
    End Class
End Namespace</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_DoesNotIncludeFields()
            Dim markup = <text>
Class TestClass
    $$Public x As Integer

    Public Sub Foo()
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndSet()
            Dim markup = <text>
Class TestClass
    Public Property Prop() As Integer
        $$Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndPrivateSet()
            Dim markup = <text>
Class TestClass
    Public Property Prop() As Integer
        $$Get
            Return 5
        End Get
        Private Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGet()
            Dim markup = <text>
Class TestClass
    Public Readonly Property Prop() As Integer
        $$Get
            Return 5
        End Get
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_ExcludesPublicProperty_WithPrivateGetAndPrivateSet()
            Dim markup = <text>
Class TestClass
    Public Property Prop() As Integer
        $$Private Get
            Return 5
        End Get
        Private Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicIndexer()
            Dim markup = <text>
Class TestClass$$
    Default Public Property Item(index As String) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="Item")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_ExcludesInternalIndexer()
            Dim markup = <text>
Class TestClass$$
    Default Friend Property Item(index As String) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicMethod()
            Dim markup = <text>
Class TestClass$$
    Public Sub M()
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="M")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_ExcludesInternalMethod()
            Dim markup = <text>
Class TestClass$$
    Friend Sub M()
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesAbstractMethod()
            Dim markup = <text>
MustInherit Class TestClass$$
    Public MustOverride Sub M()
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="M")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_IncludesPublicEvent()
            Dim markup = <text>
Class TestClass$$
    Public Event MyEvent()
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedMemberName:="MyEvent")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_ExtractableMembers_ExcludesPrivateEvent()
            Dim markup = <text>
Class TestClass$$
    Private Event MyEvent()
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_DefaultInterfaceName_DoesNotConflictWithOtherTypeNames()
            Dim markup = <text>
Class TestClass$$
    Public Sub Foo()
    End Sub
End Class

Interface ITestClass
End Interface

Structure ITestClass1
End Structure

Class ITestClass2
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceName:="ITestClass3")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_NamespaceName_NoNamespace()
            Dim markup = <text>
Class TestClass$$
    Public Sub Foo()
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedNamespaceName:="")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_NamespaceName_SingleNamespace()
            Dim markup = <text>
Namespace MyNamespace
    Class TestClass$$
        Public Sub Foo()
        End Sub
    End Class
End Namespace</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedNamespaceName:="MyNamespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_NamespaceName_NestedNamespaces()
            Dim markup = <text>
Namespace OuterNamespace
    Namespace InnerNamespace
        Class TestClass$$
            Public Sub Foo()
            End Sub
        End Class
    End Namespace
End Namespace</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedNamespaceName:="OuterNamespace.InnerNamespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_ClassesImplementExtractedInterface()
            Dim markup = <text>
Class TestClass$$
    Public Sub Foo()
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class TestClass
    Implements ITestClass
    Public Sub Foo() Implements ITestClass.Foo
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_StructsImplementExtractedInterface()
            Dim markup = <text>
Structure TestClass$$
    Public Sub Foo()
    End Sub
End Structure</text>.NormalizedValue()
            Dim expectedCode = <text>
Structure TestClass
    Implements ITestClass
    Public Sub Foo() Implements ITestClass.Foo
    End Sub
End Structure</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_InterfacesDoNotImplementExtractedInterface()
            Dim markup = <text>
Interface IMyInterface$$
    Sub Foo()
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Interface IMyInterface
    Sub Foo()
End Interface</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_Methods()
            Dim markup = <text>
Imports System
MustInherit Class TestClass$$
    Public Sub ExtractableMethod_Normal()
    End Sub
    Public MustOverride Sub ExtractableMethod_ParameterTypes(x As System.Diagnostics.CorrelationManager, Optional y As Nullable(Of Int32) = 7, Optional z As String = "42")
    Public MustOverride Sub ExtractableMethod_Abstract()
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Diagnostics

Interface ITestClass
    Sub ExtractableMethod_Normal()
    Sub ExtractableMethod_ParameterTypes(x As CorrelationManager, Optional y As Integer? = 7, Optional z As String = "42")
    Sub ExtractableMethod_Abstract()
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_Events()
            Dim markup = <text>
Imports System

MustInherit Class TestClass$$
    Public Event ExtractableEvent1()
    Public Event ExtractableEvent2(x As System.Nullable(Of System.Int32))
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Interface ITestClass
    Event ExtractableEvent1()
    Event ExtractableEvent2(x As Integer?)
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_Properties()
            Dim markup = <text>
Imports System

MustInherit Class TestClass$$
    Public Property ExtractableProp() As Integer
        Get
            Return 5
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property

    Public ReadOnly Property PropertyExtractableProp_GetOnly() As Integer
        Get
            Return 5
        End Get
    End Property

    Public WriteOnly Property ExtractableProp_SetOnly() As Integer
        Set(ByVal value As Integer)
        End Set
    End Property

    Public Property ExtractableProp_SetPrivate() As Integer
        Get
            Return 5
        End Get
        Private Set(ByVal value As Integer)
        End Set
    End Property

    Public Property ExtractableProp_GetPrivate() As Integer
        Private Get
            Return 5
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property

    Public Property ExtractableProp_SetInternal() As Integer
        Get
            Return 5
        End Get
        Friend Set(ByVal value As Integer)
        End Set
    End Property

    Public Property ExtractableProp_GetInternal As Integer
        Friend Get
            Return 5
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Interface ITestClass
    Property ExtractableProp As Integer
    ReadOnly Property PropertyExtractableProp_GetOnly As Integer
    WriteOnly Property ExtractableProp_SetOnly As Integer
    ReadOnly Property ExtractableProp_SetPrivate As Integer
    WriteOnly Property ExtractableProp_GetPrivate As Integer
    ReadOnly Property ExtractableProp_SetInternal As Integer
    WriteOnly Property ExtractableProp_GetInternal As Integer
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_Indexers()
            Dim markup = <text>
MustInherit Class TestClass$$
    Default WriteOnly Property Item(index As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property

    Default ReadOnly Property Item(index As String) As Integer
        Get
            Return 5
        End Get
    End Property

    Default Property Item(index As Double) As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Default Property Item(x As System.Nullable(Of System.Int32), Optional y As String = "42") As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Interface ITestClass
    Default WriteOnly Property Item(index As Integer) As Integer
    Default ReadOnly Property Item(index As String) As Integer
    Default Property Item(index As Double) As Integer
    Default Property Item(x As Integer?, Optional y As String = "42") As Integer
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_Imports()
            Dim markup = <text>Imports System.Collections.Generic
Public Class TestClass$$
    Public Function M1(x As System.Globalization.Calendar) As System.Diagnostics.BooleanSwitch
        Return Nothing
    End Function

    Public Sub M2(x As System.Collections.Generic.List(Of System.IO.BinaryWriter))
    End Sub

    Public Sub M3(Of T As System.Net.WebProxy)()
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net

Public Interface ITestClass
    Sub M2(x As List(Of BinaryWriter))
    Sub M3(Of T As WebProxy)()
    Function M1(x As Calendar) As BooleanSwitch
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_TypeParameters1()
            Dim markup = <text>Imports System.Collections.Generic
Public Class TestClass(Of A, B, C, D, E As F, F, G, H, NO1)$$
    Public Sub Foo1(a As A)
    End Sub

    Public Function Foo2() As B
        Return Nothing
    End Function

    Public Sub Foo3(list As List(Of C))
    End Sub

    Public Event Foo4 As Action

    Public WriteOnly Property Prop() As List(Of E)
        Set(value As List(Of E))
        End Set
    End Property

    Default WriteOnly Property Item(list As List(Of List(Of H))) As List(Of G)
        Set(value As List(Of G))
        End Set
    End Property

    Public Sub Bar1()
        Dim x As NO1 = Nothing
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic

Public Interface ITestClass(Of A, B, C, E As F, F, G, H)
    WriteOnly Property Prop As List(Of E)
    Default WriteOnly Property Item(list As List(Of List(Of H))) As List(Of G)
    Event Foo4 As Action
    Sub Foo1(a As A)
    Sub Foo3(list As List(Of C))
    Function Foo2() As B
    Sub Bar1()
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_TypeParameters2()
            Dim markup = <text>Imports System.Collections.Generic
Friend Class Program(Of A As List(Of B), B As Dictionary(Of List(Of D), List(Of E)), C, D, E)$$
    Public Sub Foo(Of T As List(Of A))(x As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic

Friend Interface IProgram(Of A As List(Of B), B As Dictionary(Of List(Of D), List(Of E)), D, E)
    Sub Foo(Of T As List(Of A))(x As T)
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact(Skip:="860565"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_TypeParameters3()
            ' Note: This test should pass after RI from Airstream branch to Main.
            Dim markup = <text>
Class Class1(Of A, B)$$
    Public Sub Method(p1 As A, p2 As Class2)
    End Sub

    Public Class Class2
    End Class
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Interface IClass1(Of A, B)
    Sub Method(p1 As A, p2 As Class1(Of A, B).Class2)
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_TypeParameters4()
            Dim markup = <text>
Class C1(Of A)
    Public Class C2(Of B As New)
        Public Class C3(Of C As System.Collections.ICollection)
            Public Class C4$$
                Public Function Method() As A
                    Return Nothing
                End Function

                Public WriteOnly Property P1()
                    Set(value)
                    End Set
                End Property

                Default Public ReadOnly Property I1(i As Integer) As C
                    Get
                        Return Nothing
                    End Get
                End Property
            End Class
        End Class
    End Class
End Class
</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections

Public Interface IC4(Of A, C As ICollection)
    WriteOnly Property P1 As Object
    Default ReadOnly Property I1(i As Integer) As C
    Function Method() As A
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact(Skip:="738545"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_VBEvents_TypeParametersAndAccessability()
            Dim markup = <text>Imports System.Collections.Generic
Public Class TestClass(Of A, B, C, D, E As F, F, G, H, NO1)$$
    Public Event Foo4(d as D)
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic

Public Interface ITestClass(Of D)
    Event Foo4(d As D)
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_NewBaseListNonGeneric()
            Dim markup = <text>
Class Program$$
    Public Sub Foo()
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements IProgram
    Public Sub Foo() Implements IProgram.Foo
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_NewBaseListGeneric()
            Dim markup = <text>
Class Program(Of T)$$
    Public Sub Foo(x As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T)
    Implements IProgram(Of T)
    Public Sub Foo(x As T) Implements IProgram(Of T).Foo
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_NewBaseListWithWhereClause()
            Dim markup = <text>
Class Program(Of T As U, U)$$
    Public Sub Foo(x As T, y As U)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T As U, U)
    Implements IProgram(Of T, U)
    Public Sub Foo(x As T, y As U) Implements IProgram(Of T, U).Foo
    End Sub
End Class</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_LargerBaseList1()
            Dim markup = <text>
Class Program$$
    Implements ISomeInterface

    Public Sub Foo()
    End Sub
End Class

Interface ISomeInterface
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements ISomeInterface
    Implements IProgram

    Public Sub Foo() Implements IProgram.Foo
    End Sub
End Class

Interface ISomeInterface
End Interface</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_LargerBaseList2()
            Dim markup = <text>
Class Program$$
    Implements ISomeInterface
    Implements IProgram

    Public Sub Foo() Implements IProgram.Foo
    End Sub
End Class

Interface ISomeInterface
End Interface

Interface IProgram
    Sub Foo()
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements ISomeInterface
    Implements IProgram
    Implements IProgram1

    Public Sub Foo() Implements IProgram.Foo, IProgram1.Foo
    End Sub
End Class

Interface ISomeInterface
End Interface

Interface IProgram
    Sub Foo()
End Interface</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_LargerBaseList3()
            Dim markup = <text>
Class Program(Of T, U)$$
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T, U)
    Implements ISomeInterface(Of T)
    Implements IProgram(Of T, U)

    Public Sub Foo(t As T, u As U) Implements IProgram(Of T, U).Foo
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_BaseList_LargerBaseList4()
            Dim markup = <text>
Class Program(Of T, U)$$
    Implements ISomeInterface(Of T), ISomeInterface2(Of T, U)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface
Interface ISomeInterface2(Of T, U)
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T, U)
    Implements ISomeInterface(Of T), ISomeInterface2(Of T, U)
    Implements IProgram(Of T, U)

    Public Sub Foo(t As T, u As U) Implements IProgram(Of T, U).Foo
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface
Interface ISomeInterface2(Of T, U)
End Interface</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_UpdateMemberDefinitions_NewImplementsClause()
            Dim markup = <text>
Class C$$
    Public Sub Foo()
    End Sub

    Public Function Bar() As Integer
        Return 4
    End Function

    Public Event E As Action

    Public Property Prop As Integer
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</text>.NormalizedValue()
            Dim expectedCode = <text>
Class C
    Implements IC
    Public Sub Foo() Implements IC.Foo
    End Sub

    Public Function Bar() As Integer Implements IC.Bar
        Return 4
    End Function

    Public Event E As Action Implements IC.E

    Public Property Prop As Integer Implements IC.Prop
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_CodeGen_UpdateMemberDefinitions_AddToExistingImplementsClause()
            Dim markup = <text>
Class C$$
    Implements IC
    Public Sub Foo() Implements IC.Foo
    End Sub

    Public Function Bar() As Integer Implements IC.Bar
        Return 4
    End Function

    Public Event E As Action Implements IC.E

    Public Property Prop As Integer Implements IC.Prop
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

Interface IC
    Property Prop As Integer
    Event E As Action
    Sub Foo()
    Function Bar() As Integer
End Interface
</text>.NormalizedValue()
            Dim expectedCode = <text>
Class C
    Implements IC
    Implements IC1
    Public Sub Foo() Implements IC.Foo, IC1.Foo
    End Sub

    Public Function Bar() As Integer Implements IC.Bar, IC1.Bar
        Return 4
    End Function

    Public Event E As Action Implements IC.E, IC1.E

    Public Property Prop As Integer Implements IC.Prop, IC1.Prop
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

Interface IC
    Property Prop As Integer
    Event E As Action
    Sub Foo()
    Function Bar() As Integer
End Interface
</text>.NormalizedValue()
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly1()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    $$Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly2()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U)
    $$Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly3()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class$$ Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly4()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, $$U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly5()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program    $$  (Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly6()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

$$Class Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly7()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class $$Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly8()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class$$ Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_TypeDiscovery_NameOnly9()
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U) $$
    Implements ISomeInterface(Of T)

    Public Sub Foo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            TestTypeDiscovery(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameTypeParameterSuffix1()
            Dim markup = <text>
Class Test(Of T)$$
    Public Sub M(a As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T)"
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameTypeParameterSuffix2()
            Dim markup = <text>
Class Test(Of T, U)$$
    Public Sub M(a As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T)"
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameTypeParameterSuffix3()
            Dim markup = <text>
Class Test(Of T, U)$$
    Public Sub M(a As T, b as U)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T, U)"
            TestExtractInterfaceCommandVisualBasic(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_PartialClass()
            Dim workspaceXml =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Partial Class C$$
    Public Sub Foo()
    End Sub
    Public Function Bar() As Integer
        Return 5
    End Function
End Class</Document>
        <Document>
Partial Class C
    Public Event Foo4 As Action
    Public WriteOnly Property Prop() As List(Of E)
        Set(value As List(Of E))
        End Set
    End Property
End Class</Document>
    </Project>
</Workspace>

            Dim expectedDoc1Text = <text>
Partial Class C
    Implements IC
    Public Sub Foo() Implements IC.Foo
    End Sub
    Public Function Bar() As Integer Implements IC.Bar
        Return 5
    End Function
End Class</text>.NormalizedValue()

            Dim expectedDoc2Text = <text>
Partial Class C
    Public Event Foo4 As Action Implements IC.Foo4

    Public WriteOnly Property Prop() As List(Of E) Implements IC.Prop
        Set(value As List(Of E))
        End Set
    End Property
End Class</text>.NormalizedValue()

            Dim workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml, exportProvider:=ExtractInterfaceTestState.ExportProvider)
            Using testState = New ExtractInterfaceTestState(workspace)
                Dim result = testState.ExtractViaCommand()
                Assert.True(result.Succeeded)

                Dim part1Id = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).Id
                Dim part2Id = workspace.Documents.Single(Function(d) Not d.CursorPosition.HasValue).Id

                Assert.Equal(expectedDoc1Text, result.UpdatedSolution.GetDocument(part1Id).GetTextAsync().Result.ToString())
                Assert.Equal(expectedDoc2Text, result.UpdatedSolution.GetDocument(part2Id).GetTextAsync().Result.ToString())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_NonEmptyRootNamespace()
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Foo()$$
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>Imports System
Class TestClass
    Implements ITestClass
    Public Sub Foo() Implements ITestClass.Foo
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>Interface ITestClass
    Sub Foo()
End Interface
</text>.NormalizedValue()

            TestExtractInterfaceCommandVisualBasic(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_NonEmptyRootNamespace_ClassInAdditionalNamespace()
            Dim markup = <text>Imports System
Namespace NS1
    Class TestClass
        Public Sub Foo()$$
        End Sub
    End Class
End Namespace
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>Imports System
Namespace NS1
    Class TestClass
        Implements ITestClass
        Public Sub Foo() Implements ITestClass.Foo
        End Sub
    End Class
End Namespace
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>Namespace NS1
    Interface ITestClass
        Sub Foo()
    End Interface
End Namespace
</text>.NormalizedValue()

            TestExtractInterfaceCommandVisualBasic(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub ExtractInterfaceCommandDisabledInSubmission()
            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveDocumentSupportsFeatureService)))

            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Public Class C
                            Public Sub M()
                            End Sub
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New ExtractInterfaceCommandHandler()
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim state = handler.GetCommandState(New Commands.ExtractInterfaceCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)
            End Using
        End Sub

        Private Shared Sub TestTypeDiscovery(markup As String, typeDiscoveryRule As TypeDiscoveryRule, expectedExtractable As Boolean)
            Using testState = New ExtractInterfaceTestState(markup, LanguageNames.VisualBasic, compilationOptions:=Nothing)
                Dim result = testState.GetTypeAnalysisResult(typeDiscoveryRule)
                Assert.Equal(expectedExtractable, result.CanExtractInterface)
            End Using
        End Sub
    End Class
End Namespace
