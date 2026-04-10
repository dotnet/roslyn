' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class OverridesTests
        Inherits BasicTestBase

        ' Test that basic overriding of properties and methods works.
        ' Test OverriddenMethod/OverriddenProperty API.
        <Fact>
        Public Sub SimpleOverrides()
            Dim code =
<compilation name="SimpleOverrides">
    <file name="a.vb">
Imports System        
Class Base
    Public Overridable Sub O1(a As String)
        Console.WriteLine("Base.O1")
    End Sub

    Public Sub N1(a As String)
        Console.WriteLine("Base.N1")
    End Sub

    Public Overridable Property O2 As String
        Get
            Console.WriteLine("Base.O2.Get")
            Return "Base.O2"
        End Get
        Set(value As String)
            Console.WriteLine("Base.O2.Set")
        End Set
    End Property

    Public Overridable Property N2 As String
        Get
            Console.WriteLine("Base.N2.Get")
            Return "Base.N2"
        End Get
        Set(value As String)
            Console.WriteLine("Base.N2.Set")
        End Set
    End Property
End Class

Class Derived
    Inherits Base

    Public Overrides Sub O1(a As String)
        Console.WriteLine("Derived.O1")
    End Sub

    Public Shadows Sub N1(a As String)
        Console.WriteLine("Derived.N1")
    End Sub

    Public Overrides Property O2 As String
        Get
            Console.WriteLine("Derived.O2.Get")
            Return "Derived.O2"
        End Get
        Set(value As String)
            Console.WriteLine("Derived.O2.Set")
        End Set
    End Property

    Public Shadows Property N2 As String
        Get
            Console.WriteLine("Derived.N2.Get")
            Return "Derived.N2"
        End Get
        Set(value As String)
            Console.WriteLine("Derived.N2.Set")
        End Set
    End Property
End Class

Module Module1
    Sub Main()
        Dim s As String

        Dim b As Base = New Derived()
        b.O1("hi")
        b.O2 = "x"
        s = b.O2
        b.N1("hi")
        b.N2 = "x"
        s = b.N2

        Console.WriteLine("---")

        b = New Base()
        b.O1("hi")
        b.O2 = "x"
        s = b.O2
        b.N1("hi")
        b.N2 = "x"
        s = b.N2

        Console.WriteLine("---")

        Dim d As Derived = New Derived()
        d.O1("hi")
        d.O2 = "x"
        s = d.O2
        d.N1("hi")
        d.N2 = "x"
        s = d.N2
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(code)

            Dim globalNS = comp.GlobalNamespace
            Dim clsBase = DirectCast(globalNS.GetMembers("Base").Single(), NamedTypeSymbol)
            Dim clsDerived = DirectCast(globalNS.GetMembers("Derived").Single(), NamedTypeSymbol)

            Dim o1Base = DirectCast(clsBase.GetMembers("O1").Single(), MethodSymbol)
            Dim o1Derived = DirectCast(clsDerived.GetMembers("O1").Single(), MethodSymbol)
            Assert.Null(o1Base.OverriddenMethod)
            Assert.Same(o1Base, o1Derived.OverriddenMethod)

            Dim o2Base = DirectCast(clsBase.GetMembers("O2").Single(), PropertySymbol)
            Dim o2Derived = DirectCast(clsDerived.GetMembers("O2").Single(), PropertySymbol)
            Assert.Null(o2Base.OverriddenProperty)
            Assert.Same(o2Base, o2Derived.OverriddenProperty)

            Dim get_o2Base = DirectCast(clsBase.GetMembers("get_O2").Single(), MethodSymbol)
            Dim get_o2Derived = DirectCast(clsDerived.GetMembers("get_O2").Single(), MethodSymbol)
            Assert.Null(get_o2Base.OverriddenMethod)
            Assert.Same(get_o2Base, get_o2Derived.OverriddenMethod)

            Dim set_o2Base = DirectCast(clsBase.GetMembers("set_O2").Single(), MethodSymbol)
            Dim set_o2Derived = DirectCast(clsDerived.GetMembers("set_O2").Single(), MethodSymbol)
            Assert.Null(set_o2Base.OverriddenMethod)
            Assert.Same(set_o2Base, set_o2Derived.OverriddenMethod)

            Dim n1Base = DirectCast(clsBase.GetMembers("N1").Single(), MethodSymbol)
            Dim n1Derived = DirectCast(clsDerived.GetMembers("N1").Single(), MethodSymbol)
            Assert.Null(n1Base.OverriddenMethod)
            Assert.Null(n1Derived.OverriddenMethod)

            Dim n2Base = DirectCast(clsBase.GetMembers("N2").Single(), PropertySymbol)
            Dim n2Derived = DirectCast(clsDerived.GetMembers("N2").Single(), PropertySymbol)
            Assert.Null(n2Base.OverriddenProperty)
            Assert.Null(n2Derived.OverriddenProperty)

            Dim get_n2Base = DirectCast(clsBase.GetMembers("get_N2").Single(), MethodSymbol)
            Dim get_n2Derived = DirectCast(clsDerived.GetMembers("get_N2").Single(), MethodSymbol)
            Assert.Null(get_n2Base.OverriddenMethod)
            Assert.Null(get_n2Derived.OverriddenMethod)

            Dim set_n2Base = DirectCast(clsBase.GetMembers("set_N2").Single(), MethodSymbol)
            Dim set_n2Derived = DirectCast(clsDerived.GetMembers("set_N2").Single(), MethodSymbol)
            Assert.Null(set_n2Base.OverriddenMethod)
            Assert.Null(set_n2Derived.OverriddenMethod)

            CompileAndVerify(code, expectedOutput:=<![CDATA[
Derived.O1
Derived.O2.Set
Derived.O2.Get
Base.N1
Base.N2.Set
Base.N2.Get
---
Base.O1
Base.O2.Set
Base.O2.Get
Base.N1
Base.N2.Set
Base.N2.Get
---
Derived.O1
Derived.O2.Set
Derived.O2.Get
Derived.N1
Derived.N2.Set
Derived.N2.Get]]>)

        End Sub

        <Fact>
        Public Sub UnimplementedMustOverride()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UnimplementedMustOverride">
    <file name="a.vb">
Option Strict On

Namespace X
    Public MustInherit Class A
        Public MustOverride Sub goo(x As Integer)
        Public MustOverride Sub bar()
        Public MustOverride Sub quux()
        Protected MustOverride Function zing() As String
        Public MustOverride Property bing As Integer
        Public MustOverride ReadOnly Property bang As Integer
    End Class
    
    Public MustInherit Class B
        Inherits A
    
        Public Overrides Sub bar()
        End Sub
    
        Protected MustOverride Function baz() As String
    
        Protected Overrides Function zing() As String
            Return ""
        End Function
    End Class
    
    Partial MustInherit Class C
        Inherits B
    
        Public Overrides Property bing As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
    
            End Set
        End Property
    
        Protected MustOverride Overrides Function zing() As String
    End Class
    
    Class D
        Inherits C
    
        Public Overrides Sub quux()
    
        End Sub
    End Class
    End Namespace
</file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30610: Class 'D' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    C: Protected MustOverride Overrides Function zing() As String
    B: Protected MustOverride Function baz() As String
    A: Public MustOverride Sub goo(x As Integer)
    A: Public MustOverride ReadOnly Property bang As Integer.
    Class D
          ~
</expected>)
        End Sub

        <Fact>
        Public Sub HidingMembersInClass()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="HidingMembersInClass">
    <file name="a.vb">
Option Strict On

Namespace N
    Class A
        Public Sub goo()
        End Sub
        Public Sub goo(x As Integer)
        End Sub
        Public Sub bar()
        End Sub
        Public Sub bar(x As Integer)
        End Sub
        Private Sub bing()
        End Sub
        Public Const baz As Integer = 5
        Public Const baz2 As Integer = 5
    End Class

    Class B
        Inherits A
        Public Shadows Sub goo(x As String)
        End Sub
    End Class

    Class C
        Inherits B
        Public goo As String
        Public bing As Integer
        Public Shadows baz As Integer
        Public baz2 As Integer

        Public Enum bar
            Red
        End Enum
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40004: variable 'goo' conflicts with sub 'goo' in the base class 'B' and should be declared 'Shadows'.
        Public goo As String
               ~~~
BC40004: variable 'baz2' conflicts with variable 'baz2' in the base class 'A' and should be declared 'Shadows'.
        Public baz2 As Integer
               ~~~~
BC40004: enum 'bar' conflicts with sub 'bar' in the base class 'A' and should be declared 'Shadows'.
        Public Enum bar
                    ~~~
</expected>)
        End Sub

        <WorkItem(540791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540791")>
        <Fact>
        Public Sub HidingMembersInClass_01()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="HidingMembersInClass">
    <file name="a.vb">
Class C1
    Inherits C2

    ' no warnings here
    Class C(Of T)

    End Class

    ' warning
    Sub goo(Of T)()

    End Sub
End Class
Class C2

    Class C

    End Class

    Sub Goo()

    End Sub
End Class


    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40003: sub 'goo' shadows an overloadable member declared in the base class 'C2'.  If you want to overload the base method, this method must be declared 'Overloads'.
    Sub goo(Of T)()
        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub HidingMembersInInterface()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="HidingMembersInInterface">
    <file name="a.vb">
Option Strict On

Namespace N
    Interface A
        Sub goo()
        Sub goo(x As Integer)
        Enum e
            Red
        End Enum
    End Interface

    Interface B
        Sub bar()
        Sub bar(x As Integer)
    End Interface

    Interface C
        Inherits A, B

        ReadOnly Property quux As Integer
    End Interface

    Interface D
        Inherits C

        Enum bar
            Red
        End Enum

        Enum goo
            Red
        End Enum

        Shadows Enum quux
            red
        End Enum
    End Interface
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40004: enum 'bar' conflicts with sub 'bar' in the base interface 'B' and should be declared 'Shadows'.
        Enum bar
             ~~~
BC40004: enum 'goo' conflicts with sub 'goo' in the base interface 'A' and should be declared 'Shadows'.
        Enum goo
             ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AccessorHidingNonAccessor()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AccessorHidingNonAccessor">
    <file name="a.vb">
Namespace N
    Public Class A
        Public Property Z As Integer
        Public Property ZZ As Integer
    End Class

    Public Class B
        Inherits A

        Public Sub get_Z()
        End Sub
        Public Shadows Sub get_ZZ()
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40014: sub 'get_Z' conflicts with a member implicitly declared for property 'Z' in the base class 'A' and should be declared 'Shadows'.
        Public Sub get_Z()
                   ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub NonAccessorHidingAccessor()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NonAccessorHidingAccessor">
    <file name="a.vb">
Namespace N
    Public Class B
        Public Sub get_X()
        End Sub
        Public Sub get_XX()
        End Sub
        Public Sub set_Z()
        End Sub
        Public Sub set_ZZ()
        End Sub
    End Class

    Public Class A
        Inherits B

        Public Property X As Integer
        Public Property Z As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
        Public Shadows Property XX As Integer
        Public Shadows Property ZZ As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40012: property 'X' implicitly declares 'get_X', which conflicts with a member in the base class 'B', and so the property should be declared 'Shadows'.
        Public Property X As Integer
                        ~
BC40012: property 'Z' implicitly declares 'set_Z', which conflicts with a member in the base class 'B', and so the property should be declared 'Shadows'.
        Public Property Z As Integer
                        ~
</expected>)
        End Sub

        <Fact>
        Public Sub HidingShouldHaveOverloadsOrOverrides()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AccessorHidingNonAccessor">
    <file name="a.vb">
Namespace N
    Public Class A
        Public Sub goo()

        End Sub

        Public Overridable Property bar As Integer

    End Class

    Public Class B
        Inherits A

        Public Sub goo(a As Integer)
        End Sub

        Public Property bar As String
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC40003: sub 'goo' shadows an overloadable member declared in the base class 'A'.  If you want to overload the base method, this method must be declared 'Overloads'.
        Public Sub goo(a As Integer)
                   ~~~
