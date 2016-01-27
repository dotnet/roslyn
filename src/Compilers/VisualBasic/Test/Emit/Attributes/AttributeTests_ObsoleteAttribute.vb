' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Xunit

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

<FooAttribute.BarAttribute.Baz>
<Obsolete("Blah")>
Class FooAttribute
    Inherits Attribute
    Class BazAttribute
        Inherits Attribute
    End Class
    Class BarAttribute
        Inherits FooAttribute
    End Class
End Class

Interface IFoo(Of T)
End Interface

<Obsolete>
Class SelfReferenceInBase
    Implements IFoo(Of SelfReferenceInBase)
End Class

Class SelfReferenceInBase1
    Implements IFoo(Of SelfReferenceInBase)
End Class

]]>
    </file>
</compilation>

            CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics(
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
            CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}).AssertTheseDiagnostics(
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

            Dim peReference = MetadataReference.CreateFromImage(CreateCompilationWithMscorlibAndVBRuntime(peSource).EmitToArray())

            Dim source =
<compilation>
    <file name="b.vb"><![CDATA[
Public Class Test
    Public Shared Sub foo1(c As TestClass1)
    End Sub
    Public Shared Sub foo2(c As TestClass2)
    End Sub
    Public Shared Sub foo3(c As TestClass3)
    End Sub
    Public Shared Sub foo4(c As TestClass4)
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

            CreateCompilationWithMscorlibAndReferences(source, {peReference}).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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
    Public Const Message As String = "foo"
End Class

<Obsolete>
Module Mod1
    Dim someField As SomeType = SomeType.Instance
    Public Property someProp As SomeType
    Sub foo(x As SomeType)
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
    Function foo(x As SomeType) As SomeType
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

            CreateCompilationWithMscorlibAndVBRuntime(source).VerifyDiagnostics()
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
    Custom Event foo As MyDeleg
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestObsoleteAttributeCycles_02()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
<Foo>
Class Foo
    Inherits Base
End Class

<Foo>
class Base
    Inherits System.Attribute

    Public Class Nested
        Inherits Foo
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
    Public Const Message As String = "foo"
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
            CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll.WithConcurrentBuild(False)).VerifyDiagnostics()
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
    public sub Foo() 
    end sub
end class 
]]>
    </file>
</compilation>

            Dim other = CreateCompilationWithMscorlib(s)

            s =
<compilation>
    <file name="b.vb"><![CDATA[
Public Class A
    Sub New(o As C)
        o.Foo()
    end sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlibAndReferences(s, {New VisualBasicCompilationReference(other)}).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "C").WithArguments("C"),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbolNoMessage1, "o.Foo()").WithArguments("Public Sub Foo()"))

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

    Custom Event foo As MyDeleg
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

    Sub foo()
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
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

        <WorkItem(578023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578023")>
        <Fact>
        Public Sub TestObsoleteInAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
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

            Dim compilation1 = CreateCompilationWithReferences(source1, WinRtRefs)
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

            Dim compilation2 = CreateCompilationWithReferences(source2, WinRtRefs.Concat(New VisualBasicCompilationReference(compilation1)))

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

            compilation2 = CreateCompilationWithReferences(source2, WinRtRefs.Concat(compilation1.EmitToImageReference()))

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
        public static void Foo()
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
            Test.Foo()
            Test.Bar()
        end Sub
    end module
]]>
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {ref})

            Dim expected = <![CDATA[
BC40000: 'Public Shared Overloads Sub Foo()' is obsolete: 'hello'.
            Test.Foo()
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
        sub Foo()
        end sub

        <Deprecated("hi", DeprecationType.Deprecate, 1)>
        Sub Bar()
        End sub

        Sub Main()
            Foo()
            Bar()
        end Sub
    end module
]]>
    </file>
