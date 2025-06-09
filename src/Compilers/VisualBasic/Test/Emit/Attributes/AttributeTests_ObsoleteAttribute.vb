' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_ObsoleteAttribute
        Inherits BasicTestBase

        <Fact()>
        Public Sub TestObsoleteAttributeOnTypes()
            Dim source =
<compilation>
    <file name="test.vb"><![CDATA[
Imports System
 
Module Module1
    Dim field1 As Class1
    Property Prop1 As Class1
    Sub Method1(c As Class1) 
    End Sub

    Sub Main()
	Dim c As Class1 = Nothing
        field1 = c
        Prop1 = c
        Method1(New Class1())

        Dim x As Mydeleg = Function(i) i
    End Sub
End Module

<Obsolete("Do not use this type", True)>
Class Class1
End Class

<Obsolete("Do not use A1", False)>
<A2>
Class A1 
  Inherits Attribute 
End Class

<Obsolete>
<A1>
Class A2
 Inherits Attribute
End Class

<A1>
Class A3
    Inherits Attribute
End Class

Class AttrWithType
    Inherits Attribute
    Sub New(t As Type)
    End Sub
End Class

<Obsolete>
<Another>
Class G(Of T, U)
End Class

<Obsolete>
<AttrWithType(GetType(G(Of Integer, AnotherAttribute)))>
Class AnotherAttribute
    Inherits Attribute
End Class

<AttrWithType(GetType(G(Of Integer, AnotherAttribute)))>
Class AnotherAttribute1
    Inherits Attribute
End Class

<System.Obsolete("This message" & " should be concat'ed", Not (False))>
<SelfRecursive1>
Class SelfRecursive1Attribute
    Inherits Attribute
End Class

<Obsolete>
Public Delegate Function Mydeleg(x As Integer) As Integer

<GooAttribute.BarAttribute.Baz>
<Obsolete("Blah")>
Class GooAttribute
    Inherits Attribute
    Class BazAttribute
        Inherits Attribute
    End Class
    Class BarAttribute
        Inherits GooAttribute
    End Class
End Class

Interface IGoo(Of T)
End Interface

<Obsolete>
Class SelfReferenceInBase
    Implements IGoo(Of SelfReferenceInBase)
End Class

Class SelfReferenceInBase1
    Implements IGoo(Of SelfReferenceInBase)
End Class

]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "AnotherAttribute").WithArguments("AnotherAttribute"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "A1").WithArguments("A1", "Do not use A1"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "G(Of Integer, AnotherAttribute)").WithArguments("G(Of Integer, AnotherAttribute)"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Class1").WithArguments("Class1", "Do not use this type"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Class1").WithArguments("Class1", "Do not use this type"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Class1").WithArguments("Class1", "Do not use this type"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "SelfReferenceInBase").WithArguments("SelfReferenceInBase"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Class1").WithArguments("Class1", "Do not use this type"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Class1").WithArguments("Class1", "Do not use this type"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "Mydeleg").WithArguments("Mydeleg"))
        End Sub

        <Fact()>
        Public Sub TestObsoleteAttributeOnMembers()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Test
    Public Shared Sub Main()
        ObsoleteMethod1()
        ObsoleteMethod2()
        ObsoleteMethod3()
        ObsoleteMethod5()

        Dim t As Test = New Test()

        t.ObsoleteMethod4()
        Dim f = t.field1
        Dim p1 = t.Property1
        Dim p2 = t.Property2
        AddHandler t.event1, Sub() Return

        t.ObsoleteExtensionMethod1()

        Dim func As Action(Of Integer) = AddressOf t.ObsoleteMethod4
        func(1)
        Dim func1 As Action = AddressOf t.ObsoleteMethod4
        func1()
        Dim t1 As Test = New Test With {.Property1 = 10, .Property2 = 20}
        Dim i1 = t1(10)

        Dim gt As GenericTest(Of Integer) = New GenericTest(Of Integer)()
        gt.ObsoleteMethod1(Of Double)()
        Dim gf = gt.field1
        Dim gp1 = gt.Property1
        AddHandler gt.event1, Sub(i) Return
    End Sub

    <Obsolete>
    Public Shared Sub ObsoleteMethod1()
    End Sub

    <Obsolete("Do not call this method")>
    Public Shared Sub ObsoleteMethod2()
    End Sub

    <Obsolete("Do not call this method", True)>
    Public Shared Sub ObsoleteMethod3()
    End Sub

    <Obsolete("Do not call this method")>
    Public Sub ObsoleteMethod4()
    End Sub

    <Obsolete("Do not call this method")>
    Public Sub ObsoleteMethod4(x As Integer)
    End Sub

    <Obsolete(Nothing, True)>
    Public Shared Sub ObsoleteMethod5()
    End Sub

    <Obsolete>
    Public Sub New()
    End Sub

    <Obsolete("Do not use this field")>
    Public field1 As Integer = 0

    <Obsolete("Do not use this property")>
    Public Property Property1 As Integer

    <Obsolete("Do not use this property")>
    Public Property Property2 As Integer
        Get
            Return 11
        End Get
        Set(value As Integer)
        End Set
    End Property

    <Obsolete("Do not use this event")>
    Public Event event1 As Action

    Public ReadOnly Property Prop2 As Integer
        <Obsolete>
        Get
            Return 10
        End Get
    End Property

    Public Property Prop3 As Integer
        Get
            Return 10
        End Get
        <Obsolete>
        Set(value As Integer)
        End Set
    End Property

    Public Custom Event event2 As Action
        <Obsolete>
        AddHandler(value As Action)
        End AddHandler
        <Obsolete>
        RemoveHandler(value As Action)
        End RemoveHandler
        <Obsolete>
        RaiseEvent()
        End RaiseEvent
    End Event

    <Obsolete>
    Default Public ReadOnly Property Item(x As Integer)
        Get
            Return 10
        End Get
    End Property
End Class

Public Class GenericTest(Of T)
    <Obsolete>
    Public Sub ObsoleteMethod1(Of U)()
    End Sub

    <Obsolete("Do not use this field")>
    Public field1 As T = Nothing

    <Obsolete("Do not use this property")>
    Public Property Property1 As T

    <Obsolete("Do not use this event")>
    Public Event event1 As Action(Of T)
End Class

Public Module TestExtension
    <Obsolete("Do not call this extension method")>
    <Extension>
    Public Sub ObsoleteExtensionMethod1(t As Test)
    End Sub
End Module
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.References.SystemCore}).AssertTheseDiagnostics(
            <![CDATA[
BC40008: 'Public Shared Sub ObsoleteMethod1()' is obsolete.
        ObsoleteMethod1()
        ~~~~~~~~~~~~~~~~~
BC40000: 'Public Shared Sub ObsoleteMethod2()' is obsolete: 'Do not call this method'.
        ObsoleteMethod2()
        ~~~~~~~~~~~~~~~~~
BC30668: 'Public Shared Sub ObsoleteMethod3()' is obsolete: 'Do not call this method'.
        ObsoleteMethod3()
        ~~~~~~~~~~~~~~~~~
BC31075: 'Public Shared Sub ObsoleteMethod5()' is obsolete.
        ObsoleteMethod5()
        ~~~~~~~~~~~~~~~~~
BC40008: 'Public Sub New()' is obsolete.
        Dim t As Test = New Test()
                        ~~~~~~~~~~
BC40000: 'Public Sub ObsoleteMethod4()' is obsolete: 'Do not call this method'.
        t.ObsoleteMethod4()
        ~~~~~~~~~~~~~~~~~~~
BC40000: 'Public field1 As Integer' is obsolete: 'Do not use this field'.
        Dim f = t.field1
                ~~~~~~~~
BC40000: 'Public Property Property1 As Integer' is obsolete: 'Do not use this property'.
        Dim p1 = t.Property1
                 ~~~~~~~~~~~
BC40000: 'Public Property Property2 As Integer' is obsolete: 'Do not use this property'.
        Dim p2 = t.Property2
                 ~~~~~~~~~~~
BC40000: 'Public Event event1 As Action' is obsolete: 'Do not use this event'.
        AddHandler t.event1, Sub() Return
                   ~~~~~~~~
BC40000: 'Public Sub ObsoleteExtensionMethod1()' is obsolete: 'Do not call this extension method'.
        t.ObsoleteExtensionMethod1()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40000: 'Public Sub ObsoleteMethod4(x As Integer)' is obsolete: 'Do not call this method'.
        Dim func As Action(Of Integer) = AddressOf t.ObsoleteMethod4
                                                   ~~~~~~~~~~~~~~~~~
BC40000: 'Public Sub ObsoleteMethod4()' is obsolete: 'Do not call this method'.
        Dim func1 As Action = AddressOf t.ObsoleteMethod4
                                        ~~~~~~~~~~~~~~~~~
BC40008: 'Public Sub New()' is obsolete.
        Dim t1 As Test = New Test With {.Property1 = 10, .Property2 = 20}
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40000: 'Public Property Property1 As Integer' is obsolete: 'Do not use this property'.
        Dim t1 As Test = New Test With {.Property1 = 10, .Property2 = 20}
                                         ~~~~~~~~~
BC40000: 'Public Property Property2 As Integer' is obsolete: 'Do not use this property'.
        Dim t1 As Test = New Test With {.Property1 = 10, .Property2 = 20}
                                                          ~~~~~~~~~
BC40008: 'Public ReadOnly Default Property Item(x As Integer) As Object' is obsolete.
        Dim i1 = t1(10)
                 ~~~~~~
BC40008: 'Public Sub ObsoleteMethod1(Of Double)()' is obsolete.
        gt.ObsoleteMethod1(Of Double)()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40000: 'Public field1 As Integer' is obsolete: 'Do not use this field'.
        Dim gf = gt.field1
                 ~~~~~~~~~
BC40000: 'Public Property Property1 As Integer' is obsolete: 'Do not use this property'.
        Dim gp1 = gt.Property1
                  ~~~~~~~~~~~~
BC40000: 'Public Event event1 As Action(Of Integer)' is obsolete: 'Do not use this event'.
        AddHandler gt.event1, Sub(i) Return
                   ~~~~~~~~~
BC31142: 'System.ObsoleteAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
        <Obsolete>
        ~~~~~~~~~~~
BC31142: 'System.ObsoleteAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
        <Obsolete>
        ~~~~~~~~~~~
BC31142: 'System.ObsoleteAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
        <Obsolete>
        ~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeOnOperators()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Test
    Public Shared Sub Main()

        Dim t As New Test()
        t = 10
        t = CType("10", Test)

        Dim c As New Test()
        Dim c1 As Test = -c
        Dim b1 As Boolean = If(c, True, False)
        If (c AndAlso c1) Then
            c1 += c
        End If
    End Sub

    <Obsolete>
    Public Shared Widening Operator CType(x As Integer) As Test
        Return New Test()
    End Operator

    <Obsolete>
    Public Shared Narrowing Operator CType(x As String) As Test
        Return New Test()
    End Operator

    <Obsolete>
    Public Shared Operator -(x As test) As Test
        Return New Test()
    End Operator

    <Obsolete>
    Public Shared Operator IsTrue(x As Test) As Boolean
        Return True
    End Operator

    <Obsolete>
    Public Shared Operator IsFalse(x As test) As Boolean
        Return False
    End Operator

    <Obsolete>
    Public Shared Operator +(x As Test, y As Test) As Test
        Return New Test()
    End Operator

    <Obsolete>
    Public Shared Operator And(x As Test, y As Test) As Test
        Return New Test()
    End Operator
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "10").WithArguments("Public Shared Widening Operator CType(x As Integer) As Test"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "CType(""10"", Test)").WithArguments("Public Shared Narrowing Operator CType(x As String) As Test"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "-c").WithArguments("Public Shared Operator -(x As Test) As Test"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "c").WithArguments("Public Shared Operator IsTrue(x As Test) As Boolean"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "c AndAlso c1").WithArguments("Public Shared Operator And(x As Test, y As Test) As Test"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "c AndAlso c1").WithArguments("Public Shared Operator IsFalse(x As Test) As Boolean"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "(c AndAlso c1)").WithArguments("Public Shared Operator IsTrue(x As Test) As Boolean"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "c1 += c").WithArguments("Public Shared Operator +(x As Test, y As Test) As Test"))
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeInMetadata()
            Dim peSource =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<Obsolete>
Public Class TestClass1
End Class

<Obsolete("TestClass2 is obsolete")>
Public Class TestClass2
End Class

<Obsolete("Do not use TestClass3", True)>
Public Class TestClass3
End Class

<Obsolete("TestClass4 is obsolete", False)>
Public Class TestClass4
End Class

<Obsolete(Nothing, True)>
Public Module TestModule
    Public Sub TestMethod()
    End Sub
End Module

Public Class TestClass
    <Obsolete("Do not use TestMethod")>
    Public Sub TestMethod()
    End Sub

    <Obsolete("Do not use Prop1", False)>
    Public Property Prop1 As Integer

    <Obsolete("Do not use field1", True)>
    Public field1 As TestClass

    <Obsolete("Do not use event", True)>
    Public Event event1 As Action
End Class
]]>
    </file>