BC40005: property 'bar' shadows an overridable method in the base class 'A'. To override the base method, this method must be declared 'Overrides'.
        Public Property bar As String
                        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub HiddenMustOverride()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="HiddenMustOverride">
    <file name="a.vb">
Option Strict On

Namespace N
    Public MustInherit Class A
        Public MustOverride Sub f()
        Public MustOverride Sub f(a As Integer)
        Public MustOverride Sub g()
        Public MustOverride Sub h()
        Public MustOverride Sub i()
        Public MustOverride Function j(a As String) as Integer
    End Class

    Public MustInherit Class B
        Inherits A

        Public Overrides Sub g()
        End Sub
    End Class

    Public MustInherit Class C
        Inherits B

        Public Overloads Sub h(x As Integer)
        End Sub
    End Class

    Public MustInherit Class D
        Inherits C

        Public Shadows f As Integer
        Public Shadows g As Integer
        Public Shadows Enum h
            Red
        End Enum

        Public Overloads Sub i(x As String, y As String)
        End Sub

        Public Overloads Function j(a as String) As String
            return ""
        End Function
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC31404: 'Public f As Integer' cannot shadow a method declared 'MustOverride'.
        Public Shadows f As Integer
                       ~
BC31404: 'D.h' cannot shadow a method declared 'MustOverride'.
        Public Shadows Enum h
                            ~
BC31404: 'Public Overloads Function j(a As String) As String' cannot shadow a method declared 'MustOverride'.
        Public Overloads Function j(a as String) As String
                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub AccessorHideMustOverride()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AccessorHideMustOverride">
    <file name="a.vb">
Namespace N
    Public MustInherit Class B
        Public MustOverride Property X As Integer
        Public MustOverride Function set_Y(a As Integer)
    End Class

    Public MustInherit Class A
        Inherits B

        Public Shadows Function get_X() As Integer
            Return 0
        End Function

        Public Shadows Property Y As Integer
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC31413: 'Public Property Set Y(AutoPropertyValue As Integer)', implicitly declared for property 'Y', cannot shadow a 'MustOverride' method in the base class 'B'.
        Public Shadows Property Y As Integer
                                ~
</expected>)
        End Sub

        <Fact>
        Public Sub NoOverride()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoOverride">
    <file name="a.vb">
Option Strict On

Namespace N
    Class A
        Public Overridable Property x As Integer

        Public Overridable Sub y()
        End Sub

        Public z As Integer

    End Class

    Class B
        Inherits A

        Public Overrides Sub x(a As String, b As Integer)
        End Sub

        Public Overrides Sub y(x As Integer)
        End Sub

        Public Overrides Property z As Integer
    End Class

    Structure K
        Public Overrides Function f() As Integer
            Return 0
        End Function
    End Structure
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30284: sub 'x' cannot be declared 'Overrides' because it does not override a sub in a base class.
        Public Overrides Sub x(a As String, b As Integer)
                             ~
BC40004: sub 'x' conflicts with property 'x' in the base class 'A' and should be declared 'Shadows'.
        Public Overrides Sub x(a As String, b As Integer)
                             ~
BC30284: sub 'y' cannot be declared 'Overrides' because it does not override a sub in a base class.
        Public Overrides Sub y(x As Integer)
                             ~
BC30284: property 'z' cannot be declared 'Overrides' because it does not override a property in a base class.
        Public Overrides Property z As Integer
                                  ~
BC40004: property 'z' conflicts with variable 'z' in the base class 'A' and should be declared 'Shadows'.
        Public Overrides Property z As Integer
                                  ~
BC30284: function 'f' cannot be declared 'Overrides' because it does not override a function in a base class.
        Public Overrides Function f() As Integer
                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousOverride()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AmbiguousOverride">
    <file name="a.vb">
Namespace N
    Class A(Of T, U)
        Public Overridable Sub goo(a As T)
        End Sub

        Public Overridable Sub goo(a As U)
        End Sub

        Public Overridable Sub goo(a As String)
        End Sub

        Public Overridable Property bar As Integer

        Public Overridable ReadOnly Property bar(a As T) As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overridable ReadOnly Property bar(a As U) As Integer
            Get
                Return 0
            End Get
        End Property
        Public Overridable ReadOnly Property bar(a As String) As Integer
            Get
                Return 0
            End Get
        End Property

    End Class

    Class B
        Inherits A(Of String, String)

        Public Overrides Sub goo(a As String)
        End Sub

        Public Overrides ReadOnly Property bar(a As String) As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30935: Member 'Public Overridable Sub goo(a As String)' that matches this signature cannot be overridden because the class 'A' contains multiple members with this same name and signature: 
   'Public Overridable Sub goo(a As T)'
   'Public Overridable Sub goo(a As U)'
   'Public Overridable Sub goo(a As String)'
        Public Overrides Sub goo(a As String)
                             ~~~
BC30935: Member 'Public Overridable ReadOnly Property bar(a As String) As Integer' that matches this signature cannot be overridden because the class 'A' contains multiple members with this same name and signature: 
   'Public Overridable ReadOnly Property bar(a As T) As Integer'
   'Public Overridable ReadOnly Property bar(a As U) As Integer'
   'Public Overridable ReadOnly Property bar(a As String) As Integer'
        Public Overrides ReadOnly Property bar(a As String) As Integer
                                           ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub OverrideNotOverridable()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="OverrideNotOverridable">
    <file name="a.vb">
Option Strict On

Namespace N
    Public Class A
        Public Overridable Sub f()
        End Sub

        Public Overridable Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Public Class B
        Inherits A

        Public NotOverridable Overrides Sub f()
        End Sub

        Public NotOverridable Overrides Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Public Class C
        Inherits B

        Public Overrides Sub f()
        End Sub

        Public NotOverridable Overrides Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30267: 'Public Overrides Sub f()' cannot override 'Public NotOverridable Overrides Sub f()' because it is declared 'NotOverridable'.
        Public Overrides Sub f()
                             ~
BC30267: 'Public NotOverridable Overrides Property p As Integer' cannot override 'Public NotOverridable Overrides Property p As Integer' because it is declared 'NotOverridable'.
        Public NotOverridable Overrides Property p As Integer
                                                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub MustBeOverridable()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MustBeOverridable">
    <file name="a.vb">
Option Strict On

Namespace N
    Public Class A
        Public Sub f()
        End Sub
        Public Property p As Integer

    End Class

    Public Class B
        Inherits A

        Public Overrides Sub f()
        End Sub

        Public Overrides Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC31086: 'Public Overrides Sub f()' cannot override 'Public Sub f()' because it is not declared 'Overridable'.
        Public Overrides Sub f()
                             ~
BC31086: 'Public Overrides Property p As Integer' cannot override 'Public Property p As Integer' because it is not declared 'Overridable'.
        Public Overrides Property p As Integer
                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub ByRefMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ByRefMismatch">
    <file name="a.vb">
Namespace N
    Class A
        Public Overridable Sub f(q As String, ByRef a As Integer)
        End Sub
    End Class

    Class B
        Inherits A
        Public Overrides Sub f(q As String, a As Integer)
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30398: 'Public Overrides Sub f(q As String, a As Integer)' cannot override 'Public Overridable Sub f(q As String, ByRef a As Integer)' because they differ by a parameter that is marked as 'ByRef' versus 'ByVal'.
        Public Overrides Sub f(q As String, a As Integer)
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub OptionalMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="OptionalMismatch">
    <file name="a.vb">
Namespace N
    Class A
        Public Overridable Sub f(q As String, Optional a As Integer = 5)
        End Sub
        Public Overridable Sub g(q As String)
        End Sub
        Public Overridable Property p1(q As String, Optional a As Integer = 5) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
        Public Overridable Property p2(q As String) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Class B
        Inherits A
        Public Overrides Sub f(q As String)
        End Sub
        Public Overrides Sub g(q As String, Optional a As Integer = 4)
        End Sub
        Public Overrides Property p1(q As String) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
        Public Overrides Property p2(q As String, Optional a As Integer = 5) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30308: 'Public Overrides Sub f(q As String)' cannot override 'Public Overridable Sub f(q As String, [a As Integer = 5])' because they differ by optional parameters.
        Public Overrides Sub f(q As String)
                             ~
BC30308: 'Public Overrides Sub g(q As String, [a As Integer = 4])' cannot override 'Public Overridable Sub g(q As String)' because they differ by optional parameters.
        Public Overrides Sub g(q As String, Optional a As Integer = 4)
                             ~
BC30308: 'Public Overrides Property p1(q As String) As Integer' cannot override 'Public Overridable Property p1(q As String, [a As Integer = 5]) As Integer' because they differ by optional parameters.
        Public Overrides Property p1(q As String) As Integer
                                  ~~
BC30308: 'Public Overrides Property p2(q As String, [a As Integer = 5]) As Integer' cannot override 'Public Overridable Property p2(q As String) As Integer' because they differ by optional parameters.
        Public Overrides Property p2(q As String, Optional a As Integer = 5) As Integer
                                  ~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReturnTypeMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ReturnTypeMismatch">
    <file name="a.vb">
Namespace N
    Class A
        Public Overridable Function x(a As Integer) As String
            Return ""
        End Function

        Public Overridable Function y(Of T)() As T
            Return Nothing
        End Function

        Public Overridable Sub z()
        End Sub

        Public Overridable Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Class B
        Inherits A

        Public Overrides Function y(Of U)() As U
            Return Nothing
        End Function

        Public Overrides Function x(a As Integer) As Integer
            Return 0
        End Function

        Public Overrides Function z() As Integer
            Return 0
        End Function

        Public Overrides Property p As String
            Get
                Return ""
            End Get
            Set(value As String)
            End Set
        End Property
    End Class
End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30437: 'Public Overrides Function x(a As Integer) As Integer' cannot override 'Public Overridable Function x(a As Integer) As String' because they differ by their return types.
        Public Overrides Function x(a As Integer) As Integer
                                  ~
BC30437: 'Public Overrides Function z() As Integer' cannot override 'Public Overridable Sub z()' because they differ by their return types.
        Public Overrides Function z() As Integer
                                  ~
BC30437: 'Public Overrides Property p As String' cannot override 'Public Overridable Property p As Integer' because they differ by their return types.
        Public Overrides Property p As String
                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub PropertyTypeMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PropertyTypeMismatch">
        <file name="a.vb">
Namespace N
    Class A
        Public Overridable Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overridable ReadOnly Property q As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overridable WriteOnly Property r As Integer
            Set(value As Integer)
            End Set
        End Property
    End Class

    Class B
        Inherits A

        Public Overrides ReadOnly Property p As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides WriteOnly Property q As Integer
            Set(value As Integer)
            End Set
        End Property

        Public Overrides Property r As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC30362: 'Public Overrides ReadOnly Property p As Integer' cannot override 'Public Overridable Property p As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
        Public Overrides ReadOnly Property p As Integer
                                           ~
BC30362: 'Public Overrides WriteOnly Property q As Integer' cannot override 'Public Overridable ReadOnly Property q As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
        Public Overrides WriteOnly Property q As Integer
                                            ~
BC30362: 'Public Overrides Property r As Integer' cannot override 'Public Overridable WriteOnly Property r As Integer' because they differ by 'ReadOnly' or 'WriteOnly'.
        Public Overrides Property r As Integer
                                  ~
    </expected>)
        End Sub

        <WorkItem(540791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540791")>
        <Fact>
        Public Sub PropertyAccessibilityMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PropertyAccessibilityMismatch">
        <file name="a.vb">
Public Class Base
    Public Overridable Property Property1() As Long
        Get
            Return m_Property1
        End Get
        Protected Set(value As Long)
            m_Property1 = Value
        End Set
    End Property
    Private m_Property1 As Long
End Class
 
Public Class Derived1
    Inherits Base
    Public Overrides Property Property1() As Long
        Get
            Return m_Property1
        End Get
        Private Set(value As Long)
            m_Property1 = Value
        End Set
    End Property
    Private m_Property1 As Long
End Class
    </file>
    </compilation>)

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadOverrideAccess2, "Set").WithArguments("Private Overrides Property Set Property1(value As Long)", "Protected Overridable Property Set Property1(value As Long)"))
        End Sub

        <Fact>
        Public Sub PropertyAccessibilityMismatch2()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PropertyAccessibilityMismatch">
        <file name="a.vb">
