' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
Imports Microsoft.CodeAnalysis.ExtractInterface
Imports Microsoft.CodeAnalysis.VisualBasic.ExtractInterface
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractInterface
    Public Class ExtractInterfaceTests
        Inherits AbstractExtractInterfaceTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_CaretInMethod() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()
        $$
    End Sub
End Class
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_CaretAfterEndClass() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()
    End Sub
End Class$$
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_CaretBeforeClassKeyword() As Task
            Dim markup = <text>Imports System
$$Class TestClass
    Public Sub Goo()
    End Sub
End Class
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromInnerClass1() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()
    End Sub

    Class AnotherClass
        $$Public Sub Bar()
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Bar")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromInnerClass2() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()
    End Sub

    $$Class AnotherClass
        Public Sub Bar()
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Bar")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromOuterClass() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()
    End Sub$$

    Class AnotherClass
        Public Async Function TestBar() As Task
        End Sub
    End Class
End Class
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Goo")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromInterface() As Task
            Dim markup = <text>Imports System
Interface IMyInterface
    Sub Goo()$$
End Interface
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Goo", expectedInterfaceName:="IMyInterface1")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromStruct() As Task
            Dim markup = <text>Imports System
Structure SomeStruct
    Sub Goo()$$
    End Sub
End Structure
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Goo", expectedInterfaceName:="ISomeStruct")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_Invocation_FromNamespace() As Task
            Dim markup = <text>
Namespace Ns$$
    Class TestClass
        Public Async Function TestGoo() As Task
        End Sub
    End Class
End Namespace</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_DoesNotIncludeFields() As Task
            Dim markup = <text>
Class TestClass
    $$Public x As Integer

    Public Sub Goo()
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Goo")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndSet() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGetAndPrivateSet() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicProperty_WithGet() As Task
            Dim markup = <text>
Class TestClass
    Public Readonly Property Prop() As Integer
        $$Get
            Return 5
        End Get
    End Property
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Prop")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_ExcludesPublicProperty_WithPrivateGetAndPrivateSet() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicIndexer() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="Item")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_ExcludesInternalIndexer() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicMethod() As Task
            Dim markup = <text>
Class TestClass$$
    Public Sub M()
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="M")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_ExcludesInternalMethod() As Task
            Dim markup = <text>
Class TestClass$$
    Friend Sub M()
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesAbstractMethod() As Task
            Dim markup = <text>
MustInherit Class TestClass$$
    Public MustOverride Sub M()
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="M")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_IncludesPublicEvent() As Task
            Dim markup = <text>
Class TestClass$$
    Public Event MyEvent()
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedMemberName:="MyEvent")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_ExtractableMembers_ExcludesPrivateEvent() As Task
            Dim markup = <text>
Class TestClass$$
    Private Event MyEvent()
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_DefaultInterfaceName_DoesNotConflictWithOtherTypeNames() As Task
            Dim markup = <text>
Class TestClass$$
    Public Sub Goo()
    End Sub
End Class

Interface ITestClass
End Interface

Structure ITestClass1
End Structure

Class ITestClass2
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceName:="ITestClass3")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_NamespaceName_NoNamespace() As Task
            Dim markup = <text>
Class TestClass$$
    Public Sub Goo()
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedNamespaceName:="")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_NamespaceName_SingleNamespace() As Task
            Dim markup = <text>
Namespace MyNamespace
    Class TestClass$$
        Public Async Function TestGoo() As Task
        End Sub
    End Class
End Namespace</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedNamespaceName:="MyNamespace")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_NamespaceName_NestedNamespaces() As Task
            Dim markup = <text>
Namespace OuterNamespace
    Namespace InnerNamespace
        Class TestClass$$
            Public Sub Goo()
            End Function
        End Class
    End Namespace
End Namespace</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedNamespaceName:="OuterNamespace.InnerNamespace")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_ClassesImplementExtractedInterface() As Task
            Dim markup = <text>
Class TestClass$$
    Public Sub Goo()
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class TestClass
    Implements ITestClass

    Public Sub Goo() Implements ITestClass.Goo
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_StructsImplementExtractedInterface() As Task
            Dim markup = <text>
Structure TestClass$$
    Public Sub Goo()
    End Sub
End Structure</text>.NormalizedValue()
            Dim expectedCode = <text>