</compilation>

            Dim peReference = MetadataReference.CreateFromImage(CreateCompilationWithMscorlib40AndVBRuntime(peSource).EmitToArray())

            Dim source =
<compilation>
    <file name="b.vb"><![CDATA[
Public Class Test
    Public Shared Sub goo1(c As TestClass1)
    End Sub
    Public Shared Sub goo2(c As TestClass2)
    End Sub
    Public Shared Sub goo3(c As TestClass3)
    End Sub
    Public Shared Sub goo4(c As TestClass4)
    End Sub

    Public Shared Sub Main()
        Dim c As TestClass = New TestClass()
        c.TestMethod()
        Dim i = c.Prop1
        c = c.field1
        AddHandler c.event1, Sub() Return
        TestModule.TestMethod()
    End Sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndReferences(source, {peReference}).VerifyDiagnostics(
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "TestClass1").WithArguments("TestClass1"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "TestClass2").WithArguments("TestClass2", "TestClass2 is obsolete"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "TestClass3").WithArguments("TestClass3", "Do not use TestClass3"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "TestClass4").WithArguments("TestClass4", "TestClass4 is obsolete"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "c.TestMethod()").WithArguments("Public Sub TestMethod()", "Do not use TestMethod"),
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "c.Prop1").WithArguments("Public Property Prop1 As Integer", "Do not use Prop1"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "c.field1").WithArguments("Public field1 As TestClass", "Do not use field1"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "c.event1").WithArguments("Public Event event1 As System.Action", "Do not use event"),
                        Diagnostic(ERRID.ERR_UseOfObsoleteSymbolNoMessage1, "TestModule").WithArguments("TestModule"))
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeCycles()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Test
    <Obsolete("F1 is obsolete")>
    <SomeAttr(F1)>
    Public Const F1 As Integer = 10

    <Obsolete("F2 is obsolete", True)>
    <SomeAttr(F3)>
    Public Const F2 As Integer = 10

    <Obsolete("F3 is obsolete")>
    <SomeAttr(F2)>
    Public Const F3 As Integer = 10

    <Obsolete(F4, True)>
    Public Const F4 As String = "blah"

    <Obsolete(F5)>
    Public F5 As String = "blah"

    <Obsolete(P1, True)>
    Public ReadOnly Property P1 As String
        Get
            Return "blah"
        End Get
    End Property

    <Obsolete>
    <SomeAttr(P2, True)>
    Public ReadOnly Property P2 As String
        Get
            Return "blah"
        End Get
    End Property

    <Obsolete(Method1)>
    Public Sub Method1()
    End Sub

    <Obsolete()>
    <SomeAttr1(Method2)>
    Public Sub Method2()
    End Sub

    <Obsolete(F6)>
    <SomeAttr(F6)>
    <SomeAttr(F7)>
    Public Const F6 As String = "F6 is obsolete"

    <Obsolete(F7, True)>
    <SomeAttr(F6)>
    <SomeAttr(F7)>
    Public Const F7 As String = "F7 is obsolete"
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class SomeAttr
    Inherits Attribute
    Public Sub New(x As Integer)
    End Sub
    Public Sub New(x As String)
    End Sub