Public Class Base
    Public Overridable Property Property1() As Long
        Get
            Return m_Property1
        End Get
        Set(value As Long)
            m_Property1 = Value
        End Set
    End Property
    Private m_Property1 As Long
End Class
 
Public Class Derived1
    Inherits Base
    Public Overrides Property Property1() As Long
        Protected Get
            Return m_Property1
        End Get
        Set(value As Long)
            m_Property1 = Value
        End Set
    End Property
    Private m_Property1 As Long
End Class
    </file>
    </compilation>)

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadOverrideAccess2, "Get").WithArguments("Protected Overrides Property Get Property1() As Long", "Public Overridable Property Get Property1() As Long"))
        End Sub

        <Fact>
        <WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")>
        Public Sub PropertyOverrideAccessibility()
            Dim csharpComp = CreateCSharpCompilation("Lib", <![CDATA[
public class A
{
    public virtual int P
    {
        get
        {
            System.Console.WriteLine("A.P.get");
            return 0;
        }

        protected internal set
        {
            System.Console.WriteLine("A.P.set");
        }
    }
}

public class B : A
{
    public override int P
    {
        protected internal set
        {
            System.Console.WriteLine("B.P.set");
        }
    }
}

public class C : A
{
    public override int P
    {
        get
        {
            System.Console.WriteLine("C.P.get");
            return 0;
        }
    }
}
]]>)
            csharpComp.VerifyDiagnostics()
            Dim csharpRef = csharpComp.EmitToImageReference()

            Dim vbComp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="PropertyOverrideAccessibility">
        <file name="a.vb">
Public Class D1
    Inherits A

    Public Overrides Property P() As Integer
        Get
            System.Console.WriteLine("D1.P.get")
            Return 0
        End Get
        Protected Set(value As Integer)
            System.Console.WriteLine("D1.P.set")
        End Set
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Public Class D2
    Inherits B

    Protected Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
            System.Console.WriteLine("D2.P.set")
        End Set
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Public Class D3
    Inherits C

    Public Overrides ReadOnly Property P() As Integer
        Get
            System.Console.WriteLine("D3.P.get")
            Return 0
        End Get
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Module Test
    Sub Main()
        Dim d1 As New D1()
        Dim d2 As New D2()
        Dim d3 As New D3()

        d1.Test()
        d2.Test()
        d3.Test()
    End Sub
End Module
    </file>
    </compilation>, {csharpRef}, TestOptions.ReleaseExe)
            CompileAndVerify(vbComp, expectedOutput:=<![CDATA[
D1.P.set
D1.P.get
D2.P.set
A.P.get
A.P.set
D3.P.get
]]>)

            Dim errorComp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="PropertyOverrideAccessibility">
        <file name="a.vb">
' Set is protected friend, but should be protected
Public Class D1
    Inherits A

    Public Overrides Property P() As Integer
        Get
            Return 0
        End Get
        Protected Friend Set(value As Integer)
        End Set
    End Property
End Class

' protected friend, should be protected
Public Class D2
    Inherits B

    Protected Friend Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
        End Set
    End Property
End Class

' Can't override getter (Dev11 also gives error about accessibility change)
Public Class D3
    Inherits B

    Public Overrides ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
End Class

' Getter has to be public
Public Class D4
    Inherits C

    Protected Overrides ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
End Class

' Can't override setter (Dev11 also gives error about accessibility change)
Public Class D5
    Inherits C

    Protected Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
    </file>
    </compilation>, {csharpRef})
            errorComp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_FriendAssemblyBadAccessOverride2, "P").WithArguments("Protected Friend Overrides WriteOnly Property P As Integer", "Protected Friend Overrides WriteOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_FriendAssemblyBadAccessOverride2, "Set").WithArguments("Protected Friend Overrides Property Set P(value As Integer)", "Protected Friend Overridable Overloads Property Set P(value As Integer)"),
                Diagnostic(ERRID.ERR_OverridingPropertyKind2, "P").WithArguments("Protected Overrides WriteOnly Property P As Integer", "Public Overrides ReadOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_OverridingPropertyKind2, "P").WithArguments("Public Overrides ReadOnly Property P As Integer", "Protected Friend Overrides WriteOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_BadOverrideAccess2, "P").WithArguments("Protected Overrides ReadOnly Property P As Integer", "Public Overrides ReadOnly Property P As Integer"))
        End Sub

        <Fact>
        <WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")>
        Public Sub PropertyOverrideAccessibilityInternalsVisibleTo()
            Dim csharpComp = CreateCSharpCompilation("Lib", <![CDATA[
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PropertyOverrideAccessibilityInternalsVisibleTo")]

public class A
{
    public virtual int P
    {
        get
        {
            System.Console.WriteLine("A.P.get");
            return 0;
        }

        protected internal set
        {
            System.Console.WriteLine("A.P.set");
        }
    }

    internal static void ConfirmIVT() { }
}

public class B : A
{
    public override int P
    {
        protected internal set
        {
            System.Console.WriteLine("B.P.set");
        }
    }
}

public class C : A
{
    public override int P
    {
        get
        {
            System.Console.WriteLine("C.P.get");
            return 0;
        }
    }
}
]]>)
            csharpComp.VerifyDiagnostics()
            Dim csharpRef = csharpComp.EmitToImageReference()

            ' Unlike in C#, internals-visible-to does not affect the way protected friend
            ' members are overridden (i.e. still must be protected).
            Dim vbComp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="PropertyOverrideAccessibilityInternalsVisibleTo">
        <file name="a.vb">
Public Class D1
    Inherits A

    Public Overrides Property P() As Integer
        Get
            System.Console.WriteLine("D1.P.get")
            Return 0
        End Get
        Protected Set(value As Integer)
            System.Console.WriteLine("D1.P.set")
        End Set
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Public Class D2
    Inherits B

    Protected Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
            System.Console.WriteLine("D2.P.set")
        End Set
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Public Class D3
    Inherits C

    Public Overrides ReadOnly Property P() As Integer
        Get
            System.Console.WriteLine("D3.P.get")
            Return 0
        End Get
    End Property

    Public Sub Test()
        Me.P = 1
        Dim x = Me.P
    End Sub
End Class

Module Test
    Sub Main()
        A.ConfirmIVT()

        Dim d1 As New D1()
        Dim d2 As New D2()
        Dim d3 As New D3()

        d1.Test()
        d2.Test()
        d3.Test()
    End Sub
End Module
    </file>
    </compilation>, {csharpRef}, TestOptions.ReleaseExe)
            CompileAndVerify(vbComp, expectedOutput:=<![CDATA[
D1.P.set
D1.P.get
D2.P.set
A.P.get
A.P.set
D3.P.get
]]>)

            Dim errorComp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="PropertyOverrideAccessibility">
        <file name="a.vb">
' Set is protected friend, but should be protected
Public Class D1
    Inherits A

    Public Overrides Property P() As Integer
        Get
            Return 0
        End Get
        Protected Friend Set(value As Integer)
        End Set
    End Property
End Class

' protected friend, should be protected
Public Class D2
    Inherits B

    Protected Friend Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
        End Set
    End Property
End Class

' Can't override getter (Dev11 also gives error about accessibility change)
Public Class D3
    Inherits B

    Public Overrides ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
End Class

' Getter has to be public
Public Class D4
    Inherits C

    Protected Overrides ReadOnly Property P() As Integer
        Get
            Return 0
        End Get
    End Property
End Class

' Can't override setter (Dev11 also gives error about accessibility change)
Public Class D5
    Inherits C

    Protected Overrides WriteOnly Property P() As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
    </file>
    </compilation>, {csharpRef})
            errorComp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_FriendAssemblyBadAccessOverride2, "P").WithArguments("Protected Friend Overrides WriteOnly Property P As Integer", "Protected Friend Overrides WriteOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_FriendAssemblyBadAccessOverride2, "Set").WithArguments("Protected Friend Overrides Property Set P(value As Integer)", "Protected Friend Overridable Overloads Property Set P(value As Integer)"),
                Diagnostic(ERRID.ERR_OverridingPropertyKind2, "P").WithArguments("Protected Overrides WriteOnly Property P As Integer", "Public Overrides ReadOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_OverridingPropertyKind2, "P").WithArguments("Public Overrides ReadOnly Property P As Integer", "Protected Friend Overrides WriteOnly Property P As Integer"),
                Diagnostic(ERRID.ERR_BadOverrideAccess2, "P").WithArguments("Protected Overrides ReadOnly Property P As Integer", "Public Overrides ReadOnly Property P As Integer"))
        End Sub

        <Fact()>
        Public Sub OptionalValueMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OptionalValueMismatch">
        <file name="a.vb">
Namespace N
    Class A
        Public Overridable Property p(Optional k As Integer = 4) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overridable Sub f(Optional k As String = "goo")
        End Sub
    End Class

    Class B
        Inherits A

        Public Overrides Property p(Optional k As Integer = 7) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overrides Sub f(Optional k As String = "hi")
        End Sub
    End Class
End Namespace    
</file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC30307: 'Public Overrides Property p([k As Integer = 7]) As Integer' cannot override 'Public Overridable Property p([k As Integer = 4]) As Integer' because they differ by the default values of optional parameters.
        Public Overrides Property p(Optional k As Integer = 7) As Integer
                                  ~
BC30307: 'Public Overrides Sub f([k As String = "hi"])' cannot override 'Public Overridable Sub f([k As String = "goo"])' because they differ by the default values of optional parameters.
        Public Overrides Sub f(Optional k As String = "hi")
                             ~
    </expected>)
        End Sub

        <Fact>
        Public Sub ParamArrayMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ParamArrayMismatch">
        <file name="a.vb">
Namespace N
    Class A
        Public Overridable Property p(x() As Integer) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overridable Sub f(ParamArray x() As String)
        End Sub
    End Class

    Class B
        Inherits A

        Public Overrides Property p(ParamArray x() As Integer) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overrides Sub f(x() As String)
        End Sub
    End Class
End Namespace
</file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC30906: 'Public Overrides Property p(ParamArray x As Integer()) As Integer' cannot override 'Public Overridable Property p(x As Integer()) As Integer' because they differ by parameters declared 'ParamArray'.
        Public Overrides Property p(ParamArray x() As Integer) As Integer
                                  ~
BC30906: 'Public Overrides Sub f(x As String())' cannot override 'Public Overridable Sub f(ParamArray x As String())' because they differ by parameters declared 'ParamArray'.
        Public Overrides Sub f(x() As String)
                             ~
    </expected>)
        End Sub

        <WorkItem(529018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529018")>
        <Fact()>
        Public Sub OptionalTypeMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OptionalTypeMismatch">
        <file name="a.vb">
Namespace N
    Class A
        Public Overridable Property p(Optional x As String = "") As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overridable Sub f(Optional x As String = "")
        End Sub
    End Class

    Class B
        Inherits A

        Public Overrides Property p(Optional x As Integer = 0) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Public Overrides Sub f(Optional x As Integer = 0)
        End Sub
    End Class
End Namespace
</file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC30697: 'Public Overrides Property p([x As Integer = 0]) As Integer' cannot override 'Public Overridable Property p([x As String = ""]) As Integer' because they differ by the types of optional parameters.
        Public Overrides Property p(Optional x As Integer = 0) As Integer
                                  ~
BC30697: 'Public Overrides Sub f([x As Integer = 0])' cannot override 'Public Overridable Sub f([x As String = ""])' because they differ by the types of optional parameters.
        Public Overrides Sub f(Optional x As Integer = 0)
                             ~        
    </expected>)
        End Sub

        <Fact()>
        Public Sub ConstraintMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConstraintMismatch">
        <file name="a.vb">