</compilation>

            Dim compilation3 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source3, {ref})

            Dim expected2 = <![CDATA[
BC40000: 'Public Sub Foo()' is obsolete: 'hello'.
            Foo()
            ~~~~~
BC40000: 'Public Sub Bar()' is obsolete: 'hi'.
            Bar()
            ~~~~~
]]>
            compilation3.AssertTheseDiagnostics(expected2)
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
    
    Private Shared Sub TestFoo1(a As IFoo1, b As ConcreteFoo1)
        a.Foo() ' IFoo1
        b.Foo() ' ConcreteFoo1
    End Sub

    Private Shared Sub TestFoo2(a As IFoo2, b As ConcreteFoo2)
        a.Foo() ' IFoo2
        b.Foo() ' ConcreteFoo2
    End Sub

    Private Shared Sub TestFoo3(a As IFoo3, b As ConcreteFoo3)
        a.Foo() ' IFoo3
        b.Foo() ' ConcreteFoo3
    End Sub
End Class

Public Interface IFoo1
    <Deprecated("IFoo1.Foo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Sub Foo()
End Interface

Public NotInheritable Class ConcreteFoo1
    Implements IFoo1
    Public Sub Foo() Implements IFoo1.Foo
    
    End Sub
End Class

Public Interface IFoo2
    Sub Foo()
End Interface

Public NotInheritable Class ConcreteFoo2
    Implements IFoo2
    
    <Deprecated("ConcreteFoo2.Foo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Public Sub Foo() Implements IFoo2.Foo
    
    End Sub
End Class

Public Interface IFoo3
    <Deprecated("IFoo3.Foo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Sub Foo()
End Interface

Public NotInheritable Class ConcreteFoo3
    Implements IFoo3

    <Deprecated("ConcreteFoo3.Foo has been deprecated", DeprecationType.Deprecate, 0, Platform.Windows)>
    Public Sub Foo() Implements IFoo3.Foo
    
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithReferences(source1, WinRtRefs)

            Dim expected = <![CDATA[
BC40000: 'Sub Foo()' is obsolete: 'IFoo1.Foo has been deprecated'.
        a.Foo() ' IFoo1
        ~~~~~~~
BC40000: 'Public Sub Foo()' is obsolete: 'ConcreteFoo2.Foo has been deprecated'.
        b.Foo() ' ConcreteFoo2
        ~~~~~~~
BC40000: 'Sub Foo()' is obsolete: 'IFoo3.Foo has been deprecated'.
        a.Foo() ' IFoo3
        ~~~~~~~
BC40000: 'Public Sub Foo()' is obsolete: 'ConcreteFoo3.Foo has been deprecated'.
        b.Foo() ' ConcreteFoo3
        ~~~~~~~
BC40000: 'Sub Foo()' is obsolete: 'IFoo1.Foo has been deprecated'.
    Public Sub Foo() Implements IFoo1.Foo
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

            Dim compilation0 = CreateCompilationWithReferences(source0, WinRtRefs, TestOptions.ReleaseDll)

            compilation0.VerifyDiagnostics()

            Dim source1 =
<compilation>
    <file name="test.vb"><![CDATA[
Imports System

Class Test
    Public Sub F(i As IExceptionalInterface)
        i.ExceptionalProp = "foo"
        Console.WriteLine(i.ExceptionalProp)
    End Sub
End Class]]>
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithReferences(source1, WinRtRefs.Append(New VisualBasicCompilationReference(compilation0)))

            Dim expected = <![CDATA[
BC30911: 'Set' accessor of 'Public Property ExceptionalProp As String' is obsolete: 'Changed my mind; don't put this prop.'.
        i.ExceptionalProp = "foo"
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30911: 'Get' accessor of 'Public Property ExceptionalProp As String' is obsolete: 'Actually, don't even use the prop at all.'.
        Console.WriteLine(i.ExceptionalProp)
                          ~~~~~~~~~~~~~~~~~
]]>
            compilation1.AssertTheseDiagnostics(expected)

            Dim compilation2 = CreateCompilationWithReferences(source1, WinRtRefs.Append(compilation0.EmitToImageReference()))

            compilation2.AssertTheseDiagnostics(expected)
        End Sub

    End Class
End Namespace