End Class

Public Class SomeAttr1
    Inherits Attribute
    Public Sub New(x As Action)
    End Sub
End Class
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "F5"),
                    Diagnostic(ERRID.ERR_RequiredConstExpr, "F5"),
                    Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "P1"),
                    Diagnostic(ERRID.ERR_RequiredConstExpr, "P1"),
                    Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "P2"),
                    Diagnostic(ERRID.ERR_NoArgumentCountOverloadCandidates1, "SomeAttr").WithArguments("New"),
                    Diagnostic(ERRID.ERR_RequiredConstExpr, "P2"),
                    Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "Method1"),
                    Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "Method2"))
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeSuppress()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<Obsolete>
Public Class SomeType

    Public Shared Instance As SomeType
    Public Const Message As String = "goo"
End Class

<Obsolete>
Module Mod1
    Dim someField As SomeType = SomeType.Instance
    Public Property someProp As SomeType
    Sub goo(x As SomeType)
    End Sub
End Module

Public Class Test

    <Obsolete>
    Dim someField As SomeType = SomeType.Instance

    <Obsolete>
    Dim someFuncField As Func(Of SomeType) = Function() New SomeType()

    <Obsolete>
    Event someEvent As Action(Of SomeType)

    <Obsolete>
    Function goo(x As SomeType) As SomeType
        Dim y As SomeType = New SomeType()
        Return x
    End Function
End Class

<Obsolete>
Public Class Base(Of T)
End Class

<Obsolete>
Public Class Derived
    Inherits Base(Of Base(Of Integer))
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeSuppress_02()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<Obsolete>
Public Class SomeType
End Class

<Obsolete>
Public Delegate Sub MyDeleg()

Public Class Test
    <Obsolete>
    Public Shared Property someProp As SomeType

    <Obsolete>
    Default Public Property Item(x As SomeType) As SomeType
        Get
            Return Nothing
        End Get
        <Obsolete>
        Set(value As SomeType)
            Dim y As SomeType = New SomeType()
        End Set
    End Property

    <Obsolete>
    Custom Event goo As MyDeleg
        AddHandler(value As MyDeleg)

        End AddHandler

        RemoveHandler(value As MyDeleg)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    <Obsolete()>
    Public Property Prop1 As SomeType
        Get
            Return New SomeType()
        End Get
        Set(ByVal Value As SomeType)
            Dim p As New SomeType()
        End Set
    End Property

End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestObsoleteAndPropertyAccessors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Namespace Windows.Foundation.Metadata
    Public NotInheritable Class DeprecatedAttribute
        Inherits Attribute
        Public Sub New(message As String, type As DeprecationType, version As UInteger)
        End Sub
    End Class
    Public Enum DeprecationType
        Deprecate
        Remove
    End Enum