Imports System
Namespace N
    Class A

        Public Overridable Sub f(Of T As ICloneable)(x As T)
        End Sub
    End Class

    Class B
        Inherits A

        Public Overrides Sub f(Of U)(x As U)
        End Sub
    End Class
End Namespace
</file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC32077: 'Public Overrides Sub f(Of U)(x As U)' cannot override 'Public Overridable Sub f(Of T)(x As T)' because they differ by type parameter constraints.
        Public Overrides Sub f(Of U)(x As U)
                             ~
    </expected>)
        End Sub

        <Fact>
        Public Sub AccessMismatch()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AccessMismatch">
        <file name="a.vb">
Namespace N
    Class A

        Public Overridable Sub f()
        End Sub

        Protected Overridable Sub g()
        End Sub

        Friend Overridable Sub h()
        End Sub
    End Class

    Class B
        Inherits A

        Protected Overrides Sub f()
        End Sub

        Public Overrides Sub g()
        End Sub

        Protected Friend Overrides Sub h()
        End Sub
    End Class
End Namespace
</file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
    <expected>
BC30266: 'Protected Overrides Sub f()' cannot override 'Public Overridable Sub f()' because they have different access levels.
        Protected Overrides Sub f()
                                ~
BC30266: 'Public Overrides Sub g()' cannot override 'Protected Overridable Sub g()' because they have different access levels.
        Public Overrides Sub g()
                             ~
BC30266: 'Protected Friend Overrides Sub h()' cannot override 'Friend Overridable Sub h()' because they have different access levels.
        Protected Friend Overrides Sub h()
                                       ~
    </expected>)
        End Sub

        <Fact>
        Public Sub PropertyShadows()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Interface IA
    Default Overloads ReadOnly Property P(o As Object)
    Property Q(o As Object)
End Interface
Interface IB
    Inherits IA
    Default Overloads ReadOnly Property P(x As Integer, y As Integer)
    Overloads Property Q(x As Integer, y As Integer)
End Interface
Interface IC
    Inherits IA
    Default Shadows ReadOnly Property P(x As Integer, y As Integer)
    Shadows Property Q(x As Integer, y As Integer)
End Interface
Module M
    Sub M(b As IB, c As IC)
        Dim value As Object
        value = b.P(1, 2)
        value = b.P(3)
        value = b(1, 2)
        value = b(3)
        b.Q(1, 2) = value
        b.Q(3) = value
        value = c.P(1, 2)
        value = c.P(3)
        value = c(1, 2)
        value = c(3)
        c.Q(1, 2) = value
        c.Q(3) = value
    End Sub
End Module
        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30455: Argument not specified for parameter 'y' of 'ReadOnly Default Property P(x As Integer, y As Integer) As Object'.
        value = c.P(3)
                  ~
BC30455: Argument not specified for parameter 'y' of 'ReadOnly Default Property P(x As Integer, y As Integer) As Object'.
        value = c(3)
                ~
BC30455: Argument not specified for parameter 'y' of 'Property Q(x As Integer, y As Integer) As Object'.
        c.Q(3) = value
          ~
</expected>)
        End Sub

        <Fact>
        Public Sub ShadowsNotOverloads()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class A
    Public Sub M1(o As Object)
    End Sub
    Public Overloads Sub M2(o As Object)
    End Sub
    Public ReadOnly Property P1(o As Object)
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads ReadOnly Property P2(o As Object)
        Get
            Return Nothing
        End Get
    End Property
End Class
Class B
    Inherits A
    Public Shadows Sub M1(x As Integer, y As Integer)
    End Sub
    Public Overloads Sub M2(x As Integer, y As Integer)
    End Sub
    Public Shadows ReadOnly Property P1(x As Integer, y As Integer)
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads ReadOnly Property P2(x As Integer, y As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
Module M
    Sub M(o As B)
        Dim value
        o.M1(1)
        o.M2(1)
        value = o.P1(1)
        value = o.P2(1)
    End Sub
End Module
        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30455: Argument not specified for parameter 'y' of 'Public Sub M1(x As Integer, y As Integer)'.
        o.M1(1)
          ~~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Property P1(x As Integer, y As Integer) As Object'.
        value = o.P1(1)
                  ~~
</expected>)
        End Sub

        <Fact>
        Public Sub OverridingBlockedByShadowing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A
    Overridable Sub Goo()
    End Sub
    Overridable Sub Bar()
    End Sub
    Overridable Sub Quux()
    End Sub
End Class

Class B
    Inherits A
    Shadows Sub Goo(x As Integer)
    End Sub
    Overloads Property Bar(x As Integer)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
    Public Shadows Quux As Integer
End Class

Class C
    Inherits B
    Overrides Sub Goo()
    End Sub
    Overrides Sub Bar()
    End Sub
    Overrides Sub Quux()
    End Sub
End Class        </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC40004: property 'Bar' conflicts with sub 'Bar' in the base class 'A' and should be declared 'Shadows'.
    Overloads Property Bar(x As Integer)
                       ~~~
BC30284: sub 'Goo' cannot be declared 'Overrides' because it does not override a sub in a base class.
    Overrides Sub Goo()
                  ~~~
BC30284: sub 'Bar' cannot be declared 'Overrides' because it does not override a sub in a base class.
    Overrides Sub Bar()
                  ~~~
BC40004: sub 'Bar' conflicts with property 'Bar' in the base class 'B' and should be declared 'Shadows'.
    Overrides Sub Bar()
                  ~~~
BC30284: sub 'Quux' cannot be declared 'Overrides' because it does not override a sub in a base class.
    Overrides Sub Quux()
                  ~~~~
BC40004: sub 'Quux' conflicts with variable 'Quux' in the base class 'B' and should be declared 'Shadows'.
    Overrides Sub Quux()
                  ~~~~
    </expected>)
        End Sub

        <WorkItem(541752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541752")>
        <Fact>
        Public Sub Bug8634()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A
    Overridable Sub Goo()
    End Sub
End Class

Class B
    Inherits A
    Shadows Property Goo() As Integer
End Class

Class C
    Inherits B
    Overrides Sub Goo()
    End Sub
End Class    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30284: sub 'Goo' cannot be declared 'Overrides' because it does not override a sub in a base class.
    Overrides Sub Goo()
                  ~~~
BC40004: sub 'Goo' conflicts with property 'Goo' in the base class 'B' and should be declared 'Shadows'.
    Overrides Sub Goo()
                  ~~~
    </expected>)
        End Sub

        <Fact>
        Public Sub HideBySig()
            Dim customIL = <![CDATA[
.class public A
{
    .method public hidebysig instance object F(object o)
    {
        ldnull
        ret
    }
    .method public instance object G(object o)
    {
        ldnull
        ret
    }
    .method public hidebysig instance object get_P(object o)
    {
        ldnull
        ret
    }
    .method public instance object get_Q(object o)
    {
        ldnull
        ret
    }
    .property object P(object o)
    {
        .get instance object A::get_P(object o)
    }
    .property object Q(object o)
    {
        .get instance object A::get_Q(object o)
    }
}
.class public B extends A
{
    .method public hidebysig instance object F(object x, object y)
    {
        ldnull
        ret
    }
    .method public instance object G(object x, object y)
    {
        ldnull
        ret
    }
    .method public hidebysig instance object get_P(object x, object y)
    {
        ldnull
        ret
    }
    .method public instance object get_Q(object x, object y)
    {
        ldnull
        ret
    }
    .property object P(object x, object y)
    {
        .get instance object B::get_P(object x, object y)
    }
    .property object Q(object x, object y)
    {
        .get instance object B::get_Q(object x, object y)
    }
}
.class public C
{
    .method public hidebysig instance object F(object o)
    {
        ldnull
        ret
    }
    .method public hidebysig instance object F(object x, object y)
    {
        ldnull
        ret
    }
    .method public instance object G(object o)
    {
        ldnull
        ret
    }
    .method public instance object G(object x, object y)
    {
        ldnull
        ret
    }
    .method public hidebysig instance object get_P(object o)
    {
        ldnull
        ret
    }
    .method public hidebysig instance object get_P(object x, object y)
    {
        ldnull
        ret
    }
    .method public instance object get_Q(object o)
    {
        ldnull
        ret
    }
    .method public instance object get_Q(object x, object y)
    {
        ldnull
        ret
    }
    .property object P(object o)
    {
        .get instance object C::get_P(object o)
    }
    .property object P(object x, object y)
    {
        .get instance object C::get_P(object x, object y)
    }
    .property object Q(object o)
    {
        .get instance object C::get_Q(object o)
    }
    .property object Q(object x, object y)
    {
        .get instance object C::get_Q(object x, object y)
    }
}
]]>
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
    Sub M(b As B, c As C)
        Dim value As Object
        value = b.F(b, c)
        value = b.F(Nothing)
        value = b.G(b, c)
        value = b.G(Nothing)
        value = b.P(b, c)
        value = b.P(Nothing)
        value = b.Q(b, c)
        value = b.Q(Nothing)
        value = c.F(b, c)
        value = c.F(Nothing)
        value = c.G(b, c)
        value = c.G(Nothing)
        value = c.P(b, c)
        value = c.P(Nothing)
        value = c.Q(b, c)
        value = c.Q(Nothing)
    End Sub
End Module
    </file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(source, customIL.Value, includeVbRuntime:=True)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30455: Argument not specified for parameter 'y' of 'Public Function G(x As Object, y As Object) As Object'.
        value = b.G(Nothing)
                  ~
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Property Q(x As Object, y As Object) As Object'.
        value = b.Q(Nothing)
                  ~
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug10702()
            Dim code =
<compilation name="SimpleOverrides">
    <file name="a.vb">
Imports System.Collections.Generic        
Class SyntaxNode : End Class
Structure SyntaxToken : End Structure
Class CancellationToken : End Class
Class Diagnostic : End Class