Structure TestClass
    Implements ITestClass

    Public Sub Goo() Implements ITestClass.Goo
    End Sub
End Structure</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_InterfacesDoNotImplementExtractedInterface() As Task
            Dim markup = <text>
Interface IMyInterface$$
    Sub Goo()
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Interface IMyInterface
    Sub Goo()
End Interface</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_Methods() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_Events() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_Properties() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_Indexers() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_Imports() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_TypeParameters1() As Task
            Dim markup =
"Imports System.Collections.Generic
Public Class TestClass(Of A, B, C, D, E As F, F, G, H, NO1)$$
    Public Sub Goo1(a As A)
    End Sub

    Public Function Goo2() As B
        Return Nothing
    End Function

    Public Sub Goo3(list As List(Of C))
    End Sub

    Public Event Goo4 As Action

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
End Class"

            Dim expectedInterfaceCode =
"Imports System.Collections.Generic

Public Interface ITestClass(Of A, B, C, E As F, F, G, H)
    WriteOnly Property Prop As List(Of E)
    Default WriteOnly Property Item(list As List(Of List(Of H))) As List(Of G)
    Event Goo4 As Action
    Sub Goo1(a As A)
    Sub Goo3(list As List(Of C))
    Sub Bar1()
    Function Goo2() As B
End Interface
"

            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_TypeParameters2() As Task
            Dim markup = <text>Imports System.Collections.Generic