End Namespace
]]>
    </file>
    <file><![CDATA[
Imports Windows.Foundation.Metadata
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class A
End Class
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class B
End Class
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class C
End Class
Class D
    ReadOnly Property P As Object
        Get
            Return New A()
        End Get
    End Property
    <Deprecated(Nothing, DeprecationType.Deprecate, 0)>ReadOnly Property Q As Object
        Get
            Return New B()
        End Get
    End Property
    ReadOnly Property R As Object
        <Deprecated(Nothing, DeprecationType.Deprecate, 0)>Get
            Return New C()
        End Get
    End Property
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC40008: 'A' is obsolete.
            Return New A()
                       ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TestObsoleteAndEventAccessors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Namespace Windows.Foundation.Metadata
    Public NotInheritable Class DeprecatedAttribute
        Inherits Attribute
        Public Sub New(message As String, type As DeprecationType, version As UInteger)
        End Sub
    End Class
    Public Enum DeprecationType
        Deprecate
        Remove
    End Enum
End Namespace
]]>
    </file>
    <file><![CDATA[
Imports System
Imports Windows.Foundation.Metadata
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class A
End Class
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class B
End Class
<Deprecated(Nothing, DeprecationType.Deprecate, 0)>Class C
End Class
Class D
    Custom Event E As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
            M(New A())
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
    <Deprecated(Nothing, DeprecationType.Deprecate, 0)>Custom Event F As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        RemoveHandler(value As EventHandler)
            M(New B())
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
    Custom Event G As EventHandler
        AddHandler(value As EventHandler)
        End AddHandler
        <Deprecated(Nothing, DeprecationType.Deprecate, 0)>RemoveHandler(value As EventHandler)
            M(New C())
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
    Shared Sub M(o As Object)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC40008: 'A' is obsolete.
            M(New A())
                  ~
BC31142: 'Windows.Foundation.Metadata.DeprecatedAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
        <Deprecated(Nothing, DeprecationType.Deprecate, 0)>RemoveHandler(value As EventHandler)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeCycles_02()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Goo>
Class Goo
    Inherits Base
End Class

<Goo>
class Base
    Inherits System.Attribute

    Public Class Nested
        Inherits Goo
    End Class
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source)

            source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<Obsolete>
Public Class SomeType

    Public Shared Instance As SomeType
    Public Const Message As String = "goo"
End Class

Public Class SomeAttr
    Inherits Attribute

    Public Sub New(message As String)
    End Sub
End Class

<Obsolete(SomeType.Message)>
Public Class Derived
    Inherits Base
End Class

Public Class Base
    <Obsolete(SomeType.Message)>
    Public Property SomeProp As SomeType
End Class
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll.WithConcurrentBuild(False)).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeP2PReference()
            Dim s =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<Obsolete>
public Class C 
    <Obsolete>
    public sub Goo() 
    end sub
end class 
]]>
    </file>
</compilation>

            Dim other = CreateCompilationWithMscorlib40(s)

            s =
<compilation>
    <file name="b.vb"><![CDATA[
Public Class A
    Sub New(o As C)
        o.Goo()
    end sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndReferences(s, {New VisualBasicCompilationReference(other)}).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "C").WithArguments("C"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "o.Goo()").WithArguments("Public Sub Goo()"))

        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeOnMembers2()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    Property p As String
        <Obsolete("", False)>
        Get
            Return "hello"
        End Get
        <Obsolete>
        Set(value As String)
        End Set
    End Property

    <Obsolete>
    WithEvents p1 As New C2

    Sub handler() Handles p1.XEvent
    End Sub

    <Obsolete>
    Sub handler2() Handles p1.XEvent
    End Sub

    Custom Event goo As MyDeleg
        AddHandler(value As MyDeleg)

        End AddHandler

        RemoveHandler(value As MyDeleg)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class

Class C2
    <Obsolete>
    Public Event XEvent()
    Sub bar(s As String)
    End Sub

    Sub goo()
        Dim s As New C1
        s.p += "as"
        bar(s.p)
    End Sub
End Class

<Obsolete>
Public Delegate Sub MyDeleg()
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "XEvent").WithArguments("Public Event XEvent()"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "MyDeleg").WithArguments("MyDeleg"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "MyDeleg").WithArguments("MyDeleg"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "MyDeleg").WithArguments("MyDeleg"),
                    Diagnostic(ERRID.WRN_UseOfObsoletePropertyAccessor2, "s.p += ""as""").WithArguments("Set", "Public Property p As String"),
                    Diagnostic(ERRID.WRN_UseOfObsoletePropertyAccessor2, "s.p").WithArguments("Get", "Public Property p As String"),
                    Diagnostic(ERRID.WRN_UseOfObsoletePropertyAccessor2, "s.p").WithArguments("Get", "Public Property p As String"))
        End Sub

        <Fact>
        <WorkItem(546636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546636")>
        Public Sub TestObsoleteAttributeOnAttributes()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class Att
    Inherits Attribute

    <Obsolete("Constructor", True)>
    Public Sub New()
    End Sub

    <Obsolete("Property", True)>
    Public Property Prop As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property

    <Obsolete("Field", True)>
    Public Field As Integer
End Class

<Att>
<Att(Field:=1)>
<Att(Prop:=1)>
Public Class Test
    <Att()>
    Public Shared Sub Main()
    End Sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Att()").WithArguments("Public Sub New()", "Constructor"),
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Att").WithArguments("Public Sub New()", "Constructor"),
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Field:=1").WithArguments("Public Field As Integer", "Field"),
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Att(Field:=1)").WithArguments("Public Sub New()", "Constructor"),
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Prop:=1").WithArguments("Public Property Prop As Integer", "Property"),
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "Att(Prop:=1)").WithArguments("Public Sub New()", "Constructor"))
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeOnMembers3()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Namespace A.B
    <Obsolete>
    Public Class C
        <Obsolete>
        Public Shared Field1 As Integer = 10

        <Obsolete>
        Public Class D
            <Obsolete>
            Public Shared Field2 As Integer = 20
        End Class
    End Class

    <Obsolete>
    Public Class C1
        Public Class D
        End Class
    End Class

    <Obsolete>
    Public Class C2(Of T)
        <Obsolete>
        Public Shared Field1 As Integer = 10

        Public Class D
        End Class

        <Obsolete>
        Public Class E(Of U)
        End Class
    End Class
End Namespace

Class B(Of T)
End Class

Class D
    Inherits B(Of A.B.C1.D)
End Class
Class D1
    Inherits B(Of A.B.C2(Of Integer).D)
End Class
Class D2
    Inherits B(Of A.B.C2(Of Integer).E(Of Integer))
End Class

Class Program
    Shared Sub Main()
        Dim x = A.B.C.Field1
        Dim x1 = A.B.C.D.Field2
        Dim y = New A.B.C1.D()
        Dim y1 = New A.B.C2(Of Integer).D()
        Dim y2 = A.B.C2(Of Integer).Field1
        Dim y3 = New a.b.c2(Of Integer).E(Of Integer)
    End Sub