MustInherit Class BaseSyntaxTree
    Protected MustOverride Overloads Function GetDiagnosticsCore(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
    Protected MustOverride Overloads Function GetDiagnosticsCore(node As SyntaxNode) As IEnumerable(Of Diagnostic)
    Protected MustOverride Overloads Function GetDiagnosticsCore(token As SyntaxToken) As IEnumerable(Of Diagnostic)
End Class

Class SyntaxTree : Inherits BaseSyntaxTree
    Protected Overrides Function GetDiagnosticsCore(Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of Diagnostic)
        Return Nothing
    End Function
    Protected Overrides Function GetDiagnosticsCore(node As SyntaxNode) As IEnumerable(Of Diagnostic)
        Return Nothing
    End Function
    Protected Overrides Function GetDiagnosticsCore(token As SyntaxToken) As IEnumerable(Of Diagnostic)
        Return Nothing
    End Function
End Class

Public Module Module1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(code)

            CompileAndVerify(code).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(543948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543948")>
        Public Sub OverrideMemberOfConstructedProtectedInnerClass()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Public Class Outer1(Of T)
    Protected MustInherit Class Inner1
        Public MustOverride Sub Method()
    End Class

    Protected MustInherit Class Inner2
        Inherits Inner1
        Public Overrides Sub Method()
        End Sub
    End Class
End Class
    </file>
    </compilation>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation>
        <file name="a.vb">
Friend Class Outer2
    Inherits Outer1(Of Outer2)

    Private Class Inner3
        Inherits Inner2
    End Class
End Class
    </file>
    </compilation>, {New VisualBasicCompilationReference(compilation1)})

            CompilationUtils.AssertNoErrors(compilation2)
        End Sub

        <Fact, WorkItem(545484, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545484")>
        Public Sub MetadataOverridesOfAccessors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On

Public Class X1
    Public Overridable Property Goo As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</file>
    </compilation>)

            Dim compilation2 = CreateCSharpCompilation("assem2",
            <![CDATA[
using System;
public class X2: X1 {
     public override int Goo {
         get { return base.Goo; }
         set { base.Goo = value; }
     }

     public virtual event Action Bar { add{} remove{}}
}

public class X3: X2 {
     public override event Action Bar { add {} remove {} }
}

]]>.Value, referencedCompilations:={compilation1})

            Dim compilation2Bytes = compilation2.EmitToArray()

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Option Strict On
Class Dummy
End Class
</file>
    </compilation>, references:={New VisualBasicCompilationReference(compilation1), MetadataReference.CreateFromImage(compilation2Bytes)})

            Dim globalNS = compilation3.GlobalNamespace
            Dim classX1 = DirectCast(globalNS.GetMembers("X1").First(), NamedTypeSymbol)
            Dim propX1Goo = DirectCast(classX1.GetMembers("Goo").First(), PropertySymbol)
            Dim accessorX1GetGoo = DirectCast(classX1.GetMembers("get_Goo").First(), MethodSymbol)
            Dim accessorX1SetGoo = DirectCast(classX1.GetMembers("set_Goo").First(), MethodSymbol)
            Dim classX2 = DirectCast(globalNS.GetMembers("X2").First(), NamedTypeSymbol)
            Dim propX2Goo = DirectCast(classX2.GetMembers("Goo").First(), PropertySymbol)
            Dim accessorX2GetGoo = DirectCast(classX2.GetMembers("get_Goo").First(), MethodSymbol)
            Dim accessorX2SetGoo = DirectCast(classX2.GetMembers("set_Goo").First(), MethodSymbol)
            Dim classX3 = DirectCast(globalNS.GetMembers("X3").First(), NamedTypeSymbol)

            Dim overriddenPropX1Goo = propX1Goo.OverriddenProperty
            Assert.Null(overriddenPropX1Goo)
            Dim overriddenPropX2Goo = propX2Goo.OverriddenProperty
            Assert.NotNull(overriddenPropX2Goo)
            Assert.Equal(propX1Goo, overriddenPropX2Goo)

            Dim overriddenAccessorX1GetGoo = accessorX1GetGoo.OverriddenMethod
            Assert.Null(overriddenAccessorX1GetGoo)
            Dim overriddenAccessorX2GetGoo = accessorX2GetGoo.OverriddenMethod
            Assert.NotNull(overriddenAccessorX2GetGoo)
            Assert.Equal(accessorX1GetGoo, overriddenAccessorX2GetGoo)

            Dim overriddenAccessorX1SetGoo = accessorX1SetGoo.OverriddenMethod
            Assert.Null(overriddenAccessorX1SetGoo)
            Dim overriddenAccessorX2SetGoo = accessorX2SetGoo.OverriddenMethod
            Assert.NotNull(overriddenAccessorX2SetGoo)
            Assert.Equal(accessorX1SetGoo, overriddenAccessorX2SetGoo)

            Dim eventX2Bar = DirectCast(classX2.GetMembers("Bar").First(), EventSymbol)
            Dim accessorX2AddBar = DirectCast(classX2.GetMembers("add_Bar").First(), MethodSymbol)
            Dim accessorX2RemoveBar = DirectCast(classX2.GetMembers("remove_Bar").First(), MethodSymbol)
            Dim eventX3Bar = DirectCast(classX3.GetMembers("Bar").First(), EventSymbol)
            Dim accessorX3AddBar = DirectCast(classX3.GetMembers("add_Bar").First(), MethodSymbol)
            Dim accessorX3RemoveBar = DirectCast(classX3.GetMembers("remove_Bar").First(), MethodSymbol)

            Dim overriddenEventX2Bar = eventX2Bar.OverriddenEvent
            Assert.Null(overriddenEventX2Bar)
            Dim overriddenEventX3Bar = eventX3Bar.OverriddenEvent
            Assert.NotNull(overriddenEventX3Bar)
            Assert.Equal(eventX2Bar, overriddenEventX3Bar)

            Dim overriddenAccessorsX2AddBar = accessorX2AddBar.OverriddenMethod
            Assert.Null(overriddenAccessorsX2AddBar)
            Dim overriddenAccessorsX3AddBar = accessorX3AddBar.OverriddenMethod
            Assert.NotNull(overriddenAccessorsX3AddBar)
            Assert.Equal(accessorX2AddBar, overriddenAccessorsX3AddBar)

            Dim overriddenAccessorsX2RemoveBar = accessorX2RemoveBar.OverriddenMethod
            Assert.Null(overriddenAccessorsX2RemoveBar)
            Dim overriddenAccessorsX3RemoveBar = accessorX3RemoveBar.OverriddenMethod
            Assert.NotNull(overriddenAccessorsX3RemoveBar)
            Assert.Equal(accessorX2RemoveBar, overriddenAccessorsX3RemoveBar)

        End Sub

        <Fact, WorkItem(545484, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545484")>
        Public Sub OverridesOfConstructedMethods()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On

Public Class X1
    Public Overridable Function Goo(Of T)(x as T) As Integer
            Return 1
    End Function
End Class
</file>
    </compilation>)

            Dim compilation2 = CreateCSharpCompilation("assem2",
            <![CDATA[
using System;
public class X2: X1 {
     public override int Goo<T>(T x)
     {
         return base.Goo(x);
     }
}
]]>.Value, referencedCompilations:={compilation1})

            Dim compilation2Bytes = compilation2.EmitToArray()

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Option Strict On
Class Dummy
End Class
</file>
    </compilation>, references:={New VisualBasicCompilationReference(compilation1), MetadataReference.CreateFromImage(compilation2Bytes)})

            Dim globalNS = compilation3.GlobalNamespace
            Dim classX1 = DirectCast(globalNS.GetMembers("X1").First(), NamedTypeSymbol)
            Dim methodX1Goo = DirectCast(classX1.GetMembers("Goo").First(), MethodSymbol)
            Dim classX2 = DirectCast(globalNS.GetMembers("X2").First(), NamedTypeSymbol)
            Dim methodX2Goo = DirectCast(classX2.GetMembers("Goo").First(), MethodSymbol)

            Dim overriddenMethX1Goo = methodX1Goo.OverriddenMethod
            Assert.Null(overriddenMethX1Goo)
            Dim overriddenMethX2Goo = methodX2Goo.OverriddenMethod
            Assert.NotNull(overriddenMethX2Goo)
            Assert.Equal(methodX1Goo, overriddenMethX2Goo)

            ' Constructed methods should never override.
            Dim constructedMethodX1Goo = methodX1Goo.Construct(compilation3.GetWellKnownType(WellKnownType.System_Exception))
            Dim constructedMethodX2Goo = methodX2Goo.Construct(compilation3.GetWellKnownType(WellKnownType.System_Exception))

            Dim overriddenConstructedMethX1Goo = constructedMethodX1Goo.OverriddenMethod
            Assert.Null(overriddenConstructedMethX1Goo)
            Dim overriddenConstructedMethX2Goo = constructedMethodX2Goo.OverriddenMethod
            Assert.Null(overriddenConstructedMethX2Goo)
        End Sub

        <Fact, WorkItem(539893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539893")>
        Public Sub AccessorMetadataCasing()
            Dim compilation1 = CreateCSharpCompilation("assem2",
            <![CDATA[
using System;
using System.Collections.Generic;

public class CSharpBase
{
    public virtual int Prop1 { get { return 0; } set { } }
}
]]>.Value)

            Dim compilation1Bytes = compilation1.EmitToArray()

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Option Strict On
Imports System

Class X1
    Inherits CSharpBase

    Public Overloads Property pRop1(x As String) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property

    Public Overrides Property prop1 As Integer
        Get
            Return MyBase.Prop1
        End Get
        Set(value As Integer)
            MyBase.Prop1 = value
        End Set
    End Property

    Public Overridable Overloads Property pROP1(x As Long) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property

End Class

Class X2
    Inherits X1
    Public Overloads Property PROP1(x As Double) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property

    Public Overloads Overrides Property proP1(x As Long) As String
        Get
            Return ""
        End Get
        Set(value As String)
        End Set
    End Property

    Public Overrides Property PrOp1 As Integer

End Class
</file>
    </compilation>, references:={MetadataReference.CreateFromImage(compilation1Bytes)})

            Dim globalNS = compilation2.GlobalNamespace
            Dim classX1 = DirectCast(globalNS.GetMembers("X1").First(), NamedTypeSymbol)
            Dim classX2 = DirectCast(globalNS.GetMembers("X2").First(), NamedTypeSymbol)

            Dim x1Getters = (From memb In classX1.GetMembers("get_Prop1")
                             Where memb.Kind = SymbolKind.Method
                             Select DirectCast(memb, MethodSymbol))
            Dim x1Setters = (From memb In classX1.GetMembers("set_Prop1")
                             Where memb.Kind = SymbolKind.Method
                             Select DirectCast(memb, MethodSymbol))
            Dim x2Getters = (From memb In classX2.GetMembers("get_Prop1")
                             Where memb.Kind = SymbolKind.Method
                             Select DirectCast(memb, MethodSymbol))
            Dim x2Setters = (From memb In classX2.GetMembers("set_Prop1")
                             Where memb.Kind = SymbolKind.Method
                             Select DirectCast(memb, MethodSymbol))

            Dim x1noArgGetter = (From meth In x1Getters Let params = meth.Parameters Where params.Length = 0 Select meth).First()
            Assert.Equal("get_prop1", x1noArgGetter.Name)
            Assert.Equal("get_Prop1", x1noArgGetter.MetadataName)
            Dim x1StringArgGetter = (From meth In x1Getters Let params = meth.Parameters Where params.Length = 1 AndAlso params(0).Type.SpecialType = SpecialType.System_String Select meth).First()
            Assert.Equal("get_pRop1", x1StringArgGetter.Name)
            Assert.Equal("get_pRop1", x1StringArgGetter.MetadataName)
            Dim x1LongArgGetter = (From meth In x1Getters Let params = meth.Parameters Where params.Length = 1 AndAlso params(0).Type.SpecialType = SpecialType.System_Int64 Select meth).First()
            Assert.Equal("get_pROP1", x1LongArgGetter.Name)
            Assert.Equal("get_pROP1", x1LongArgGetter.MetadataName)

            Dim x2noArgGetter = (From meth In x2Getters Let params = meth.Parameters Where params.Length = 0 Select meth).First()
            Assert.Equal("get_PrOp1", x2noArgGetter.Name)
            Assert.Equal("get_Prop1", x2noArgGetter.MetadataName)
            Dim x2LongArgGetter = (From meth In x2Getters Let params = meth.Parameters Where params.Length = 1 AndAlso params(0).Type.SpecialType = SpecialType.System_Int64 Select meth).First()
            Assert.Equal("get_proP1", x2LongArgGetter.Name)
            Assert.Equal("get_pROP1", x2LongArgGetter.MetadataName)
            Dim x2DoubleArgGetter = (From meth In x2Getters Let params = meth.Parameters Where params.Length = 1 AndAlso params(0).Type.SpecialType = SpecialType.System_Double Select meth).First()
            Assert.Equal("get_PROP1", x2DoubleArgGetter.Name)
            Assert.Equal("get_PROP1", x2DoubleArgGetter.MetadataName)

            Dim x1noArgSetter = (From meth In x1Setters Let params = meth.Parameters Where params.Length = 1 Select meth).First()
            Assert.Equal("set_prop1", x1noArgSetter.Name)
            Assert.Equal("set_Prop1", x1noArgSetter.MetadataName)
            Dim x1StringArgSetter = (From meth In x1Setters Let params = meth.Parameters Where params.Length = 2 AndAlso params(0).Type.SpecialType = SpecialType.System_String Select meth).First()
            Assert.Equal("set_pRop1", x1StringArgSetter.Name)
            Assert.Equal("set_pRop1", x1StringArgSetter.MetadataName)
            Dim x1LongArgSetter = (From meth In x1Setters Let params = meth.Parameters Where params.Length = 2 AndAlso params(0).Type.SpecialType = SpecialType.System_Int64 Select meth).First()
            Assert.Equal("set_pROP1", x1LongArgSetter.Name)
            Assert.Equal("set_pROP1", x1LongArgSetter.MetadataName)

            Dim x2noArgSetter = (From meth In x2Setters Let params = meth.Parameters Where params.Length = 1 Select meth).First()
            Assert.Equal("set_PrOp1", x2noArgSetter.Name)
            Assert.Equal("set_Prop1", x2noArgSetter.MetadataName)
            Dim x2LongArgSetter = (From meth In x2Setters Let params = meth.Parameters Where params.Length = 2 AndAlso params(0).Type.SpecialType = SpecialType.System_Int64 Select meth).First()
            Assert.Equal("set_proP1", x2LongArgSetter.Name)
            Assert.Equal("set_pROP1", x2LongArgSetter.MetadataName)
            Dim x2DoubleArgSetter = (From meth In x2Setters Let params = meth.Parameters Where params.Length = 2 AndAlso params(0).Type.SpecialType = SpecialType.System_Double Select meth).First()
            Assert.Equal("set_PROP1", x2DoubleArgSetter.Name)
            Assert.Equal("set_PROP1", x2DoubleArgSetter.MetadataName)

        End Sub

        <Fact(), WorkItem(546816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546816")>
        Public Sub Bug16887()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
 <compilation name="E">
     <file name="a.vb"><![CDATA[
Class SelfDestruct
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class     ]]></file>
 </compilation>, {MscorlibRef_v20})

            Dim obj = compilation.GetSpecialType(SpecialType.System_Object)
            Dim finalize = DirectCast(obj.GetMembers("Finalize").Single(), MethodSymbol)

            Assert.True(finalize.IsOverridable)
            Assert.False(finalize.IsOverrides)

            AssertTheseDiagnostics(compilation, <expected></expected>)
            CompileAndVerify(compilation)
        End Sub

        <WorkItem(608228, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608228")>
        <Fact>
        Public Sub OverridePropertyWithByRefParameter()
            Dim il = <![CDATA[
.class public auto ansi Base
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public newslot specialname strict virtual 
          instance string  get_P(int32& x) cil managed
  {
    ldnull
    ret
  }

  .method public newslot specialname strict virtual 
          instance void  set_P(int32& x,
                               string 'value') cil managed
  {
    ret
  }

  .property instance string P(int32&)
  {
    .set instance void Base::set_P(int32&, string)
    .get instance string Base::get_P(int32&)
  }
} // end of class Base
]]>

            Dim source =
               <compilation>
                   <file name="a.vb">