Friend Class Program(Of A As List(Of B), B As Dictionary(Of List(Of D), List(Of E)), C, D, E)$$
    Public Sub Goo(Of T As List(Of A))(x As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic

Friend Interface IProgram(Of A As List(Of B), B As Dictionary(Of List(Of D), List(Of E)), D, E)
    Sub Goo(Of T As List(Of A))(x As T)
End Interface
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact(Skip:="860565"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_TypeParameters3() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_TypeParameters4() As Task
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact(Skip:="738545"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_VBEvents_TypeParametersAndAccessability() As Task
            Dim markup = <text>Imports System.Collections.Generic
Public Class TestClass(Of A, B, C, D, E As F, F, G, H, NO1)$$
    Public Event Goo4(d as D)
End Class</text>.NormalizedValue()
            Dim expectedInterfaceCode = <text>Imports System.Collections.Generic

Public Interface ITestClass(Of D)
    Event Goo4(d As D)
End Interface
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedInterfaceCode:=expectedInterfaceCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_NewBaseListNonGeneric() As Task
            Dim markup = <text>
Class Program$$
    Public Sub Goo()
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements IProgram

    Public Sub Goo() Implements IProgram.Goo
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_NewBaseListGeneric() As Task
            Dim markup = <text>
Class Program(Of T)$$
    Public Sub Goo(x As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T)
    Implements IProgram(Of T)

    Public Sub Goo(x As T) Implements IProgram(Of T).Goo
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_NewBaseListWithWhereClause() As Task
            Dim markup = <text>
Class Program(Of T As U, U)$$
    Public Sub Goo(x As T, y As U)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T As U, U)
    Implements IProgram(Of T, U)

    Public Sub Goo(x As T, y As U) Implements IProgram(Of T, U).Goo
    End Sub
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_LargerBaseList1() As Task
            Dim markup = <text>
Class Program$$
    Implements ISomeInterface

    Public Sub Goo()
    End Sub
End Class

Interface ISomeInterface
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements ISomeInterface, IProgram

    Public Sub Goo() Implements IProgram.Goo
    End Sub
End Class

Interface ISomeInterface
End Interface</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_LargerBaseList2() As Task
            Dim markup = <text>
Class Program$$
    Implements ISomeInterface
    Implements IProgram

    Public Sub Goo() Implements IProgram.Goo
    End Sub
End Class

Interface ISomeInterface
End Interface

Interface IProgram
    Sub Goo()
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program
    Implements ISomeInterface
    Implements IProgram
    Implements IProgram1

    Public Sub Goo() Implements IProgram.Goo, IProgram1.Goo
    End Sub
End Class

Interface ISomeInterface
End Interface

Interface IProgram
    Sub Goo()
End Interface</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_LargerBaseList3() As Task
            Dim markup = <text>
Class Program(Of T, U)$$
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T, U)
    Implements ISomeInterface(Of T), IProgram(Of T, U)

    Public Sub Goo(t As T, u As U) Implements IProgram(Of T, U).Goo
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_BaseList_LargerBaseList4() As Task
            Dim markup = <text>
Class Program(Of T, U)$$
    Implements ISomeInterface(Of T), ISomeInterface2(Of T, U)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface
Interface ISomeInterface2(Of T, U)
End Interface</text>.NormalizedValue()
            Dim expectedCode = <text>
Class Program(Of T, U)
    Implements ISomeInterface(Of T), ISomeInterface2(Of T, U), IProgram(Of T, U)

    Public Sub Goo(t As T, u As U) Implements IProgram(Of T, U).Goo
    End Sub
End Class


Interface ISomeInterface(Of T)
End Interface
Interface ISomeInterface2(Of T, U)
End Interface</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_UpdateMemberDefinitions_NewImplementsClause() As Task
            Dim markup = <text>
Class C$$
    Public Sub Goo()
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

    Public Sub Goo() Implements IC.Goo
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
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_CodeGen_UpdateMemberDefinitions_AddToExistingImplementsClause() As Task
            Dim markup = <text>
Class C$$
    Implements IC
    Public Sub Goo() Implements IC.Goo
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
    Sub Goo()
    Function Bar() As Integer
End Interface
</text>.NormalizedValue()
            Dim expectedCode = <text>
Class C
    Implements IC, IC1

    Public Sub Goo() Implements IC.Goo, IC1.Goo
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
    Sub Goo()
    Function Bar() As Integer
End Interface
</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedUpdatedOriginalDocumentCode:=expectedCode)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly1() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    $$Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly2() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U)
    $$Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly3() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class$$ Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly4() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, $$U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly5() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program    $$  (Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly6() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

$$Class Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly7() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class $$Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly8() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class$$ Program(Of T As U, U)
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_TypeDiscovery_NameOnly9() As Task
            Dim markup = <text>
Interface ISomeInterface(Of T)
End Interface

Class Program(Of T As U, U) $$
    Implements ISomeInterface(Of T)

    Public Sub Goo(t As T, u As U)
    End Sub
End Class</text>.NormalizedValue()
            Await TestTypeDiscoveryAsync(markup, TypeDiscoveryRule.TypeNameOnly, expectedExtractable:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_GeneratedNameTypeParameterSuffix1() As Task
            Dim markup = <text>
Class Test(Of T)$$
    Public Sub M(a As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T)"
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_GeneratedNameTypeParameterSuffix2() As Task
            Dim markup = <text>
Class Test(Of T, U)$$
    Public Sub M(a As T)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T)"
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_GeneratedNameTypeParameterSuffix3() As Task
            Dim markup = <text>
Class Test(Of T, U)$$
    Public Sub M(a As T, b as U)
    End Sub
End Class</text>.NormalizedValue()
            Dim expectedTypeParameterSuffix = "(Of T, U)"
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=True, expectedTypeParameterSuffix:=expectedTypeParameterSuffix)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_PartialClass() As Task
            Dim workspaceXml =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Partial Class C$$
    Public Sub Goo()
    End Sub
    Public Function Bar() As Integer
        Return 5
    End Function
End Class</Document>
        <Document>
Partial Class C
    Public Event Goo4 As Action
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

    Public Sub Goo() Implements IC.Goo
    End Sub
    Public Function Bar() As Integer Implements IC.Bar
        Return 5
    End Function
End Class</text>.NormalizedValue()

            Dim expectedDoc2Text = <text>
Partial Class C
    Public Event Goo4 As Action Implements IC.Goo4

    Public WriteOnly Property Prop() As List(Of E) Implements IC.Prop
        Set(value As List(Of E))
        End Set
    End Property
End Class</text>.NormalizedValue()

            Dim workspace = EditorTestWorkspace.Create(workspaceXml, composition:=ExtractInterfaceTestState.Composition)
            Using testState = New ExtractInterfaceTestState(workspace)
                Dim result = Await testState.ExtractViaCommandAsync()
                Assert.True(result.Succeeded)

                Dim part1Id = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).Id
                Dim part2Id = workspace.Documents.Single(Function(d) Not d.CursorPosition.HasValue).Id

                Assert.Equal(expectedDoc1Text, (Await result.UpdatedSolution.GetDocument(part1Id).GetTextAsync()).ToString())
                Assert.Equal(expectedDoc2Text, (Await result.UpdatedSolution.GetDocument(part2Id).GetTextAsync()).ToString())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_NonEmptyRootNamespace() As Task
            Dim markup = <text>Imports System