End Class

]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C1").WithArguments("A.B.C1"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer)").WithArguments("A.B.C2(Of Integer)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer)").WithArguments("A.B.C2(Of Integer)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer).E(Of Integer)").WithArguments("A.B.C2(Of Integer).E(Of Integer)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C").WithArguments("A.B.C"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C.Field1").WithArguments("Public Shared Field1 As Integer"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C").WithArguments("A.B.C"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C.D").WithArguments("A.B.C.D"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C.D.Field2").WithArguments("Public Shared Field2 As Integer"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C1").WithArguments("A.B.C1"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer)").WithArguments("A.B.C2(Of Integer)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer)").WithArguments("A.B.C2(Of T)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "A.B.C2(Of Integer).Field1").WithArguments("Public Shared Field1 As Integer"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "a.b.c2(Of Integer)").WithArguments("A.B.C2(Of Integer)"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "a.b.c2(Of Integer).E(Of Integer)").WithArguments("A.B.C2(Of Integer).E(Of Integer)"))
        End Sub

        Private ReadOnly ObsoleteAttributeSource As XElement = <file name="ObsoleteAttribute.vb"><![CDATA[
Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Sub New()
        End Sub
    
        Public Sub New(message As String)
        End Sub
    
        Public Sub New(message As String, isError As Boolean)
        End Sub
    
        Public Property DiagnosticId As String
        Public Property UrlFormat As String
    End Class
End Namespace
]]></file>

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_01()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(DiagnosticId:="TEST1")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]>
    </file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_02()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(UrlFormat:="https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/{0}")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]>
    </file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/BC40008", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_03()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(UrlFormat:="https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/{0}/{1}")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]>
    </file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_04()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(UrlFormat:="https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/elementname-is-obsolete-visual-basic-warning")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]>
    </file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/elementname-is-obsolete-visual-basic-warning", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadAttribute_01()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(DiagnosticId:="TEST1")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Sub New()
        End Sub
    
        Public Sub New(message As String)
        End Sub
    
        Public Sub New(message As String, isError As Boolean)
        End Sub
    
        Public Dim DiagnosticId As String
        Public Property DiagnosticId As String
    End Class
End Namespace
]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(
                Diagnostic(ERRID.ERR_MetadataMembersAmbiguous3, "DiagnosticId").WithArguments("DiagnosticId", "class", "System.ObsoleteAttribute").WithLocation(4, 15),
                Diagnostic(ERRID.ERR_MultiplyDefinedType3, "DiagnosticId").WithArguments("DiagnosticId", "Public DiagnosticId As String", "class").WithLocation(27, 25))
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_05()

            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete("don't use", false, DiagnosticId:="TEST1", UrlFormat:="https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/{0}")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]>
    </file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()", "don't use").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("TEST1", diag.Id)
            Assert.Equal(ERRID.WRN_UseOfObsoleteSymbol2, DirectCast(diag.Code, ERRID))
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/TEST1", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadAttribute_02()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete(DiagnosticId:="A", DiagnosticId:="B", UrlFormat:="C", UrlFormat:="D")>
    Sub M1()
    End Sub

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim diags = comp.GetDiagnostics()
            diags.Verify(Diagnostic("A", "M1()").WithArguments("Public Sub M1()").WithLocation(9, 9))

            Dim diag = diags.Single()
            Assert.Equal("C", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_Suppression_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    <Obsolete("don't use", false, DiagnosticId:="TEST1")>
    Sub M1()
    End Sub

    <Obsolete>
    Sub M2()
    End Sub

    Sub M3()
        M1()
        M2()

#Disable Warning TEST1
        M1()
        M2()
#Enable Warning TEST1

#Disable Warning BC40008
        M1()
        M2()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.VerifyDiagnostics(
                Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()", "don't use").WithLocation(13, 9),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M2()").WithArguments("Public Sub M2()").WithLocation(14, 9),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M2()").WithArguments("Public Sub M2()").WithLocation(18, 9),
                Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()", "don't use").WithLocation(22, 9))
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_FromMetadata_01()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete("don't use", false, DiagnosticId:="TEST1")>
    Public Sub M1()
    End Sub

    <Obsolete>
    Public Sub M2()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M3()
        M1()
        M2()

#Disable Warning TEST1
        M1()
        M2()
#Enable Warning TEST1

#Disable Warning BC40008
        M1()
        M2()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim expected = {
                Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()", "don't use").WithLocation(5, 9),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M2()").WithArguments("Public Sub M2()").WithLocation(6, 9),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M2()").WithArguments("Public Sub M2()").WithLocation(10, 9),
                Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()", "don't use").WithLocation(14, 9)
            }

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            comp2.VerifyDiagnostics(expected)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            comp2.VerifyDiagnostics(expected)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_FromMetadata_02()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete(DiagnosticId:="TEST1")>
    Public Sub M1()
    End Sub

    <Obsolete("don't use", DiagnosticId:="TEST2")>
    Public Sub M2()
    End Sub
    
    <Obsolete("don't use", false, DiagnosticId:="TEST3")>
    Public Sub M3()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M4()
        M1()
        M2()
        M3()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim expected = {
                Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9),
                Diagnostic("TEST2", "M2()").WithArguments("Public Sub M2()", "don't use").WithLocation(6, 9),
                Diagnostic("TEST3", "M3()").WithArguments("Public Sub M3()", "don't use").WithLocation(7, 9)
            }

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            comp2.VerifyDiagnostics(expected)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            comp2.VerifyDiagnostics(expected)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_FromMetadata_03()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete(DiagnosticId:="TEST1", UrlFormat:="https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/{0}")>
    Public Sub M1()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/TEST1", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/compiler-messages/TEST1", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_FromMetadata_04()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete(DiagnosticId:=Nothing, UrlFormat:=Nothing)>
    Public Sub M1()
    End Sub
End Class
]]></file>
    <%= ObsoleteAttributeSource %>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_01()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete(Flag:=False, DiagnosticId:="TEST1")>
    Public Sub M1()
    End Sub