Public Class Derived
    Inherits Base

    Public Overrides Property P(x As Integer) As String
        Get
            Return Nothing
        End Get
        Set(value As String)

        End Set
    End Property
End Class
                    </file>
               </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il)

            ' Note: matches dev11, but not interface implementation (which treats the PEProperty as bogus).
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_OverrideWithByref2, "P").WithArguments("Public Overrides Property P(x As Integer) As String", "Public Overridable Property P(ByRef x As Integer) As String"))

            Dim globalNamespace = compilation.GlobalNamespace

            Dim baseType = globalNamespace.GetMember(Of NamedTypeSymbol)("Base")
            Dim baseProperty = baseType.GetMember(Of PropertySymbol)("P")

            Assert.True(baseProperty.Parameters.Single().IsByRef)

            Dim derivedType = globalNamespace.GetMember(Of NamedTypeSymbol)("Derived")
            Dim derivedProperty = derivedType.GetMember(Of PropertySymbol)("P")

            Assert.False(derivedProperty.Parameters.Single().IsByRef)

            ' Note: matches dev11, but not interface implementation (which treats the PEProperty as bogus).
            Assert.Equal(baseProperty, derivedProperty.OverriddenProperty)
        End Sub

        <Fact(), WorkItem(528549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528549")>
        Public Sub Bug528549()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
 <compilation>
     <file name="a.vb"><![CDATA[
Module CORError033mod
    NotOverridable Sub abcDef()
 
    End Sub

    Overrides Sub abcDef2()
 
    End Sub
End Module
     ]]></file>
 </compilation>, TestOptions.ReleaseDll)

            AssertTheseDeclarationDiagnostics(compilation,
<expected>
BC30433: Methods in a Module cannot be declared 'NotOverridable'.
    NotOverridable Sub abcDef()
    ~~~~~~~~~~~~~~
BC30433: Methods in a Module cannot be declared 'Overrides'.
    Overrides Sub abcDef2()
    ~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_01()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler doesn't produce any error, but neither method is considered overridden by the runtime.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30935: Member 'Public Overridable Function M1(x As Integer) As Integer' that matches this signature cannot be overridden because the class 'Base' contains multiple members with this same name and signature: 
   'Public Overridable Function M1(x As Integer) As Integer'
   'Public Overridable Function M1(x As Integer) As Integer'
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_02()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base::M1_1
            'Derived.M1
            'Base::M1_3
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="Base::M1_1" & Environment.NewLine & "Derived.M1" & Environment.NewLine & "Base::M1_3")
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_03()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base::M1_1
            'Derived.M1
            'Base::M1_3
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="Base::M1_1" & Environment.NewLine & "Derived.M1" & Environment.NewLine & "Base::M1_3")
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_04()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler doesn't produce any error, but neither method is considered overridden by the runtime.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30935: Member 'Public Overridable Function M1(x As Integer) As Integer' that matches this signature cannot be overridden because the class 'Base' contains multiple members with this same name and signature: 
   'Public Overridable Function M1(x As Integer) As Integer'
   'Public Overridable Function M1(x As Integer) As Integer'
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_05()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler doesn't produce any error, but neither method is considered overridden by the runtime.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30935: Member 'Public Overridable Function M1(x As Integer) As Integer' that matches this signature cannot be overridden because the class 'Base' contains multiple members with this same name and signature: 
   'Public Overridable Function M1(x As Integer) As Integer'
   'Public Overridable Function M1(x As Integer) As Integer'
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_06()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 BaseBase::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base::M1_1
            'Derived.M1
            'Base::M1_3
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="Base::M1_1" & Environment.NewLine & "Derived.M1" & Environment.NewLine & "Base::M1_3")
            compilation.VerifyDiagnostics()
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_07()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int64 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int64 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int32 M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_3"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int64 BaseBase::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler doesn't produce any error, but neither method is considered overridden by the runtime.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Long' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_08()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int64 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int64 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int64 BaseBase::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler doesn't produce any error, but neither method is considered overridden by the runtime.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Long' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_09()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int64 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int64 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int64 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 BaseBase::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Long' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_10()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int64 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int64 V_0)
  IL_0000:  ldstr      "Base::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int64 Base::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler: no errors, nothing is overridden
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Long' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_11()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public newslot strict virtual 
          instance int64 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int64 V_0)
  IL_0000:  ldstr      "Base::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int64 Base::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            ' Native compiler: no errors
            ' Derived.M1
            ' Base::M1_2
            ' Roslyn's behavior looks reasonable and it has nothing to do with custom modifiers.
            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30935: Member 'Public Overridable Function M1(x As Integer) As Integer' that matches this signature cannot be overridden because the class 'Base' contains multiple members with this same name and signature: 
   'Public Overridable Function M1(x As Integer) As Integer'
   'Public Overridable Function M1(x As Integer) As Long'
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_12()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldnull
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 BaseBase::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Integer()' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_13()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi BaseBase
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int32 V_0)
  IL_0000:  ldstr      "BaseBase::M1_2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldloc.0
  IL_000f:  ret
  } // end of method Base::M1


} // end of class BaseBase

.class public abstract auto ansi Base
       extends BaseBase
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseBase::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 x) cil managed
  {
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      "Base::M1_1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000e:  ldnull
  IL_000f:  ret
  } // end of method Base::M1

  .method public  
          instance void  Test() cil managed
  {
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::M1(int32)
    pop
    ldarg.0
    ldc.i4.0
    callvirt       instance int32 BaseBase::M1(int32)
    pop
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim x as Base = New Derived()
	x.Test()
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30437: 'Public Overrides Function M1(x As Integer) As Integer' cannot override 'Public Overridable Function M1(x As Integer) As Integer()' because they differ by their return types.
    Public Overrides Function M1(x As Integer) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_14()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method Base::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
  } // end of method Base::M2

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M3(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method Base::M3

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M11(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method Base::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] M12(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
  } // end of method Base::M2

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M13(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method Base::M3

  .method public newslot abstract strict virtual 
          instance !!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M4<T>(!!T y, !!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x, !!T [] z) cil managed
  {
  } // end of method Base::M4
  
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Imports System.Runtime.InteropServices

Module Module1
    Sub Main()
        Dim x as Base = New Derived()
        x.M1(Nothing)
        x.M2(Nothing)
        x.M3(Nothing)
        x.M11(Nothing)
        x.M12(Nothing)
        x.M13(Nothing)
        x.M4(Of Integer)(Nothing, Nothing, Nothing)
    End Sub
End Module

Class Derived
    Inherits Base

    Public Overrides Function M2(x() As Integer) As Integer()
        System.Console.WriteLine("Derived.M2")
        return Nothing
    End Function

    Public Overrides Function M1(x As Integer) As Integer
        System.Console.WriteLine("Derived.M1")
        return Nothing
    End Function

    Public Overrides Function M3(x() As Integer) As Integer()
        System.Console.WriteLine("Derived.M3")
        return Nothing
    End Function

    Public Overrides Function M12(<[In]> x() As Integer) As Integer()
        System.Console.WriteLine("Derived.M12")
        return Nothing
    End Function

    Public Overrides Function M11(<[In]> x As Integer) As Integer
        System.Console.WriteLine("Derived.M11")
        return Nothing
    End Function

    Public Overrides Function M13(<[In]> x() As Integer) As Integer()
        System.Console.WriteLine("Derived.M13")
        return Nothing
    End Function

    Public Overrides Function M4(Of S)(y as S, x() As S, z() as S) As S
        System.Console.WriteLine("Derived.M4")
        return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe,
                                                        expectedOutput:="Derived.M1" & Environment.NewLine & "Derived.M2" & Environment.NewLine & "Derived.M3" & Environment.NewLine &
                                                                        "Derived.M11" & Environment.NewLine & "Derived.M12" & Environment.NewLine & "Derived.M13" & Environment.NewLine &
                                                                        "Derived.M4")
            compilation.VerifyDiagnostics()

            Dim derived = DirectCast(compilation.Compilation, VisualBasicCompilation).GetTypeByMetadataName("Derived")

            Assert.IsAssignableFrom(Of SourceSimpleParameterSymbol)(derived.GetMember(Of MethodSymbol)("M1").Parameters(0))
            Assert.IsAssignableFrom(Of SourceSimpleParameterSymbol)(derived.GetMember(Of MethodSymbol)("M2").Parameters(0))
            Assert.IsAssignableFrom(Of SourceSimpleParameterSymbol)(derived.GetMember(Of MethodSymbol)("M3").Parameters(0))

            Assert.IsAssignableFrom(Of SourceComplexParameterSymbol)(derived.GetMember(Of MethodSymbol)("M11").Parameters(0))
            Assert.IsAssignableFrom(Of SourceComplexParameterSymbol)(derived.GetMember(Of MethodSymbol)("M12").Parameters(0))
            Assert.IsAssignableFrom(Of SourceComplexParameterSymbol)(derived.GetMember(Of MethodSymbol)("M13").Parameters(0))
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_15()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
        x = New Derived2()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived1.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived1.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived1.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived1.P2_set")
        End Set
    End Property
End Class

Class Derived2
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived2.P1_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived2.P2_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Derived1.P1_get" & Environment.NewLine &
"Derived1.P1_set" & Environment.NewLine &
"Derived1.P2_get" & Environment.NewLine &
"Derived1.P2_set" & Environment.NewLine &
"Derived2.P1_get" & Environment.NewLine &
"Derived2.P1_set" & Environment.NewLine &
"Derived2.P2_get" & Environment.NewLine &
"Derived2.P2_set")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_16()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
        x = New Derived2()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived1.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived1.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived1.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived1.P2_set")
        End Set
    End Property