Class TestClass
    Public Sub Goo()$$
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>Imports System
Class TestClass
    Implements ITestClass

    Public Sub Goo() Implements ITestClass.Goo
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>Interface ITestClass
    Sub Goo()
End Interface
</text>.NormalizedValue()

            Await TestExtractInterfaceCommandVisualBasicAsync(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_NonEmptyRootNamespace_ClassInAdditionalNamespace() As Task
            Dim markup = <text>Imports System
Namespace NS1
    Class TestClass
        Public Sub Goo()$$
        End Sub
    End Class
End Namespace
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>Imports System
Namespace NS1
    Class TestClass
        Implements ITestClass

        Public Sub Goo() Implements ITestClass.Goo
        End Sub
    End Class
End Namespace
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>Namespace NS1
    Interface ITestClass
        Sub Goo()
    End Interface
End Namespace
</text>.NormalizedValue()

            Await TestExtractInterfaceCommandVisualBasicAsync(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub TestExtractInterfaceCommandDisabledInSubmission()
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Public Class C
                            Public Sub M()
                            End Sub
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                composition:=EditorTestCompositions.EditorFeatures)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New ExtractInterfaceCommandHandler(workspace.GetService(Of IThreadingContext))

                Dim state = handler.GetCommandState(New ExtractInterfaceCommandArgs(textView, textView.TextBuffer))
                Assert.True(state.IsUnspecified)
            End Using
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/23855")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_WithCopyright1() As Task
            Dim markup = <text>'' Copyright

Imports System
Class TestClass
    Public Sub Goo()$$
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>'' Copyright

Imports System
Class TestClass
    Implements ITestClass

    Public Sub Goo() Implements ITestClass.Goo
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>'' Copyright

Interface ITestClass
    Sub Goo()
End Interface
</text>.NormalizedValue()

            Await TestExtractInterfaceCommandVisualBasicAsync(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/23855")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_WithCopyright2() As Task
            Dim markup = <text>'' Copyright

Imports System

Class Program
    Class A$$
        Sub Main(args As String())
        End Sub
    End Class
End Class
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>'' Copyright

Imports System

Class Program
    Class A
        Implements IA

        Sub Main(args As String()) Implements IA.Main
        End Sub
    End Class
End Class
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>'' Copyright

Interface IA
    Sub Main(args() As String)
End Interface
</text>.NormalizedValue()

            Await TestExtractInterfaceCommandVisualBasicAsync(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/43952")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_IgnoreWithEvents() As Task
            Dim markup = <text>Class C$$
    Public WithEvents X As Object
End Class</text>.NormalizedValue()
            Await TestExtractInterfaceCommandVisualBasicAsync(markup, expectedSuccess:=False)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/43952")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_IgnoreWithEvents2() As Task
            Dim markup = <text>Class C$$
    Public WithEvents X As Object

    Sub Method()
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedUpdatedDocument = <text>Class C
    Implements IC

    Public WithEvents X As Object

    Sub Method() Implements IC.Method
    End Sub
End Class
</text>.NormalizedValue()

            Dim expectedInterfaceCode = <text>Interface IC
    Sub Method()
End Interface
</text>.NormalizedValue()

            Await TestExtractInterfaceCommandVisualBasicAsync(
                markup,
                expectedSuccess:=True,
                expectedUpdatedOriginalDocumentCode:=expectedUpdatedDocument,
                expectedInterfaceCode:=expectedInterfaceCode,
                rootNamespace:="RootNamespace")
        End Function

        Private Shared Async Function TestTypeDiscoveryAsync(markup As String, typeDiscoveryRule As TypeDiscoveryRule, expectedExtractable As Boolean) As System.Threading.Tasks.Task
            Using testState = ExtractInterfaceTestState.Create(markup, LanguageNames.VisualBasic, compilationOptions:=Nothing)
                Dim result = Await testState.GetTypeAnalysisResultAsync(typeDiscoveryRule)
                Assert.Equal(expectedExtractable, result.CanExtractInterface)
            End Using
        End Function
    End Class
End Namespace