End Class
]]></file>
    <file name="ObsoleteAttribute.vb"><![CDATA[
Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute

        Public Property Flag As Boolean
        Public Property DiagnosticId As String
    End Class
End Namespace
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_02()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    <Obsolete(DiagnosticId:="TEST1", UrlFormat:="TEST2")>
    Public Sub M1()
    End Sub
End Class
]]></file>
    <file name="ObsoleteAttribute.vb"><![CDATA[
Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute

        Public Dim DiagnosticId As String
        Public Dim UrlFormat As String
    End Class
End Namespace
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_03()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum E1
    A
    B
End Enum

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Property ByteProp As Byte
        Public Property SByteProp As SByte
        Public Property BooleanProp As Boolean
        Public Property ShortProp As Short
        Public Property UshortProp As UShort
        Public Property CharProp As Char
        Public Property IntProp As Integer
        Public Property UintProp As UInteger
        Public Property FloatProp As Single
        Public Property LongProp As Long
        Public Property UlongProp As ULong
        Public Property DoubleProp As Double
        Public Property EnumProp As E1
        Public Property DiagnosticId As String
    End Class
End Namespace

Public Class C1
    <Obsolete(
        ByteProp:=0,
        SByteProp:=0,
        BooleanProp:=false,
        ShortProp:=0,
        UShortProp:=0,
        CharProp:="\0",
        IntProp:=0,
        UIntProp:=0,
        FloatProp:=0,
        LongProp:=0,
        ULongProp:=0,
        DoubleProp:=0,
        EnumProp:=E1.A,
        DiagnosticId:="TEST1")>
    Public Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_04()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum E1
    A
    B
End Enum

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Property IntProp As Integer()
        Public Property EnumProp As E1()
        Public Property DiagnosticId As String
    End Class
End Namespace

Public Class C1
    <Obsolete(
        IntProp:={0, 1, 2},
        EnumProp:={E1.A, E1.B},
        DiagnosticId:="TEST1")>
    Public Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("TEST1", "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_05()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum E1
    A
    B
End Enum

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Property DiagnosticId As Char()
        Public Property UrlFormat As Char()
    End Class
End Namespace

Public Class C1
    <Obsolete(
        DiagnosticId:={"A"},
        UrlFormat:={"B"})>
    Public Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_06()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum E1
    A
    B
End Enum

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Property DiagnosticId As Char()
        Public Property UrlFormat As Char()
    End Class
End Namespace

Public Class C1
    <Obsolete(
        DiagnosticId:=Nothing,
        UrlFormat:=Nothing)>
    Public Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_07()

            ' In this program C1.M1 has an ObsoleteAttribute with multiple values provided for DiagnosticId and UrlFormat
            Dim ilSource = "
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.class public auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          M1() cil managed
  {
    .custom instance void System.ObsoleteAttribute::.ctor() = ( 01 00 04 00                                         // ....
                                                                54 0E 0C 44 69 61 67 6E 6F 73 74 69 63 49 64 01 41  // T..DiagnosticId.A
                                                                54 0E 0C 44 69 61 67 6E 6F 73 74 69 63 49 64 01 42  // T..DiagnosticId.B
                                                                54 0E 09 55 72 6C 46 6F 72 6D 61 74 01 43           // T..UrlFormat.C
                                                                54 0E 09 55 72 6C 46 6F 72 6D 61 74 01 44 )         // T..UrlFormat.D
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method C1::M1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C1::.ctor

} // end of class C1

.class public auto ansi beforefieldinit System.ObsoleteAttribute
       extends [mscorlib]System.Attribute
{
  .field private string '<DiagnosticId>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .field private string '<UrlFormat>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .method public hidebysig specialname instance string 
          get_DiagnosticId() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      string System.ObsoleteAttribute::'<DiagnosticId>k__BackingField'
    IL_0006:  ret
  } // end of method ObsoleteAttribute::get_DiagnosticId

  .method public hidebysig specialname instance void 
          set_DiagnosticId(string 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string System.ObsoleteAttribute::'<DiagnosticId>k__BackingField'
    IL_0007:  ret
  } // end of method ObsoleteAttribute::set_DiagnosticId

  .method public hidebysig specialname instance string 
          get_UrlFormat() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      string System.ObsoleteAttribute::'<UrlFormat>k__BackingField'
    IL_0006:  ret
  } // end of method ObsoleteAttribute::get_UrlFormat

  .method public hidebysig specialname instance void 
          set_UrlFormat(string 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string System.ObsoleteAttribute::'<UrlFormat>k__BackingField'
    IL_0007:  ret
  } // end of method ObsoleteAttribute::set_UrlFormat

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ObsoleteAttribute::.ctor

  .property instance string DiagnosticId()
  {
    .get instance string System.ObsoleteAttribute::get_DiagnosticId()
    .set instance void System.ObsoleteAttribute::set_DiagnosticId(string)
  } // end of property ObsoleteAttribute::DiagnosticId
  .property instance string UrlFormat()
  {
    .get instance string System.ObsoleteAttribute::get_UrlFormat()
    .set instance void System.ObsoleteAttribute::set_UrlFormat(string)
  } // end of property ObsoleteAttribute::UrlFormat
} // end of class System.ObsoleteAttribute
"

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim ilComp = CompileIL(ilSource)
            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={ilComp})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("A", "M1()").WithArguments("Public Overloads Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("C", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_08()

            ' In this program C1.M1 has an ObsoleteAttribute with multiple values provided for DiagnosticId and UrlFormat
            Dim ilSource = "
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.class public auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          M1() cil managed
  {
    .custom instance void System.ObsoleteAttribute::.ctor() = ( 01 00 02 00                                         // ....
                                                                54 0E 0C 44 69 61 67 6E 6F 73 74 69 63 49 64 01 41  // T..DiagnosticId.A
                                                                0E 09 55 72 6C 46 6F 72 6D 61 74 01 42 )            // ..UrlFormat.B
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method C1::M1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C1::.ctor

} // end of class C1

.class public auto ansi beforefieldinit System.ObsoleteAttribute
       extends [mscorlib]System.Attribute
{
  .field private string '<DiagnosticId>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .field private string '<UrlFormat>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .method public hidebysig specialname instance string 
          get_DiagnosticId() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      string System.ObsoleteAttribute::'<DiagnosticId>k__BackingField'
    IL_0006:  ret
  } // end of method ObsoleteAttribute::get_DiagnosticId

  .method public hidebysig specialname instance void 
          set_DiagnosticId(string 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string System.ObsoleteAttribute::'<DiagnosticId>k__BackingField'
    IL_0007:  ret
  } // end of method ObsoleteAttribute::set_DiagnosticId

  .method public hidebysig specialname instance string 
          get_UrlFormat() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      string System.ObsoleteAttribute::'<UrlFormat>k__BackingField'
    IL_0006:  ret
  } // end of method ObsoleteAttribute::get_UrlFormat

  .method public hidebysig specialname instance void 
          set_UrlFormat(string 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string System.ObsoleteAttribute::'<UrlFormat>k__BackingField'
    IL_0007:  ret
  } // end of method ObsoleteAttribute::set_UrlFormat

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ObsoleteAttribute::.ctor

  .property instance string DiagnosticId()
  {
    .get instance string System.ObsoleteAttribute::get_DiagnosticId()
    .set instance void System.ObsoleteAttribute::set_DiagnosticId(string)
  } // end of property ObsoleteAttribute::DiagnosticId
  .property instance string UrlFormat()
  {
    .get instance string System.ObsoleteAttribute::get_UrlFormat()
    .set instance void System.ObsoleteAttribute::set_UrlFormat(string)
  } // end of property ObsoleteAttribute::UrlFormat
} // end of class System.ObsoleteAttribute
"

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim ilComp = CompileIL(ilSource)
            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={ilComp})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic("A", "M1()").WithArguments("Public Overloads Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <Fact, WorkItem(42119, "https://github.com/dotnet/roslyn/issues/42119")>
        Public Sub Obsolete_CustomDiagnosticId_BadMetadata_09()
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Enum E1
    A
    B
End Enum

Namespace System
    Public Class ObsoleteAttribute
        Inherits Attribute
    
        Public Property DiagnosticId As Object
        Public Property UrlFormat As Object
    End Class
End Namespace

Public Class C1
    <Obsolete(
        DiagnosticId:="A",
        UrlFormat:="B")>
    Public Sub M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Class C2
    Inherits C1

    Sub M2()
        M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(source1)
            comp1.VerifyDiagnostics()

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.ToMetadataReference()})
            Dim diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            Dim diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)

            comp2 = CreateCompilationWithMscorlib40(source2, references:={comp1.EmitToImageReference()})
            diags = comp2.GetDiagnostics()
            diags.Verify(Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "M1()").WithArguments("Public Sub M1()").WithLocation(5, 9))

            diag = diags.Single()
            Assert.Equal("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC40008)", diag.Descriptor.HelpLinkUri)
        End Sub

        <WorkItem(578023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578023")>
        <Fact>
        Public Sub TestObsoleteInAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports X = C
Imports Y = A(Of C)
Imports Z = A(Of C()).B

Imports C
Imports A(Of C)
Imports A(Of C()).B

Class A(Of T)
    Friend Class B
    End Class
End Class
<System.Obsolete>
Class C
End Class
Module M
    Private F As X
    Private G As Y
    Private H As Z
End Module
]]>
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC40008: 'C' is obsolete.
Imports X = C
            ~