End Class

Class Derived2
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived2.P1_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived2.P2_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Derived1.P1_get" & Environment.NewLine &
"Derived1.P1_set" & Environment.NewLine &
"Derived1.P2_get" & Environment.NewLine &
"Derived1.P2_set" & Environment.NewLine &
"Derived2.P1_get" & Environment.NewLine &
"Derived2.P1_set" & Environment.NewLine &
"Derived2.P2_get" & Environment.NewLine &
"Derived2.P2_set")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_17()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 )
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base

    Public Shared Sub Main()
        Dim x As Base = New Derived()
        x.Test()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30643: Property 'Base.P2(x As Integer())' is of an unsupported type.
    Public Overrides Property P2(x As Integer()) As Integer
                              ~~
                                               </expected>)

            vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
        x = New Derived2()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived1.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived1.P1_set")
        End Set
    End Property
End Class

Class Derived2
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived2.P1_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P1_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set
            Dim verifier = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Derived1.P1_get" & Environment.NewLine &
"Derived1.P1_set" & Environment.NewLine &
"Base.P2_get" & Environment.NewLine &
"Base.P2_set" & Environment.NewLine &
"Derived2.P1_get" & Environment.NewLine &
"Derived2.P1_set" & Environment.NewLine &
"Base.P2_get" & Environment.NewLine &
"Base.P2_set")
            verifier.VerifyDiagnostics()

            AssertOverridingProperty(verifier.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_18()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 [] P1(int32 )
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 P2(int32 [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base

    Public Shared Sub Main()
        Dim x As Base = New Derived()
        x.Test()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30643: Property 'Base.P2(x As Integer())' is of an unsupported type.
    Public Overrides Property P2(x As Integer()) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_19()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 []  get_P1(int32 x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 x,
                                int32 [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 get_P2(int32 [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 [] x,
                                int32 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 [] Base::get_P1(int32)
    IL_0009:  callvirt   instance void Base::set_P1(int32,
                                                    int32 [])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 Base::get_P2(int32 [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 [],
                                                    int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 [] Base::get_P1(int32 )
    .set instance void Base::set_P1(int32 ,
                                    int32 [])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32  Base::get_P2(int32  [])
    .set instance void Base::set_P2(int32 [],
                                    int32 )
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base

    Public Shared Sub Main()
        Dim x As Base = New Derived()
        x.Test()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Derived.P1_get
            'Derived.P1_set
            'Derived.P2_get
            'Derived.P2_set

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30643: Property 'Base.P2(x As Integer())' is of an unsupported type.
    Public Overrides Property P2(x As Integer()) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_20()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base1
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base1.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base1.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test1() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base1::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base1::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base1::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base1::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base

.class public abstract auto ansi Base2
       extends Base1
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base1::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 []  get_P1(int32 x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base2.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 x,
                                int32 [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 get_P2(int32 [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base2.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 [] x,
                                int32 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test2() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 [] Base2::get_P1(int32)
    IL_0009:  callvirt   instance void Base2::set_P1(int32,
                                                    int32 [])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 Base2::get_P2(int32 [])
    IL_0017:  callvirt   instance void Base2::set_P2(int32 [],
                                                    int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 [] Base2::get_P1(int32 )
    .set instance void Base2::set_P1(int32 ,
                                    int32 [])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32  Base2::get_P2(int32  [])
    .set instance void Base2::set_P2(int32 [],
                                    int32 )
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base2

    Public Shared Sub Main()
        Dim x As Base2 = New Derived()
        x.Test1()
        x.Test2()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base1.P1_get
            'Base1.P1_set
            'Base1.P2_get
            'Base1.P2_set
            'Derived.P1_get
            'Derived.P1_set
            'Derived.P2_get
            'Derived.P2_set

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30643: Property 'Base2.P2(x As Integer())' is of an unsupported type.
    Public Overrides Property P2(x As Integer()) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_21()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base1
       extends Base2
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base2::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base1.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base1.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test1() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base1::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base1::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base1::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base1::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base

.class public abstract auto ansi Base2
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 []  get_P1(int32 x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base2.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 x,
                                int32 [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 get_P2(int32 [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base2.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 [] x,
                                int32 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test2() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 [] Base2::get_P1(int32)
    IL_0009:  callvirt   instance void Base2::set_P1(int32,
                                                    int32 [])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 Base2::get_P2(int32 [])
    IL_0017:  callvirt   instance void Base2::set_P2(int32 [],
                                                    int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 [] Base2::get_P1(int32 )
    .set instance void Base2::set_P1(int32 ,
                                    int32 [])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32  Base2::get_P2(int32  [])
    .set instance void Base2::set_P2(int32 [],
                                    int32 )
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base1

    Public Shared Sub Main()
        Dim x As Base1 = New Derived()
        x.Test2()
        x.Test1()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Derived.P1_get
            'Derived.P1_set
            'Derived.P2_get
            'Derived.P2_set
            'Base1.P1_get
            'Base1.P1_set
            'Base1.P2_get
            'Base1.P2_set
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Base2.P1_get" & Environment.NewLine &
"Base2.P1_set" & Environment.NewLine &
"Base2.P2_get" & Environment.NewLine &
"Base2.P2_set" & Environment.NewLine &
"Derived.P1_get" & Environment.NewLine &
"Derived.P1_set" & Environment.NewLine &
"Derived.P2_get" & Environment.NewLine &
"Derived.P2_set"
)
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_22()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base1
       extends Base2
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base2::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 []  get_P1(int32 x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base1.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base1.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 [] x,
                                int32  'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base1.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test1() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32  [] Base1::get_P1(int32 )
    IL_0009:  callvirt   instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base1::set_P2(int32  [],
                                                    int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 [] Base1::get_P1(int32 )
    .set instance void Base1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base1::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base1::set_P2(int32 [],
                                    int32 )
  } // end of property Base::P2
} // end of class Base

.class public abstract auto ansi Base2
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base2.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 x,
                                int32 [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 get_P2(int32 [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base2.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base2.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test2() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base2::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base2::set_P1(int32,
                                                    int32 [])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 Base2::get_P2(int32 [])
    IL_0017:  callvirt   instance void Base2::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base2::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base2::set_P1(int32 ,
                                    int32 [])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32  Base2::get_P2(int32  [])
    .set instance void Base2::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Derived
    Inherits Base1

    Public Shared Sub Main()
        Dim x As Base1 = New Derived()
        x.Test2()
        x.Test1()
    End Sub

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base2.P1_get
            'Derived.P1_set
            'Derived.P2_get
            'Base2.P2_set
            'Derived.P1_get
            'Base1.P1_set
            'Base1.P2_get
            'Derived.P2_set

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(<expected>
BC30643: Property 'Base1.P2(x As Integer())' is of an unsupported type.
    Public Overrides Property P2(x As Integer()) As Integer
                              ~~
                                               </expected>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_23()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0002:  ldarg.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1()
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_0010:  ldarg.0
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2()
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1()
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2()
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1() As Integer()
    Public Overrides Property P2() As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_24()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 []  get_P1() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 get_P2() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0002:  ldarg.0
    IL_0004:  callvirt   instance int32 [] Base::get_P1()
    IL_0009:  callvirt   instance void Base::set_P1(int32 [])
    IL_000e:  ldarg.0
    IL_0010:  ldarg.0
    IL_0012:  callvirt   instance int32 Base::get_P2()
    IL_0017:  callvirt   instance void Base::set_P2(int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1()
  {
    .get instance int32 [] Base::get_P1()
    .set instance void Base::set_P1(int32 [])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2()
  {
    .get instance int32 Base::get_P2()
    .set instance void Base::set_P2(int32)
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1() As Integer()
    Public Overrides Property P2() As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_25()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0002:  ldarg.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1()
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_0010:  ldarg.0
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2()
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 [] P1()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1()
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 P2()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2()
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1() As Integer()
    Public Overrides Property P2() As Integer
End Class
]]>
                    </file>
                </compilation>

            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:="")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_26()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_0009:  callvirt   instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32[] P1(int32)
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[] Base::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
    .set instance void Base::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong),
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[])
  } // end of property Base::P1
  .property instance int32 P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
        x = New Derived2()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived1.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived1.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived1.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived1.P2_set")
        End Set
    End Property
End Class

Class Derived2
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived2.P1_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived2.P2_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Derived1.P1_get" & Environment.NewLine &
"Derived1.P1_set" & Environment.NewLine &
"Derived1.P2_get" & Environment.NewLine &
"Derived1.P2_set" & Environment.NewLine &
"Derived2.P1_get" & Environment.NewLine &
"Derived2.P1_set" & Environment.NewLine &
"Derived2.P2_get" & Environment.NewLine &
"Derived2.P2_set")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_27()
            Dim ilSource = <![CDATA[
.class public abstract auto ansi Base
       extends [mscorlib]System.Object
{
  .method family specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

  .method public newslot specialname strict virtual 
          instance int32  []  get_P1(int32  x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32[] V_0)
    IL_0000:  ldstr      "Base.P1_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldnull
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P1

  .method public newslot specialname strict virtual 
          instance void  set_P1(int32  x,
                                int32  [] 'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P1_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P1

  .method public newslot specialname strict virtual 
          instance int32  get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Base.P2_get"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method Base::get_P2

  .method public newslot specialname strict virtual 
          instance void  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x,
                                int32  'value') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Base.P2_set"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method Base::set_P2

  .method public instance void  Test() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  callvirt   instance int32  [] Base::get_P1(int32 )
    IL_0009:  callvirt   instance void Base::set_P1(int32 ,
                                                    int32 [])
    IL_000e:  ldarg.0
    IL_000f:  ldnull
    IL_0010:  ldarg.0
    IL_0011:  ldnull
    IL_0012:  callvirt   instance int32  Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    IL_0017:  callvirt   instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [],
                                                    int32 )
    IL_001c:  ret
  } // end of method Base::Test

  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong))
  {
    .get instance int32 [] Base::get_P1(int32 )
    .set instance void Base::set_P1(int32,
                                    int32[])
  } // end of property Base::P1
  .property instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  {
    .get instance int32 Base::get_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
    .set instance void Base::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)[],
                                    int32)
  } // end of property Base::P2
} // end of class Base
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module M
    Public Sub Main()
        Dim x As Base = New Derived1()
        x.Test()
        x = New Derived2()
        x.Test()
    End Sub
End Module

Class Derived1
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived1.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Derived1.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived1.P2_get")
            Return Nothing
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Derived1.P2_set")
        End Set
    End Property
End Class

Class Derived2
    Inherits Base

    Public Overrides Property P1(x As Integer) As Integer()
        Get
            System.Console.WriteLine("Derived2.P1_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P1_set")
        End Set
    End Property

    Public Overrides Property P2(x As Integer()) As Integer
        Get
            System.Console.WriteLine("Derived2.P2_get")
            Return Nothing
        End Get
        Set
            System.Console.WriteLine("Derived2.P2_set")
        End Set
    End Property
End Class
]]>
                    </file>
                </compilation>

            ' Output from native compiler:
            'Base.P1_get
            'Base.P1_set
            'Base.P2_get
            'Base.P2_set
            Dim compilation = CompileWithCustomILSource(vbSource, ilSource, options:=TestOptions.ReleaseExe, expectedOutput:=
"Derived1.P1_get" & Environment.NewLine &
"Derived1.P1_set" & Environment.NewLine &
"Derived1.P2_get" & Environment.NewLine &
"Derived1.P2_set" & Environment.NewLine &
"Derived2.P1_get" & Environment.NewLine &
"Derived2.P1_set" & Environment.NewLine &
"Derived2.P2_get" & Environment.NewLine &
"Derived2.P2_set")
            compilation.VerifyDiagnostics()

            AssertOverridingProperty(compilation.Compilation)
        End Sub

        Private Sub AssertOverridingProperty(compilation As Compilation)
            For Each namedType In compilation.SourceModule.GlobalNamespace.GetTypeMembers()
                If namedType.Name.StartsWith("Derived", StringComparison.OrdinalIgnoreCase) Then
                    For Each member In namedType.GetMembers()
                        If member.Kind = SymbolKind.Property Then
                            Dim thisProperty = DirectCast(member, PropertySymbol)
                            Dim overriddenProperty = thisProperty.OverriddenProperty

                            Assert.True(overriddenProperty.TypeCustomModifiers.SequenceEqual(thisProperty.TypeCustomModifiers))
                            Assert.Equal(overriddenProperty.Type, thisProperty.Type)

                            For i As Integer = 0 To thisProperty.ParameterCount - 1
                                Assert.True(overriddenProperty.Parameters(i).CustomModifiers.SequenceEqual(thisProperty.Parameters(i).CustomModifiers))
                                Assert.Equal(overriddenProperty.Parameters(i).Type, thisProperty.Parameters(i).Type)
                                Assert.True(overriddenProperty.Parameters(i).RefCustomModifiers.SequenceEqual(thisProperty.Parameters(i).RefCustomModifiers))
                            Next
                        End If
                    Next
                End If
            Next
        End Sub

        <Fact(), WorkItem(830352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830352")>
        Public Sub Bug830352()
            Dim code =
<compilation>
    <file name="a.vb">
Public Class Base
    Overridable Sub Test(Of T As Structure)(x As T?)
    End Sub
End Class

Class Derived
    Inherits Base

    Public Overrides Sub Test(Of T As Structure)(x As T?)
        MyBase.Test(x)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseDll)

            CompileAndVerify(comp).VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(837884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837884")>
        Public Sub Bug837884()
            Dim code1 =
<compilation>
    <file name="a.vb">
Public Class Cls
    Public Overridable Property r()
        Get
            Return 1
        End Get
        Friend Set(ByVal Value)
        End Set
    End Property
End Class
    </file>
</compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(code1, options:=TestOptions.ReleaseDll)

            CompileAndVerify(comp1).VerifyDiagnostics()

            Dim code2 =
<compilation>
    <file name="a.vb">
Class cls2
    Inherits Cls
    Public Overrides Property r() As Object
        Get
            Return 1
        End Get
        Friend Set(ByVal Value As Object)
        End Set
    End Property
End Class
    </file>
</compilation>

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(code2, {New VisualBasicCompilationReference(comp1)}, TestOptions.ReleaseDll)

            Dim expected = <expected>
BC31417: 'Friend Overrides Property Set r(Value As Object)' cannot override 'Friend Overridable Property Set r(Value As Object)' because it is not accessible in this context.
        Friend Set(ByVal Value As Object)
               ~~~
                           </expected>

            AssertTheseDeclarationDiagnostics(comp2, expected)

            Dim comp3 = CreateCompilationWithMscorlib40AndReferences(code2, {comp1.EmitToImageReference()}, TestOptions.ReleaseDll)
            AssertTheseDeclarationDiagnostics(comp3, expected)

        End Sub

        <WorkItem(1067044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067044")>
        <Fact>
        Public Sub Bug1067044()
            Dim il = <![CDATA[
.class public abstract auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance int32*  M1() cil managed
  {
  } // end of method C1::M1

  .method family hidebysig specialname rtspecialname 
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

.class public abstract auto ansi beforefieldinit C2
       extends C1
{
  .method public hidebysig virtual instance int32* 
          M1() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (int32* V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  conv.u
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  } // end of method C2::M1

  .method public hidebysig newslot abstract virtual 
          instance void  M2() cil managed
  {
  } // end of method C2::M2

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void C1::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C2::.ctor

} // end of class C2
]]>

            Dim source =
               <compilation>
                   <file name="a.vb">
Public Class C3
    Inherits C2

    Public Overrides Sub M2()
    End Sub
End Class
                    </file>
               </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il.Value, options:=TestOptions.DebugDll)

            CompileAndVerify(compilation)
        End Sub

        <Fact(), WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")>
        Public Sub AbstractGenericBase_01()
            Dim code =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1
    Public Shared Sub Main()
        Dim t = New Required()
        t.Test1(Nothing)
        t.Test2(Nothing)
    End Sub
End Class


Public MustInherit Class Validator
    Public MustOverride Sub DoValidate(objectToValidate As Object)

    Public Sub Test1(objectToValidate As Object)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class Validator(Of T)
    Inherits Validator

    Public Overrides Sub DoValidate(objectToValidate As Object)
        System.Console.WriteLine("void Validator<T>.DoValidate(object objectToValidate)")
    End Sub

    Protected MustOverride Overloads Sub DoValidate(objectToValidate As T)

    Public Sub Test2(objectToValidate As T)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class ValidatorBase(Of T)
    Inherits Validator(Of T)

    Protected Overrides Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void ValidatorBase<T>.DoValidate(T objectToValidate)")
    End Sub
End Class

Public Class Required
    Inherits ValidatorBase(Of Object)
End Class
        ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseExe)

            Dim validatorBaseT = compilation.GetTypeByMetadataName("ValidatorBase`1")
            Dim doValidateT = validatorBaseT.GetMember(Of MethodSymbol)("DoValidate")

            Assert.Equal(1, doValidateT.OverriddenMembers.OverriddenMembers.Length)
            Assert.Equal("Sub Validator(Of T).DoValidate(objectToValidate As T)", doValidateT.OverriddenMethod.ToTestDisplayString())

            Dim validatorBaseObject = validatorBaseT.Construct(compilation.ObjectType)
            Dim doValidateObject = validatorBaseObject.GetMember(Of MethodSymbol)("DoValidate")

            Assert.Equal(2, doValidateObject.OverriddenMembers.OverriddenMembers.Length)
            Assert.Equal("Sub Validator(Of T).DoValidate(objectToValidate As T)", doValidateObject.OverriddenMethod.OriginalDefinition.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)")
        End Sub

        <Fact(), WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")>
        Public Sub AbstractGenericBase_02()
            Dim code =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1

    Public Shared Sub Main()
        Dim t = New Required()
        t.Test1(Nothing)
        t.Test2(Nothing)
    End Sub
End Class


Public MustInherit Class Validator
    Public MustOverride Sub DoValidate(objectToValidate As Object)

    Public Sub Test1(objectToValidate As Object)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class Validator(Of T)
    Inherits Validator

    Public MustOverride Overrides Sub DoValidate(objectToValidate As Object)

    Public Overloads Overridable Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void Validator<T>.DoValidate(T objectToValidate)")
    End Sub

    Public Sub Test2(objectToValidate As T)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class ValidatorBase(Of T)
    Inherits Validator(Of T)

    Public Overrides Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void ValidatorBase<T>.DoValidate(T objectToValidate)")
    End Sub
End Class

Public Class Required
    Inherits ValidatorBase(Of Object)
End Class
        ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
                <expected>
BC30610: Class 'Required' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    Validator(Of Object): Public MustOverride Overrides Sub DoValidate(objectToValidate As Object).
Public Class Required
             ~~~~~~~~
                </expected>)
        End Sub

        <Fact(), WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")>
        Public Sub AbstractGenericBase_03()
            Dim code =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1

    Public Shared Sub Main()
        Dim t = New Required()
        t.Test1(Nothing)
        t.Test2(Nothing)
    End Sub
End Class


Public MustInherit Class Validator0(Of T)
    Public MustOverride Sub DoValidate(objectToValidate As T)

    Public Sub Test2(objectToValidate As T)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class Validator(Of T)
    Inherits Validator0(Of T)

    Public Overloads Overridable Sub DoValidate(objectToValidate As Object)
        System.Console.WriteLine("void Validator<T>.DoValidate(object objectToValidate)")
    End Sub

    Public Sub Test1(objectToValidate As Object)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class ValidatorBase(Of T)
    Inherits Validator(Of T)

    Public Overrides Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void ValidatorBase<T>.DoValidate(T objectToValidate)")
    End Sub
End Class

Public Class Required
    Inherits ValidatorBase(Of Object)
End Class
        ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)")
        End Sub

        <Fact(), WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")>
        Public Sub AbstractGenericBase_04()
            Dim code =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1
    Public Shared Sub Main()
        Dim t = New Required()
        t.Test1(Nothing)
        t.Test2(Nothing)
    End Sub
End Class


Public MustInherit Class Validator
    Public MustOverride Sub DoValidate(objectToValidate As Object)

    Public Sub Test1(objectToValidate As Object)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class Validator(Of T)
    Inherits Validator

    Public Overloads Overridable Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void Validator<T>.DoValidate(T objectToValidate)")
    End Sub

    Public Sub Test2(objectToValidate As T)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class ValidatorBase(Of T)
    Inherits Validator(Of T)

    Public Overrides Sub DoValidate(objectToValidate As Object)
        System.Console.WriteLine("void ValidatorBase<T>.DoValidate(object objectToValidate)")
    End Sub
End Class

Public Class Required
    Inherits ValidatorBase(Of Object)
End Class
        ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="void ValidatorBase<T>.DoValidate(object objectToValidate)
void Validator<T>.DoValidate(T objectToValidate)")
        End Sub

        <Fact(), WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")>
        Public Sub AbstractGenericBase_05()
            Dim code =
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1
    Public Shared Sub Main()
        Dim t = New Required()
        t.Test1(Nothing)
        t.Test2(Nothing)
    End Sub
End Class


Public MustInherit Class Validator0(Of T)
    Public MustOverride Sub DoValidate(objectToValidate As Object)

    Public Sub Test1(objectToValidate As Object)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class Validator(Of T)
    Inherits Validator0(Of Integer)

    Public Overrides Sub DoValidate(objectToValidate As Object)
        System.Console.WriteLine("void Validator<T>.DoValidate(object objectToValidate)")
    End Sub

    Protected MustOverride Overloads Sub DoValidate(objectToValidate As T)

    Public Sub Test2(objectToValidate As T)
        DoValidate(objectToValidate)
    End Sub
End Class

Public MustInherit Class ValidatorBase(Of T)
    Inherits Validator(Of T)

    Protected Overrides Sub DoValidate(objectToValidate As T)
        System.Console.WriteLine("void ValidatorBase<T>.DoValidate(T objectToValidate)")
    End Sub
End Class

Public Class Required
    Inherits ValidatorBase(Of Object)
End Class
        ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(code, options:=TestOptions.ReleaseExe)

            Dim validatorBaseT = compilation.GetTypeByMetadataName("ValidatorBase`1")
            Dim doValidateT = validatorBaseT.GetMember(Of MethodSymbol)("DoValidate")

            Assert.Equal(1, doValidateT.OverriddenMembers.OverriddenMembers.Length)
            Assert.Equal("Sub Validator(Of T).DoValidate(objectToValidate As T)", doValidateT.OverriddenMethod.ToTestDisplayString())

            Dim validatorBaseObject = validatorBaseT.Construct(compilation.ObjectType)
            Dim doValidateObject = validatorBaseObject.GetMember(Of MethodSymbol)("DoValidate")

            Assert.Equal(2, doValidateObject.OverriddenMembers.OverriddenMembers.Length)
            Assert.Equal("Sub Validator(Of T).DoValidate(objectToValidate As T)", doValidateObject.OverriddenMethod.OriginalDefinition.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)")
        End Sub
    End Class
End Namespace