BC40008: 'C' is obsolete.
Imports Y = A(Of C)
                 ~
BC40008: 'C' is obsolete.
Imports Z = A(Of C()).B
                 ~
BC40008: 'C' is obsolete.
Imports C
        ~
BC40008: 'C' is obsolete.
Imports A(Of C)
             ~
BC40008: 'C' is obsolete.
Imports A(Of C()).B
             ~
     </errors>)
        End Sub

        <WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")>
        <Fact>
        Public Sub TestObsoleteOnVirtualMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class A
    <Obsolete("A")>
    Public Overridable Sub M()
    End Sub
End Class

Public Class B
    Inherits A

    <Obsolete("B")>
    Public Overrides Sub M()
    End Sub

    Private Sub Base()
        MyBase.M()
    End Sub
End Class

Public Class C
    Inherits B

    <Obsolete("C")>
    Public Overrides Sub M()
    End Sub

    Private Sub Test(pa As A, pb As B, pc As C)
        pa.M()
        pb.M()
        pc.M()
    End Sub

    Private Sub Base()
        MyBase.M()
    End Sub
End Class
]]>
    </file>
</compilation>)

            ' Unlike in C#, VB does not walk up to the least-overridden method.
            compilation.AssertTheseDiagnostics(<errors>
BC40000: 'Public Overridable Sub M()' is obsolete: 'A'.
        MyBase.M()
        ~~~~~~~~~~
BC40000: 'Public Overridable Sub M()' is obsolete: 'A'.
        pa.M()
        ~~~~~~
BC40000: 'Public Overrides Sub M()' is obsolete: 'B'.
        pb.M()
        ~~~~~~
BC40000: 'Public Overrides Sub M()' is obsolete: 'C'.
        pc.M()
        ~~~~~~
BC40000: 'Public Overrides Sub M()' is obsolete: 'B'.
        MyBase.M()
        ~~~~~~~~~~
     </errors>)
        End Sub

        <Fact()>
        Public Sub TestDeprecatedAttribute()
            Dim source1 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports Windows.Foundation.Metadata

<Deprecated("Class1 is deprecated.", DeprecationType.Deprecate, 0)>
public class Class1
End Class

<Deprecated("Class2 is deprecated.", DeprecationType.Deprecate, 0, Platform.Windows)>
public class Class2
End Class

<Deprecated("Class3 is deprecated.", DeprecationType.Remove, 1)>
public class Class3
End Class

<Deprecated("Class4 is deprecated.", DeprecationType.Remove, 0, Platform.WindowsPhone)>
public class Class4
End Class
]]>
    </file>
</compilation>

            Dim compilation1 = CreateEmptyCompilationWithReferences(source1, WinRtRefs)
            compilation1.VerifyDiagnostics()

            Dim source2 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports Windows.Foundation.Metadata

class Class5
    Sub Test()
        Dim x1 As Class1 = Nothing
        Dim x2 As Class2 = Nothing
        Dim x3 As Class3 = Nothing
        Dim x4 As Class4 = Nothing

        Dim x5 As Object
        x5=x1
        x5 = x2
        x5 = x3
        x5 = x4
    End Sub
End Class

class Class6
    Readonly Property P1 As Integer
        <Deprecated("P1.get is deprecated.", DeprecationType.Remove, 1)>
        get
            return 1
        End Get
    End Property

    Custom Event E1 As System.Action
        <Deprecated("E1.add is deprecated.", DeprecationType.Remove, 1)>
        AddHandler(value As System.Action)
        End AddHandler
        RemoveHandler(value As System.Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
]]>
    </file>
</compilation>

            Dim compilation2 = CreateEmptyCompilationWithReferences(source2, WinRtRefs.Concat(New VisualBasicCompilationReference(compilation1)))

            Dim expected = <![CDATA[
BC40000: 'Class1' is obsolete: 'Class1 is deprecated.'.
        Dim x1 As Class1 = Nothing
                  ~~~~~~
BC40000: 'Class2' is obsolete: 'Class2 is deprecated.'.
        Dim x2 As Class2 = Nothing
                  ~~~~~~
BC30668: 'Class3' is obsolete: 'Class3 is deprecated.'.
        Dim x3 As Class3 = Nothing
                  ~~~~~~
BC30668: 'Class4' is obsolete: 'Class4 is deprecated.'.
        Dim x4 As Class4 = Nothing
                  ~~~~~~
BC31142: 'Windows.Foundation.Metadata.DeprecatedAttribute' cannot be applied to the 'AddHandler', 'RemoveHandler', or 'RaiseEvent' definitions. If required, apply the attribute directly to the event.
        <Deprecated("E1.add is deprecated.", DeprecationType.Remove, 1)>
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
            compilation2.AssertTheseDiagnostics(expected)

            compilation2 = CreateEmptyCompilationWithReferences(source2, WinRtRefs.Concat(compilation1.EmitToImageReference()))

            compilation2.AssertTheseDiagnostics(expected)
        End Sub

        <Fact()>
        Public Sub TestDeprecatedAttribute001()
            Dim source1 =
            <![CDATA[
using System;
using Windows.Foundation.Metadata;

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }

        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version, Type contract)
        {
        }
    }

    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}

public class Test
{
        [Deprecated("hello", DeprecationType.Deprecate, 1, typeof(int))]
        public static void Goo()
        {

        }

        [Deprecated("hi", DeprecationType.Deprecate, 1)]
        public static void Bar()
        {

        }
}
]]>

            Dim compilation1 = CreateCSharpCompilation("Dll1", source1.Value)

            Dim ref = compilation1.EmitToImageReference()

            Dim source2 =
<compilation>
    <file name="test.vb"><![CDATA[
    Module Program
        Sub Main()
            Test.Goo()
            Test.Bar()
        end Sub
    end module
]]>
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source2, {ref})

            Dim expected = <![CDATA[
BC40000: 'Public Shared Overloads Sub Goo()' is obsolete: 'hello'.
            Test.Goo()
            ~~~~~~~~~~
BC40000: 'Public Shared Overloads Sub Bar()' is obsolete: 'hi'.
            Test.Bar()
            ~~~~~~~~~~
]]>
            compilation2.AssertTheseDiagnostics(expected)

            Dim source3 =
<compilation>
    <file name="test.vb"><![CDATA[
    Imports Windows.Foundation.Metadata

    Module Program
        <Deprecated("hello", DeprecationType.Deprecate, 1, gettype(integer))>
        sub Goo()
        end sub

        <Deprecated("hi", DeprecationType.Deprecate, 1)>
        Sub Bar()
        End sub

        Sub Main()
            Goo()
            Bar()
        end Sub
    end module
]]>
    </file>
</compilation>

            Dim compilation3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source3, {ref})

            Dim expected2 = <![CDATA[
BC40000: 'Public Sub Goo()' is obsolete: 'hello'.
            Goo()
            ~~~~~
BC40000: 'Public Sub Bar()' is obsolete: 'hi'.
            Bar()
            ~~~~~
]]>
            compilation3.AssertTheseDiagnostics(expected2)
        End Sub

        <Fact>
        <WorkItem(22447, "https://github.com/dotnet/roslyn/issues/22447")>
        Public Sub TestRefLikeType()
            Dim csSource = <![CDATA[
public ref struct S { }
]]>

            Dim csCompilation = CreateCSharpCompilation("Dll1", csSource.Value, parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp7_2))
            Dim ref = csCompilation.EmitToImageReference()

            Dim vbSource =
<compilation>
    <file name="test.vb"><![CDATA[
Module Program
    Sub M(s As S)
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim vbCompilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(vbSource, {ref})

            vbCompilation.AssertTheseDiagnostics((<![CDATA[
BC30668: 'S' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
    Sub M(s As S)
               ~
]]>))
            vbCompilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UseOfObsoleteSymbol2, "S").WithArguments("S", "Types with embedded references are not supported in this version of your compiler.").WithLocation(2, 16)
                )
        End Sub

        <Fact(), WorkItem(858839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858839")>
        Public Sub Bug858839_1()
            Dim source1 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports Windows.Foundation.Metadata

Public Class MainPage
    Public Shared Sub Main(args As String())
    
    End Sub
    
    Private Shared Sub TestGoo1(a As IGoo1, b As ConcreteGoo1)
        a.Goo() ' IGoo1
        b.Goo() ' ConcreteGoo1
    End Sub

    Private Shared Sub TestGoo2(a As IGoo2, b As ConcreteGoo2)
        a.Goo() ' IGoo2
        b.Goo() ' ConcreteGoo2
    End Sub

    Private Shared Sub TestGoo3(a As IGoo3, b As ConcreteGoo3)
        a.Goo() ' IGoo3
        b.Goo() ' ConcreteGoo3
    End Sub
End Class

Public Interface IGoo1
    <Deprecated("IGoo1.Goo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Sub Goo()
End Interface

Public NotInheritable Class ConcreteGoo1
    Implements IGoo1
    Public Sub Goo() Implements IGoo1.Goo
    
    End Sub
End Class

Public Interface IGoo2
    Sub Goo()
End Interface

Public NotInheritable Class ConcreteGoo2
    Implements IGoo2
    
    <Deprecated("ConcreteGoo2.Goo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Public Sub Goo() Implements IGoo2.Goo
    
    End Sub
End Class

Public Interface IGoo3
    <Deprecated("IGoo3.Goo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Sub Goo()
End Interface

Public NotInheritable Class ConcreteGoo3
    Implements IGoo3

    <Deprecated("ConcreteGoo3.Goo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Public Sub Goo() Implements IGoo3.Goo
    
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim compilation1 = CreateEmptyCompilationWithReferences(source1, WinRtRefs)

            Dim expected = <![CDATA[
BC40000: 'Sub Goo()' is obsolete: 'IGoo1.Goo has been deprecated'.
        a.Goo() ' IGoo1
        ~~~~~~~
BC40000: 'Public Sub Goo()' is obsolete: 'ConcreteGoo2.Goo has been deprecated'.
        b.Goo() ' ConcreteGoo2
        ~~~~~~~
BC40000: 'Sub Goo()' is obsolete: 'IGoo3.Goo has been deprecated'.
        a.Goo() ' IGoo3
        ~~~~~~~
BC40000: 'Public Sub Goo()' is obsolete: 'ConcreteGoo3.Goo has been deprecated'.
        b.Goo() ' ConcreteGoo3
        ~~~~~~~
BC40000: 'Sub Goo()' is obsolete: 'IGoo1.Goo has been deprecated'.
    Public Sub Goo() Implements IGoo1.Goo
                     ~~~~~~~~~~~~~~~~~~~~
]]>
            compilation1.AssertTheseDiagnostics(expected)
        End Sub

        <Fact(), WorkItem(858839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858839")>
        Public Sub Bug858839_2()
            Dim source0 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports Windows.Foundation.Metadata

public Class IExceptionalInterface
    Property ExceptionalProp As String
        <Deprecated("Actually, don't even use the prop at all.", DeprecationType.Remove, 50331648UI)>
        Get
            Return String.Empty
        End Get
        <Deprecated("Changed my mind; don't put this prop.", DeprecationType.Remove, 33554432UI)>
        Set(value As String)
        End Set
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation0 = CreateEmptyCompilationWithReferences(source0, WinRtRefs, TestOptions.ReleaseDll)

            compilation0.VerifyDiagnostics()

            Dim source1 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports System

Class Test
    Public Sub F(i As IExceptionalInterface)
        i.ExceptionalProp = "goo"
        Console.WriteLine(i.ExceptionalProp)
    End Sub
End Class]]>
    </file>
</compilation>

            Dim compilation1 = CreateEmptyCompilationWithReferences(source1, WinRtRefs.Append(New VisualBasicCompilationReference(compilation0)))

            Dim expected = <![CDATA[
BC30911: 'Set' accessor of 'Public Property ExceptionalProp As String' is obsolete: 'Changed my mind; don't put this prop.'.
        i.ExceptionalProp = "goo"
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30911: 'Get' accessor of 'Public Property ExceptionalProp As String' is obsolete: 'Actually, don't even use the prop at all.'.
        Console.WriteLine(i.ExceptionalProp)
                          ~~~~~~~~~~~~~~~~~
]]>
            compilation1.AssertTheseDiagnostics(expected)

            Dim compilation2 = CreateEmptyCompilationWithReferences(source1, WinRtRefs.Append(compilation0.EmitToImageReference()))

            compilation2.AssertTheseDiagnostics(expected)
        End Sub

    End Class
End Namespace
